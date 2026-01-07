using System.Collections.Generic;
using FinanceSimplify.Data;
using FinanceSimplify.Dtos.Transactions;
using FinanceSimplify.Enum;
using FinanceSimplify.Models.Transaction;
using FinanceSimplify.Models.User;
using MongoDB.Driver;

namespace FinanceSimplify.Services.TransactionService {
    public class TransactionService : ITransactionInterface {
        private readonly MongoDbContext _context;

        public TransactionService(MongoDbContext context) {
            _context = context;
        }

        public async Task<TransactionResponseModel<TransactionResponseDto>> CreateTransaction(Guid userId, TransactionCreateDto transactionDto) {

            TransactionResponseModel<TransactionResponseDto> response = new();

            if (transactionDto.PaymentMethod == Enum.TypePaymentMethodEnum.Credito) {
                if ((transactionDto.Installments is null || transactionDto.Installments == 0) || transactionDto.CardId is null) {

                    response.Status = false;
                    response.Message = "Para pagamentos com crédito, é necessário informar o número de parcelas e um cartão.";
                    return response;
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
                var result = await _context.Transactions.DeleteOneAsync(t => t.Id == transactionId);

                if (result.DeletedCount == 0) {
                    response.Message = "Transação não encontrada!";
                    response.Status = false;
                    return response;
                }

                response.TransactionData = true;
                response.Message = "Transação removida com sucesso!";
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

        
    }
}
