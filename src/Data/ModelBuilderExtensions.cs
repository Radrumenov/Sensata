using Microsoft.EntityFrameworkCore;
using Sensata.Models;

namespace Sensata.Data
{
public static class ModelBuilderExtensions
    {
        public static void Seed(this ModelBuilder modelBuilder)
        {
            // 1. Дефинираме 5-те машини
            var machines = new List<ProductionMachine>
            {
                new ProductionMachine { Id = 1, Name = "TPMS Assembly Line", Status = "Green", LastComment = "В норма." },
                new ProductionMachine { Id = 2, Name = "Temp Sensor Press", Status = "Red", LastComment = "Чести спирания." },
                new ProductionMachine { Id = 3, Name = "Brake Switch Line", Status = "Yellow", LastComment = "Проблем с материалите." },
                new ProductionMachine { Id = 4, Name = "Relay Coil Winding", Status = "Green", LastComment = "В норма." },
                new ProductionMachine { Id = 5, Name = "Speed Sensor Calibration", Status = "Green", LastComment = "В норма." }
            };
            modelBuilder.Entity<ProductionMachine>().HasData(machines);

            var reports = new List<ProductionReport>();
            var alerts = new List<MachineAlert>();
            
            // Използваме фиксиран Random seed, за да генерираме едни и същи данни при всяка миграция
            var random = new Random(123); 
            
            // Фиксираме крайната дата спрямо твоята снимка (Март 2026)
            DateTime endDate = new DateTime(2026, 3, 28); 
            int reportId = 1;
            int alertId = 1;

            // 2. Генерираме данни за 30 дни назад за всяка машина
            for (int machineId = 1; machineId <= 5; machineId++)
            {
                for (int daysAgo = 30; daysAgo >= 0; daysAgo--)
                {
                    DateTime currentDate = endDate.AddDays(-daysAgo);
                    bool isBadDay = random.Next(1, 100) <= 15; // 15% шанс денят да е "проблемен" (Червен в SQDC)

                    int produced = isBadDay ? random.Next(500, 800) : random.Next(950, 1100);
                    int defects = isBadDay ? random.Next(50, 120) : random.Next(5, 20);
                    double temp = isBadDay ? Math.Round(random.NextDouble() * 30 + 190, 1) : Math.Round(random.NextDouble() * 10 + 175, 1);
                    double oee = isBadDay ? Math.Round(random.NextDouble() * 20 + 60, 1) : Math.Round(random.NextDouble() * 10 + 88, 1);

                    string comment = "Нормална смяна.";
                    if (isBadDay)
                    {
                        comment = machineId switch
                        {
                            2 => "Машината прегрява. Чест брак заради висока температура.",
                            3 => "Липсваха компоненти от склада, забавяне от 2 часа.",
                            _ => "Нестабилно налягане, много дефектни бройки."
                        };

                        // Добавяме аларма за проблемните дни
                        alerts.Add(new MachineAlert
                        {
                            Id = alertId++,
                            MachineId = machineId,
                            Timestamp = currentDate.AddHours(random.Next(8, 16)), // Случаен час по време на смяната
                            AlertType = defects > 80 ? "Quality" : "Delivery", // Обвързваме със SQDC
                            Severity = "Critical",
                            Message = comment
                        });
                    }

                    reports.Add(new ProductionReport
                    {
                        Id = reportId++,
                        MachineId = machineId,
                        Date = currentDate,
                        TotalProduced = produced,
                        DefectiveUnits = defects,
                        AvgTemperature = temp,
                        AvgPressure = Math.Round(random.NextDouble() * 2 + 4, 1),
                        EnergyConsumed = Math.Round(random.NextDouble() * 50 + 300, 1),
                        OEE = oee,
                        OperatorComment = comment
                    });
                }
            }

            // 3. Записваме масивите в базата
            modelBuilder.Entity<ProductionReport>().HasData(reports);
            modelBuilder.Entity<MachineAlert>().HasData(alerts);
        }
    }
}