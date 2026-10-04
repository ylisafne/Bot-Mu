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
# VELOCIDAD DE LAS TECLAS
# ============================================================

# Tiempo entre eventos de teclado.
#
# 0.05 = muy rápido
# 0.10 = rápido
# 0.15 = normal
# 0.20 = más seguro

SLEEP = 0.10


# ============================================================
# ESPERA ENTRE VUELTAS
# ============================================================

# Segundos entre el final de una vuelta
# y el comienzo de la siguiente.

LOOP_SLEEP = 5.0


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
# TECLAS OPCIONALES AL FINAL DE CADA VUELTA
# ============================================================

# Puedes poner números o letras.
#
# Ejemplo:
#
# TECLAS_FINALES = [
#     "4",
#     "a",
#     "b",
# ]
#
# Si no quieres ninguna tecla:
#
# TECLAS_FINALES = []

TECLAS_FINALES = [
    "4",
]


# ============================================================
# ESTADO
# ============================================================

activa = False
ejecutando = True

user32 = ctypes.windll.user32


# ============================================================
# PAUSA GLOBAL
# ============================================================

def pausa():

    time.sleep(SLEEP)


# ============================================================
# ESPERA INTERRUMPIBLE
# ============================================================

def esperar_loop():

    inicio = time.time()

    while activa:

        transcurrido = time.time() - inicio

        if transcurrido >= LOOP_SLEEP:

            return True

        time.sleep(0.05)

    return False


# ============================================================
# BUSCAR PESTAÑA DEL JUEGO
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

    print("MuMagdalenas no está abierto.")
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
# OBTENER JUEGO
# ============================================================

def obtener_juego():

    juego = buscar_juego()


    # Ya está abierto

    if juego is not None:

        print()
        print("======================================")
        print(" MU MAGDALENAS YA ESTÁ ABIERTO")
        print("======================================")
        print()
        print(juego["title"])
        print(juego["url"])
        print()

        return juego


    # Abrir Chrome

    lanzar_chrome()


    print("Esperando Chrome...")


    for _ in range(40):

        juego = buscar_juego()

        if juego is not None:

            break

        time.sleep(0.5)


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
# CDP
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

    print("Esperando canvas...")


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

    encontrada = None


    CALLBACK = ctypes.WINFUNCTYPE(
        ctypes.c_bool,
        ctypes.c_void_p,
        ctypes.c_void_p
    )


    def callback(hwnd, lparam):

        nonlocal encontrada


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

            encontrada = hwnd

            return False


        return True


    user32.EnumWindows(
        CALLBACK(callback),
        0
    )


    return encontrada


# ============================================================
# ACTIVAR VENTANA
# ============================================================

def activar_ventana():

    hwnd = buscar_ventana()


    if hwnd is None:

        print(
            "No se encontró la ventana de MU Online."
        )

        return None


    # Mostrar si está minimizada

    user32.ShowWindow(
        hwnd,
        9
    )


    # Poner delante

    user32.SetForegroundWindow(
        hwnd
    )


    pausa()


    return hwnd


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
# ENVIAR TECLA POR CDP
# ============================================================

def enviar_tecla_cdp(
    ws,
    key,
    code,
    vk
):

    # -----------------------------
    # KEY DOWN
    # -----------------------------

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


    # -----------------------------
    # KEY UP
    # -----------------------------

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

    print("  -> ESC x2")


    # ESC #1

    enviar_tecla_cdp(
        ws,
        "Escape",
        "Escape",
        27
    )


    pausa()


    # ESC #2

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

    print("  -> ENTER")


    enviar_tecla_cdp(
        ws,
        "Enter",
        "Enter",
        13
    )


# ============================================================
# ENVIAR TECLA SIMPLE
# ============================================================

def enviar_tecla(ws, tecla):

    # Convertir a string por seguridad

    tecla = str(tecla)


    # Solo permitimos una tecla por elemento

    if len(tecla) != 1:

        print(
            f"  -> TECLA NO VÁLIDA: {tecla}"
        )

        return False


    # --------------------------------
    # NÚMEROS
    # --------------------------------

    if tecla.isdigit():

        vk = ord(tecla)

        code = f"Digit{tecla}"

        key = tecla


    # --------------------------------
    # LETRAS
    # --------------------------------

    elif tecla.isalpha():

        tecla = tecla.lower()

        vk = ord(tecla.upper())

        code = f"Key{tecla.upper()}"

        key = tecla.upper()


    # --------------------------------
    # OTROS
    # --------------------------------

    else:

        print(
            f"  -> TECLA NO SOPORTADA: {tecla}"
        )

        return False


    print(
        f"  -> TECLA {key}"
    )


    enviar_tecla_cdp(
        ws,
        key,
        code,
        vk
    )


    return True


