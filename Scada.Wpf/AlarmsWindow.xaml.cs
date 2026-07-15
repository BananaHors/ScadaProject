using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Scada.DataConcentrator;

namespace Scada.Wpf;

public partial class AlarmsWindow : Window
{
    private readonly Scada.DataConcentrator.DataConcentrator _dc;
    private readonly string _tagName;

    public AlarmsWindow(Scada.DataConcentrator.DataConcentrator dc, string tagName)
    {
        InitializeComponent();
        _dc = dc;
        _tagName = tagName;
        Refresh();
    }

    private void Refresh()
    {
        Tag? tag = _dc.GetTags().FirstOrDefault(t => t.Name == _tagName);

        if (tag == null)
        {
            HeaderText.Text = $"Tag '{_tagName}' not found.";
            return;
        }

        HeaderText.Text = $"Alarms for {tag.Name}  (I/O address {tag.IoAddress})";

        // A fresh list each time so the grid re-reads the current alarm states.
        AlarmsGrid.ItemsSource = tag.Alarms.ToList();
    }

    private void AcknowledgeButton_Click(object sender, RoutedEventArgs e)
    {
        _dc.Acknowledge(_tagName);   // acknowledge all active alarms on this tag
        Refresh();
    }

    private void AckOneButton_Click(object sender, RoutedEventArgs e)
    {
        // The clicked button's DataContext is the Alarm on its row.
        Button button = (Button)sender;
        Alarm alarm = (Alarm)button.DataContext;
        _dc.AcknowledgeAlarm(alarm);
        Refresh();
    }
}
