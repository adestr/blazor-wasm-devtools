(function () {
  const statusEl = document.getElementById("status");

  function setStatus(text) {
    if (statusEl) {
      statusEl.textContent = text;
    }
  }

  function refreshState() {
    queryPageState((state, err) => {
      if (err) {
        setStatus("Could not read the inspected page (see DevTools console).");
        return;
      }
      if (!state.hasHook) {
        setStatus(
          "Global hook not found on window. Reload the page after installing the extension.",
        );
        return;
      }
      const parts = [
        state.hasBlazor ? "Blazor detected" : "Blazor not detected",
        state.isActive ? "DevTools active" : "DevTools dormant",
      ];
      setStatus(parts.join(" · "));
    });
  }

  function initialize() {
    activateDevToolsInPage(() => {
      refreshState();
    });
  }

  window.blazorWasmDevToolsInitialize = initialize;

  initialize();
})();
