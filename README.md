# Blazor WASM DevTools

These tools work on the same principle as many other UI framework developer tools: a global hook on `window` that the app (via a companion package) can register with, and a browser extension that activates and inspects it.

## Chrome extension layout

| File | Context | Role |
|------|---------|------|
| `inject.js` | Inspected page (`MAIN`, `document_start`) | Defines `window.__BLAZOR_WASM_DEVTOOLS_GLOBAL_HOOK__` before Blazor loads |
| `page-bridge.js` | DevTools extension pages | `eval` + `CustomEvent` to talk to the hook in the page |
| `panel.html` / `panel.js` | Custom DevTools panel UI | Activate hook and show status |

After loading the unpacked extension, **reload** any open tabs so `inject.js` runs. Open the **Blazor WASM** panel and use **Initialize DevTools** (or switch to the panel tab) to call `hook.activate()`. In the page console you should see `window.__BLAZOR_WASM_DEVTOOLS_GLOBAL_HOOK__` and `typeof Blazor !== 'undefined'` on Blazor apps.
