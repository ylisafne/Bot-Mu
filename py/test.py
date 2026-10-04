import ctypes
import json
import subprocess
import threading
import time

import pyautogui
import requests
import websocket


# ============================================================
# CONFIGURACIÓN
# ============================================================

CHROME_EXE = r"C:\Program Files\Google\Chrome\Application\chrome.exe"

CHROME_DEBUG_PORT = 9222

CHROME_PROFILE = r"C:\ChromeDebug"

GAME_URL = "https://play.mumagdalenas.com/"

CDP_URL = f"http://127.0.0.1:{CHROME_DEBUG_PORT}/json"

VK_F8 = 0x77


# ============================================================
# COMANDOS
# ============================================================

COMANDOS = [
    "/agi 65000",
    "/cmd 65000",
    "/str 65000",
    "/ene 65000",
    "/sta 65000",
]


# ============================================================
# ESTADO
# ============================================================

activa = False
ejecutando = True

user32 = ctypes.windll.user32


# ============================================================
# RECTÁNGULO DE WINDOWS
# ============================================================

class RECT(ctypes.Structure):

    _fields_ = [
        ("left", ctypes.c_long),
        ("top", ctypes.c_long),
        ("right", ctypes.c_long),
        ("bottom", ctypes.c_long),
    ]


# ============================================================
# BUSCAR PESTAÑA DE MU MAGDALENAS
# ============================================================

def buscar_juego():

    try:

        respuesta = requests.get(
            CDP_URL,
            timeout=1
        )

        if not respuesta.ok:
            return None

        pestanas = respuesta.json()

    except Exception:

        return None


    for pestana in pestanas:

        if (
            pestana.get("type") == "page"
            and "mumagdalenas.com" in pestana.get("url", "")
        ):

            return pestana


    return None


# ============================================================
# LANZAR CHROME
# ============================================================

def lanzar_chrome():

    print("Chrome de automatización no está abierto.")
    print("Lanzando Chrome...")

    subprocess.Popen(
        [
            CHROME_EXE,
            f"--remote-debugging-port={CHROME_DEBUG_PORT}",
            "--remote-allow-origins=*",
            f"--user-data-dir={CHROME_PROFILE}",
            GAME_URL,
        ],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL
    )


# ============================================================
# OBTENER O ABRIR EL JUEGO
# ============================================================

def obtener_juego():

    # ----------------------------------------
    # Primero comprobar si ya existe
    # ----------------------------------------

    juego = buscar_juego()

    if juego is not None:

        print()
        print("======================================")
        print(" MU MAGDALENAS YA ESTÁ ABIERTO")
        print("======================================")
        print()
        print("Pestaña encontrada:")
        print(juego["title"])
        print(juego["url"])
        print()

        return juego


    # ----------------------------------------
    # No existe -> lanzar Chrome
    # ----------------------------------------

    lanzar_chrome()


    # ----------------------------------------
    # Esperar a Chrome
    # ----------------------------------------

    print("Esperando a Chrome...")

    for _ in range(40):

        if buscar_juego() is not None:
            break

        time.sleep(0.5)


    # ----------------------------------------
    # Esperar la pestaña
    # ----------------------------------------

    print("Esperando MuMagdalenas...")

    for _ in range(60):

        juego = buscar_juego()

        if juego is not None:

            print()
            print("======================================")
            print(" PESTAÑA ENCONTRADA")
            print("======================================")
            print()
            print(juego["title"])
            print(juego["url"])
            print()

            return juego

        time.sleep(0.5)


    return None


# ============================================================
# CONECTAR A CHROME DEVTOOLS
# ============================================================

contador_cdp = 0


def conectar_cdp(juego):

    print("Conectando con Chrome DevTools...")

    return websocket.create_connection(
        juego["webSocketDebuggerUrl"],
        origin="http://127.0.0.1:9222"
    )


def cdp(ws, method, params=None):

    global contador_cdp

    contador_cdp += 1

    mensaje = {
        "id": contador_cdp,
        "method": method
    }


    if params is not None:

        mensaje["params"] = params


    ws.send(
        json.dumps(mensaje)
    )


    while True:

        respuesta = json.loads(
            ws.recv()
        )


        if respuesta.get("id") == contador_cdp:

            return respuesta


# ============================================================
# ESPERAR CANVAS
# ============================================================

def esperar_canvas(ws):

    print("Esperando canvas del juego...")

    for _ in range(60):

        resultado = cdp(
            ws,
            "Runtime.evaluate",
            {
                "expression":
                    "!!document.getElementById('canvas')",
                "returnByValue": True
            }
        )


        try:

            existe = resultado[
                "result"
            ][
                "result"
            ].get(
                "value",
                False
            )

        except Exception:

            existe = False


        if existe:

            print("Canvas encontrado.")

            return True


        time.sleep(0.5)


    return False


