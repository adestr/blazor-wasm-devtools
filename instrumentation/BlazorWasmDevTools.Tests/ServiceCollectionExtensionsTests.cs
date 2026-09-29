using BlazorWasmDevTools.Events;
using BlazorWasmDevTools.Lifecycle;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;

namespace BlazorWasmDevTools.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddBlazorWasmDevTools_RegistersCoreServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IJSRuntime>());
        services.AddBlazorWasmDevTools();

        await using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<DevToolsBridge>());
        Assert.NotNull(provider.GetService<EventHub>());
        Assert.Same(provider.GetService<EventHub>(), provider.GetService<IEventSource>());
        Assert.Same(provider.GetService<EventHub>(), provider.GetService<IEventSink>());
        Assert.IsType<InstrumentedComponentActivator>(provider.GetService<IComponentActivator>());
        Assert.Contains(
            provider.GetServices<ILoggerProvider>(),
            providerItem => providerItem is RendererLifecycleLoggerProvider);
    }
}
