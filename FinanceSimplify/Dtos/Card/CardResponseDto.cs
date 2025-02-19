using System.ComponentModel.DataAnnotations;
using FinanceSimplify.Enum;

namespace FinanceSimplify.Dtos.Card {
    public class CardResponseDto {

        [Key]
        public Guid Id { get; set; }

        public required string Name { get; set; }

        public TypeCardTransactionEnum Type { get; set; }
    }
}
