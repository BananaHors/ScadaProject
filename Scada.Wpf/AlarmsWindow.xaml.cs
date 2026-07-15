using System.Linq;
using System.Windows;
using Scada.DataConcentrator;

namespace Scada.Wpf;

public partial class AlarmsWindow : Window
{
    public AlarmsWindow(Scada.DataConcentrator.DataConcentrator dc, string tagName)
    {
        InitializeComponent();

        Tag? tag = dc.GetTags().FirstOrDefault(t => t.Name == tagName);

        if (tag == null)
        {
            HeaderText.Text = $"Tag '{tagName}' not found.";
            return;
        }

        HeaderText.Text = $"Alarms for {tag.Name}  (I/O address {tag.IoAddress})";
        AlarmsGrid.ItemsSource = tag.Alarms; // bind straight to the tag's alarm list
    }
}
