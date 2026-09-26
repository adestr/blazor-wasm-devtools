import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./App";
import { useDevToolsPanel } from "./hooks/useDevToolsPanel";
import "./index.css";

function PanelRoot() {
  const panel = useDevToolsPanel();
  return (
    <App
      pageState={panel.pageState}
      pageError={panel.pageError}
      events={panel.events}
      initializing={panel.initializing}
      onInitialize={() => {
        void panel.initialize();
      }}
    />
  );
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <PanelRoot />
  </StrictMode>,
);
