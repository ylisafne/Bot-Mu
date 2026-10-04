import requests
import websocket
import json
import time
import ctypes
import pyautogui


# ============================================================
# CONFIGURACIÓN
# ============================================================

CDP_URL = "http://127.0.0.1:9222/json"


# ============================================================
# WINDOWS
# ============================================================

user32 = ctypes.windll.user32


# ============================================================
# BUSCAR MU ONLINE
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
    print("No se encontró MU Online.")
    raise SystemExit


print("Pestaña encontrada:")
print(juego["title"])
print(juego["url"])


# ============================================================
# CONECTAR A CHROME DEVTOOLS
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
# ACTIVAR LISTENER EN EL CANVAS
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

            window.__enter_test = [];

            window.__enter_listener = function(e) {

                if (
                    e.key === "Enter" ||
                    e.code === "Enter" ||
                    e.keyCode === 13
                ) {

                    window.__enter_test.push({
                        type: e.type,
                        key: e.key,
                        code: e.code,
                        keyCode: e.keyCode,
                        which: e.which,
                        repeat: e.repeat,
                        isTrusted: e.isTrusted
                    });

                }

            };

            canvas.addEventListener(
                "keydown",
                window.__enter_listener,
                true
            );

            canvas.addEventListener(
                "keyup",
                window.__enter_listener,
                true
            );

            canvas.focus();

            return "LISTENER_ACTIVADO";

        })()
        """,
        "returnByValue": True
    }
)

print("Listener:", resultado)


# ============================================================
# SELECCIONAR MÉTODO ANTES DE ENFOCAR EL JUEGO
# ============================================================

print()
print("======================================")
print(" ENTER POR CHROME DEVTOOLS")
print("======================================")
print()
print("Se enviará Enter directamente a Chromium.")
print()

input("Presiona ENTER para continuar...")


# ============================================================
# PONER LA VENTANA DE CHROME AL FRENTE
# ============================================================

# Buscar ventana MU Online

EnumWindowsProc = ctypes.WINFUNCTYPE(
    ctypes.c_bool,
    ctypes.c_void_p,
    ctypes.c_void_p
)

hwnd_juego = None


def buscar_ventana(hwnd, lparam):

    global hwnd_juego

    length = user32.GetWindowTextLengthW(hwnd)

    if length == 0:
        return True

    buffer = ctypes.create_unicode_buffer(
        length + 1
    )

    user32.GetWindowTextW(
        hwnd,
        buffer,
        length + 1
    )

    titulo = buffer.value

    if "MU Online" in titulo:

        hwnd_juego = hwnd

        return False

    return True


user32.EnumWindows(
    EnumWindowsProc(buscar_ventana),
    0
)


if hwnd_juego is None:

    print("No se encontró la ventana de MU Online.")

    ws.close()

    raise SystemExit


user32.SetForegroundWindow(
    hwnd_juego
)

time.sleep(0.7)


# ============================================================
# OBTENER CENTRO DE LA VENTANA
# ============================================================

class RECT(ctypes.Structure):
    _fields_ = [
        ("left", ctypes.c_long),
        ("top", ctypes.c_long),
        ("right", ctypes.c_long),
        ("bottom", ctypes.c_long)
    ]


rect = RECT()

user32.GetWindowRect(
    hwnd_juego,
    ctypes.byref(rect)
)

x = (rect.left + rect.right) // 2
y = (rect.top + rect.bottom) // 2


# ============================================================
# CLICK EN EL CANVAS
# ============================================================

pyautogui.moveTo(
    x,
    y,
    duration=0.2
)

time.sleep(0.3)

pyautogui.click()

time.sleep(0.7)


# ============================================================
# ENTER MEDIANTE CHROME DEVTOOLS
# ============================================================

print("Enviando ENTER mediante Chrome DevTools...")

# KEY DOWN

cdp(
    "Input.dispatchKeyEvent",
    {
        "type": "rawKeyDown",
        "windowsVirtualKeyCode": 13,
        "nativeVirtualKeyCode": 13,
        "key": "Enter",
        "code": "Enter",
        "text": "\r",
        "unmodifiedText": "\r"
    }
)

time.sleep(0.2)

# KEY UP

cdp(
    "Input.dispatchKeyEvent",
    {
        "type": "keyUp",
        "windowsVirtualKeyCode": 13,
        "nativeVirtualKeyCode": 13,
        "key": "Enter",
        "code": "Enter"
    }
)

time.sleep(0.5)


# ============================================================
# LEER EVENTOS
# ============================================================

resultado = cdp(
    "Runtime.evaluate",
    {
        "expression": "window.__enter_test",
        "returnByValue": True
    }
)

eventos = resultado[
    "result"
]["result"].get(
    "value",
    []
)


print()
print("======================================")
print(" EVENTOS DEL CANVAS")
print("======================================")

print(
    json.dumps(
        eventos,
        indent=2,
        ensure_ascii=False
    )
)


# ============================================================
# LIMPIAR
# ============================================================

cdp(
    "Runtime.evaluate",
    {
        "expression": """
        (() => {

            const canvas = document.getElementById("canvas");

            if (
                canvas &&
                window.__enter_listener
            ) {

                canvas.removeEventListener(
                    "keydown",
                    window.__enter_listener,
                    true
                );

                canvas.removeEventListener(
                    "keyup",
                    window.__enter_listener,
                    true
                );

            }

        })()
        """
    }
)

ws.close()
