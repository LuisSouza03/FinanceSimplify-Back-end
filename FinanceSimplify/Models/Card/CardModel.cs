using FinanceSimplify.Enum;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceSimplify.Models.Card {
    public class CardModel {

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [BsonElement("name")]
        public required string Name { get; set; }
        
        [BsonElement("type")]
        public TypeCardTransactionEnum Type { get; set; }

        [BsonElement("bankAccountId")]
        [BsonRepresentation(BsonType.String)]
        public Guid BankAccountId { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.String)]
        public Guid UserId { get; set; }
    }
}
