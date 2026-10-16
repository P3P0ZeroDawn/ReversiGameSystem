using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ReversiGameSystem.Domain.Accounts;

namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>
/// Represents create account handler.
/// </summary>
public sealed class CreateAccountHandler : ICreateAccountHandler
{
    private const int MaximumLocaleCodeLength = 35;
    private readonly IAccountRegistrationStore _accountStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IProfileImageStore _profileImageStore;
    private readonly IUtcClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAccountHandler"/> class.
    /// </summary>
    /// <param name="accountStore">The account store.</param>
    /// <param name="passwordHasher">The password hasher.</param>
    /// <param name="profileImageStore">The profile image store.</param>
    /// <param name="clock">The clock.</param>
    public CreateAccountHandler(
        IAccountRegistrationStore accountStore,
        IPasswordHasher passwordHasher,
        IProfileImageStore profileImageStore,
        IUtcClock clock)
    {
        _accountStore = accountStore ?? throw new ArgumentNullException(nameof(accountStore));
        _passwordHasher = passwordHasher
            ?? throw new ArgumentNullException(nameof(passwordHasher));
        _profileImageStore = profileImageStore
            ?? throw new ArgumentNullException(nameof(profileImageStore));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>
    /// Performs the execute async operation.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<CreateAccountResult> ExecuteAsync(CreateAccountCommand command)
    {
        if (command == null)
        {
            return new CreateAccountResult(CreateAccountOutcome.InvalidRequest);
        }

        NormalizedAccountInput input = Normalize(command);
        CreateAccountValidationResult validationResult = Validate(input);
        if (!validationResult.IsValid)
        {
            return new CreateAccountResult(validationResult.FailureOutcome);
        }

        PasswordHashResult passwordResult = CreatePasswordHash(input.Password);
        if (!passwordResult.IsSuccess)
        {
            return new CreateAccountResult(CreateAccountOutcome.ServiceUnavailable);
        }

        ProfileImageStorageResult imageResult = StoreProfileImage(input.ProfileImage);
        if (imageResult.Outcome != ProfileImageStorageOutcome.Success)
        {
            return MapImageFailure(imageResult.Outcome);
        }

        AccountRegistrationStoreResult storeResult = await RegisterAsync(
            input,
            passwordResult.PasswordHash,
            imageResult.ImageUri);
        if (storeResult.Outcome != AccountRegistrationStoreOutcome.Success)
        {
            DeleteProfileImageIfNecessary(imageResult.ImageUri);
        }

        return Map(storeResult);
    }

    private static NormalizedAccountInput Normalize(CreateAccountCommand command)
    {
        return new NormalizedAccountInput
        {
            Username = command.Username?.Trim() ?? string.Empty,
            EmailAddress = command.EmailAddress?.Trim() ?? string.Empty,
            Password = command.Password ?? string.Empty,
            LocaleCode = command.PreferredLocaleCode?.Trim() ?? string.Empty,
            ProfileImage = command.ProfileImage
        };
    }

    private static CreateAccountValidationResult Validate(NormalizedAccountInput input)
    {
        if (HasMissingOrInvalidRequiredValue(input))
        {
            return CreateAccountValidationResult.Failure(
                CreateAccountOutcome.InvalidRequest);
        }

        if (UsernamePolicy.Validate(input.Username) != UsernameValidationError.None)
        {
            return CreateAccountValidationResult.Failure(
                CreateAccountOutcome.InvalidUsername);
        }

        if (EmailPolicy.Validate(input.EmailAddress) != EmailValidationError.None)
        {
            return CreateAccountValidationResult.Failure(
                CreateAccountOutcome.InvalidEmailAddress);
        }

        if (PasswordPolicy.Validate(input.Password) != PasswordValidationError.None)
        {
            return CreateAccountValidationResult.Failure(
                CreateAccountOutcome.InvalidPassword);
        }

        if (input.ProfileImage != null
            && !ProfileImagePolicy.IsSupported(
                input.ProfileImage.Content,
                input.ProfileImage.MediaType))
        {
            return CreateAccountValidationResult.Failure(
                CreateAccountOutcome.InvalidRequest);
        }

        return CreateAccountValidationResult.Success();
    }

    private static bool HasMissingOrInvalidRequiredValue(NormalizedAccountInput input)
    {
        return string.IsNullOrWhiteSpace(input.Username)
            || string.IsNullOrWhiteSpace(input.EmailAddress)
            || string.IsNullOrWhiteSpace(input.Password)
            || string.IsNullOrWhiteSpace(input.LocaleCode)
            || input.LocaleCode.Length > MaximumLocaleCodeLength;
    }

    private PasswordHashResult CreatePasswordHash(string password)
    {
        PasswordHashResult result;
        try
        {
            string normalizedPassword = password.Normalize(NormalizationForm.FormC);
            result = PasswordHashResult.Success(_passwordHasher.Hash(normalizedPassword));
        }
        catch (Exception exception) when (exception is ArgumentException or CryptographicException)
        {
            result = PasswordHashResult.Failure();
        }

        return result;
    }

    private ProfileImageStorageResult StoreProfileImage(
        ProfileImageData? profileImage)
    {
        if (profileImage == null)
        {
            return new ProfileImageStorageResult(ProfileImageStorageOutcome.Success);
        }

        return _profileImageStore.Save(profileImage);
    }

    private Task<AccountRegistrationStoreResult> RegisterAsync(
        NormalizedAccountInput input,
        string passwordHash,
        string? imageUri)
    {
        AccountRegistrationData registration = new AccountRegistrationData(
            input.Username,
            input.EmailAddress,
            passwordHash,
            input.LocaleCode,
            imageUri,
            _clock.UtcNow);
        return _accountStore.RegisterAsync(registration);
    }

    private void DeleteProfileImageIfNecessary(string? imageUri)
    {
        if (imageUri == null || string.IsNullOrWhiteSpace(imageUri))
        {
            return;
        }

        _profileImageStore.Delete(imageUri);
    }

    private static CreateAccountResult MapImageFailure(
        ProfileImageStorageOutcome outcome)
    {
        CreateAccountOutcome accountOutcome = outcome ==
            ProfileImageStorageOutcome.InvalidImage
                ? CreateAccountOutcome.InvalidRequest
                : CreateAccountOutcome.ServiceUnavailable;
        return new CreateAccountResult(accountOutcome);
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

    private sealed class NormalizedAccountInput
    {
        internal string Username { get; set; } = string.Empty;

        internal string EmailAddress { get; set; } = string.Empty;

        internal string Password { get; set; } = string.Empty;

        internal string LocaleCode { get; set; } = string.Empty;

        internal ProfileImageData? ProfileImage { get; set; }
    }

    private sealed class CreateAccountValidationResult
    {
        private CreateAccountValidationResult(
            bool isValid,
            CreateAccountOutcome failureOutcome)
        {
            IsValid = isValid;
            FailureOutcome = failureOutcome;
        }

        internal bool IsValid { get; }

        internal CreateAccountOutcome FailureOutcome { get; }

        internal static CreateAccountValidationResult Success()
        {
            return new CreateAccountValidationResult(
                true,
                CreateAccountOutcome.InvalidRequest);
        }

        internal static CreateAccountValidationResult Failure(
            CreateAccountOutcome outcome)
        {
            return new CreateAccountValidationResult(false, outcome);
        }
    }

    private sealed class PasswordHashResult
    {
        private PasswordHashResult(bool isSuccess, string passwordHash)
        {
            IsSuccess = isSuccess;
            PasswordHash = passwordHash;
        }

        internal bool IsSuccess { get; }

        internal string PasswordHash { get; }

        internal static PasswordHashResult Success(string passwordHash)
        {
            return new PasswordHashResult(true, passwordHash);
        }

        internal static PasswordHashResult Failure()
        {
            return new PasswordHashResult(false, string.Empty);
        }
    }
}
