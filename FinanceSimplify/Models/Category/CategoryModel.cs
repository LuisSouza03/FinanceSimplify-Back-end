using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceSimplify.Models.Category {
    public class CategoryModel {

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.String)]
        public Guid UserId { get; set; }
    }
}
