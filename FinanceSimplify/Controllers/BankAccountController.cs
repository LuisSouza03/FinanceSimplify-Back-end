using FinanceSimplify.Dtos.BankAccount;
using FinanceSimplify.Models.BankAccount;
using FinanceSimplify.Services.BankAccountService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSimplify.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BankAccountController : Controller {
        
        private readonly IBankAccountInterface _bankAccountService;

        public BankAccountController(IBankAccountInterface bankAccountService) {
            _bankAccountService  = bankAccountService;
        }


        [HttpPost("create/{userId}")]
        public async Task<ActionResult> CreateBankAccount(Guid userId, BankAccountDto bankAccountDto) {
            var response = await _bankAccountService.CreateBankAccount(userId, bankAccountDto);
            return Ok(response);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<BankAccountModel>>> GetBankAccountsByUserId(Guid userId, int page = 1, int pageSize = 10) {
            var response = await _bankAccountService.GetBankAccountsByUserId(userId, page, pageSize);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BankAccountModel>> GetBankAccountById(Guid id) {
            var bankAccount = await _bankAccountService.GetBankAccountById(id);
            if (bankAccount == null) {
                return NotFound("Conta bancária não encontrada.");
            }
            return Ok(bankAccount);
        }

        
    }
}
