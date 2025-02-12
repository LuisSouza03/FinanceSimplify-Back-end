namespace FinanceSimplify.Models.Transaction {
    public class TransactionResponseModel<T> {

        public T? TransactionData { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }
}
