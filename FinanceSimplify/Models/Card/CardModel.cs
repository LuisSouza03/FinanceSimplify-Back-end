using FinanceSimplify.Enum;
using FinanceSimplify.Models.Transaction;

namespace FinanceSimplify.Models.Card {
    public class CardModel {

        public int Id { get; set; }
        public string Name { get; set; }
        public TypeCardTransactionEnum Type { get; set; }
    }
}
