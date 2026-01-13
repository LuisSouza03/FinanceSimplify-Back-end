namespace FinanceSimplify.Models.Invoice {
    public class InvoiceResponseModel<T> {
        public T? InvoiceData { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }
}
