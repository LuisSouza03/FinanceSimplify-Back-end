using FinanceSimplify.Data;
using FinanceSimplify.Dtos.Card;
using FinanceSimplify.Models.Card;
using Microsoft.EntityFrameworkCore;

namespace FinanceSimplify.Services.CardService {
    public class CardService : ICardInterface {
        private readonly AppDbContext _context;

        public CardService(AppDbContext context) {
            _context = context;
        }

        public async Task<CardCreateResponseModel<CardDto>> CreateCard(CardDto cardDto) {
            CardCreateResponseModel<CardDto> response = new();

            try {
                CardModel card = new() {
                    Id = Guid.NewGuid(),
                    Name = cardDto.Name,
                    Type = cardDto.Type,
                };

                _context.Card.Add(card);
                await _context.SaveChangesAsync();

                response.CardData = new CardDto {
                    Id = card.Id,
                    Name = cardDto.Name,
                    Type = cardDto.Type
                };

                response.Message = "Cartão cadastrado com sucesso!";
                return response;
            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }
        }

        //public async Task<CardCreateResponseModel<CardDto>> GetCardById() { }

        public async Task<List<CardModel>> GetAllCards() {
            return await _context.Card.ToListAsync();
        }
    }
}
