using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using ReversiGameSystem.Server.Contracts;

namespace ReversiGameSystem.Server.ServiceApi.Wcf;

internal static class CorrelationContext
{
    private const string HeaderName = "CorrelationId";

    internal static string GetOrCreate()
    {
        OperationContext? operationContext = OperationContext.Current;
        string correlationId = Read(operationContext?.IncomingMessageHeaders)
            ?? Guid.NewGuid().ToString("D");
        if (operationContext != null)
        {
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

        int headerIndex = headers.FindHeader(HeaderName, ContractNamespaces.Diagnostics);
        if (headerIndex < 0)
        {
            return null;
        }

        try
        {
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
