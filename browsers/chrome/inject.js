/**
 * Runs in the inspected page (MAIN world) at document_start.
 * Installs the global hook before Blazor WASM bootstraps.
 */
(function () {
  const PANEL_EVENT = "__blazor_wasm_devtools_panel__";

  if (window.__BLAZOR_WASM_DEVTOOLS_GLOBAL_HOOK__) {
    return;
  }

  const listeners = new Map();

  const lifecycleBuffer = [];

  const hook = {
    isDormant: true,
    isActive: false,
    renderers: new Map(),
    lifecycleBuffer,

    activate() {
      this.isDormant = false;
      this.isActive = true;
      window.__BLAZOR_WASM_DEVTOOLS_DORMANT__ = false;
      window.__BLAZOR_WASM_DEVTOOLS_ACTIVE__ = true;
      this.emit("activate");
      console.info("[Blazor WASM DevTools] extension activated");
    },

    on(event, listener) {
      if (!listeners.has(event)) {
        listeners.set(event, new Set());
      }
      listeners.get(event).add(listener);
      return () => listeners.get(event).delete(listener);
    },

    emit(event, payload) {
      if (event === "lifecycle" && payload) {
        lifecycleBuffer.push(payload);
        if (lifecycleBuffer.length > 500) {
          lifecycleBuffer.splice(0, lifecycleBuffer.length - 500);
        }
      }

      const set = listeners.get(event);
      if (!set) {
        return;
      }
      for (const listener of set) {
        try {
          listener(payload);
        } catch (err) {
          console.error("[Blazor WASM DevTools] hook listener error", err);
        }
      }
    },

    registerRenderer(id, renderer) {
      this.renderers.set(id, renderer);
      this.emit("renderer", { id, renderer });
    },
  };

  window.__BLAZOR_WASM_DEVTOOLS_GLOBAL_HOOK__ = hook;
  window.__BLAZOR_WASM_DEVTOOLS_DORMANT__ = true;
  window.__BLAZOR_WASM_DEVTOOLS_ACTIVE__ = false;

  window.addEventListener(PANEL_EVENT, (event) => {
    const { type, payload } = event.detail || {};
    if (type === "activate") {
      hook.activate();
      return;
    }
    if (type === "ping") {
      hook.emit("ping", payload);
    }
  });
})();
