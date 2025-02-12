using System.ComponentModel.DataAnnotations;

namespace FinanceSimplify.Models {
    public class UsuarioModel {

       public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public DateTime TokenCreationDate { get; set; } = DateTime.Now;
    }
}
