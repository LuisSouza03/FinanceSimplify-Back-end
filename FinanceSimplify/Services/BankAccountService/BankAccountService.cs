using FinanceSimplify.Data;
using FinanceSimplify.Dtos.BankAccount;
using FinanceSimplify.Models.BankAccount;
using Microsoft.EntityFrameworkCore;

namespace FinanceSimplify.Services.BankAccountService {
    public class BankAccountService: IBankAccountInterface {

        private readonly AppDbContext _context;

        public BankAccountService(AppDbContext context) {
            _context = context;
        }

        public async Task<BankAccountResponseModel<BankAccountResponseDto>> CreateBankAccount(Guid userId, BankAccountDto bankAccountDto) {

            BankAccountResponseModel<BankAccountResponseDto> response = new();

            try {
                BankAccountModel bankAccount = new() {
                    AccountName = bankAccountDto.NameAccount,
                    UserId = userId,
                };

                _context.BankAccount.Add(bankAccount);
                await _context.SaveChangesAsync();

                response.BankAccountData = new BankAccountResponseDto {
                    Id = bankAccount.Id,
                    NameAccount = bankAccount.AccountName
                };
                response.Message = "Conta criada com sucesso!";
                return response;
            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }


        public async Task<List<BankAccountModel>> GetBankAccountsByUserId(Guid userId, int page, int pageSize) {
            return await _context.BankAccount
                .Where(b => b.UserId == userId)
                .Include(b => b.Cards)
                .Include(b => b.Transactions)
                .ToListAsync();
        }

        public async Task<BankAccountModel?> GetBankAccountById(Guid id) {
            return await _context.BankAccount
                .Include(b => b.Cards)
                .Include(b => b.Transactions)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

       
    }
}
