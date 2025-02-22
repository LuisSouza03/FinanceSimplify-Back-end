namespace FinanceSimplify.Models.BankAccount {
    public class BankAccountResponseModel<T> {

        public T? BankAccountData { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }
}
