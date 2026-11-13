using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.Application.Leaderboards;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests;

internal static class PlayerBoundaryTests
{
    internal static void RegisterAccount_ThroughWcf_MapsCredentialsAndOutcome()
    {
        // Arrange
        RecordingRegistrationHandler handler = new RecordingRegistrationHandler();
        AccountAccessService service = new AccountAccessService(handler);
        // Act
        AccountRegistrationOutcome result = Invoke<IAccountAccessService, AccountRegistrationOutcome>(
            service, channel => channel.RegisterAccountAsync(new AccountRegistrationCredentials
            {
                Username = "PlayerOne",
                EmailAddress = "player@example.com",
                Password = "Password1!",
                PreferredLocaleCode = "en-US",
                ProfileImage = new ProfileImageContent
                {
                    MediaType = "image/png",
                    Content = new byte[] { 1, 2 }
                }
            }).GetAwaiter().GetResult());
        // Assert
        TestAssert.AreEqual("PlayerOne", handler.Command?.Username);
        TestAssert.AreEqual("player@example.com", handler.Command?.EmailAddress);
        TestAssert.AreEqual("Password1!", handler.Command?.Password);
        TestAssert.AreEqual("en-US", handler.Command?.PreferredLocaleCode);
        TestAssert.AreEqual("image/png", handler.Command?.ProfileImage?.MediaType);
        TestAssert.AreEqual((byte)2, handler.Command?.ProfileImage?.Content[1]);
        TestAssert.AreEqual(42L, TestAssert.IsNotNull(result.RegisteredAccount).AccountId);
    }

