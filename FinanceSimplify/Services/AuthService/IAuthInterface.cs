using FinanceSimplify.Dtos.User;
using FinanceSimplify.Models.User;

namespace FinanceSimplify.Services.AuthService {
    public interface IAuthInterface {

        Task<UserResponseModel<UserCreateDto>> Registrar(UserCreateDto userRegister);
        Task<UserResponseModel<string>> Login(UserLoginDto userLogin);

    }
}
