namespace FinanceSimplify.Models.Insight {
    public class InsightResponseModel<T> {
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }
}
