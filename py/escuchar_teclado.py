import requests
import websocket
import json
import time


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
# CONECTAR
# ============================================================

ws = websocket.create_connection(
    juego["webSocketDebuggerUrl"],
    origin="http://127.0.0.1:9222"
)

contador = 0


def comando(method, params=None):

    global contador

    contador += 1

    mensaje = {
        "id": contador,
        "method": method
    }

    if params:
        mensaje["params"] = params

    ws.send(json.dumps(mensaje))

    while True:

        respuesta = json.loads(ws.recv())

        if respuesta.get("id") == contador:
            return respuesta


# ============================================================
# ACTIVAR ESCUCHA DE EVENTOS
# ============================================================

comando("Runtime.evaluate", {
    "expression": """
    (() => {

        const canvas = document.getElementById("canvas");

        if (!canvas) {
            return "NO CANVAS";
        }

        window.__teclas = [];

        window.__listenerTeclado = function(e) {

            window.__teclas.push({
                tipo: e.type,
                key: e.key,
                code: e.code,
                keyCode: e.keyCode,
                which: e.which,
                repeat: e.repeat,
                bubbles: e.bubbles,
                cancelable: e.cancelable,
                isTrusted: e.isTrusted
            });

        };

        canvas.addEventListener(
            "keydown",
            window.__listenerTeclado,
            true
        );

        canvas.addEventListener(
            "keyup",
            window.__listenerTeclado,
            true
        );

        return "LISTENER ACTIVADO";

    })()
    """
})


print()
print("========================================")
print(" ESCUCHANDO TECLADO")
print("========================================")
print()
print("Ahora haz clic dentro del juego.")
print()
print("Después PRESIONA ENTER FÍSICAMENTE.")
print()
print("Luego vuelve aquí y presiona ENTER.")
print("")

input("Presiona ENTER aquí cuando hayas hecho la prueba...")


# ============================================================
# LEER EVENTOS
# ============================================================

resultado = comando("Runtime.evaluate", {
    "expression": "window.__teclas",
    "returnByValue": True
})


print()
print("========================================")
print(" EVENTOS RECIBIDOS POR EL CANVAS")
print("========================================")
print()

try:

    eventos = resultado["result"]["result"].get(
        "value",
        []
    )

    if not eventos:

        print("No se detectaron eventos.")

    else:

        for i, evento in enumerate(eventos, 1):

            print(f"Evento #{i}")
            print(
                json.dumps(
                    evento,
                    indent=2,
                    ensure_ascii=False
                )
            )
            print()

finally:

    # Quitar listeners
    comando("Runtime.evaluate", {
        "expression": """
        (() => {

            const canvas = document.getElementById("canvas");

            if (canvas && window.__listenerTeclado) {

                canvas.removeEventListener(
                    "keydown",
                    window.__listenerTeclado,
                    true
                );

                canvas.removeEventListener(
                    "keyup",
                    window.__listenerTeclado,
                    true
                );
            }

            return "LISTENER ELIMINADO";

        })()
        """
    })

    ws.close()
