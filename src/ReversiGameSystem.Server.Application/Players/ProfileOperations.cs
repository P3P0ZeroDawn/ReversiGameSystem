using System;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Application.Players;

/// <summary>Persists changes to the authenticated public profile.</summary>
public interface IPlayerProfileEditorStore
{
    /// <summary>Checks that the requesting account remains active.</summary>
    /// <param name="accountId">The requesting account identifier.</param>
    /// <returns>Whether the account exists and remains active.</returns>
    public bool IsActive(long accountId);

    /// <summary>Commits an available username for an active account.</summary>
    /// <param name="accountId">The authenticated account identifier.</param>
    /// <param name="username">The normalized and validated public username.</param>
    /// <exception cref="ProfileRejectedException">The account or username is rejected.</exception>
    public void ChangeUsername(long accountId, string username);

    /// <summary>Commits an image reference for an active account.</summary>
    /// <param name="accountId">The authenticated account identifier.</param>
    /// <param name="imageReference">The server-owned image reference.</param>
    /// <exception cref="ProfileRejectedException">The account is inactive.</exception>
    public void SetProfileImage(long accountId, string imageReference);
}

/// <summary>Stores validated profile image content without exposing physical paths.</summary>
public interface IPlayerImageRepository
{
    /// <summary>Reads an existing image and reports stored-content failures explicitly.</summary>
    /// <param name="imageReference">The server-owned image reference.</param>
    /// <returns>The validated stored image content.</returns>
    /// <exception cref="System.IO.IOException">The stored image cannot be read.</exception>
    public ProfileImageData Read(string imageReference);

    /// <summary>Validates, recodes and stores an image, returning its internal reference.</summary>
    /// <param name="image">The proposed image content and media type.</param>
    /// <returns>The internal reference of the stored image.</returns>
    /// <exception cref="ProfileRejectedException">The proposed image is invalid.</exception>
    /// <exception cref="System.IO.IOException">The image cannot be stored.</exception>
    public string Save(ProfileImageData image);

    /// <summary>Removes a new image when its database association cannot be committed.</summary>
    /// <param name="imageReference">The unassociated server-owned image reference.</param>
    /// <exception cref="System.IO.IOException">The image cannot be removed.</exception>
    public void Delete(string imageReference);
}

/// <summary>Identifies an expected profile or session rejection.</summary>
public enum ProfileRejection
{
    /// <summary>The caller omitted the session token.</summary>
    MissingSessionToken,
    /// <summary>The session is unknown, expired, revoked or unauthenticated.</summary>
    InvalidSession,
    /// <summary>The requesting account is no longer active.</summary>
    InactiveAccount,
    /// <summary>The target identifier is not positive.</summary>
    InvalidAccountId,
    /// <summary>The requested public account does not exist.</summary>
    AccountNotFound,
    /// <summary>The proposed username violates the domain policy.</summary>
    InvalidUsername,
    /// <summary>Another account already owns the username.</summary>
    UsernameUnavailable,
    /// <summary>The proposed image is absent or invalid.</summary>
    InvalidImage
}

/// <summary>Reports an expected profile rejection without provider information.</summary>
public sealed class ProfileRejectedException : Exception
{
    /// <summary>Initializes an expected rejection.</summary>
    /// <param name="reason">The expected profile or session rejection.</param>
    public ProfileRejectedException(ProfileRejection reason)
        : base("The profile request was rejected.")
    {
        Reason = reason;
    }

    /// <summary>Gets the rejection reason.</summary>
    public ProfileRejection Reason { get; }
}
