const RENDERER_ID = "blazor-wasm";

let dotNetBridge;
let lifecycleBuffer = [];

function getHook() {
  return window.__BLAZOR_WASM_DEVTOOLS_GLOBAL_HOOK__;
}

function appendLifecycle(event) {
  lifecycleBuffer.push(event);
  if (lifecycleBuffer.length > 500) {
    lifecycleBuffer = lifecycleBuffer.slice(-500);
  }
  const hook = getHook();
  hook?.emit("lifecycle", event);
}

export function initialize(bridge) {
  dotNetBridge = bridge;
  const hook = getHook();
  if (!hook) {
    console.warn(
      "[Blazor WASM DevTools] Global hook not found. Install the browser extension and reload the page.",
    );
    return;
  }

  hook.on("activate", () => {
    bridge.invokeMethodAsync("OnDevToolsActivated");
  });

  if (hook.isActive) {
    bridge.invokeMethodAsync("OnDevToolsActivated");
  }
}

export function notify(lifecycleEvent, component) {
  const hook = getHook();
  hook?.emit("lifecycle", { lifecycleEvent, component });

  console.log("[bwdt] Notified lifecycle event:", {
    lifecycleEvent,
    component,
  });
}

export function registerRenderer(bridge) {
  const hook = getHook();
  if (!hook) {
    return;
  }

  hook.registerRenderer(RENDERER_ID, {
    id: RENDERER_ID,
    getSnapshot() {
      return bridge.invokeMethodAsync("GetSnapshotJson");
    },
    getLifecycleBuffer() {
      return lifecycleBuffer;
    },
  });
}

export function publishLifecycle(serializedEvent) {
  try {
    const event = JSON.parse(serializedEvent);
    appendLifecycle(event);
  } catch (err) {
    console.error(
      "[Blazor WASM DevTools] Failed to publish lifecycle event",
      err,
    );
  }
}
