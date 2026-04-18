using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sensata.Data;
using Sensata.Models;
using Sensata.Services;

namespace Sensata.Controllers
{
    // ── AnalyticsController ────────────────────────────────────────────────────────
    // Отговорност: САМО оркестрация.
    // 1. Чете данни от DB
    // 2. Подава ги на KpiCalculatorService
    // 3. Подава резултата на CopilotService
    // 4. Връща AI отговора към фронтенда
    //
    // Никаква бизнес логика тук — всички изчисления живеят в KpiCalculatorService.
    // ──────────────────────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext       _db;
        private readonly CopilotService     _copilot;
        private readonly DashboardDataService _dataService;

        public AnalyticsController(
            AppDbContext         db,
            CopilotService       copilot,
            DashboardDataService dataService)
        {
            _db          = db;
            _copilot     = copilot;
            _dataService = dataService;
        }

        // ── GET /api/analytics/analyze?lineId=1&days=7 ────────────────────────────
        // Стъпки: DB → KPI → Copilot → AI JSON
        // Връща: ai_analysis.json (currentState, anomalies, rootCauses, recommendations, riskLevel)
        [HttpGet("analyze")]
        public async Task<IActionResult> Analyze(int lineId, int days = 7)
        {
            var line = await _db.Lines.FindAsync(lineId);
            if (line == null) return NotFound($"Линия {lineId} не съществува.");

            var from = DateTime.Today.AddDays(-days);

            // ── 1. Данни от DB ────────────────────────────────────────────────────
            var reports = await _db.Reports
                .Where(r => r.LineId == lineId && r.Date >= from)
                .OrderBy(r => r.Date).ThenBy(r => r.ShiftName)
                .ToListAsync();

            if (!reports.Any())
                return NotFound($"Няма репорти за линия {lineId} за последните {days} дни.");

            var alerts = await _db.Alerts
                .Where(a => a.LineId == lineId && a.Timestamp >= from)
                .OrderBy(a => a.Timestamp)
                .ToListAsync();

            var lastShift = await _db.Shifts
                .Where(s => s.LineId == lineId)
                .OrderByDescending(s => s.Date).ThenByDescending(s => s.ShiftName)
                .FirstOrDefaultAsync();

            List<ShiftWorker> shiftWorkers = new();
            List<Worker>      workers      = new();
            if (lastShift != null)
            {
                shiftWorkers = await _db.ShiftWorkers
                    .Where(sw => sw.ShiftId == lastShift.Id)
                    .ToListAsync();
                var ids = shiftWorkers.Select(sw => sw.WorkerId).ToList();
                workers = await _db.Workers.Where(w => ids.Contains(w.Id)).ToListAsync();
            }

            // ── 2. KPI изчисление (KpiCalculatorService) ─────────────────────────
            var kpis    = KpiCalculatorService.CalculateAll(reports);
            var summary = KpiCalculatorService.Summarize(line, reports, alerts);

            var absentWorkers = shiftWorkers
                .Where(sw => !sw.IsPresent)
                .Select(sw =>
                {
                    var w = workers.FirstOrDefault(x => x.Id == sw.WorkerId);
                    return w == null ? null : new
                    {
                        w.Name, w.SkillSet, w.SkillLevel,
                        Reason = sw.AbsenceReason,
                    };
                })
                .Where(x => x != null)
                .ToList();

            // ── 3. Контекст за Copilot — структурирани реални данни ───────────────
            var copilotContext = new
            {
                Line = new
                {
                    line.Id,
                    line.Name,
                    Type = line.IsAutomated ? "Автоматизирана" : "Ръчна",
                },
                Period = new
                {
                    From = from.ToString("dd.MM.yyyy"),
                    To   = DateTime.Today.ToString("dd.MM.yyyy"),
                    Days = days,
                },
                Summary = new
                {
                    summary.TotalShifts,
                    summary.AvgProducedPerShift,
                    summary.AvgDefectRate,
                    summary.AvgOEE,
                    summary.AvgTemperature,
                    summary.TotalAlerts,
                    summary.BadShiftsCount,
                    summary.MaxTokenScore,
                },
                Workforce = new
                {
                    TotalAssigned = shiftWorkers.Count,
                    AbsentCount   = absentWorkers.Count,
                    AbsentWorkers = absentWorkers,
                },
                Alerts = alerts
                    .GroupBy(a => a.ErrorCode)
                    .Select(g => new
                    {
                        Code      = g.Key,
                        Count     = g.Count(),
                        MaxValue  = g.Max(x => x.RawValue),
                    }),
                BadShifts = kpis
                    .Where(k => k.OperatorComment != "OK")
                    .Select(k => new
                    {
                        k.Date, k.ShiftName,
                        k.OperatorComment, k.TokenScore,
                        k.TotalProduced,   k.DefectRate, k.OEE,
                    }),
                // Последните 10 смени като тренд
                Trend = kpis.TakeLast(10).Select(k => new
                {
                    k.Date, k.ShiftName,
                    k.OEE, k.DefectRate,
                    k.Performance, k.Availability, k.Quality,
                    k.EnergyIntensity, k.TokenScore,
                }),
            };

            // ── 4. Copilot анализ → AI JSON ───────────────────────────────────────
            var aiResult = await _copilot.AnalyzeAsync(copilotContext);
            return Ok(aiResult);
        }

