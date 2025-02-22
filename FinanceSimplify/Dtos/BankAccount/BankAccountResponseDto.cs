using FinanceSimplify.Enum;
using System.ComponentModel.DataAnnotations;

namespace FinanceSimplify.Dtos.BankAccount {
    public class BankAccountResponseDto {

        [Key]
        public Guid Id { get; set; }

        public required string NameAccount { get; set; }
    }
}
