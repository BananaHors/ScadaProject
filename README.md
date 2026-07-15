# SCADA System

A small SCADA (Supervisory Control and Data Acquisition) application that manages
tags (analog/digital inputs and outputs), raises and acknowledges alarms, records
value history, and persists everything to a database. Values come from a simulated
PLC; the default configuration models a **hydro power plant** unit.

Built with C# / .NET 10, WPF, and Entity Framework Core (SQLite). It is split into
three projects: **Scada.PlcSimulator** (simulated field values),
**Scada.DataConcentrator** (tags, alarms, scanning, logging, database), and
**Scada.Wpf** (the user interface).

## Features

**Core:** add/remove tags and alarms with validation, live values, red/yellow alarm
signaling with acknowledge, writing to outputs, on/off scanning, a timestamped
`system.log` audit log, and a `.txt` report.

**Four extra features:**
- **Role-based access** — login with roles; only Admin can write, others are read-only.
- **History charts** — live chart of an analog input with alarm lines and min/max/average.
- **Data filtering** — search the value history by tag / time / value and export to `.txt`.
- **Trace bits** — choose which log categories are written (stored as a numeric traceword).

## Running

Requires the **.NET 10 SDK**. From the project root:

```
dotnet run --project Scada.Wpf
```

Log in with the predefined administrator account:

- **Username:** `admin`
- **Password:** `Admin!Password1`

An admin can create additional users (and assign roles) from the **Users** button.
