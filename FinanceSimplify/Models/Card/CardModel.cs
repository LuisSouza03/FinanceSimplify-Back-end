using System.ComponentModel.DataAnnotations;
using FinanceSimplify.Enum;
using FinanceSimplify.Models.Transaction;

namespace FinanceSimplify.Models.Card {
    public class CardModel {

        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public TypeCardTransactionEnum Type { get; set; }
    }
}
