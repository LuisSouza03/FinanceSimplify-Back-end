using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceSimplify.Models.Invoice {
    public class InvoiceModel {

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("cardId")]
        [BsonRepresentation(BsonType.String)]
        public Guid CardId { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.String)]
        public Guid UserId { get; set; }

        [BsonElement("referenceMonth")]
        public int ReferenceMonth { get; set; }

        [BsonElement("referenceYear")]
        public int ReferenceYear { get; set; }

        [BsonElement("closingDate")]
        public DateTime ClosingDate { get; set; }

        [BsonElement("dueDate")]
        public DateTime DueDate { get; set; }

        [BsonElement("totalAmount")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalAmount { get; set; }

        [BsonElement("isPaid")]
        public bool IsPaid { get; set; } = false;

        [BsonElement("paidDate")]
        public DateTime? PaidDate { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
