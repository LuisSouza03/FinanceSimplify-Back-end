using FinanceSimplify.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceSimplify.Data {
    public class AppDbContext : DbContext {

        public AppDbContext(DbContextOptions<AppDbContext> options): base(options) {
            
        }

        public DbSet<UsuarioModel> Usuario { get; set; }

    }



}
