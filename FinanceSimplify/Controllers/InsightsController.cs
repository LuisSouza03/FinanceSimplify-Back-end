using FinanceSimplify.Dtos.Insights;
using FinanceSimplify.Services.InsightService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSimplify.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class InsightsController : ControllerBase {
        private readonly IInsightInterface _insightService;

        public InsightsController(IInsightInterface insightService) {
            _insightService = insightService;
        }

        [Authorize]
        [HttpPost("chat/{userId}")]
        public async Task<IActionResult> Chat(Guid userId, [FromBody] ChatRequestDto request) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            }

            var result = await _insightService.ProcessChat(userId, request);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("monthly/{userId}")]
        public async Task<IActionResult> GetMonthlyInsight(Guid userId) {
            var result = await _insightService.GetMonthlyInsight(userId, forceRefresh: false);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("monthly/{userId}/refresh")]
        public async Task<IActionResult> RefreshMonthlyInsight(Guid userId) {
            var result = await _insightService.GetMonthlyInsight(userId, forceRefresh: true);
            return Ok(result);
        }
    }
}
