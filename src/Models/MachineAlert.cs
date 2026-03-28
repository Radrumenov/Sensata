namespace Sensata.Models;

public class MachineAlert
    {
        public int Id { get; set; }
        public int MachineId { get; set; }
        public DateTime Timestamp { get; set; }
        public string AlertType { get; set; } = ""; // "Alarm" или "Deviation"
        public string Severity { get; set; } = "";  // "Warning", "Critical"
        public string Message { get; set; } = "";   // Описание на проблема
    }
