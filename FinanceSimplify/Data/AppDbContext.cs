using FinanceSimplify.Models.BankAccount;
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

        public DbSet<BankAccountModel> BankAccount { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BankAccountModel>()
                .HasMany(b => b.Cards)
                .WithOne(c => c.BankAccount)
                .HasForeignKey(c => c.BankAccountId);

            modelBuilder.Entity<BankAccountModel>()
                .HasMany(b => b.Transactions)
                .WithOne(t => t.BankAccount)
                .HasForeignKey(t => t.BankAccountId);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            if (!optionsBuilder.IsConfigured) {
                optionsBuilder.UseSqlServer("SuaConnectionString", sqlOptions => {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5, // Número máximo de tentativas
                        maxRetryDelay: TimeSpan.FromSeconds(10), // Tempo máximo entre tentativas
                        errorNumbersToAdd: null // Use null para aplicar a todos os erros transitórios conhecidos
                    );
                });
            }
        }
    }



}
