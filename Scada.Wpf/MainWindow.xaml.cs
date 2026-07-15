using System;
using System.Collections.Generic;
using System.Windows;
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

        _dc.AddTag(new Tag { Name = "BUS1_V", Type = TagType.AI, IoAddress = 40001, Units = "kV", OnOffScan = true, LowLimit = 104.5, HighLimit = 115.5 });
        _dc.AddTag(new Tag { Name = "GRID_FREQ", Type = TagType.AI, IoAddress = 40002, Units = "Hz", OnOffScan = true, LowLimit = 49.5, HighLimit = 50.5 });
        _dc.AddTag(new Tag { Name = "LINE1_I", Type = TagType.AI, IoAddress = 40003, Units = "A", OnOffScan = true, LowLimit = 0, HighLimit = 630 });
    }

    private void RefreshGrid()
    {
        List<TagDisplay> rows = new();

        foreach (Tag tag in _dc.GetTags())
        {
            rows.Add(new TagDisplay
            {
                Name = tag.Name,
                Type = tag.Type.ToString(),
                Address = tag.IoAddress.ToString(),
                Value = _dc.GetCurrentValue(tag.Name).ToString("F2"),
                Units = tag.Units ?? ""
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
}

// A simple row for the grid - just the columns we want to show.
public class TagDisplay
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Address { get; set; } = "";
    public string Value { get; set; } = "";
    public string Units { get; set; } = "";
}
