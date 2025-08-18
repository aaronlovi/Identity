namespace Identity.Grains;

/// <summary>
/// Error codes for admin operations.
/// </summary>
public static class AdminErrorCodes
{
    public const int Success = 0;
    public const int UserNotFound = 1001;
    public const int InvalidRequest = 1002;
    public const int DatabaseError = 1003;
    public const int FirebaseError = 1004;
    public const int UnknownError = 1999;
}
