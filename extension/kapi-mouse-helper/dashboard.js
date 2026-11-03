
const logEntries = [];
let lastState = null;
let lastCanvasData = null;
let lastResourceCount = null;
let lastDrawRate = null;

function addLog(message) {
  const time = new Date().toLocaleTimeString();

  logEntries.unshift(`[${time}] ${message}`);

  if (logEntries.length > 500) {
    logEntries.pop();
  }

  renderLog();
}

function renderLog() {
  document.getElementById("log").textContent =
    logEntries.join("\n");
}

async function update() {
  const stateEl = document.getElementById("state");

  try {
    const tabs = await chrome.tabs.query({
      url: ["http://*/*", "https://*/*"]
    });

    // Busca una pestaña que realmente tenga el monitor instalado.
    let response = null;
    let selectedTab = null;

    for (const tab of tabs) {
      try {
        const result = await chrome.tabs.sendMessage(tab.id, {
          type: "MU_GET_STATE"
        });

        if (result) {
          response = result;
          selectedTab = tab;
          break;
        }
      } catch (_) {}
    }

    if (!response) {
      stateEl.textContent = "Esperando conexión con el juego";
      document.getElementById("details").textContent =
        "Recarga la pestaña del MU Online y vuelve a abrir este panel.";
      return;
    }

    const data = response.latest;

    stateEl.textContent = data.state;

    document.getElementById("canvas").textContent =
      data.canvases.length;

    document.getElementById("draws").textContent =
      data.drawsPerSecond;

    document.getElementById("resources").textContent =
      data.resourceCount;

    document.getElementById("quiet").textContent =
      `${data.resourcesQuietFor} s`;

    document.getElementById("details").textContent =
      `URL: ${selectedTab.url}\n` +
      `Monitor principal detectado: ${response.mainSeen}\n` +
      `Canvases:\n` +
      JSON.stringify(data.canvases, null, 2);

    if (lastState !== data.state) {
      addLog(`Estado: ${lastState ?? "INICIO"} → ${data.state}`);
      lastState = data.state;
    }

    const canvasData = JSON.stringify(data.canvases);

    if (lastCanvasData !== canvasData) {
      addLog(`Cambio de canvas: ${canvasData}`);
      lastCanvasData = canvasData;
    }

    if (lastResourceCount !== data.resourceCount) {
      addLog(`Recursos observados: ${data.resourceCount}`);
      lastResourceCount = data.resourceCount;
    }

    if (lastDrawRate !== data.drawsPerSecond) {
      addLog(`Dibujos/segundo: ${data.drawsPerSecond}`);
      lastDrawRate = data.drawsPerSecond;
    }
  } catch (error) {
    stateEl.textContent = "Error de conexión";
    addLog(error.message);
  }
}

document.getElementById("refresh").addEventListener("click", update);

document.getElementById("clear").addEventListener("click", () => {
  logEntries.length = 0;
  renderLog();
});

update();
setInterval(update, 1000);
