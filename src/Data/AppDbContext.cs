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
     


        public DbSet<ProductionReport> Reports { get; set; }
        public DbSet<MachineAlert> Alerts { get; set; }

        // conection with SQlite DB
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=Sensata.db");
        }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Reports with MachineId
            modelBuilder.Entity<ProductionMachine>()
            .HasMany(m => m.Reports)
            .WithOne()
            .HasForeignKey(r => r.MachineId);

        // Alerts with MachineId
            modelBuilder.Entity<ProductionMachine>()
            .HasMany(m => m.Alerts)
            .WithOne()
            .HasForeignKey(a => a.MachineId);

          
            modelBuilder.Seed();
        }
    }
}