using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using FinanceSimplify.Data;
using FinanceSimplify.Dtos.BankAccount;
using FinanceSimplify.Dtos.Category;
using FinanceSimplify.Dtos.Insights;
using FinanceSimplify.Dtos.Transactions;
using FinanceSimplify.Enum;
using FinanceSimplify.Models.Insight;
using FinanceSimplify.Models.Transaction;
using FinanceSimplify.Services.AiService;
using FinanceSimplify.Services.BankAccountService;
using FinanceSimplify.Services.Category;
using FinanceSimplify.Services.TransactionService;
using MongoDB.Driver;

namespace FinanceSimplify.Services.InsightService {
    public class InsightService : IInsightInterface {
        private readonly IAiProvider _aiProvider;
        private readonly ITransactionInterface _transactionService;
        private readonly ICategoryInterface _categoryService;
        private readonly IBankAccountInterface _bankAccountService;
        private readonly MongoDbContext _context;
        private readonly ILogger<InsightService> _logger;

        // Rate limiting em memória: UserId -> Fila de timestamps das últimas requisições
        private static readonly ConcurrentDictionary<Guid, ConcurrentQueue<DateTime>> _userRateLimits = new();
        private const int MaxMessagesPerHour = 30;

        public InsightService(
            IAiProvider aiProvider,
            ITransactionInterface transactionService,
            ICategoryInterface categoryService,
            IBankAccountInterface bankAccountService,
            MongoDbContext context,
            ILogger<InsightService> logger) {
            _aiProvider = aiProvider;
            _transactionService = transactionService;
            _categoryService = categoryService;
            _bankAccountService = bankAccountService;
            _context = context;
            _logger = logger;
        }

        public async Task<InsightResponseModel<ChatResponseDto>> ProcessChat(Guid userId, ChatRequestDto request) {
            var response = new InsightResponseModel<ChatResponseDto>();

            try {
                // 1. Rate Limit
                if (IsRateLimited(userId)) {
                    response.Status = false;
                    response.Message = "Você atingiu o limite de mensagens do assistente por hora. Aguarde alguns instantes.";
                    response.Data = new ChatResponseDto {
                        Response = "Você atingiu o limite de mensagens por hora. Por favor, aguarde alguns minutos antes de enviar uma nova solicitação.",
                        IsAction = false
                    };
                    return response;
                }

                // 2. Coletar dados agregados do usuário (últimos 30 dias) de forma anônima
                var now = DateTime.UtcNow;
                var startDate = new DateTime(now.Year, now.Month, 1);
                var endDate = startDate.AddMonths(1).AddTicks(-1);

                var transactions = await _transactionService.GetTransactionByDateRange(userId, startDate, endDate);
                var recentTransactions = await _transactionService.GetTransactionsByUserId(userId, 1, 100);
                var categories = await _categoryService.GetCategoriesByUserId(userId, 1, 100);
                var bankAccounts = await _bankAccountService.GetBankAccountsByUserId(userId, 1, 10);

                // Se não houver movimentações no mês corrente exato, baseia a análise em todas as transações recentes registradas
                var analysisTransactions = (transactions != null && transactions.Count > 0)
                    ? transactions
                    : recentTransactions;

                var totalIncome = analysisTransactions.Where(t => t.Type == TypeTransactionEnum.Receita).Sum(t => t.Amount);
                var totalExpense = analysisTransactions.Where(t => t.Type == TypeTransactionEnum.Despesa).Sum(t => t.Amount);
                var netSavings = totalIncome - totalExpense;
                var savingsRate = totalIncome > 0 ? Math.Round((netSavings / totalIncome) * 100, 1) : 0;

                // Agrupamento por categoria
                var categoryExpenses = analysisTransactions
                    .Where(t => t.Type == TypeTransactionEnum.Despesa && t.CategoryId.HasValue)
                    .GroupBy(t => t.CategoryId!.Value)
                    .Select(g => {
                        var catName = categories.FirstOrDefault(c => c.Id == g.Key)?.Name ?? "Outros";
                        return new { Category = catName, Total = g.Sum(x => x.Amount) };
                    })
                    .OrderByDescending(x => x.Total)
                    .Take(5)
                    .ToList();

                var topCategory = categoryExpenses.FirstOrDefault();

                // 3. Tentar detectar intenção via Gemini (ou fallback se offline)
                var intentResult = await ParseIntentWithAiOrFallback(
                    request.Message,
                    categories.Select(c => c.Name).ToList(),
                    recentTransactions);

                // 4. Executar ação detectada: CRIAR, DELETAR ou EDITAR transação
                if (intentResult.Intent == "create_transaction" && intentResult.Amount > 0) {
                    var actionDto = await ExecuteCreateTransaction(userId, intentResult, categories, bankAccounts);
                    response.Status = true;
                    response.Message = "Transação criada com sucesso.";
                    response.Data = new ChatResponseDto {
                        Response = intentResult.Reply ?? $"Pronto! Registrei a despesa de {intentResult.Amount.ToString("C", new CultureInfo("pt-BR"))} em {intentResult.Category}.",
                        IsAction = true,
                        ActionType = "create_transaction",
                        ActionDetails = actionDto
                    };
                    return response;
                }

                if (intentResult.Intent == "delete_transaction") {
                    var actionDto = await ExecuteDeleteTransaction(intentResult, recentTransactions, categories);
                    if (actionDto != null) {
                        response.Status = true;
                        response.Message = "Transação excluída com sucesso.";
                        response.Data = new ChatResponseDto {
                            Response = intentResult.Reply ?? $"Pronto! Excluí a transação {actionDto.Name} de {actionDto.FormattedAmount}.",
                            IsAction = true,
                            ActionType = "delete_transaction",
                            ActionDetails = actionDto
                        };
                        return response;
                    } else {
                        response.Status = true;
                        response.Message = "Transação não encontrada para exclusão.";
                        response.Data = new ChatResponseDto {
                            Response = "Não encontrei a transação que você deseja excluir. Poderia me confirmar o nome ou o valor exato dela?",
                            IsAction = false
                        };
                        return response;
                    }
                }

                if (intentResult.Intent == "edit_transaction") {
                    var actionDto = await ExecuteEditTransaction(intentResult, recentTransactions, categories);
                    if (actionDto != null) {
                        response.Status = true;
                        response.Message = "Transação atualizada com sucesso.";
                        response.Data = new ChatResponseDto {
                            Response = intentResult.Reply ?? $"Pronto! Atualizei a transação {actionDto.Name} para {actionDto.FormattedAmount}.",
                            IsAction = true,
                            ActionType = "edit_transaction",
                            ActionDetails = actionDto
                        };
                        return response;
                    } else {
                        response.Status = true;
                        response.Message = "Transação não encontrada para edição.";
                        response.Data = new ChatResponseDto {
                            Response = "Não encontrei a transação que você deseja alterar. Poderia me informar o nome original ou o valor dela?",
                            IsAction = false
                        };
                        return response;
                    }
                }

                // 5. Se for dúvida/conversa sobre finanças
                string aiText;
                if (_aiProvider.IsConfigured) {
                    var financialSummaryPrompt = BuildFinancialSummaryPrompt(
                        request.Message, totalIncome, totalExpense, netSavings, savingsRate, categoryExpenses, topCategory?.Category);

                    var systemInstruction =
                        "Você é o assistente virtual de Private Wealth do FinanceSimplify. " +
                        "Seu tom é polido, sofisticado, encorajador e objetivo. Fale em português do Brasil. " +
                        "Não invente números fictícios; baseie-se estritamente nos totais agregados informados no resumo. " +
                        "Não forneça conselhos de investimentos de risco ou promessas de ganhos. " +
                        "Seja conciso, até 3 parágrafos curtos.";

                    aiText = await _aiProvider.GenerateContentAsync(financialSummaryPrompt, systemInstruction);
                } else {
                    aiText = string.Empty;
                }

                // Fallback inteligente se IA falhar ou não estiver configurada
                if (string.IsNullOrWhiteSpace(aiText)) {
                    aiText = GenerateLocalFallbackResponse(request.Message, totalIncome, totalExpense, netSavings, savingsRate, topCategory?.Category, topCategory?.Total ?? 0);
                }

                response.Status = true;
                response.Message = "Resposta gerada com sucesso.";
                response.Data = new ChatResponseDto {
                    Response = aiText,
                    IsAction = false
                };
                return response;

            } catch (Exception ex) {
                _logger.LogError(ex, "Erro ao processar chat no InsightService.");
                response.Status = false;
                response.Message = "Não foi possível processar sua mensagem no momento.";
                response.Data = new ChatResponseDto {
                    Response = "Desculpe, tive uma instabilidade momentânea ao processar sua solicitação. Por favor, tente novamente.",
                    IsAction = false
                };
                return response;
            }
        }

