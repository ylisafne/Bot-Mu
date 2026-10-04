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
# VELOCIDAD
# ============================================================

# Velocidad de las acciones individuales.

SLEEP = 0.10


# ============================================================
# ESPERA ENTRE VUELTAS
# ============================================================

LOOP_SLEEP = 5.0


# ============================================================
# COMANDOS
# ============================================================

COMANDOS = [
    "/agi 50000",
    #"/cmd 65000",
    #"/str 65000",
    "/ene 50000",
    "/sta 50000",
]


# ============================================================
# ESTADO
# ============================================================

activa = False
ejecutando = True

user32 = ctypes.windll.user32


# ============================================================
# CDP
# ============================================================

contador_cdp = 0


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
# PAUSA
# ============================================================

def pausa():

    time.sleep(SLEEP)


# ============================================================
# BUSCAR PESTAÑAS
# ============================================================

def obtener_pestanas():

    try:

        respuesta = requests.get(
            CDP_URL,
            timeout=2
        )

        respuesta.raise_for_status()

        return respuesta.json()

    except Exception as e:

        print(
            f"Error consultando Chrome: {e}"
        )

        return []


# ============================================================
# MOSTRAR PESTAÑAS DEL JUEGO
# ============================================================

def seleccionar_pestana():

    pestanas = obtener_pestanas()


    juegos = []


    for pestana in pestanas:

        if pestana.get("type") != "page":
            continue


        url = pestana.get(
            "url",
            ""
        )


        if "mumagdalenas.com" not in url:
            continue


        if not pestana.get(
            "webSocketDebuggerUrl"
        ):
            continue


        juegos.append(
            pestana
        )


    if not juegos:

        return None


    print()
    print("======================================")
    print(" PESTAÑAS DE MU MAGDALENAS")
    print("======================================")
    print()


    for i, juego in enumerate(
        juegos,
        start=1
    ):

        print(
            f"[{i}] {juego.get('title', '')}"
        )

        print(
            f"    {juego.get('url', '')}"
        )

        print(
            f"    ID: {juego.get('id', '')}"
        )

        print()


    if len(juegos) == 1:

        print(
            "Solo hay una pestaña de MuMagdalenas."
        )

        return juegos[0]


    while True:

        try:

            seleccion = int(
                input(
                    "Selecciona la pestaña: "
                )
            )

        except ValueError:

            print(
                "Introduce un número válido."
            )

            continue


        if 1 <= seleccion <= len(juegos):

            return juegos[
                seleccion - 1
            ]


        print(
            "Selección inválida."
        )


# ============================================================
# LANZAR CHROME
# ============================================================

def lanzar_chrome():

    print(
        "No se encontró MuMagdalenas."
    )

    print(
        "Lanzando Chrome..."
    )


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
# OBTENER PESTAÑA OBJETIVO
# ============================================================

def obtener_objetivo():

    juego = seleccionar_pestana()


    if juego is not None:

        return juego


    lanzar_chrome()


    print(
        "Esperando a que Chrome abra el juego..."
    )


    for _ in range(60):

        juego = seleccionar_pestana()


        if juego is not None:

            return juego


        time.sleep(0.5)


    return None


# ============================================================
# CONECTAR AL WEBSOCKET ESPECÍFICO
# ============================================================

def conectar_objetivo(juego):

    websocket_url = juego[
        "webSocketDebuggerUrl"
    ]


    print()
    print("======================================")
    print(" OBJETIVO SELECCIONADO")
    print("======================================")
    print()
    print(
        f"Título: {juego.get('title')}"
    )
    print(
        f"URL: {juego.get('url')}"
    )
    print()
    print(
        "WebSocketDebuggerUrl:"
    )
    print(
        websocket_url
    )
    print()


    # ESTA CONEXIÓN QUEDA FIJA
    # A ESTA PESTAÑA.

    ws = websocket.create_connection(
        websocket_url,
        origin="http://127.0.0.1:9222"
    )


    return ws


# ============================================================
# OBTENER WINDOW ID DE LA PESTAÑA
# ============================================================

def obtener_window_id(ws):

    respuesta = cdp(
        ws,
        "Browser.getWindowForTarget"
    )


    try:

        return respuesta[
            "result"
        ][
            "windowId"
        ]

    except Exception:

        return None


# ============================================================
# ACTIVAR VENTANA CORRESPONDIENTE
# ============================================================

def activar_ventana_cdp(ws):

    window_id = obtener_window_id(
        ws
    )


    if window_id is None:

        print(
            "No se pudo obtener windowId."
        )

        return False


    # Restaurar si está minimizada.

    cdp(
        ws,
        "Browser.setWindowBounds",
        {
            "windowId": window_id,

            "bounds": {
                "windowState": "normal"
            }
        }
    )


    pausa()


    # Traer al frente.

    cdp(
        ws,
        "Page.bringToFront"
    )


    pausa()


    return True


# ============================================================
# ESPERAR CANVAS
# ============================================================

def esperar_canvas(ws):

    print(
        "Esperando canvas..."
    )


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

            print(
                "Canvas encontrado."
            )

            return True


        time.sleep(0.5)


    return False


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
# ENVIAR TECLA
# ============================================================

def enviar_tecla_cdp(
    ws,
    key,
    code,
    vk
):

    # KEY DOWN

    cdp(
        ws,
        "Input.dispatchKeyEvent",
        {
            "type": "rawKeyDown",

            "windowsVirtualKeyCode": vk,

            "nativeVirtualKeyCode": vk,

            "key": key,

            "code": code,

            "text": "",

            "unmodifiedText": "",
        }
    )


    pausa()


    # KEY UP

    cdp(
        ws,
        "Input.dispatchKeyEvent",
        {
            "type": "keyUp",

            "windowsVirtualKeyCode": vk,

            "nativeVirtualKeyCode": vk,

            "key": key,

            "code": code,
        }
    )


    pausa()


