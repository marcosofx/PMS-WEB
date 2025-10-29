using Microsoft.EntityFrameworkCore;
using PrinterMonitorAPI.Models;
using System.Text.Json;

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
            var options = new JsonSerializerOptions();

            modelBuilder.Entity<Printer>()
                .Property(p => p.Toners)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, options),
                    v => JsonSerializer.Deserialize<Dictionary<string, int>>(v, options) ?? new Dictionary<string, int>());

            modelBuilder.Entity<Printer>()
                .Property(p => p.Bandejas)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, options),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, options) ?? new Dictionary<string, string>());

            modelBuilder.Entity<Printer>()
                .Property(p => p.Alertas)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, options),
                    v => JsonSerializer.Deserialize<List<string>>(v, options) ?? new List<string>());
        }
    }
}
