namespace Sensata.Models;

public class DailyTargetExcelModel
    {
        public DateTime Date { get; set; }
        public string MachineName { get; set; } = "";
        public int PlannedQuantity { get; set; } // Колко е трябвало да произведат
        public string ShiftManager { get; set; } = ""; // Кой е бил на смяна
    }

    
