namespace FinanceSimplify.Dtos.Insights {
    public class MonthlyInsightDto {
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Type { get; set; } = "positive"; // "saving", "warning", "positive"
        public string Recommendation { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetSavings { get; set; }
        public decimal SavingsRate { get; set; }
    }
}
