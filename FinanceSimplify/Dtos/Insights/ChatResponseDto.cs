namespace FinanceSimplify.Dtos.Insights {
    public class ChatResponseDto {
        public string Response { get; set; } = string.Empty;
        public bool IsAction { get; set; } = false;
        public string? ActionType { get; set; }
        public TransactionActionDetailsDto? ActionDetails { get; set; }
    }

    public class TransactionActionDetailsDto {
        public Guid? TransactionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string FormattedAmount { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Type { get; set; } = "Despesa";
        public string Status { get; set; } = "success";
        public DateTime Date { get; set; }
    }
}
