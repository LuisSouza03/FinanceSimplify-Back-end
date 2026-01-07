using FinanceSimplify.Models.BankAccount;
using FinanceSimplify.Models.Card;
using FinanceSimplify.Models.Category;
using FinanceSimplify.Models.Transaction;
using FinanceSimplify.Models.User;
using MongoDB.Driver;

namespace FinanceSimplify.Data {
    public class MongoDbContext {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IMongoDatabase database) {
            _database = database;
        }

        public IMongoCollection<UsuarioModel> Users => _database.GetCollection<UsuarioModel>("Users");
        public IMongoCollection<TransactionModel> Transactions => _database.GetCollection<TransactionModel>("Transactions");
        public IMongoCollection<CardModel> Cards => _database.GetCollection<CardModel>("Cards");
        public IMongoCollection<CategoryModel> Categories => _database.GetCollection<CategoryModel>("Categories");
        public IMongoCollection<BankAccountModel> BankAccounts => _database.GetCollection<BankAccountModel>("BankAccounts");
    }
}
