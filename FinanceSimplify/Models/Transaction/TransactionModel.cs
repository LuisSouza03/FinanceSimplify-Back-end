using System.ComponentModel.DataAnnotations.Schema;
using FinanceSimplify.Enum;
using FinanceSimplify.Models.Card;
using FinanceSimplify.Models.Category;

namespace FinanceSimplify.Models.Transaction {
    public class TransactionModel {

        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public TypeTransactionEnum Type { get; set; }
        public TypePaymentMethodEnum? PaymentMethod { get; set; }
        public int? Installments { get; set; } // parcelas

        public int? CardId { get; set; }
        [ForeignKey("CardId")]
        public CardModel? Card { get; set; }

        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public CategoryModel Category { get; set; }
    }
}
