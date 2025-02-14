using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FinanceSimplify.Enum;
using FinanceSimplify.Models.Card;
using FinanceSimplify.Models.Category;

namespace FinanceSimplify.Models.Transaction {
    public class TransactionModel {

        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public TypeTransactionEnum Type { get; set; }
        public TypePaymentMethodEnum? PaymentMethod { get; set; }
        public int? Installments { get; set; } // parcelas

        public Guid UserId { get; set; }

        public Guid? CardId { get; set; }

        public Guid? CategoryId { get; set; }
    }
}
