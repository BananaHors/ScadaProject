using Scada.PlcSimulator;

namespace Scada.DataConcentrator;

public class DataConcentrator
{
    // The Data Concentrator owns a PLC and gets its values from it.
    private readonly Plc _plc = new();

    // Read the current value at an I/O address, by asking the PLC.
    public double ReadValue(int address)
    {
        return _plc.Read(address);
    }
}