    internal static void GetProfile_ThroughWcf_MapsIdentityAndStatistics()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        ReversiGameSystem.Server.Contracts.Players.PlayerProfile result =
            Invoke<IPlayerProfileService, ReversiGameSystem.Server.Contracts.Players.PlayerProfile>(
                context.Service, channel => channel.GetPlayerProfile("session-token", null));
        TestAssert.AreEqual(42L, result.Account.AccountId);
        TestAssert.AreEqual("PlayerOne", result.Account.Username);
        TestAssert.AreEqual(6, result.Statistics.MatchesPlayed);
        TestAssert.AreEqual(3, result.Statistics.Wins);
        TestAssert.AreEqual(0.5d, result.Statistics.WinRate);
    }

    internal static void GetProfile_MissingAndInvalidSessionThroughWcf_ReturnsTypedFaults()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        AssertSessionFault(context, "", SessionFailure.MissingSessionToken);
        context.Sessions.Session = null;
        AssertSessionFault(context, "unknown", SessionFailure.InvalidSession);
    }

    internal static void GetProfile_InactiveCallerRequestingAnotherAccount_RejectsThroughWcf()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        context.Profiles.Profiles.Clear();
        FaultException<ProfileFault> fault = TestAssert.Throws<FaultException<ProfileFault>>(() =>
            Invoke<IPlayerProfileService, object>(context.Service,
                channel => channel.GetPlayerProfile("session-token", 99)));
        TestAssert.AreEqual(ProfileFailure.InactiveAccount, fault.Detail.Reason);
        TestAssert.AreEqual(1, context.Profiles.AccountIds.Count);
        TestAssert.AreEqual(42L, context.Profiles.AccountIds[0]);
    }

    internal static void GetProfile_InvalidTargetThroughWcf_ReturnsTypedFault()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        FaultException<ProfileFault> fault = TestAssert.Throws<FaultException<ProfileFault>>(() =>
            Invoke<IPlayerProfileService, object>(context.Service,
                channel => channel.GetPlayerProfile("session-token", 0)));
        TestAssert.AreEqual(ProfileFailure.InvalidAccountId, fault.Detail.Reason);
    }

    internal static void GetProfile_MissingTargetThroughWcf_ReturnsTypedFault()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();

        FaultException<ProfileFault> fault = TestAssert.Throws<FaultException<ProfileFault>>(() =>
            Invoke<IPlayerProfileService, object>(context.Service,
                channel => channel.GetPlayerProfile("session-token", 99)));

        TestAssert.AreEqual(ProfileFailure.AccountNotFound, fault.Detail.Reason);
    }

    internal static void GetProfile_RepositoryFailureThroughWcf_ReturnsServiceFault()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        context.Profiles.ThrowOnGet = true;

        FaultException<ServiceFault> fault = TestAssert.Throws<FaultException<ServiceFault>>(() =>
            Invoke<IPlayerProfileService, object>(context.Service,
                channel => channel.GetPlayerProfile("session-token", null)));

        TestAssert.AreEqual("PROFILE_OPERATION_FAILURE", fault.Detail.FailureCode);
        TestAssert.AreEqual(true, !string.IsNullOrWhiteSpace(fault.Detail.CorrelationId));
        TestAssert.IsTrue(!fault.ToString().Contains("private-provider-message"),
            "The profile fault must not expose the repository message.");
    }

    internal static void GetImage_AbsentImageThroughWcf_ReturnsNull()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        ImageContent? result = Invoke<IPlayerProfileService, ImageContent?>(context.Service,
            channel => channel.GetProfileImage("session-token", null));
        TestAssert.AreEqual<ImageContent?>(null, result);
        TestAssert.AreEqual(false, context.Images.ReadCalled);
    }

    internal static void GetImage_PresentImageThroughWcf_ReturnsContent()
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
        TestAssert.AreEqual("image/png", image.MediaType);
        TestAssert.AreEqual((byte)2, image.Content[1]);
        TestAssert.AreEqual(true, context.Images.ReadCalled);
    }

    internal static void GetImage_ReadFailureThroughWcf_ReturnsServiceFault()
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
        FaultException<ServiceFault> fault = TestAssert.Throws<FaultException<ServiceFault>>(() =>
            Invoke<IPlayerProfileService, ImageContent?>(context.Service,
                channel => channel.GetProfileImage("session-token", null)));
        TestAssert.AreEqual("PROFILE_OPERATION_FAILURE", fault.Detail.FailureCode);
        TestAssert.AreEqual(true, !string.IsNullOrWhiteSpace(fault.Detail.CorrelationId));
        TestAssert.AreEqual(false, fault.ToString().Contains("private-provider-message"));
    }

    internal static void ChangeUsername_ThroughWcf_NormalizesAndUpdatesOnlyCaller()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        Invoke<IPlayerProfileService, bool>(context.Service, channel =>
        {
            channel.ChangeUsername("session-token", "  NewPlayer  ");
            return true;
        });
        TestAssert.AreEqual<long?>(42, context.Profiles.ChangedAccountId);
        TestAssert.AreEqual("NewPlayer", context.Profiles.ChangedUsername);
    }

    internal static void ChangeUsername_UnavailableThroughWcf_ReturnsTypedFault()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        context.Profiles.ChangeUsernameStatus =
            ProfileRepositoryUpdateStatus.UsernameUnavailable;

        FaultException<ProfileFault> fault = TestAssert.Throws<FaultException<ProfileFault>>(
            () => Invoke<IPlayerProfileService, bool>(context.Service, channel =>
            {
                channel.ChangeUsername("session-token", "OtherPlayer");
                return true;
            }));

        TestAssert.AreEqual(ProfileFailure.UsernameUnavailable, fault.Detail.Reason);
    }

    internal static void UpdateImage_InvalidThroughWcf_ReturnsTypedFault()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        context.Images.RejectSave = true;

        FaultException<ProfileFault> fault = TestAssert.Throws<FaultException<ProfileFault>>(
            () => Invoke<IPlayerProfileService, bool>(context.Service, channel =>
            {
                channel.UpdateProfileImage("session-token", new ImageContent
                {
                    MediaType = "image/png",
                    Content = new byte[] { 1, 2 }
                });
                return true;
            }));

        TestAssert.AreEqual(ProfileFailure.InvalidImage, fault.Detail.Reason);
    }

    internal static void UpdateImage_AssociationFailureThroughWcf_RemovesNewImage()
    {
        ProfileBoundaryContext context = new ProfileBoundaryContext();
        context.Profiles.ThrowOnSetImage = true;
        TestAssert.Throws<FaultException<ServiceFault>>(() =>
            Invoke<IPlayerProfileService, bool>(context.Service, channel =>
            {
                channel.UpdateProfileImage("session-token", new ImageContent
                { MediaType = "image/png", Content = new byte[] { 1, 2 } });
                return true;
            }));
        TestAssert.AreEqual("new-image", context.Images.Deleted);
        TestAssert.AreEqual<long?>(42, context.Profiles.ImageAccountId);
    }

    internal static void GetLeaderboard_ThroughWcf_MapsOrderedEntries()
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
        TestAssert.AreEqual(1, result.Length);
        TestAssert.AreEqual(1, result[0].Position);
        TestAssert.AreEqual(42L, result[0].Account.AccountId);
        TestAssert.AreEqual(true, result[0].HasProfileImage);
        TestAssert.AreEqual(0.5d, result[0].WinRate);
        TestAssert.AreEqual(true, repository.WasCalled);
    }

    internal static void GetLeaderboard_InvalidLimitThroughWcf_ReturnsTypedFault()
    {
        LeaderboardService service = new LeaderboardService(
            new LeaderboardApplicationService(new LeaderboardRepositoryStub()));
        FaultException<LeaderboardFault> fault = TestAssert.Throws<FaultException<LeaderboardFault>>(() =>
            Invoke<ILeaderboardService, object>(service, channel => channel.GetLeaderboard(0)));
        TestAssert.AreEqual(LeaderboardFailure.InvalidMaximumEntries, fault.Detail.Reason);
    }

    internal static void GetLeaderboard_UnavailableThroughWcf_ReturnsServiceFault()
    {
        LeaderboardService service = new LeaderboardService(new LeaderboardApplicationService(
            new LeaderboardRepositoryStub { ThrowOnGet = true }));
        FaultException<ServiceFault> fault = TestAssert.Throws<FaultException<ServiceFault>>(() =>
            Invoke<ILeaderboardService, object>(service, channel => channel.GetLeaderboard(10)));
        TestAssert.AreEqual("LEADERBOARD_OPERATION_FAILURE", fault.Detail.FailureCode);
        TestAssert.AreEqual(true, !string.IsNullOrWhiteSpace(fault.Detail.CorrelationId));
    }

    private static void AssertSessionFault(ProfileBoundaryContext context, string token, SessionFailure expected)
    {
        FaultException<SessionFault> fault = TestAssert.Throws<FaultException<SessionFault>>(() =>
            Invoke<IPlayerProfileService, object>(context.Service,
                channel => channel.GetPlayerProfile(token, null)));
        TestAssert.AreEqual(expected, fault.Detail.Reason);
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
}
