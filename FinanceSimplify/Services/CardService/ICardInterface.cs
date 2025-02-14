using FinanceSimplify.Dtos.Card;
using FinanceSimplify.Models.Card;

namespace FinanceSimplify.Services.CardService {
    public interface ICardInterface {

        Task<CardCreateResponseModel<CardDto>> CreateCard(CardDto cardDto);

        Task<List<CardModel>> GetAllCards();
    }
}
