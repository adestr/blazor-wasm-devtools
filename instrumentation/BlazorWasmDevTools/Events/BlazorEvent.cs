namespace BlazorWasmDevTools.Events;

public sealed record BlazorEvent(
    long Sequence,
    string Name,
    Dictionary<string, object?> Properties,
    string Source,
    long TimestampUnixMilliseconds
) : IBlazorEvent;