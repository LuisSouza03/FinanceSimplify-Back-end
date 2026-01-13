using System.Collections.Generic;
using FinanceSimplify.Data;
using FinanceSimplify.Dtos.Transactions;
using FinanceSimplify.Enum;
using FinanceSimplify.Models.Transaction;
using FinanceSimplify.Models.User;
using FinanceSimplify.Services.InstallmentService;
using FinanceSimplify.Services.CardService;
using FinanceSimplify.Services.InvoiceService;
using FinanceSimplify.Services.Category;
using FinanceSimplify.Models.Category;
using FinanceSimplify.Dtos.Category;
using FinanceSimplify.Models.Installment;
using MongoDB.Driver;

namespace FinanceSimplify.Services.TransactionService {
    public class TransactionService : ITransactionInterface {
        private readonly MongoDbContext _context;
        private readonly IInstallmentInterface _installmentService;
        private readonly ICardInterface _cardService;
        private readonly IInvoiceInterface _invoiceService;
        private readonly ICategoryInterface _categoryService;

        public TransactionService(MongoDbContext context, IInstallmentInterface installmentService, ICardInterface cardService, IInvoiceInterface invoiceService, ICategoryInterface categoryService) {
            _context = context;
            _installmentService = installmentService;
            _cardService = cardService;
            _invoiceService = invoiceService;
            _categoryService = categoryService;
        }

        public async Task<TransactionResponseModel<TransactionResponseDto>> CreateTransaction(Guid userId, TransactionCreateDto transactionDto) {

            TransactionResponseModel<TransactionResponseDto> response = new();

            if (transactionDto.PaymentMethod == Enum.TypePaymentMethodEnum.Credito) {
                if ((transactionDto.Installments is null || transactionDto.Installments == 0) || transactionDto.CardId is null) {

                    response.Status = false;
                    response.Message = "Para pagamentos com crédito, é necessário informar o número de parcelas e um cartão.";
                    return response;
                }

                // Verificar limite disponível
                var card = await _context.Cards.Find(c => c.Id == transactionDto.CardId).FirstOrDefaultAsync();
                if (card == null) {
                    response.Status = false;
                    response.Message = "Cartão não encontrado!";
                    return response;
                }

                if (card.Type != TypeCardTransactionEnum.Credito) {
                    response.Status = false;
                    response.Message = "Este cartão não é de crédito!";
                    return response;
                }

                if (card.CreditLimit.HasValue) {
                    var availableLimit = await _cardService.GetAvailableLimit(card.Id);
                    if (transactionDto.Amount > availableLimit) {
                        response.Status = false;
                        response.Message = $"Limite insuficiente! Limite disponível: R$ {availableLimit:F2}";
                        return response;
                    }
                }
            }
            else if (transactionDto.PaymentMethod == Enum.TypePaymentMethodEnum.Debito) { 

                if(transactionDto.CardId is null) {
                    response.Status = false;
                    response.Message = "Para pagamentos com débito, é necessário informar um cartão";
                    return response;
                }
            }

            if (transactionDto.Amount <= 0) {
                response.Status = false;
                response.Message = "O valor deve ser maior que zero.";
                return response;
            }

            if(transactionDto.CategoryId is null) {
                response.Status = false;
                response.Message = "Deve inserir uma categoria";
                return response;
            }

            if (transactionDto.BankAccountId is null) {
                response.Status = false;
                response.Message = "Deve inserir uma Conta Bancaria";
                return response;
            }


            try {
                TransactionModel transaction = new() {
                    Id = Guid.NewGuid(),
                    Name = transactionDto.Name,
                    Amount = transactionDto.Amount,
                    Date = transactionDto.Date,
                    Type = transactionDto.Type,
                    PaymentMethod = transactionDto.PaymentMethod,
                    Installments = transactionDto.Installments,
                    CardId = transactionDto.CardId,
                    CategoryId = transactionDto.CategoryId,
                    UserId = userId,
                    BankAccountId = transactionDto.BankAccountId
                };

                await _context.Transactions.InsertOneAsync(transaction);

                // Se for crédito parcelado, criar as parcelas
                if (transactionDto.PaymentMethod == TypePaymentMethodEnum.Credito && 
                    transactionDto.Installments.HasValue && 
                    transactionDto.Installments.Value > 0 &&
                    transactionDto.CardId.HasValue) {
                    
                    var card = await _context.Cards.Find(c => c.Id == transactionDto.CardId).FirstOrDefaultAsync();
                    if (card != null && card.DueDay.HasValue && card.ClosingDay.HasValue) {
                        var firstDueDate = new DateTime(transactionDto.Date.Year, transactionDto.Date.Month, card.DueDay.Value);
                        
                        // Se já passou do vencimento deste mês, primeira parcela vence no próximo mês
                        if (transactionDto.Date.Day > card.ClosingDay.Value) {
                            firstDueDate = firstDueDate.AddMonths(1);
                        }

                        await _installmentService.CreateInstallmentsForTransaction(
                            transaction.Id,
                            transactionDto.CardId.Value,
                            userId,
                            transactionDto.Amount,
                            transactionDto.Installments.Value,
                            firstDueDate,
                            transactionDto.Name,
                            card.ClosingDay.Value
                        );

                        // Gerar faturas automaticamente para os meses necessários
                        var monthsToGenerate = new HashSet<(int month, int year)>();
                        var currentDate = firstDueDate;
                        
                        for (int i = 0; i < transactionDto.Installments.Value; i++) {
                            monthsToGenerate.Add((currentDate.Month, currentDate.Year));
                            currentDate = currentDate.AddMonths(1);
                        }

                        foreach (var (month, year) in monthsToGenerate) {
                            // Tentar gerar fatura (ignora se já existe)
                            try {
                                await _invoiceService.GenerateInvoiceForCard(card.Id, userId, month, year);
                            }
                            catch {
                                // Fatura já existe, continuar
                            }
                        }

                        // Atualizar limite disponível
                        var newAvailableLimit = await _cardService.GetAvailableLimit(card.Id);
                        await _cardService.UpdateAvailableLimit(card.Id, newAvailableLimit);
                    }
                }

                response.TransactionData = new TransactionResponseDto {
                    Id = transaction.Id,
                    Name = transactionDto.Name,
                    Amount = transactionDto.Amount,
                    Date = transactionDto.Date,
                    Type = transactionDto.Type,
                    PaymentMethod = transactionDto.PaymentMethod,
                    Installments = transactionDto.Installments,
                    CardId = transactionDto.CardId,
                    CategoryId = transactionDto.CategoryId,
                    BankAccountId = transactionDto.BankAccountId
                };

                response.Message = "Transação criada com sucesso!";
                return response;
            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }

        }

