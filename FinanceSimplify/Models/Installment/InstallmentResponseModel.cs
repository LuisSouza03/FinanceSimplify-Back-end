namespace FinanceSimplify.Models.Installment {
    public class InstallmentResponseModel<T> {
        public T? InstallmentData { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }
}
