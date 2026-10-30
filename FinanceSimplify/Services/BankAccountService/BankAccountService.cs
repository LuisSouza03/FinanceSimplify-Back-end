using FinanceSimplify.Data;
using FinanceSimplify.Dtos.BankAccount;
using FinanceSimplify.Models.BankAccount;
using MongoDB.Driver;

namespace FinanceSimplify.Services.BankAccountService {
    public class BankAccountService: IBankAccountInterface {

        private readonly MongoDbContext _context;

        public BankAccountService(MongoDbContext context) {
            _context = context;
        }

        public async Task<BankAccountResponseModel<BankAccountResponseDto>> CreateBankAccount(Guid userId, BankAccountDto bankAccountDto) {

            BankAccountResponseModel<BankAccountResponseDto> response = new();

            try {
                var accountName = bankAccountDto.ResolvedName;
                if (string.IsNullOrWhiteSpace(accountName)) {
                    response.Message = "O nome da conta bancária é obrigatório.";
                    response.Status = false;
                    return response;
                }

                BankAccountModel bankAccount = new() {
                    AccountName = accountName,
                    UserId = userId,
                };

                await _context.BankAccounts.InsertOneAsync(bankAccount);

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
            return await _context.BankAccounts
                .Find(b => b.UserId == userId)
                .ToListAsync();
        }

        public async Task<BankAccountModel?> GetBankAccountById(Guid id) {
            return await _context.BankAccounts
                .Find(b => b.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<BankAccountResponseModel<bool>> DeleteBankAccount(Guid id) {
            BankAccountResponseModel<bool> response = new();

            try {
                var result = await _context.BankAccounts.DeleteOneAsync(b => b.Id == id);
                if (result.DeletedCount > 0) {
                    response.BankAccountData = true;
                    response.Message = "Conta bancária excluída com sucesso!";
                    response.Status = true;
                } else {
                    response.BankAccountData = false;
                    response.Message = "Conta bancária não encontrada.";
                    response.Status = false;
                }
                return response;
            } catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }
    }
}
