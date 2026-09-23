using BlazorWasmDevTools.Bridge;
using BlazorWasmDevTools.Hosting;
using BlazorWasmDevTools.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BlazorWasmDevTools.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Blazor WASM DevTools services, renderer lifecycle logging, and the JS bridge.
    /// </summary>
    public static IServiceCollection AddBlazorWasmDevTools(this IServiceCollection services)
    {
        services.AddSingleton<ComponentLifecycleHub>();
        services.AddSingleton<BlazorWasmDevToolsBridge>();
        services.AddSingleton<IComponentLifecycleSink>(sp => sp.GetRequiredService<ComponentLifecycleHub>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, RendererLifecycleLoggerProvider>());
        services.AddHostedService<BlazorWasmDevToolsInitializer>();
        return services;
    }
}
