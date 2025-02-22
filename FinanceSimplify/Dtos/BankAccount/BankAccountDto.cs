using FinanceSimplify.Enum;
using System.ComponentModel.DataAnnotations;

namespace FinanceSimplify.Dtos.BankAccount {
    public class BankAccountDto {

        [Required(ErrorMessage = "O campo NameAccount é obrigatório")]
        public required string NameAccount { get; set; }

    }
}
