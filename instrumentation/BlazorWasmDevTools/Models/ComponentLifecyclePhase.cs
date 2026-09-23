namespace BlazorWasmDevTools.Models;

/// <summary>
/// High-level component lifecycle phases reported to the DevTools extension.
/// </summary>
public enum ComponentLifecyclePhase
{
    Initializing,
    Initialized,
    ParametersSet,
    Rendering,
    AfterRender,
    Disposed,
}
