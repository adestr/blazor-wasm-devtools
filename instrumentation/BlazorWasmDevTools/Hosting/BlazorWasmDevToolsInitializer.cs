using BlazorWasmDevTools.Bridge;
using Microsoft.Extensions.Hosting;

namespace BlazorWasmDevTools.Hosting;

internal sealed class BlazorWasmDevToolsInitializer : IHostedService
{
    private readonly BlazorWasmDevToolsBridge _bridge;

    public BlazorWasmDevToolsInitializer(BlazorWasmDevToolsBridge bridge)
    {
        _bridge = bridge;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _bridge.InitializeAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
