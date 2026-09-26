import type { ComponentLifecycleEvent, PageState } from "./types/lifecycle";

function formatStatus(pageState: PageState | null, pageError: string | null) {
  if (pageError) {
    return pageError;
  }
  if (!pageState) {
    return "Checking inspected page…";
  }
  if (!pageState.hasHook) {
    return "Global hook not found on window. Reload the page after installing the extension.";
  }

  return [
    pageState.hasBlazor ? "Blazor detected" : "Blazor not detected",
    pageState.isActive ? "DevTools active" : "DevTools dormant",
    `${pageState.rendererCount} renderer(s)`,
    `${pageState.lifecycleEventCount} lifecycle event(s)`,
  ].join(" · ");
}

function formatTime(timestampUnixMilliseconds: number) {
  return new Date(timestampUnixMilliseconds).toLocaleTimeString(undefined, {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    fractionalSecondDigits: 3,
  });
}

type Props = {
  pageState: PageState | null;
  pageError: string | null;
  events: ComponentLifecycleEvent[];
  initializing: boolean;
  onInitialize: () => void;
};

export function App({
  pageState,
  pageError,
  events,
  initializing,
  onInitialize,
}: Props) {
  const status = formatStatus(pageState, pageError);

  return (
    <div className="flex h-full min-h-screen flex-col gap-3 p-3 text-[12px] text-[#e8e8e8]">
      <header className="space-y-2">
        <h1 className="text-sm font-semibold tracking-tight">
          Blazor WASM DevTools
        </h1>
        <p className="text-[#b0b0b0]">
          Add the NuGet instrumentation package to your Blazor WASM app so
          lifecycle events stream into this panel when DevTools is active.
        </p>
        <div className="flex flex-wrap items-center gap-2">
          <button
            type="button"
            className="rounded border border-[#555] bg-[#2d2d2d] px-2 py-1 hover:bg-[#3a3a3a] disabled:opacity-60"
            disabled={initializing}
            onClick={onInitialize}
          >
            {initializing ? "Initializing…" : "Initialize DevTools"}
          </button>
        </div>
        <p
          className="rounded bg-[#2d2d2d] px-2 py-1.5 text-[#d4d4d4]"
          aria-live="polite"
        >
          {status}
        </p>
      </header>

      <section className="flex min-h-0 flex-1 flex-col gap-2">
        <h2 className="text-[11px] font-medium uppercase tracking-wide text-[#9a9a9a]">
          Lifecycle stream
        </h2>
        <ol className="min-h-0 flex-1 space-y-1 overflow-y-auto rounded border border-[#333] bg-[#252526] p-2 font-mono text-[11px]">
          {events.length === 0 ? (
            <li className="text-[#888]">
              No lifecycle events yet. Open a Blazor WASM app with
              instrumentation and interact with the UI. To set up the
              instrumentation, follow the instructions in the documentation at{" "}
              <a
                href="https://github.com/adestr/blazor-wasm-devtools"
                className="text-[#9cdcfe] underline"
              >
                https://github.com/adestr/blazor-wasm-devtools
              </a>
              .
            </li>
          ) : (
            events
              .slice()
              .reverse()
              .map((event) => (
                <li
                  key={event.sequence}
                  className="border-b border-[#333] pb-1 last:border-0"
                >
                  <span className="text-[#888]">
                    {formatTime(event.timestampUnixMilliseconds)}
                  </span>{" "}
                  <span className="text-[#9cdcfe]">#{event.componentId}</span>{" "}
                  <span className="text-[#ce9178]">{event.componentType}</span>{" "}
                  <span className="text-[#dcdcaa]">{event.phase}</span>
                  {event.firstRender ? (
                    <span className="text-[#4ec9b0]"> first</span>
                  ) : null}
                  <span className="text-[#808080]"> · {event.source}</span>
                </li>
              ))
          )}
        </ol>
      </section>
    </div>
  );
}
