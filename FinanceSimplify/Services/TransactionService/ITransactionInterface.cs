using FinanceSimplify.Dtos.Transactions;
using FinanceSimplify.Models.Transaction;

namespace FinanceSimplify.Services.TransactionService {
    public interface ITransactionInterface {

        Task<TransactionResponseModel<TransactionResponseDto>> CreateTransaction(TransactionCreateDto transactionDto);
    }
}