# ============================================================
# BUSCAR VENTANA DE MU ONLINE
# ============================================================

def buscar_ventana():

    hwnd_encontrado = None


    EnumWindowsProc = ctypes.WINFUNCTYPE(
        ctypes.c_bool,
        ctypes.c_void_p,
        ctypes.c_void_p
    )


    def callback(hwnd, lparam):

        nonlocal hwnd_encontrado


        longitud = user32.GetWindowTextLengthW(
            hwnd
        )


        if longitud == 0:

            return True


        buffer = ctypes.create_unicode_buffer(
            longitud + 1
        )


        user32.GetWindowTextW(
            hwnd,
            buffer,
            longitud + 1
        )


        titulo = buffer.value


        if "MU Online" in titulo:

            hwnd_encontrado = hwnd

            return False


        return True


    user32.EnumWindows(
        EnumWindowsProc(callback),
        0
    )


    return hwnd_encontrado


# ============================================================
# PONER VENTANA AL FRENTE
# ============================================================

def activar_ventana():

    hwnd = buscar_ventana()


    if hwnd is None:

        print(
            "No se encontró la ventana de MU Online."
        )

        return None


    user32.SetForegroundWindow(
        hwnd
    )


    time.sleep(0.6)


    return hwnd


# ============================================================
# MOVER MOUSE Y HACER CLICK
# ============================================================

def click_centro(hwnd):

    rect = RECT()


    user32.GetWindowRect(
        hwnd,
        ctypes.byref(rect)
    )


    x = (
        rect.left +
        rect.right
    ) // 2


    y = (
        rect.top +
        rect.bottom
    ) // 2


    print(
        f"Mouse -> centro: {x}, {y}"
    )


    pyautogui.moveTo(
        x,
        y,
        duration=0.2
    )


    time.sleep(0.2)


    pyautogui.click()


    time.sleep(0.5)


# ============================================================
# ENFOCAR CANVAS
# ============================================================

def enfocar_canvas(ws):

    resultado = cdp(
        ws,
        "Runtime.evaluate",
        {
            "expression": """
            (() => {

                const canvas =
                    document.getElementById("canvas");

                if (!canvas) {
                    return false;
                }

                canvas.focus();

                return document.activeElement === canvas;

            })()
            """,
            "returnByValue": True
        }
    )


    try:

        return resultado[
            "result"
        ][
            "result"
        ].get(
            "value",
            False
        )

    except Exception:

        return False


# ============================================================
# ENVIAR ENTER
# ============================================================

def enviar_enter(ws):

    # KEY DOWN

    cdp(
        ws,
        "Input.dispatchKeyEvent",
        {
            "type": "rawKeyDown",

            "windowsVirtualKeyCode": 13,

            "nativeVirtualKeyCode": 13,

            "key": "Enter",

            "code": "Enter",

            "text": "\r",

            "unmodifiedText": "\r",
        }
    )


    time.sleep(0.15)


    # KEY UP

    cdp(
        ws,
        "Input.dispatchKeyEvent",
        {
            "type": "keyUp",

            "windowsVirtualKeyCode": 13,

            "nativeVirtualKeyCode": 13,

            "key": "Enter",

            "code": "Enter",
        }
    )


    time.sleep(0.15)


# ============================================================
# ENVIAR CARÁCTER
# ============================================================

def enviar_caracter(ws, caracter):


    # ----------------------------------------
    # LETRAS
    # ----------------------------------------

    if caracter.isalpha():

        tecla = {

            "key": caracter,

            "code":
                f"Key{caracter.upper()}",

            "windowsVirtualKeyCode":
                ord(caracter.upper()),

            "nativeVirtualKeyCode":
                ord(caracter.upper()),
        }


    # ----------------------------------------
    # NÚMEROS
    # ----------------------------------------

    elif caracter.isdigit():

        tecla = {

            "key": caracter,

            "code":
                f"Digit{caracter}",

            "windowsVirtualKeyCode":
                ord(caracter),

            "nativeVirtualKeyCode":
                ord(caracter),
        }


    # ----------------------------------------
    # SLASH
    # ----------------------------------------

    elif caracter == "/":

        tecla = {

            "key": "/",

            "code": "Slash",

            "windowsVirtualKeyCode": 191,

            "nativeVirtualKeyCode": 191,
        }


    # ----------------------------------------
    # ESPACIO
    # ----------------------------------------

    elif caracter == " ":

        tecla = {

            "key": " ",

            "code": "Space",

            "windowsVirtualKeyCode": 32,

            "nativeVirtualKeyCode": 32,
        }


    else:

        print(
            "Carácter no soportado:",
            repr(caracter)
        )

        return


    # ----------------------------------------
    # KEY DOWN
    # ----------------------------------------

    cdp(
        ws,
        "Input.dispatchKeyEvent",
        {
            "type": "rawKeyDown",

            "windowsVirtualKeyCode":
                tecla["windowsVirtualKeyCode"],

            "nativeVirtualKeyCode":
                tecla["nativeVirtualKeyCode"],

            "key":
                tecla["key"],

            "code":
                tecla["code"],

            "text":
                caracter,

            "unmodifiedText":
                caracter,
        }
    )


    time.sleep(0.04)


    # ----------------------------------------
    # KEY UP
    # ----------------------------------------

    cdp(
        ws,
        "Input.dispatchKeyEvent",
        {
            "type": "keyUp",

            "windowsVirtualKeyCode":
                tecla["windowsVirtualKeyCode"],

            "nativeVirtualKeyCode":
                tecla["nativeVirtualKeyCode"],

            "key":
                tecla["key"],

            "code":
                tecla["code"],
        }
    )


    time.sleep(0.04)


