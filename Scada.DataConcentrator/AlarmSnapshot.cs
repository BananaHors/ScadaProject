namespace Scada.DataConcentrator;

// A read-only copy of an alarm's data for the UI, taken under the lock so the
// UI never reads the live (mutating) alarm objects directly.
public class AlarmSnapshot
{
    public int Id { get; set; }
    public AlarmDirection Direction { get; set; }
    public double Threshold { get; set; }
    public string Message { get; set; } = "";
    public AlarmState State { get; set; }
}
