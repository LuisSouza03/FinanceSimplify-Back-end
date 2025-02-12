namespace FinanceSimplify.Models.User {
    public class UserResponseModel<T> {

        public T? Token { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool status { get; set; } = true;

    }
}
