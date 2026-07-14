namespace Scada.DataConcentrator;

// An ILogger that appends timestamped lines to a text file (system.log by default).
public class FileLogger : ILogger
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public FileLogger(string filePath = "system.log")
    {
        _filePath = filePath;
    }

    public void Log(string message)
    {
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}";

        // Guard the file: several threads may try to log at the same time.
        lock (_lock)
        {
            File.AppendAllText(_filePath, line + Environment.NewLine);
        }
    }
}
