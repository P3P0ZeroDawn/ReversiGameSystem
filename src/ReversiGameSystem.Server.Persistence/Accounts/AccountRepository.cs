using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Persistence.Matches;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Accounts;

public sealed class AccountRepository : IAccountRepository
{
    private const string ActiveStatusCode = "ACTIVE";
    private const string DeactivatedStatusCode = "DEACTIVATED";
    private const string StandardPieceStyleCode = "STANDARD";
    private const string WaitingMatchStatusCode = "WAITING";
    private const string InProgressMatchStatusCode = "IN_PROGRESS";
    private const string RegistrationLockResource = "account-registration";
    private readonly IUtcClock _clock;

    public AccountRepository(IUtcClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public AccountResult<LoginAccount> FindByLogin(string loginIdentifier)
    {
        if (string.IsNullOrWhiteSpace(loginIdentifier))
        {
            return new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        }

        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        {
            Account? account = context.Account
                .Include(candidate => candidate.AccountSettings)
                .AsNoTracking()
                .FirstOrDefault(
                    candidate => candidate.username == loginIdentifier
                        || candidate.emailAddress == loginIdentifier);
            return MapLoginAccountResult(account);
        }
    }

    public AccountResult<LoginAccount> FindByEmail(string emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            return new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        }

        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        {
            Account? account = context.Account
                .Include(candidate => candidate.AccountSettings)
                .AsNoTracking()
                .FirstOrDefault(candidate => candidate.emailAddress == emailAddress);
            return MapLoginAccountResult(account);
        }
    }

    public AccountResult<LoginAccount> FindById(long accountId)
    {
        if (accountId <= 0)
        {
            return new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        }

        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        {
            Account? account = context.Account
                .Include(candidate => candidate.AccountSettings)
                .AsNoTracking()
                .FirstOrDefault(candidate => candidate.accountId == accountId);
            return MapLoginAccountResult(account);
        }
    }

    public AccountStatus CheckRegistrationAvailability(
        string username,
        string emailAddress)
    {
        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        {
            if (context.Account.AsNoTracking().Any(account => account.username == username))
            {
                return AccountStatus.UsernameUnavailable;
            }

            if (context.Account.AsNoTracking().Any(
                account => account.emailAddress == emailAddress))
            {
                return AccountStatus.EmailUnavailable;
            }

            return AccountStatus.Success;
        }
    }

    public AccountResult<long> CreateAccount(AccountRegistrationData registration)
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        using (DbContextTransaction transaction =
            context.Database.BeginTransaction(IsolationLevel.Serializable))
        {
            MatchDatabase.AcquireTransactionLock(context, RegistrationLockResource);
            AccountResult<long> result = SaveNewAccount(context, registration);
            if (result.Status == AccountStatus.Success)
            {
                transaction.Commit();
            }

            return result;
        }
    }

    public AccountStatus UpdatePasswordHash(long accountId, string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new ArgumentException(
                "A password hash is required.",
                nameof(newPasswordHash));
        }

        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        {
            Account? account = context.Account.SingleOrDefault(
                candidate => candidate.accountId == accountId
                    && candidate.statusCode == ActiveStatusCode);
            if (account == null)
            {
                return AccountStatus.InactiveAccount;
            }

            account.passwordHash = newPasswordHash;
            context.SaveChanges();
            return AccountStatus.Success;
        }
    }

    public AccountStatus DeactivateAccount(long accountId)
    {
        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        {
            Account? account = context.Account.SingleOrDefault(
                candidate => candidate.accountId == accountId
                    && candidate.statusCode == ActiveStatusCode);
            if (account == null)
            {
                return AccountStatus.InactiveAccount;
            }

            if (HasOpenMatch(context, accountId))
            {
                return AccountStatus.ActiveMatchInProgress;
            }

            account.statusCode = DeactivatedStatusCode;
            context.SaveChanges();
            return AccountStatus.Success;
        }
    }

    private static bool HasOpenMatch(ReversiGameSystemEntities context, long accountId)
    {
        long[] openMatchIds = context.GameMatch
            .Where(match => match.statusCode == WaitingMatchStatusCode
                || match.statusCode == InProgressMatchStatusCode)
            .Select(match => match.matchId)
            .ToArray();
        if (openMatchIds.Length == 0)
        {
            return false;
        }

        bool isParticipant = context.MatchParticipant.Any(participant =>
            participant.accountId == accountId
            && openMatchIds.Contains(participant.matchId));
        return isParticipant;
    }

    private AccountResult<long> SaveNewAccount(
        ReversiGameSystemEntities context,
        AccountRegistrationData registration)
    {
        if (context.Account.AsNoTracking().Any(
            account => account.username == registration.Username))
        {
            return new AccountResult<long>(AccountStatus.UsernameUnavailable);
        }

        if (context.Account.AsNoTracking().Any(
            account => account.emailAddress == registration.EmailAddress))
        {
            return new AccountResult<long>(AccountStatus.EmailUnavailable);
        }

        bool localeSupported = context.SupportedLocale.AsNoTracking().Any(
            locale => locale.localeCode == registration.PreferredLocaleCode
                && locale.isEnabled);
        if (!localeSupported)
        {
            return new AccountResult<long>(AccountStatus.InvalidLocale);
        }

        Account accountEntity = BuildAccountEntity(registration, _clock.UtcNow);
        context.Account.Add(accountEntity);
        context.SaveChanges();

        AccountResult<long> result = new AccountResult<long>(accountEntity.accountId);
        return result;
    }

    private static Account BuildAccountEntity(
        AccountRegistrationData registration,
        DateTime registeredAtUtc)
    {
        Account account = new Account
        {
            username = registration.Username,
            emailAddress = registration.EmailAddress,
            passwordHash = registration.PasswordHash,
            profileImageUri = registration.ProfileImageUri,
            statusCode = ActiveStatusCode,
            registeredAtUtc = registeredAtUtc
        };

        AccountSettings settings = new AccountSettings
        {
            preferredLocaleCode = registration.PreferredLocaleCode,
            pieceStyleCode = StandardPieceStyleCode,
            Account = account
        };
        account.AccountSettings = settings;
        return account;
    }

    private static AccountResult<LoginAccount> MapLoginAccountResult(Account? account)
    {
        if (account == null)
        {
            return new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);
        }

        LoginAccount loginAccount = new LoginAccount(
            account.accountId,
            account.username,
            account.emailAddress,
            account.passwordHash,
            account.statusCode,
            account.AccountSettings?.preferredLocaleCode ?? string.Empty);
        AccountResult<LoginAccount> result = new AccountResult<LoginAccount>(loginAccount);
        return result;
    }
}