        public async Task<TransactionResponseModel<List<TransactionResponseDto>>> GetAllTransactions() {
            TransactionResponseModel<List<TransactionResponseDto>> response = new();

            try {
                var transactions = await _context.Transactions.Find(_ => true).ToListAsync();

                var data = transactions.Select(transactionDB => new TransactionResponseDto {
                    Name = transactionDB.Name,
                    Amount = transactionDB.Amount,
                    Date = transactionDB.Date,
                    Type = transactionDB.Type,
                    PaymentMethod = transactionDB.PaymentMethod,
                    Installments = transactionDB.Installments,
                    CardId = transactionDB.CardId,
                    CategoryId = transactionDB.CategoryId
                }).ToList();

                response.TransactionData = data;
                response.Message = "Todas as transações foram coletadas!";
                return response;


            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }

        public async Task<List<TransactionModel>> GetTransactionsByCard(Guid cardId, int page, int pageSize) {

            return await _context.Transactions
                .Find(t => t.CardId == cardId)
                .SortByDescending(t => t.Date)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<TransactionResponseModel<TransactionResponseDto>> GetTransactionById(Guid transactionId) {
            TransactionResponseModel<TransactionResponseDto> response = new();

            try {

                var transaction = await _context.Transactions.Find(t => t.Id == transactionId).FirstOrDefaultAsync();

                if (transaction == null) {
                    response.Message = "Transação não encontrada!";
                    response.Status = false;
                    return response;
                }

                response.TransactionData = new TransactionResponseDto {
                    Id = transaction.Id,
                    Name = transaction.Name,
                    Amount = transaction.Amount,
                    Date = transaction.Date,
                    Type = transaction.Type,
                    PaymentMethod = transaction.PaymentMethod,
                    Installments = transaction.Installments,
                    CardId = transaction.CardId,
                    CategoryId = transaction.CategoryId
                };

                return response;

            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }

        public async Task<List<TransactionModel>> GetTransactionsByUserId(Guid userId, int page, int pageSize, TransactionFilterDto? filter = null) {

            var filterBuilder = Builders<TransactionModel>.Filter;
            var mongoFilter = filterBuilder.Eq(t => t.UserId, userId);

            if(filter?.StartDate.HasValue == true) {
                mongoFilter = mongoFilter & filterBuilder.Gte(t => t.Date, filter.StartDate.Value);
            }

            if(filter?.EndDate.HasValue == true) {
                mongoFilter = mongoFilter & filterBuilder.Lte(t => t.Date, filter.EndDate.Value);
            }

            return await _context.Transactions
                .Find(mongoFilter)
                .SortByDescending(t => t.Date)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<List<TransactionModel>> GetTransactionByDateRange(Guid userId, DateTime startDate, DateTime endDate) {
            
            var filterBuilder = Builders<TransactionModel>.Filter;
            var filter = filterBuilder.Eq(t => t.UserId, userId) &
                        filterBuilder.Gte(t => t.Date, startDate) &
                        filterBuilder.Lte(t => t.Date, endDate);
            
            return await _context.Transactions.Find(filter).ToListAsync();
        }

        public async Task<List<TransactionModel>> GetTransactionByType(Guid userId, TypeTransactionEnum type) {
            var filterBuilder = Builders<TransactionModel>.Filter;
            var filter = filterBuilder.Eq(t => t.UserId, userId) & filterBuilder.Eq(t => t.Type, type);
            return await _context.Transactions.Find(filter).ToListAsync();
        }

        public async Task<TransactionResponseModel<bool>> DeleteTransaction(Guid transactionId) {
            TransactionResponseModel<bool> response = new();

            try {
                // Buscar a transação antes de deletar para pegar informações
                var transaction = await _context.Transactions.Find(t => t.Id == transactionId).FirstOrDefaultAsync();

                if (transaction == null) {
                    response.Message = "Transação não encontrada!";
                    response.Status = false;
                    return response;
                }

                // Buscar parcelas associadas a esta transação
                var installments = await _installmentService.GetInstallmentsByTransaction(transactionId);

                // Guardar IDs das faturas afetadas para recalcular depois
                var affectedInvoiceIds = installments
                    .Where(i => i.InvoiceId.HasValue)
                    .Select(i => i.InvoiceId.Value)
                    .Distinct()
                    .ToList();

                // Deletar todas as parcelas desta transação
                await _context.Installments.DeleteManyAsync(i => i.TransactionId == transactionId);

                // Deletar a transação
                await _context.Transactions.DeleteOneAsync(t => t.Id == transactionId);

                // Recalcular total das faturas afetadas
                foreach (var invoiceId in affectedInvoiceIds) {
                    var total = await _invoiceService.CalculateInvoiceTotal(invoiceId);
                    var update = Builders<Models.Invoice.InvoiceModel>.Update.Set(i => i.TotalAmount, total);
                    await _context.Invoices.UpdateOneAsync(i => i.Id == invoiceId, update);
                }

                // Se a transação tinha cartão, recalcular o limite disponível
                if (transaction.CardId.HasValue) {
                    var newAvailableLimit = await _cardService.GetAvailableLimit(transaction.CardId.Value);
                    await _cardService.UpdateAvailableLimit(transaction.CardId.Value, newAvailableLimit);
                }

                response.TransactionData = true;
                response.Message = "Transação e parcelas removidas com sucesso! Limite do cartão atualizado.";
                return response;

            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }

        public async Task<TransactionResponseModel<TransactionResponseDto>> EditTransaction(Guid transactionId, TransactionEditDto transactionEditDto) {
            TransactionResponseModel<TransactionResponseDto> response = new();

            try {

                var transaction = await _context.Transactions.Find(t => t.Id == transactionId).FirstOrDefaultAsync();

                if (transaction == null) {
                    response.Message = "Nenhuma transaction localizada!";
                    response.Status = false;
                    return response;
                }

                transaction.Amount = transactionEditDto.Amount;
                transaction.PaymentMethod = transactionEditDto.PaymentMethod;
                transaction.Name = transactionEditDto.Name;
                transaction.Installments = transactionEditDto.Installments;
                transaction.CardId = transactionEditDto.CardId;
                transaction.CategoryId = transactionEditDto.CategoryId;
                transaction.Date = transactionEditDto.Date;

                await _context.Transactions.ReplaceOneAsync(t => t.Id == transactionId, transaction);

                response.TransactionData = new TransactionResponseDto {
                    Id = transaction.Id,
                    Name = transaction.Name,
                    Amount = transaction.Amount,
                    Date = transaction.Date,
                    Type = transaction.Type,
                    PaymentMethod = transaction.PaymentMethod,
                    Installments = transaction.Installments,
                    CardId = transaction.CardId,
                    CategoryId = transaction.CategoryId,
                };
                response.Message = "Transaction atualizada com sucesso!";
                return response;

            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
             
        }

        public async Task<TransactionResponseModel<CsvTransactionImportResultDto>> ImportTransactionsFromCsv(Guid userId, Guid cardId, IFormFile csvFile) {
            TransactionResponseModel<CsvTransactionImportResultDto> response = new();
            CsvTransactionImportResultDto result = new();

            try {
                // Validar arquivo
                if (csvFile == null || csvFile.Length == 0) {
                    response.Status = false;
                    response.Message = "Arquivo CSV está vazio ou não foi fornecido";
                    return response;
                }

                if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) {
                    response.Status = false;
                    response.Message = "O arquivo deve ter extensão .csv";
                    return response;
                }

                // Verificar se o cartão existe e pertence ao usuário
                var card = await _context.Cards.Find(c => c.Id == cardId && c.UserId == userId).FirstOrDefaultAsync();
                if (card == null) {
                    response.Status = false;
                    response.Message = "Cartão não encontrado ou não pertence ao usuário";
                    return response;
                }

                // Verificar se o cartão tem conta bancária
                if (card.BankAccountId == Guid.Empty) {
                    response.Status = false;
                    response.Message = "O cartão não possui uma conta bancária associada";
                    return response;
                }

                // Ler o arquivo CSV
                using var reader = new StreamReader(csvFile.OpenReadStream());
                string? headerLine = await reader.ReadLineAsync();
                
                if (string.IsNullOrWhiteSpace(headerLine)) {
                    response.Status = false;
                    response.Message = "Arquivo CSV está vazio";
                    return response;
                }

                int lineNumber = 1;
                string? line;

                while ((line = await reader.ReadLineAsync()) != null) {
                    lineNumber++;
                    
                    if (string.IsNullOrWhiteSpace(line)) {
                        continue;
                    }

                    try {
                        // Parse da linha CSV
                        var fields = CsvParser.ParseCsvLine(line);
                        
                        if (!CsvParser.IsValidCsvLine(fields, 5)) {
                            result.Errors.Add($"Linha {lineNumber}: Formato inválido - esperado 5 campos, encontrado {fields.Count}");
                            continue;
                        }

                        // Extrair campos
                        string dateStr = fields[0];
                        string lancamento = fields[1];
                        string categoriaName = fields[2];
                        string tipo = fields[3];
                        string valorStr = fields[4];

                        // Parse dos dados
                        DateTime purchaseDate = CsvParser.ParseBrazilianDate(dateStr);
                        decimal amount = CsvParser.ParseBrazilianCurrency(valorStr);
                        var (currentInstallment, totalInstallments) = CsvParser.ExtractInstallmentInfo(tipo);

                        // Buscar ou criar categoria
                        var category = await _categoryService.GetCategoryByName(userId, categoriaName);
                        
                        if (category == null) {
                            // Criar categoria automaticamente
                            var categoryDto = new CategoryDto { Name = categoriaName };
                            var categoryResponse = await _categoryService.CreateCategory(userId, categoryDto);
                            
                            if (categoryResponse.Status && categoryResponse.CategoryData != null) {
                                category = new CategoryModel {
                                    Id = categoryResponse.CategoryData.Id,
                                    Name = categoryResponse.CategoryData.Name,
                                    UserId = userId
                                };
                                result.CategoriesCreated++;
                                result.NewCategories.Add(categoriaName);
                            } else {
                                result.Errors.Add($"Linha {lineNumber}: Erro ao criar categoria '{categoriaName}'");
                                continue;
                            }
                        }

                        // Criar transação
                        TransactionModel transaction = new() {
                            Id = Guid.NewGuid(),
                            Name = lancamento,
                            Amount = amount,
                            Date = purchaseDate,
                            Type = TypeTransactionEnum.Despesa,
                            PaymentMethod = TypePaymentMethodEnum.Credito,
                            Installments = totalInstallments > 0 ? totalInstallments : 1,
                            CardId = cardId,
                            CategoryId = category.Id,
                            UserId = userId,
                            BankAccountId = card.BankAccountId
                        };

                        await _context.Transactions.InsertOneAsync(transaction);
                        result.TransactionsCreated++;

                        // Criar parcelas
                        if (totalInstallments > 0 && currentInstallment > 0) {
                            // Tem parcelas - criar todas até a parcela atual
                            if (card.DueDay.HasValue) {
                                // Calcular data de vencimento da parcela atual
                                var currentDueDate = new DateTime(purchaseDate.Year, purchaseDate.Month, card.DueDay.Value);
                                
                                // Se a compra foi depois do dia de fechamento, vence no próximo mês
                                if (card.ClosingDay.HasValue && purchaseDate.Day > card.ClosingDay.Value) {
                                    currentDueDate = currentDueDate.AddMonths(1);
                                }

                                // Criar parcelas de 1 até currentInstallment
                                for (int i = 1; i <= currentInstallment; i++) {
                                    // Calcular data de vencimento desta parcela
                                    // Parcela 1 vence em currentDueDate - (currentInstallment - 1) meses
                                    var installmentDueDate = currentDueDate.AddMonths(-(currentInstallment - i));

                                    InstallmentModel installment = new() {
                                        Id = Guid.NewGuid(),
                                        TransactionId = transaction.Id,
                                        CardId = cardId,
                                        UserId = userId,
                                        Amount = amount,
                                        InstallmentNumber = i,
                                        TotalInstallments = totalInstallments,
                                        DueDate = installmentDueDate,
                                        IsPaid = i < currentInstallment, // Parcelas anteriores já foram pagas
                                        Description = $"{lancamento} - Parcela {i}/{totalInstallments}",
                                        CreatedAt = DateTime.UtcNow
                                    };

                                    await _context.Installments.InsertOneAsync(installment);
                                    result.InstallmentsCreated++;

                                    // Associar à fatura do mês correspondente
                                    try {
                                        var invoice = await _invoiceService.GenerateInvoiceForCard(
                                            cardId, 
                                            userId, 
                                            installmentDueDate.Month, 
                                            installmentDueDate.Year
                                        );
                                        
                                        if (invoice != null && invoice.InvoiceData != null) {
                                            // Atualizar parcela com InvoiceId
                                            var update = Builders<InstallmentModel>.Update.Set(inst => inst.InvoiceId, invoice.InvoiceData.Id);
                                            await _context.Installments.UpdateOneAsync(inst => inst.Id == installment.Id, update);
                                        }
                                    } catch {
                                        // Fatura pode já existir, continuar
                                    }
                                }
                            }
                        } else {
                            // Compra à vista - criar apenas uma parcela
                            var dueDate = purchaseDate;
                            if (card.DueDay.HasValue) {
                                dueDate = new DateTime(purchaseDate.Year, purchaseDate.Month, card.DueDay.Value);
                                if (card.ClosingDay.HasValue && purchaseDate.Day > card.ClosingDay.Value) {
                                    dueDate = dueDate.AddMonths(1);
                                }
                            }

                            InstallmentModel installment = new() {
                                Id = Guid.NewGuid(),
                                TransactionId = transaction.Id,
                                CardId = cardId,
                                UserId = userId,
                                Amount = amount,
                                InstallmentNumber = 1,
                                TotalInstallments = 1,
                                DueDate = dueDate,
                                IsPaid = false,
                                Description = $"{lancamento} - À vista",
                                CreatedAt = DateTime.UtcNow
                            };

                            await _context.Installments.InsertOneAsync(installment);
                            result.InstallmentsCreated++;

                            // Associar à fatura
                            try {
                                var invoice = await _invoiceService.GenerateInvoiceForCard(
                                    cardId, 
                                    userId, 
                                    dueDate.Month, 
                                    dueDate.Year
                                );
                                
                                if (invoice != null && invoice.InvoiceData != null) {
                                    var update = Builders<InstallmentModel>.Update.Set(inst => inst.InvoiceId, invoice.InvoiceData.Id);
                                    await _context.Installments.UpdateOneAsync(inst => inst.Id == installment.Id, update);
                                }
                            } catch {
                                // Fatura pode já existir, continuar
                            }
                        }

                        result.TotalProcessed++;

                    } catch (Exception ex) {
                        result.Errors.Add($"Linha {lineNumber}: {ex.Message}");
                    }
                }

                // Atualizar limite disponível do cartão
                if (card.CreditLimit.HasValue) {
                    var newAvailableLimit = await _cardService.GetAvailableLimit(cardId);
                    await _cardService.UpdateAvailableLimit(cardId, newAvailableLimit);
                }

                response.TransactionData = result;
                response.Message = $"Importação concluída! {result.TransactionsCreated} transações criadas, {result.InstallmentsCreated} parcelas criadas, {result.CategoriesCreated} categorias criadas.";
                response.Status = true;
                return response;

            } catch (Exception ex) {
                response.Message = $"Erro ao processar arquivo CSV: {ex.Message}";
                response.Status = false;
                response.TransactionData = result;
                return response;
            }
        }

        
    }
}
