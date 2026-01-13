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
        [HttpPost("create/{userId}")]
        public async Task<ActionResult> CreateCard(Guid userId, CardDto cardDto) {
            var response = await _cardService.CreateCard(userId, cardDto);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("getAll")]
        public async Task<ActionResult> GetAllCards() {
            var cards = await _cardService.GetAllCards();
            return Ok(cards);
        }

        [Authorize]
        [HttpGet("byUser/{userId}")]
        public async Task<ActionResult> GetCardByUserId(Guid userId, int page = 1, int pageSize = 10) {
            var response = await _cardService.GetCardByUserId(userId, page, pageSize);
            return Ok(response);
        }

        [Authorize]
        [HttpGet("byId/{cardId}")]
        public async Task<ActionResult> GetCardById(Guid cardId) {
            var card = await _cardService.GetCardById(cardId);
            return Ok(card);
        }

        [Authorize]
        [HttpDelete("delete/{userId}/{cardId}")]
        public async Task<ActionResult> DeleteCard(Guid userId, Guid cardId) {
            var card = await _cardService.DeleteCard(userId, cardId);
            return Ok(card);
        }

        [Authorize]
        [HttpPut("update/{userId}/{cardId}")]
        public async Task<ActionResult> UpdateCard(Guid userId, Guid cardId, CardUpdateDto cardUpdateDto) {
            var response = await _cardService.UpdateCard(cardId, userId, cardUpdateDto);
            return Ok(response);
        }
    }
}
