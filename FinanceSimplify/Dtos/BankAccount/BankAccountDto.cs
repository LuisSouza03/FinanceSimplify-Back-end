namespace FinanceSimplify.Dtos.BankAccount {
    public class BankAccountDto {

        public string? NameAccount { get; set; }

        public string? AccountName { get; set; }

        public string ResolvedName => !string.IsNullOrWhiteSpace(NameAccount)
            ? NameAccount
            : (!string.IsNullOrWhiteSpace(AccountName) ? AccountName : string.Empty);
    }
}
