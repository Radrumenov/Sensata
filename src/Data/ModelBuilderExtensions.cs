using Microsoft.EntityFrameworkCore;
using Sensata.Models;

namespace Sensata.Data
{
    public static class ModelBuilderExtensions
    {
        // Ето го речникът с грешките (15 броя). AI-ът ще трябва да ги свързва със суровите данни!
        public static readonly Dictionary<string, string> ErrorCodesDictionary = new()
        {
            { "ERR-TMP-HI", "Критично висока температура" },
            { "ERR-TMP-LO", "Температурата е под минимума" },
            { "ERR-PRS-HI", "Опасно високо налягане в системата" },
            { "ERR-PRS-LO", "Загуба на налягане (възможен теч)" },
            { "ERR-VIB-01", "Анормални вибрации в главния вал" },
            { "ERR-MTR-OL", "Претоварване на двигателя" },
            { "ERR-SNS-FL", "Отказ на оптичния сензор" },
            { "ERR-SPD-DW", "Спад в оборотите на машината" },
            { "ERR-PWR-FL", "Флуктуация в захранването (токов удар)" },
            { "ERR-OIL-LK", "Регистриран теч на хидравлична течност" },
            { "ERR-PNE-FL", "Отказ на пневматичния цилиндър" },
            { "ERR-CYC-TM", "Превишено време за цикъл на асемблиране" },
            { "ERR-CAL-ER", "Грешка в софтуерната калибрация" },
            { "ERR-MAT-JM", "Засядане на материал по лентата" },
            { "ERR-EXH-BL", "Блокирана изпускателна система" }
        };

        public static void Seed(this ModelBuilder modelBuilder)
        {
            var machines = new List<ProductionMachine>
            {
                new ProductionMachine { Id = 1, Name = "TPMS Assembly Line" },
                new ProductionMachine { Id = 2, Name = "Temp Sensor Press" },
                new ProductionMachine { Id = 3, Name = "Brake Switch Line" },
                new ProductionMachine { Id = 4, Name = "Relay Coil Winding" },
                new ProductionMachine { Id = 5, Name = "Speed Sensor Calibration" }
            };
            modelBuilder.Entity<ProductionMachine>().HasData(machines);

            // 15 различни коментара от оператори за "лошите" дни
            string[] operatorComments = new string[]
            {
                "Миризма на изгоряло от панела.",
                "Материал партида #8445 е с лошо качество.",
                "Странен шум от компресора.",
                "Лентата прекъсва на всеки 10 минути.",
                "Проблем с пневматиката - слабо налягане.",
                "Сензорът за позиция дава фалшиви сигнали.",
                "Силни вибрации при стартиране на цикъла.",
                "Изтичане на масло, викнахме поддръжката.",
                "Охладителната течност завря.",
                "Скъсан ремък на подаващия механизъм.",
                "Машината се рестартира сама два пъти.",
                "Забавяне при софтуерния цикъл, дисплеят забива.",
                "Калибрацията избяга след обедната почивка.",
                "Счупен пин на пресоващата матрица.",
                "Прекалено много стружки блокират лазера."
            };

            var reports = new List<ProductionReport>();
            var alerts = new List<MachineAlert>();
            var random = new Random(123); 
            DateTime endDate = new DateTime(2026, 4, 30); 
            int reportId = 1;
            int alertId = 1;

            // Взимаме списък с всички кодове за грешки, за да избираме от тях
            var errorCodesList = ErrorCodesDictionary.Keys.ToList();

            for (int machineId = 1; machineId <= 5; machineId++)
            {
                for (int daysAgo = 90; daysAgo >= 0; daysAgo--)
                {
                    DateTime currentDate = endDate.AddDays(-daysAgo);
                    bool isBadDay = random.Next(1, 100) <= 15; // 15% шанс за проблем

                    int produced = isBadDay ? random.Next(500, 800) : random.Next(950, 1100);
                    int defects = isBadDay ? random.Next(50, 120) : random.Next(5, 20);
                    double temp = isBadDay ? Math.Round(random.NextDouble() * 30 + 190, 1) : Math.Round(random.NextDouble() * 10 + 175, 1);
                    double pressure = Math.Round(random.NextDouble() * 2 + 4, 1);
                    double energy = Math.Round(random.NextDouble() * 50 + 300, 1);

                    string comment = "OK";
                    if (isBadDay)
                    {
                        // Избираме случаен коментар от масива
                        comment = operatorComments[random.Next(operatorComments.Length)];

                        // Избираме случаен код за грешка от речника
                        string randomErrorCode = errorCodesList[random.Next(errorCodesList.Count)];

                        alerts.Add(new MachineAlert
                        {
                            Id = alertId++,
                            MachineId = machineId,
                            Timestamp = currentDate.AddHours(random.Next(8, 16)),
                            ErrorCode = randomErrorCode,
                            RawValue = randomErrorCode.Contains("TMP") ? temp : pressure // Ако е температурна грешка записваме градусите, иначе налягането
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
                        AvgPressure = pressure,
                        EnergyConsumed = energy,
                        OperatorComment = comment
                    });
                }
            }

            modelBuilder.Entity<ProductionReport>().HasData(reports);
            modelBuilder.Entity<MachineAlert>().HasData(alerts);
        }
    }
}