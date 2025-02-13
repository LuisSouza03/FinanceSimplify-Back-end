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

        public async Task<CardCreateResponseModel<CardModel>> CreateCard(CardDto cardDto) {
            CardCreateResponseModel<CardModel> response = new();

            try {
                CardModel card = new() {
                    Name = cardDto.Name,
                    Type = cardDto.Type,
                };

                _context.Card.Add(card);
                await _context.SaveChangesAsync();

                response.CardData = new CardModel {
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

        public async Task<List<CardModel>> GetAllCards() {
            return await _context.Card.ToListAsync();
        }
    }
}
