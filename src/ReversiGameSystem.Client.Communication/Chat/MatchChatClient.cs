using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReversiGameSystem.Chat;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

[CallbackBehavior(UseSynchronizationContext = false, ConcurrencyMode = ConcurrencyMode.Multiple)]
internal sealed class MatchChatClient : IDisposable, Proxy.IMatchChatServiceCallback
{
    private readonly string _sessionToken;
    private readonly object _chatDataLock = new object();
    private readonly DuplexChannel<Proxy.MatchChatServiceClient, Proxy.IMatchChatService> _channel;

    private readonly Dictionary<long, SortedDictionary<int, ChatMessage>> _messagesByMatch =
        new Dictionary<long, SortedDictionary<int, ChatMessage>>();
    private DateTime? _muteEndsAtUtc;

    internal MatchChatClient(string sessionToken, ILogger<MatchChatClient> logger)
        : this(sessionToken, null, logger)
    {
    }

    internal MatchChatClient(string sessionToken, string? endpoint, ILogger<MatchChatClient> logger)
    {
        _sessionToken = sessionToken;
        Proxy.MatchChatServiceClient CreateClient()
        {
            Proxy.MatchChatServiceClient client;
            if (endpoint == null)
            {
                client = ServerConnectionFactory.CreateChatClient(new InstanceContext(this));
            }
            else
            {
                client = new Proxy.MatchChatServiceClient(new InstanceContext(this),
                    DuplexChannel<Proxy.MatchChatServiceClient, Proxy.IMatchChatService>
                        .CreateOverrideBinding(endpoint), new EndpointAddress(endpoint));
            }
            return client;
        }
        _channel = new DuplexChannel<Proxy.MatchChatServiceClient, Proxy.IMatchChatService>(
            CreateClient, logger);
    }

    internal event Action<long>? MessagesChanged;
    internal event Action? MuteChanged;

    internal DateTime? MuteEndsAtUtc
    {
        get
        {
            lock (_chatDataLock)
            {
                return _muteEndsAtUtc;
            }
        }
    }

    internal Task<ChatStatus> OpenChatChannel(
        long matchId, CancellationToken cancellation = default)
    {
        return SendChatOperation(
            client => client.OpenChatChannelAsync(_sessionToken, matchId), cancellation);
    }

    internal Task<ChatStatus> LoadChatHistory(
        long matchId, CancellationToken cancellation = default)
    {
        async Task LoadMessages(Proxy.MatchChatServiceClient client)
        {
            Proxy.MatchChatEntry[] messageHistory = await client.GetChatHistoryAsync(
                _sessionToken, matchId)
                .ConfigureAwait(false);
            foreach (Proxy.MatchChatEntry message in messageHistory)
            {
                StoreMessage(MapMessage(message));
            }
        }
        return SendChatOperation(LoadMessages, cancellation);
    }

    internal Task<ChatStatus> SendChatMessage(
        long matchId, string content, CancellationToken cancellation = default)
    {
        async Task SubmitMessage(Proxy.MatchChatServiceClient client)
        {
            Proxy.MatchChatEntry message = await client.SendChatMessageAsync(
                _sessionToken, matchId, content.Trim()).ConfigureAwait(false);
            StoreMessage(MapMessage(message));
        }
        return SendChatOperation(SubmitMessage, cancellation);
    }

    private async Task<ChatStatus> SendChatOperation(
        Func<Proxy.MatchChatServiceClient, Task> operation, CancellationToken cancellation)
    {
        ChatStatus status;
        try
        {
            await _channel.SendServiceCall(operation, cancellation).ConfigureAwait(false);
            status = ChatStatus.Success;
        }
        catch (FaultException<Proxy.ChatFault> fault)
            when (Enum.IsDefined(typeof(Proxy.ChatFailure), fault.Detail.Reason))
        {
            status = MapChatFailure(fault.Detail.Reason);
            if (fault.Detail.MuteEndsAtUtc.HasValue)
            {
                UpdateMute(fault.Detail.MuteEndsAtUtc.Value);
            }
        }
        catch (CommunicationException exception) when (CanMapAccessFailure(exception))
        {
            status = MapAccessFailure(exception);
        }
        catch (CommunicationException exception) when (exception is not FaultException)
        {
            status = ChatStatus.ServiceUnavailable;
        }
        catch (TimeoutException)
        {
            status = ChatStatus.ServiceUnavailable;
        }
        return status;
    }