        public async Task<InsightResponseModel<MonthlyInsightDto>> GetMonthlyInsight(Guid userId, bool forceRefresh = false) {
            var response = new InsightResponseModel<MonthlyInsightDto>();

            try {
                var now = DateTime.UtcNow;
                var currentYear = now.Year;
                var currentMonth = now.Month;

                // 1. Verificar cache do MongoDB se não for forçado
                if (!forceRefresh) {
                    var cached = await _context.Insights
                        .Find(x => x.UserId == userId && x.Year == currentYear && x.Month == currentMonth)
                        .SortByDescending(x => x.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (cached != null) {
                        response.Status = true;
                        response.Message = "Insight recuperado do cache.";
                        response.Data = new MonthlyInsightDto {
                            Title = cached.Title,
                            Summary = cached.Summary,
                            Tag = cached.Tag,
                            Type = cached.Type,
                            Recommendation = cached.Recommendation,
                            GeneratedAt = cached.CreatedAt,
                            TotalIncome = cached.TotalIncome,
                            TotalExpense = cached.TotalExpense,
                            NetSavings = cached.NetSavings,
                            SavingsRate = cached.SavingsRate
                        };
                        return response;
                    }
                }

                // 2. Agregar dados do mês corrente
                var startDate = new DateTime(currentYear, currentMonth, 1);
                var endDate = startDate.AddMonths(1).AddTicks(-1);

                var transactions = await _transactionService.GetTransactionByDateRange(userId, startDate, endDate);
                if (transactions == null || transactions.Count == 0) {
                    transactions = await _transactionService.GetTransactionsByUserId(userId, 1, 100);
                }
                var totalIncome = transactions.Where(t => t.Type == TypeTransactionEnum.Receita).Sum(t => t.Amount);
                var totalExpense = transactions.Where(t => t.Type == TypeTransactionEnum.Despesa).Sum(t => t.Amount);
                var netSavings = totalIncome - totalExpense;
                var savingsRate = totalIncome > 0 ? Math.Round((netSavings / totalIncome) * 100, 1) : 0;

                MonthlyInsightDto insightDto;

                // Se houver Gemini configurado, gera síntese personalizada
                if (_aiProvider.IsConfigured && (totalIncome > 0 || totalExpense > 0)) {
                    var prompt = $"Analise este mês: Receitas: R$ {totalIncome:N2}, Despesas: R$ {totalExpense:N2}, Saldo: R$ {netSavings:N2}, Taxa de Poupança: {savingsRate}%. " +
                        "Gere um JSON com: title (string curta), summary (2 frases elegantes de diagnóstico), tag (palavra de status), type ('saving', 'warning', ou 'positive'), recommendation (1 conselho financeiro prudente).";

                    var jsonResponse = await _aiProvider.GenerateContentAsync(prompt, "Você é um analista sênior de Private Wealth. Responda APENAS em JSON.", jsonOutput: true);

                    var parsed = ParseMonthlyInsightJson(jsonResponse);
                    if (parsed != null) {
                        insightDto = parsed;
                    } else {
                        insightDto = GenerateDefaultMonthlyInsight(totalIncome, totalExpense, netSavings, savingsRate);
                    }
                } else {
                    insightDto = GenerateDefaultMonthlyInsight(totalIncome, totalExpense, netSavings, savingsRate);
                }

                insightDto.TotalIncome = totalIncome;
                insightDto.TotalExpense = totalExpense;
                insightDto.NetSavings = netSavings;
                insightDto.SavingsRate = savingsRate;
                insightDto.GeneratedAt = DateTime.UtcNow;

                // 3. Salvar no MongoDB
                var cacheEntry = new InsightCacheModel {
                    UserId = userId,
                    Year = currentYear,
                    Month = currentMonth,
                    Title = insightDto.Title,
                    Summary = insightDto.Summary,
                    Tag = insightDto.Tag,
                    Type = insightDto.Type,
                    Recommendation = insightDto.Recommendation,
                    TotalIncome = totalIncome,
                    TotalExpense = totalExpense,
                    NetSavings = netSavings,
                    SavingsRate = savingsRate,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Insights.InsertOneAsync(cacheEntry);

                response.Status = true;
                response.Message = "Insight mensal computado com sucesso.";
                response.Data = insightDto;
                return response;

            } catch (Exception ex) {
                _logger.LogError(ex, "Erro ao gerar Insight Mensal no InsightService.");
                response.Status = false;
                response.Message = "Erro ao gerar insight.";
                return response;
            }
        }

        #region Métodos Auxiliares e Fallbacks

        private bool IsRateLimited(Guid userId) {
            var now = DateTime.UtcNow;
            var queue = _userRateLimits.GetOrAdd(userId, _ => new ConcurrentQueue<DateTime>());

            // Remove chamadas com mais de 1 hora
            while (queue.TryPeek(out var oldest) && (now - oldest).TotalHours > 1) {
                queue.TryDequeue(out _);
            }

            if (queue.Count >= MaxMessagesPerHour) {
                return true;
            }

            queue.Enqueue(now);
            return false;
        }

        private async Task<IntentParseResult> ParseIntentWithAiOrFallback(
            string message,
            List<string> knownCategories,
            List<FinanceSimplify.Models.Transaction.TransactionModel> recentTransactions) {

            var lower = message.ToLower();
            var actionKeywords = new[] {
                "adiciona", "cria", "registra", "inserir", "gastei", "despesa", "receita", "paguei", "comprei",
                "deleta", "apaga", "exclui", "remove", "cancela",
                "muda", "altera", "edita", "troca", "atualiza"
            };
            bool hasActionKeyword = actionKeywords.Any(k => lower.Contains(k));

            if (!hasActionKeyword) {
                return new IntentParseResult { Intent = "chat" };
            }

            // Se o Gemini estiver configurado, podemos pedir para interpretar intenção e alvo com precisão
            if (_aiProvider.IsConfigured) {
                try {
                    var categoriesList = string.Join(", ", knownCategories);
                    var todayIso = DateTime.UtcNow.ToString("yyyy-MM-dd");
                    var recentTxSummary = string.Join("\n", recentTransactions.Take(15).Select(t =>
                        $"ID: {t.Id} | Nome: \"{t.Name}\" | Valor: R$ {t.Amount:N2} | Data: {t.Date:yyyy-MM-dd} | Categoria: {knownCategories.FirstOrDefault() ?? "Geral"}"));

                    var prompt = $"Analise a seguinte mensagem do usuário e determine a ação financeira desejada:\n\"{message}\"\n\n" +
                        $"Data de referência (hoje): {todayIso}.\n" +
                        $"Categorias existentes do usuário: [{categoriesList}].\n" +
                        $"Transações recentes do usuário:\n{recentTxSummary}\n\n" +
                        "Instruções:\n" +
                        "- \"create_transaction\": registrar nova transação (name, amount, date, type, category, reply)\n" +
                        "- \"delete_transaction\": excluir transação existente da lista (targetTransactionId com o ID correspondente da lista acima, name, amount, reply)\n" +
                        "- \"edit_transaction\": alterar transação existente (targetTransactionId com o ID da transação da lista, name [novo nome ou null], amount [novo valor ou null], date [nova data ou null], category [nova categoria ou null], reply)\n" +
                        "- \"chat\": se for dúvida ou conversa sem comando direto\n\n" +
                        "REGRAS CRÍTICAS:\n" +
                        "- name: OBRIGATÓRIO extrair o nome REAL e conciso do item mencionado (Ex: 'Claro', 'Almoço', 'Uber', 'Mercado', 'Academia', 'Freelance Site'). NUNCA inclua prefixos como 'Cadastra', 'Adiciona', 'Despesa', 'No Valor De', etc. NUNCA retorne 'Despesa Outros', 'Outros' ou 'Transação'!\n" +
                        "- type: se o usuário mencionar termos de ganho/entrada ('entrada', 'receita', 'recebi', 'ganhei', 'salário', 'freelance', 'depósito', 'rendimento', 'venda'), defina OBRIGATORIAMENTE 'type': 'Receita'. Caso contrário, 'Despesa'.\n\n" +
                        "Responda SOMENTE em formato JSON com o seguinte esquema:\n" +
                        "{\n" +
                        "  \"intent\": \"create_transaction\" ou \"delete_transaction\" ou \"edit_transaction\" ou \"chat\",\n" +
                        "  \"targetTransactionId\": \"guid ou null\",\n" +
                        "  \"name\": \"nome real descritivo do item\",\n" +
                        "  \"amount\": número decimal do valor,\n" +
                        "  \"date\": \"YYYY-MM-DD ou null\",\n" +
                        "  \"type\": \"Despesa\" ou \"Receita\",\n" +
                        "  \"category\": \"categoria ou null\",\n" +
                        "  \"reply\": \"resposta amigável confirmando a ação realizada\"\n" +
                        "}";

                    var json = await _aiProvider.GenerateContentAsync(prompt, "Você é um assistente financeiro que extrai comandos estruturados com nomes limpos. Retorne SOMENTE JSON válido.", jsonOutput: true);
                    if (!string.IsNullOrWhiteSpace(json)) {
                        var cleanJson = json.Trim();
                        if (cleanJson.StartsWith("```json")) cleanJson = cleanJson[7..];
                        if (cleanJson.StartsWith("```")) cleanJson = cleanJson[3..];
                        if (cleanJson.EndsWith("```")) cleanJson = cleanJson[..^3];
                        cleanJson = cleanJson.Trim();

                        using var doc = JsonDocument.Parse(cleanJson);
                        var root = doc.RootElement;

                        var intentType = root.TryGetProperty("intent", out var intProp) ? intProp.GetString() : "chat";
                        if (intentType == "create_transaction" || intentType == "delete_transaction" || intentType == "edit_transaction") {
                            decimal amount = 0;
                            if (root.TryGetProperty("amount", out var amtProp)) {
                                if (amtProp.ValueKind == JsonValueKind.Number) amount = amtProp.GetDecimal();
                                else if (decimal.TryParse(amtProp.GetString()?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedAmt)) amount = parsedAmt;
                            }

                            Guid? targetId = null;
                            if (root.TryGetProperty("targetTransactionId", out var idProp) && Guid.TryParse(idProp.GetString(), out var parsedId)) {
                                targetId = parsedId;
                            }

                            DateTime? parsedDate = null;
                            if (root.TryGetProperty("date", out var dtProp) && !string.IsNullOrWhiteSpace(dtProp.GetString())) {
                                if (DateTime.TryParse(dtProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) {
                                    parsedDate = DateTime.SpecifyKind(d, DateTimeKind.Utc);
                                }
                            }

                            var name = root.TryGetProperty("name", out var nProp) ? nProp.GetString() : null;
                            var type = root.TryGetProperty("type", out var tProp) ? tProp.GetString() ?? "Despesa" : "Despesa";
                            var category = root.TryGetProperty("category", out var cProp) ? cProp.GetString() ?? "Outros" : "Outros";
                            var reply = root.TryGetProperty("reply", out var rProp) ? rProp.GetString() : null;

                            // Se o nome retornado pela IA for genérico ou contiver prefixos residuais de comando, extrai o nome real da mensagem
                            if (string.IsNullOrWhiteSpace(name) ||
                                name.Equals("Transação", StringComparison.OrdinalIgnoreCase) ||
                                name.Equals("Despesa", StringComparison.OrdinalIgnoreCase) ||
                                name.Equals("Receita", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("Outros", StringComparison.OrdinalIgnoreCase) ||
                                name.StartsWith("cadastra", StringComparison.OrdinalIgnoreCase) ||
                                name.StartsWith("adiciona", StringComparison.OrdinalIgnoreCase) ||
                                name.StartsWith("cria", StringComparison.OrdinalIgnoreCase) ||
                                name.StartsWith("registra", StringComparison.OrdinalIgnoreCase) ||
                                name.StartsWith("insere", StringComparison.OrdinalIgnoreCase) ||
                                name.StartsWith("lanca", StringComparison.OrdinalIgnoreCase) ||
                                name.StartsWith("lança", StringComparison.OrdinalIgnoreCase)) {
                                name = ExtractCleanTransactionName(message, amount, type == "Receita", category);
                            }

                            return new IntentParseResult {
                                Intent = intentType,
                                TargetTransactionId = targetId,
                                Name = name,
                                Amount = amount,
                                Date = parsedDate,
                                Type = type,
                                Category = category,
                                Reply = reply
                            };
                        }
                    }
                } catch (Exception ex) {
                    _logger.LogWarning(ex, "Erro ao interpretar intenção via Gemini. Usando parser local.");
                }
            }

            // Fallback via regex e correspondência local
            return ParseIntentLocally(message, recentTransactions);
        }

        private IntentParseResult ParseIntentLocally(string message, List<FinanceSimplify.Models.Transaction.TransactionModel> recentTransactions) {
            var lower = message.ToLower();

            // 1. Detectar intenção de DELETAR
            var isDelete = lower.Contains("deleta") || lower.Contains("apaga") || lower.Contains("exclui") || lower.Contains("remove") || lower.Contains("cancela");
            if (isDelete) {
                var target = FindMatchingTransaction(message, recentTransactions);
                if (target != null) {
                    var formatted = target.Amount.ToString("C", new CultureInfo("pt-BR"));
                    return new IntentParseResult {
                        Intent = "delete_transaction",
                        TargetTransactionId = target.Id,
                        Name = target.Name,
                        Amount = target.Amount,
                        Reply = $"Pronto! Excluí a transação {target.Name} de {formatted}."
                    };
                }
            }

            // 2. Detectar intenção de EDITAR
            var isEdit = lower.Contains("muda") || lower.Contains("altera") || lower.Contains("edita") || lower.Contains("troca") || lower.Contains("atualiza");
            if (isEdit) {
                var target = FindMatchingTransaction(message, recentTransactions);
                if (target != null) {
                    // Buscar novo valor especificado na mensagem (ex: "para 120" ou "120,00")
                    var newAmount = target.Amount;
                    var matchNumbers = Regex.Matches(message, @"(?:r\$|\$)?\s*(\d+(?:[.,]\d{1,2})?)", RegexOptions.IgnoreCase);
                    if (matchNumbers.Count > 0) {
                        var lastMatch = matchNumbers[^1].Groups[1].Value.Replace(',', '.');
                        if (decimal.TryParse(lastMatch, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedVal) && parsedVal > 0) {
                            newAmount = parsedVal;
                        }
                    }

                    // Buscar nova data se especificada
                    DateTime? newDate = null;
                    var dateMatch = Regex.Match(message, @"\b(\d{1,2})/(\d{1,2})(?:/(\d{2,4}))?\b");
                    if (dateMatch.Success) {
                        int day = int.Parse(dateMatch.Groups[1].Value);
                        int month = int.Parse(dateMatch.Groups[2].Value);
                        int year = dateMatch.Groups[3].Success ? int.Parse(dateMatch.Groups[3].Value) : DateTime.UtcNow.Year;
                        if (year < 100) year += 2000;
                        try { newDate = DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc); } catch { }
                    }

                    var formatted = newAmount.ToString("C", new CultureInfo("pt-BR"));
                    return new IntentParseResult {
                        Intent = "edit_transaction",
                        TargetTransactionId = target.Id,
                        Name = target.Name,
                        Amount = newAmount,
                        Date = newDate ?? target.Date,
                        Reply = $"Pronto! Atualizei a transação {target.Name} para {formatted}."
                    };
                }
            }

            // 3. Detectar intenção de CRIAR (padrão)
            var amountMatch = Regex.Match(message, @"(?:r\$|\$)?\s*(\d+(?:[.,]\d{1,2})?)", RegexOptions.IgnoreCase);
            if (!amountMatch.Success) {
                return new IntentParseResult { Intent = "chat" };
            }

            var rawAmount = amountMatch.Groups[1].Value.Replace(',', '.');
            if (!decimal.TryParse(rawAmount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) || amount <= 0) {
                return new IntentParseResult { Intent = "chat" };
            }

            // Extração de data
            DateTime txDate = DateTime.UtcNow;
            var dtMatch = Regex.Match(message, @"\b(\d{1,2})/(\d{1,2})(?:/(\d{2,4}))?\b");
            if (dtMatch.Success) {
                int day = int.Parse(dtMatch.Groups[1].Value);
                int month = int.Parse(dtMatch.Groups[2].Value);
                int year = dtMatch.Groups[3].Success ? int.Parse(dtMatch.Groups[3].Value) : DateTime.UtcNow.Year;
                if (year < 100) year += 2000;
                try { txDate = DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc); } catch { }
            } else if (lower.Contains("ontem")) {
                txDate = DateTime.UtcNow.AddDays(-1);
            } else if (lower.Contains("anteontem")) {
                txDate = DateTime.UtcNow.AddDays(-2);
            } else if (lower.Contains("amanhã") || lower.Contains("amanha")) {
                txDate = DateTime.UtcNow.AddDays(1);
            }

            var incomeKeywords = new[] {
                "entrada", "entradas", "receita", "receitas", "recebi", "ganhei", "salário", "salario",
                "depósito", "deposito", "pix recebido", "rendimento", "venda", "freelance", "freela",
                "bônus", "bonus", "comissão", "comissao", "proventos", "lucro", "faturamento", "recebimento"
            };
            var isIncome = incomeKeywords.Any(k => lower.Contains(k));
            var type = isIncome ? "Receita" : "Despesa";

            // Inferir categoria inteligente
            string category = isIncome ? "Rendimentos" : "Outros";
            if (lower.Contains("claro") || lower.Contains("vivo") || lower.Contains("tim") || lower.Contains("internet") || lower.Contains("celular") || lower.Contains("telefone") || lower.Contains("netflix") || lower.Contains("spotify") || lower.Contains("assinatura")) category = "Assinatura";
            else if (lower.Contains("mercado") || lower.Contains("compras") || lower.Contains("supermercado") || lower.Contains("carrefour")) category = "Mercado";
            else if (lower.Contains("almoço") || lower.Contains("almoco") || lower.Contains("jantar") || lower.Contains("restaurante") || lower.Contains("comida") || lower.Contains("lanche") || lower.Contains("padaria") || lower.Contains("café") || lower.Contains("cafe") || lower.Contains("pizza") || lower.Contains("burger")) category = "Alimentação";
            else if (lower.Contains("uber") || lower.Contains("gasolina") || lower.Contains("combustível") || lower.Contains("combustivel") || lower.Contains("transporte") || lower.Contains("estacionamento") || lower.Contains("pedágio") || lower.Contains("pedagio") || lower.Contains("99")) category = "Transporte";
            else if (lower.Contains("farmácia") || lower.Contains("farmacia") || lower.Contains("remédio") || lower.Contains("remedio") || lower.Contains("saúde") || lower.Contains("saude") || lower.Contains("médico") || lower.Contains("medico") || lower.Contains("dentista") || lower.Contains("academia")) category = "Saúde";
            else if (lower.Contains("aluguel") || lower.Contains("condomínio") || lower.Contains("condominio") || lower.Contains("luz") || lower.Contains("energia") || lower.Contains("água") || lower.Contains("agua") || lower.Contains("gás") || lower.Contains("gas") || lower.Contains("moradia")) category = "Moradia";
            else if (lower.Contains("salário") || lower.Contains("salario") || lower.Contains("adiantamento") || lower.Contains("holerite")) category = "Salário";
            else if (lower.Contains("freelance") || lower.Contains("freela") || lower.Contains("consultoria") || lower.Contains("bico") || lower.Contains("projeto")) category = "Freelance";
            else if (lower.Contains("investimento") || lower.Contains("dividendos") || lower.Contains("lucro") || lower.Contains("rendimento")) category = "Investimentos";
            else if (lower.Contains("lazer") || lower.Contains("cinema") || lower.Contains("show") || lower.Contains("viagem") || lower.Contains("hotel")) category = "Lazer";

            // Extração do Nome Real sem "Despesa Outros"
            string name = ExtractCleanTransactionName(message, amount, isIncome, category);

            var formattedVal = amount.ToString("C", new CultureInfo("pt-BR"));
            var formattedDate = txDate.ToString("dd/MM/yyyy");
            var reply = isIncome
                ? $"Excelente! Registrei sua receita de {formattedVal} ({name}) em {category} no dia {formattedDate}."
                : $"Perfeito! Registrei a despesa de {formattedVal} ({name}) em {category} no dia {formattedDate}.";

            return new IntentParseResult {
                Intent = "create_transaction",
                Amount = amount,
                Date = txDate,
                Name = name,
                Type = type,
                Category = category,
                Reply = reply
            };
        }

        private string ExtractCleanTransactionName(string message, decimal amount, bool isIncome, string category) {
            var text = message;

            // 1. Saudação inicial
            text = Regex.Replace(text, @"^(?:por\s+favor\s*,?\s*|pfv\s*,?\s*|opa\s*,?\s*|olá\s*,?\s*|ola\s*,?\s*|ei\s*,?\s*|oi\s*,?\s*)", "", RegexOptions.IgnoreCase);

            // 2. Verbo de ação inicial
            text = Regex.Replace(text, @"^(?:cadastra(?:r)?|cadastre|adiciona(?:r)?|adicione|cria(?:r)?|crie|registra(?:r)?|registre|insere|inseri(?:r)?|lança(?:r)?|lanca(?:r)?|lance|coloca(?:r)?|coloque|bota(?:r)?|bote|anota(?:r)?|anote|paguei|comprei|gastei|recebi|ganhei)\b\s*", "", RegexOptions.IgnoreCase);

            // 3. Remover valores monetários (com preposições antes/depois)
            text = Regex.Replace(text, @"(?:no\s+valor\s+de|no\s+valor|com\s+valor\s+de|com\s+valor|valor\s+de|valor|no\s+total\s+de|no\s+total|total\s+de|total|por|de)?\s*(?:r\$|\$)?\s*\d+(?:[.,]\d{1,2})?\s*(?:reais|real)?", " ", RegexOptions.IgnoreCase);

            // 4. Remover datas
            text = Regex.Replace(text, @"(?:no\s+dia|no\s+dia\s+de|dia|data)\s*\d{1,2}/\d{1,2}(?:/\d{2,4})?", " ", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b\d{1,2}/\d{1,2}(?:/\d{2,4})?\b", " ", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b(?:de\s+hoje|hoje|de\s+ontem|ontem|anteontem|amanhã|amanha)\b", " ", RegexOptions.IgnoreCase);

            // 5. Remover pronomes / preenchimentos ('pra mim', 'para mim', 'aí', etc)
            text = Regex.Replace(text, @"\b(?:pra\s+mim|para\s+mim|aí|ai)\b", " ", RegexOptions.IgnoreCase);

            // 6. Remover termos de transação genéricos ('uma despesa', 'a despesa', 'uma receita', 'conta', etc)
            text = Regex.Replace(text, @"\b(?:uma?\s+nova|um\s+novo|uma?|o|a|os|as)\s+(?:despesa|receita|entrada|gasto|conta|pagamento|transação|transacao)\b", " ", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b(?:despesa|receita|entrada|gasto|conta|pagamento|transação|transacao)\b", " ", RegexOptions.IgnoreCase);

            // 7. Remover conectivos e preposições isoladas no início ou fim
            text = text.Trim();
            text = Regex.Replace(text, @"^(?:de|da|do|em|no|na|com|para|pra|por)\s+", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\s+(?:de|da|do|em|no|na|com|para|pra|por)$", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"^(?:de|da|do|em|no|na|com|para|pra|por)\s+", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\s+(?:de|da|do|em|no|na|com|para|pra|por)$", "", RegexOptions.IgnoreCase);

            // 8. Limpa pontuações nas bordas e espaços múltiplos
            text = Regex.Replace(text, @"^[,\.\-\s]+|[,\.\-\s]+$", "").Trim();
            text = Regex.Replace(text, @"\s+", " ").Trim();

            if (text.Length >= 2 &&
                !text.Equals("despesa", StringComparison.OrdinalIgnoreCase) &&
                !text.Equals("receita", StringComparison.OrdinalIgnoreCase) &&
                !text.Equals("entrada", StringComparison.OrdinalIgnoreCase)) {
                return CultureInfo.GetCultureInfo("pt-BR").TextInfo.ToTitleCase(text.ToLower());
            }

            if (category != "Outros" && category != "Rendimentos") {
                return category;
            }

            return isIncome ? "Receita" : "Despesa";
        }

        private FinanceSimplify.Models.Transaction.TransactionModel? FindMatchingTransaction(
            string message,
            List<FinanceSimplify.Models.Transaction.TransactionModel> recentTransactions) {

            if (!recentTransactions.Any()) return null;

            var lower = message.ToLower();

            // 1. Se pediu "última" ou "ultimo"
            if (lower.Contains("última") || lower.Contains("ultima") || lower.Contains("último") || lower.Contains("ultimo")) {
                return recentTransactions.OrderByDescending(t => t.Date).FirstOrDefault();
            }

            // 2. Buscar por palavra-chave no nome (ex: "claro", "uber", "almoço", "mercado")
            var keywords = new[] { "claro", "uber", "mercado", "almoço", "almoco", "farmácia", "farmacia", "aluguel", "salário", "salario", "jantar", "gasolina" };
            foreach (var kw in keywords) {
                if (lower.Contains(kw)) {
                    var match = recentTransactions.FirstOrDefault(t => t.Name.Contains(kw, StringComparison.OrdinalIgnoreCase));
                    if (match != null) return match;
                }
            }

            // 3. Buscar por valor monetário mencionado
            var amountMatch = Regex.Match(message, @"(?:r\$|\$)?\s*(\d+(?:[.,]\d{1,2})?)", RegexOptions.IgnoreCase);
            if (amountMatch.Success) {
                var raw = amountMatch.Groups[1].Value.Replace(',', '.');
                if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var val)) {
                    var match = recentTransactions.FirstOrDefault(t => Math.Abs(t.Amount - val) < 0.05m);
                    if (match != null) return match;
                }
            }

            // Fallback: primeira transação mais recente
            return recentTransactions.FirstOrDefault();
        }

        private async Task<TransactionActionDetailsDto?> ExecuteDeleteTransaction(
            IntentParseResult intent,
            List<FinanceSimplify.Models.Transaction.TransactionModel> recentTransactions,
            List<FinanceSimplify.Models.Category.CategoryModel> categories) {

            FinanceSimplify.Models.Transaction.TransactionModel? target = null;

            if (intent.TargetTransactionId.HasValue) {
                target = recentTransactions.FirstOrDefault(t => t.Id == intent.TargetTransactionId.Value);
            }

            if (target == null && !string.IsNullOrWhiteSpace(intent.Name)) {
                target = recentTransactions.FirstOrDefault(t => t.Name.Contains(intent.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (target == null && intent.Amount > 0) {
                target = recentTransactions.FirstOrDefault(t => Math.Abs(t.Amount - intent.Amount) < 0.05m);
            }

            if (target == null) return null;

            var deleteResult = await _transactionService.DeleteTransaction(target.Id);
            if (!deleteResult.Status) return null;

            var catName = categories.FirstOrDefault(c => c.Id == target.CategoryId)?.Name ?? "Geral";

            return new TransactionActionDetailsDto {
                TransactionId = target.Id,
                Name = target.Name,
                Amount = target.Amount,
                FormattedAmount = target.Amount.ToString("C", new CultureInfo("pt-BR")),
                CategoryName = catName,
                Type = target.Type == TypeTransactionEnum.Receita ? "Receita" : "Despesa",
                ActionKind = "delete",
                Status = "success",
                Date = target.Date
            };
        }

        private async Task<TransactionActionDetailsDto?> ExecuteEditTransaction(
            IntentParseResult intent,
            List<FinanceSimplify.Models.Transaction.TransactionModel> recentTransactions,
            List<FinanceSimplify.Models.Category.CategoryModel> categories) {

            FinanceSimplify.Models.Transaction.TransactionModel? target = null;

            if (intent.TargetTransactionId.HasValue) {
                target = recentTransactions.FirstOrDefault(t => t.Id == intent.TargetTransactionId.Value);
            }

            if (target == null && !string.IsNullOrWhiteSpace(intent.Name)) {
                target = recentTransactions.FirstOrDefault(t => t.Name.Contains(intent.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (target == null) {
                target = recentTransactions.FirstOrDefault();
            }

            if (target == null) return null;

            var newName = !string.IsNullOrWhiteSpace(intent.Name) && intent.Name != "Transação" ? intent.Name : target.Name;
            var newAmount = intent.Amount > 0 ? intent.Amount : target.Amount;
            var newDate = intent.Date ?? target.Date;

            Guid? targetCategoryId = target.CategoryId;
            if (!string.IsNullOrWhiteSpace(intent.Category) && intent.Category != "Outros") {
                var matched = categories.FirstOrDefault(c => c.Name.Equals(intent.Category, StringComparison.OrdinalIgnoreCase) || c.Name.Contains(intent.Category, StringComparison.OrdinalIgnoreCase));
                if (matched != null) targetCategoryId = matched.Id;
            }

            var editDto = new TransactionEditDto {
                Name = newName,
                Amount = newAmount,
                Date = newDate,
                Type = target.Type,
                PaymentMethod = target.PaymentMethod,
                Installments = target.Installments,
                CardId = target.CardId,
                BankAccountId = target.BankAccountId,
                CategoryId = targetCategoryId
            };

            var editResult = await _transactionService.EditTransaction(target.Id, editDto);
            if (!editResult.Status) return null;

            var catName = categories.FirstOrDefault(c => c.Id == targetCategoryId)?.Name ?? "Geral";

            return new TransactionActionDetailsDto {
                TransactionId = target.Id,
                Name = newName,
                Amount = newAmount,
                FormattedAmount = newAmount.ToString("C", new CultureInfo("pt-BR")),
                CategoryName = catName,
                Type = target.Type == TypeTransactionEnum.Receita ? "Receita" : "Despesa",
                ActionKind = "edit",
                Status = "success",
                Date = newDate
            };
        }

        private async Task<TransactionActionDetailsDto> ExecuteCreateTransaction(
            Guid userId,
            IntentParseResult intent,
            List<FinanceSimplify.Models.Category.CategoryModel> userCategories,
            List<FinanceSimplify.Models.BankAccount.BankAccountModel> userAccounts) {

            // 1. Resolver CategoryId
            Guid categoryId;
            var matchedCategory = userCategories.FirstOrDefault(c =>
                c.Name.Equals(intent.Category, StringComparison.OrdinalIgnoreCase) ||
                c.Name.Contains(intent.Category, StringComparison.OrdinalIgnoreCase));

            if (matchedCategory != null) {
                categoryId = matchedCategory.Id;
            } else if (userCategories.Any()) {
                categoryId = userCategories.First().Id;
            } else {
                // Se usuário não tiver categorias, cria uma
                var newCat = await _categoryService.CreateCategory(userId, new CategoryDto {
                    Name = string.IsNullOrWhiteSpace(intent.Category) ? "Geral" : intent.Category
                });
                categoryId = newCat.CategoryData?.Id ?? Guid.NewGuid();
            }

            // 2. Resolver BankAccountId
            Guid bankAccountId;
            if (userAccounts.Any()) {
                bankAccountId = userAccounts.First().Id;
            } else {
                var newAccount = await _bankAccountService.CreateBankAccount(userId, new BankAccountDto {
                    NameAccount = "Conta Principal"
                });
                bankAccountId = newAccount.BankAccountData?.Id ?? Guid.NewGuid();
            }

            var typeEnum = intent.Type.Equals("Receita", StringComparison.OrdinalIgnoreCase)
                ? TypeTransactionEnum.Receita
                : TypeTransactionEnum.Despesa;

            var transactionDate = intent.Date ?? DateTime.UtcNow;

            var createDto = new TransactionCreateDto {
                Name = intent.Name ?? (typeEnum == TypeTransactionEnum.Receita ? "Receita" : "Despesa"),
                Amount = intent.Amount,
                Date = transactionDate,
                Type = typeEnum,
                CategoryId = categoryId,
                BankAccountId = bankAccountId,
                PaymentMethod = TypePaymentMethodEnum.Pix
            };

            var createResult = await _transactionService.CreateTransaction(userId, createDto);

            return new TransactionActionDetailsDto {
                TransactionId = createResult.TransactionData?.Id,
                Name = createDto.Name,
                Amount = createDto.Amount,
                FormattedAmount = createDto.Amount.ToString("C", new CultureInfo("pt-BR")),
                CategoryName = intent.Category ?? "Geral",
                Type = intent.Type,
                Status = createResult.Status ? "success" : "failed",
                Date = transactionDate
            };
        }

        private string BuildFinancialSummaryPrompt(
            string userMessage,
            decimal totalIncome,
            decimal totalExpense,
            decimal netSavings,
            decimal savingsRate,
            dynamic topCategories,
            string? topCategoryName) {

            var topCatString = string.Join("; ", ((IEnumerable<dynamic>)topCategories).Select(c => $"{c.Category}: R$ {c.Total:N2}"));

            return $"Contexto Financeiro Agregado do Usuário neste mês:\n" +
                $"- Total de Receitas: R$ {totalIncome:N2}\n" +
                $"- Total de Despesas: R$ {totalExpense:N2}\n" +
                $"- Balanço Líquido (Poupança): R$ {netSavings:N2}\n" +
                $"- Taxa de Poupança: {savingsRate}%\n" +
                $"- Maiores Categorias de Despesa: {topCatString}\n\n" +
                $"Pergunta do Usuário:\n\"{userMessage}\"";
        }

        private string GenerateLocalFallbackResponse(
            string message,
            decimal totalIncome,
            decimal totalExpense,
            decimal netSavings,
            decimal savingsRate,
            string? topCategoryName,
            decimal topCategoryTotal) {

            var ptBr = new CultureInfo("pt-BR");
            var lower = message.ToLower();

            if (lower.Contains("onde gastei") || lower.Contains("maior gasto") || lower.Contains("categoria")) {
                if (!string.IsNullOrEmpty(topCategoryName)) {
                    return $"Sua maior concentração de despesas neste mês é na categoria **{topCategoryName}**, com um total de **{topCategoryTotal.ToString("C", ptBr)}**.";
                }
                return "Ainda não identifiquei despesas suficientes neste mês para consolidar as categorias.";
            }

            if (lower.Contains("economizar") || lower.Contains("guardar") || lower.Contains("poupar")) {
                return $"Sua taxa de retenção atual é de **{savingsRate}%**. Para aumentar sua margem, recomendo acompanhar as despesas na categoria **{topCategoryName ?? "Geral"}** e fixar um teto de aportes assim que suas receitas entram.";
            }

            if (lower.Contains("saldo") || lower.Contains("patrimônio") || lower.Contains("balanço") || lower.Contains("resultado")) {
                return $"Seu balanço líquido acumulado no mês é de **{netSavings.ToString("C", ptBr)}** (Receitas: {totalIncome.ToString("C", ptBr)} | Despesas: {totalExpense.ToString("C", ptBr)}).";
            }

            return $"Analisei seu balanço: você acumula **{totalExpense.ToString("C", ptBr)}** em despesas e **{totalIncome.ToString("C", ptBr)}** em receitas, com taxa de poupança em **{savingsRate}%**. Você também pode me pedir para cadastrar transações em linguagem natural, por exemplo: *\"adiciona pra mim a despesa de R$ 60 na padaria\"*!";
        }

        private MonthlyInsightDto GenerateDefaultMonthlyInsight(decimal income, decimal expense, decimal net, decimal rate) {
            var ptBr = new CultureInfo("pt-BR");

            if (income == 0 && expense == 0) {
                return new MonthlyInsightDto {
                    Title = "Bem-vindo ao seu cofre privado",
                    Summary = "Comece adicionando suas primeiras contas e transações para desbloquear diagnósticos da IA.",
                    Tag = "Dica Inicial",
                    Type = "positive",
                    Recommendation = "Cadastre suas contas e cartões para sincronizar seu fluxo patrimonial."
                };
            }

            if (rate >= 30) {
                return new MonthlyInsightDto {
                    Title = "Alta Eficiência de Liquidez",
                    Summary = $"Você está poupando {rate}% das suas receitas este mês. Seu balanço líquido está positivo em {net.ToString("C", ptBr)}.",
                    Tag = "Desempenho VIP",
                    Type = "positive",
                    Recommendation = "Excelente retenção. Considere direcionar o excedente para reservas de liquidez ou aportes."
                };
            }

            if (rate < 10 && expense > 0) {
                return new MonthlyInsightDto {
                    Title = "Margem Mensal Estreita",
                    Summary = $"Suas despesas de {expense.ToString("C", ptBr)} estão consumindo quase a totalidade das suas receitas deste período.",
                    Tag = "Atenção de Fluxo",
                    Type = "warning",
                    Recommendation = "Monitore lançamentos esporádicos para restabelecer uma margem de segurança de ao menos 20%."
                };
            }

            return new MonthlyInsightDto {
                Title = "Fluxo Mensal em Equilíbrio",
                Summary = $"Suas economias somam {net.ToString("C", ptBr)}, representando uma taxa de retenção de {rate}%.",
                Tag = "Equilíbrio",
                Type = "saving",
                Recommendation = "Mantenha a regularidade nos registros para consolidar sua média histórica."
            };
        }

        private MonthlyInsightDto? ParseMonthlyInsightJson(string json) {
            try {
                if (string.IsNullOrWhiteSpace(json)) return null;

                var clean = json.Trim();
                if (clean.StartsWith("```json")) clean = clean[7..];
                if (clean.StartsWith("```")) clean = clean[3..];
                if (clean.EndsWith("```")) clean = clean[..^3];
                clean = clean.Trim();

                return JsonSerializer.Deserialize<MonthlyInsightDto>(clean, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            } catch {
                return null;
            }
        }

        private class IntentParseResult {
            public string Intent { get; set; } = "chat";
            public Guid? TargetTransactionId { get; set; }
            public string? Name { get; set; }
            public decimal Amount { get; set; }
            public DateTime? Date { get; set; }
            public string Type { get; set; } = "Despesa";
            public string Category { get; set; } = "Outros";
            public string? Reply { get; set; }
        }

        #endregion
    }
}
