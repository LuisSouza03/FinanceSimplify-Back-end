using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FinanceSimplify.Enum;
using FinanceSimplify.Models.BankAccount;
using FinanceSimplify.Models.Transaction;

namespace FinanceSimplify.Models.Card {
    public class CardModel {

        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Name { get; set; }
        public TypeCardTransactionEnum Type { get; set; }

        public Guid BankAccountId { get; set; }

        [ForeignKey("BankAccountId")]
        public BankAccountModel BankAccount { get; set; } = null!;

        public Guid UserId { get; set; }
    }
}
