using BlazorWasmDevTools.Events;
using BlazorWasmDevTools.Lifecycle;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BlazorWasmDevTools;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Blazor WASM DevTools services, renderer lifecycle logging, and the JS bridge.
    /// </summary>
    /// <remarks>
    /// You can invoke <see cref="AddBlazorWasmDevTools" /> on your <see cref="IServiceCollection" /> or on <see cref="WebAssemblyHostBuilder" />. If you choose to invoke from <see cref="IServiceCollection" />, you will need to configure the logging level appropriately.
    /// </remarks>
    public static IServiceCollection AddBlazorWasmDevTools(this IServiceCollection services)
    {
        services.AddLogging();

        services.AddSingleton<DevToolsBridge>();
        services.AddScoped<IComponentActivator, InstrumentedComponentActivator>();

        services.AddSingleton<EventHub>();
        services.AddSingleton<IEventSource>(sp => sp.GetRequiredService<EventHub>());
        services.AddSingleton<IEventSink>(sp => sp.GetRequiredService<EventHub>());

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, RendererLifecycleLoggerProvider>());

        return services;
    }
}
