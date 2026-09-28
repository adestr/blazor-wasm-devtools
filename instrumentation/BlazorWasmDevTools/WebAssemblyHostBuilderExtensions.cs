using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;

namespace BlazorWasmDevTools;

public static class WebAssemblyHostBuilderExtensions
{
    /// <summary>
    /// Adds DevTools instrumentation and enables renderer debug logs used for component lifecycle events.
    /// </summary>
    public static WebAssemblyHostBuilder AddBlazorWasmDevTools(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddBlazorWasmDevTools();
        builder.Logging.AddFilter("Microsoft.AspNetCore.Components.RenderTree.Renderer", LogLevel.Debug);
        return builder;
    }
}
