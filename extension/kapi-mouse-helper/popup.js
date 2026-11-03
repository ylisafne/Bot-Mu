async function refresh() {
  const stateEl = document.getElementById("state");

  try {
    const [tab] = await chrome.tabs.query({
      active: true,
      currentWindow: true
    });

    if (!tab?.id) {
      stateEl.textContent = "No hay pestaña activa";
      return;
    }

    const response = await chrome.tabs.sendMessage(tab.id, {
      type: "MU_GET_STATE"
    });

    if (!response) {
      stateEl.textContent = "Monitor no disponible";
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
      data.canvases.map(c =>
        `Canvas ${c.index}: ${c.width}×${c.height}, ` +
        `visible=${c.visible}, CSS=${c.cssWidth}×${c.cssHeight}`
      ).join("\n") || "No se encontraron canvas";

    const list = document.getElementById("events");
    list.replaceChildren();

    for (const item of response.events) {
      const li = document.createElement("li");
      li.textContent = `[${item.time}] ${item.message}`;
      list.appendChild(li);
    }
  } catch (error) {
    stateEl.textContent = "No se puede leer esta pestaña";
    document.getElementById("details").textContent =
      "Abre o recarga la página del juego y vuelve a intentarlo. " +
      "Las páginas internas del navegador no permiten esta inyección.";
  }
}

document.getElementById("refresh").addEventListener("click", refresh);

refresh();
setInterval(refresh, 1000);