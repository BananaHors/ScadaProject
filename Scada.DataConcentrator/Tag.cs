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

    // --- Input tags only (DI, AI) ---
    public int? ScanTime { get; set; }    // milliseconds between reads
    public bool? OnOffScan { get; set; }  // is scanning currently turned on?

    // --- Analog tags only (AI, AO) ---
    public double? LowLimit { get; set; }
    public double? HighLimit { get; set; }
    public string? Units { get; set; }

    // --- Output tags only (DO, AO) ---
    public double? InitialValue { get; set; }

    // --- Analog input only (AI) ---
    public double? Deadband { get; set; }   // how big a change we react to
    public double? Hysteresis { get; set; } // margin for turning alarms on/off
}
