namespace Scada.DataConcentrator;

// The lifecycle state of an alarm.
public enum AlarmState
{
    Inactive,     // the value is normal - no alarm
    Active,       // the value is in the alarm zone and NOT yet acknowledged
    Acknowledged  // the operator has acknowledged it, but the value is still in the alarm zone
}
