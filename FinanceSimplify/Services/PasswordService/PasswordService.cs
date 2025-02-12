using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FinanceSimplify.Dtos;
using FinanceSimplify.Models;
using Microsoft.IdentityModel.Tokens;

namespace FinanceSimplify.Services.PasswordService {
    public class PasswordService: IPasswordInterface {
        public IConfiguration _config { get; }

        public PasswordService(IConfiguration config) {
            _config = config;
        }

        public void CreateHashPassword(string password, out byte[] passwordHash, out byte[] passwordSalt) {

            using (var hmac = new HMACSHA512()) {

                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            };
        }

        public bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt) {

            using (var hmac = new HMACSHA512(passwordSalt)) {

                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                return computedHash.SequenceEqual(passwordHash);
            };
        }

        public string CreateToken(UsuarioModel user) {

            List<Claim> claims = [
                new("Email", user.Email),
                new("Name", user.Name)
            ];

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value!));

            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: cred
             );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}
