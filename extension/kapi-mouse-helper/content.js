(() => {
  let latest = {
    state: "WAITING",
    canvases: [],
    drawsPerSecond: 0,
    resourceCount: 0,
    resourcesQuietFor: 0,
    canvasStableFor: 0,
    timestamp: Date.now()
  };

  const events = [];
  let mainSeen = false;

  function log(message) {
    events.unshift({
      time: new Date().toLocaleTimeString(),
      message
    });

    if (events.length > 100) events.length = 100;
  }

  window.addEventListener("message", event => {
    if (event.source !== window) return;

    if (event.data?.source === "MU_CANVAS_MONITOR") {
      mainSeen = true;
      latest = event.data.data || latest;
    }
  });

  // Diagnóstico independiente: cuenta canvas desde el DOM.
  function inspectDOM() {
    const canvases = [...document.querySelectorAll("canvas")];

    return canvases.map((canvas, index) => {
      const rect = canvas.getBoundingClientRect();

      return {
        index,
        width: canvas.width,
        height: canvas.height,
        visible: rect.width > 0 && rect.height > 0
      };
    });
  }

  chrome.runtime.onMessage.addListener(
    (message, sender, sendResponse) => {
      if (message?.type === "MU_GET_STATE") {
        sendResponse({
          latest: {
            ...latest,
            canvases: inspectDOM().length
              ? inspectDOM()
              : latest.canvases
          },
          events,
          mainSeen,
          url: location.href
        });
      }

      return true;
    }
  );

  log("Content script instalado");
})();