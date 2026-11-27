using System;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Server.ServiceApi;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Services;

internal static class WcfTestHostHelper
{
    internal static WcfChannelScope<TContract> OpenChannel<TContract>(
        object serviceInstance,
        string endpointName,
        ILogger<ServiceErrorBehavior>? errorLogger = null)
        where TContract : class
    {
        OpenedHost opened = OpenHost(
            serviceInstance,
            typeof(TContract),
            endpointName,
            errorLogger);
        ChannelFactory<TContract> factory = new ChannelFactory<TContract>(
            opened.Binding,
            new EndpointAddress(opened.EndpointAddress));
        TContract channel = factory.CreateChannel();
        return new WcfChannelScope<TContract>(
            opened.Host,
            factory,
            channel,
            opened.EndpointAddress);
    }

    internal static WcfChannelScope<TContract> OpenDuplexChannel<TContract, TCallback>(
        object serviceInstance,
        string endpointName,
        TCallback callback,
        ILogger<ServiceErrorBehavior>? errorLogger = null)
        where TContract : class
        where TCallback : class
    {
        OpenedHost opened = OpenHost(
            serviceInstance,
            typeof(TContract),
            endpointName,
            errorLogger);
        DuplexChannelFactory<TContract> factory = new DuplexChannelFactory<TContract>(
            new InstanceContext(callback),
            opened.Binding,
            new EndpointAddress(opened.EndpointAddress));
        TContract channel = factory.CreateChannel();
        return new WcfChannelScope<TContract>(
            opened.Host,
            factory,
            channel,
            opened.EndpointAddress);
    }

    internal static DuplexChannelFactory<TContract> CreateDuplexFactory<TContract, TCallback>(
        Uri endpointAddress,
        TCallback callback)
        where TContract : class
        where TCallback : class
    {
        NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
        return new DuplexChannelFactory<TContract>(
            new InstanceContext(callback),
            binding,
            new EndpointAddress(endpointAddress));
    }

    internal static void CloseOrAbort(object? target)
    {
        if (target is not ICommunicationObject communicationObject)
        {
            return;
        }

        try
        {
            if (communicationObject.State == CommunicationState.Opened)
            {
                communicationObject.Close();
            }
            else
            {
                communicationObject.Abort();
            }
        }
        catch (Exception exception) when (exception is CommunicationException
            or TimeoutException)
        {
            communicationObject.Abort();
        }
    }

    private static OpenedHost OpenHost(
        object serviceInstance,
        Type contractType,
        string endpointName,
        ILogger<ServiceErrorBehavior>? errorLogger)
    {
        Uri baseAddress = new Uri(
            "net.pipe://localhost/ReversiGameSystem.Tests/"
            + Guid.NewGuid().ToString("N") + "/");
        NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
        ServiceHost host = new ServiceHost(serviceInstance, baseAddress);
        host.Description.Behaviors.Add(new ServiceErrorBehavior(
            errorLogger ?? NullLogger<ServiceErrorBehavior>.Instance));
        host.AddServiceEndpoint(contractType, binding, endpointName);
        host.Open();
        return new OpenedHost(host, new Uri(baseAddress, endpointName), binding);
    }

    private sealed class OpenedHost
    {
        internal OpenedHost(
            ServiceHost host,
            Uri endpointAddress,
            NetNamedPipeBinding binding)
        {
            Host = host;
            EndpointAddress = endpointAddress;
            Binding = binding;
        }

        internal ServiceHost Host { get; }

        internal Uri EndpointAddress { get; }

        internal NetNamedPipeBinding Binding { get; }
    }
}

internal sealed class WcfChannelScope<TContract> : IDisposable
    where TContract : class
{
    internal WcfChannelScope(
        ServiceHost host,
        ChannelFactory<TContract> factory,
        TContract channel,
        Uri endpointAddress)
    {
        Host = host;
        Factory = factory;
        Channel = channel;
        EndpointAddress = endpointAddress;
    }

    internal ServiceHost Host { get; }

    internal ChannelFactory<TContract> Factory { get; }

    internal TContract Channel { get; }

    internal Uri EndpointAddress { get; }

    internal CommunicationState ChannelState
    {
        get
        {
            return ((ICommunicationObject)Channel).State;
        }
    }

    public void Dispose()
    {
        WcfTestHostHelper.CloseOrAbort(Channel);
        WcfTestHostHelper.CloseOrAbort(Factory);
        WcfTestHostHelper.CloseOrAbort(Host);
    }
}
