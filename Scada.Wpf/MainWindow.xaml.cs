using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Scada.DataConcentrator;

namespace Scada.Wpf;

public partial class MainWindow : Window
{
    // Our class and its namespace are both called "DataConcentrator", so we
    // write the type fully-qualified here to be unambiguous.
    private readonly Scada.DataConcentrator.DataConcentrator _dc;
    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _inactivityTimer;
    private readonly User _currentUser;
    private readonly bool _isAdmin;

    // True when the window closed because the user logged out (vs. quit the app).
    public bool LogoutRequested { get; private set; }

    public MainWindow(Scada.DataConcentrator.DataConcentrator dc, User user)
    {
        InitializeComponent();

        _dc = dc;
        _currentUser = user;
        Title = $"SCADA - Substation Monitor   [{user.Username} / {user.Role}]";

        // Per the assignment, only Admin gets read/write; every other role is
        // read-only (can view but not add/remove/write/acknowledge/configure).
        _isAdmin = user.Role == UserRole.Admin;
        if (!_isAdmin)
        {
            AddButton.IsEnabled = false;
            WriteButton.IsEnabled = false;
            UsersButton.Visibility = Visibility.Collapsed;
            LogsButton.Visibility = Visibility.Collapsed;
        }

        SeedSampleTagsIfEmpty();

        // Refresh the grid once per second so the values stay current on screen.
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (sender, e) => RefreshGrid();
        _timer.Start();

        // Log out automatically after 5 minutes with no mouse/keyboard activity.
        _inactivityTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        _inactivityTimer.Tick += (sender, e) => Logout();
        _inactivityTimer.Start();

        // Any input resets the inactivity countdown.
        PreviewMouseMove += (sender, e) => ResetInactivity();
        PreviewMouseDown += (sender, e) => ResetInactivity();
        PreviewKeyDown += (sender, e) => ResetInactivity();

        RefreshGrid();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        Logout();
    }

    private void Logout()
    {
        LogoutRequested = true;
        Close(); // returns control to App, which shows the login screen again
    }

    private void ResetInactivity()
    {
        _inactivityTimer.Stop();
        _inactivityTimer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Stop our timers so they don't keep firing on a closed window.
        _timer.Stop();
        _inactivityTimer.Stop();
        base.OnClosed(e);
    }

