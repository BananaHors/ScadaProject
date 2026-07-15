namespace Scada.DataConcentrator;

// The roles a user can have. Admin gets read/write; the others are read-only.
public enum UserRole
{
    Admin,
    Operator,
    Student,
    Teacher
}
