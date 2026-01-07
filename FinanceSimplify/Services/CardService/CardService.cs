using FinanceSimplify.Data;
using FinanceSimplify.Dtos.Card;
using FinanceSimplify.Models.Card;
using MongoDB.Driver;

namespace FinanceSimplify.Services.CardService {
    public class CardService : ICardInterface {
        private readonly MongoDbContext _context;

        public CardService(MongoDbContext context) {
            _context = context;
        }

        public async Task<CardResponseModel<CardResponseDto>> CreateCard(Guid userId, CardDto cardDto) {
            CardResponseModel<CardResponseDto> response = new();

            try {
                CardModel card = new() {
                    Id = Guid.NewGuid(),
                    Name = cardDto.Name,
                    Type = cardDto.Type,
                    UserId = userId,
                    BankAccountId = cardDto.BankAccountId
                };

                await _context.Cards.InsertOneAsync(card);

                response.CardData = new CardResponseDto {
                    Id = card.Id,
                    Name = cardDto.Name,
                    Type = cardDto.Type,
                    BankAccountId = cardDto.BankAccountId
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

        public async Task<CardResponseModel<CardResponseDto>> GetCardById(Guid cardId) {
            CardResponseModel<CardResponseDto> response = new();

            try {

                var card = await _context.Cards.Find(c => c.Id == cardId).FirstOrDefaultAsync();

                if (card == null) {
                    response.Message = "Cartão não encontrado!";
                    response.Status = false;
                    return response;
                }

                response.CardData = new CardResponseDto {
                    Id = card.Id,
                    Name = card.Name,
                    Type = card.Type,
                };

                response.Message = "Cartão encontrado com sucesso!";
                return response;


            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }

        }

        public async Task<List<CardModel>> GetCardByUserId(Guid userId, int page, int pageSize) {

            return await _context.Cards
                .Find(c => c.UserId == userId)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<List<CardModel>> GetAllCards() {
            return await _context.Cards.Find(_ => true).ToListAsync();
        }

        public async Task<CardResponseModel<bool>> DeleteCard(Guid userId, Guid cardId) {
             CardResponseModel<bool> response = new();

            try {
                var result = await _context.Cards.DeleteOneAsync(c => c.Id == cardId && c.UserId == userId);

                if (result.DeletedCount == 0) {
                    response.Message = "Nenhum cartão encontrado!";
                    response.Status = false;
                    return response;
                }

                response.CardData = true;
                response.Message = "Cartão excluido com sucesso!";
                return response;

            }
            catch (Exception ex) {
                response.Message = ex.Message;
                response.Status = false;
                return response;
            }

        }
    }
}
