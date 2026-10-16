namespace ReversiGameSystem.Services;

/// <summary>
/// Represents the account-server response to saving a profile image.
/// </summary>
internal enum ProfileImageModificationResult
{
    Success,
    ServerConnectionFailed,
    PersistenceError
}

internal interface IProfileImageModificationService
{
    public ProfileImageModificationResult Save(string imagePath);
}

/// <summary>
/// Holds the service boundary until profile-image storage is implemented.
/// </summary>
internal sealed class ProfileImageModificationService : IProfileImageModificationService
{
    public ProfileImageModificationResult Save(string imagePath)
    {
        // TODO: Upload the validated image and replace the account's stored image atomically.
        return ProfileImageModificationResult.ServerConnectionFailed;
    }
}
