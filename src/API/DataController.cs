using Microsoft.AspNetCore.Mvc;
using Sensata.Services;

namespace Sensata.Controllers
{
    // ── DataController ─────────────────────────────────────────────────────────────
    // Отговорност: Сервира двата JSON файла към фронтенда.
    //
    //  GET /api/data/dashboard          → data.json        (реални KPI данни)
    //  GET /api/data/scenarios          → scenarios.json   (3-те сценария с тренд)
    //  GET /api/data/ai/analyze/{id}    → ai_analysis.json (Copilot анализ)
    //  GET /api/data/ai/workers/{id}    → ai_workers.json  (Copilot препоръка)
    //  GET /api/data/ai/forecast/{id}   → ai_forecast.json (Copilot прогноза)
    //
    // Фронтендът вика само тези endpoints.
    // Колегата не знае нищо за DB или AI — получава готов JSON.
    // ──────────────────────────────────────────────────────────────────────────────

    [Route("api/data")]
    [ApiController]
    public class DataController : ControllerBase
    {
        private readonly DashboardDataService _dataService;
        private readonly CopilotService       _copilot;

        public DataController(DashboardDataService dataService, CopilotService copilot)
        {
            _dataService = dataService;
            _copilot     = copilot;
        }

        // ── GET /api/data/dashboard?days=7 ────────────────────────────────────────
        // Връща data.json — реални KPI данни за всички линии.
        // Фронтендът ползва това вместо mockData.
        //
        // JSON структура:
        // {
        //   "generatedAt": "18.04.2026 14:23:01",
        //   "periodDays": 7,
        //   "lines": [
        //     {
        //       "lineId": 1,
        //       "lineName": "TPMS Assembly Line",
        //       "summary": { "avgOEE": 78.4, "avgDefectRate": 5.2, ... },
        //       "trend": [ { "date": "12.04", "oee": 76.1, ... }, ... ],
        //       "alerts": [ { "code": "ERR-VIB-01", "count": 3 }, ... ],
        //       "badShifts": [ ... ],
        //       "lastShift": {
        //         "workers": [ { "name": "Иван", "isPresent": true, ... } ]
        //       }
        //     },
        //     ...
        //   ]
        // }
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] int days = 7)
        {
            var payload = await _dataService.BuildAsync(days);
            return Ok(payload);
        }

        // ── GET /api/data/scenarios ───────────────────────────────────────────────
        // Връща scenarios.json — 3 сценария с тренд данни.
        // Директна замяна на const mockData в app.js.
        //
        // JSON структура:
        // [
        //   {
        //     "scenarioId": 1,
        //     "scenarioLabel": "Микро-спирания",
        //     "lineId": 1,
        //     "trend": [ { "date": "...", "oee": 78, ... } ],
        //     "alerts": [ ... ],
        //     "absentWorkers": [ ... ],
        //     "targets": { "oee": 85, "quality": 98, ... }
        //   },
        //   ...
        // ]
        [HttpGet("scenarios")]
        public async Task<IActionResult> GetScenarios()
        {
            var scenarios = await _dataService.BuildScenariosAsync();
            return Ok(scenarios);
        }

        // ── GET /api/data/ai/analyze/{lineId}?days=7 ──────────────────────────────
        // Стъпка 1: Вземи реалните данни чрез DashboardDataService
        // Стъпка 2: Прати към Copilot
        // Стъпка 3: Върни ai_analysis.json
        //
        // Фронтендът ползва това за pop-up "Copilot Анализ".
        //
        // JSON структура (генерира се от Azure OpenAI):
        // {
        //   "currentState":     "Линията работи с...",
        //   "anomalies":        ["OEE под прага", "Температура над нормата"],
        //   "rootCauses":       ["Вероятна причина..."],
        //   "patterns":         ["Повтарящ се модел..."],
        //   "recommendations":  ["Препоръка за мениджмънта..."],
        //   "riskLevel":        "medium"
        // }
        [HttpGet("ai/analyze/{lineId:int}")]
        public async Task<IActionResult> GetAiAnalysis(int lineId, [FromQuery] int days = 7)
        {
            // Вземаме пълния payload за линията
            var payload = await _dataService.BuildAsync(days);
            var lineData = payload.Lines.FirstOrDefault(l => l.LineId == lineId);

            if (lineData == null)
                return NotFound($"Линия {lineId} не е намерена.");

            // Подготвяме контекста за Copilot — само данните за тази линия
            var copilotContext = new
            {
                Line = new
                {
                    lineData.LineId,
                    lineData.LineName,
                    Type = lineData.IsAutomated ? "Автоматизирана" : "Ръчна",
                },
                Period = new
                {
                    payload.PeriodFrom,
                    payload.PeriodTo,
                    payload.PeriodDays,
                },
                Summary  = lineData.Summary,
                Trend    = lineData.Trend.TakeLast(10),   // последните 10 смени
                Alerts   = lineData.Alerts,
                BadShifts = lineData.BadShifts,
                LastShift = lineData.LastShift,
            };

            var aiResult = await _copilot.AnalyzeAsync(copilotContext);
            return Ok(aiResult);
        }

        // ── GET /api/data/ai/workers/{lineId} ─────────────────────────────────────
        // Праща към Copilot данните за отсъстващи работници.
        // Връща ai_workers.json — кой да замести кого и защо.
        //
        // JSON структура:
        // {
        //   "situationSummary":      "3-ма работника отсъстват...",
        //   "suggestions": [
        //     {
        //       "workerName":  "Иван Петров",
        //       "skillMatch":  ["assembly", "soldering"],
        //       "reason":      "Има нужните умения и е lead ниво",
        //       "priority":    1
        //     }
        //   ],
        //   "riskAfterReplacement": "low",
        //   "managerNote":           "Препоръчваме незабавно действие..."
        // }
        [HttpGet("ai/workers/{lineId:int}")]
        public async Task<IActionResult> GetAiWorkers(int lineId)
        {
            var scenarios = await _dataService.BuildScenariosAsync();
            var sc = scenarios.FirstOrDefault(s => s.LineId == lineId);

            if (sc == null)
                return NotFound($"Линия {lineId} не е намерена.");

            if (!sc.AbsentWorkers.Any())
                return Ok(new { message = "Всички работници присъстват. Не са нужни заместници." });

            var neededSkills = sc.AbsentWorkers
                .SelectMany(w => w.SkillSet.Split(','))
                .Distinct()
                .ToList();

            // Кандидати = присъстващи работници с нужните умения
            var candidates = sc.AllWorkers
                .Where(w => w.IsPresent)
                .Select(w => new
                {
                    w.Name, w.SkillSet, w.SkillLevel,
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
                sc.LineId,
                sc.LineName,
                LineType     = sc.IsAutomated ? "Автоматизирана" : "Ръчна",
                Absent       = sc.AbsentWorkers.Select(w => new { w.Name, w.SkillSet, w.SkillLevel, w.AbsenceReason }),
                NeededSkills = neededSkills,
                Candidates   = candidates,
            };

            var aiResult = await _copilot.SuggestWorkersAsync(copilotContext);
            return Ok(aiResult);
        }

        // ── GET /api/data/ai/forecast/{lineId} ────────────────────────────────────
        // Праща историята към Copilot и получава прогноза за следващите 7 дни.
        //
        // JSON структура:
        // {
        //   "trendAnalysis":     "Последните 30 дни показват...",
        //   "forecast": [
        //     { "day": 1, "expectedProduction": 320, "riskLevel": "low",  "note": "..." },
        //     { "day": 2, "expectedProduction": 290, "riskLevel": "high", "note": "Риск от авария" }
        //   ],
        //   "mainRisks":          ["Износване на лагер", "Липса на оператор"],
        //   "preventiveActions":  ["Планирай поддръжка", "Осигури заместник"],
        //   "confidenceLevel":    "medium"
        // }
        [HttpGet("ai/forecast/{lineId:int}")]
        public async Task<IActionResult> GetAiForecast(int lineId)
        {
            // За прогнозата взимаме по-дълъг период — 30 дни
            var payload  = await _dataService.BuildAsync(days: 30);
            var lineData = payload.Lines.FirstOrDefault(l => l.LineId == lineId);

            if (lineData == null)
                return NotFound($"Линия {lineId} не е намерена.");

            var copilotContext = new
            {
                Line = new
                {
                    lineData.LineId,
                    lineData.LineName,
                    Type = lineData.IsAutomated ? "Автоматизирана" : "Ръчна",
                },
                ForecastDays = 7,
                History      = lineData.Trend.Select(k => new
                {
                    k.Date, k.ShiftName,
                    k.TotalProduced, k.DefectiveUnits,
                    k.OEE, k.DefectRate,
                    k.AvgTemperature, k.EnergyIntensity,
                    k.TokenScore, k.OperatorComment,
                }),
                Alerts       = lineData.Alerts,
                BadShifts    = lineData.BadShifts,
            };

            var aiResult = await _copilot.ForecastAsync(copilotContext);
            return Ok(aiResult);
        }
    }
}
