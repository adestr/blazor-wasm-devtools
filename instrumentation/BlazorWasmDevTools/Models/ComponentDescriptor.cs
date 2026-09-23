namespace BlazorWasmDevTools.Models;

public sealed record ComponentDescriptor(
    int ComponentId,
    string ComponentType,
    int? ParentComponentId,
    ComponentLifecyclePhase LastPhase,
    long LastUpdatedUnixMilliseconds);
