using System.Security.Cryptography;

namespace Scada.DataConcentrator;

// Handles user registration and login, storing only salted password hashes.
public class UserService
{
    private const int SaltSize = 16;        // bytes of random salt
    private const int HashSize = 32;        // bytes of derived hash
    private const int Iterations = 100_000; // PBKDF2 rounds (deliberately slow)

    // Register a new user. Empty list = success; otherwise the reasons it failed.
    public List<string> Register(string username, string password, UserRole role)
    {
        List<string> errors = ValidatePassword(password);

        using var db = new ScadaDbContext();

        if (string.IsNullOrWhiteSpace(username))
        {
            errors.Add("Username is required.");
        }
        else if (db.Users.Any(u => u.Username == username))
        {
            errors.Add($"A user named '{username}' already exists.");
        }

        // The assignment forbids two accounts sharing the same password.
        if (PasswordAlreadyUsed(db, password))
        {
            errors.Add("That password is already used by another account.");
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        string salt = GenerateSalt();
        db.Users.Add(new User
        {
            Username = username,
            Salt = salt,
            PasswordHash = Hash(password, salt),
            Role = role
        });
        db.SaveChanges();
        

        return errors; // empty
    }

    // Check a login. Returns the matching user, or null if it fails.
    public User? Authenticate(string username, string password)
    {
        using var db = new ScadaDbContext();
        User? user = db.Users.FirstOrDefault(u => u.Username == username);

        if (user == null)
        {
            return null;
        }

        if (Hash(password, user.Salt) == user.PasswordHash)
        {
            return user;
        }

        return null;
    }

    // List all accounts (for the admin's Users window).
    public List<User> GetUsers()
    {
        using var db = new ScadaDbContext();
        return db.Users.OrderBy(u => u.Username).ToList();
    }

    // Delete a user. Empty list = success; otherwise the reason it was refused.
    public List<string> DeleteUser(string username)
    {
        List<string> errors = new();

        using var db = new ScadaDbContext();
        User? user = db.Users.FirstOrDefault(u => u.Username == username);

        if (user == null)
        {
            errors.Add($"No user named '{username}'.");
            return errors;
        }

        // Never delete the last admin - that would lock everyone out of admin.
        if (user.Role == UserRole.Admin && db.Users.Count(u => u.Role == UserRole.Admin) <= 1)
        {
            errors.Add("Cannot delete the last admin account.");
            return errors;
        }

        db.Users.Remove(user);
        db.SaveChanges();
        return errors;
    }

    // The assignment's password rules.
    public List<string> ValidatePassword(string password)
    {
        List<string> errors = new();

        if (password.Length < 15)
        {
            errors.Add("Password must be at least 15 characters.");
        }
        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password must contain an uppercase letter.");
        }
        if (!password.Any(char.IsLower))
        {
            errors.Add("Password must contain a lowercase letter.");
        }
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
        {
            errors.Add("Password must contain a special character.");
        }

        return errors;
    }

    private bool PasswordAlreadyUsed(ScadaDbContext db, string password)
    {
        // With per-user salts we can't compare hashes directly, so we hash the
        // candidate with each existing user's salt and look for a match.
        foreach (User u in db.Users.ToList())
        {
            if (Hash(password, u.Salt) == u.PasswordHash)
            {
                return true;
            }
        }

        return false;
    }

    private string GenerateSalt()
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        return Convert.ToBase64String(salt);
    }

    private string Hash(string password, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password, saltBytes, Iterations, HashAlgorithmName.SHA256, HashSize);
        return Convert.ToBase64String(hash);
    }
}
