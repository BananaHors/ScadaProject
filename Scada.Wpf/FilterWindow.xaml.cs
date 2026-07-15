using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Scada.DataConcentrator;

namespace Scada.Wpf;

public partial class FilterWindow : Window
{
    private readonly Scada.DataConcentrator.DataConcentrator _dc;
    private List<TagValue> _results = new();

    public FilterWindow(Scada.DataConcentrator.DataConcentrator dc)
    {
        InitializeComponent();
        _dc = dc;

        TagCombo.Items.Add("(any)");
        foreach (Tag tag in _dc.GetTags())
        {
            if (tag.Type == TagType.AI)
            {
                TagCombo.Items.Add(tag.Name);
            }
        }
        TagCombo.SelectedIndex = 0;
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        string? tag = TagCombo.SelectedItem as string;
        if (tag == "(any)")
        {
            tag = null;
        }

        DateTime? from = FromPicker.SelectedDate;
        // Include the whole "to" day, up to just before the next midnight.
        DateTime? to = ToPicker.SelectedDate?.Date.AddDays(1).AddTicks(-1);

        double? min = ParseNullable(MinBox.Text);
        double? max = ParseNullable(MaxBox.Text);

        _results = _dc.FilterTagValues(tag, from, to, min, max);
        ResultsGrid.ItemsSource = _results;
        StatusText.Text = $"{_results.Count} row(s)";
    }

    private void GenerateTxtButton_Click(object sender, RoutedEventArgs e)
    {
        if (_results.Count == 0)
        {
            StatusText.Text = "Nothing to export - run a search first.";
            return;
        }

        List<string> lines = new();
        lines.Add("Filtered AI history");
        lines.Add($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        lines.Add("");
        foreach (TagValue v in _results)
        {
            lines.Add($"{v.Timestamp:yyyy-MM-dd HH:mm:ss}  {v.TagName}  {v.Value:F2}");
        }

        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "scada-filter.txt");
        File.WriteAllText(path, string.Join(Environment.NewLine, lines));

        MessageBox.Show($"Exported {_results.Count} rows to:\n{path}", "Generate TXT",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private double? ParseNullable(string text)
    {
        if (double.TryParse(text, out double value))
        {
            return value;
        }
        return null;
    }
}
