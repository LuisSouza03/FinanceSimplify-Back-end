using System.ComponentModel.DataAnnotations;
using FinanceSimplify.Enum;

namespace FinanceSimplify.Dtos.Category {
    public class CategoryResponseDto {

        [Key]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "O campo Name é obrigatório")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "O campo Type é obrigatório")]
        public TypeCardTransactionEnum Type { get; set; }
    }
}
