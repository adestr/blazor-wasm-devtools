namespace BlazorWasmDevTools.Tests.TestSupport;

internal static class StructuredLogState
{
    public static List<KeyValuePair<string, object?>> Create(params (string Key, object? Value)[] pairs) =>
        pairs.Select(pair => new KeyValuePair<string, object?>(pair.Key, pair.Value)).ToList();
}
