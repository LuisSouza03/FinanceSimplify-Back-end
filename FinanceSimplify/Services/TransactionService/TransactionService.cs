using FinanceSimplify.Data;
using FinanceSimplify.Dtos.Transactions;
using FinanceSimplify.Models.Transaction;
using FinanceSimplify.Models.User;

namespace FinanceSimplify.Services.TransactionService {
    public class TransactionService : ITransactionInterface {
        private readonly AppDbContext _context;

        public TransactionService(AppDbContext context) {
            _context = context;
        }

        public async Task<TransactionResponseModel<TransactionResponseDto>> CreateTransaction(TransactionCreateDto transactionDto) {

            TransactionResponseModel<TransactionResponseDto> response = new();

            try {
                TransactionModel transaction = new() {
                    Name = transactionDto.Name,
                    Amount = transactionDto.Amount,
                    Date = transactionDto.Date,
                    Type = transactionDto.Type,
                    PaymentMethod = transactionDto.PaymentMethod,
                    Installments = transactionDto.Installments,
                    CardId = transactionDto.CardId,
                    CategoryId = transactionDto.CategoryId
                };

                _context.Transaction.Add(transaction);
                await _context.SaveChangesAsync();

                response.TransactionData = new TransactionResponseDto {
                    Id = transaction.Id,
                    Name = transactionDto.Name,
                    Amount = transactionDto.Amount,
                    Date = transactionDto.Date,
                    Type = transactionDto.Type,
                    PaymentMethod = transactionDto.PaymentMethod,
                    Installments = transactionDto.Installments,
                    CardId = transactionDto.CardId,
                    CategoryId = transactionDto.CategoryId
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
    }
}
