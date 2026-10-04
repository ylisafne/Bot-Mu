import requests
import websocket
import json


# ============================================================
# BUSCAR MU ONLINE
# ============================================================

pestanas = requests.get(
    "http://127.0.0.1:9222/json"
).json()

juego = next(
    (
        p for p in pestanas
        if p.get("type") == "page"
        and "mumagdalenas.com" in p.get("url", "")
    ),
    None
)

if juego is None:
    print("No se encontró MU Online.")
    raise SystemExit


# ============================================================
# CONECTAR A CHROME
# ============================================================

ws = websocket.create_connection(
    juego["webSocketDebuggerUrl"],
    origin="http://127.0.0.1:9222"
)

contador = 0


def ejecutar(codigo):

    global contador

    contador += 1

    ws.send(json.dumps({
        "id": contador,
        "method": "Runtime.evaluate",
        "params": {
            "expression": codigo,
            "returnByValue": True
        }
    }))

    while True:

        respuesta = json.loads(ws.recv())

        if respuesta.get("id") == contador:
            return respuesta


# ============================================================
# ENVIAR ENTER AL CANVAS
# ============================================================

javascript = """
(() => {

    const canvas = document.getElementById("canvas");

    if (!canvas) {
        return "NO SE ENCONTRO CANVAS";
    }

    canvas.focus();

    const down = new KeyboardEvent("keydown", {
        key: "Enter",
        code: "Enter",
        keyCode: 13,
        which: 13,
        bubbles: true,
        cancelable: true
    });

    const up = new KeyboardEvent("keyup", {
        key: "Enter",
        code: "Enter",
        keyCode: 13,
        which: 13,
        bubbles: true,
        cancelable: true
    });

    canvas.dispatchEvent(down);

    setTimeout(() => {
        canvas.dispatchEvent(up);
    }, 300);

    return {
        resultado: "ENTER ENVIADO",
        activo: document.activeElement === canvas
    };

})()
"""


resultado = ejecutar(javascript)

print(
    json.dumps(
        resultado,
        indent=2,
        ensure_ascii=False
    )
)


ws.close()