    private void SeedSampleTagsIfEmpty()
    {
        if (_dc.GetTags().Count > 0)
        {
            return;
        }

        // --- Analog inputs (3xxxx) - a hydro power plant unit ---
        _dc.AddTag(new Tag { Name = "GEN_ACTIVE_POWER",  Type = TagType.AI, IoAddress = 30001, Description = "Generator active power",   Units = "MW",   OnOffScan = true, ScanTime = 1000, LowLimit = 5,    HighLimit = 55 });
        _dc.AddTag(new Tag { Name = "GEN_REACTIVE_POWER",Type = TagType.AI, IoAddress = 30002, Description = "Generator reactive power", Units = "MVAr", OnOffScan = true, ScanTime = 1000, LowLimit = -25,  HighLimit = 25 });
        _dc.AddTag(new Tag { Name = "GEN_VOLTAGE",       Type = TagType.AI, IoAddress = 30003, Description = "Generator voltage",        Units = "kV",   OnOffScan = true, ScanTime = 1000, LowLimit = 9.9,  HighLimit = 11.1 });
        _dc.AddTag(new Tag { Name = "GEN_CURRENT",       Type = TagType.AI, IoAddress = 30004, Description = "Generator current",        Units = "A",    OnOffScan = true, ScanTime = 1000, LowLimit = 0,    HighLimit = 3200 });
        _dc.AddTag(new Tag { Name = "GRID_FREQUENCY",    Type = TagType.AI, IoAddress = 30005, Description = "Grid frequency",           Units = "Hz",   OnOffScan = true, ScanTime = 1000, LowLimit = 49.5, HighLimit = 50.5, Deadband = 0.02 });
        _dc.AddTag(new Tag { Name = "TURBINE_SPEED",     Type = TagType.AI, IoAddress = 30006, Description = "Turbine speed",            Units = "RPM",  OnOffScan = true, ScanTime = 1000, LowLimit = 90,   HighLimit = 115 });
        _dc.AddTag(new Tag { Name = "HEADWATER_LEVEL",   Type = TagType.AI, IoAddress = 30007, Description = "Upstream water level",     Units = "m",    OnOffScan = true, ScanTime = 5000, LowLimit = 295,  HighLimit = 320 });
        _dc.AddTag(new Tag { Name = "TAILWATER_LEVEL",   Type = TagType.AI, IoAddress = 30008, Description = "Downstream water level",   Units = "m",    OnOffScan = true, ScanTime = 5000, LowLimit = 250,  HighLimit = 262 });
        _dc.AddTag(new Tag { Name = "WATER_FLOW",        Type = TagType.AI, IoAddress = 30009, Description = "Turbine discharge",        Units = "m3/s", OnOffScan = true, ScanTime = 5000, LowLimit = 20,   HighLimit = 120 });
        _dc.AddTag(new Tag { Name = "STATOR_TEMP",       Type = TagType.AI, IoAddress = 30010, Description = "Generator stator temp",    Units = "C",    OnOffScan = true, ScanTime = 5000, LowLimit = 20,   HighLimit = 120 });
        _dc.AddTag(new Tag { Name = "TRAFO_OIL_TEMP",    Type = TagType.AI, IoAddress = 30011, Description = "Transformer oil temp",     Units = "C",    OnOffScan = true, ScanTime = 5000, LowLimit = 20,   HighLimit = 85 });

        // --- Digital inputs (1xxxx) ---
        _dc.AddTag(new Tag { Name = "GEN_BREAKER_CLOSED", Type = TagType.DI, IoAddress = 10001, Description = "Generator breaker closed", OnOffScan = true, ScanTime = 1000 });
        _dc.AddTag(new Tag { Name = "TURBINE_RUNNING",    Type = TagType.DI, IoAddress = 10002, Description = "Turbine running",          OnOffScan = true, ScanTime = 1000 });
        _dc.AddTag(new Tag { Name = "EMERGENCY_STOP",     Type = TagType.DI, IoAddress = 10003, Description = "Emergency stop active",    OnOffScan = true, ScanTime = 1000 });
        _dc.AddTag(new Tag { Name = "GOVERNOR_FAULT",     Type = TagType.DI, IoAddress = 10004, Description = "Governor fault",           OnOffScan = true, ScanTime = 1000 });
        _dc.AddTag(new Tag { Name = "COOLING_WATER_OK",   Type = TagType.DI, IoAddress = 10005, Description = "Cooling water flow OK",     OnOffScan = true, ScanTime = 1000 });

        // --- Digital outputs (0xxxx) ---
        _dc.AddTag(new Tag { Name = "BREAKER_TRIP_CMD",   Type = TagType.DO, IoAddress = 1, Description = "Generator breaker trip command", InitialValue = 0 });
        _dc.AddTag(new Tag { Name = "START_TURBINE_CMD",  Type = TagType.DO, IoAddress = 2, Description = "Start turbine command",          InitialValue = 0 });

        // --- Analog outputs (4xxxx) ---
        _dc.AddTag(new Tag { Name = "GATE_SETPOINT",  Type = TagType.AO, IoAddress = 40001, Description = "Wicket gate opening setpoint", Units = "%",  InitialValue = 50 });
        _dc.AddTag(new Tag { Name = "POWER_SETPOINT", Type = TagType.AO, IoAddress = 40002, Description = "Generator power setpoint",      Units = "MW", InitialValue = 30 });

        // --- A few alarms (frequency ones fire during normal operation) ---
        _dc.AddAlarm("GRID_FREQUENCY", new Alarm { Threshold = 50.5, Direction = AlarmDirection.Above, Message = "Overfrequency" });
        _dc.AddAlarm("GRID_FREQUENCY", new Alarm { Threshold = 49.5, Direction = AlarmDirection.Below, Message = "Underfrequency" });
        _dc.AddAlarm("GEN_VOLTAGE", new Alarm { Threshold = 11.1, Direction = AlarmDirection.Above, Message = "Generator overvoltage" });
        _dc.AddAlarm("STATOR_TEMP", new Alarm { Threshold = 120, Direction = AlarmDirection.Above, Message = "Stator overtemperature" });
        _dc.AddAlarm("TURBINE_SPEED", new Alarm { Threshold = 115, Direction = AlarmDirection.Above, Message = "Turbine overspeed" });
    }

