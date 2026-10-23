using System;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.ServiceApi;

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


}
