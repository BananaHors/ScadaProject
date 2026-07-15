using System.Windows;
using Scada.DataConcentrator;

namespace Scada.Wpf;

public partial class TraceWindow : Window
{
    private readonly Scada.DataConcentrator.DataConcentrator _dc;

    public TraceWindow(Scada.DataConcentrator.DataConcentrator dc)
    {
        InitializeComponent();
        _dc = dc;

        // Tick the boxes for whichever category bits are currently on.
        LogCategory tw = _dc.GetTraceWord();
        LoginBox.IsChecked = tw.HasFlag(LogCategory.Login);
        TagChangeBox.IsChecked = tw.HasFlag(LogCategory.TagChange);
        WriteBox.IsChecked = tw.HasFlag(LogCategory.Write);
        AckBox.IsChecked = tw.HasFlag(LogCategory.Acknowledge);
        AlarmBox.IsChecked = tw.HasFlag(LogCategory.Alarm);
        WarningBox.IsChecked = tw.HasFlag(LogCategory.Warning);
        ShowTraceWord(tw);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Combine the ticked boxes into one traceword using bitwise OR.
        LogCategory tw = 0;
        if (LoginBox.IsChecked == true) tw |= LogCategory.Login;
        if (TagChangeBox.IsChecked == true) tw |= LogCategory.TagChange;
        if (WriteBox.IsChecked == true) tw |= LogCategory.Write;
        if (AckBox.IsChecked == true) tw |= LogCategory.Acknowledge;
        if (AlarmBox.IsChecked == true) tw |= LogCategory.Alarm;
        if (WarningBox.IsChecked == true) tw |= LogCategory.Warning;

        _dc.SetTraceWord(tw);
        ShowTraceWord(tw);
    }

    private void ShowTraceWord(LogCategory tw)
    {
        TraceWordText.Text = $"Traceword = {(int)tw}";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
