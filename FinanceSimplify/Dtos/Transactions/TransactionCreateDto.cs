﻿using System.ComponentModel.DataAnnotations;
using FinanceSimplify.Enum;

namespace FinanceSimplify.Dtos.Transactions {
    public class TransactionCreateDto {

        [Required(ErrorMessage = "O campo Name é obrigatório")]
        public string Name { get; set; }

        [Required(ErrorMessage = "O campo Amount é obrigatório")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "O campo Date é obrigatório")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "O campo Type é obrigatório")]
        public TypeTransactionEnum Type {  get; set; }

        public TypePaymentMethodEnum? PaymentMethod { get; set; }
        public int? Installments {  get; set; }
        public Guid? CardId { get; set; }

        [Required(ErrorMessage = "O campo CategoryId é obrigatório")]
        public Guid? BankAccountId { get; set; }

        [Required(ErrorMessage = "O campo CategoryId é obrigatório")]
        public Guid? CategoryId { get; set; }



    }
}
