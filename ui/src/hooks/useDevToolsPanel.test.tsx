import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ComponentLifecycleEvent } from "../types/lifecycle";
import { useDevToolsPanel } from "./useDevToolsPanel";

const bridge = vi.hoisted(() => ({
  onEvents: null as ((events: ComponentLifecycleEvent[]) => void) | null,
}));

vi.mock("../bridge/page-bridge", () => ({
  activateDevToolsInPage: vi.fn(async () => undefined),
  queryPageState: vi.fn(async () => ({
    hasHook: true,
    isActive: true,
    hasBlazor: true,
    lifecycleEventCount: 0,
    rendererCount: 1,
  })),
  subscribeLifecycleEvents: (
    onEvents: (events: ComponentLifecycleEvent[]) => void,
  ) => {
    bridge.onEvents = onEvents;
    return () => {
      bridge.onEvents = null;
    };
  },
}));

function event(sequence: number): ComponentLifecycleEvent {
  return {
    sequence,
    componentId: sequence,
    componentType: `C${sequence}`,
    parentComponentId: null,
    phase: "afterRender",
    firstRender: true,
    source: "instrumentation",
    timestampUnixMilliseconds: sequence,
  };
}

describe("useDevToolsPanel", () => {
  beforeEach(() => {
    bridge.onEvents = null;
  });

  it("keeps a component after its event falls out of the event list", () => {
    const { result } = renderHook(() => useDevToolsPanel());
    const incoming = Array.from({ length: 201 }, (_, index) => event(index + 1));

    act(() => {
      bridge.onEvents?.(incoming);
    });

    expect(result.current.events).toHaveLength(200);
    expect(result.current.events.some((item) => item.componentId === 1)).toBe(
      false,
    );
    expect(result.current.components.get(1)?.componentType).toBe("C1");
  });
});
