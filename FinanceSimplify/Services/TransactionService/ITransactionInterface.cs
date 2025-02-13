using FinanceSimplify.Dtos.Transactions;
using FinanceSimplify.Models.Transaction;

namespace FinanceSimplify.Services.TransactionService {
    public interface ITransactionInterface {

        Task<TransactionResponseModel<TransactionResponseDto>> CreateTransaction(TransactionCreateDto transactionDto);

        Task<TransactionResponseModel<List<TransactionResponseDto>>> GetAllTransactions();

        Task<List<TransactionModel>> GetTransactionsByCard(int cardId, int page, int pageSize);

        Task<TransactionResponseModel<TransactionResponseDto>> GetTransactionById(int id);

        Task<TransactionResponseModel<bool>> DeleteTransaction(int id);

        Task<TransactionResponseModel<TransactionResponseDto>> EditTransaction(int transactionId, TransactionEditDto transactionEditDto);
    }
}
