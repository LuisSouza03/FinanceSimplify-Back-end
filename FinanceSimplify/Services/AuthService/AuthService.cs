using FinanceSimplify.Data;
using FinanceSimplify.Dtos;
using FinanceSimplify.Models;
using FinanceSimplify.Services.PasswordService;
using Microsoft.EntityFrameworkCore;

namespace FinanceSimplify.Services.AuthService {
    public class AuthService: IAuthInterface {
        private readonly AppDbContext _context;
        private readonly IPasswordInterface _passwordInterface;

        public AuthService(AppDbContext context, IPasswordInterface passwordInterface) {
            this._context = context;
            this._passwordInterface = passwordInterface;
        }
        

        public async Task<ResponseModel<UserCreateDto>> Registrar(UserCreateDto userRegister) {

            ResponseModel<UserCreateDto> response = new ResponseModel<UserCreateDto>();

            try {

                if(!UserAndEmailExists(userRegister)) {
                    response.Token = null;
                    response.Message = "Usuário ja cadastrado!";
                    response.status = false;

                    return response;
                }

                _passwordInterface.CreateHashPassword(userRegister.Password, out byte[] passwordHash, out byte[] passwordSalt);

                UsuarioModel user = new() {
                    Email = userRegister.Email,
                    Name = userRegister.Name,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt
                };

                _context.Add(user);
                await _context.SaveChangesAsync();

                response.Message = "Usuário criado com suceso!";
                return response;

            }
            catch (Exception ex) {
                response.Token = null;
                response.Message = ex.Message;
                response.status = false;
                return response;
            }

        }

        public async Task<ResponseModel<string>> Login(UserLoginDto userLogin) {

            ResponseModel<string> response = new ResponseModel<string>();

            try {

                var user = await _context.Usuario.FirstOrDefaultAsync(userBanco => userBanco.Email == userLogin.Email);

                if (user == null) {
                    response.Message = "Credencias inválidas!";
                    response.status = false;
                    return response;
                }

                if(!_passwordInterface.VerifyPasswordHash(userLogin.Password, user.PasswordHash, user.PasswordSalt)) {
                    response.Message = "Credencias inválidas!";
                    response.status = false;
                    return response;
                }

                var token = _passwordInterface.CreateToken(user);

                response.Token = token;
                response.Message = "Usuário logado com sucesso!";
                return response;

            }
            catch (Exception ex) {
                response.Token = null;
                response.Message = ex.Message;
                response.status = false;
                return response;
            }
        }

        public bool UserAndEmailExists(UserCreateDto userRegister) {

            var user = _context.Usuario.FirstOrDefault(userBanco => userBanco.Email == userRegister.Email);

            if (user != null) return false;

            return true;
        }

        

    }
}
