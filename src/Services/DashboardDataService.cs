using Microsoft.EntityFrameworkCore;
using Sensata.Data;
using Sensata.Models;

namespace Sensata.Services
{
    // ── DashboardDataService ───────────────────────────────────────────────────────
    // Отговорност: Чете от базата → изчислява KPI → сглобява JSON структурата
    // която фронтендът ползва директно (data.json).
    //
    // НЕ знае нищо за AI / Copilot.
    // НЕ знае нищо за HTTP / контролери.
    // Просто: DB → обект → JSON.
    // ──────────────────────────────────────────────────────────────────────────────

    public class DashboardDataService
    {
        private readonly AppDbContext _db;

        public DashboardDataService(AppDbContext db)
        {
            _db = db;
        }

        // ── Главен метод — генерира целия data payload ────────────────────────────
        // Извиква се от API endpoint-а GET /api/data/dashboard
        public async Task<DashboardPayload> BuildAsync(int days = 7)
        {
            var lines = await _db.Lines.ToListAsync();
            var from  = DateTime.Today.AddDays(-days);

            var linePayloads = new List<LinePayload>();

            foreach (var line in lines)
            {
                var reports = await _db.Reports
                    .Where(r => r.LineId == line.Id && r.Date >= from)
                    .OrderBy(r => r.Date).ThenBy(r => r.ShiftName)
                    .ToListAsync();

                var alerts = await _db.Alerts
                    .Where(a => a.LineId == line.Id && a.Timestamp >= from)
                    .OrderBy(a => a.Timestamp)
                    .ToListAsync();

                // Последна смяна с работници
                var lastShift = await _db.Shifts
                    .Where(s => s.LineId == line.Id)
                    .OrderByDescending(s => s.Date).ThenByDescending(s => s.ShiftName)
                    .FirstOrDefaultAsync();

                List<WorkerStatus> workerStatuses = new();
                if (lastShift != null)
                {
                    var shiftWorkers = await _db.ShiftWorkers
                        .Where(sw => sw.ShiftId == lastShift.Id)
                        .ToListAsync();

                    var workerIds = shiftWorkers.Select(sw => sw.WorkerId).ToList();
                    var workers   = await _db.Workers
                        .Where(w => workerIds.Contains(w.Id))
                        .ToListAsync();

                    workerStatuses = shiftWorkers.Select(sw =>
                    {
                        var w = workers.First(x => x.Id == sw.WorkerId);
                        return new WorkerStatus
                        {
                            WorkerId      = w.Id,
                            Name          = w.Name,
                            SkillSet      = w.SkillSet,
                            SkillLevel    = w.SkillLevel,
                            IsPresent     = sw.IsPresent,
                            AbsenceReason = sw.AbsenceReason,
                            TokenScore    = sw.TokenScore,
                            Comment       = sw.OperatorComment,
                        };
                    }).ToList();
                }

                // KPI изчисления (само чрез KpiCalculatorService)
                var kpis    = KpiCalculatorService.CalculateAll(reports);
                var summary = KpiCalculatorService.Summarize(line, reports, alerts);

                // Аларми по тип
                var alertGroups = alerts
                    .GroupBy(a => a.ErrorCode)
                    .Select(g => new AlertSummary
                    {
                        Code      = g.Key,
                        Count     = g.Count(),
                        MaxValue  = g.Max(a => a.RawValue),
                        LastSeen  = g.Max(a => a.Timestamp).ToString("dd.MM.yyyy HH:mm"),
                    })
                    .OrderByDescending(a => a.Count)
                    .ToList();

                // Лоши смени (с коментар != "OK")
                var badShifts = kpis
                    .Where(k => k.OperatorComment != "OK")
                    .Select(k => new BadShiftEntry
                    {
                        Date        = k.Date,
                        ShiftName   = k.ShiftName,
                        Comment     = k.OperatorComment,
                        TokenScore  = k.TokenScore,
                        OEE         = k.OEE,
                        DefectRate  = k.DefectRate,
                        Produced    = k.TotalProduced,
                    })
                    .ToList();

                linePayloads.Add(new LinePayload
                {
                    LineId      = line.Id,
                    LineName    = line.Name,
                    IsAutomated = line.IsAutomated,
                    Summary     = summary,
                    Trend       = kpis,           // всички смени за периода — фронтендът избира последните N
                    Alerts      = alertGroups,
                    BadShifts   = badShifts,
                    LastShift   = lastShift == null ? null : new LastShiftInfo
                    {
                        Date           = lastShift.Date.ToString("dd.MM.yyyy"),
                        ShiftName      = lastShift.ShiftName,
                        SupervisorName = lastShift.SupervisorName,
                        Workers        = workerStatuses,
                        AbsentCount    = workerStatuses.Count(w => !w.IsPresent),
                        PresentCount   = workerStatuses.Count(w => w.IsPresent),
                    },
                });
            }

            return new DashboardPayload
            {
                GeneratedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                PeriodDays  = days,
                PeriodFrom  = from.ToString("dd.MM.yyyy"),
                PeriodTo    = DateTime.Today.ToString("dd.MM.yyyy"),
                Lines       = linePayloads,
            };
        }

