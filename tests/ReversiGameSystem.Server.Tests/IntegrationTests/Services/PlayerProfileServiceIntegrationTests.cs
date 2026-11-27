using System;
using System.Collections.Generic;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.Domain.Accounts;
using ReversiGameSystem.Server.ServiceApi;
using ContractPlayerProfile = ReversiGameSystem.Server.Contracts.Players.PlayerProfile;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Services;

[TestClass]
[TestCategory("Integration")]
public sealed class PlayerProfileServiceIntegrationTests
{
    private const string DefaultToken = "session-token";
    private const string DefaultUsername = "PlayerOne";
    private const string ExpectedOperationFailureCode = "PROFILE_OPERATION_FAILURE";
    private const string PrivateProviderMessage = "private-provider-message";
    private const string PngMediaType = "image/png";

    private ProfileBoundaryContext _context = null!;
    private WcfChannelScope<IPlayerProfileService> _scope = null!;
    private IPlayerProfileService _channel = null!;

    [TestInitialize]
    public void Initialize()
    {
        _context = new ProfileBoundaryContext();
        _scope = WcfTestHostHelper.OpenChannel<IPlayerProfileService>(
            _context.Service,
            "PlayerProfile");
        _channel = _scope.Channel;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public void GetPlayerProfile_ThroughWcf_MapsIdentityAndStatistics()
    {
        long expectedAccountId = 42L;
        int expectedMatches = 6;
        int expectedWins = 3;
        double expectedWinRate = 0.5d;

        ContractPlayerProfile actualProfile = _channel.GetPlayerProfile(DefaultToken, null);

        Assert.AreEqual(expectedAccountId, actualProfile.Account.AccountId);
        Assert.AreEqual(DefaultUsername, actualProfile.Account.Username);
        Assert.AreEqual(expectedMatches, actualProfile.Statistics.MatchesPlayed);
        Assert.AreEqual(expectedWins, actualProfile.Statistics.Wins);
        Assert.AreEqual(expectedWinRate, actualProfile.Statistics.WinRate);
    }

    [TestMethod]
    [DataRow("", SessionFailure.MissingSessionToken)]
    [DataRow("unknown", SessionFailure.InvalidSession)]
    public void GetPlayerProfile_RejectedSessionThroughWcf_ReturnsTypedFaultAndKeepsChannelOpen(
        string sessionToken,
        SessionFailure expectedReason)
    {
        CommunicationState expectedChannelState = CommunicationState.Opened;
        Action action = () => _channel.GetPlayerProfile(sessionToken, null);

        FaultException<SessionFault> fault =
            Assert.Throws<FaultException<SessionFault>>(action);
        SessionFailure actualReason = fault.Detail.Reason;
        CommunicationState actualChannelState = _scope.ChannelState;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedChannelState, actualChannelState);
    }

