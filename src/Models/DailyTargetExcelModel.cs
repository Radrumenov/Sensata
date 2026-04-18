namespace Sensata.Models;

public class DailyTargetExcelModel
{
    public DateTime Date { get; set; }
    public int LineId { get; set; }          // беше MachineId — сега е LineId
    public int PlannedQuantity { get; set; }
}

// Excel шаблонът трябва да има колони: Date | LineId | PlannedQuantity
