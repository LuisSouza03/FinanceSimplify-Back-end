using System.ComponentModel.DataAnnotations;
using FinanceSimplify.Enum;

namespace FinanceSimplify.Dtos.Card {
    public class CardDto {

        [Required(ErrorMessage = "O campo Name é obrigatório")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "O campo Type é obrigatório")]
        public TypeCardTransactionEnum Type { get; set; }

        [Required(ErrorMessage = "O campo BankAccountId é obrigatório")]
        public Guid BankAccountId { get; set; }

        public decimal? CreditLimit { get; set; }

        public int? ClosingDay { get; set; }

        public int? DueDay { get; set; }
    }
}
