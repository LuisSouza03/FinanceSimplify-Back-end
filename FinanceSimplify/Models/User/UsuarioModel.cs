using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinanceSimplify.Models.User {
    public class UsuarioModel {

        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [BsonElement("email")]
        public string Email { get; set; }
        
        [BsonElement("name")]
        public string Name { get; set; }
        
        [BsonElement("passwordHash")]
        public byte[] PasswordHash { get; set; }
        
        [BsonElement("passwordSalt")]
        public byte[] PasswordSalt { get; set; }
        
        [BsonElement("tokenCreationDate")]
        public DateTime TokenCreationDate { get; set; } = DateTime.Now;
    }
}
