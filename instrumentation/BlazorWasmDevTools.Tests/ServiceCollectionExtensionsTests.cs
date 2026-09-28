using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
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
        Assert.NotNull(provider.GetService<IComponentActivator>());
        Assert.IsType<InstrumentedComponentActivator>(provider.GetService<IComponentActivator>());
    }
}