        // ── GET /api/analytics/workers/suggest?lineId=1 ───────────────────────────
        // Стъпки: DB → намери отсъстващи → намери кандидати → Copilot → AI JSON
        // Връща: ai_workers.json (situationSummary, suggestions, riskAfterReplacement, managerNote)
        [HttpGet("workers/suggest")]
        public async Task<IActionResult> SuggestWorkers(int lineId)
        {
            var line = await _db.Lines.FindAsync(lineId);
            if (line == null) return NotFound($"Линия {lineId} не съществува.");

            var lastShift = await _db.Shifts
                .Where(s => s.LineId == lineId)
                .OrderByDescending(s => s.Date).ThenByDescending(s => s.ShiftName)
                .FirstOrDefaultAsync();

            if (lastShift == null)
                return NotFound("Няма намерена смяна за тази линия.");

            var shiftWorkers = await _db.ShiftWorkers
                .Where(sw => sw.ShiftId == lastShift.Id)
                .ToListAsync();

            var absentIds = shiftWorkers
                .Where(sw => !sw.IsPresent)
                .Select(sw => sw.WorkerId)
                .ToList();

            if (!absentIds.Any())
                return Ok(new { message = "Всички работници присъстват. Не са нужни заместници." });

            var absentWorkers = await _db.Workers
                .Where(w => absentIds.Contains(w.Id))
                .ToListAsync();

            var neededSkills = absentWorkers
                .SelectMany(w => w.SkillSet.Split(','))
                .Distinct()
                .ToList();

            // Кандидати: работници извън отсъстващите, с нужните умения
            var allOtherWorkers = await _db.Workers
                .Where(w => !absentIds.Contains(w.Id))
                .ToListAsync();

            var candidates = allOtherWorkers
                .Select(w => new
                {
                    w.Id, w.Name, w.SkillSet, w.SkillLevel,
                    MatchedSkills = neededSkills
                        .Where(s => w.SkillSet.Contains(s))
                        .ToList(),
                })
                .Where(w => w.MatchedSkills.Any())
                .OrderByDescending(w => w.MatchedSkills.Count)
                .ThenBy(w => w.SkillLevel == "lead" ? 0 : w.SkillLevel == "senior" ? 1 : 2)
                .ToList();

            var copilotContext = new
            {
                LineId       = lineId,
                LineName     = line.Name,
                LineType     = line.IsAutomated ? "Автоматизирана" : "Ръчна",
                ShiftDate    = lastShift.Date.ToString("dd.MM.yyyy"),
                ShiftName    = lastShift.ShiftName,
                Absent       = absentWorkers.Select(w => new { w.Name, w.SkillSet, w.SkillLevel }),
                NeededSkills = neededSkills,
                Candidates   = candidates,
            };

            var aiResult = await _copilot.SuggestWorkersAsync(copilotContext);
            return Ok(aiResult);
        }

        // ── GET /api/analytics/forecast?lineId=1 ──────────────────────────────────
        // Стъпки: DB (30 дни история) → KPI тренд → Copilot → AI JSON
        // Връща: ai_forecast.json (trendAnalysis, forecast[7 дни], mainRisks, preventiveActions)
        [HttpGet("forecast")]
        public async Task<IActionResult> Forecast(int lineId)
        {
            var line = await _db.Lines.FindAsync(lineId);
            if (line == null) return NotFound($"Линия {lineId} не съществува.");

            var from = DateTime.Today.AddDays(-30);

            var reports = await _db.Reports
                .Where(r => r.LineId == lineId && r.Date >= from)
                .OrderBy(r => r.Date).ThenBy(r => r.ShiftName)
                .ToListAsync();

            if (!reports.Any())
                return NotFound($"Няма достатъчно история за прогноза.");

            var alerts = await _db.Alerts
                .Where(a => a.LineId == lineId && a.Timestamp >= from)
                .OrderBy(a => a.Timestamp)
                .ToListAsync();

            // KPI за целия период — Copilot вижда тренда
            var kpis = KpiCalculatorService.CalculateAll(reports);

            var copilotContext = new
            {
                Line = new
                {
                    line.Id,
                    line.Name,
                    Type = line.IsAutomated ? "Автоматизирана" : "Ръчна",
                },
                ForecastDays = 7,
                History = kpis.Select(k => new
                {
                    k.Date, k.ShiftName,
                    k.TotalProduced, k.DefectiveUnits,
                    k.OEE, k.DefectRate,
                    k.AvgTemperature, k.EnergyIntensity,
                    k.TokenScore, k.OperatorComment,
                }),
                Alerts = alerts.Select(a => new
                {
                    a.ErrorCode,
                    Date  = a.Timestamp.ToString("dd.MM.yyyy"),
                    a.RawValue,
                }),
            };

            var aiResult = await _copilot.ForecastAsync(copilotContext);
            return Ok(aiResult);
        }
    }
}
