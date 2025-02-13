using System.ComponentModel.DataAnnotations;

namespace FinanceSimplify.Dtos.Category {
    public class CategoryDto {

        [Required(ErrorMessage = "O campo Name é obrigatório")]
        public string Name { get; set; }
    }
}