# ============================================================
# ESCRIBIR TEXTO
# ============================================================

def escribir_texto(ws, texto):

    for caracter in texto:

        # Permitir detener inmediatamente
        if not activa:

            return False


        enviar_caracter(
            ws,
            caracter
        )


    return True


# ============================================================
# EJECUTAR TODA LA SECUENCIA
# ============================================================

def ejecutar_secuencia(ws):

    global activa


    hwnd = activar_ventana()


    if hwnd is None:

        activa = False

        return


    print()
    print("======================================")
    print(" SECUENCIA INICIADA")
    print("======================================")


    # ----------------------------------------
    # Dar foco
    # ----------------------------------------

    click_centro(hwnd)

    enfocar_canvas(ws)

    time.sleep(0.3)


    # ----------------------------------------
    # EJECUTAR COMANDOS
    # ----------------------------------------

    for indice, comando in enumerate(
        COMANDOS,
        start=1
    ):


        if not activa:

            break


        print()
        print(
            f"[{indice}/{len(COMANDOS)}] "
            f"{comando}"
        )


        # ------------------------------------
        # ENTER ANTES
        # ------------------------------------

        if not activa:
            break

        print("  -> ENTER")

        enviar_enter(ws)

        time.sleep(0.25)


        # ------------------------------------
        # TEXTO
        # ------------------------------------

        if not activa:
            break

        print(
            f"  -> {comando}"
        )


        if not escribir_texto(
            ws,
            comando
        ):

            break


        time.sleep(0.25)


        # ------------------------------------
        # ENTER DESPUÉS
        # ------------------------------------

        if not activa:
            break

        print("  -> ENTER")

        enviar_enter(ws)

        time.sleep(0.5)


    activa = False


    print()
    print("======================================")
    print(" SECUENCIA TERMINADA")
    print("======================================")
    print()


# ============================================================
# MONITOR F8
# ============================================================

def monitor_f8(ws):

    global activa
    global ejecutando


    estado_anterior = False


    print()
    print("======================================")
    print(" AUTOMATIZACIÓN LISTA")
    print("======================================")
    print()
    print("F8 = iniciar")
    print("F8 = detener")
    print()
    print("Comandos:")

    for comando in COMANDOS:

        print(
            f"  ENTER -> {comando} -> ENTER"
        )

    print()


    while ejecutando:


        estado_actual = bool(
            user32.GetAsyncKeyState(
                VK_F8
            ) & 0x8000
        )


        # Detectar solamente
        # la pulsación inicial

        if (
            estado_actual
            and not estado_anterior
        ):


            activa = not activa


            if activa:

                print()
                print(
                    ">>> F8: ACTIVADO"
                )


                threading.Thread(
                    target=ejecutar_secuencia,
                    args=(ws,),
                    daemon=True
                ).start()


            else:

                print()
                print(
                    ">>> F8: DETENIDO"
                )


        estado_anterior = estado_actual


        time.sleep(0.03)


# ============================================================
# PROGRAMA PRINCIPAL
# ============================================================

print()
print("======================================")
print(" MU MAGDALENAS AUTOMATIZACIÓN")
print("======================================")
print()


# ============================================================
# OBTENER JUEGO
# ============================================================

juego = obtener_juego()


if juego is None:

    print()
    print(
        "No se pudo encontrar MuMagdalenas."
    )

    raise SystemExit


# ============================================================
# CONECTAR CDP
# ============================================================

ws = conectar_cdp(
    juego
)


# ============================================================
# ESPERAR CANVAS
# ============================================================

if not esperar_canvas(ws):

    print()
    print(
        "No se encontró el canvas del juego."
    )

    ws.close()

    raise SystemExit


# ============================================================
# ESPERAR F8
# ============================================================

try:

    monitor_f8(ws)


except KeyboardInterrupt:

    print()
    print(
        "Programa detenido."
    )


finally:

    ejecutando = False

    activa = False

    ws.close()
