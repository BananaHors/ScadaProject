namespace Scada.DataConcentrator;

// Represents one tag (a named signal) in the SCADA system.
// For now, just the properties common to every tag type. We'll add the
// analog-only and input-only properties in the next step.
public class Tag
{
    public string Name { get; set; } = "";
    public TagType Type { get; set; }
    public string Description { get; set; } = "";
    public int IoAddress { get; set; }
}
