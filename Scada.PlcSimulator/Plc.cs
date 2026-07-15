namespace Scada.PlcSimulator;

public class Plc
{
    // The storage: maps an I/O address (a whole number) to its current value.
    private readonly Dictionary<int, double> _values = new();

    // Used to generate the small random changes each simulation step.
    private readonly Random _random = new();

    // The "key" that guards access to _values. Only one thread may hold it at a time.
    private readonly object _lock = new();

    // The constructor: runs when someone does "new Plc()".
    // Here we seed realistic starting values for our substation.
    public Plc()
    {
        // Analog inputs live in the Modbus input-register range (3xxxx).
        _values[30001] = 110.0; // bus voltage (kV)
        _values[30002] = 50.0;  // grid frequency (Hz)
        _values[30003] = 200.0; // line current (A)
        _values[30006] = 65.0;  // transformer oil temperature (Celsius)

        // Kick off the background simulation so the values start moving.
        StartSimulation();
    }

    // Launch a background thread that keeps nudging the values over time.
    private void StartSimulation()
    {
        Thread thread = new Thread(SimulationLoop);
        thread.IsBackground = true; // don't keep the app alive just for this thread
        thread.Start();
    }

    // Runs forever on the background thread: update, wait one second, repeat.
    private void SimulationLoop()
    {
        while (true)
        {
            SimulateStep();
            Thread.Sleep(1000); // 1000 milliseconds = 1 second
        }
    }

    // Nudge every stored value by a small random amount.
    private void SimulateStep()
    {
        lock (_lock)
        {
            foreach (int address in _values.Keys.ToList())
            {
                // Only simulate analog inputs (input registers, 3xxxx). Outputs
                // (4xxxx/0xxxx) hold whatever was written; digital inputs stay put.
                if (address >= 30001 && address <= 39999)
                {
                    double change = _random.NextDouble() - 0.5; // between -0.5 and +0.5
                    _values[address] = _values[address] + change;
                }
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
                // The simulation loop then nudges it like the seeded ones.
                if (address >= 30001 && address <= 39999)
                {
                    _values[address] = 100.0;
                }
                else
                {
                    return 0.0; // outputs/digital inputs read 0 until written
                }
            }

            return _values[address];
        }
    }

    // Write (store) a value at an address.
    public void Write(int address, double value)
    {
        lock (_lock)
        {
            _values[address] = value;
        }
    }
}
