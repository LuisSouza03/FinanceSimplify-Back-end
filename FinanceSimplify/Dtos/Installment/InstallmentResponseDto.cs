namespace FinanceSimplify.Dtos.Installment {
    public class InstallmentResponseDto {
        public Guid Id { get; set; }
        public Guid TransactionId { get; set; }
        public Guid CardId { get; set; }
        public decimal Amount { get; set; }
        public int InstallmentNumber { get; set; }
        public int TotalInstallments { get; set; }
        public DateTime DueDate { get; set; }
        public Guid? InvoiceId { get; set; }
        public bool IsPaid { get; set; }
        public string Description { get; set; }
    }
}
