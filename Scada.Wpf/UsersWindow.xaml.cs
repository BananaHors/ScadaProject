using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Scada.DataConcentrator;

namespace Scada.Wpf;

public partial class UsersWindow : Window
{
    private readonly UserService _users = new();

    public UsersWindow()
    {
        InitializeComponent();
        RoleCombo.SelectedIndex = 1; // default to Operator
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        // Combo items are in the same order as the UserRole enum.
        UserRole role = (UserRole)RoleCombo.SelectedIndex;

        List<string> errors = _users.Register(UsernameBox.Text, PasswordBox.Password, role);
        if (errors.Count == 0)
        {
            ResultText.Foreground = Brushes.Green;
            ResultText.Text = $"User '{UsernameBox.Text}' created.";
            UsernameBox.Clear();
            PasswordBox.Clear();
        }
        else
        {
            ResultText.Foreground = Brushes.Red;
            ResultText.Text = string.Join("\n", errors);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
