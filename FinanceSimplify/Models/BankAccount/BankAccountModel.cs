using System.ComponentModel.DataAnnotations;
using FinanceSimplify.Models.Card;
using FinanceSimplify.Models.Transaction;

namespace FinanceSimplify.Models.BankAccount {
    public class BankAccountModel {

        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string AccountName { get; set; }

        public List<CardModel> Cards { get; set; } = new();

        public List<TransactionModel> Transactions { get; set; } = new();

        public Guid UserId { get; set; }

    }
}