# ============================================================
# EJECUTAR TECLAS FINALES
# ============================================================

def ejecutar_teclas_finales(ws):

    # Si el array está vacío,
    # simplemente no hace nada.

    for tecla in TECLAS_FINALES:

        if not activa:

            return False


        if not enviar_tecla(
            ws,
            tecla
        ):

            print(
                f"  -> No se pudo enviar: {tecla}"
            )


    return True


# ============================================================
# ESCRIBIR COMANDO
# ============================================================

def escribir_comando(comando):

    print(
        f"  -> ESCRIBIENDO: {comando}"
    )


    pyautogui.write(
        comando,
        interval=SLEEP
    )


# ============================================================
# CLICK DERECHO
# ============================================================

def mantener_click_derecho():

    print(
        "  -> BOTÓN DERECHO PRESIONADO"
    )


    pyautogui.mouseDown(
        button="right"
    )


def soltar_click_derecho():

    pyautogui.mouseUp(
        button="right"
    )


    print(
        "  -> BOTÓN DERECHO LIBERADO"
    )


# ============================================================
# EJECUTAR UNA VUELTA
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
    # ACTIVAR VENTANA
    # ----------------------------------------

    hwnd = activar_ventana()


    if hwnd is None:

        activa = False

        return False


    # ----------------------------------------
    # FOCO DEL CANVAS
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
            f"[{indice}/{len(COMANDOS)}] "
            f"{comando}"
        )


        # ------------------------------------
        # ENTER ANTES
        # ------------------------------------

        enviar_enter(ws)


        if not activa:

            return False


        # ------------------------------------
        # ESCRIBIR COMANDO
        # ------------------------------------

        escribir_comando(
            comando
        )


        pausa()


        if not activa:

            return False


        # ------------------------------------
        # ENTER DESPUÉS
        # ------------------------------------

        enviar_enter(ws)


    # ----------------------------------------
    # TECLAS OPCIONALES AL FINAL
    # ----------------------------------------

    if not ejecutar_teclas_finales(ws):

        return False


    return True


# ============================================================
# LOOP INFINITO
# ============================================================

def ejecutar_loop(ws):

    global activa


    numero_vuelta = 1


    print()
    print("======================================")
    print(" LOOP INICIADO")
    print("======================================")
    print(
        f"SLEEP       = {SLEEP}"
    )
    print(
        f"LOOP_SLEEP  = {LOOP_SLEEP}"
    )

    print(
        f"TECLAS_FINALES = {TECLAS_FINALES}"
    )

    print()


    # Mantener botón derecho presionado
    # durante TODO el loop.

    mantener_click_derecho()


    try:

        while activa:


            # --------------------------------
            # EJECUTAR VUELTA
            # --------------------------------

            if not ejecutar_una_vuelta(
                ws,
                numero_vuelta
            ):

                break


            numero_vuelta += 1


            if not activa:

                break


            # --------------------------------
            # ESPERAR
            # --------------------------------

            print()
            print(
                f"Esperando "
                f"{LOOP_SLEEP} segundos..."
            )


            if not esperar_loop():

                break


    finally:

        # IMPORTANTE:
        # liberar siempre el botón derecho

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
        f"SLEEP      = {SLEEP}"
    )
    print(
        f"LOOP_SLEEP = {LOOP_SLEEP}"
    )
    print(
        f"TECLAS_FINALES = {TECLAS_FINALES}"
    )
    print()
    print("F8 = iniciar / detener")
    print("Ctrl+C = salir")
    print()


    while ejecutando:


        estado_actual = bool(
            user32.GetAsyncKeyState(
                VK_F8
            ) & 0x8000
        )


        # ------------------------------------
        # NUEVA PULSACIÓN F8
        # ------------------------------------

        if (
            estado_actual
            and not estado_anterior
        ):


            activa = not activa


            if activa:

                print()
                print(
                    ">>> F8: LOOP ACTIVADO"
                )


                threading.Thread(
                    target=ejecutar_loop,
                    args=(ws,),
                    daemon=True
                ).start()


            else:

                print()
                print(
                    ">>> F8: LOOP DETENIDO"
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
        "No se encontró el canvas."
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


    # Por seguridad, liberar
    # el botón derecho.

    try:

        pyautogui.mouseUp(
            button="right"
        )

    except Exception:

        pass


    ws.close()
