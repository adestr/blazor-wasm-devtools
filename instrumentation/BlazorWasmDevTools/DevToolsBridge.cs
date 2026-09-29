using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorWasmDevTools.Events;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace BlazorWasmDevTools;

public sealed class DevToolsBridge : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public DevToolsBridge(IJSRuntime js, IEventSource componentLifecycleHub, ILogger<DevToolsBridge> logger)
    {
        _jsRuntime = js;
        _lifecycleHub = componentLifecycleHub;
        _logger = logger;

        _dotNetRef = DotNetObjectReference.Create(this);

        _lifecycleHub.LifecyclePublished += PublishLifecycleEvent;
    }

    private readonly IJSRuntime _jsRuntime;

    private readonly IEventSource _lifecycleHub;

    private IJSObjectReference? _module;

    private readonly DotNetObjectReference<DevToolsBridge>? _dotNetRef;

    private bool _initialized;

    private ILogger<DevToolsBridge> _logger;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./_content/BlazorWasmDevTools/blazorWasmDevTools.js");

        await _module.InvokeVoidAsync("initialize", _dotNetRef);
        _initialized = true;
    }

    [JSInvokable]
    public void OnDevToolsActivated()
    {
        foreach (var lifecycleEvent in _lifecycleHub.GetSnapshot().RecentEvents)
        {
            PublishLifecycleEvent(lifecycleEvent);
        }

        _lifecycleHub.SetActive(true);
        _ = RegisterRendererAsync();
    }

    [JSInvokable]
    public string GetSnapshotJson()
    {
        var snapshot = _lifecycleHub.GetSnapshot();
        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    private void PublishLifecycleEvent(ComponentLifecycleEvent lifecycleEvent)
    {
        if (_module is null)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(lifecycleEvent, JsonOptions);
        _ = _module.InvokeVoidAsync("publishLifecycle", payload);
    }

    private async Task RegisterRendererAsync()
    {
        if (_module is null || _dotNetRef is null)
        {
            return;
        }

        await _module.InvokeVoidAsync("registerRenderer", _dotNetRef);
    }

    public async ValueTask DisposeAsync()
    {
        _lifecycleHub.LifecyclePublished -= PublishLifecycleEvent;
        _dotNetRef?.Dispose();
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }
}
