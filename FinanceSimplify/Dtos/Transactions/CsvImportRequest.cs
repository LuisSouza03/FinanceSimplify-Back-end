using Microsoft.AspNetCore.Http;

namespace FinanceSimplify.Dtos.Transactions {
    public class CsvImportRequest {
        public Guid CardId { get; set; }
        public IFormFile CsvFile { get; set; }
    }
}
