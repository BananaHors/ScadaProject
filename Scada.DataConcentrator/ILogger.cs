namespace Scada.DataConcentrator;

// A contract for anything that can record log messages.
public interface ILogger
{
    // Which categories actually get written (the "traceword").
    LogCategory TraceWord { get; set; }

    // Log a message. It is only written if its category is enabled in TraceWord.
    void Log(LogLevel level, LogCategory category, string message);
}
