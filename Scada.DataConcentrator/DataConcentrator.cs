using Scada.PlcSimulator;

namespace Scada.DataConcentrator;

public class DataConcentrator
{
    // The Data Concentrator owns a PLC and gets its values from it.
    private readonly Plc _plc = new();

    // All the tags currently defined in the system.
    private readonly List<Tag> _tags = new();

    // The latest scanned value for each tag, keyed by tag name.
    private readonly Dictionary<string, double> _currentValues = new();

    // Guards _tags and _currentValues - the state shared with the scan thread.
    private readonly object _lock = new();

    public DataConcentrator()
    {
        StartScanning();
    }

    // Raised whenever a scanned tag's value changes. Carries the tag name and
    // the new value. Anyone interested (like the UI) can subscribe.
    public event Action<string, double>? ValueChanged;

    // Read the current value at an I/O address, by asking the PLC.
    public double ReadValue(int address)
    {
        return _plc.Read(address);
    }

    // Add a tag after validating it. Returns an empty list if the tag was
    // added, or a list of problems if it was rejected (and NOT added).
    public List<string> AddTag(Tag tag)
    {
        List<string> errors = tag.Validate();

        lock (_lock)
        {
            // The name is the tag's id, so it must be unique across all tags.
            if (_tags.Any(existing => existing.Name == tag.Name))
            {
                errors.Add($"A tag named '{tag.Name}' already exists.");
            }

            if (errors.Count == 0)
            {
                _tags.Add(tag);
            }
        }

        return errors;
    }

    // Remove a tag by its name. Returns true if a matching tag was removed.
    public bool RemoveTag(string name)
    {
        lock (_lock)
        {
            Tag? tag = _tags.FirstOrDefault(existing => existing.Name == name);

            if (tag == null)
            {
                return false;
            }

            _tags.Remove(tag);
            _currentValues.Remove(name);
            return true;
        }
    }

    // Attach an alarm to an existing AI tag. Returns an empty list on success,
    // or a list of problems if the alarm was rejected (and NOT attached).
    public List<string> AddAlarm(string tagName, Alarm alarm)
    {
        List<string> errors = new();

        lock (_lock)
        {
            Tag? tag = _tags.FirstOrDefault(existing => existing.Name == tagName);

            if (tag == null)
            {
                errors.Add($"No tag named '{tagName}' exists.");
            }
            else if (tag.Type != TagType.AI)
            {
                errors.Add("Alarms can only be added to analog input (AI) tags.");
            }
            else
            {
                tag.Alarms.Add(alarm);
            }
        }

        return errors;
    }

    // Get the most recently scanned value for a tag. Returns 0 if we have not
    // scanned a value for that tag yet.
    public double GetCurrentValue(string tagName)
    {
        lock (_lock)
        {
            if (_currentValues.ContainsKey(tagName))
            {
                return _currentValues[tagName];
            }

            return 0.0;
        }
    }

    // Launch a background thread that keeps scanning input tags.
    private void StartScanning()
    {
        Thread thread = new Thread(ScanLoop);
        thread.IsBackground = true;
        thread.Start();
    }

    // Runs forever on the background thread: scan, wait one second, repeat.
    private void ScanLoop()
    {
        while (true)
        {
            ScanOnce();
            Thread.Sleep(1000);
        }
    }

    // Read the current PLC value for every input tag that has scanning on,
    // and remember which tags actually changed.
    private void ScanOnce()
    {
        List<string> changed = new();

        lock (_lock)
        {
            foreach (Tag tag in _tags)
            {
                bool isInput = tag.Type == TagType.AI || tag.Type == TagType.DI;
                bool scanning = tag.OnOffScan == true;

                if (isInput && scanning)
                {
                    double newValue = _plc.Read(tag.IoAddress);
                    bool isNew = !_currentValues.ContainsKey(tag.Name);

                    if (isNew || _currentValues[tag.Name] != newValue)
                    {
                        _currentValues[tag.Name] = newValue;
                        changed.Add(tag.Name);
                    }
                }
            }
        }

        // Announce changes AFTER releasing the lock, so a subscriber's code
        // never runs while we are holding it.
        foreach (string name in changed)
        {
            ValueChanged?.Invoke(name, GetCurrentValue(name));
        }
    }
}
