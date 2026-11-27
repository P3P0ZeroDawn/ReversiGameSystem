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
        ServiceHost host = OpenHost(
            serviceInstance,
            typeof(TContract),
            endpointName,
            errorLogger,
            out Uri endpointAddress,
            out NetNamedPipeBinding binding);
        ChannelFactory<TContract> factory = new ChannelFactory<TContract>(
            binding,
            new EndpointAddress(endpointAddress));
        TContract channel = factory.CreateChannel();
        return new WcfChannelScope<TContract>(host, factory, channel);
    }

    internal static WcfChannelScope<TContract> OpenDuplexChannel<TContract, TCallback>(
        object serviceInstance,
        string endpointName,
        TCallback callback,
        ILogger<ServiceErrorBehavior>? errorLogger = null)
        where TContract : class
        where TCallback : class
    {
        ServiceHost host = OpenHost(
            serviceInstance,
            typeof(TContract),
            endpointName,
            errorLogger,
            out Uri endpointAddress,
            out NetNamedPipeBinding binding);
        DuplexChannelFactory<TContract> factory = new DuplexChannelFactory<TContract>(
            new InstanceContext(callback),
            binding,
            new EndpointAddress(endpointAddress));
        TContract channel = factory.CreateChannel();
        return new WcfChannelScope<TContract>(host, factory, channel);
    }

    internal static ServiceHost OpenHost(
        object serviceInstance,
        Type contractType,
        string endpointName,
        ILogger<ServiceErrorBehavior>? errorLogger,
        out Uri endpointAddress,
        out NetNamedPipeBinding binding)
    {
        Uri baseAddress = new Uri(
            "net.pipe://localhost/ReversiGameSystem.Tests/"
            + Guid.NewGuid().ToString("N") + "/");
        binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
        ServiceHost host = new ServiceHost(serviceInstance, baseAddress);
        host.Description.Behaviors.Add(new ServiceErrorBehavior(
            errorLogger ?? NullLogger<ServiceErrorBehavior>.Instance));
        host.AddServiceEndpoint(contractType, binding, endpointName);
        host.Open();
        endpointAddress = new Uri(baseAddress, endpointName);
        return host;
    }

    internal static void CloseOrAbort(ICommunicationObject? communicationObject)
    {
        if (communicationObject == null)
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
}

internal sealed class WcfChannelScope<TContract> : IDisposable
    where TContract : class
{
    internal WcfChannelScope(
        ServiceHost host,
        ChannelFactory<TContract> factory,
        TContract channel)
    {
        Host = host;
        Factory = factory;
        Channel = channel;
    }

    internal ServiceHost Host { get; }

    internal ChannelFactory<TContract> Factory { get; }

    internal TContract Channel { get; }

    public void Dispose()
    {
        WcfTestHostHelper.CloseOrAbort((ICommunicationObject)Channel);
        WcfTestHostHelper.CloseOrAbort(Factory);
        WcfTestHostHelper.CloseOrAbort(Host);
    }
}
