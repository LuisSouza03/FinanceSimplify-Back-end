using System.ComponentModel.DataAnnotations;
using FinanceSimplify.Enum;

namespace FinanceSimplify.Dtos.Card {
    public class CardDto {

        [Required(ErrorMessage = "O campo Name é obrigatório")]
        public string Name { get; set; }

        [Required(ErrorMessage = "O campo Type é obrigatório")]
        public TypeCardTransactionEnum Type { get; set; }
    }
}
