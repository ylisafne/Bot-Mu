
(() => {
  if (window.__muConsoleMonitorInstalled) return;
  window.__muConsoleMonitorInstalled = true;

  const methods = ["log", "warn", "error", "info", "debug"];
  const originals = {};

  function emit(level, args) {
    const message = args.map(arg => {
      try {
        return typeof arg === "string"
          ? arg
          : JSON.stringify(arg);
      } catch (_) {
        return String(arg);
      }
    }).join(" ");

    if (
      /RenderMainScene|world render|WEB NET|scenes/i.test(message)
    ) {
      window.postMessage({
        source: "MU_ENGINE_LOG",
        data: {
          level,
          message,
          timestamp: Date.now()
        }
      }, "*");
    }
  }

  for (const level of methods) {
    originals[level] = console[level];

    console[level] = function (...args) {
      emit(level, args);
      return originals[level].apply(this, args);
    };
  }

  window.postMessage({
    source: "MU_ENGINE_LOG",
    data: {
      level: "monitor",
      message: "Monitor de consola instalado",
      timestamp: Date.now()
    }
  }, "*");
})();
