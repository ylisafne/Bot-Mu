import requests
import websocket
import json


# ============================================================
# BUSCAR LA PESTAÑA DE MU ONLINE
# ============================================================

pestanas = requests.get(
    "http://127.0.0.1:9222/json"
).json()

juego = None

for pestana in pestanas:
    if (
        pestana.get("type") == "page"
        and "mumagdalenas.com" in pestana.get("url", "")
    ):
        juego = pestana
        break


if juego is None:
    print("No se encontró la pestaña de MU Online.")
    raise SystemExit


print("Pestaña encontrada:")
print(juego["title"])
print(juego["url"])


# ============================================================
# CONECTAR AL DEBUGGER
# ============================================================

ws = websocket.create_connection(
    juego["webSocketDebuggerUrl"]
)


contador = 0


def ejecutar_javascript(codigo):

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

        respuesta = json.loads(
            ws.recv()
        )

        if respuesta.get("id") == contador:
            return respuesta


# ============================================================
# INSPECCIONAR FOCO
# ============================================================

resultado = ejecutar_javascript("""
(() => {
    const el = document.activeElement;

    return {
        tag: el ? el.tagName : null,
        id: el ? el.id : null,
        className: el ? el.className : null,
        name: el ? el.getAttribute("name") : null,
        type: el ? el.getAttribute("type") : null
    };
})()
""")


print()
print("Elemento actualmente enfocado:")
print(
    json.dumps(
        resultado,
        indent=2,
        ensure_ascii=False
    )
)


# ============================================================
# INFORMACIÓN DE LA PÁGINA
# ============================================================

resultado = ejecutar_javascript("""
({
    url: location.href,
    title: document.title,
    body: document.body ? document.body.tagName : null,
    activeElement: document.activeElement
        ? document.activeElement.tagName
        : null
})
""")


print()
print("Información de la página:")
print(
    json.dumps(
        resultado,
        indent=2,
        ensure_ascii=False
    )
)


ws.close()
