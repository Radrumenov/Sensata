namespace Sensata.Models;

public class Shift
{
    public int Id { get; set; }
    public int LineId { get; set; }
    public DateTime Date { get; set; }
    public string ShiftName { get; set; } = ""; // "Смяна 1", "Смяна 2", "Смяна 3"
    public string SupervisorName { get; set; } = "";

    public List<ShiftWorker> Workers { get; set; } = new();
}