        // ── Сценарии — същата структура, но по сценарий ───────────────────────────
        // Scenario 1 → Линия 1 (micro-stops)
        // Scenario 2 → Линия 2 (speed vs quality)
        // Scenario 3 → Линия 3 (predictive maintenance)
        public async Task<List<ScenarioPayload>> BuildScenariosAsync()
        {
            var scenarioMap = new[]
            {
                new { Id = 1, LineId = 1, Label = "Микро-спирания" },
                new { Id = 2, LineId = 2, Label = "Performance vs Качество" },
                new { Id = 3, LineId = 3, Label = "Предиктивна Поддръжка" },
            };

            var result = new List<ScenarioPayload>();
            var from   = DateTime.Today.AddDays(-14);   // 2 седмици за тренд

            foreach (var sc in scenarioMap)
            {
                var line = await _db.Lines.FindAsync(sc.LineId);
                if (line == null) continue;

                var reports = await _db.Reports
                    .Where(r => r.LineId == sc.LineId && r.Date >= from)
                    .OrderBy(r => r.Date).ThenBy(r => r.ShiftName)
                    .ToListAsync();

                var alerts = await _db.Alerts
                    .Where(a => a.LineId == sc.LineId && a.Timestamp >= from)
                    .OrderBy(a => a.Timestamp)
                    .ToListAsync();

                var kpis    = KpiCalculatorService.CalculateAll(reports);
                var summary = KpiCalculatorService.Summarize(line, reports, alerts);

                // Последна смяна + работници
                var lastShift = await _db.Shifts
                    .Where(s => s.LineId == sc.LineId)
                    .OrderByDescending(s => s.Date).ThenByDescending(s => s.ShiftName)
                    .FirstOrDefaultAsync();

                List<WorkerStatus> workers = new();
                if (lastShift != null)
                {
                    var sws = await _db.ShiftWorkers
                        .Where(sw => sw.ShiftId == lastShift.Id)
                        .ToListAsync();
                    var wIds = sws.Select(s => s.WorkerId).ToList();
                    var wList = await _db.Workers.Where(w => wIds.Contains(w.Id)).ToListAsync();

                    workers = sws.Select(sw =>
                    {
                        var w = wList.First(x => x.Id == sw.WorkerId);
                        return new WorkerStatus
                        {
                            WorkerId      = w.Id,
                            Name          = w.Name,
                            SkillSet      = w.SkillSet,
                            SkillLevel    = w.SkillLevel,
                            IsPresent     = sw.IsPresent,
                            AbsenceReason = sw.AbsenceReason,
                            TokenScore    = sw.TokenScore,
                            Comment       = sw.OperatorComment,
                        };
                    }).ToList();
                }

                var alertGroups = alerts
                    .GroupBy(a => a.ErrorCode)
                    .Select(g => new AlertSummary
                    {
                        Code     = g.Key,
                        Count    = g.Count(),
                        MaxValue = g.Max(a => a.RawValue),
                        LastSeen = g.Max(a => a.Timestamp).ToString("dd.MM.yyyy HH:mm"),
                    })
                    .OrderByDescending(a => a.Count)
                    .ToList();

                result.Add(new ScenarioPayload
                {
                    ScenarioId    = sc.Id,
                    ScenarioLabel = sc.Label,
                    LineId        = sc.LineId,
                    LineName      = line.Name,
                    IsAutomated   = line.IsAutomated,
                    Summary       = summary,
                    Trend         = kpis,
                    Alerts        = alertGroups,
                    AbsentWorkers = workers.Where(w => !w.IsPresent).ToList(),
                    AllWorkers    = workers,
                    Targets       = new KpiTargets
                    {
                        Availability    = 95,
                        Performance     = 95,
                        Quality         = 98,
                        OEE             = 85,
                        EnergyIntensity = 1.10,
                    },
                });
            }

            return result;
        }
    }

