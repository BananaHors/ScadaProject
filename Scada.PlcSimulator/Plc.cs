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
        _values[40001] = 110.0; // BUS1_V       - busbar voltage (kV)
        _values[40002] = 50.0;  // GRID_FREQ    - grid frequency (Hz)
        _values[40003] = 200.0; // LINE1_I      - line current (A)
        _values[40004] = 25.0;  // TRAFO1_P     - active power (MW)
        _values[40005] = 45.0;  // TRAFO1_OIL_T - transformer oil temperature (Celsius)

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
                double change = _random.NextDouble() - 0.5; // a number between -0.5 and +0.5
                _values[address] = _values[address] + change;
            }
        }
    }

    // Read the current value at an address. Returns 0 if nothing is there yet.
    public double Read(int address)
    {
        lock (_lock)
        {
            if (_values.ContainsKey(address))
            {
                return _values[address];
            }

            return 0.0;
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
