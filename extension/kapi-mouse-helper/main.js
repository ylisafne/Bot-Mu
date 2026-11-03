(() => {
  if (window.__muCanvasMonitorInstalled) return;
  window.__muCanvasMonitorInstalled = true;

  const PREFIX = "MU_CANVAS_MONITOR";
  const contexts = new WeakSet();

  let drawCalls = 0;
  let lastDrawCalls = 0;
  let lastResourceCount = 0;
  let lastResourceChange = performance.now();
  let lastCanvasSignature = "";
  let lastCanvasChange = performance.now();
  let lastReport = 0;

  function send(data) {
    window.postMessage({
      source: PREFIX,
      data
    }, "*");
  }

  function instrumentContext(gl) {
    if (!gl || contexts.has(gl)) return;
    contexts.add(gl);

    const methods = [
      "drawArrays",
      "drawElements",
      "drawArraysInstanced",
      "drawElementsInstanced"
    ];

    for (const name of methods) {
      const original = gl[name];

      if (typeof original !== "function") continue;

      try {
        gl[name] = function (...args) {
          drawCalls++;
          return original.apply(this, args);
        };
      } catch (_) {
        // Algunas implementaciones no permiten reemplazar métodos.
      }
    }
  }

  function instrumentCanvas(canvas) {
    if (canvas.__muObserved) return;
    canvas.__muObserved = true;

    const original = canvas.getContext;

    canvas.getContext = function (type, ...args) {
      const context = original.call(this, type, ...args);

      if (
        context &&
        /^(webgl|webgl2|experimental-webgl)$/i.test(type)
      ) {
        instrumentContext(context);
      }

      return context;
    };

    // Detecta también contextos creados antes de instalar el monitor.
    for (const type of ["webgl2", "webgl", "experimental-webgl"]) {
      try {
        const gl = canvas.getContext(type);
        if (gl) {
          instrumentContext(gl);
          break;
        }
      } catch (_) {}
    }
  }

  function scanCanvases() {
    const canvases = [...document.querySelectorAll("canvas")];

    for (const canvas of canvases) {
      instrumentCanvas(canvas);
    }

    const details = canvases.map((canvas, index) => {
      const rect = canvas.getBoundingClientRect();

      return {
        index,
        width: canvas.width,
        height: canvas.height,
        visible: rect.width > 0 && rect.height > 0,
        cssWidth: Math.round(rect.width),
        cssHeight: Math.round(rect.height)
      };
    });

    const signature = JSON.stringify(details);

    if (signature !== lastCanvasSignature) {
      lastCanvasSignature = signature;
      lastCanvasChange = performance.now();
    }

    return details;
  }

  function tick() {
    const now = performance.now();
    const canvases = scanCanvases();

    const resources = performance.getEntriesByType("resource");
    const resourceCount = resources.length;

    if (resourceCount !== lastResourceCount) {
      lastResourceCount = resourceCount;
      lastResourceChange = now;
    }

    const drawsPerSecond = Math.round(
      (drawCalls - lastDrawCalls) * 1000 /
      Math.max(1, now - lastReport || 1000)
    );

    const resourcesQuietFor = Math.round(
      (now - lastResourceChange) / 1000
    );

    const canvasStableFor = Math.round(
      (now - lastCanvasChange) / 1000
    );

    const hasCanvas = canvases.length > 0;
    const hasVisibleCanvas = canvases.some(c => c.visible);

    let state = "NO_CANVAS";

    if (hasCanvas) {
      state = "INITIALIZING";

      if (hasVisibleCanvas) {
        state = "CANVAS_VISIBLE";

        if (drawsPerSecond > 0) {
          state = "RENDERING";
        }

        if (
          drawsPerSecond > 0 &&
          resourcesQuietFor >= 5 &&
          canvasStableFor >= 5
        ) {
          state = "POSSIBLY_READY";
        }
      }
    }

    send({
      state,
      canvases,
      drawsPerSecond,
      resourceCount,
      resourcesQuietFor,
      canvasStableFor,
      timestamp: Date.now()
    });

    lastDrawCalls = drawCalls;
    lastReport = now;

    setTimeout(tick, 1000);
  }

  const observer = new MutationObserver(() => scanCanvases());

  function start() {
    if (document.documentElement) {
      observer.observe(document.documentElement, {
        childList: true,
        subtree: true
      });
    }

    tick();
  }

  start();
})();