namespace FinanceSimplify.Models.Category {
    public class CategoryResponseModel<T> {

        public T? CategoryData { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }
}
