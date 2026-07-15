namespace Scada.DataConcentrator;

// The roles a user can have. Per the assignment, only Admin gets read/write;
// operator, student, and teacher are all read-only.
public enum UserRole
{
    Admin,
    Operator,
    Student,
    Teacher
}
