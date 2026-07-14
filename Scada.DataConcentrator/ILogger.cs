namespace Scada.DataConcentrator;

// A contract for anything that can record log messages.
// It promises one thing: you can hand it a message to log.
public interface ILogger
{
    void Log(string message);
}
