namespace Scada.DataConcentrator;

// Categories of log message. [Flags] means the values are single bits that can
// be combined into one number (the "traceword"): Login|Write == 1|4 == 5.
[Flags]
public enum LogCategory
{
    Login = 1,        // login / logout
    TagChange = 2,    // add/remove tag, add alarm
    Write = 4,        // writing a value to an output
    Acknowledge = 8,  // acknowledging alarms
    Alarm = 16,       // an alarm became active
    Warning = 32,     // a value approaching a threshold

    All = Login | TagChange | Write | Acknowledge | Alarm | Warning
}
