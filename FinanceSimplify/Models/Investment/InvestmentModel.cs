using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceSimplify.Models.Investment {
    public class InvestmentModel {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.String)]
        public Guid UserId { get; set; }

        [BsonElement("name")]
        public required string Name { get; set; }

        [BsonElement("initialAmount")]
        public decimal InitialAmount { get; set; }

        [BsonElement("startDate")]
        public DateTime StartDate { get; set; }

        [BsonElement("cdbRate")]
        public decimal CdbRate { get; set; } = 100; // Porcentagem do CDI (ex: 100 = 100% do CDI, 110 = 110%)

        [BsonElement("bank")]
        public string Bank { get; set; } = "Nubank"; // Ex: Nubank, Inter, Bradesco, C6, Itaú

        [BsonElement("type")]
        public string Type { get; set; } = "CDB"; // Tipo de investimento (CDB / Caixinha)

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
