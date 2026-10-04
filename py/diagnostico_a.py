import json
import time
import requests
import websocket


CDP_URL = "http://127.0.0.1:9222/json"


# ============================================================
# BUSCAR JUEGO
# ============================================================

pestanas = requests.get(CDP_URL).json()

juego = next(
    (
        p for p in pestanas
        if p.get("type") == "page"
        and "mumagdalenas.com" in p.get("url", "")
    ),
    None
)

if juego is None:
    print("No se encontró MuMagdalenas.")
    raise SystemExit


print("Pestaña encontrada:")
print(juego["title"])
print(juego["url"])


# ============================================================
# CONEXIÓN CDP
# ============================================================

ws = websocket.create_connection(
    juego["webSocketDebuggerUrl"],
    origin="http://127.0.0.1:9222"
)

contador = 0


def cdp(method, params=None):

    global contador

    contador += 1

    mensaje = {
        "id": contador,
        "method": method
    }

    if params is not None:
        mensaje["params"] = params

    ws.send(json.dumps(mensaje))

    while True:

        respuesta = json.loads(ws.recv())

        if respuesta.get("id") == contador:
            return respuesta


# ============================================================
# INSTALAR LISTENER
# ============================================================

listener = cdp(
    "Runtime.evaluate",
    {
        "expression": r"""
        (() => {

            const canvas =
                document.getElementById("canvas");

            if (!canvas) {
                return "NO_CANVAS";
            }

            window.__teclas = [];

            window.__listener = function(e) {

                window.__teclas.push({
                    tipo: e.type,
                    key: e.key,
                    code: e.code,
                    keyCode: e.keyCode,
                    which: e.which,
                    repeat: e.repeat,
                    isTrusted: e.isTrusted
                });

            };

            canvas.addEventListener(
                "keydown",
                window.__listener,
                true
            );

            canvas.addEventListener(
                "keyup",
                window.__listener,
                true
            );

            canvas.focus();

            return "LISTENER_ACTIVADO";

        })()
        """,
        "returnByValue": True
    }
)

print()
print("Listener:")
print(json.dumps(listener, indent=2))


# ============================================================
# PRUEBA FÍSICA
# ============================================================

print()
print("======================================")
print(" PRUEBA 1")
print("======================================")
print()
print("Haz clic dentro del juego.")
print("Presiona físicamente la letra A.")
print()

input(
    "Después presiona ENTER aquí..."
)


# ============================================================
# LEER EVENTOS FÍSICOS
# ============================================================

resultado = cdp(
    "Runtime.evaluate",
    {
        "expression": "window.__teclas",
        "returnByValue": True
    }
)

eventos_fisicos = (
    resultado
    .get("result", {})
    .get("result", {})
    .get("value", [])
)


print()
print("EVENTOS DE LA A FÍSICA:")
print(
    json.dumps(
        eventos_fisicos,
        indent=2
    )
)


# ============================================================
# LIMPIAR
# ============================================================

cdp(
    "Runtime.evaluate",
    {
        "expression": """
        window.__teclas = [];
        """,
        "returnByValue": True
    }
)


# ============================================================
# PRUEBA CDP
# ============================================================

input(
    "\nPresiona ENTER aquí para enviar A mediante CDP..."
)


print()
print("Enviando A mediante CDP...")


cdp(
    "Input.dispatchKeyEvent",
    {
        "type": "rawKeyDown",
        "windowsVirtualKeyCode": 65,
        "nativeVirtualKeyCode": 65,
        "key": "a",
        "code": "KeyA",
        "text": "a",
        "unmodifiedText": "a",
    }
)

time.sleep(0.2)


cdp(
    "Input.dispatchKeyEvent",
    {
        "type": "keyUp",
        "windowsVirtualKeyCode": 65,
        "nativeVirtualKeyCode": 65,
        "key": "a",
        "code": "KeyA",
    }
)

time.sleep(0.5)


# ============================================================
# LEER EVENTOS CDP
# ============================================================

resultado = cdp(
    "Runtime.evaluate",
    {
        "expression": "window.__teclas",
        "returnByValue": True
    }
)

eventos_cdp = (
    resultado
    .get("result", {})
    .get("result", {})
    .get("value", [])
)


print()
print("======================================")
print(" EVENTOS DE A POR CDP")
print("======================================")

print(
    json.dumps(
        eventos_cdp,
        indent=2
    )
)


# ============================================================
# FIN
# ============================================================

print()
print("Diagnóstico terminado.")

ws.close()
