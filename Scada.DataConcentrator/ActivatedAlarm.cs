namespace Scada.DataConcentrator;

// A permanent record that an alarm fired. One row is written to the database
// journal every time an alarm occurs - it stays forever, no matter what the
// value or the alarm's state does afterward. This is the "aktivirani alarmi" table.
public class ActivatedAlarm
{
    public int Id { get; set; }
    public string TagName { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
