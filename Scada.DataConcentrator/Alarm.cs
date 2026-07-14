namespace Scada.DataConcentrator;

// An alarm defined on an analog input tag. It fires when that tag's value
// crosses the threshold in the configured direction.
public class Alarm
{
    // Database primary key. Set automatically by the database.
    public int Id { get; set; }

    public double Threshold { get; set; }
    public AlarmDirection Direction { get; set; }
    public string Message { get; set; } = "";
    public AlarmState State { get; set; } = AlarmState.Inactive;

    // Optional: warn when the value comes within this many units of the
    // threshold (approaching it). Leave null for no warning.
    public double? WarningMargin { get; set; }

    // Runtime flag: are we currently inside the warning band? Used so we log
    // the warning only once per approach, not every scan.
    public bool WarningActive { get; set; }
}
