import { act, fireEvent, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

const { components } = vi.hoisted(() => ({
  components: new Map([
    [
      1,
      {
        componentId: 1,
        componentType: "Widget",
        parentComponentId: null,
        phase: "afterRender" as const,
        timestampUnixMilliseconds: 0,
      },
    ],
  ]),
}));

vi.mock("./hooks/useDevToolsPanel", () => ({
  useDevToolsPanel: () => ({
    pageState: {
      hasHook: true,
      isActive: true,
      hasBlazor: true,
      lifecycleEventCount: 0,
      rendererCount: 1,
    },
    pageError: null,
    events: [],
    components,
    initializing: false,
    initialize: () => undefined,
    refreshState: () => undefined,
  }),
}));

describe("PanelRoot", () => {
  it("passes the hook component map into App", async () => {
    document.body.innerHTML = '<div id="root"></div>';

    await act(async () => {
      await import("./main");
    });

    fireEvent.click(screen.getByRole("tab", { name: "Components" }));

    expect(screen.getByText("Widget", { exact: true })).toBeTruthy();
  });
});
