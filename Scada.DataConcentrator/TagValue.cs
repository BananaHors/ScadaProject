namespace Scada.DataConcentrator;

// One recorded reading of a tag's value at a moment in time - the value history.
// Feeds the Report, the history charts (feature #2), and DB filtering (feature #4).
public class TagValue
{
    public int Id { get; set; }
    public string TagName { get; set; } = "";
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}
