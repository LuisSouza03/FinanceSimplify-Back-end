namespace FinanceSimplify.Models.Card {
    public class CardCreateResponseModel<T> {

        public T? CardData { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }
}
