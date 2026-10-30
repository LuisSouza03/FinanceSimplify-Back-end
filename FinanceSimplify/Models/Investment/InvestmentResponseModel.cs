namespace FinanceSimplify.Models.Investment {
    public class InvestmentResponseModel<T> {
        public T? InvestmentData { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }
}
