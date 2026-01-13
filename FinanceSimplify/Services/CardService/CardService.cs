using FinanceSimplify.Data;
using FinanceSimplify.Dtos.Card;
using FinanceSimplify.Models.Card;
using FinanceSimplify.Enum;
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
                // Validar dias de fechamento e vencimento para cartões de crédito
                if (cardDto.Type == TypeCardTransactionEnum.Credito) {
                    if (cardDto.CreditLimit.HasValue && cardDto.CreditLimit.Value <= 0) {
                        response.Message = "O limite de crédito deve ser maior que zero!";
                        response.Status = false;
                        return response;
                    }

                    if (cardDto.ClosingDay.HasValue && (cardDto.ClosingDay.Value < 1 || cardDto.ClosingDay.Value > 31)) {
                        response.Message = "O dia de fechamento deve estar entre 1 e 31!";
                        response.Status = false;
                        return response;
                    }

                    if (cardDto.DueDay.HasValue && (cardDto.DueDay.Value < 1 || cardDto.DueDay.Value > 31)) {
                        response.Message = "O dia de vencimento deve estar entre 1 e 31!";
                        response.Status = false;
                        return response;
                    }
                }

                // Validar cor (se fornecida)
                if (!string.IsNullOrEmpty(cardDto.Color)) {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(cardDto.Color, "^#[0-9A-Fa-f]{6}$")) {
                        response.Message = "A cor deve estar no formato hexadecimal (#RRGGBB)!";
                        response.Status = false;
                        return response;
                    }
                }

                CardModel card = new() {
                    Id = Guid.NewGuid(),
                    Name = cardDto.Name,
                    Type = cardDto.Type,
                    UserId = userId,
                    BankAccountId = cardDto.BankAccountId,
                    CreditLimit = cardDto.CreditLimit,
                    AvailableLimit = cardDto.CreditLimit, // Inicialmente, limite disponível = limite total
                    ClosingDay = cardDto.ClosingDay,
                    DueDay = cardDto.DueDay,
                    Color = string.IsNullOrEmpty(cardDto.Color) ? "#22C55E" : cardDto.Color // Verde padrão se não fornecido
                };

                await _context.Cards.InsertOneAsync(card);

                response.CardData = new CardResponseDto {
                    Id = card.Id,
                    Name = cardDto.Name,
                    Type = cardDto.Type,
                    BankAccountId = cardDto.BankAccountId,
                    CreditLimit = card.CreditLimit,
                    AvailableLimit = card.AvailableLimit,
                    ClosingDay = card.ClosingDay,
                    DueDay = card.DueDay,
                    Color = card.Color
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
                    BankAccountId = card.BankAccountId,
                    CreditLimit = card.CreditLimit,
                    AvailableLimit = card.AvailableLimit,
                    ClosingDay = card.ClosingDay,
                    DueDay = card.DueDay,
                    Color = card.Color
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

        public async Task<decimal> GetAvailableLimit(Guid cardId) {
            var card = await _context.Cards.Find(c => c.Id == cardId).FirstOrDefaultAsync();
            
            if (card == null || !card.CreditLimit.HasValue) {
                return 0;
            }

            // Buscar todas as parcelas pendentes (não pagas) deste cartão
            var pendingInstallments = await _context.Installments
                .Find(i => i.CardId == cardId && !i.IsPaid)
                .ToListAsync();

            var usedLimit = pendingInstallments.Sum(i => i.Amount);
            var availableLimit = card.CreditLimit.Value - usedLimit;

            return availableLimit > 0 ? availableLimit : 0;
        }

        public async Task<bool> UpdateAvailableLimit(Guid cardId, decimal newAvailableLimit) {
            try {
                var update = Builders<CardModel>.Update.Set(c => c.AvailableLimit, newAvailableLimit);
                var result = await _context.Cards.UpdateOneAsync(c => c.Id == cardId, update);
                return result.ModifiedCount > 0;
            }
            catch {
                return false;
            }
        }
    }
}
