using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceSimplify.Models.User {
    public class UsuarioModel {

        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; }
        public string Name { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public DateTime TokenCreationDate { get; set; } = DateTime.Now;
    }
}
