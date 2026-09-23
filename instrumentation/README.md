# BlazorWasmDevTools (NuGet)

Instrumentation package for Blazor WebAssembly apps that connects to the **Blazor WASM DevTools** browser extension.

## Install

```bash
dotnet add package BlazorWasmDevTools
```

Or reference the project:

```bash
dotnet add reference ../instrumentation/BlazorWasmDevTools/BlazorWasmDevTools.csproj
```

## Setup

In `Program.cs`:

```csharp
using BlazorWasmDevTools;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.AddBlazorWasmDevTools();
// ... root components, services
await builder.Build().RunAsync();
```

This registers:

- A JS module that listens for `hook.on("activate", …)` on `window.__BLAZOR_WASM_DEVTOOLS_GLOBAL_HOOK__`
- Renderer debug logging (`Microsoft.AspNetCore.Components.RenderTree.Renderer`) mapped to lifecycle events
- A `blazor-wasm` renderer registration on the hook when DevTools activates

Reload the page after installing the extension so `inject.js` runs before Blazor starts.

## Component lifecycle

| Source | Phases |
|--------|--------|
| Renderer logs | `Initializing`, `Rendering`, `Disposed` (all components) |
| `DevToolsComponentBase` | `Initialized`, `ParametersSet`, `AfterRender`, `Disposed` (opt-in) |

Derive from `DevToolsComponentBase` when you need finer-grained phases than renderer logging provides:

```csharp
@inherits BlazorWasmDevTools.Components.DevToolsComponentBase
```

Events are buffered in .NET and forwarded to the extension hook as `lifecycle` events after activation.

## Build and test

Tests use **xUnit** as the runner, with **bUnit** for Blazor component tests, **NSubstitute** for JS/DI mocks, and **AutoFixture** where sample data helps.

```bash
cd instrumentation
dotnet test
dotnet pack BlazorWasmDevTools/BlazorWasmDevTools.csproj -c Release
```

The `.nupkg` is written to `BlazorWasmDevTools/bin/Release/`.
