namespace Sensata.Models;
 
public class ProductionReport
{
    public int Id { get; set; }
    public int LineId { get; set; }          // заменя MachineId
    public DateTime Date { get; set; }
    public string ShiftName { get; set; } = "";
 
    // Сурови производствени данни (машините само записват, не изчисляват)
    public int TotalProduced { get; set; }
    public int DefectiveUnits { get; set; }
    public double AvgTemperature { get; set; }  // Celsius
    public double AvgPressure { get; set; }      // bar
    public double EnergyConsumed { get; set; }   // kWh
 
    // KPI се изчислява в бизнес логиката (KpiCalculatorService), не тук
    // OEE = Availability × Performance × Quality
    // DefectRate = DefectiveUnits / TotalProduced * 100
 
    public string OperatorComment { get; set; } = "";
    public int CommentTokenScore { get; set; } = 0; // изчислен от TokenScoringService
}