# ============================================================
# ESC x2
# ============================================================

def enviar_escape(ws):

    print(
        "  -> ESC #1"
    )


    enviar_tecla_cdp(
        ws,
        "Escape",
        "Escape",
        27
    )


    pausa()


    print(
        "  -> ESC #2"
    )


    enviar_tecla_cdp(
        ws,
        "Escape",
        "Escape",
        27
    )


# ============================================================
# ENTER
# ============================================================

def enviar_enter(ws):

    print(
        "  -> ENTER"
    )


    enviar_tecla_cdp(
        ws,
        "Enter",
        "Enter",
        13
    )


# ============================================================
# ESCRIBIR TEXTO
# ============================================================

def escribir_comando(comando):

    print(
        f"  -> {comando}"
    )


    pyautogui.write(
        comando,
        interval=SLEEP
    )


# ============================================================
# BOTÓN DERECHO
# ============================================================

def mantener_click_derecho():

    print(
        "  -> CLICK DERECHO PRESIONADO"
    )


    pyautogui.mouseDown(
        button="right"
    )


def soltar_click_derecho():

    try:

        pyautogui.mouseUp(
            button="right"
        )

    except Exception:

        pass


    print(
        "  -> CLICK DERECHO LIBERADO"
    )


# ============================================================
# UNA VUELTA
# ============================================================

def ejecutar_una_vuelta(
    ws,
    numero
):

    global activa


    if not activa:

        return False


    print()
    print("--------------------------------------")
    print(
        f" VUELTA #{numero}"
    )
    print("--------------------------------------")


    # ----------------------------------------
    # ACTIVAR LA VENTANA DE ESTA PESTAÑA
    # ----------------------------------------

    if not activar_ventana_cdp(ws):

        activa = False

        return False


    # ----------------------------------------
    # FOCO
    # ----------------------------------------

    enfocar_canvas(ws)

    pausa()


    if not activa:

        return False


    # ----------------------------------------
    # ESC x2
    # ----------------------------------------

    enviar_escape(ws)


    # ----------------------------------------
    # COMANDOS
    # ----------------------------------------

    for indice, comando in enumerate(
        COMANDOS,
        start=1
    ):

        if not activa:

            return False


        print()
        print(
            f"[{indice}/{len(COMANDOS)}]"
        )


        # ENTER ANTES

        enviar_enter(ws)


        if not activa:

            return False


        # COMANDO

        escribir_comando(
            comando
        )


        pausa()


        if not activa:

            return False


        # ENTER DESPUÉS

        enviar_enter(ws)


    return True


# ============================================================
# LOOP
# ============================================================

def ejecutar_loop(ws):

    global activa


    numero_vuelta = 1


    print()
    print("======================================")
    print(" LOOP INICIADO")
    print("======================================")
    print(
        f"SLEEP      = {SLEEP}"
    )
    print(
        f"LOOP_SLEEP = {LOOP_SLEEP}"
    )
    print()


    # Mantener botón derecho.

    mantener_click_derecho()


    try:

        while activa:

            if not ejecutar_una_vuelta(
                ws,
                numero_vuelta
            ):

                break


            numero_vuelta += 1


            if not activa:

                break


            print()
            print(
                f"Esperando {LOOP_SLEEP} segundos..."
            )


            inicio = time.time()


            while activa:

                if (
                    time.time() - inicio
                    >= LOOP_SLEEP
                ):

                    break


                time.sleep(0.05)


    finally:

        soltar_click_derecho()


        print()
        print("======================================")
        print(" LOOP DETENIDO")
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
    print(
        "F8 = iniciar / detener"
    )
    print(
        "Ctrl+C = salir"
    )
    print()


    while ejecutando:

        estado_actual = bool(
            user32.GetAsyncKeyState(
                VK_F8
            ) & 0x8000
        )


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
                    target=ejecutar_loop,
                    args=(ws,),
                    daemon=True
                ).start()


            else:

                print()
                print(
                    ">>> F8: DETENIENDO..."
                )


        estado_anterior = estado_actual


        time.sleep(0.01)


# ============================================================
# MAIN
# ============================================================

print()
print("======================================")
print(" MU MAGDALENAS AUTOMATIZACIÓN")
print("======================================")
print()


# ============================================================
# SELECCIONAR PESTAÑA
# ============================================================

juego = obtener_objetivo()


if juego is None:

    print()
    print(
        "No se encontró ninguna pestaña "
        "de MuMagdalenas."
    )

    raise SystemExit


# ============================================================
# CONECTAR AL WEBSOCKET DE ESA PESTAÑA
# ============================================================

ws = conectar_objetivo(
    juego
)


# ============================================================
# CANVAS
# ============================================================

if not esperar_canvas(ws):

    print(
        "No se encontró el canvas."
    )

    ws.close()

    raise SystemExit


# ============================================================
# MOSTRAR WINDOW ID
# ============================================================

window_id = obtener_window_id(
    ws
)


print(
    f"Window ID asociado: {window_id}"
)


# ============================================================
# MONITOR F8
# ============================================================

try:

    monitor_f8(ws)


except KeyboardInterrupt:

    print()
    print(
        "Programa detenido."
    )


finally:

    activa = False
    ejecutando = False


    try:

        pyautogui.mouseUp(
            button="right"
        )

    except Exception:

        pass


    try:

        ws.close()

    except Exception:

        pass
