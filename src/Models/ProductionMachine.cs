namespace Sensata.Models;

  public class ProductionMachine
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        
        // Връщаме липсващите полета!
        public string LastComment { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Връзки към другите таблици
        public List<ProductionReport> Reports { get; set; } = new();
        public List<MachineAlert> Alerts { get; set; } = new();
    }