    private void RefreshGrid()
    {
        List<TagDisplay> rows = new();

        foreach (Tag tag in _dc.GetTags())
        {
            // Worst alarm state on this tag drives its row colour (read under
            // the concentrator's lock, not from the live alarm objects).
            string alarmStatus = _dc.GetAlarmStatus(tag.Name);

            rows.Add(new TagDisplay
            {
                Name = tag.Name,
                Type = tag.Type.ToString(),
                Address = tag.IoAddress.ToString(),
                Value = _dc.GetCurrentValue(tag.Name).ToString("F2"),
                Units = tag.Units ?? "",
                AlarmStatus = alarmStatus
            });
        }

        TagsGrid.ItemsSource = rows;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        AddWindow window = new(_dc);
        window.Owner = this;
        window.ShowDialog();   // modal: waits here until the Add window is closed
        RefreshGrid();         // refresh in case a tag was added
    }

    private void WriteButton_Click(object sender, RoutedEventArgs e)
    {
        WriteWindow window = new(_dc);
        window.Owner = this;
        window.ShowDialog();
        RefreshGrid();
    }

    private void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        FilterWindow window = new(_dc);
        window.Owner = this;
        window.ShowDialog();
    }

    private void HistoryButton_Click(object sender, RoutedEventArgs e)
    {
        HistoryWindow window = new(_dc);
        window.Owner = this;
        window.ShowDialog();
    }

    private void UsersButton_Click(object sender, RoutedEventArgs e)
    {
        UsersWindow window = new(_currentUser.Username);
        window.Owner = this;
        window.ShowDialog();
    }

    private void LogsButton_Click(object sender, RoutedEventArgs e)
    {
        TraceWindow window = new(_dc);
        window.Owner = this;
        window.ShowDialog();
    }

    private void ReportButton_Click(object sender, RoutedEventArgs e)
    {
        string report = _dc.GenerateReport();

        // Save into the app's working directory (the project folder when run
        // via `dotnet run` from the project root).
        string path = Path.Combine(Directory.GetCurrentDirectory(), "scada-report.txt");
        File.WriteAllText(path, report);

        MessageBox.Show($"Report saved to:\n{path}", "Report",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DetailsButton_Click(object sender, RoutedEventArgs e)
    {
        // The clicked button's DataContext is the TagDisplay row it sits in.
        Button button = (Button)sender;
        TagDisplay row = (TagDisplay)button.DataContext;

        AlarmsWindow window = new(_dc, row.Name, _isAdmin);
        window.Owner = this;
        window.ShowDialog();
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isAdmin)
        {
            return; // only admins can remove tags
        }

        Button button = (Button)sender;
        TagDisplay row = (TagDisplay)button.DataContext;

        MessageBoxResult result = MessageBox.Show(
            $"Remove tag '{row.Name}'? This cannot be undone.",
            "Confirm remove",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _dc.RemoveTag(row.Name);
            RefreshGrid();
        }
    }
}

// A simple row for the grid - just the columns we want to show.
public class TagDisplay
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Address { get; set; } = "";
    public string Value { get; set; } = "";
    public string Units { get; set; } = "";
    public string AlarmStatus { get; set; } = "Normal";
}
