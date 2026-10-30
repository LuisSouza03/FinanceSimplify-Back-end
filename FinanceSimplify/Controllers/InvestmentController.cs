using FinanceSimplify.Dtos.Investment;
using FinanceSimplify.Services.InvestmentService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSimplify.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvestmentController : Controller {
        private readonly IInvestmentInterface _investmentService;

        public InvestmentController(IInvestmentInterface investmentService) {
            _investmentService = investmentService;
        }

        [HttpPost("create/{userId}")]
        public async Task<ActionResult> CreateInvestment(Guid userId, [FromBody] InvestmentCreateDto dto) {
            var response = await _investmentService.CreateInvestment(userId, dto);
            if (!response.Status) {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult> GetInvestmentsByUserId(Guid userId, [FromQuery] decimal? customCdi = null) {
            var response = await _investmentService.GetInvestmentsByUserId(userId, customCdi);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetInvestmentById(Guid id, [FromQuery] decimal? customCdi = null) {
            var response = await _investmentService.GetInvestmentById(id, customCdi);
            if (!response.Status) {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpPut("update/{id}")]
        public async Task<ActionResult> UpdateInvestment(Guid id, [FromBody] InvestmentEditDto dto) {
            var response = await _investmentService.UpdateInvestment(id, dto);
            if (!response.Status) {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpDelete("delete/{id}")]
        public async Task<ActionResult> DeleteInvestment(Guid id) {
            var response = await _investmentService.DeleteInvestment(id);
            if (!response.Status) {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpGet("cdi-rate")]
        [AllowAnonymous]
        public async Task<ActionResult> GetCurrentCdiRate() {
            var rate = await _investmentService.GetCurrentCdiRate();
            return Ok(new { cdiRate = rate });
        }
    }
}
