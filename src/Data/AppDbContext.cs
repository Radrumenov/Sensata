using Microsoft.EntityFrameworkCore;
using Sensata.Models;

namespace Sensata.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<ProductionMachine> Machines { get; set; }
        // Добавяме новите таблици
        public DbSet<ProductionReport> Reports { get; set; }
        public DbSet<MachineAlert> Alerts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=Sensata.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Извикваме изнесената логика за пълнене на данни!
            modelBuilder.Seed();
        }
    }
}