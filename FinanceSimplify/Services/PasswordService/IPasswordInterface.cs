using FinanceSimplify.Models.User;

namespace FinanceSimplify.Services.PasswordService {
    public interface IPasswordInterface {

        void CreateHashPassword(string password, out byte[] passwordHash, out byte[] passwordSalt);

        bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt);

        string CreateToken(UsuarioModel user);
    }
}
