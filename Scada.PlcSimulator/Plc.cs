namespace Scada.PlcSimulator;

public class Plc
{
    // The storage: maps an I/O address (a whole number) to its current value.
    private readonly Dictionary<int, double> _values = new();

    // Read the current value at an address. Returns 0 if nothing is there yet.
    public double Read(int address)
    {
        if (_values.ContainsKey(address))
        {
            return _values[address];
        }

        return 0.0;
    }

    // Write (store) a value at an address.
    public void Write(int address, double value)
    {
        _values[address] = value;
    }
}
