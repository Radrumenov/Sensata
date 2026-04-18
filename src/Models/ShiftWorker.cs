namespace Sensata.Models;

public class ShiftWorker
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public int WorkerId { get; set; }
    public bool IsPresent { get; set; } = true;
    public string AbsenceReason { get; set; } = "";
    public int TokenScore { get; set; } = 0;
    public string OperatorComment { get; set; } = "";
}
