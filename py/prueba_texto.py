import json
import time
import requests
import websocket

CDP_URL = "http://127.0.0.1:9222/json"

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
# ENFOCAR CANVAS
# ============================================================

resultado = cdp(
    "Runtime.evaluate",
    {
        "expression": """
        (() => {
            const canvas = document.getElementById("canvas");

            if (!canvas) {
                return "NO_CANVAS";
            }

            canvas.focus();

            return {
                activo: document.activeElement === canvas,
                tag: document.activeElement.tagName
            };
        })()
        """,
        "returnByValue": True
    }
)

print()
print("FOCO:")
print(json.dumps(resultado, indent=2))


input(
    "\nHaz clic en el juego si hace falta y "
    "presiona ENTER aquí para enviar la letra A..."
)


# ============================================================
# LETRA A
# ============================================================

print()
print("Enviando A...")


resultado = cdp(
    "Input.dispatchKeyEvent",
    {
        "type": "rawKeyDown",
        "windowsVirtualKeyCode": 65,
        "nativeVirtualKeyCode": 65,
        "key": "a",
        "code": "KeyA",
        "text": "a",
        "unmodifiedText": "a"
    }
)

print("KEYDOWN:")
print(json.dumps(resultado, indent=2))

time.sleep(0.2)


resultado = cdp(
    "Input.dispatchKeyEvent",
    {
        "type": "keyUp",
        "windowsVirtualKeyCode": 65,
        "nativeVirtualKeyCode": 65,
        "key": "a",
        "code": "KeyA"
    }
)

print("KEYUP:")
print(json.dumps(resultado, indent=2))


print()
print("Prueba terminada.")

ws.close()
