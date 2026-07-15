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

    public MainWindow()
    {
        InitializeComponent();

        // Create the Data Concentrator (with a file logger) and make sure there
        // are some tags to look at the first time the app runs.
        _dc = new Scada.DataConcentrator.DataConcentrator(new FileLogger("system.log"));
        SeedSampleTagsIfEmpty();

        // Refresh the grid once per second so the values stay current on screen.
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (sender, e) => RefreshGrid();
        _timer.Start();

        RefreshGrid();
    }

    private void SeedSampleTagsIfEmpty()
    {
        if (_dc.GetTags().Count > 0)
        {
            return;
        }

        _dc.AddTag(new Tag { Name = "BUS1_V", Type = TagType.AI, IoAddress = 30001, Units = "kV", OnOffScan = true, LowLimit = 104.5, HighLimit = 115.5 });
        _dc.AddTag(new Tag { Name = "GRID_FREQ", Type = TagType.AI, IoAddress = 30002, Units = "Hz", OnOffScan = true, LowLimit = 49.5, HighLimit = 50.5 });
        _dc.AddTag(new Tag { Name = "LINE1_I", Type = TagType.AI, IoAddress = 30003, Units = "A", OnOffScan = true, LowLimit = 0, HighLimit = 630 });
    }

    private void RefreshGrid()
    {
        List<TagDisplay> rows = new();

        foreach (Tag tag in _dc.GetTags())
        {
            // Worst alarm state on this tag drives its row colour.
            string alarmStatus = "Normal";
            if (tag.Alarms.Any(a => a.State == AlarmState.Active))
            {
                alarmStatus = "Active";
            }
            else if (tag.Alarms.Any(a => a.State == AlarmState.Acknowledged))
            {
                alarmStatus = "Acknowledged";
            }

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

    private void ReportButton_Click(object sender, RoutedEventArgs e)
    {
        string report = _dc.GenerateReport();

        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "scada-report.txt");
        File.WriteAllText(path, report);

        MessageBox.Show($"Report saved to:\n{path}", "Report",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DetailsButton_Click(object sender, RoutedEventArgs e)
    {
        // The clicked button's DataContext is the TagDisplay row it sits in.
        Button button = (Button)sender;
        TagDisplay row = (TagDisplay)button.DataContext;

        AlarmsWindow window = new(_dc, row.Name);
        window.Owner = this;
        window.ShowDialog();
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
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
