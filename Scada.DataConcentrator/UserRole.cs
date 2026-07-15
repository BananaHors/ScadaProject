namespace Scada.DataConcentrator;

// The roles a user can have.
// Admin  - can configure (add/remove tags, manage users) and operate.
// Operator - can operate (write values, acknowledge alarms) but not configure.
public enum UserRole
{
    Admin,
    Operator
}
