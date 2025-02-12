using FinanceSimplify.Dtos;
using FinanceSimplify.Models;

namespace FinanceSimplify.Services.AuthService {
    public interface IAuthInterface {

        Task<ResponseModel<UserCreateDto>> Registrar(UserCreateDto userRegister);
        Task<ResponseModel<string>> Login(UserLoginDto userLogin);

    }
}
