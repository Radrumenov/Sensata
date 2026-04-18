namespace Sensata.Models;
 
public class LineAlert
{
    public int Id { get; set; }
    public int LineId { get; set; }
    public DateTime Timestamp { get; set; }
    public string ErrorCode { get; set; } = "";
    public double RawValue { get; set; }
}
 
// table DB for alert code