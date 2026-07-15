using System.Windows;
using Scada.DataConcentrator;

namespace Scada.Wpf;

public partial class LoginWindow : Window
{
    private readonly UserService _users = new();

    // Set to the logged-in user on success; stays null if closed without login.
    public User? AuthenticatedUser { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
        EnsureDefaultAdmin();
    }

    // Make sure the predefined admin exists. Username: admin  Password: Admin!Password1
    // (Does nothing if an 'admin' account already exists.)
    private void EnsureDefaultAdmin()
    {
        _users.Register("admin", "Admin!Password1", UserRole.Admin);
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";

        User? user = _users.Authenticate(UsernameBox.Text, PasswordBox.Password);
        if (user == null)
        {
            ErrorText.Text = "Invalid username or password.";
            return;
        }

        AuthenticatedUser = user;
        Close();
    }
}
