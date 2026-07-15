using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Scada.DataConcentrator;

namespace Scada.Wpf;

public partial class AlarmsWindow : Window
{
    private readonly Scada.DataConcentrator.DataConcentrator _dc;
    private readonly string _tagName;
    private readonly bool _canWrite;

    public AlarmsWindow(Scada.DataConcentrator.DataConcentrator dc, string tagName, bool canWrite)
    {
        InitializeComponent();
        _dc = dc;
        _tagName = tagName;
        _canWrite = canWrite;

        // Read-only users can view alarms but not acknowledge them.
        AcknowledgeButton.IsEnabled = canWrite;

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

        // Snapshots taken under the lock - the UI never touches live alarms.
        AlarmsGrid.ItemsSource = _dc.GetAlarmsSnapshot(_tagName);
    }

    private void AcknowledgeButton_Click(object sender, RoutedEventArgs e)
    {
        _dc.Acknowledge(_tagName);   // acknowledge all active alarms on this tag
        Refresh();
    }

    private void AckOneButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_canWrite)
        {
            return; // read-only users cannot acknowledge
        }

        // The clicked button's DataContext is the AlarmSnapshot on its row.
        Button button = (Button)sender;
        AlarmSnapshot alarm = (AlarmSnapshot)button.DataContext;
        _dc.AcknowledgeAlarm(alarm.Id);
        Refresh();
    }
}
