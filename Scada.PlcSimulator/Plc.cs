namespace Scada.PlcSimulator;

public class Plc
{
    // The storage: maps an I/O address (a whole number) to its current value.
    private readonly Dictionary<int, double> _values = new();

    // The value each analog input drifts back toward, so it oscillates around a
    // sensible level instead of wandering off in a pure random walk.
    private readonly Dictionary<int, double> _centers = new();

    private readonly Random _random = new();

    // Guards _values / _centers. Only one thread may hold it at a time.
    private readonly object _lock = new();

    public Plc()
    {
        // Seed the substation analog inputs (Modbus input registers, 3xxxx).
        SetAnalog(30001, 110.0); // bus voltage (kV)
        SetAnalog(30002, 50.0);  // grid frequency (Hz)
        SetAnalog(30003, 200.0); // line current (A)
        SetAnalog(30006, 65.0);  // transformer oil temperature (Celsius)

        StartSimulation();
    }

    // Set an analog input's current value AND the level it oscillates around.
    // The Data Concentrator uses this to start a new AI tag inside its range.
    public void SetAnalog(int address, double value)
    {
        lock (_lock)
        {
            _values[address] = value;
            _centers[address] = value;
        }
    }

    private void StartSimulation()
    {
        Thread thread = new Thread(SimulationLoop);
        thread.IsBackground = true;
        thread.Start();
    }

    private void SimulationLoop()
    {
        while (true)
        {
            try
            {
                SimulateStep();
            }
            catch
            {
                // A simulation hiccup must not take down this background thread.
            }

            Thread.Sleep(1000);
        }
    }

    // Move each analog input a little: pull it gently back toward its center,
    // plus a small random wobble. This keeps values fluctuating but bounded.
    private void SimulateStep()
    {
        lock (_lock)
        {
            foreach (int address in _values.Keys.ToList())
            {
                if (address >= 30001 && address <= 39999)
                {
                    // Analog input: drift gently back toward its center + a wobble.
                    double center = _values[address];
                    if (_centers.TryGetValue(address, out double c))
                    {
                        center = c;
                    }

                    double pull = (center - _values[address]) * 0.15;
                    double wobble = _random.NextDouble() - 0.5; // between -0.5 and +0.5
                    _values[address] = _values[address] + pull + wobble;
                }
                else if (address >= 10001 && address <= 19999)
                {
                    // Digital input: occasionally flip between 0 and 1 (~10%/tick).
                    if (_random.NextDouble() < 0.1)
                    {
                        _values[address] = _values[address] == 0.0 ? 1.0 : 0.0;
                    }
                }
                // Outputs (0xxxx / 4xxxx) hold whatever was written.
            }
        }
    }

    // Read the current value at an address. Returns 0 if nothing is there yet.
    public double Read(int address)
    {
        lock (_lock)
        {
            if (!_values.ContainsKey(address))
            {
                // Lazily bring a new analog-input address (3xxxx) to life so any
                // AI tag mapped to it shows moving data instead of a frozen 0.
                if (address >= 30001 && address <= 39999)
                {
                    _values[address] = 100.0;
                    _centers[address] = 100.0;
                }
                else if (address >= 10001 && address <= 19999)
                {
                    _values[address] = 0.0; // digital input starts OFF; simulation toggles it
                }
                else
                {
                    return 0.0; // outputs read 0 until written
                }
            }

            return _values[address];
        }
    }

    // Write (store) a value at an address (used for outputs).
    public void Write(int address, double value)
    {
        lock (_lock)
        {
            _values[address] = value;
        }
    }
}
