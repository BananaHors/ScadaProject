namespace Scada.DataConcentrator;

// An alarm defined on an analog input tag. It fires when that tag's value
// crosses the threshold in the configured direction.
public class Alarm
{
    public double Threshold { get; set; }
    public AlarmDirection Direction { get; set; }
    public string Message { get; set; } = "";
    public AlarmState State { get; set; } = AlarmState.Inactive;
}
