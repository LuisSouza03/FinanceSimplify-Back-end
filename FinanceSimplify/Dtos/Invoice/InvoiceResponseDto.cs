namespace FinanceSimplify.Dtos.Invoice {
    public class InvoiceResponseDto {
        public Guid Id { get; set; }
        public Guid CardId { get; set; }
        public int ReferenceMonth { get; set; }
        public int ReferenceYear { get; set; }
        public DateTime ClosingDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidDate { get; set; }
        public List<Dtos.Installment.InstallmentResponseDto> Installments { get; set; } = new();
    }
}
