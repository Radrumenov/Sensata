using Microsoft.AspNetCore.Mvc;
using MiniExcelLibs;
using Sensata.Models;
using Sensata.Data;

namespace Sensata.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // POST /api/dashboard/upload-excel-targets
        // Сравнява план от Excel с реалното производство от базата
        [HttpPost("upload-excel-targets")]
        public IActionResult UploadExcelTargets(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Моля, качете валиден Excel файл.");

            try
            {
                using var stream = file.OpenReadStream();
                var excelTargets = stream.Query<DailyTargetExcelModel>().ToList();

                var results = new List<object>();

                foreach (var target in excelTargets)
                {
                    var actualData = _context.Reports
                        .FirstOrDefault(r =>
                            r.LineId == target.LineId &&
                            r.Date.Date == target.Date.Date);

                    if (actualData != null)
                    {
                        bool isGreen = actualData.TotalProduced >= target.PlannedQuantity;

                        // Намираме абсентите за тази линия и дата
                        var shift = _context.Shifts
                            .FirstOrDefault(s =>
                                s.LineId == target.LineId &&
                                s.Date.Date == target.Date.Date);

                        int absentCount = 0;
                        if (shift != null)
                        {
                            absentCount = _context.ShiftWorkers
                                .Count(sw => sw.ShiftId == shift.Id && !sw.IsPresent);
                        }

                        results.Add(new
                        {
                            LineId         = target.LineId,
                            Date           = target.Date.ToString("dd.MM.yyyy"),
                            Target         = target.PlannedQuantity,
                            Actual         = actualData.TotalProduced,
                            DeliveryStatus = isGreen ? "Green" : "Red",
                            TokenScore     = actualData.CommentTokenScore,
                            AbsentWorkers  = absentCount,
                            OperatorComment = actualData.OperatorComment
                        });
                    }
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Грешка: {ex.Message}");
            }
        }

        // GET /api/dashboard/lines
        // Връща всички линии с текущия им статус
        [HttpGet("lines")]
        public IActionResult GetLines()
        {
            var lines = _context.Lines.Select(l => new
            {
                l.Id,
                l.Name,
                l.IsAutomated,
                Type = l.IsAutomated ? "Автоматизирана" : "Ръчна"
            }).ToList();

            return Ok(lines);
        }

        // GET /api/dashboard/workers/available?lineId=1&date=2026-04-17
        // Връща работници с нужния skill set, които могат да се прехвърлят
        [HttpGet("workers/available")]
        public IActionResult GetAvailableWorkers(int lineId, DateTime date)
        {
            // Намираме смяната за тази линия и дата
            var shift = _context.Shifts
                .FirstOrDefault(s => s.LineId == lineId && s.Date.Date == date.Date);

            if (shift == null)
                return NotFound("Няма смяна за тази линия и дата.");

            // Намираме отсъстващите работници
            var absentWorkerIds = _context.ShiftWorkers
                .Where(sw => sw.ShiftId == shift.Id && !sw.IsPresent)
                .Select(sw => sw.WorkerId)
                .ToList();

            if (!absentWorkerIds.Any())
                return Ok(new { Message = "Всички работници присъстват.", Suggestions = new List<object>() });

            // Намираме skill set-а на отсъстващите
            var neededSkills = _context.Workers
                .Where(w => absentWorkerIds.Contains(w.Id))
                .Select(w => w.SkillSet)
                .ToList();

            // Намираме налични работници от други линии с нужния skill
            var suggestions = _context.Workers
                .Where(w =>
                    !absentWorkerIds.Contains(w.Id) &&
                    neededSkills.Any(skill => w.SkillSet.Contains(skill)))
                .Select(w => new
                {
                    w.Id,
                    w.Name,
                    w.SkillSet,
                    w.SkillLevel,
                    Suggestion = $"Може да замести на Линия {lineId}"
                })
                .ToList();

            return Ok(new
            {
                AbsentCount = absentWorkerIds.Count,
                Suggestions = suggestions
            });
        }
    }
}
