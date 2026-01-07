using FinanceSimplify.Dtos.Transactions;
using FinanceSimplify.Enum;
using FinanceSimplify.Models.Transaction;

namespace FinanceSimplify.Services.TransactionService {
    public interface ITransactionInterface {

        Task<TransactionResponseModel<TransactionResponseDto>> CreateTransaction(Guid userId, TransactionCreateDto transactionDto);

        Task<TransactionResponseModel<List<TransactionResponseDto>>> GetAllTransactions();

        Task<List<TransactionModel>> GetTransactionsByCard(Guid cardId, int page, int pageSize);

        Task<TransactionResponseModel<TransactionResponseDto>> GetTransactionById(Guid id);

        Task<List<TransactionModel>> GetTransactionsByUserId(Guid userId, int page, int pageSize, TransactionFilterDto? filter = null);

        Task<List<TransactionModel>> GetTransactionByDateRange(Guid userId, DateTime startDate, DateTime endDate);

        Task<List<TransactionModel>> GetTransactionByType(Guid userId, TypeTransactionEnum type);

        Task<TransactionResponseModel<bool>> DeleteTransaction(Guid id);

        Task<TransactionResponseModel<TransactionResponseDto>> EditTransaction(Guid transactionId, TransactionEditDto transactionEditDto);
    }
}
