using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal static class RemoteCall
{
    internal static async Task<TResult> ExecuteAsync<TContract, TResult, TCategory>(
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
                    "CorrelationId", "urn:reversi:diagnostics:v1", correlationId));
                pending = operation();
            }

            TResult result = await pending;
            try
            {
                client.Close();
            }
            catch (Exception exception) when (
                exception is CommunicationException or TimeoutException)
            {
                logger.LogWarning("The acknowledged remote channel could not close. "
                    + "CorrelationId: {CorrelationId}", correlationId);
            }
            return result;
        }
        catch (FaultException<Proxy.ServiceFault> fault)
        {
            logger.LogWarning("The server returned {FailureCode}. "
                + "ServerCorrelationId: {ServerCorrelationId}. CorrelationId: {CorrelationId}",
                fault.Detail.FailureCode, fault.Detail.CorrelationId, correlationId);
            throw;
        }
        catch (Exception exception) when (IsExpectedFailure(exception))
        {
            logger.LogWarning("The remote request failed with {ExceptionType}. "
                + "CorrelationId: {CorrelationId}",
                exception.GetType().FullName, correlationId);
            throw;
        }
        finally
        {
            client.Abort();
        }
    }

    internal static bool IsExpectedFailure(Exception exception) =>
        exception is CommunicationException or TimeoutException or SerializationException
            or InvalidOperationException;
}
