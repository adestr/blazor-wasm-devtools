chrome.devtools.panels.create(
  "Blazor WASM",
  "",
  "panel/index.html",
  function (panel) {
    panel.onShown.addListener(function () {
      activateDevToolsInPage();
    });
  },
);
