import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { App } from "./App";
import type { ComponentLifecycleEvent, PageState } from "./types/lifecycle";

const pageState: PageState = {
  hasHook: true,
  isActive: true,
  hasBlazor: true,
  lifecycleEventCount: 2,
  rendererCount: 1,
};

const events: ComponentLifecycleEvent[] = [
  {
    sequence: 1,
    componentId: 1,
    componentType: "App",
    parentComponentId: null,
    phase: "afterRender",
    firstRender: true,
    source: "instrumentation",
    timestampUnixMilliseconds: Date.parse("2026-10-16T09:00:00Z"),
  },
  {
    sequence: 2,
    componentId: 2,
    componentType: "Counter",
    parentComponentId: 1,
    phase: "parametersSet",
    firstRender: false,
    source: "instrumentation",
    timestampUnixMilliseconds: Date.parse("2026-10-16T09:00:01Z"),
  },
];

function parentAndChild() {
  return new Map([
    [
      1,
      {
        componentId: 1,
        componentType: "App",
        parentComponentId: null,
        phase: "afterRender" as const,
        timestampUnixMilliseconds: 0,
      },
    ],
    [
      2,
      {
        componentId: 2,
        componentType: "Counter",
        parentComponentId: 1,
        phase: "parametersSet" as const,
        timestampUnixMilliseconds: 1,
      },
    ],
  ]);
}

function renderApp(overrides: Partial<Parameters<typeof App>[0]> = {}) {
  const onInitialize = vi.fn();
  render(
    <App
      pageState={pageState}
      pageError={null}
      events={[]}
      initializing={false}
      onInitialize={onInitialize}
      {...overrides}
    />,
  );
  return { onInitialize };
}

describe("App", () => {
  it("shows the empty lifecycle stream", () => {
    renderApp();

    expect(screen.getByText(/No lifecycle events yet/)).toBeTruthy();
  });

  it("lists lifecycle events newest first with parent ids", () => {
    renderApp({ events });

    const items = screen.getAllByRole("listitem");
    expect(items).toHaveLength(2);
    expect(items[0]?.textContent).toContain("#2");
    expect(items[0]?.textContent).toContain("p1");
    expect(items[0]?.textContent).toContain("Counter");
    expect(items[0]?.textContent).toContain("parametersSet");
    expect(items[1]?.textContent).toContain("#1");
    expect(items[1]?.textContent).toContain("p-");
    expect(items[1]?.textContent).toContain("App");
    expect(items[1]?.textContent).toContain("first");
  });

  it("shows a page error in place of the status summary", () => {
    renderApp({ pageError: "Could not read the inspected page" });

    expect(screen.getByText("Could not read the inspected page")).toBeTruthy();
    expect(screen.queryByText(/Blazor detected/)).toBeNull();
  });

  it("calls onInitialize from the initialize button", () => {
    const { onInitialize } = renderApp();

    fireEvent.click(
      screen.getByRole("button", { name: "Initialize DevTools" }),
    );

    expect(onInitialize).toHaveBeenCalledOnce();
  });

  it("marks the lifecycle stream as the selected tab panel", () => {
    renderApp();

    expect(
      screen.getByRole("tabpanel", { name: "Lifecycle stream" }),
    ).toBeTruthy();

    const lifecycleTab = screen.getByRole("tab", { name: "Lifecycle stream" });
    const componentsTab = screen.getByRole("tab", { name: "Components" });
    expect(lifecycleTab.className).toContain("border-[#9cdcfe]");
    expect(lifecycleTab.className).toContain("text-[#9cdcfe]");
    expect(componentsTab.className).not.toContain("border-[#9cdcfe]");
    expect(componentsTab.className).not.toContain("text-[#9cdcfe]");

    fireEvent.click(componentsTab);

    expect(componentsTab.className).toContain("border-[#9cdcfe]");
    expect(componentsTab.className).toContain("text-[#9cdcfe]");
    expect(lifecycleTab.className).not.toContain("border-[#9cdcfe]");
    expect(lifecycleTab.className).not.toContain("text-[#9cdcfe]");
  });

  it("shows a child nested under its parent in the Components tab without events", () => {
    renderApp({
      events: [],
      components: parentAndChild(),
    });

    fireEvent.click(screen.getByRole("tab", { name: "Components" }));

    const panel = screen.getByRole("tabpanel", { name: "Components" });
    const appItem = within(panel).getByText("App", { exact: true }).closest("li");
    expect(appItem).not.toBeNull();
    expect(
      within(appItem as HTMLElement).getByText("Counter", { exact: true }),
    ).toBeTruthy();
  });

  it("indents a nested list and toggles that row from the indicator", () => {
    renderApp({
      events: [],
      components: parentAndChild(),
    });

    fireEvent.click(screen.getByRole("tab", { name: "Components" }));

    const panel = screen.getByRole("tabpanel", { name: "Components" });
    const appItem = within(panel)
      .getByText("App", { exact: true })
      .closest("li") as HTMLElement;
    const rootList = appItem.parentElement as HTMLElement;
    const nestedList = within(appItem).getByRole("list");
    expect(rootList.className).not.toContain("pl-4");
    expect(nestedList.className).toContain("pl-4");
    const counterItem = within(panel)
      .getByText("Counter", { exact: true })
      .closest("li") as HTMLElement;
    expect(within(counterItem).queryByRole("button")).toBeNull();

    fireEvent.click(within(appItem).getByRole("button", { name: "-" }));
    expect(within(panel).queryByText("Counter", { exact: true })).toBeNull();

    fireEvent.click(within(appItem).getByRole("button", { name: "+" }));
    expect(within(panel).getByText("Counter", { exact: true })).toBeTruthy();
  });
});
