using FinanceSimplify.Dtos.Transactions;
using FinanceSimplify.Models.Transaction;

namespace FinanceSimplify.Services.TransactionService {
    public interface ITransactionInterface {

        Task<TransactionResponseModel<TransactionResponseDto>> CreateTransaction(TransactionCreateDto transactionDto);

        Task<TransactionResponseModel<List<TransactionResponseDto>>> GetAllTransactions();

        Task<List<TransactionModel>> GetTransactionsByCard(Guid cardId, int page, int pageSize);

        Task<TransactionResponseModel<TransactionResponseDto>> GetTransactionById(Guid id);

        Task<TransactionResponseModel<bool>> DeleteTransaction(Guid id);

        Task<TransactionResponseModel<TransactionResponseDto>> EditTransaction(Guid transactionId, TransactionEditDto transactionEditDto);

        Task<List<TransactionModel>> GetTransactionsByUserId(Guid userId, int page, int pageSize);
    }
}
