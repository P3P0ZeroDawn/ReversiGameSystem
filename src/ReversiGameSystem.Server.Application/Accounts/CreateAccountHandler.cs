using System;
using System.Text;
using System.Threading.Tasks;
using ReversiGameSystem.Domain.Accounts;

namespace ReversiGameSystem.Server.Application.Accounts;

public sealed class CreateAccountHandler : ICreateAccountHandler
{
    private const int MaximumLocaleCodeLength = 35;
    private readonly IAccountRegistrationStore accountStore;
    private readonly IPasswordHasher passwordHasher;
    private readonly IProfileImageStore profileImageStore;
    private readonly IUtcClock clock;

    public CreateAccountHandler(
        IAccountRegistrationStore accountStore,
        IPasswordHasher passwordHasher,
        IProfileImageStore profileImageStore,
        IUtcClock clock)
    {
        this.accountStore = accountStore ?? throw new ArgumentNullException(nameof(accountStore));
        this.passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        this.profileImageStore = profileImageStore
            ?? throw new ArgumentNullException(nameof(profileImageStore));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<CreateAccountResult> ExecuteAsync(CreateAccountCommand command)
    {
        if (command == null)
        {
            return new CreateAccountResult(CreateAccountOutcome.InvalidRequest);
        }

        string username = command.Username?.Trim() ?? string.Empty;
        string emailAddress = command.EmailAddress?.Trim() ?? string.Empty;
        string password = command.Password ?? string.Empty;
        string localeCode = command.PreferredLocaleCode?.Trim() ?? string.Empty;

        CreateAccountOutcome? validationFailure = Validate(
            username,
            emailAddress,
            password,
            localeCode,
            command.ProfileImage);
        if (validationFailure.HasValue)
        {
            return new CreateAccountResult(validationFailure.Value);
        }

        string normalizedPassword = password.Normalize(NormalizationForm.FormC);
        string passwordHash;
        try
        {
            passwordHash = passwordHasher.Hash(normalizedPassword);
        }
        catch (Exception)
        {
            return new CreateAccountResult(CreateAccountOutcome.ServiceUnavailable);
        }

        ProfileImageStorageResult imageResult = await StoreProfileImageAsync(command.ProfileImage);
        if (imageResult.Outcome != ProfileImageStorageOutcome.Success)
        {
            return new CreateAccountResult(
                imageResult.Outcome == ProfileImageStorageOutcome.InvalidImage
                    ? CreateAccountOutcome.InvalidRequest
                    : CreateAccountOutcome.ServiceUnavailable);
        }

        AccountRegistrationStoreResult storeResult;
        try
        {
            storeResult = await accountStore.RegisterAsync(
                new AccountRegistrationData(
                    username,
                    emailAddress,
                    passwordHash,
                    localeCode,
                    imageResult.ImageUri,
                    clock.UtcNow));
        }
        catch (Exception)
        {
            await DeleteProfileImageIfNecessaryAsync(imageResult.ImageUri);
            return new CreateAccountResult(CreateAccountOutcome.ServiceUnavailable);
        }

        if (storeResult.Outcome != AccountRegistrationStoreOutcome.Success)
        {
            await DeleteProfileImageIfNecessaryAsync(imageResult.ImageUri);
        }

        return Map(storeResult);
    }

    private static CreateAccountOutcome? Validate(
        string username,
        string emailAddress,
        string password,
        string localeCode,
        ProfileImageData? profileImage)
    {
        if (string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(emailAddress)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(localeCode)
            || localeCode.Length > MaximumLocaleCodeLength)
        {
            return CreateAccountOutcome.InvalidRequest;
        }

        if (UsernamePolicy.Validate(username) != UsernameValidationError.None)
        {
            return CreateAccountOutcome.InvalidUsername;
        }

        if (EmailPolicy.Validate(emailAddress) != EmailValidationError.None)
        {
            return CreateAccountOutcome.InvalidEmailAddress;
        }

        if (PasswordPolicy.Validate(password) != PasswordValidationError.None)
        {
            return CreateAccountOutcome.InvalidPassword;
        }

        return profileImage != null
            && !ProfileImagePolicy.IsSupported(profileImage.Content, profileImage.MediaType)
                ? CreateAccountOutcome.InvalidRequest
                : null;
    }

    private async Task<ProfileImageStorageResult> StoreProfileImageAsync(
        ProfileImageData? profileImage)
    {
        if (profileImage == null)
        {
            return new ProfileImageStorageResult(ProfileImageStorageOutcome.Success);
        }

        try
        {
            return await profileImageStore.SaveAsync(profileImage);
        }
        catch (Exception)
        {
            return new ProfileImageStorageResult(ProfileImageStorageOutcome.Unavailable);
        }
    }

    private async Task DeleteProfileImageIfNecessaryAsync(string? imageUri)
    {
        if (string.IsNullOrWhiteSpace(imageUri))
        {
            return;
        }

        try
        {
            await profileImageStore.DeleteAsync(imageUri!);
        }
        catch (Exception)
        {
            // Account creation has already failed; cleanup is best effort.
        }
    }

    private static CreateAccountResult Map(AccountRegistrationStoreResult result)
    {
        return result.Outcome switch
        {
            AccountRegistrationStoreOutcome.Success =>
                new CreateAccountResult(CreateAccountOutcome.Success, result.AccountId),
            AccountRegistrationStoreOutcome.UsernameAlreadyExists =>
                new CreateAccountResult(CreateAccountOutcome.UsernameAlreadyExists),
            AccountRegistrationStoreOutcome.EmailAddressAlreadyExists =>
                new CreateAccountResult(CreateAccountOutcome.EmailAddressAlreadyExists),
            AccountRegistrationStoreOutcome.UnsupportedLocale =>
                new CreateAccountResult(CreateAccountOutcome.InvalidRequest),
            AccountRegistrationStoreOutcome.Unavailable =>
                new CreateAccountResult(CreateAccountOutcome.ServiceUnavailable),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, null)
        };
    }
}
