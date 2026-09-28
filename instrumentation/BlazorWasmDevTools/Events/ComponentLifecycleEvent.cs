using BlazorWasmDevTools.Models;

namespace BlazorWasmDevTools.Events;

/// <summary>
/// Serializable lifecycle notification sent to the browser extension hook.
/// </summary>
public sealed record ComponentLifecycleEvent(
    long Sequence,
    int ComponentId,
    string ComponentType,
    int? ParentComponentId,
    ComponentLifecyclePhase Phase,
    bool FirstRender,
    string Source,
    long TimestampUnixMilliseconds) : IBlazorEvent;

