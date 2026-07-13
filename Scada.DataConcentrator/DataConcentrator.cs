using Scada.PlcSimulator;

namespace Scada.DataConcentrator;

public class DataConcentrator
{
    // The Data Concentrator owns a PLC and gets its values from it.
    private readonly Plc _plc = new();

    // All the tags currently defined in the system.
    private readonly List<Tag> _tags = new();

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

        // The name is the tag's id, so it must be unique across all tags.
        if (_tags.Any(existing => existing.Name == tag.Name))
        {
            errors.Add($"A tag named '{tag.Name}' already exists.");
        }

        if (errors.Count == 0)
        {
            _tags.Add(tag);
        }

        return errors;
    }

    // Remove a tag by its name. Returns true if a matching tag was removed.
    public bool RemoveTag(string name)
    {
        Tag? tag = _tags.FirstOrDefault(existing => existing.Name == name);

        if (tag == null)
        {
            return false;
        }

        _tags.Remove(tag);
        return true;
    }
}
