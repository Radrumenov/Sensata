namespace Sensata.Models;

public class ProductionLine
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsAutomated { get; set; } = false; // false = ръчна, true = автоматизирана

    // Навигационни връзки
    public List<ProductionReport> Reports { get; set; } = new();
    public List<LineAlert> Alerts { get; set; } = new();
    public List<Shift> Shifts { get; set; } = new();
}