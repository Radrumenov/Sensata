using Microsoft.AspNetCore.Mvc;
using MiniExcelLibs;
using Sensata.Models;
using Sensata.Data;
using Sensata.Services;

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

        [HttpPost("upload-excel-targets")]
        public IActionResult UploadExcelTargets(IFormFile file)
        {
            // 1. Проверка дали изобщо е качен файл
            if (file == null || file.Length == 0)
            {
                return BadRequest("Моля, качете валиден Excel файл.");
            }

            try
            {
                // 2. ОТВАРЯМЕ ФАЙЛА ДИРЕКТНО В ПАМЕТТА (Stream)
                using var stream = file.OpenReadStream();

                // 3. MiniExcel прочита редовете и ги превръща в списък от C# обекти за милисекунди!
                var excelTargets = stream.Query<DailyTargetExcelModel>().ToList();

                // 4. Създаваме списък с резултати, които ще върнем на фронтенда
                var dashboardResults = new List<object>();

                // 5. Обхождаме всеки ред от качения Excel файл
                foreach (var target in excelTargets)
                {
                    // ПРОМЯНАТА С ДАТАТА Е ТУК:
                    // Вместо да взимаме DateTime.Today, ние търсим в базата данни
                    // запис, който отговаря на Машината И на конкретната Дата от Excel-а.
                    // Използваме .Date, за да игнорираме часовете и минутите при сравнението.
                    var actualData = _context.Reports
                        .FirstOrDefault(r => r.MachineId == target.MachineId && r.Date.Date == target.Date.Date);

                    if (actualData != null)
                    {
                        // 6. Сравняваме плана (от Excel) с реалността (от SQL)
                        bool isDeliveryGreen = actualData.TotalProduced >= target.PlannedQuantity;

                        // 7. Добавяме резултата за тази конкретна дата и машина
                        dashboardResults.Add(new 
                        {
                            MachineId = target.MachineId,
                            Date = target.Date.ToString("dd.MM.yyyy"), // Добавих датата тук, за да си сигурен, че чете правилния ден!
                            Target = target.PlannedQuantity,
                            Actual = actualData.TotalProduced,
                            DeliveryStatus = isDeliveryGreen ? "Green" : "Red"
                        });
                    }
                }

                // 8. Връщаме готовите сдвоени данни към UI-а, за да се оцвети диаграмата
                return Ok(dashboardResults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Грешка при четене на Excel: {ex.Message}");
            }
        }
    }
}