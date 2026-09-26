import type {
  ComponentLifecycleEvent,
  PageState,
} from "../types/lifecycle";

const BLAZOR_WASM_DEVTOOLS_PANEL_EVENT = "__blazor_wasm_devtools_panel__";

function evalInPage<T>(
  expression: string,
): Promise<{ result: T | null; error: unknown }> {
  return new Promise((resolve) => {
    if (typeof chrome === "undefined" || !chrome.devtools?.inspectedWindow) {
      resolve({ result: null, error: new Error("chrome.devtools unavailable") });
      return;
    }

    chrome.devtools.inspectedWindow.eval(expression, (result, exceptionInfo) => {
      if (exceptionInfo) {
        resolve({ result: null, error: exceptionInfo });
        return;
      }
      resolve({ result: result as T, error: null });
    });
  });
}

export function activateDevToolsInPage(): Promise<void> {
  const detail = JSON.stringify({ type: "activate", payload: null });
  return evalInPage<void>(
    `window.dispatchEvent(new CustomEvent("${BLAZOR_WASM_DEVTOOLS_PANEL_EVENT}", { detail: ${detail} }));`,
  ).then(() => undefined);
}

export async function queryPageState(): Promise<PageState> {
  const { result, error } = await evalInPage<string>(
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
  );

  if (error || result == null) {
    throw error ?? new Error("Could not read inspected page state");
  }

  return JSON.parse(result) as PageState;
}

export async function fetchLifecycleEventsAfter(
  afterSequence: number,
): Promise<ComponentLifecycleEvent[]> {
  const { result, error } = await evalInPage<string>(
    `(function () {
      var hook = window.__BLAZOR_WASM_DEVTOOLS_GLOBAL_HOOK__;
      if (!hook || !hook.lifecycleBuffer) {
        return "[]";
      }
      var after = ${afterSequence};
      return JSON.stringify(
        hook.lifecycleBuffer.filter(function (event) {
          return event && typeof event.sequence === "number" && event.sequence > after;
        }),
      );
    })();`,
  );

  if (error || result == null) {
    throw error ?? new Error("Could not read lifecycle events");
  }

  return JSON.parse(result) as ComponentLifecycleEvent[];
}

export function subscribeLifecycleEvents(
  onEvents: (events: ComponentLifecycleEvent[]) => void,
  intervalMs = 400,
): () => void {
  let afterSequence = 0;
  let stopped = false;

  const tick = async () => {
    if (stopped) {
      return;
    }
    try {
      const events = await fetchLifecycleEventsAfter(afterSequence);
      if (events.length > 0) {
        afterSequence = events[events.length - 1]!.sequence;
        onEvents(events);
      }
    } catch {
      // Inspected page may be unavailable while navigating.
    }
  };

  const id = window.setInterval(() => {
    void tick();
  }, intervalMs);
  void tick();

  return () => {
    stopped = true;
    window.clearInterval(id);
  };
}

if (typeof window !== "undefined") {
  window.activateDevToolsInPage = activateDevToolsInPage;
  window.queryPageState = queryPageState;
}

declare global {
  interface Window {
    activateDevToolsInPage: typeof activateDevToolsInPage;
    queryPageState: typeof queryPageState;
  }
}
