using FinanceSimplify.Dtos.Card;
using FinanceSimplify.Services.CardService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSimplify.Controllers {

    [Route("api/cards")]
    [ApiController]
    public class CardController : Controller {

        private readonly ICardInterface _cardService;
        public CardController(ICardInterface cardService) {
            _cardService = cardService;
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<ActionResult> CreateCard(CardDto cardDto) {
            var response = await _cardService.CreateCard(cardDto);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("getAll")]
        public async Task<ActionResult> GetAllCards() {
            var cards = await _cardService.GetAllCards();
            return Ok(cards);
        }
    }
}
