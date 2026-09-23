chrome.devtools.panels.create(
  "Blazor WASM",
  "",
  "panel.html",
  function (panel) {
    panel.onShown.addListener(function () {
      activateDevToolsInPage();
    });
  },
);
