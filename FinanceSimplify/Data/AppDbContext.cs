using FinanceSimplify.Models.Transaction;
using FinanceSimplify.Models.User;
using Microsoft.EntityFrameworkCore;

namespace FinanceSimplify.Data {
    public class AppDbContext : DbContext {

        public AppDbContext(DbContextOptions<AppDbContext> options): base(options) {
            
        }

        public DbSet<UsuarioModel> Usuario { get; set; }

        public DbSet<TransactionModel> Transaction { get; set; }

    }



}