    private static bool CanMapAccessFailure(CommunicationException exception)
    {
        bool recognized = exception is FaultException<Proxy.SessionFault> session
            && Enum.IsDefined(typeof(Proxy.SessionFailure), session.Detail.Reason);
        if (exception is FaultException<Proxy.MatchFault> fault)
        {
            recognized = fault.Detail.Reason is Proxy.MatchFailure.InactiveAccount
                or Proxy.MatchFailure.Unauthorized
                or Proxy.MatchFailure.MatchNotFound
                or Proxy.MatchFailure.MatchNotInProgress;
        }
        return recognized;
    }

    private static ChatStatus MapAccessFailure(CommunicationException exception)
    {
        ChatStatus status = ChatStatus.ServiceUnavailable;
        if (exception is FaultException<Proxy.SessionFault>)
        {
            status = ChatStatus.InvalidSession;
        }
        else if (exception is FaultException<Proxy.MatchFault> fault)
        {
            status = fault.Detail.Reason switch
            {
                Proxy.MatchFailure.InactiveAccount => ChatStatus.InactiveAccount,
                Proxy.MatchFailure.Unauthorized => ChatStatus.Unauthorized,
                Proxy.MatchFailure.MatchNotFound => ChatStatus.MatchNotFound,
                Proxy.MatchFailure.MatchNotInProgress => ChatStatus.MatchNotInProgress,
                _ => throw new ArgumentOutOfRangeException(nameof(exception))
            };
        }
        return status;
    }

    private void UpdateMute(DateTime endsAtUtc)
    {
        lock (_chatDataLock)
        {
            _muteEndsAtUtc = endsAtUtc;
        }
        MuteChanged?.Invoke();
    }

    public void OnPlayerMuted(Proxy.ChatMuteNotification mute)
    {
        UpdateMute(mute.EndsAtUtc);
    }

    public void Dispose()
    {
        _channel.Dispose();
    }
    internal ChatMessage[] ReadMessages(long id)
    {
        lock (_chatDataLock)
        {
            if (!_messagesByMatch.ContainsKey(id))
            {
                return Array.Empty<ChatMessage>();
            }
            return _messagesByMatch[id].Values.ToArray();
        }
    }
    private void StoreMessage(ChatMessage message)
    {
        lock (_chatDataLock)
        {
            if (!_messagesByMatch.ContainsKey(message.MatchId))
            {
                _messagesByMatch.Add(
                    message.MatchId, new SortedDictionary<int, ChatMessage>());
            }
            if (_messagesByMatch[message.MatchId].ContainsKey(message.Number))
            {
                return;
            }
            _messagesByMatch[message.MatchId].Add(message.Number, message);
        }

        MessagesChanged?.Invoke(message.MatchId);
    }
    public void OnChatMessageReceived(Proxy.MatchChatEntry message)
    {
        StoreMessage(MapMessage(message));
    }
    private static ChatMessage MapMessage(Proxy.MatchChatEntry message)
    {
        return new ChatMessage(
            message.MatchId,
            message.MessageNumber,
            message.SenderSeatNumber,
            message.Content);
    }
    private static ChatStatus MapChatFailure(Proxy.ChatFailure failure)
    {
        ChatStatus status = failure switch
        {
            Proxy.ChatFailure.EmptyMessage => ChatStatus.EmptyMessage,
            Proxy.ChatFailure.MessageTooLong => ChatStatus.MessageTooLong,
            Proxy.ChatFailure.MessageBlocked => ChatStatus.MessageBlocked,
            Proxy.ChatFailure.PlayerMuted => ChatStatus.PlayerMuted,
            Proxy.ChatFailure.RateLimitExceeded => ChatStatus.RateLimitExceeded,
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
        return status;
    }
}
