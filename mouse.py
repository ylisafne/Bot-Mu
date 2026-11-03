import ctypes
import ctypes.wintypes as wintypes
import json
import os
import time
import requests
import websocket


# ============================================================
# MU MAGDALENAS
#
# F8 = ACTIVAR / DETENER
#
# No mueve el cursor físico.
#
# F8 ACTIVAR:
#   - Captura el punto actual del canvas.
#   - Guarda la coordenada relativa.
#   - Busca la ventana de Chrome.
#   - Envía WM_RBUTTONDOWN.
#   - Mantiene el estado mientras está activo.
#
# F8 DETENER:
#   - Envía WM_RBUTTONUP.
#
# ============================================================


# ============================================================
# CONFIGURACIÓN
# ============================================================

CHROME_DEBUG_PORT = 9222

CDP_URL = (
    f"http://127.0.0.1:{CHROME_DEBUG_PORT}/json"
)

GAME_DOMAIN = "mumagdalenas.com"

CONFIG_FILE = "mouse_position.json"

VK_F8 = 0x77

CHECK_INTERVAL = 0.05


# ============================================================
# WINDOWS
# ============================================================

user32 = ctypes.WinDLL(
    "user32",
    use_last_error=True
)


# ============================================================
# MENSAJES DE WINDOWS
# ============================================================

WM_MOUSEMOVE = 0x0200

WM_RBUTTONDOWN = 0x0204

WM_RBUTTONUP = 0x0205


MK_RBUTTON = 0x0002


# ============================================================
# POINT
# ============================================================

class POINT(ctypes.Structure):

    _fields_ = [
        ("x", wintypes.LONG),
        ("y", wintypes.LONG)
    ]


# ============================================================
# ESTADO
# ============================================================

contador_cdp = 0

activa = False

ejecutando = True

posicion_guardada = None

chrome_hwnd = None


# ============================================================
# BUSCAR JUEGO
# ============================================================

def buscar_juego():

    try:

        respuesta = requests.get(
            CDP_URL,
            timeout=2
        )

        respuesta.raise_for_status()

        pestanas = respuesta.json()

    except Exception as error:

        print(
            "Error conectando con Chrome:",
            error
        )

        return None


    for pestana in pestanas:

        if (
            pestana.get("type") == "page"
            and GAME_DOMAIN.lower()
            in pestana.get(
                "url",
                ""
            ).lower()
        ):

            return pestana


    return None


# ============================================================
# CONECTAR CDP
# ============================================================

def conectar_cdp(pestana):

    url = pestana.get(
        "webSocketDebuggerUrl"
    )

    if not url:

        raise RuntimeError(
            "No existe WebSocket CDP."
        )

    return websocket.create_connection(
        url,
        origin="http://127.0.0.1:9222",
        timeout=5
    )


# ============================================================
# CDP
# ============================================================

