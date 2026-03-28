namespace Sensata.Models; 

 public class ProductionReport
    {
        public int Id { get; set; }
        public int MachineId { get; set; }
        public DateTime Date { get; set; }

        // 5 Исторически производствени фактора
        public int TotalProduced { get; set; }      // Общо произведени
        public int DefectiveUnits { get; set; }     // Брак (дефекти)
        public double AvgTemperature { get; set; }  // Средна температура (°C)
        public double AvgPressure { get; set; }     // Средно налягане (bar)
        public double EnergyConsumed { get; set; }  // Консумирана енергия (kWh)

        // KPI       
        public string OperatorComment { get; set; } = ""; // Коментар на оператора
    }
