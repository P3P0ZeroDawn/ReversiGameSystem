using System;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;
using ApplicationSession =
    ReversiGameSystem.Server.Application.Accounts.AuthenticatedSession;
using ContractSession = ReversiGameSystem.Server.Contracts.Accounts.AuthenticatedSession;

namespace ReversiGameSystem.Server.ServiceApi.Wcf;

internal static class AccountContractMapper
{
    internal static CreateAccountCommand Map(AccountRegistrationCredentials? credentials)
    {
        ProfileImageData? profileImage = credentials?.ProfileImage == null
            ? null
            : new ProfileImageData(
                (byte[])credentials.ProfileImage.Content.Clone(),
                credentials.ProfileImage.MediaType);

        return new CreateAccountCommand(
            credentials?.Username ?? string.Empty,
            credentials?.EmailAddress ?? string.Empty,
            credentials?.Password ?? string.Empty,
            credentials?.PreferredLocaleCode ?? string.Empty,
            profileImage);
    }

    internal static LoginCommand Map(AuthenticationCredentials? credentials)
    {
        return new LoginCommand(
            credentials?.LoginIdentifier ?? string.Empty,
            credentials?.Password ?? string.Empty);
    }

    internal static AccountRegistrationOutcome Map(
        CreateAccountResult result,
        AccountRegistrationCredentials? credentials)
    {
        AccountRegistrationOutcome outcome = new AccountRegistrationOutcome
        {
            Status = Map(result.Outcome, credentials)
        };
        if (result.Outcome == CreateAccountOutcome.Success)
        {
            long accountId = result.AccountId
                ?? throw new InvalidOperationException(
                    "A successful registration must identify the account.");
            outcome.RegisteredAccount = new AccountReference
            {
                AccountId = accountId,
                Username = credentials?.Username ?? string.Empty
            };
        }

        return outcome;
    }

    internal static AuthenticationOutcome Map(LoginResult result)
    {
        AuthenticationOutcome outcome = new AuthenticationOutcome
        {
            Status = Map(result.Outcome)
        };
        if (result.Outcome == LoginOutcome.Success)
        {
            ApplicationSession session = result.Session
                ?? throw new InvalidOperationException(
                    "A successful authentication must include a session.");
            outcome.Session = Map(session);
        }

        return outcome;
    }

    private static AccountRegistrationStatus Map(
        CreateAccountOutcome outcome,
        AccountRegistrationCredentials? credentials)
    {
        return outcome switch
        {
            CreateAccountOutcome.Success => AccountRegistrationStatus.Success,
            CreateAccountOutcome.InvalidRequest => DetectInvalidRegistration(credentials),
            CreateAccountOutcome.InvalidUsername => AccountRegistrationStatus.InvalidUsername,
            CreateAccountOutcome.InvalidEmailAddress => AccountRegistrationStatus.InvalidEmail,
            CreateAccountOutcome.InvalidPassword => AccountRegistrationStatus.InvalidPassword,
            CreateAccountOutcome.UsernameAlreadyExists =>
                AccountRegistrationStatus.UsernameUnavailable,
            CreateAccountOutcome.EmailAddressAlreadyExists =>
                AccountRegistrationStatus.EmailUnavailable,
            CreateAccountOutcome.ServiceUnavailable =>
                AccountRegistrationStatus.PersistenceUnavailable,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
        };
    }

    private static AccountRegistrationStatus DetectInvalidRegistration(
        AccountRegistrationCredentials? credentials)
    {
        if (string.IsNullOrWhiteSpace(credentials?.Username))
        {
            return AccountRegistrationStatus.InvalidUsername;
        }

        if (string.IsNullOrWhiteSpace(credentials!.EmailAddress))
        {
            return AccountRegistrationStatus.InvalidEmail;
        }

        if (string.IsNullOrEmpty(credentials.Password))
        {
            return AccountRegistrationStatus.InvalidPassword;
        }

        if (string.IsNullOrWhiteSpace(credentials.PreferredLocaleCode))
        {
            return AccountRegistrationStatus.InvalidLocale;
        }

        if (credentials.ProfileImage != null)
        {
            return AccountRegistrationStatus.InvalidProfileImage;
        }

        return AccountRegistrationStatus.InvalidLocale;
    }

    private static AuthenticationStatus Map(LoginOutcome outcome)
    {
        return outcome switch
        {
            LoginOutcome.Success => AuthenticationStatus.Success,
            LoginOutcome.InvalidRequest => AuthenticationStatus.InvalidCredentials,
            LoginOutcome.InvalidCredentials => AuthenticationStatus.InvalidCredentials,
            LoginOutcome.AccountInactive => AuthenticationStatus.InactiveAccount,
            LoginOutcome.ServiceUnavailable => AuthenticationStatus.PersistenceUnavailable,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
        };
    }

    private static ContractSession Map(ApplicationSession session)
    {
        return new ContractSession
        {
            SessionToken = session.SessionToken,
            Account = new AccountReference
            {
                AccountId = session.AccountId,
                Username = session.Username
            },
            EmailAddress = session.EmailAddress,
            PreferredLocaleCode = session.PreferredLocaleCode,
            ExpiresAtUtc = session.ExpiresAtUtc
        };
    }
}
