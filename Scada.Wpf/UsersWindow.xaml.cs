using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Scada.DataConcentrator;

namespace Scada.Wpf;

public partial class UsersWindow : Window
{
    private readonly UserService _users = new();
    private readonly string _currentUsername;

    public UsersWindow(string currentUsername)
    {
        InitializeComponent();
        _currentUsername = currentUsername;
        RoleCombo.SelectedIndex = 1; // default to Operator
        RefreshList();
    }

    private void RefreshList()
    {
        UsersGrid.ItemsSource = _users.GetUsers();
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        UserRole role = (UserRole)RoleCombo.SelectedIndex;

        List<string> errors = _users.Register(UsernameBox.Text, PasswordBox.Password, role);
        if (errors.Count == 0)
        {
            ResultText.Foreground = Brushes.Green;
            ResultText.Text = $"User '{UsernameBox.Text}' created.";
            UsernameBox.Clear();
            PasswordBox.Clear();
            RefreshList();
        }
        else
        {
            ResultText.Foreground = Brushes.Red;
            ResultText.Text = string.Join("\n", errors);
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        Button button = (Button)sender;
        User user = (User)button.DataContext;

        if (user.Username == _currentUsername)
        {
            ResultText.Foreground = Brushes.Red;
            ResultText.Text = "You cannot delete the account you are logged in as.";
            return;
        }

        MessageBoxResult result = MessageBox.Show(
            $"Delete user '{user.Username}'?", "Confirm delete",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        List<string> errors = _users.DeleteUser(user.Username);
        if (errors.Count == 0)
        {
            ResultText.Foreground = Brushes.Green;
            ResultText.Text = $"User '{user.Username}' deleted.";
            RefreshList();
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
