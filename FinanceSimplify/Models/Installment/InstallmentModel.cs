using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceSimplify.Models.Installment {
    public class InstallmentModel {

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("transactionId")]
        [BsonRepresentation(BsonType.String)]
        public Guid TransactionId { get; set; }

        [BsonElement("cardId")]
        [BsonRepresentation(BsonType.String)]
        public Guid CardId { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.String)]
        public Guid UserId { get; set; }

        [BsonElement("amount")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Amount { get; set; }

        [BsonElement("installmentNumber")]
        public int InstallmentNumber { get; set; }

        [BsonElement("totalInstallments")]
        public int TotalInstallments { get; set; }

        [BsonElement("dueDate")]
        public DateTime DueDate { get; set; }

        [BsonElement("invoiceId")]
        [BsonRepresentation(BsonType.String)]
        public Guid? InvoiceId { get; set; }

        [BsonElement("isPaid")]
        public bool IsPaid { get; set; } = false;

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
