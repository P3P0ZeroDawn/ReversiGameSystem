using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Accounts;

public interface ICreateAccountHandler
{
    Task<CreateAccountResult> ExecuteAsync(CreateAccountCommand command);
}

public sealed class CreateAccountCommand
{
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

    public string Username { get; }

    public string EmailAddress { get; }

    public string Password { get; }

    public string PreferredLocaleCode { get; }

    public ProfileImageData? ProfileImage { get; }
}

public sealed class ProfileImageData
{
    public ProfileImageData(byte[] content, string mediaType)
    {
        Content = content;
        MediaType = mediaType;
    }

    public byte[] Content { get; }

    public string MediaType { get; }
}

public sealed class CreateAccountResult
{
    public CreateAccountResult(CreateAccountOutcome outcome, long? accountId = null)
    {
        Outcome = outcome;
        AccountId = accountId;
    }

    public CreateAccountOutcome Outcome { get; }

    public long? AccountId { get; }
}

public enum CreateAccountOutcome
{
    Success,
    InvalidRequest,
    InvalidUsername,
    InvalidEmailAddress,
    InvalidPassword,
    UsernameAlreadyExists,
    EmailAddressAlreadyExists,
    ServiceUnavailable
}
