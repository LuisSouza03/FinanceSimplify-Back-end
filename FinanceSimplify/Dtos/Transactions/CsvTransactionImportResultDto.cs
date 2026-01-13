namespace FinanceSimplify.Dtos.Transactions {
    public class CsvTransactionImportResultDto {
        
        public int TotalProcessed { get; set; }
        public int TransactionsCreated { get; set; }
        public int InstallmentsCreated { get; set; }
        public int CategoriesCreated { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> NewCategories { get; set; } = new List<string>();
    }
}
