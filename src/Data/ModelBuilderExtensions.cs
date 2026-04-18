using Microsoft.EntityFrameworkCore;
using Sensata.Models;

namespace Sensata.Data
{
    public static class ModelBuilderExtensions
    {
        // ── ERROR CODE РЕЧНИК ──────────────────────────────────────────────────────
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

        // ── TOKEN SCORING РЕЧНИК ───────────────────────────────────────────────────
        public static readonly Dictionary<string, int> TokenScoreDictionary = new()
        {
            { "странен шум",              1  },
            { "вибрации",                 2  },
            { "изгоряло",                 3  },
            { "лошо качество",            3  },
            { "прекъсва",                 3  },
            { "слабо налягане",           4  },
            { "фалшиви сигнали",          4  },
            { "изтичане на масло",        5  },
            { "охладителната течност",    6  },
            { "скъсан ремък",             5  },
            { "рестартира",               5  },
            { "дисплеят забива",          3  },
            { "забавяне",                 2  },
            { "калибрацията избяга",      4  },
            { "счупен пин",               4  },
            { "стружки блокират",         3  },
        };

        // ── ДЕТЕРМИНИСТИЧЕН ID ЗА СМЯНА ───────────────────────────────────────────
        // Формула: уникален ID базиран на линия + ден от периода + индекс на смяната.
        // При повторен seed същата смяна получава СЪЩОТО ID → няма дублирани записи.
        //
        //   lineId      : 1–3      → множител 100 000
        //   dayIndex    : 0–90     → множител 10
        //   shiftIndex  : 0–2      → единици
        //
        // Максимална стойност: 3×100000 + 90×10 + 2 = 300 902  (безопасно за int)
        private static int ShiftId(int lineId, int dayIndex, int shiftIndex)
            => lineId * 100_000 + dayIndex * 10 + shiftIndex;

        // ── ДЕТЕРМИНИСТИЧЕН ID ЗА SHIFT WORKER ────────────────────────────────────
        // shiftId × 10 + позиция на работника в смяната (0–4)
        // Максимум: 300 902 × 10 + 4 = 3 009 024  (безопасно за int)
        private static int ShiftWorkerId(int shiftId, int workerPos)
            => shiftId * 10 + workerPos;

        // ── ДЕТЕРМИНИСТИЧЕН ID ЗА REPORT ──────────────────────────────────────────
        // Идентичен с ShiftId — всяка смяна генерира точно 1 report
        private static int ReportId(int lineId, int dayIndex, int shiftIndex)
            => lineId * 100_000 + dayIndex * 10 + shiftIndex;

