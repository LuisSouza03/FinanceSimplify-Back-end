using FinanceSimplify.Enum;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceSimplify.Models.Transaction {
    public class TransactionModel {

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("amount")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Amount { get; set; }
        
        [BsonElement("date")]
        public DateTime Date { get; set; }
        
        [BsonElement("type")]
        public TypeTransactionEnum Type { get; set; }
        
        [BsonElement("paymentMethod")]
        public TypePaymentMethodEnum? PaymentMethod { get; set; }
        
        [BsonElement("installments")]
        public int? Installments { get; set; } // parcelas

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.String)]
        public Guid UserId { get; set; }

        [BsonElement("cardId")]
        [BsonRepresentation(BsonType.String)]
        public Guid? CardId { get; set; }

        [BsonElement("bankAccountId")]
        [BsonRepresentation(BsonType.String)]
        public Guid? BankAccountId { get; set; }

        [BsonElement("categoryId")]
        [BsonRepresentation(BsonType.String)]
        public Guid? CategoryId { get; set; }
    }
}
