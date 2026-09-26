import { useCallback, useEffect, useState } from "react";
import {
  activateDevToolsInPage,
  queryPageState,
  subscribeLifecycleEvents,
} from "../bridge/page-bridge";
import type { ComponentLifecycleEvent, PageState } from "../types/lifecycle";

const MAX_EVENTS = 200;

export function useDevToolsPanel() {
  const [pageState, setPageState] = useState<PageState | null>(null);
  const [pageError, setPageError] = useState<string | null>(null);
  const [events, setEvents] = useState<ComponentLifecycleEvent[]>([]);
  const [initializing, setInitializing] = useState(false);

  const refreshState = useCallback(async () => {
    try {
      setPageState(await queryPageState());
      setPageError(null);
    } catch {
      setPageError("Could not read the inspected page (see DevTools console).");
    }
  }, []);

  const initialize = useCallback(async () => {
    setInitializing(true);
    try {
      await activateDevToolsInPage();
      await refreshState();
    } finally {
      setInitializing(false);
    }
  }, [refreshState]);

  useEffect(() => {
    void initialize();
  }, [initialize]);

  useEffect(() => {
    const unsubscribe = subscribeLifecycleEvents((incoming) => {
      setEvents((current) => {
        const merged = [...current, ...incoming];
        return merged.length > MAX_EVENTS
          ? merged.slice(merged.length - MAX_EVENTS)
          : merged;
      });
      void refreshState();
    });

    return unsubscribe;
  }, [refreshState]);

  return {
    pageState,
    pageError,
    events,
    initializing,
    initialize,
    refreshState,
  };
}