    [TestMethod]
    public void GetPlayerProfile_InactiveCallerRequestingAnotherAccount_RejectsThroughWcf()
    {
        _context.Profiles.Profiles.Clear();
        ProfileFailure expectedReason = ProfileFailure.InactiveAccount;
        int expectedLookupCount = 1;
        long expectedLookupAccountId = 42L;
        CommunicationState expectedChannelState = CommunicationState.Opened;
        Action action = () => _channel.GetPlayerProfile(DefaultToken, 99L);

        FaultException<ProfileFault> fault =
            Assert.Throws<FaultException<ProfileFault>>(action);
        ProfileFailure actualReason = fault.Detail.Reason;
        int actualLookupCount = _context.Profiles.AccountIds.Count;
        long actualLookupAccountId = _context.Profiles.AccountIds[0];
        CommunicationState actualChannelState = _scope.ChannelState;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedLookupCount, actualLookupCount);
        Assert.AreEqual(expectedLookupAccountId, actualLookupAccountId);
        Assert.AreEqual(expectedChannelState, actualChannelState);
    }

    [TestMethod]
    [DataRow(0L, ProfileFailure.InvalidAccountId)]
    [DataRow(99L, ProfileFailure.AccountNotFound)]
    public void GetPlayerProfile_InvalidOrMissingTargetThroughWcf_ReturnsTypedFault(
        long targetAccountId,
        ProfileFailure expectedReason)
    {
        CommunicationState expectedChannelState = CommunicationState.Opened;
        Action action = () => _channel.GetPlayerProfile(DefaultToken, targetAccountId);

        FaultException<ProfileFault> fault =
            Assert.Throws<FaultException<ProfileFault>>(action);
        ProfileFailure actualReason = fault.Detail.Reason;
        CommunicationState actualChannelState = _scope.ChannelState;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedChannelState, actualChannelState);
    }

    [TestMethod]
    public void GetPlayerProfile_RepositoryFailureThroughWcf_ReturnsSanitizedServiceFault()
    {
        _context.Profiles.ThrowOnGet = true;
        bool expectedHasCorrelation = true;
        bool expectedContainsSecret = false;
        Action action = () => _channel.GetPlayerProfile(DefaultToken, null);

        FaultException<ServiceFault> fault =
            Assert.Throws<FaultException<ServiceFault>>(action);
        string actualFailureCode = fault.Detail.FailureCode;
        bool actualHasCorrelation = !string.IsNullOrWhiteSpace(fault.Detail.CorrelationId);
        bool actualContainsSecret = fault.ToString().Contains(PrivateProviderMessage);

        Assert.AreEqual(ExpectedOperationFailureCode, actualFailureCode);
        Assert.AreEqual(expectedHasCorrelation, actualHasCorrelation);
        Assert.AreEqual(expectedContainsSecret, actualContainsSecret);
    }

    [TestMethod]
    public void GetProfileImage_AbsentImageThroughWcf_ReturnsNullWithoutReadingStorage()
    {
        bool expectedReadCalled = false;

        ImageContent? actualImage = _channel.GetProfileImage(DefaultToken, null);
        bool actualReadCalled = _context.Images.ReadCalled;

        Assert.IsNull(actualImage);
        Assert.AreEqual(expectedReadCalled, actualReadCalled);
    }

    [TestMethod]
    public void GetProfileImage_PresentImageThroughWcf_ReturnsContent()
    {
        _context.Profiles.Profiles[42L] = new PlayerProfile(
            42L,
            DefaultUsername,
            "profile-images/player.png",
            3,
            2,
            1);
        byte expectedSecondByte = 2;
        bool expectedReadCalled = true;

        ImageContent? result = _channel.GetProfileImage(DefaultToken, null);

        ImageContent actualImage = TestAssert.IsNotNull(result);
        string actualMediaType = actualImage.MediaType;
        byte actualSecondByte = actualImage.Content[1];
        bool actualReadCalled = _context.Images.ReadCalled;
        Assert.AreEqual(PngMediaType, actualMediaType);
        Assert.AreEqual(expectedSecondByte, actualSecondByte);
        Assert.AreEqual(expectedReadCalled, actualReadCalled);
    }

    [TestMethod]
    public void GetProfileImage_ReadFailureThroughWcf_ReturnsSanitizedServiceFault()
    {
        _context.Profiles.Profiles[42L] = new PlayerProfile(
            42L,
            DefaultUsername,
            "private-reference",
            3,
            2,
            1);
        _context.Images.FailRead = true;
        bool expectedHasCorrelation = true;
        bool expectedContainsSecret = false;
        Action action = () => _channel.GetProfileImage(DefaultToken, null);

        FaultException<ServiceFault> fault =
            Assert.Throws<FaultException<ServiceFault>>(action);
        string actualFailureCode = fault.Detail.FailureCode;
        bool actualHasCorrelation = !string.IsNullOrWhiteSpace(fault.Detail.CorrelationId);
        bool actualContainsSecret = fault.ToString().Contains(PrivateProviderMessage);

        Assert.AreEqual(ExpectedOperationFailureCode, actualFailureCode);
        Assert.AreEqual(expectedHasCorrelation, actualHasCorrelation);
        Assert.AreEqual(expectedContainsSecret, actualContainsSecret);
    }

    [TestMethod]
    public void ChangeUsername_ThroughWcf_NormalizesAndUpdatesOnlyCaller()
    {
        long expectedAccountId = 42L;
        string expectedUsername = "NewPlayer";

        _channel.ChangeUsername(DefaultToken, "  NewPlayer  ");
        long? actualAccountId = _context.Profiles.ChangedAccountId;
        string? actualUsername = _context.Profiles.ChangedUsername;

        Assert.AreEqual(expectedAccountId, actualAccountId);
        Assert.AreEqual(expectedUsername, actualUsername);
    }

    [TestMethod]
    public void ChangeUsername_UnavailableThroughWcf_ReturnsTypedFault()
    {
        _context.Profiles.ChangeUsernameStatus = ProfileStatus.UsernameUnavailable;
        ProfileFailure expectedReason = ProfileFailure.UsernameUnavailable;
        CommunicationState expectedChannelState = CommunicationState.Opened;
        Action action = () => _channel.ChangeUsername(DefaultToken, "OtherPlayer");

        FaultException<ProfileFault> fault =
            Assert.Throws<FaultException<ProfileFault>>(action);
        ProfileFailure actualReason = fault.Detail.Reason;
        CommunicationState actualChannelState = _scope.ChannelState;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedChannelState, actualChannelState);
    }

    [TestMethod]
    public void UpdateProfileImage_InvalidThroughWcf_ReturnsTypedFault()
    {
        _context.Images.RejectSave = true;
        ProfileFailure expectedReason = ProfileFailure.InvalidImage;
        CommunicationState expectedChannelState = CommunicationState.Opened;
        ImageContent content = new ImageContent
        {
            MediaType = PngMediaType,
            Content = new byte[] { 1, 2 }
        };
        Action action = () => _channel.UpdateProfileImage(DefaultToken, content);

        FaultException<ProfileFault> fault =
            Assert.Throws<FaultException<ProfileFault>>(action);
        ProfileFailure actualReason = fault.Detail.Reason;
        CommunicationState actualChannelState = _scope.ChannelState;

        Assert.AreEqual(expectedReason, actualReason);
        Assert.AreEqual(expectedChannelState, actualChannelState);
    }

    [TestMethod]
    public void UpdateProfileImage_AssociationFailureThroughWcf_RemovesNewImage()
    {
        _context.Profiles.ThrowOnSetImage = true;
        string expectedDeletedReference = "new-image";
        long expectedAccountId = 42L;
        ImageContent content = new ImageContent
        {
            MediaType = PngMediaType,
            Content = new byte[] { 1, 2 }
        };
        Action action = () => _channel.UpdateProfileImage(DefaultToken, content);

        Assert.Throws<FaultException<ServiceFault>>(action);
        string? actualDeletedReference = _context.Images.Deleted;
        long? actualAccountId = _context.Profiles.ImageAccountId;
        Assert.AreEqual(expectedDeletedReference, actualDeletedReference);
        Assert.AreEqual(expectedAccountId, actualAccountId);
    }

    private sealed class ProfileBoundaryContext
    {
        internal ProfileBoundaryContext()
        {
            Sessions.TryAddAuthenticated(new AuthenticatedSession(
                DefaultToken,
                42L,
                DefaultUsername,
                "player@example.com",
                "en-US",
                DateTime.UtcNow.AddHours(1)));
            Profiles.Profiles.Add(42L, new PlayerProfile(
                42L,
                DefaultUsername,
                null,
                3,
                2,
                1));
        }

        internal SessionStore Sessions { get; } = new SessionStore(new TestUtcClock());

        internal BoundaryProfileRepositoryStub Profiles { get; } =
            new BoundaryProfileRepositoryStub();

        internal BoundaryImageRepositoryStub Images { get; } =
            new BoundaryImageRepositoryStub();

        internal PlayerProfileService Service
        {
            get
            {
                PlayerProfileApplicationService applicationService =
                    new PlayerProfileApplicationService(Sessions, Profiles, Images);
                return new PlayerProfileService(applicationService);
            }
        }
    }

    private sealed class BoundaryImageRepositoryStub : IPlayerImageRepository
    {
        internal bool FailRead { get; set; }

        internal bool RejectSave { get; set; }

        internal bool ReadCalled { get; private set; }

        internal string? Deleted { get; private set; }

        public ProfileImageData ReadImage(string imageReference)
        {
            ReadCalled = true;
            if (FailRead)
            {
                throw new InvalidOperationException(PrivateProviderMessage);
            }

            return new ProfileImageData(new byte[] { 1, 2 }, PngMediaType);
        }

        public ProfileResult<string> SaveImage(ProfileImageData image, PlayerImageKind kind)
        {
            if (RejectSave)
            {
                return new ProfileResult<string>(ProfileStatus.InvalidImage);
            }

            return new ProfileResult<string>("new-image");
        }

        public void DeleteImage(string imageReference)
        {
            Deleted = imageReference;
        }
    }

    private sealed class BoundaryProfileRepositoryStub : IPlayerProfileRepository
    {
        internal Dictionary<long, PlayerProfile> Profiles { get; } =
            new Dictionary<long, PlayerProfile>();

        internal List<long> AccountIds { get; } = new List<long>();

        internal bool ThrowOnGet { get; set; }

        internal bool ThrowOnSetImage { get; set; }

        internal ProfileStatus ChangeUsernameStatus { get; set; } = ProfileStatus.Success;

        internal long? ChangedAccountId { get; private set; }

        internal string? ChangedUsername { get; private set; }

        internal long? ImageAccountId { get; private set; }

        public ProfileResult<PlayerProfile> GetProfile(long accountId)
        {
            AccountIds.Add(accountId);
            if (ThrowOnGet)
            {
                throw new InvalidOperationException(PrivateProviderMessage);
            }

            if (!Profiles.TryGetValue(accountId, out PlayerProfile? profile))
            {
                return new ProfileResult<PlayerProfile>(ProfileStatus.AccountNotFound);
            }

            return new ProfileResult<PlayerProfile>(profile);
        }

        public ProfileResult<string> GetProfileImageReference(long accountId)
        {
            AccountIds.Add(accountId);
            if (ThrowOnGet)
            {
                throw new InvalidOperationException(PrivateProviderMessage);
            }

            if (!Profiles.TryGetValue(accountId, out PlayerProfile? profile))
            {
                return new ProfileResult<string>(ProfileStatus.AccountNotFound);
            }

            return new ProfileResult<string>(profile.ProfileImageUri ?? string.Empty);
        }

        public bool IsActive(long accountId)
        {
            AccountIds.Add(accountId);
            return Profiles.ContainsKey(accountId);
        }

        public ProfileStatus ChangeUsername(long accountId, string username)
        {
            ChangedAccountId = accountId;
            ChangedUsername = username;
            return ChangeUsernameStatus;
        }

        public ProfileStatus SetProfileImage(long accountId, string imageReference)
        {
            ImageAccountId = accountId;
            if (ThrowOnSetImage)
            {
                throw new InvalidOperationException(PrivateProviderMessage);
            }

            return ProfileStatus.Success;
        }
    }
}
