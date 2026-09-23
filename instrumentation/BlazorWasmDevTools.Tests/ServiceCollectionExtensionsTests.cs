using BlazorWasmDevTools.Bridge;
using BlazorWasmDevTools.Extensions;
using BlazorWasmDevTools.Hosting;
using BlazorWasmDevTools.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

        Assert.NotNull(provider.GetService<ComponentLifecycleHub>());
        Assert.NotNull(provider.GetService<BlazorWasmDevToolsBridge>());
        Assert.NotNull(provider.GetService<IComponentLifecycleSink>());
        Assert.Contains(
            provider.GetServices<IHostedService>(),
            service => service is BlazorWasmDevToolsInitializer);
    }
}
