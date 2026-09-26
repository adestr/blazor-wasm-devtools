# Blazor WASM DevTools panel (Vite + React)

Source for the Chrome DevTools custom panel. Built artifacts are written into `../browsers/chrome/`.

## Commands

```bash
pnpm install
pnpm dev      # local UI only (no chrome.devtools APIs)
pnpm build    # page-bridge.js + panel/ for the extension
pnpm lint     # TypeScript check
```

From the repository root:

```bash
pnpm install --dir ui
pnpm build
```

## Output

| Build step | Output |
|------------|--------|
| `build:bridge` | `browsers/chrome/page-bridge.js` (shared with `devtools.html`) |
| `build:panel` | `browsers/chrome/panel/` (`index.html` + assets) |

Load the unpacked extension from `browsers/chrome` after running `pnpm build`.
