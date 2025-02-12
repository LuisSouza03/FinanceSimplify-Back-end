using System.ComponentModel.DataAnnotations;

namespace FinanceSimplify.Dtos.User {
    public class UserLoginDto {

        [Required(ErrorMessage = "O campo email é obrigatório"), EmailAddress(ErrorMessage = "Email inválido!")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O campo Senha é obrigatório")]
        public string Password { get; set; }

    }
}
