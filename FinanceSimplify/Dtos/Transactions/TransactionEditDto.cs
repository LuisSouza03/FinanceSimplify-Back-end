using FinanceSimplify.Enum;
using System.ComponentModel.DataAnnotations;

namespace FinanceSimplify.Dtos.Transactions {
    public class TransactionEditDto {
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public TypeTransactionEnum Type { get; set; }
        public TypePaymentMethodEnum? PaymentMethod { get; set; }
        public int? Installments { get; set; }
        public int? CardId { get; set; }
        public int? CategoryId { get; set; }
    }
}
