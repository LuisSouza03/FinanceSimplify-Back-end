namespace FinanceSimplify.Models {
    public class ResponseModel<T> {

        public T? Token { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool status { get; set; } = true;

    }
}
