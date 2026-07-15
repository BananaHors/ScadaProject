using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Scada.DataConcentrator;

namespace Scada.Wpf;

public partial class AddWindow : Window
{
    private readonly Scada.DataConcentrator.DataConcentrator _dc;

    public AddWindow(Scada.DataConcentrator.DataConcentrator dc)
    {
        InitializeComponent();
        _dc = dc;

        // Fill the alarm target list with the names of existing AI tags.
        foreach (Tag tag in _dc.GetTags())
        {
            if (tag.Type == TagType.AI)
            {
                AlarmTagCombo.Items.Add(tag.Name);
            }
        }

        TypeCombo.SelectedIndex = 0; // default to AI; this also triggers the first reshape
    }

    private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateVisibility();
    }

    // Show only the field groups that apply to the currently selected type.
    private void UpdateVisibility()
    {
        string type = SelectedType();

        bool isTag = type != "Alarm";
        bool isAnalog = type == "AI" || type == "AO";
        bool isInput = type == "AI" || type == "DI";
        bool isOutput = type == "AO" || type == "DO";

        TagCommonPanel.Visibility = Show(isTag);
        InputPanel.Visibility = Show(isInput);
        AnalogPanel.Visibility = Show(isAnalog);
        OutputPanel.Visibility = Show(isOutput);
        AiPanel.Visibility = Show(type == "AI");
        AlarmPanel.Visibility = Show(type == "Alarm");
    }

    private Visibility Show(bool visible)
    {
        if (visible)
        {
            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    // Read the text of the currently selected item in the Type combo box.
    private string SelectedType()
    {
        ComboBoxItem item = (ComboBoxItem)TypeCombo.SelectedItem;
        return (string)item.Content;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // Live validation: runs when focus leaves the field.
    private void NameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            NameError.Text = "- name is required";
        }
        else
        {
            NameError.Text = "";
        }
    }

    private void AddressBox_LostFocus(object sender, RoutedEventArgs e)
    {
        // "out _" discards the parsed value - we only care whether it parses.
        if (int.TryParse(AddressBox.Text, out _))
        {
            AddressError.Text = "";
        }
        else
        {
            AddressError.Text = "- must be a whole number";
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        string type = SelectedType();

        List<string> errors;
        if (type == "Alarm")
        {
            errors = TryAddAlarm();
        }
        else
        {
            errors = TryAddTag(type);
        }

        if (errors.Count == 0)
        {
            Close(); // success
        }
        else
        {
            // Show EVERY unmet condition, one per line.
            ErrorText.Text = string.Join("\n", errors);
        }
    }

    private List<string> TryAddTag(string type)
    {
        List<string> errors = new();

        // --- Validate the raw input first ---
        if (!int.TryParse(AddressBox.Text, out int address))
        {
            errors.Add("I/O Address must be a whole number.");
        }

        bool isAnalog = type == "AI" || type == "AO";
        bool isInput = type == "AI" || type == "DI";
        bool isOutput = type == "AO" || type == "DO";
        bool isAi = type == "AI";

        int? scanTime = null;
        double? lowLimit = null, highLimit = null, initialValue = null, deadband = null, hysteresis = null;

        if (isInput)
        {
            scanTime = ParseOptionalInt(ScanTimeBox.Text, "Scan Time", errors);
        }
        if (isAnalog)
        {
            lowLimit = ParseOptionalDouble(LowLimitBox.Text, "Low Limit", errors);
            highLimit = ParseOptionalDouble(HighLimitBox.Text, "High Limit", errors);
        }
        if (isOutput)
        {
            initialValue = ParseOptionalDouble(InitialValueBox.Text, "Initial Value", errors);
        }
        if (isAi)
        {
            deadband = ParseOptionalDouble(DeadbandBox.Text, "Deadband", errors);
            hysteresis = ParseOptionalDouble(HysteresisBox.Text, "Hysteresis", errors);
        }

        // If any input couldn't be read, stop here and report those.
        if (errors.Count > 0)
        {
            return errors;
        }

        // --- Build the tag ---
        Tag tag = new()
        {
            Name = NameBox.Text,
            Description = DescriptionBox.Text,
            Type = Enum.Parse<TagType>(type),
            IoAddress = address
        };

        if (isInput)
        {
            tag.ScanTime = scanTime;
            tag.OnOffScan = OnOffScanBox.IsChecked == true;
        }
        if (isAnalog)
        {
            if (!string.IsNullOrWhiteSpace(UnitsBox.Text))
            {
                tag.Units = UnitsBox.Text;
            }
            tag.LowLimit = lowLimit;
            tag.HighLimit = highLimit;
        }
        if (isOutput)
        {
            tag.InitialValue = initialValue;
        }
        if (isAi)
        {
            tag.Deadband = deadband;
            tag.Hysteresis = hysteresis;
        }

        // Domain validation (name required, limits, duplicate name...) happens here.
        List<string> addErrors = _dc.AddTag(tag);
        if (addErrors.Count > 0)
        {
            return addErrors;
        }

        // Optionally auto-create high/low alarms from the limits (AI only).
        if (isAi && CreateAlarmsBox.IsChecked == true)
        {
            if (tag.HighLimit.HasValue)
            {
                _dc.AddAlarm(tag.Name, new Alarm
                {
                    Threshold = tag.HighLimit.Value,
                    Direction = AlarmDirection.Above,
                    Message = $"{tag.Name} above high limit"
                });
            }

            if (tag.LowLimit.HasValue)
            {
                _dc.AddAlarm(tag.Name, new Alarm
                {
                    Threshold = tag.LowLimit.Value,
                    Direction = AlarmDirection.Below,
                    Message = $"{tag.Name} below low limit"
                });
            }
        }

        return addErrors;
    }

    private List<string> TryAddAlarm()
    {
        List<string> errors = new();

        string? tagName = AlarmTagCombo.SelectedItem as string;
        if (tagName == null)
        {
            errors.Add("Please select a target AI tag.");
        }
        if (DirectionCombo.SelectedIndex < 0)
        {
            errors.Add("Please select a direction (Above or Below).");
        }
        if (!double.TryParse(ThresholdBox.Text, out double threshold))
        {
            errors.Add("Threshold must be a number.");
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        AlarmDirection direction = AlarmDirection.Above;
        if (DirectionCombo.SelectedIndex == 1)
        {
            direction = AlarmDirection.Below;
        }

        Alarm alarm = new()
        {
            Threshold = threshold,
            Direction = direction,
            Message = AlarmMessageBox.Text
        };

        // tagName can't be null here (we returned above if it was), so tell the
        // compiler that with the "!" null-forgiving operator.
        return _dc.AddAlarm(tagName!, alarm);
    }

    // Empty text -> null (not set). Non-empty but invalid -> records an error.
    private int? ParseOptionalInt(string text, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        if (int.TryParse(text, out int value))
        {
            return value;
        }
        errors.Add($"{fieldName} must be a whole number.");
        return null;
    }

    private double? ParseOptionalDouble(string text, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        if (double.TryParse(text, out double value))
        {
            return value;
        }
        errors.Add($"{fieldName} must be a number.");
        return null;
    }
}
