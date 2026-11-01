using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using ReversiGameSystem.Server.Contracts;

namespace ReversiGameSystem.Server.ServiceApi;

internal static class CorrelationContext
{
    private const string HeaderName = "CorrelationId";

    internal static string GetOrCreate()
    {
        OperationContext? operationContext = OperationContext.Current;
        string? outgoingCorrelationId = Read(operationContext?.OutgoingMessageHeaders);
        string correlationId = outgoingCorrelationId
            ?? Read(operationContext?.IncomingMessageHeaders)
            ?? Guid.NewGuid().ToString("D");
        if (operationContext != null && outgoingCorrelationId == null)
        {
            operationContext.OutgoingMessageHeaders.RemoveAll(
                HeaderName,
                ContractNamespaces.Diagnostics);
            MessageHeader header = MessageHeader.CreateHeader(
                HeaderName,
                ContractNamespaces.Diagnostics,
                correlationId);
            operationContext.OutgoingMessageHeaders.Add(header);
        }

        return correlationId;
    }

    private static string? Read(MessageHeaders? headers)
    {
        if (headers == null)
        {
            return null;
        }

        try
        {
            int headerIndex = headers.FindHeader(HeaderName, ContractNamespaces.Diagnostics);
            if (headerIndex < 0)
            {
                return null;
            }

            string value = headers.GetHeader<string>(headerIndex);
            return Guid.TryParse(value, out Guid parsed)
                ? parsed.ToString("D")
                : null;
        }
        catch (Exception exception) when (exception is MessageHeaderException
                                            or SerializationException)
        {
            return null;
        }
    }
}
