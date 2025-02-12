using System.ComponentModel.DataAnnotations;

namespace FinanceSimplify.Dtos {
    public class UserCreateDto {

        [Required(ErrorMessage = "O campo usuário é obrigatório")]
        public string Name { get; set; }

        [Required(ErrorMessage = "O campo email é obrigatório"), EmailAddress(ErrorMessage = "Email inválido!")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O campo Senha é obrigatório")]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Senhas não coincidem!")]
        public string PasswordConfirmation { get; set; }

    }
}
