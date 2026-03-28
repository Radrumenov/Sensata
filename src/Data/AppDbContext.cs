using Microsoft.EntityFrameworkCore;
using Sensata.Models;

namespace Sensata.Data
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
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
            // 1. Изрично свързваме Reports с MachineId
            modelBuilder.Entity<ProductionMachine>()
            .HasMany(m => m.Reports)
            .WithOne()
            .HasForeignKey(r => r.MachineId);

        // 2. Изрично свързваме Alerts с MachineId
            modelBuilder.Entity<ProductionMachine>()
            .HasMany(m => m.Alerts)
            .WithOne()
            .HasForeignKey(a => a.MachineId);

            // Извикваме изнесената логика за пълнене на данни!
            modelBuilder.Seed();
        }
    }
}