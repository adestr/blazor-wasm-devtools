namespace BlazorWasmDevTools.Models;

public sealed record DevToolsComponentSnapshot(
    IReadOnlyList<ComponentDescriptor> Components,
    IReadOnlyList<ComponentLifecycleEvent> RecentEvents);
