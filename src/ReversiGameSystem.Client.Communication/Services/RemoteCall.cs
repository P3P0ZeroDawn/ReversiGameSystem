using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal static class RemoteCall
{
    private const string RequestFailureMessagePrefix = "The remote request failed. CorrelationId: ";
    private const string ConnectionFailureMessage =
        "The remote connection failed. CorrelationId: {CorrelationId}";
    private const string CorrelationHeaderName = "CorrelationId";
    private const string CorrelationHeaderNamespace = "urn:reversi:diagnostics:v1";

    internal static async Task<TResult> SendServiceCall<TContract, TResult, TCategory>(
        ClientBase<TContract> client, Func<Task<TResult>> operation, ILogger<TCategory> logger)
        where TContract : class
    {
        string correlationId = Guid.NewGuid().ToString("D");
        try
        {
            Task<TResult> pending;
            using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
            {
                OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader(
                    CorrelationHeaderName, CorrelationHeaderNamespace, correlationId));
                pending = operation();
            }
            TResult value = await pending.ConfigureAwait(false);
            CloseAcknowledgedChannel(client, logger, correlationId);
            return value;
        }
        catch (Exception exception) when (IsConnectionFailure(exception))
        {
            logger.LogWarning(exception,
                ConnectionFailureMessage, correlationId);
            string message = RequestFailureMessagePrefix + correlationId;
            if (exception is TimeoutException)
            {
                throw new TimeoutException(message, exception);
            }
            throw new CommunicationException(message, exception);
        }
        finally
        {
            client.Abort();
        }
    }

    internal static async Task SendServiceCall<TContract, TCategory>(
        ClientBase<TContract> client, Func<Task> operation, ILogger<TCategory> logger)
        where TContract : class
    {
        string correlationId = Guid.NewGuid().ToString("D");
        try
        {
            Task pending;
            using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
            {
                OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader(
                    CorrelationHeaderName, CorrelationHeaderNamespace, correlationId));
                pending = operation();
            }
            await pending.ConfigureAwait(false);
            CloseAcknowledgedChannel(client, logger, correlationId);
        }
        catch (Exception exception) when (IsConnectionFailure(exception))
        {
            logger.LogWarning(exception,
                ConnectionFailureMessage, correlationId);
            string message = RequestFailureMessagePrefix + correlationId;
            if (exception is TimeoutException)
            {
                throw new TimeoutException(message, exception);
            }
            throw new CommunicationException(message, exception);
        }
        finally
        {
            client.Abort();
        }
    }

    private static void CloseAcknowledgedChannel<TContract>(ClientBase<TContract> client,
        ILogger logger, string correlationId) where TContract : class
    {
        try
        {
            client.Close();
        }
        catch (Exception exception) when (IsConnectionFailure(exception))
        {
            logger.LogWarning(exception,
                "The acknowledged channel could not close. CorrelationId: {CorrelationId}",
                correlationId);
        }
    }

    internal static bool IsConnectionFailure(Exception exception)
    {
        return exception is TimeoutException
            || (exception is CommunicationException && exception is not FaultException);
    }

    internal static bool IsDeclaredFault(Exception exception)
    {
        return exception is FaultException<Proxy.ServiceFault>
            or FaultException<Proxy.SessionFault>
            or FaultException<Proxy.AccountFault>
            or FaultException<Proxy.ProfileFault>
            or FaultException<Proxy.PreferencesFault>
            or FaultException<Proxy.LeaderboardFault>
            or FaultException<Proxy.FriendshipFault>
            or FaultException<Proxy.MatchFault>
            or FaultException<Proxy.ChatFault>;
    }

    internal static string ReadServerCorrelation(Exception exception)
    {
        string correlationId = string.Empty;
        if (exception is FaultException<Proxy.ServiceFault> fault)
        {
            correlationId = fault.Detail.CorrelationId;
        }
        return correlationId;
    }
}
