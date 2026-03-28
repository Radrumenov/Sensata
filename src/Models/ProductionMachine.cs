namespace Sensata.Models
{
  public class ProductionMachine
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Status { get; set; } = "Green"; // Green, Yellow, Red
        
        // Връщаме липсващите полета!
        public string LastComment { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Връзки към другите таблици
        public List<ProductionReport> Reports { get; set; } = new();
        public List<MachineAlert> Alerts { get; set; } = new();
    }


// Клас, който "мапва" колоните от Excel файла
    public class DailyTargetExcelModel
    {
        public DateTime Date { get; set; }
        public string MachineName { get; set; } = "";
        public int PlannedQuantity { get; set; } // Колко е трябвало да произведат
        public string ShiftManager { get; set; } = ""; // Кой е бил на смяна
    }


}