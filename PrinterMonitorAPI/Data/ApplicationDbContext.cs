using Microsoft.EntityFrameworkCore;
using PrinterMonitorAPI.Models;

namespace PrinterMonitorAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Printer> Impressoras { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Usa a configuração centralizada definida dentro da classe Printer
            Printer.Configure(modelBuilder);
        }
    }
}
