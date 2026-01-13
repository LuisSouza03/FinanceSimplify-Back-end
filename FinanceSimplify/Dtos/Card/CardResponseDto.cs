using System.ComponentModel.DataAnnotations;
using FinanceSimplify.Enum;

namespace FinanceSimplify.Dtos.Card {
    public class CardResponseDto {

        [Key]
        public Guid Id { get; set; }

        public required string Name { get; set; }

        public TypeCardTransactionEnum Type { get; set; }

        public Guid? BankAccountId { get; set; }

        public decimal? CreditLimit { get; set; }

        public decimal? AvailableLimit { get; set; }

        public int? ClosingDay { get; set; }

        public int? DueDay { get; set; }
    }
}
