import { describe, expect, it } from "vitest";
import { applyLifecycleEvents, buildComponentTree } from "./hierarchy";
import type { ComponentLifecycleEvent } from "./types/lifecycle";

const parent: ComponentLifecycleEvent = {
  sequence: 1,
  componentId: 1,
  componentType: "App",
  parentComponentId: null,
  phase: "afterRender",
  firstRender: true,
  source: "instrumentation",
  timestampUnixMilliseconds: Date.parse("2026-10-16T09:00:00Z"),
};

const child: ComponentLifecycleEvent = {
  sequence: 2,
  componentId: 2,
  componentType: "Counter",
  parentComponentId: 1,
  phase: "parametersSet",
  firstRender: false,
  source: "instrumentation",
  timestampUnixMilliseconds: Date.parse("2026-10-16T09:00:01Z"),
};

describe("component hierarchy", () => {
  it("nests a child under its parent from the component map alone", () => {
    const components = applyLifecycleEvents(new Map(), [parent, child]);
    const tree = buildComponentTree(components);

    expect(tree).toMatchObject([
      {
        componentId: 1,
        componentType: "App",
        parentComponentId: null,
        children: [
          {
            componentId: 2,
            componentType: "Counter",
            parentComponentId: 1,
            children: [],
          },
        ],
      },
    ]);
  });

  it("replaces a component when a later event has the same id", () => {
    const components = applyLifecycleEvents(new Map(), [
      parent,
      { ...parent, sequence: 3, componentType: "Routes" },
    ]);
    const tree = buildComponentTree(components);

    expect(tree).toMatchObject([
      {
        componentId: 1,
        componentType: "Routes",
        children: [],
      },
    ]);
  });

  it("shows a component as a root when its parent is missing from the map", () => {
    const components = applyLifecycleEvents(new Map(), [
      { ...child, parentComponentId: 99 },
    ]);
    const tree = buildComponentTree(components);

    expect(tree).toMatchObject([
      {
        componentId: 2,
        componentType: "Counter",
        parentComponentId: 99,
        children: [],
      },
    ]);
  });
});
