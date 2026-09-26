import { defineConfig } from "vite";

export default defineConfig({
  build: {
    outDir: "../browsers/chrome",
    emptyOutDir: false,
    lib: {
      entry: "src/bridge/page-bridge.ts",
      formats: ["iife"],
      name: "BlazorWasmDevToolsPageBridge",
      fileName: () => "page-bridge.js",
    },
    rollupOptions: {
      output: {
        extend: true,
      },
    },
  },
});
