using System.Collections.Generic;
using System.Windows;
using Scada.DataConcentrator;

namespace Scada.Wpf;

public partial class WriteWindow : Window
{
    private readonly Scada.DataConcentrator.DataConcentrator _dc;

    public WriteWindow(Scada.DataConcentrator.DataConcentrator dc)
    {
        InitializeComponent();
        _dc = dc;

        // Only output tags can be written to.
        foreach (Tag tag in _dc.GetTags())
        {
            if (tag.Type == TagType.DO || tag.Type == TagType.AO)
            {
                TagCombo.Items.Add(tag.Name);
            }
        }
    }

    private void WriteButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";

        string? tagName = TagCombo.SelectedItem as string;
        if (tagName == null)
        {
            ErrorText.Text = "Please select an output tag.";
            return;
        }

        if (!double.TryParse(ValueBox.Text, out double value))
        {
            ErrorText.Text = "Value must be a number.";
            return;
        }

        List<string> errors = _dc.WriteToTag(tagName, value);
        if (errors.Count == 0)
        {
            Close();
        }
        else
        {
            ErrorText.Text = string.Join("\n", errors);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
