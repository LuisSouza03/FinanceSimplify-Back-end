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
                var categories = await _categoryService.GetCategoriesByUserId(userId, 1, 100);
                var bankAccounts = await _bankAccountService.GetBankAccountsByUserId(userId, 1, 10);

                var totalIncome = transactions.Where(t => t.Type == TypeTransactionEnum.Receita).Sum(t => t.Amount);
                var totalExpense = transactions.Where(t => t.Type == TypeTransactionEnum.Despesa).Sum(t => t.Amount);
                var netSavings = totalIncome - totalExpense;
                var savingsRate = totalIncome > 0 ? Math.Round((netSavings / totalIncome) * 100, 1) : 0;

                // Agrupamento por categoria
                var categoryExpenses = transactions
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
                var intentResult = await ParseIntentWithAiOrFallback(request.Message, categories.Select(c => c.Name).ToList());

                // 4. Se a intenção for registrar transação
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

        private async Task<IntentParseResult> ParseIntentWithAiOrFallback(string message, List<string> knownCategories) {
            var lower = message.ToLower();
            var actionKeywords = new[] { "adiciona", "cria", "registra", "inserir", "gastei", "despesa", "receita", "paguei", "comprei" };
            bool hasActionKeyword = actionKeywords.Any(k => lower.Contains(k));

            if (!hasActionKeyword) {
                return new IntentParseResult { Intent = "chat" };
            }

            // Se o Gemini estiver configurado, podemos pedir para extrair a intenção com rigor
            if (_aiProvider.IsConfigured) {
                try {
                    var categoriesList = string.Join(", ", knownCategories);
                    var prompt = $"Analise a seguinte mensagem do usuário e determine se ele quer registrar uma transação financeira:\n\"{message}\"\n\n" +
                        $"Categorias existentes do usuário: [{categoriesList}].\n" +
                        "Responda SOMENTE em formato JSON com o seguinte esquema:\n" +
                        "{\n" +
                        "  \"intent\": \"create_transaction\" ou \"chat\",\n" +
                        "  \"name\": \"nome descritivo curto\",\n" +
                        "  \"amount\": número decimal do valor,\n" +
                        "  \"type\": \"Despesa\" ou \"Receita\",\n" +
                        "  \"category\": \"categoria que melhor se adequa\",\n" +
                        "  \"reply\": \"resposta amigável confirmando o registro\"\n" +
                        "}";

                    var json = await _aiProvider.GenerateContentAsync(prompt, "Você é um interpretador de intenções financeiras. Retorne SOMENTE JSON válido.", jsonOutput: true);
                    if (!string.IsNullOrWhiteSpace(json)) {
                        // Limpa possíveis marcações de markdown ```json
                        var cleanJson = json.Trim();
                        if (cleanJson.StartsWith("```json")) cleanJson = cleanJson[7..];
                        if (cleanJson.StartsWith("```")) cleanJson = cleanJson[3..];
                        if (cleanJson.EndsWith("```")) cleanJson = cleanJson[..^3];
                        cleanJson = cleanJson.Trim();

                        var parsed = JsonSerializer.Deserialize<IntentParseResult>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (parsed != null && parsed.Amount > 0) {
                            return parsed;
                        }
                    }
                } catch (Exception ex) {
                    _logger.LogWarning(ex, "Erro ao interpretar intenção via Gemini. Usando parser local.");
                }
            }

            // Fallback via regex local
            return ParseIntentLocally(message);
        }

        private IntentParseResult ParseIntentLocally(string message) {
            var lower = message.ToLower();

            // Expressão regular para extrair valores monetários: "85", "85,50", "R$ 150.00", etc.
            var match = Regex.Match(message, @"(?:r\$|\$)?\s*(\d+(?:[.,]\d{1,2})?)", RegexOptions.IgnoreCase);
            if (!match.Success) {
                return new IntentParseResult { Intent = "chat" };
            }

            var rawAmount = match.Groups[1].Value.Replace(',', '.');
            if (!decimal.TryParse(rawAmount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) || amount <= 0) {
                return new IntentParseResult { Intent = "chat" };
            }

            var isIncome = lower.Contains("receita") || lower.Contains("salário") || lower.Contains("salario") || lower.Contains("recebi");
            var type = isIncome ? "Receita" : "Despesa";

            // Inferir categoria
            string category = "Outros";
            if (lower.Contains("mercado") || lower.Contains("compras")) category = "Mercado";
            else if (lower.Contains("almoço") || lower.Contains("almoco") || lower.Contains("jantar") || lower.Contains("restaurante") || lower.Contains("comida") || lower.Contains("lanche")) category = "Alimentação";
            else if (lower.Contains("uber") || lower.Contains("gasolina") || lower.Contains("combustível") || lower.Contains("transporte")) category = "Transporte";
            else if (lower.Contains("farmácia") || lower.Contains("farmacia") || lower.Contains("remédio") || lower.Contains("saúde") || lower.Contains("saude")) category = "Saúde";
            else if (lower.Contains("aluguel") || lower.Contains("condomínio") || lower.Contains("luz") || lower.Contains("energia") || lower.Contains("água")) category = "Moradia";
            else if (lower.Contains("salário") || lower.Contains("salario") || lower.Contains("freelance")) category = "Salário";

            // Inferir nome da transação
            string name = isIncome ? $"Receita {category}" : $"Despesa {category}";
            if (lower.Contains("uber")) name = "Uber / Transporte";
            else if (lower.Contains("mercado")) name = "Compras de Mercado";
            else if (lower.Contains("almoço") || lower.Contains("almoco")) name = "Almoço";
            else if (lower.Contains("farmácia") || lower.Contains("farmacia")) name = "Farmácia";

            var formatted = amount.ToString("C", new CultureInfo("pt-BR"));
            var reply = isIncome
                ? $"Excelente! Registrei sua receita de {formatted} em {category}."
                : $"Perfeito! Registrei a despesa de {formatted} em {category}.";

            return new IntentParseResult {
                Intent = "create_transaction",
                Amount = amount,
                Name = name,
                Type = type,
                Category = category,
                Reply = reply
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

            var createDto = new TransactionCreateDto {
                Name = intent.Name ?? (typeEnum == TypeTransactionEnum.Receita ? "Receita" : "Despesa"),
                Amount = intent.Amount,
                Date = DateTime.UtcNow,
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
                Date = DateTime.UtcNow
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
            public string? Name { get; set; }
            public decimal Amount { get; set; }
            public string Type { get; set; } = "Despesa";
            public string Category { get; set; } = "Outros";
            public string? Reply { get; set; }
        }

        #endregion
    }
}