def cdp(ws, method, params=None):

    global contador_cdp

    contador_cdp += 1

    mensaje = {

        "id":
            contador_cdp,

        "method":
            method
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

        if (
            respuesta.get("id")
            == contador_cdp
        ):

            return respuesta


# ============================================================
# JAVASCRIPT
# ============================================================

def ejecutar_js(ws, codigo):

    respuesta = cdp(
        ws,
        "Runtime.evaluate",
        {
            "expression":
                codigo,

            "returnByValue":
                True
        }
    )


    try:

        return (
            respuesta
            ["result"]
            ["result"]
            .get("value")
        )

    except Exception:

        return None


# ============================================================
# OBTENER CANVAS
# ============================================================

def obtener_canvas(ws):

    codigo = r"""
    (() => {

        const canvas =
            document.getElementById("canvas");

        if (!canvas)
            return null;

        const rect =
            canvas.getBoundingClientRect();

        return {

            left:
                rect.left,

            top:
                rect.top,

            width:
                rect.width,

            height:
                rect.height

        };

    })()
    """


    return ejecutar_js(
        ws,
        codigo
    )


# ============================================================
# MOUSE FÍSICO
# ============================================================

def obtener_mouse():

    punto = POINT()

    if not user32.GetCursorPos(
        ctypes.byref(punto)
    ):

        return None

    return (
        punto.x,
        punto.y
    )


# ============================================================
# CAPTURAR POSICIÓN
# ============================================================

def capturar_posicion(ws):

    global posicion_guardada

    mouse = obtener_mouse()

    if mouse is None:

        print(
            "No se pudo obtener posición."
        )

        return False


    canvas = obtener_canvas(
        ws
    )

    if canvas is None:

        print(
            "No existe #canvas."
        )

        return False


    mouse_x, mouse_y = mouse


    x = (
        mouse_x
        - canvas["left"]
    )

    y = (
        mouse_y
        - canvas["top"]
    )


    if (
        x < 0
        or y < 0
        or x >= canvas["width"]
        or y >= canvas["height"]
    ):

        print()
        print(
            "El mouse debe estar sobre el canvas."
        )

        print(
            f"Mouse: {mouse_x}, {mouse_y}"
        )

        print(
            f"Canvas: "
            f"{canvas['left']:.1f}, "
            f"{canvas['top']:.1f}"
        )

        print()

        return False


    posicion_guardada = {

        "x":
            x,

        "y":
            y,

        "width":
            canvas["width"],

        "height":
            canvas["height"]

    }


    try:

        with open(
            CONFIG_FILE,
            "w",
            encoding="utf-8"
        ) as archivo:

            json.dump(
                posicion_guardada,
                archivo,
                indent=4
            )

    except Exception as error:

        print(
            "No se pudo guardar:",
            error
        )


    print()
    print(
        "======================================"
    )

    print(
        " PUNTO GUARDADO"
    )

    print(
        "======================================"
    )

    print()

    print(
        f"Canvas X: {x:.2f}"
    )

    print(
        f"Canvas Y: {y:.2f}"
    )

    print(
        f"Canvas: "
        f"{canvas['width']:.1f} x "
        f"{canvas['height']:.1f}"
    )

    print()

    return True


# ============================================================
# CARGAR POSICIÓN
# ============================================================

def cargar_posicion():

    global posicion_guardada

    if not os.path.exists(
        CONFIG_FILE
    ):

        return False


    try:

        with open(
            CONFIG_FILE,
            "r",
            encoding="utf-8"
        ) as archivo:

            posicion_guardada = json.load(
                archivo
            )

        return True

    except Exception:

        return False


# ============================================================
# BUSCAR VENTANA DE CHROME
# ============================================================

def buscar_chrome():

    encontrada = None


    CALLBACK = ctypes.WINFUNCTYPE(
        ctypes.c_bool,
        wintypes.HWND,
        wintypes.LPARAM
    )


    def callback(hwnd, lparam):

        nonlocal encontrada


        if not user32.IsWindowVisible(
            hwnd
        ):

            return True


        longitud = user32.GetWindowTextLengthW(
            hwnd
        )


        if longitud <= 0:

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
# ALTERNATIVA: BUSCAR CHROME
# ============================================================

def buscar_ventana_chrome():

    encontrada = None


    CALLBACK = ctypes.WINFUNCTYPE(
        ctypes.c_bool,
        wintypes.HWND,
        wintypes.LPARAM
    )


    def callback(hwnd, lparam):

        nonlocal encontrada


        longitud = user32.GetWindowTextLengthW(
            hwnd
        )


        if longitud <= 0:

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


        if (
            "MU Online" in titulo
            or "MuMagdalenas" in titulo
        ):

            encontrada = hwnd

            return False


        return True


    user32.EnumWindows(
        CALLBACK(callback),
        0
    )


    return encontrada


# ============================================================
# OBTENER RECTÁNGULO
# ============================================================

def obtener_rect(hwnd):

    rect = wintypes.RECT()

    resultado = user32.GetWindowRect(
        hwnd,
        ctypes.byref(rect)
    )

    if not resultado:

        return None

    return rect


# ============================================================
# CREAR LPARAM
# ============================================================

def crear_lparam(x, y):

    # Coordenadas relativas a la ventana.
    #
    # LOWORD = X
    # HIWORD = Y

    return (
        (int(y) << 16)
        |
        (int(x) & 0xFFFF)
    )


# ============================================================
# OBTENER POSICIÓN ABSOLUTA
# ============================================================

def calcular_punto_ventana(ws, hwnd):

    if posicion_guardada is None:

        return None


    canvas = obtener_canvas(
        ws
    )

    if canvas is None:

        return None


    rect = obtener_rect(
        hwnd
    )

    if rect is None:

        return None


    # ----------------------------------------
    # Canvas dentro de la ventana Chrome
    # ----------------------------------------

    pantalla_x = (
        canvas["left"]
        +
        posicion_guardada["x"]
    )

    pantalla_y = (
        canvas["top"]
        +
        posicion_guardada["y"]
    )


    # ----------------------------------------
    # Pantalla -> ventana
    # ----------------------------------------

    ventana_x = (
        pantalla_x
        - rect.left
    )

    ventana_y = (
        pantalla_y
        - rect.top
    )


    return (
        int(ventana_x),
        int(ventana_y)
    )


# ============================================================
# ENVIAR WM_MOUSEMOVE
# ============================================================

def enviar_mousemove(
    hwnd,
    x,
    y
):

    lparam = crear_lparam(
        x,
        y
    )


    user32.PostMessageW(
        hwnd,
        WM_MOUSEMOVE,
        MK_RBUTTON,
        lparam
    )


# ============================================================
# ENVIAR RMB DOWN
# ============================================================

def enviar_rmb_down(
    hwnd,
    x,
    y
):

    lparam = crear_lparam(
        x,
        y
    )


    resultado = user32.PostMessageW(
        hwnd,
        WM_RBUTTONDOWN,
        MK_RBUTTON,
        lparam
    )


    return bool(resultado)


# ============================================================
# ENVIAR RMB UP
# ============================================================

def enviar_rmb_up(
    hwnd,
    x,
    y
):

    lparam = crear_lparam(
        x,
        y
    )


    resultado = user32.PostMessageW(
        hwnd,
        WM_RBUTTONUP,
        0,
        lparam
    )


    return bool(resultado)


# ============================================================
# ACTIVAR
# ============================================================

def activar(ws):

    global activa
    global chrome_hwnd


    if activa:

        return


    # ----------------------------------------
    # Capturar punto
    # ----------------------------------------

    if not capturar_posicion(
        ws
    ):

        return


    # ----------------------------------------
    # Buscar ventana
    # ----------------------------------------

    chrome_hwnd = (
        buscar_chrome()
    )


    if chrome_hwnd is None:

        chrome_hwnd = (
            buscar_ventana_chrome()
        )


    if chrome_hwnd is None:

        print()
        print(
            "No se encontró la ventana de MU Online."
        )

        return


    print(
        f"HWND encontrado: "
        f"{chrome_hwnd}"
    )


    # ----------------------------------------
    # Punto
    # ----------------------------------------

    punto = calcular_punto_ventana(
        ws,
        chrome_hwnd
    )


    if punto is None:

        print(
            "No se pudo calcular el punto."
        )

        return


    x, y = punto


    print(
        f"Punto ventana: "
        f"{x}, {y}"
    )


    # ----------------------------------------
    # RMB DOWN
    # ----------------------------------------

    if not enviar_rmb_down(
        chrome_hwnd,
        x,
        y
    ):

        print(
            "No se pudo enviar WM_RBUTTONDOWN."
        )

        return


    activa = True


    print()
    print(
        "======================================"
    )

    print(
        " ACTIVADO"
    )

    print(
        "======================================"
    )

    print()

    print(
        "RMB virtual: PRESIONADO"
    )

    print(
        "Puedes cambiar de ventana."
    )

    print()

    print(
        "F8 = detener"
    )

    print()


# ============================================================
# DESACTIVAR
# ============================================================

def desactivar(ws):

    global activa
    global chrome_hwnd


    if not activa:

        return


    print()
    print(
        "Deteniendo..."
    )


    if chrome_hwnd is not None:

        try:

            punto = calcular_punto_ventana(
                ws,
                chrome_hwnd
            )

            if punto is not None:

                enviar_rmb_up(
                    chrome_hwnd,
                    punto[0],
                    punto[1]
                )

        except Exception as error:

            print(
                "Error liberando RMB:",
                error
            )


    activa = False

    chrome_hwnd = None


    print(
        "RMB virtual: LIBERADO"
    )

    print()


# ============================================================
# MANTENER RMB
# ============================================================

def mantener_rmb(ws):

    global activa


    while (
        activa
        and chrome_hwnd is not None
    ):

        try:

            punto = calcular_punto_ventana(
                ws,
                chrome_hwnd
            )


            if punto is not None:

                enviar_mousemove(
                    chrome_hwnd,
                    punto[0],
                    punto[1]
                )


        except Exception:

            pass


        time.sleep(
            CHECK_INTERVAL
        )


# ============================================================
# F8
# ============================================================

def f8_presionada():

    return bool(
        user32.GetAsyncKeyState(
            VK_F8
        ) & 0x8000
    )


# ============================================================
# CONTROL
# ============================================================

def controlador(ws):

    global ejecutando
    global activa


    estado_anterior = False


    print()
    print(
        "======================================"
    )

    print(
        " LISTO"
    )

    print(
        "======================================"
    )

    print()

    print(
        "Coloca el mouse sobre el punto"
    )

    print(
        "deseado del juego."
    )

    print()

    print(
        "F8 = ACTIVAR / DETENER"
    )

    print(
        "Ctrl+C = SALIR"
    )

    print()


    import threading


    hilo_rmb = None


    while ejecutando:

        estado_actual = (
            f8_presionada()
        )


        if (
            estado_actual
            and not estado_anterior
        ):

            if not activa:

                activar(
                    ws
                )


                if activa:

                    hilo_rmb = threading.Thread(
                        target=mantener_rmb,
                        args=(ws,),
                        daemon=True
                    )

                    hilo_rmb.start()


            else:

                desactivar(
                    ws
                )


        estado_anterior = (
            estado_actual
        )


        time.sleep(
            0.01
        )


# ============================================================
# MAIN
# ============================================================

def main():

    global ejecutando
    global activa


    print()
    print(
        "======================================"
    )

    print(
        " MU MAGDALENAS"
    )

    print(
        " WINDOWS RMB"
    )

    print(
        "======================================"
    )

    print()


    cargar_posicion()


    # ----------------------------------------
    # Buscar pestaña
    # ----------------------------------------

    print(
        "Buscando MuMagdalenas..."
    )


    juego = buscar_juego()


    if juego is None:

        print()
        print(
            "No se encontró el juego."
        )

        print()

        return


    print()

    print(
        "Pestaña encontrada:"
    )

    print(
        "Título:",
        juego.get("title")
    )

    print(
        "URL:",
        juego.get("url")
    )

    print()


    ws = None


    try:

        # ------------------------------------
        # CDP
        # ------------------------------------

        print(
            "Conectando con Chrome DevTools..."
        )


        ws = conectar_cdp(
            juego
        )


        print(
            "Conectado."
        )

        print()


        # ------------------------------------
        # Canvas
        # ------------------------------------

        canvas = obtener_canvas(
            ws
        )


        if canvas is None:

            print(
                "No existe #canvas."
            )

            return


        print(
            "Canvas encontrado:"
        )

        print(
            f"{canvas['width']:.1f} x "
            f"{canvas['height']:.1f}"
        )

        print()


        # ------------------------------------
        # Control
        # ------------------------------------

        controlador(
            ws
        )


    except KeyboardInterrupt:

        print()
        print(
            "Ctrl+C."
        )


    except Exception as error:

        print()
        print(
            "ERROR:"
        )

        print(
            error
        )

        print()


    finally:

        ejecutando = False

        activa = False


        # Seguridad
        if (
            ws is not None
            and chrome_hwnd is not None
        ):

            try:

                punto = calcular_punto_ventana(
                    ws,
                    chrome_hwnd
                )

                if punto:

                    enviar_rmb_up(
                        chrome_hwnd,
                        punto[0],
                        punto[1]
                    )

            except Exception:

                pass


        if ws is not None:

            try:

                ws.close()

            except Exception:

                pass


        print()
        print(
            "Programa finalizado."
        )


# ============================================================
# INICIO
# ============================================================

if __name__ == "__main__":

    main()
