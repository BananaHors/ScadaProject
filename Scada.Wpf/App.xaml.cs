using System.Windows;
using Microsoft.EntityFrameworkCore;
using Scada.DataConcentrator;

namespace Scada.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // We control shutdown ourselves (login/main windows come and go).
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Make sure the database exists and is up to date before anything uses it.
        using (var db = new ScadaDbContext())
        {
            db.Database.Migrate();
        }

        // One concentrator for the whole app life - reused across logins.
        var dc = new Scada.DataConcentrator.DataConcentrator(new FileLogger("system.log"));

        // Loop: log in -> use the app -> (log out -> log in again) or quit.
        while (true)
        {
            LoginWindow login = new();
            login.ShowDialog();

            if (login.AuthenticatedUser == null)
            {
                Shutdown(); // closed the login without logging in
                return;
            }

            User user = login.AuthenticatedUser;
            dc.Log(LogLevel.Info, LogCategory.Login, $"User '{user.Username}' ({user.Role}) logged in.");

            MainWindow main = new(dc, user);
            main.ShowDialog(); // modal: blocks until the main window closes

            dc.Log(LogLevel.Info, LogCategory.Login, $"User '{user.Username}' logged out.");

            if (!main.LogoutRequested)
            {
                Shutdown(); // closed via the window's X, not the Logout button
                return;
            }
            // otherwise: they logged out - loop back to the login screen
        }
    }
}
