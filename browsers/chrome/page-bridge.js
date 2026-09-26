(function(o){"use strict";const f="__blazor_wasm_devtools_panel__";function a(t){return new Promise(e=>{if(typeof chrome>"u"||!chrome.devtools?.inspectedWindow){e({result:null,error:new Error("chrome.devtools unavailable")});return}chrome.devtools.inspectedWindow.eval(t,(n,r)=>{if(r){e({result:null,error:r});return}e({result:n,error:null})})})}function l(){const t=JSON.stringify({type:"activate",payload:null});return a(`window.dispatchEvent(new CustomEvent("${f}", { detail: ${t} }));`).then(()=>{})}async function c(){const{result:t,error:e}=await a(`(function () {
      var hook = window.__BLAZOR_WASM_DEVTOOLS_GLOBAL_HOOK__;
      return JSON.stringify({
        hasHook: !!hook,
        isActive: !!(hook && hook.isActive),
        isDormant: hook ? hook.isDormant : null,
        hasBlazor: typeof Blazor !== "undefined",
        lifecycleEventCount: (function () {
          var hook = window.__BLAZOR_WASM_DEVTOOLS_GLOBAL_HOOK__;
          return hook && hook.lifecycleBuffer ? hook.lifecycleBuffer.length : 0;
        })(),
        rendererCount: (function () {
          var hook = window.__BLAZOR_WASM_DEVTOOLS_GLOBAL_HOOK__;
          return hook && hook.renderers ? hook.renderers.size : 0;
        })()
      });
    })();`);if(e||t==null)throw e??new Error("Could not read inspected page state");return JSON.parse(t)}async function u(t){const{result:e,error:n}=await a(`(function () {
      var hook = window.__BLAZOR_WASM_DEVTOOLS_GLOBAL_HOOK__;
      if (!hook || !hook.lifecycleBuffer) {
        return "[]";
      }
      var after = ${t};
      return JSON.stringify(
        hook.lifecycleBuffer.filter(function (event) {
          return event && typeof event.sequence === "number" && event.sequence > after;
        }),
      );
    })();`);if(n||e==null)throw n??new Error("Could not read lifecycle events");return JSON.parse(e)}function d(t,e=400){let n=0,r=!1;const s=async()=>{if(!r)try{const i=await u(n);i.length>0&&(n=i[i.length-1].sequence,t(i))}catch{}},_=window.setInterval(()=>{s()},e);return s(),()=>{r=!0,window.clearInterval(_)}}typeof window<"u"&&(window.activateDevToolsInPage=l,window.queryPageState=c),o.activateDevToolsInPage=l,o.fetchLifecycleEventsAfter=u,o.queryPageState=c,o.subscribeLifecycleEvents=d,Object.defineProperty(o,Symbol.toStringTag,{value:"Module"})})(this.BlazorWasmDevToolsPageBridge=this.BlazorWasmDevToolsPageBridge||{});
