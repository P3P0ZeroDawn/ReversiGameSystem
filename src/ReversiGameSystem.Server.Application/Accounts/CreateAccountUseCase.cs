using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>
/// Defines the create-account use-case contract.
/// </summary>
public interface ICreateAccountHandler
{
    /// <summary>
    /// Performs the execute async operation.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The result of the operation.</returns>
    Task<CreateAccountResult> ExecuteAsync(CreateAccountCommand command);
}

/// <summary>
/// Represents create account command.
/// </summary>
public sealed class CreateAccountCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAccountCommand"/> class.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="emailAddress">The email address.</param>
    /// <param name="password">The password.</param>
    /// <param name="preferredLocaleCode">The preferred locale code.</param>
    /// <param name="profileImage">The profile image.</param>
    public CreateAccountCommand(
        string username,
        string emailAddress,
        string password,
        string preferredLocaleCode,
        ProfileImageData? profileImage)
    {
        Username = username;
        EmailAddress = emailAddress;
        Password = password;
        PreferredLocaleCode = preferredLocaleCode;
        ProfileImage = profileImage;
    }

    /// <summary>
    /// Gets the username.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Gets the email address.
    /// </summary>
    public string EmailAddress { get; }

    /// <summary>
    /// Gets the password.
    /// </summary>
    public string Password { get; }

    /// <summary>
    /// Gets the preferred locale code.
    /// </summary>
    public string PreferredLocaleCode { get; }

    /// <summary>
    /// Gets the profile image.
    /// </summary>
    public ProfileImageData? ProfileImage { get; }
}

/// <summary>
/// Represents profile image data.
/// </summary>
public sealed class ProfileImageData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileImageData"/> class.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="mediaType">The media type.</param>
    public ProfileImageData(byte[] content, string mediaType)
    {
        Content = content;
        MediaType = mediaType;
    }

    /// <summary>
    /// Gets the content.
    /// </summary>
    public byte[] Content { get; }

    /// <summary>
    /// Gets the media type.
    /// </summary>
    public string MediaType { get; }
}

/// <summary>
/// Represents create account result.
/// </summary>
public sealed class CreateAccountResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAccountResult"/> class.
    /// </summary>
    /// <param name="outcome">The outcome.</param>
    /// <param name="accountId">The account id.</param>
    public CreateAccountResult(CreateAccountOutcome outcome, long? accountId = null)
    {
        Outcome = outcome;
        AccountId = accountId;
    }

    /// <summary>
    /// Gets the outcome.
    /// </summary>
    public CreateAccountOutcome Outcome { get; }

    /// <summary>
    /// Gets the account id.
    /// </summary>
    public long? AccountId { get; }
}

/// <summary>
/// Defines the supported create account outcome values.
/// </summary>
public enum CreateAccountOutcome
{
    /// <summary>
    /// Represents success.
    /// </summary>
    Success,
    /// <summary>
    /// Represents invalid request.
    /// </summary>
    InvalidRequest,
    /// <summary>
    /// Represents invalid username.
    /// </summary>
    InvalidUsername,
    /// <summary>
    /// Represents invalid email address.
    /// </summary>
    InvalidEmailAddress,
    /// <summary>
    /// Represents invalid password.
    /// </summary>
    InvalidPassword,
    /// <summary>
    /// Represents username already exists.
    /// </summary>
    UsernameAlreadyExists,
    /// <summary>
    /// Represents email address already exists.
    /// </summary>
    EmailAddressAlreadyExists,
    /// <summary>
    /// Represents service unavailable.
    /// </summary>
    ServiceUnavailable
}
