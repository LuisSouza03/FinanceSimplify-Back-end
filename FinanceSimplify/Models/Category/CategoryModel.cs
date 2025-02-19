using System.ComponentModel.DataAnnotations;
using FinanceSimplify.Models.Transaction;

namespace FinanceSimplify.Models.Category {
    public class CategoryModel {

        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; }

        public Guid UserId { get; set; }

    }
}
