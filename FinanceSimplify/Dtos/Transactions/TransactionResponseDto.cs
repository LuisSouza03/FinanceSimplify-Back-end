using FinanceSimplify.Enum;
using System.ComponentModel.DataAnnotations;

namespace FinanceSimplify.Dtos.Transactions {
    public class TransactionResponseDto {

        [Required(ErrorMessage = "O campo Id é obrigatório")]
        public int Id { get; set; }

        [Required(ErrorMessage = "O campo Name é obrigatório")]
        public string Name { get; set; }

        [Required(ErrorMessage = "O campo Amount é obrigatório")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "O campo Date é obrigatório")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "O campo Type é obrigatório")]
        public TypeTransactionEnum Type { get; set; }

        public TypePaymentMethodEnum? PaymentMethod { get; set; }
        public int? Installments { get; set; }
        public int? CardId { get; set; }
        public int? CategoryId { get; set; }
    }
}
