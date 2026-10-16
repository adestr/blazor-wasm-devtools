import type {
  ComponentLifecycleEvent,
  ComponentLifecyclePhase,
} from "./types/lifecycle";

export type ComponentRecord = {
  componentId: number;
  componentType: string;
  parentComponentId: number | null;
  phase: ComponentLifecyclePhase;
  timestampUnixMilliseconds: number;
};

export type ComponentTreeNode = ComponentRecord & {
  children: ComponentTreeNode[];
};

export function applyLifecycleEvents(
  components: ReadonlyMap<number, ComponentRecord>,
  events: readonly ComponentLifecycleEvent[],
): Map<number, ComponentRecord> {
  const next = new Map(components);
  for (const event of events) {
    next.set(event.componentId, {
      componentId: event.componentId,
      componentType: event.componentType,
      parentComponentId: event.parentComponentId,
      phase: event.phase,
      timestampUnixMilliseconds: event.timestampUnixMilliseconds,
    });
  }
  return next;
}

export function buildComponentTree(
  components: ReadonlyMap<number, ComponentRecord>,
): ComponentTreeNode[] {
  const nodes = new Map<number, ComponentTreeNode>();
  for (const component of components.values()) {
    nodes.set(component.componentId, { ...component, children: [] });
  }

  const roots: ComponentTreeNode[] = [];
  for (const node of nodes.values()) {
    const parent =
      node.parentComponentId == null
        ? undefined
        : nodes.get(node.parentComponentId);
    if (parent) {
      parent.children.push(node);
    } else {
      roots.push(node);
    }
  }
  return roots;
}
