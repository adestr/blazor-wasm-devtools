/**
 * Shared helpers for the DevTools panel (extension context).
 * Talks to inject.js in the inspected page via eval + CustomEvent.
 */
const BLAZOR_WASM_DEVTOOLS_PANEL_EVENT = "__blazor_wasm_devtools_panel__";

function evalInPage(expression, callback) {
  chrome.devtools.inspectedWindow.eval(expression, callback);
}

function sendPanelMessage(type, payload, callback) {
  const detail = JSON.stringify({ type, payload });
  evalInPage(
    `window.dispatchEvent(new CustomEvent("${BLAZOR_WASM_DEVTOOLS_PANEL_EVENT}", { detail: ${detail} }));`,
    callback,
  );
}

function activateDevToolsInPage(callback) {
  sendPanelMessage("activate", null, callback);
}

function queryPageState(callback) {
  evalInPage(
    `(function () {
      var hook = window.__BLAZOR_WASM_DEVTOOLS_GLOBAL_HOOK__;
      return JSON.stringify({
        hasHook: !!hook,
        isActive: !!(hook && hook.isActive),
        isDormant: hook ? hook.isDormant : null,
        hasBlazor: typeof Blazor !== "undefined",
        lifecycleEventCount: (function () {
          var hook = window.__BLAZOR_WASM_DEVTOOLS_GLOBAL_HOOK__;
          return hook && hook.lifecycleBuffer ? hook.lifecycleBuffer.length : 0;
        })(),
        rendererCount: (function () {
          var hook = window.__BLAZOR_WASM_DEVTOOLS_GLOBAL_HOOK__;
          return hook && hook.renderers ? hook.renderers.size : 0;
        })()
      });
    })();`,
    (result, exceptionInfo) => {
      if (exceptionInfo) {
        callback(null, exceptionInfo);
        return;
      }
      try {
        callback(JSON.parse(result), null);
      } catch (err) {
        callback(null, err);
      }
    },
  );
}