    // ── DTO класове — само структури, без логика ───────────────────────────────────

    public class DashboardPayload
    {
        public string            GeneratedAt { get; init; } = "";
        public int               PeriodDays  { get; init; }
        public string            PeriodFrom  { get; init; } = "";
        public string            PeriodTo    { get; init; } = "";
        public List<LinePayload> Lines       { get; init; } = new();
    }

    public class LinePayload
    {
        public int              LineId      { get; init; }
        public string           LineName    { get; init; } = "";
        public bool             IsAutomated { get; init; }
        public LineSummaryKpi   Summary     { get; init; } = null!;
        public List<ShiftKpi>   Trend       { get; init; } = new();
        public List<AlertSummary>  Alerts   { get; init; } = new();
        public List<BadShiftEntry> BadShifts{ get; init; } = new();
        public LastShiftInfo?   LastShift   { get; init; }
    }

    public class ScenarioPayload
    {
        public int                 ScenarioId    { get; init; }
        public string              ScenarioLabel { get; init; } = "";
        public int                 LineId        { get; init; }
        public string              LineName      { get; init; } = "";
        public bool                IsAutomated   { get; init; }
        public LineSummaryKpi      Summary       { get; init; } = null!;
        public List<ShiftKpi>      Trend         { get; init; } = new();
        public List<AlertSummary>  Alerts        { get; init; } = new();
        public List<WorkerStatus>  AbsentWorkers { get; init; } = new();
        public List<WorkerStatus>  AllWorkers    { get; init; } = new();
        public KpiTargets          Targets       { get; init; } = null!;
    }

    public class LastShiftInfo
    {
        public string             Date           { get; init; } = "";
        public string             ShiftName      { get; init; } = "";
        public string             SupervisorName { get; init; } = "";
        public int                PresentCount   { get; init; }
        public int                AbsentCount    { get; init; }
        public List<WorkerStatus> Workers        { get; init; } = new();
    }

    public class WorkerStatus
    {
        public int    WorkerId      { get; init; }
        public string Name          { get; init; } = "";
        public string SkillSet      { get; init; } = "";
        public string SkillLevel    { get; init; } = "";
        public bool   IsPresent     { get; init; }
        public string AbsenceReason { get; init; } = "";
        public int    TokenScore    { get; init; }
        public string Comment       { get; init; } = "";
    }

    public class AlertSummary
    {
        public string Code     { get; init; } = "";
        public int    Count    { get; init; }
        public double MaxValue { get; init; }
        public string LastSeen { get; init; } = "";
    }

    public class BadShiftEntry
    {
        public string Date       { get; init; } = "";
        public string ShiftName  { get; init; } = "";
        public string Comment    { get; init; } = "";
        public int    TokenScore { get; init; }
        public double OEE        { get; init; }
        public double DefectRate { get; init; }
        public int    Produced   { get; init; }
    }

    public class KpiTargets
    {
        public double Availability    { get; init; }
        public double Performance     { get; init; }
        public double Quality         { get; init; }
        public double OEE             { get; init; }
        public double EnergyIntensity { get; init; }
    }
}
