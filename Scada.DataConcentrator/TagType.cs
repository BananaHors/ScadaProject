namespace Scada.DataConcentrator;

// The four kinds of tag a SCADA system works with.
public enum TagType
{
    DI, // Digital Input  - a read-only on/off signal (e.g. breaker open/closed)
    DO, // Digital Output - an on/off signal we can set (e.g. breaker command)
    AI, // Analog Input   - a read-only measured value (e.g. voltage)
    AO  // Analog Output  - a value we can set (e.g. tap-changer setpoint)
}
