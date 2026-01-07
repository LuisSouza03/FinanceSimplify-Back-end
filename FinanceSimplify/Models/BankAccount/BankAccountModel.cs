using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceSimplify.Models.BankAccount {
    public class BankAccountModel {

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("accountName")]
        public required string AccountName { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.String)]
        public Guid UserId { get; set; }
    }
}