        // ── SEED ───────────────────────────────────────────────────────────────────
        public static void Seed(this ModelBuilder modelBuilder)
        {
            // ── 1. ЛИНИИ ───────────────────────────────────────────────────────────
            var lines = new List<ProductionLine>
            {
                new ProductionLine { Id = 1, Name = "TPMS Assembly Line",       IsAutomated = false },
                new ProductionLine { Id = 2, Name = "Brake Switch Line",         IsAutomated = false },
                new ProductionLine { Id = 3, Name = "Speed Sensor Calibration", IsAutomated = true  },
            };
            modelBuilder.Entity<ProductionLine>().HasData(lines);

            // ── 2. UNIQUE INDEX НА СМЕНИТЕ ─────────────────────────────────────────
            // (LineId + Date + ShiftName) трябва да е уникална комбинация.
            // Това гарантира, че дори при повторен migration/seed не се появяват
            // дублирани смени с различни ID-та но еднакво съдържание.
            modelBuilder.Entity<Shift>()
                .HasIndex(s => new { s.LineId, s.Date, s.ShiftName })
                .IsUnique();

            // ── 3. РАБОТНИЦИ СЪС SKILL SET ─────────────────────────────────────────
            var workers = new List<Worker>
            {
                // Линия 1 (Ръчна) — 11 човека
                new Worker { Id = 1,  Name = "Иван Петров",      SkillSet = "assembly,soldering",          SkillLevel = "lead"   },
                new Worker { Id = 2,  Name = "Мария Иванова",    SkillSet = "assembly,quality-control",    SkillLevel = "senior" },
                new Worker { Id = 3,  Name = "Георги Стоев",     SkillSet = "soldering,assembly",          SkillLevel = "senior" },
                new Worker { Id = 4,  Name = "Елена Димова",     SkillSet = "quality-control",             SkillLevel = "junior" },
                new Worker { Id = 5,  Name = "Николай Русев",    SkillSet = "logistics,assembly",          SkillLevel = "junior" },
                new Worker { Id = 6,  Name = "Петя Стефанова",   SkillSet = "assembly",                    SkillLevel = "junior" },
                new Worker { Id = 7,  Name = "Стоян Николов",    SkillSet = "pressing,pneumatics",         SkillLevel = "lead"   },
                new Worker { Id = 8,  Name = "Надя Тодорова",    SkillSet = "pressing,quality-control",    SkillLevel = "senior" },
                new Worker { Id = 9,  Name = "Борис Атанасов",   SkillSet = "pneumatics,pressing",         SkillLevel = "senior" },
                new Worker { Id = 10, Name = "Силвия Маринова",  SkillSet = "quality-control,logistics",   SkillLevel = "senior" },
                new Worker { Id = 11, Name = "Калин Ангелов",    SkillSet = "welding,pressing",            SkillLevel = "junior" },

                // Линия 2 (Ръчна) — 11 човека
                new Worker { Id = 12, Name = "Десислава Ченова", SkillSet = "logistics",                   SkillLevel = "junior" },
                new Worker { Id = 13, Name = "Красимир Генов",   SkillSet = "pneumatics,machine-op",       SkillLevel = "lead"   },
                new Worker { Id = 14, Name = "Весела Попова",    SkillSet = "pressing,quality-control",    SkillLevel = "senior" },
                new Worker { Id = 15, Name = "Тодор Василев",    SkillSet = "machine-op,maintenance",      SkillLevel = "senior" },
                new Worker { Id = 16, Name = "Анелия Кирова",    SkillSet = "pressing",                    SkillLevel = "junior" },
                new Worker { Id = 17, Name = "Румен Захариев",   SkillSet = "maintenance,machine-op",      SkillLevel = "junior" },
                new Worker { Id = 18, Name = "Димитър Колев",    SkillSet = "assembly,pressing,logistics", SkillLevel = "senior" },
                new Worker { Id = 19, Name = "Валентина Янева",  SkillSet = "quality-control,assembly",    SkillLevel = "senior" },
                new Worker { Id = 20, Name = "Огнян Петков",     SkillSet = "maintenance,pneumatics",      SkillLevel = "senior" },
                new Worker { Id = 21, Name = "Цветелина Бонева", SkillSet = "soldering,welding",           SkillLevel = "junior" },
                new Worker { Id = 22, Name = "Мартин Илиев",     SkillSet = "assembly,quality-control",    SkillLevel = "junior" },

                // Линия 3 (Автоматизирана) — 13 човека
                new Worker { Id = 23, Name = "Светослав Пеев",   SkillSet = "automation,calibration",      SkillLevel = "lead"   },
                new Worker { Id = 24, Name = "Иванка Добрева",   SkillSet = "automation,machine-op",       SkillLevel = "senior" },
                new Worker { Id = 25, Name = "Росен Димитров",   SkillSet = "automation,maintenance",      SkillLevel = "senior" },
                new Worker { Id = 26, Name = "Теодора Костова",  SkillSet = "automation,quality-control",  SkillLevel = "junior" },
                new Worker { Id = 27, Name = "Владимир Илиев",   SkillSet = "automation,calibration",      SkillLevel = "senior" },
                new Worker { Id = 28, Name = "Гергана Митева",   SkillSet = "automation,logistics",        SkillLevel = "junior" },
                new Worker { Id = 29, Name = "Емил Тодоров",     SkillSet = "automation,machine-op",       SkillLevel = "senior" },
                new Worker { Id = 30, Name = "Радослав Борисов", SkillSet = "automation,maintenance",      SkillLevel = "senior" },
                new Worker { Id = 31, Name = "Зорница Иванова",  SkillSet = "automation,machine-op",       SkillLevel = "junior" },
                new Worker { Id = 32, Name = "Михаил Георгиев",  SkillSet = "automation,calibration",      SkillLevel = "lead"   },
                new Worker { Id = 33, Name = "Надежда Колева",   SkillSet = "automation,quality-control",  SkillLevel = "senior" },
                new Worker { Id = 34, Name = "Илиян Спасов",     SkillSet = "automation,maintenance",      SkillLevel = "senior" },
                new Worker { Id = 35, Name = "Даниела Михова",   SkillSet = "automation,machine-op",       SkillLevel = "junior" }
            };
            modelBuilder.Entity<Worker>().HasData(workers);

            // ── 4. SEED НА ОТЧЕТИ, СМЕНИ И АЛАРМИ ──────────────────────────────────
            var reports      = new List<ProductionReport>();
            var alerts       = new List<LineAlert>();
            var shifts       = new List<Shift>();
            var shiftWorkers = new List<ShiftWorker>();

            // Фиксиран seed → детерминистични резултати при всяко изпълнение
            var random = new Random(123);

            var badCommentsMap = new Dictionary<string, string>
            {
                { "Миризма на изгоряло от панела.",             "ERR-MTR-OL" },
                { "Охладителната течност завря.",               "ERR-TMP-HI" },
                { "Силни вибрации при стартиране на цикъла.",   "ERR-VIB-01" },
                { "Изтичане на масло, викнахме поддръжката.",   "ERR-OIL-LK" },
                { "Скъсан ремък на подаващия механизъм.",       "ERR-SPD-DW" },
                { "Машината се рестартира сама два пъти.",      "ERR-PWR-FL" },
                { "Проблем с пневматиката - слабо налягане.",   "ERR-PNE-FL" },
                { "Сензорът за позиция дава фалшиви сигнали.",  "ERR-SNS-FL" },
            };

            var microStopComments = new List<string>
            {
                "Странен шум от компресора.",
                "Лентата прекъсва на всеки 10 минути.",
                "Забавяне при софтуерния цикъл, дисплеят забива.",
                "Калибрацията избяга след обедната почивка.",
                "Счупен пин на пресоващата матрица.",
                "Прекалено много стружки блокират лазера.",
                "Материал партида #8445 е с лошо качество.",
            };

            var badCommentsList = badCommentsMap.Keys.ToList();
            string[] supervisors = { "Петър Димов", "Анна Христова", "Симеон Велев" };

            // Пулове от работници по линии
            var lineWorkers = new Dictionary<int, List<int>>
            {
                { 1, new List<int> { 1,  2,  3,  4,  5,  6,  7,  8,  9,  10, 11 } },
                { 2, new List<int> { 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22 } },
                { 3, new List<int> { 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35 } },
            };

            // alertId стартира от 1 и се инкрементира — само alerts нямат нужда
            // от детерминистична формула, тъй като се добавят строго по ред.
            int alertId = 1;

            DateTime endDate = new DateTime(2026, 4, 18);

            // ── ГЛАВЕН ЦИКЪЛ ─────────────────────────────────────────────────────────
            // ВАЖНО: shiftCounters НЕ се нулира между линиите — всяка линия
            // поддържа собствена позиция в пула, така че ротацията е коректна.
            var shiftCounters = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 } };

