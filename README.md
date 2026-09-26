# Blazor WASM DevTools

These tools work on the same principle as many other UI framework developer tools: a global hook on `window` that the app (via a companion package) can register with, and a browser extension that activates and inspects it.

## Chrome extension layout

| File | Context | Role |
|------|---------|------|
| `inject.js` | Inspected page (`MAIN`, `document_start`) | Defines `window.__BLAZOR_WASM_DEVTOOLS_GLOBAL_HOOK__` before Blazor loads |
| `page-bridge.js` | DevTools extension pages | `eval` + `CustomEvent` to talk to the hook in the page |
| `panel/` (built from `ui/`) | Custom DevTools panel UI | React panel; activate hook and stream lifecycle events |
| `page-bridge.js` (built from `ui/`) | DevTools extension pages | Talk to the hook in the inspected page |

Build the panel before loading the extension:

```bash
pnpm install --dir ui
pnpm build
```

After loading the unpacked extension from `browsers/chrome`, **reload** any open tabs so `inject.js` runs. Open the **Blazor WASM** panel and use **Initialize DevTools** (or switch to the panel tab) to call `hook.activate()`. In the page console you should see `window.__BLAZOR_WASM_DEVTOOLS_GLOBAL_HOOK__` and `typeof Blazor !== 'undefined'` on Blazor apps.

Panel UI source lives in [`ui/`](ui/); `pnpm build` writes `browsers/chrome/page-bridge.js` and `browsers/chrome/panel/`.

## NuGet instrumentation

See [instrumentation/README.md](instrumentation/README.md) for the `BlazorWasmDevTools` package (`builder.AddBlazorWasmDevTools()`), which listens for `hook.on("activate")`, registers a `blazor-wasm` renderer, and streams component lifecycle events to the extension.
