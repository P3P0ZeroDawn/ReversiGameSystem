using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Players;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Services;
using ContractProfile = ReversiGameSystem.Server.Contracts.Players.PlayerProfile;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>Exposes public profile lookup and authenticated profile modifications.</summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
    ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
public sealed class PlayerProfileService : IPlayerProfileService
{
    private readonly PlayerProfileReader _reader;
    private readonly PlayerProfileEditor _editor;
    private readonly ILogger<PlayerProfileService> _logger;

    /// <summary>Initializes the profile boundary with shared server sessions.</summary>
    /// <param name="reader">The authenticated public profile reader.</param>
    /// <param name="editor">The authenticated profile mutation use cases.</param>
    /// <param name="logger">The boundary diagnostic logger.</param>
    /// <exception cref="ArgumentNullException">A required dependency is absent.</exception>
    public PlayerProfileService(PlayerProfileReader reader, PlayerProfileEditor editor,
        ILogger<PlayerProfileService> logger)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ContractProfile GetPlayerProfile(string sessionToken, long? targetAccountId) =>
        Execute(nameof(GetPlayerProfile), () => PlayerDataMapper.Map(
            _reader.GetProfile(sessionToken, targetAccountId)));

    /// <inheritdoc />
    public ImageContent? GetProfileImage(string sessionToken, long? targetAccountId) =>
        Execute(nameof(GetProfileImage), () =>
        {
            ProfileImageData? image = _reader.GetImage(sessionToken, targetAccountId);
            return image == null ? null : new ImageContent
            {
                MediaType = image.MediaType, Content = image.Content
            };
        });

    /// <inheritdoc />
    public void ChangeUsername(string sessionToken, string username) =>
        Execute(nameof(ChangeUsername), () =>
        {
            _editor.ChangeUsername(sessionToken, username);
            return true;
        });

    /// <inheritdoc />
    public void UpdateProfileImage(string sessionToken, ImageContent image) =>
        Execute(nameof(UpdateProfileImage), () =>
        {
            _editor.UpdateImage(sessionToken, image == null ? null
                : new ProfileImageData(image.Content, image.MediaType));
            return true;
        });

    private TResult Execute<TResult>(string operation, Func<TResult> action)
    {
        string correlationId = CorrelationContext.GetOrCreate();
        try
        {
            return action();
        }
        catch (ProfileRejectedException exception)
        {
            throw MapRejection(exception.Reason);
        }
        catch (Exception exception)
        {
            _logger.LogError("The {Operation} operation failed. "
                + "CorrelationId: {CorrelationId}. {Diagnostics}",
                operation, correlationId, FailureDiagnostics.Describe(exception));
            throw ServiceFaultFactory.Create(operation, correlationId);
        }
    }

    private static FaultException MapRejection(ProfileRejection reason)
    {
        if (reason == ProfileRejection.MissingSessionToken
            || reason == ProfileRejection.InvalidSession)
        {
            return new FaultException<SessionFault>(new SessionFault
            {
                Reason = reason == ProfileRejection.MissingSessionToken
                    ? SessionFailure.MissingSessionToken : SessionFailure.InvalidSession
            }, "The session request was rejected.");
        }

        ProfileFailure failure = reason switch
        {
            ProfileRejection.InvalidAccountId => ProfileFailure.InvalidAccountId,
            ProfileRejection.AccountNotFound => ProfileFailure.AccountNotFound,
            ProfileRejection.InvalidUsername => ProfileFailure.InvalidUsername,
            ProfileRejection.UsernameUnavailable => ProfileFailure.UsernameUnavailable,
            ProfileRejection.InvalidImage => ProfileFailure.InvalidImage,
            ProfileRejection.InactiveAccount => ProfileFailure.InactiveAccount,
            _ => throw new ArgumentOutOfRangeException(nameof(reason))
        };
        return new FaultException<ProfileFault>(new ProfileFault { Reason = failure },
            "The profile request was rejected.");
    }
}
