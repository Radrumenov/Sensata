namespace Sensata.Models; 

 public class ProductionReport
    {
        public int Id { get; set; }
        public int MachineId { get; set; }
        public DateTime Date { get; set; }

        // Faktori
        public int TotalProduced { get; set; }      
        public int DefectiveUnits { get; set; }     
        public double AvgTemperature { get; set; }  // Celsium
        public double AvgPressure { get; set; }     // bar
        public double EnergyConsumed { get; set; }  // kWh

        // KPI       
        public string OperatorComment { get; set; } = ""; // Коментар на оператора
    }
