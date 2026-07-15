# SCADA System — Substation Monitoring

A SCADA (Supervisory Control and Data Acquisition) system built as a course
project, themed around monitoring an **electrical substation** — busbar voltage,
line current, transformer oil temperature, grid frequency, breaker status, and
so on.

## Architecture

The system is split into independent layers, each its own .NET project:

- **Scada.PlcSimulator** — simulates the "hardware": a thread-safe store of live
  values addressed by I/O address, with a background thread that makes the
  values change over time.
- **Scada.DataConcentrator** — the brain: manages tags and alarms, scans the
  PLC, checks alarm conditions, raises events, logs actions, and (in progress)
  persists data to a database.
- **Scada.Wpf** *(planned)* — the graphical user interface.

Data flows `WPF ⇄ DataConcentrator ⇄ PLC`. Layers are loosely coupled (they
communicate through **events**), so new features can be added without changing
the core.

## Implemented so far

**PLC Simulator**
- Thread-safe read/write of values by I/O address (lock-protected).
- Background simulation thread that nudges values every second.
- Seeded with realistic substation nominal values (110 kV, 50 Hz, …).

**Tags**
- `Tag` model: type (DI/DO/AI/AO), name, address, and type-specific properties
  (limits, units, scan time, deadband, hysteresis) using nullable types.
- Self-validation (e.g. units only on analog tags) plus uniqueness checks.
- Add / remove tags.

**Scanning**
- Background scan loop reads input tags from the PLC, honoring per-tag on/off
  scan.
- Tracks current values; raises a `ValueChanged` event only on real changes.

**Alarms**
- `Alarm` model (threshold, direction, message, state) attached to AI tags.
- **Latching** state machine: Inactive → Active → Acknowledged; an alarm stays
  visible until a human acknowledges it, even if the value recovers.
- Alarm checking on each value change; `AlarmRaised` event; operator acknowledge.

**Outputs**
- Write values to output tags (DO/AO), pushed down to the PLC.

**Logging**
- `ILogger` interface + `FileLogger` writing timestamped, severity-tagged
  (Info/Warning/Error) lines to `system.log`.
- Injected into the Data Concentrator; logs every action.
- ERROR on alarm activation; WARNING when a value approaches a threshold
  (optional per-alarm margin).

**Database (EF Core + SQLite)**

* `ScadaDbContext` with three tables: `Tags`, `Alarms`, `ActivatedAlarms`(the permanent alarm journal).
- Initial migration generating the schema.

## Planned extra features (4 chosen)

- **#5 Role-based access** — login, roles, password rules, inactivity logout.
- **#2 History charts** — time-series of an analog input with alarm lines and
  min/max/average.
- **#4 Data filtering** — search AI history by tag / time / value → TXT export.
- **#7 Trace bits** — checkboxes selecting which log categories are written,
  stored as a numeric traceword.

## Still to do

- Connect the Data Concentrator to the database (save/load tags, write the alarm
  journal).
- Build the WPF user interface.
- Implement the 4 chosen features.

## Tech stack

- C# / .NET 10
* WPF _(planned)_
- Entity Framework Core 10 + SQLite
- Git

## Building

```
dotnet build
```

​
