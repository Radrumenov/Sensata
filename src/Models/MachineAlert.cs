namespace Sensata.Models;

public class MachineAlert
    {
        public int Id { get; set; }
        public int MachineId { get; set; }
        public DateTime Timestamp { get; set; }
        public string ErrorCode { get; set; } = ""; // напр. ERR-TEMP-01
        public double RawValue { get; set; }        // Отчетената стойност от сензора
    }
