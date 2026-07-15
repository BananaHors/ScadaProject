using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Scada.DataConcentrator;

namespace Scada.Wpf;

public partial class HistoryWindow : Window
{
    private readonly Scada.DataConcentrator.DataConcentrator _dc;
    private readonly DispatcherTimer _timer;

    public HistoryWindow(Scada.DataConcentrator.DataConcentrator dc)
    {
        InitializeComponent();
        _dc = dc;

        foreach (Tag tag in _dc.GetTags())
        {
            if (tag.Type == TagType.AI)
            {
                TagCombo.Items.Add(tag.Name);
            }
        }

        if (TagCombo.Items.Count > 0)
        {
            TagCombo.SelectedIndex = 0; // triggers the first load
        }

        // Reload the chart every 2 seconds so it keeps up with new readings.
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (sender, e) => LoadChart();
        _timer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        base.OnClosed(e);
    }

    private void TagCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadChart();
    }

    private void LoadChart()
    {
        string? tagName = TagCombo.SelectedItem as string;
        if (tagName == null)
        {
            return;
        }

        // Reuse the filter query with no filters = the tag's whole history.
        List<TagValue> history = _dc.FilterTagValues(tagName, null, null, null, null);

        Plot.Plot.Clear();

        if (history.Count == 0)
        {
            StatsText.Text = "No history yet for this tag.";
            Plot.Refresh();
            return;
        }

        double[] xs = history.Select(v => v.Timestamp.ToOADate()).ToArray();
        double[] ys = history.Select(v => v.Value).ToArray();

        Plot.Plot.Add.Scatter(xs, ys);
        Plot.Plot.Axes.DateTimeTicksBottom(); // show the X axis as dates/times

        // A horizontal line for each alarm threshold on this tag.
        Tag? tag = _dc.GetTags().FirstOrDefault(t => t.Name == tagName);
        if (tag != null)
        {
            foreach (Alarm alarm in tag.Alarms)
            {
                Plot.Plot.Add.HorizontalLine(alarm.Threshold);
            }
        }

        StatsText.Text = $"Min {ys.Min():F2}     Max {ys.Max():F2}     Average {ys.Average():F2}";
        Plot.Plot.Axes.AutoScale(); // fit the view to the (growing) data
        Plot.Refresh();
    }
}
