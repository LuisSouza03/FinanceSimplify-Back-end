using System.ComponentModel.DataAnnotations;

namespace FinanceSimplify.Dtos.Transactions {
    public class CsvUploadDto {
        
        [Required(ErrorMessage = "O arquivo CSV é obrigatório")]
        public IFormFile CsvFile { get; set; }
        
        [Required(ErrorMessage = "O CardId é obrigatório")]
        public Guid CardId { get; set; }
    }
}
