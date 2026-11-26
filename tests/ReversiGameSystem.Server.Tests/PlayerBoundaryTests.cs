using ReversiGameSystem.Server.Domain.Accounts;
using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class PlayerBoundaryTests
{
    [TestMethod]
    public void RegisterAccount_ThroughWcf_MapsCredentialsAndOutcome()
    {
        // Arrange
        AccountServiceBoundaryStub stub = new AccountServiceBoundaryStub();
        VerificationCodeProtector protector = new VerificationCodeProtector(new byte[32]);
        VerificationCodeService verificationCodes = new VerificationCodeService(
            protector,
            stub,
            stub,
            stub);
        SessionStore sessions = new SessionStore(stub);
        AccountApplicationService applicationService = new AccountApplicationService(
            stub,
            stub,
            verificationCodes,
            sessions);
        AccountService service = new AccountService(applicationService);

        // Act
        AccountReference result = Invoke<IAccountService, AccountReference>(
            service, channel => channel.CompleteRegistration(new NewAccount
            {
                VerificationCode = "123456",
                Username = "PlayerOne",
                EmailAddress = "player@example.com",
                Password = "Password1!",
                PreferredLocaleCode = "en-US",
                ProfileImage = new ImageContent
                {
                    MediaType = "image/png",
                    Content = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }
                }
            }));

        // Assert
        Assert.AreEqual("PlayerOne", stub.LastRegistration?.Username);
        Assert.AreEqual("player@example.com", stub.LastRegistration?.EmailAddress);
        Assert.AreEqual("en-US", stub.LastRegistration?.PreferredLocaleCode);
        Assert.AreEqual("image/png", stub.LastImage?.MediaType);
        Assert.AreEqual((byte)80, stub.LastImage?.Content[1]);
        Assert.AreEqual(42L, result.AccountId);
        Assert.AreEqual("PlayerOne", result.Username);
    }

    [TestMethod]
    public void GetProfile_ThroughWcf_MapsIdentityAndStatistics()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        ReversiGameSystem.Server.Contracts.Players.PlayerProfile result =
            Invoke<IPlayerProfileService, ReversiGameSystem.Server.Contracts.Players.PlayerProfile>(
                context.Service, channel => channel.GetPlayerProfile("session-token", null));
        Assert.AreEqual(42L, result.Account.AccountId);
        Assert.AreEqual("PlayerOne", result.Account.Username);
        Assert.AreEqual(6, result.Statistics.MatchesPlayed);
        Assert.AreEqual(3, result.Statistics.Wins);
        Assert.AreEqual(0.5d, result.Statistics.WinRate);
    }

    [TestMethod]
    public void GetProfile_MissingAndInvalidSessionThroughWcf_ReturnsTypedFaults()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        AssertSessionFault(context, "", SessionFailure.MissingSessionToken);
        context.Sessions.RevokeSession("session-token");
        AssertSessionFault(context, "unknown", SessionFailure.InvalidSession);
    }

    [TestMethod]
    public void GetProfile_InactiveCallerRequestingAnotherAccount_RejectsThroughWcf()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        context.Profiles.Profiles.Clear();
        FaultException<ProfileFault> fault = Assert.Throws<FaultException<ProfileFault>>(() =>
            Invoke<IPlayerProfileService, object>(context.Service,
                channel => channel.GetPlayerProfile("session-token", 99)));
        Assert.AreEqual(ProfileFailure.InactiveAccount, fault.Detail.Reason);
        Assert.AreEqual(1, context.Profiles.AccountIds.Count);
        Assert.AreEqual(42L, context.Profiles.AccountIds[0]);
    }

    [TestMethod]
    public void GetProfile_InvalidTargetThroughWcf_ReturnsTypedFault()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        FaultException<ProfileFault> fault = Assert.Throws<FaultException<ProfileFault>>(() =>
            Invoke<IPlayerProfileService, object>(context.Service,
                channel => channel.GetPlayerProfile("session-token", 0)));
        Assert.AreEqual(ProfileFailure.InvalidAccountId, fault.Detail.Reason);
    }

    [TestMethod]
    public void GetProfile_MissingTargetThroughWcf_ReturnsTypedFault()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();

        FaultException<ProfileFault> fault = Assert.Throws<FaultException<ProfileFault>>(() =>
            Invoke<IPlayerProfileService, object>(context.Service,
                channel => channel.GetPlayerProfile("session-token", 99)));

        Assert.AreEqual(ProfileFailure.AccountNotFound, fault.Detail.Reason);
    }

    [TestMethod]
    public void GetProfile_RepositoryFailureThroughWcf_ReturnsServiceFault()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        context.Profiles.ThrowOnGet = true;

        FaultException<ServiceFault> fault = Assert.Throws<FaultException<ServiceFault>>(() =>
            Invoke<IPlayerProfileService, object>(context.Service,
                channel => channel.GetPlayerProfile("session-token", null)));

        Assert.AreEqual("PROFILE_OPERATION_FAILURE", fault.Detail.FailureCode);
        Assert.AreEqual(true, !string.IsNullOrWhiteSpace(fault.Detail.CorrelationId));
        Assert.IsTrue(!fault.ToString().Contains("private-provider-message"),
            "The profile fault must not expose the repository message.");
    }

    [TestMethod]
    public void GetImage_AbsentImageThroughWcf_ReturnsNull()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        ImageContent? result = Invoke<IPlayerProfileService, ImageContent?>(context.Service,
            channel => channel.GetProfileImage("session-token", null));
        Assert.AreEqual<ImageContent?>(null, result);
        Assert.AreEqual(false, context.Images.ReadCalled);
    }

    [TestMethod]
    public void GetImage_PresentImageThroughWcf_ReturnsContent()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        context.Profiles.Profiles[42] = new PlayerProfile(
            42,
            "PlayerOne",
            "profile-images/player.png",
            3,
            2,
            1);

        ImageContent? result = Invoke<IPlayerProfileService, ImageContent?>(
            context.Service,
            channel => channel.GetProfileImage("session-token", null));

        ImageContent image = TestAssert.IsNotNull(result);
        Assert.AreEqual("image/png", image.MediaType);
        Assert.AreEqual((byte)2, image.Content[1]);
        Assert.AreEqual(true, context.Images.ReadCalled);
    }

    [TestMethod]
    public void GetImage_ReadFailureThroughWcf_ReturnsServiceFault()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        context.Profiles.Profiles[42] = new PlayerProfile(
            42,
            "PlayerOne",
            "private-reference",
            3,
            2,
            1);
        context.Images.FailRead = true;
        FaultException<ServiceFault> fault = Assert.Throws<FaultException<ServiceFault>>(() =>
            Invoke<IPlayerProfileService, ImageContent?>(context.Service,
                channel => channel.GetProfileImage("session-token", null)));
        Assert.AreEqual("PROFILE_OPERATION_FAILURE", fault.Detail.FailureCode);
        Assert.AreEqual(true, !string.IsNullOrWhiteSpace(fault.Detail.CorrelationId));
        Assert.AreEqual(false, fault.ToString().Contains("private-provider-message"));
    }

    [TestMethod]
    public void ChangeUsername_ThroughWcf_NormalizesAndUpdatesOnlyCaller()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        Invoke<IPlayerProfileService, bool>(context.Service, channel =>
        {
            channel.ChangeUsername("session-token", "  NewPlayer  ");
            return true;
        });
        Assert.AreEqual<long?>(42, context.Profiles.ChangedAccountId);
        Assert.AreEqual("NewPlayer", context.Profiles.ChangedUsername);
    }

    [TestMethod]
    public void ChangeUsername_UnavailableThroughWcf_ReturnsTypedFault()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        context.Profiles.ChangeUsernameStatus =
            ProfileStatus.UsernameUnavailable;

        FaultException<ProfileFault> fault = Assert.Throws<FaultException<ProfileFault>>(
            () => Invoke<IPlayerProfileService, bool>(context.Service, channel =>
            {
                channel.ChangeUsername("session-token", "OtherPlayer");
                return true;
            }));

        Assert.AreEqual(ProfileFailure.UsernameUnavailable, fault.Detail.Reason);
    }

    [TestMethod]
    public void UpdateImage_InvalidThroughWcf_ReturnsTypedFault()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        context.Images.RejectSave = true;

        FaultException<ProfileFault> fault = Assert.Throws<FaultException<ProfileFault>>(
            () => Invoke<IPlayerProfileService, bool>(context.Service, channel =>
            {
                channel.UpdateProfileImage("session-token", new ImageContent
                {
                    MediaType = "image/png",
                    Content = new byte[] { 1, 2 }
                });
                return true;
            }));

        Assert.AreEqual(ProfileFailure.InvalidImage, fault.Detail.Reason);
    }

    [TestMethod]
    public void UpdateImage_AssociationFailureThroughWcf_RemovesNewImage()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        context.Profiles.ThrowOnSetImage = true;
        Assert.Throws<FaultException<ServiceFault>>(() =>
            Invoke<IPlayerProfileService, bool>(context.Service, channel =>
            {
                channel.UpdateProfileImage("session-token", new ImageContent
                { MediaType = "image/png", Content = new byte[] { 1, 2 } });
                return true;
            }));
        Assert.AreEqual("new-image", context.Images.Deleted);
        Assert.AreEqual<long?>(42, context.Profiles.ImageAccountId);
    }

    [TestMethod]
    public void GetLeaderboard_ThroughWcf_MapsOrderedEntries()
    {
        LeaderboardRepositoryStub repository = new LeaderboardRepositoryStub
        {
            Players = new[]
            {
                new LeaderboardPlayer(42, "PlayerOne", "image", 6, 3)
            }
        };
        LeaderboardService service = new LeaderboardService(
            new LeaderboardApplicationService(repository));
        ReversiGameSystem.Server.Contracts.Leaderboards.LeaderboardEntry[] result =
            Invoke<ILeaderboardService, ReversiGameSystem.Server.Contracts.Leaderboards.LeaderboardEntry[]>(
                service, channel => channel.GetLeaderboard(10));
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(1, result[0].Position);
        Assert.AreEqual(42L, result[0].Account.AccountId);
        Assert.AreEqual(true, result[0].HasProfileImage);
        Assert.AreEqual(0.5d, result[0].WinRate);
        Assert.AreEqual(true, repository.WasCalled);
    }

    [TestMethod]
    public void GetLeaderboard_InvalidLimitThroughWcf_ReturnsTypedFault()
    {
        LeaderboardService service = new LeaderboardService(
            new LeaderboardApplicationService(new LeaderboardRepositoryStub()));
        FaultException<LeaderboardFault> fault = Assert.Throws<FaultException<LeaderboardFault>>(() =>
            Invoke<ILeaderboardService, object>(service, channel => channel.GetLeaderboard(0)));
        Assert.AreEqual(LeaderboardFailure.InvalidMaximumEntries, fault.Detail.Reason);
    }

    [TestMethod]
    public void GetLeaderboard_UnavailableThroughWcf_ReturnsServiceFault()
    {
        LeaderboardService service = new LeaderboardService(new LeaderboardApplicationService(
            new LeaderboardRepositoryStub { ThrowOnGet = true }));
        FaultException<ServiceFault> fault = Assert.Throws<FaultException<ServiceFault>>(() =>
            Invoke<ILeaderboardService, object>(service, channel => channel.GetLeaderboard(10)));
        Assert.AreEqual("LEADERBOARD_OPERATION_FAILURE", fault.Detail.FailureCode);
        Assert.AreEqual(true, !string.IsNullOrWhiteSpace(fault.Detail.CorrelationId));
    }

    private static void AssertSessionFault(ProfileBoundaryContext context, string token, SessionFailure expected)
    {
        FaultException<SessionFault> fault = Assert.Throws<FaultException<SessionFault>>(() =>
            Invoke<IPlayerProfileService, object>(context.Service,
                channel => channel.GetPlayerProfile(token, null)));
        Assert.AreEqual(expected, fault.Detail.Reason);
    }

    private static TResult Invoke<TContract, TResult>(object service, Func<TContract, TResult> action)
    {
        Uri address = new Uri("net.pipe://localhost/ReversiGameSystem.PlayerBoundaryTests/"
            + Guid.NewGuid().ToString("N") + "/");
        NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
        using ServiceHost host = new ServiceHost(service, address);
        host.Description.Behaviors.Add(new ServiceErrorBehavior(
            NullLogger<ServiceErrorBehavior>.Instance));
        host.AddServiceEndpoint(typeof(TContract), binding, "Service");
        host.Open();
        ChannelFactory<TContract> factory = new ChannelFactory<TContract>(binding,
            new EndpointAddress(new Uri(address, "Service")));
        TContract channel = factory.CreateChannel();
        try
        {
            TResult result = action(channel);
            ((ICommunicationObject)channel!).Close();
            factory.Close();
            return result;
        }
        catch
        {
            ((ICommunicationObject)channel!).Abort();
            factory.Abort();
            throw;
        }
    }

    private sealed class AccountServiceBoundaryStub :
        IAccountRepository,
        IPlayerImageRepository,
        IVerificationCodeRepository,
        ITemplatedEmailSender,
        IUtcClock
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
        internal AccountRegistrationData? LastRegistration { get; private set; }
        internal ProfileImageData? LastImage { get; private set; }

        public AccountResult<LoginAccount> FindByLogin(string loginIdentifier) =>
            new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);

        public AccountResult<LoginAccount> FindByEmail(string emailAddress) =>
            new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);

        public AccountResult<LoginAccount> FindById(long accountId) =>
            new AccountResult<LoginAccount>(AccountStatus.AccountNotFound);

        public AccountStatus CheckRegistrationAvailability(string username, string emailAddress) =>
            AccountStatus.Success;

        public AccountResult<long> CreateAccount(AccountRegistrationData registration)
        {
            LastRegistration = registration;
            return new AccountResult<long>(42L);
        }

        public AccountStatus UpdatePasswordHash(long accountId, string newPasswordHash) =>
            AccountStatus.Success;

        public AccountStatus DeactivateAccount(long accountId) => AccountStatus.Success;

        public ProfileImageData ReadImage(string imageReference) =>
            throw new NotSupportedException();

        public ProfileResult<string> SaveImage(ProfileImageData image, PlayerImageKind kind)
        {
            LastImage = image;
            return new ProfileResult<string>("img/42.png");
        }

        public void DeleteImage(string imageReference)
        {
        }

        public void SavePendingCode(PendingVerificationCode pendingCode)
        {
        }

        public VerificationStatus ProcessVerificationAttempt(
            string identityKey,
            Func<StoredVerificationCode, VerificationEvaluationResult> evaluateAttempt) =>
            VerificationStatus.Accepted;

        public void SendTemplatedEmail(
            string recipientAddress,
            string templateName,
            System.Collections.Generic.IReadOnlyDictionary<string, string> templateValues)
        {
        }
    }
}
