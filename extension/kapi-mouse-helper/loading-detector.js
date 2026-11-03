
(() => {
  if (window.__muLoadingDetectorInstalled) return;
  window.__muLoadingDetectorInstalled = true;

  const SELECTORS = [
    '[class*="load" i]',
    '[id*="load" i]',
    '[class*="progress" i]',
    '[id*="progress" i]',
    '[class*="spinner" i]',
    '[id*="spinner" i]',
    '[class*="loading" i]',
    '[id*="loading" i]'
  ];

  const history = [];
  let previousSignature = "";
  let lastState = "UNKNOWN";
  let candidateSince = 0;
  let currentState = "SCANNING";

  function visible(el) {
    if (!(el instanceof Element)) return false;

    const style = getComputedStyle(el);
    const rect = el.getBoundingClientRect();

    return (
      style.display !== "none" &&
      style.visibility !== "hidden" &&
      Number(style.opacity) > 0 &&
      rect.width > 0 &&
      rect.height > 0
    );
  }

  function describe(el) {
    return {
      tag: el.tagName.toLowerCase(),
      id: el.id || "",
      className: typeof el.className === "string"
        ? el.className.slice(0, 200)
        : "",
      text: (el.innerText || el.textContent || "")
        .trim()
        .slice(0, 100),
      visible: visible(el),
      width: Math.round(el.getBoundingClientRect().width),
      height: Math.round(el.getBoundingClientRect().height)
    };
  }

  function scan() {
    const found = new Set();

    for (const selector of SELECTORS) {
      try {
        document.querySelectorAll(selector).forEach(el => {
          if (visible(el)) found.add(el);
        });
      } catch (_) {}
    }

    const elements = [...found].map(describe);

    const signature = JSON.stringify(elements);

    if (signature !== previousSignature) {
      previousSignature = signature;

      history.unshift({
        time: new Date().toLocaleTimeString(),
        event: "DOM_CANDIDATES_CHANGED",
        count: elements.length,
        elements
      });

      if (history.length > 100) history.pop();
    }

    return elements;
  }

  function tick() {
    const elements = scan();

    // Es una heurística: no todo elemento cuyo nombre contiene
    // "load" representa la pantalla de carga del juego.
    const loadingVisible = elements.length > 0;

    const next = loadingVisible
      ? "LOADING_CANDIDATE_VISIBLE"
      : "NO_LOADING_CANDIDATE";

    if (next !== lastState) {
      lastState = next;
      candidateSince = Date.now();

      history.unshift({
        time: new Date().toLocaleTimeString(),
        event: "STATE_CHANGED",
        state: next
      });

      if (history.length > 100) history.pop();
    }

    currentState = next;

    window.postMessage({
      source: "MU_HTML_LOADING_DETECTOR",
      data: {
        state: currentState,
        candidates: elements,
        history: history.slice(0, 50),
        timestamp: Date.now()
      }
    }, "*");

    setTimeout(tick, 500);
  }

  const observer = new MutationObserver(() => scan());

  observer.observe(document, {
    subtree: true,
    childList: true,
    attributes: true,
    attributeFilter: ["class", "style", "hidden", "aria-hidden"]
  });

  tick();
  document.addEventListener("DOMContentLoaded", () => {
    console.log("[MU Monitor] DOM cargado");
    });
})();
