namespace Scada.DataConcentrator;

// An ILogger that appends timestamped lines to a text file (system.log by default),
// but only for the categories enabled in its traceword.
public class FileLogger : ILogger
{
    private readonly string _filePath;
    private readonly string _traceFilePath;
    private readonly object _lock = new();

    // Read by the background scan thread (in Log) and written by the UI thread
    // (via TraceWord). 'volatile' guarantees each thread sees the other's latest
    // write instead of a stale cached copy - without needing to take the lock on
    // the (hot) logging path.
    private volatile LogCategory _traceWord;

    public FileLogger(string filePath = "system.log")
    {
        _filePath = filePath;

        // Store the traceword (a number) in the stable per-user folder so the
        // choice of which logs to keep survives restarts.
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScadaProject");
        Directory.CreateDirectory(folder);
        _traceFilePath = Path.Combine(folder, "traceword.txt");

        _traceWord = LoadTraceWord();
    }

    // Which categories get written. Setting it saves the number to disk.
    public LogCategory TraceWord
    {
        get => _traceWord;
        set
        {
            _traceWord = value;
            SaveTraceWord(value);
        }
    }

    public void Log(LogLevel level, LogCategory category, string message)
    {
        // Skip if this category's bit is turned off in the traceword.
        if ((_traceWord & category) == 0)
        {
            return;
        }

        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  [{level}]  {message}";

        lock (_lock)
        {
            File.AppendAllText(_filePath, line + Environment.NewLine);
        }
    }

    private LogCategory LoadTraceWord()
    {
        try
        {
            if (File.Exists(_traceFilePath) &&
                int.TryParse(File.ReadAllText(_traceFilePath), out int value))
            {
                return (LogCategory)value;
            }
        }
        catch
        {
            // If reading fails for any reason, fall back to the default below.
        }

        return LogCategory.All; // default: log everything
    }

    private void SaveTraceWord(LogCategory value)
    {
        try
        {
            File.WriteAllText(_traceFilePath, ((int)value).ToString());
        }
        catch
        {
            // A logging-config write failure shouldn't crash the app.
        }
    }
}
