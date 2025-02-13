using FinanceSimplify.Models.Card;
using FinanceSimplify.Models.Category;
using FinanceSimplify.Models.Transaction;
using FinanceSimplify.Models.User;
using Microsoft.EntityFrameworkCore;

namespace FinanceSimplify.Data {
    public class AppDbContext : DbContext {

        public AppDbContext(DbContextOptions<AppDbContext> options): base(options) {
            
        }

        public DbSet<UsuarioModel> Usuario { get; set; }
        public DbSet<TransactionModel> Transaction { get; set; }
        public DbSet<CardModel> Card { get; set; }
        public DbSet<CategoryModel> Categories { get; set; }
    }



}
