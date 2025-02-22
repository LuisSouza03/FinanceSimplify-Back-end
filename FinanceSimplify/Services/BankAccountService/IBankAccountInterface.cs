using FinanceSimplify.Dtos.BankAccount;
using FinanceSimplify.Models.BankAccount;

namespace FinanceSimplify.Services.BankAccountService {
    public interface IBankAccountInterface {

        Task<BankAccountResponseModel<BankAccountResponseDto>> CreateBankAccount(Guid userId, BankAccountDto bankAccountDto);
        Task<List<BankAccountModel>> GetBankAccountsByUserId(Guid userId, int page = 1, int pageSize = 10);

        Task<BankAccountModel?> GetBankAccountById(Guid id);

    }
}
