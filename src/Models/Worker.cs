namespace Sensata.Models;

public class Worker
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string SkillSet { get; set; } = ""; // "assembly", "calibration", "pressing", "winding"
    public string SkillLevel { get; set; } = ""; // "junior", "senior", "lead"

    public List<ShiftWorker> ShiftAssignments { get; set; } = new();
}