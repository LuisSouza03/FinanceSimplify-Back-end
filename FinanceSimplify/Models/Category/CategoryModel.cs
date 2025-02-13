using FinanceSimplify.Models.Transaction;

namespace FinanceSimplify.Models.Category {
    public class CategoryModel {

        public int Id { get; set; }

        public string Name { get; set; }

        public List<TransactionModel> Transactions { get; set; } = new();

    }
}
