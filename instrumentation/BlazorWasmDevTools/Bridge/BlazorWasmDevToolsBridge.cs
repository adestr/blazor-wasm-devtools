using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorWasmDevTools.Lifecycle;
using BlazorWasmDevTools.Models;
using Microsoft.JSInterop;

namespace BlazorWasmDevTools.Bridge;

public sealed class BlazorWasmDevToolsBridge : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly ComponentLifecycleHub _lifecycleHub;
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;
    private DotNetObjectReference<BlazorWasmDevToolsBridge>? _dotNetRef;
    private bool _initialized;

    public BlazorWasmDevToolsBridge(ComponentLifecycleHub lifecycleHub, IJSRuntime jsRuntime)
    {
        _lifecycleHub = lifecycleHub;
        _jsRuntime = jsRuntime;
        _lifecycleHub.LifecyclePublished += PublishLifecycleEvent;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./_content/BlazorWasmDevTools/blazorWasmDevTools.js");

        _dotNetRef = DotNetObjectReference.Create(this);
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
