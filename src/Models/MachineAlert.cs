namespace Sensata.Models;

public class MachineAlert
    {
        public int Id { get; set; }
        public int MachineId { get; set; }
        public DateTime Timestamp { get; set; }
        public string ErrorCode { get; set; } = ""; 
        public double RawValue { get; set; }       
    }

// table DB for alert code