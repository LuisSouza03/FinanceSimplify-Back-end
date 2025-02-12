using FinanceSimplify.Dtos.Transactions;
using FinanceSimplify.Services.TransactionService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSimplify.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : Controller {

        private readonly ITransactionInterface _transactionInterface;

        public TransactionController(ITransactionInterface transactionService) {
            _transactionInterface = transactionService;
        }

        [Authorize]
        [HttpPost("Create")]
        public async Task<ActionResult> CreateTransaction (TransactionCreateDto trnsaction) {
            var response = await _transactionInterface.CreateTransaction(trnsaction);
            return Ok(response);
        }
    }
}
