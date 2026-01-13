using System.ComponentModel.DataAnnotations;
using FinanceSimplify.Enum;

namespace FinanceSimplify.Dtos.Card {
    public class CardUpdateDto {

        [Required(ErrorMessage = "O campo Name é obrigatório")]
        public required string Name { get; set; }

        public decimal? CreditLimit { get; set; }

        public int? ClosingDay { get; set; }

        public int? DueDay { get; set; }

        public string? Color { get; set; }
    }
}
