export type ComponentLifecyclePhase =
  | "initializing"
  | "initialized"
  | "parametersSet"
  | "rendering"
  | "afterRender"
  | "disposed";

export type ComponentLifecycleEvent = {
  sequence: number;
  componentId: number;
  componentType: string;
  parentComponentId: number | null;
  phase: ComponentLifecyclePhase;
  firstRender: boolean;
  source: string;
  timestampUnixMilliseconds: number;
};

export type PageState = {
  hasHook: boolean;
  isActive: boolean;
  isDormant: boolean | null;
  hasBlazor: boolean;
  lifecycleEventCount: number;
  rendererCount: number;
};