            for (int lineId = 1; lineId <= 3; lineId++)
            {
                for (int daysAgo = 90; daysAgo >= 0; daysAgo--)
                {
                    // dayIndex = 0 (най-стар ден) … 90 (днес)
                    // Използва се в детерминистичните ID формули.
                    int dayIndex = 90 - daysAgo;

                    DateTime currentDate = endDate.AddDays(-daysAgo);
                    DayOfWeek dayOfWeek  = currentDate.DayOfWeek;

                    // Смени за деня — зависят от линията и деня от седмицата
                    var shiftsForToday = new List<string>();

                    if (lineId == 1 || lineId == 2)
                    {
                        // Ръчни линии: пон–пет 2 смени, събота 1 смяна, неделя — почивка
                        if (dayOfWeek >= DayOfWeek.Monday && dayOfWeek <= DayOfWeek.Friday)
                        {
                            shiftsForToday.Add("Смяна 1 (06:00-14:00)");
                            shiftsForToday.Add("Смяна 2 (14:00-22:00)");
                        }
                        else if (dayOfWeek == DayOfWeek.Saturday)
                        {
                            shiftsForToday.Add("Смяна 1 (06:00-14:00)");
                        }
                        // Неделя → без смени
                    }
                    else // lineId == 3 — автоматизирана, работи 24/7
                    {
                        shiftsForToday.Add("Смяна 1 (06:00-14:00)");
                        shiftsForToday.Add("Смяна 2 (14:00-22:00)");
                        shiftsForToday.Add("Смяна 3 (22:00-06:00)");
                    }

                    if (shiftsForToday.Count == 0) continue;

                    for (int shiftIndex = 0; shiftIndex < shiftsForToday.Count; shiftIndex++)
                    {
                        var shiftName = shiftsForToday[shiftIndex];

                        // ── ДЕТЕРМИНИСТИЧНИ ID-та ──────────────────────────────────
                        // Тези стойности са ЕДНАКВИ при всяко изпълнение на seed-а
                        // за същата (lineId, dayIndex, shiftIndex) комбинация.
                        int currentShiftId  = ShiftId(lineId, dayIndex, shiftIndex);
                        int currentReportId = ReportId(lineId, dayIndex, shiftIndex);

                        // ── СЦЕНАРИЙ ──────────────────────────────────────────────
                        int  shiftRoll    = random.Next(1, 101);
                        bool isBreakdown  = shiftRoll <= 8;
                        bool isMicroStop  = shiftRoll > 8  && shiftRoll <= 15;
                        bool isOverspeed  = shiftRoll > 15 && shiftRoll <= 20;
                        bool isBadShift   = isBreakdown || isMicroStop || isOverspeed;

                        // ── ПРОИЗВОДСТВО ──────────────────────────────────────────
                        int produced, defects;
                        if (isBreakdown)
                        {
                            produced = random.Next(60, 180);
                            defects  = random.Next(15, 45);
                        }
                        else if (isMicroStop)
                        {
                            produced = random.Next(140, 240);
                            defects  = random.Next(8, 25);
                        }
                        else if (isOverspeed)
                        {
                            produced = random.Next(370, 420);
                            defects  = random.Next(30, 65);
                        }
                        else
                        {
                            produced = random.Next(300, 375);
                            defects  = random.Next(1, 6);
                        }

                        double temp     = (isBreakdown && random.Next(2) == 0)
                            ? Math.Round(random.NextDouble() * 30 + 190, 1)
                            : Math.Round(random.NextDouble() * 10 + 175, 1);
                        double pressure = Math.Round(random.NextDouble() * 2 + 4, 1);
                        double energy   = isOverspeed
                            ? Math.Round(random.NextDouble() * 10 + 110, 1)
                            : Math.Round(random.NextDouble() * 15 + 90, 1);

                        // ── КОМЕНТАР И TOKEN SCORE ─────────────────────────────────
                        string comment    = "OK";
                        int    tokenScore = 0;

                        if (isBreakdown)
                        {
                            comment = badCommentsList[random.Next(badCommentsList.Count)];
                            string errorCodeForComment = badCommentsMap[comment];

                            foreach (var kv in TokenScoreDictionary)
                                if (comment.ToLower().Contains(kv.Key.ToLower()))
                                    tokenScore += kv.Value;

                            int alertHour = shiftName.Contains("06:00-14:00") ? 10 :
                                            shiftName.Contains("14:00-22:00") ? 18 : 2;

                            alerts.Add(new LineAlert
                            {
                                Id        = alertId++,
                                LineId    = lineId,
                                Timestamp = currentDate.AddHours(alertHour),
                                ErrorCode = errorCodeForComment,
                                RawValue  = errorCodeForComment.Contains("TMP") ? temp : pressure
                            });
                        }
                        else if (isMicroStop)
                        {
                            comment = microStopComments[random.Next(microStopComments.Count)];

                            foreach (var kv in TokenScoreDictionary)
                                if (comment.ToLower().Contains(kv.Key.ToLower()))
                                    tokenScore += kv.Value;
                        }
                        else if (isOverspeed)
                        {
                            comment    = "Линията е ускорена за наваксване. Качеството пада.";
                            tokenScore = 25;
                        }

                        string supervisor = shiftName.Contains("06:00") ? supervisors[0] :
                                            shiftName.Contains("14:00") ? supervisors[1] : supervisors[2];

                        // ── ЗАПИС НА СМЯНАТА ──────────────────────────────────────
                        shifts.Add(new Shift
                        {
                            Id             = currentShiftId,
                            LineId         = lineId,
                            Date           = currentDate,
                            ShiftName      = shiftName,
                            SupervisorName = supervisor
                        });

                        // ── РАБОТНИЦИ В СМЯНАТА (Round-Robin) ─────────────────────
                        // Линия 3 (автоматизирана) → 4 работника; ръчни → 5 работника
                        int workersPerShift = lineId == 3 ? 4 : 5;
                        var pool            = lineWorkers[lineId];
                        var currentShiftWorkers = new List<int>();

                        for (int i = 0; i < workersPerShift; i++)
                        {
                            int wIdx = (shiftCounters[lineId] + i) % pool.Count;
                            currentShiftWorkers.Add(pool[wIdx]);
                        }

                        // Преместваме брояча → следващата смяна взима следващите хора
                        shiftCounters[lineId] += workersPerShift;

                        for (int workerPos = 0; workerPos < currentShiftWorkers.Count; workerPos++)
                        {
                            int workerId = currentShiftWorkers[workerPos];

                            bool absenceChance = isBadShift
                                ? random.Next(1, 100) <= 15
                                : random.Next(1, 100) <= 5;
                            bool isPresent   = !absenceChance;
                            string absenceReason = "";

                            string workerComment  = "OK";
                            int    workerTokenScore = 0;

                            if (!isPresent)
                            {
                                absenceReason   = random.Next(2) == 0 ? "болен" : "отпуска";
                                workerComment   = $"Липса на оператор ({absenceReason}) - нужни са заместници!";
                                workerTokenScore = 50;
                            }
                            else if (isBadShift)
                            {
                                workerComment   = comment;
                                workerTokenScore = tokenScore;
                            }

                            shiftWorkers.Add(new ShiftWorker
                            {
                                // Детерминистичен ID: shiftId × 10 + позиция на работника
                                Id              = ShiftWorkerId(currentShiftId, workerPos),
                                ShiftId         = currentShiftId,
                                WorkerId        = workerId,
                                IsPresent       = isPresent,
                                AbsenceReason   = absenceReason,
                                OperatorComment = workerComment,
                                TokenScore      = workerTokenScore
                            });
                        }

                        // ── ПРОИЗВОДСТВЕН ОТЧЕТ ───────────────────────────────────
                        reports.Add(new ProductionReport
                        {
                            Id                = currentReportId,
                            LineId            = lineId,
                            Date              = currentDate,
                            ShiftName         = shiftName,
                            TotalProduced     = produced,
                            DefectiveUnits    = defects,
                            AvgTemperature    = temp,
                            AvgPressure       = pressure,
                            EnergyConsumed    = energy,
                            OperatorComment   = comment,
                            CommentTokenScore = tokenScore
                        });

                    } // foreach shiftIndex
                } // foreach daysAgo
            } // foreach lineId

