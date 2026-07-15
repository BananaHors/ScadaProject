using Microsoft.EntityFrameworkCore;
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

    // Where we record actions and problems. Passed in by whoever creates us.
    private readonly ILogger _logger;

    public DataConcentrator(ILogger logger)
    {
        _logger = logger;
        EnsureDatabase();
        LoadTags();
        StartScanning();
    }

    // Raised whenever a scanned tag's value changes. Carries the tag name and
    // the new value. Anyone interested (like the UI) can subscribe.
    public event Action<string, double>? ValueChanged;

    // Raised when an alarm first becomes Active (newly triggered). Carries the
    // tag name and the alarm that fired.
    public event Action<string, Alarm>? AlarmRaised;

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

            // Each tag must map to its own I/O address.
            if (_tags.Any(existing => existing.IoAddress == tag.IoAddress))
            {
                errors.Add($"I/O address {tag.IoAddress} is already used by another tag.");
            }

            if (errors.Count == 0)
            {
                _tags.Add(tag);
            }
        }

        if (errors.Count == 0)
        {
            SaveTag(tag);
            _logger.Log(LogLevel.Info, $"Tag '{tag.Name}' added.");
        }

        return errors;
    }

    // Remove a tag by its name. Returns true if a matching tag was removed.
    public bool RemoveTag(string name)
    {
        bool removed;

        lock (_lock)
        {
            Tag? tag = _tags.FirstOrDefault(existing => existing.Name == name);

            if (tag == null)
            {
                removed = false;
            }
            else
            {
                _tags.Remove(tag);
                _currentValues.Remove(name);
                removed = true;
            }
        }

        if (removed)
        {
            DeleteTag(name);
            _logger.Log(LogLevel.Info, $"Tag '{name}' removed.");
        }

        return removed;
    }

    // Attach an alarm to an existing AI tag. Returns an empty list on success,
    // or a list of problems if the alarm was rejected (and NOT attached).
    public List<string> AddAlarm(string tagName, Alarm alarm)
    {
        List<string> errors = new();
        Tag? target = null;

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
                target = tag;
            }
        }

        if (errors.Count == 0 && target != null)
        {
            SaveTag(target);
            _logger.Log(LogLevel.Info, $"Alarm added to '{tagName}'.");
        }

        return errors;
    }

    // Acknowledge all Active alarms on a tag (an operator action). An alarm
    // that is still in its zone becomes Acknowledged (yellow); one whose value
    // has already returned to normal is cleared to Inactive.
    public void Acknowledge(string tagName)
    {
        int acknowledged = 0;

        lock (_lock)
        {
            Tag? tag = _tags.FirstOrDefault(existing => existing.Name == tagName);

            if (tag == null)
            {
                return;
            }

            double value = 0.0;
            if (_currentValues.ContainsKey(tagName))
            {
                value = _currentValues[tagName];
            }

            foreach (Alarm alarm in tag.Alarms)
            {
                if (alarm.State != AlarmState.Active)
                {
                    continue;
                }

                if (IsInZone(alarm, value))
                {
                    alarm.State = AlarmState.Acknowledged; // still bad -> yellow
                }
                else
                {
                    alarm.State = AlarmState.Inactive; // latched but recovered -> clear
                }

                acknowledged++;
            }
        }

        if (acknowledged > 0)
        {
            _logger.Log(LogLevel.Info, $"Acknowledged {acknowledged} alarm(s) on '{tagName}'.");
        }
    }

    // Write a value to an output tag (DO/AO), pushing it down to the PLC.
    // Returns an empty list on success, or a list of problems otherwise.
    public List<string> WriteToTag(string tagName, double value)
    {
        List<string> errors = new();
        bool written = false;

        lock (_lock)
        {
            Tag? tag = _tags.FirstOrDefault(existing => existing.Name == tagName);

            if (tag == null)
            {
                errors.Add($"No tag named '{tagName}' exists.");
            }
            else if (tag.Type != TagType.DO && tag.Type != TagType.AO)
            {
                errors.Add("Only output tags (DO/AO) can be written.");
            }
            else
            {
                _plc.Write(tag.IoAddress, value);
                _currentValues[tag.Name] = value;
                written = true;
            }
        }

        // Let subscribers know the output's value changed (outside the lock).
        if (written)
        {
            ValueChanged?.Invoke(tagName, value);
            _logger.Log(LogLevel.Info, $"Wrote {value:F2} to '{tagName}'.");
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

    // Get a snapshot copy of all tags currently in the system.
    public List<Tag> GetTags()
    {
        lock (_lock)
        {
            return _tags.ToList();
        }
    }

    // Make sure the database exists and has the latest schema. Applies any
    // pending migrations, creating the database file if it is not there yet.
    private void EnsureDatabase()
    {
        using var db = new ScadaDbContext();
        db.Database.Migrate();
    }

    // Load all saved tags (and their alarms) from the database into memory.
    private void LoadTags()
    {
        using var db = new ScadaDbContext();
        List<Tag> saved = db.Tags.Include(tag => tag.Alarms).ToList();

        lock (_lock)
        {
            _tags.Clear();
            _tags.AddRange(saved);
        }
    }

    // Insert or update a tag (and its alarms) in the database.
    private void SaveTag(Tag tag)
    {
        using var db = new ScadaDbContext();
        db.Update(tag);   // Id == 0 -> insert a new row; Id > 0 -> update the existing one
        db.SaveChanges();
    }

    // Delete a tag (and its alarms) from the database.
    private void DeleteTag(string name)
    {
        using var db = new ScadaDbContext();
        Tag? tag = db.Tags.Include(t => t.Alarms).FirstOrDefault(t => t.Name == name);

        if (tag != null)
        {
            db.Alarms.RemoveRange(tag.Alarms);
            db.Tags.Remove(tag);
            db.SaveChanges();
        }
    }

    // Write a permanent journal row recording that an alarm fired.
    private void SaveActivatedAlarm(string tagName, Alarm alarm)
    {
        using var db = new ScadaDbContext();

        db.ActivatedAlarms.Add(new ActivatedAlarm
        {
            TagName = tagName,
            Message = alarm.Message,
            Timestamp = DateTime.Now
        });

        db.SaveChanges();
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
        List<(string TagName, Alarm Alarm)> raised = new();
        List<(LogLevel Level, string Message)> logs = new();

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
                        CheckAlarms(tag, newValue, raised, logs);
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

        foreach (var item in raised)
        {
            AlarmRaised?.Invoke(item.TagName, item.Alarm);
            SaveActivatedAlarm(item.TagName, item.Alarm);
        }

        // Write any alarm/warning log entries (also outside the lock).
        foreach (var entry in logs)
        {
            _logger.Log(entry.Level, entry.Message);
        }
    }

    // Evaluate every alarm on an AI tag against its new value, applying the
    // latching state machine. Any alarm that becomes newly Active is added to
    // 'raised' so it can be announced after the lock is released.
    // NOTE: this runs while _lock is held (called from inside ScanOnce).
    private void CheckAlarms(Tag tag, double value,
        List<(string TagName, Alarm Alarm)> raised,
        List<(LogLevel Level, string Message)> logs)
    {
        if (tag.Type != TagType.AI)
        {
            return;
        }

        foreach (Alarm alarm in tag.Alarms)
        {
            bool inZone = IsInZone(alarm, value);

            if (alarm.State == AlarmState.Inactive && inZone)
            {
                // Just entered the alarm zone: raise it.
                alarm.State = AlarmState.Active;
                raised.Add((tag.Name, alarm));
                logs.Add((LogLevel.Error, $"Alarm active on '{tag.Name}': {alarm.Message}"));
            }
            else if (alarm.State == AlarmState.Acknowledged && !inZone)
            {
                // An acknowledged alarm whose value returned to normal: clear it.
                alarm.State = AlarmState.Inactive;
            }

            // If the alarm is Active, it stays Active no matter what the value
            // does - that is the latch. It only clears once a human acknowledges.

            UpdateWarning(tag, alarm, value, inZone, logs);
        }
    }

    // Log a WARNING once when the value enters an alarm's warning band, and
    // reset when it leaves so a later approach warns again.
    private void UpdateWarning(Tag tag, Alarm alarm, double value, bool inZone,
        List<(LogLevel Level, string Message)> logs)
    {
        if (alarm.WarningMargin == null)
        {
            return; // no warning configured for this alarm
        }

        bool inWarningBand = !inZone && IsNearThreshold(alarm, value);

        if (inWarningBand && !alarm.WarningActive)
        {
            alarm.WarningActive = true;
            logs.Add((LogLevel.Warning, $"'{tag.Name}' approaching threshold: {alarm.Message}"));
        }
        else if (!inWarningBand)
        {
            alarm.WarningActive = false;
        }
    }

    // True if the value is within the warning margin of the threshold, on the
    // approaching side (but not yet in the alarm zone).
    private bool IsNearThreshold(Alarm alarm, double value)
    {
        double margin = alarm.WarningMargin ?? 0.0;

        if (alarm.Direction == AlarmDirection.Above)
        {
            return value >= alarm.Threshold - margin && value < alarm.Threshold;
        }

        return value <= alarm.Threshold + margin && value > alarm.Threshold;
    }

    // True if the value is past the alarm's threshold in its direction.
    private bool IsInZone(Alarm alarm, double value)
    {
        if (alarm.Direction == AlarmDirection.Above)
        {
            return value > alarm.Threshold;
        }

        return value < alarm.Threshold;
    }
}
