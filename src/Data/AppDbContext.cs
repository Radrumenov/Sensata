using Microsoft.EntityFrameworkCore;
using Sensata.Models;

namespace Sensata.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ── Таблици ───────────────────────────────────────────────────────────────
        public DbSet<ProductionLine>   Lines        { get; set; }
        public DbSet<ProductionReport> Reports      { get; set; }
        public DbSet<LineAlert>        Alerts       { get; set; }
        public DbSet<Worker>           Workers      { get; set; }
        public DbSet<Shift>            Shifts       { get; set; }
        public DbSet<ShiftWorker>      ShiftWorkers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=Sensata.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Reports → ProductionLine
            modelBuilder.Entity<ProductionLine>()
                .HasMany(l => l.Reports)
                .WithOne()
                .HasForeignKey(r => r.LineId);

            // Alerts → ProductionLine
            modelBuilder.Entity<ProductionLine>()
                .HasMany(l => l.Alerts)
                .WithOne()
                .HasForeignKey(a => a.LineId);

            // Shifts → ProductionLine
            modelBuilder.Entity<ProductionLine>()
                .HasMany(l => l.Shifts)
                .WithOne()
                .HasForeignKey(s => s.LineId);

            // ShiftWorkers → Shift (junction таблица)
            modelBuilder.Entity<Shift>()
                .HasMany(s => s.Workers)
                .WithOne()
                .HasForeignKey(sw => sw.ShiftId);

            // ShiftWorkers → Worker
            modelBuilder.Entity<Worker>()
                .HasMany(w => w.ShiftAssignments)
                .WithOne()
                .HasForeignKey(sw => sw.WorkerId);

            modelBuilder.Seed();
        }
    }
}