            // ── PREDICTIVE MAINTENANCE СЦЕНАРИЙ (Линия 3) ───────────────────────────
            // Добавяме нарастващи вибрации + температурни аларми в последните 10 дни,
            // за да е видим предиктивният тренд в AI анализа.
            var predictiveBase = new DateTime(2026, 4, 8);
            var predictiveAlerts = new[]
            {
                new { DaysAfter = 0, Hour = 2,  Code = "ERR-VIB-01", Val = 4.8 },
                new { DaysAfter = 2, Hour = 14, Code = "ERR-TMP-HI", Val = 193.2 },
                new { DaysAfter = 3, Hour = 2,  Code = "ERR-VIB-01", Val = 5.1 },
                new { DaysAfter = 6, Hour = 10, Code = "ERR-TMP-HI", Val = 197.4 },
                new { DaysAfter = 8, Hour = 2,  Code = "ERR-VIB-01", Val = 5.6 },
            };

            foreach (var pa in predictiveAlerts)
            {
                alerts.Add(new LineAlert
                {
                    Id        = alertId++,
                    LineId    = 3,
                    Timestamp = predictiveBase.AddDays(pa.DaysAfter).AddHours(pa.Hour),
                    ErrorCode = pa.Code,
                    RawValue  = pa.Val
                });
            }

            // ── ЗАПИС В БАЗАТА ───────────────────────────────────────────────────────
            modelBuilder.Entity<ProductionReport>().HasData(reports);
            modelBuilder.Entity<LineAlert>().HasData(alerts);
            modelBuilder.Entity<Shift>().HasData(shifts);
            modelBuilder.Entity<ShiftWorker>().HasData(shiftWorkers);
        }
    }
}