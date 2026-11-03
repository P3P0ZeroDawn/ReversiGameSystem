using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Players;

/// <summary>Stores validated profile image content without exposing physical paths.</summary>
public interface IPlayerImageRepository
{
    /// <summary>Reads an existing image and reports stored-content failures explicitly.</summary>
    /// <param name="imageReference">The server-owned image reference.</param>
    /// <returns>The validated stored image content.</returns>
    /// <exception cref="System.IO.IOException">The stored image cannot be read.</exception>
    public ProfileImageData Read(string imageReference);

    /// <summary>Validates, recodes and stores an image when its content is accepted.</summary>
    /// <param name="image">The proposed image content and media type.</param>
    /// <param name="imageReference">The internal reference when storage succeeds.</param>
    /// <returns>Whether the image was valid and stored.</returns>
    /// <exception cref="System.IO.IOException">The image cannot be stored.</exception>
    public bool TrySave(ProfileImageData image, out string imageReference);

    /// <summary>Removes a new image when its database association cannot be committed.</summary>
    /// <param name="imageReference">The unassociated server-owned image reference.</param>
    /// <exception cref="System.IO.IOException">The image cannot be removed.</exception>
    public void Delete(string imageReference);
}
