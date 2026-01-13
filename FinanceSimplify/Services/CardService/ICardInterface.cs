using FinanceSimplify.Dtos.Card;
using FinanceSimplify.Models.Card;

namespace FinanceSimplify.Services.CardService {
    public interface ICardInterface {

        Task<CardResponseModel<CardResponseDto>> CreateCard(Guid userId, CardDto cardDto);

        Task<CardResponseModel<CardResponseDto>> GetCardById(Guid cardId);

        Task<List<CardModel>> GetCardByUserId(Guid userId, int page, int pageSize);

        Task<List<CardModel>> GetAllCards();

        Task<CardResponseModel<bool>> DeleteCard(Guid userId, Guid cardId);

        Task<decimal> GetAvailableLimit(Guid cardId);

        Task<bool> UpdateAvailableLimit(Guid cardId, decimal newAvailableLimit);
    }
}
