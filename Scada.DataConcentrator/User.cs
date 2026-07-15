namespace Scada.DataConcentrator;

// A user account for logging in. We never store the raw password - only a
// salted hash of it (built and checked in UserService).
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Salt { get; set; } = "";
    public UserRole Role { get; set; }
}
