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
            
            if (file == null || file.Length == 0)
            {
                return BadRequest("Моля, качете валиден Excel файл.");
            }

            try
            {
                
                using var stream = file.OpenReadStream();

                
                var excelTargets = stream.Query<DailyTargetExcelModel>().ToList();

                
                var dashboardResults = new List<object>();

                
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
                       
                        bool isDeliveryGreen = actualData.TotalProduced >= target.PlannedQuantity;

                       
                        dashboardResults.Add(new 
                        {
                            MachineId = target.MachineId,
                            Date = target.Date.ToString("dd.MM.yyyy"),
                            Target = target.PlannedQuantity,
                            Actual = actualData.TotalProduced,
                            DeliveryStatus = isDeliveryGreen ? "Green" : "Red"
                        });
                    }
                }

               
                return Ok(dashboardResults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Грешка при четене на Excel: {ex.Message}");
            }
        }
    }
}