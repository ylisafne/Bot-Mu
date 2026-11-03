import ctypes
import ctypes.wintypes as wintypes
import json
import time
import requests
import websocket


# ============================================================
# CONFIGURACIÓN
# ============================================================

CHROME_DEBUG_PORT = 9222

CDP_URL = (
    f"http://127.0.0.1:{CHROME_DEBUG_PORT}/json"
)

GAME_DOMAIN = "mumagdalenas.com"

TIEMPO_FUERA = 15


# ============================================================
# WINDOWS API
# ============================================================

user32 = ctypes.WinDLL(
    "user32",
    use_last_error=True
)


SW_RESTORE = 9
SW_SHOW = 5


# ============================================================
# SENDINPUT
# ============================================================

INPUT_MOUSE = 0

MOUSEEVENTF_RIGHTDOWN = 0x0008
MOUSEEVENTF_RIGHTUP = 0x0010


class MOUSEINPUT(ctypes.Structure):

    _fields_ = [

        ("dx", wintypes.LONG),

        ("dy", wintypes.LONG),

        ("mouseData", wintypes.DWORD),

        ("dwFlags", wintypes.DWORD),

        ("time", wintypes.DWORD),

        (
            "dwExtraInfo",
            ctypes.POINTER(wintypes.ULONG)
        )

    ]


class INPUT_UNION(ctypes.Union):

    _fields_ = [

        ("mi", MOUSEINPUT)

    ]


class INPUT(ctypes.Structure):

    _anonymous_ = (
        "u",
    )

    _fields_ = [

        ("type", wintypes.DWORD),

        ("u", INPUT_UNION)

    ]


SendInput = user32.SendInput

SendInput.argtypes = [
    wintypes.UINT,
    ctypes.POINTER(INPUT),
    ctypes.c_int
]

SendInput.restype = wintypes.UINT


# ============================================================
# CDP
# ============================================================

contador_cdp = 0


def buscar_juego():

    print()
    print(
        "======================================"
    )

    print(
        "[1/8] BUSCANDO MU"
    )

    print(
        "======================================"
    )

    print()


    try:

        respuesta = requests.get(
            CDP_URL,
            timeout=2
        )

        respuesta.raise_for_status()

        pestañas = respuesta.json()

    except Exception as error:

        print(
            "ERROR:",
            error
        )

        return None


    for pestaña in pestañas:

        url = pestaña.get(
            "url",
            ""
        )


        if (
            pestaña.get("type") == "page"
            and
            GAME_DOMAIN.lower()
            in url.lower()
        ):

            print(
                "OK: pestaña encontrada."
            )

            print()

            print(
                "Título:",
                pestaña.get("title")
            )

            print(
                "URL   :",
                url
            )

            print()

            return pestaña


    print(
        "ERROR: no se encontró MU."
    )

    return None


def conectar_cdp(pestaña):

    print(
        "[2/8] CONECTANDO CON CHROME..."
    )


    ws = websocket.create_connection(
        pestaña["webSocketDebuggerUrl"],
        origin="http://127.0.0.1:9222"
    )


    print(
        "OK: conexión establecida."
    )

    return ws


def cdp(
    ws,
    method,
    params=None
):

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


        if (
            respuesta.get("id")
            ==
            contador_cdp
        ):

            return respuesta


def ejecutar_js(
    ws,
    codigo
):

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
# CANVAS
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

            left: rect.left,
            top: rect.top,

            width: rect.width,
            height: rect.height

        };

    })()
    """


    return ejecutar_js(
        ws,
        codigo
    )


# ============================================================
# VENTANA MU
# ============================================================

def buscar_ventana_mu():

    print(
        "[3/8] BUSCANDO VENTANA MU..."
    )


    encontrada = None


    CALLBACK = ctypes.WINFUNCTYPE(
        ctypes.c_bool,
        wintypes.HWND,
        wintypes.LPARAM
    )


    def callback(
        hwnd,
        lparam
    ):

        nonlocal encontrada


        longitud = (
            user32.GetWindowTextLengthW(
                hwnd
            )
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
            or
            "MuMagdalenas" in titulo
        ):

            encontrada = hwnd

            return False


        return True


    user32.EnumWindows(
        CALLBACK(callback),
        0
    )


    if encontrada:

        print(
            "OK: ventana encontrada."
        )

        print(
            "HWND:",
            encontrada
        )

    else:

        print(
            "ERROR: ventana no encontrada."
        )


    print()

    return encontrada


# ============================================================
# PRIMER PLANO
# ============================================================

def poner_al_frente(hwnd):

    print(
        "[4/8] PONIENDO MU EN PRIMER PLANO..."
    )


    if user32.IsIconic(hwnd):

        user32.ShowWindow(
            hwnd,
            SW_RESTORE
        )

        time.sleep(
            0.5
        )

    else:

        user32.ShowWindow(
            hwnd,
            SW_SHOW
        )


    user32.BringWindowToTop(
        hwnd
    )


    resultado = (
        user32.SetForegroundWindow(
            hwnd
        )
    )


    time.sleep(
        0.7
    )


    foreground = (
        user32.GetForegroundWindow()
    )


    print(
        "SetForegroundWindow:",
        bool(resultado)
    )

    print(
        "Foreground HWND:",
        foreground
    )


    if foreground == hwnd:

        print(
            "OK: MU está en primer plano."
        )

    else:

        print(
            "ADVERTENCIA: MU no quedó en primer plano."
        )


    print()


# ============================================================
# RECT
# ============================================================

class RECT(ctypes.Structure):

    _fields_ = [

        ("left", ctypes.c_long),

        ("top", ctypes.c_long),

        ("right", ctypes.c_long),

        ("bottom", ctypes.c_long)

    ]


def obtener_rect(hwnd):

    rect = RECT()


    resultado = user32.GetWindowRect(
        hwnd,
        ctypes.byref(rect)
    )


    if not resultado:

        return None


    return rect


# ============================================================
# MOVER CURSOR
# ============================================================

def mover_mouse(
    x,
    y
):

    return bool(
        user32.SetCursorPos(
            int(x),
            int(y)
        )
    )


# ============================================================
# RMB DOWN
# ============================================================

def rmb_down():

    extra = wintypes.ULONG(0)


    mouse = MOUSEINPUT(

        dx=0,

        dy=0,

        mouseData=0,

        dwFlags=MOUSEEVENTF_RIGHTDOWN,

        time=0,

        dwExtraInfo=ctypes.pointer(
            extra
        )

    )


    entrada = INPUT(

        type=INPUT_MOUSE,

        mi=mouse

    )


    enviados = SendInput(
        1,
        ctypes.byref(entrada),
        ctypes.sizeof(INPUT)
    )


    return enviados == 1


# ============================================================
# RMB UP
# ============================================================

def rmb_up():

    extra = wintypes.ULONG(0)


    mouse = MOUSEINPUT(

        dx=0,

        dy=0,

        mouseData=0,

        dwFlags=MOUSEEVENTF_RIGHTUP,

        time=0,

        dwExtraInfo=ctypes.pointer(
            extra
        )

    )


    entrada = INPUT(

        type=INPUT_MOUSE,

        mi=mouse

    )


    enviados = SendInput(
        1,
        ctypes.byref(entrada),
        ctypes.sizeof(INPUT)
    )


    return enviados == 1


# ============================================================
# ESTADO RMB
# ============================================================

def rmb_presionado():

    estado = (
        user32.GetAsyncKeyState(0x02)
    )


    return bool(
        estado & 0x8000
    )


# ============================================================
# PRUEBA
# ============================================================

def ejecutar_prueba(
    ws,
    hwnd
):

    print(
        "[5/8] OBTENIENDO CANVAS..."
    )


    canvas = obtener_canvas(
        ws
    )


    if canvas is None:

        print(
            "ERROR: no se encontró #canvas."
        )

        return


    print(
        "OK: canvas encontrado."
    )

    print(
        f"Tamaño: "
        f"{canvas['width']:.1f} x "
        f"{canvas['height']:.1f}"
    )

    print()


    # --------------------------------------------------------
    # POSICIÓN DEL CANVAS EN PANTALLA
    # --------------------------------------------------------

    rect = obtener_rect(
        hwnd
    )


    if rect is None:

        print(
            "ERROR obteniendo posición."
        )

        return


    pantalla_x = (
        rect.left
        +
        canvas["left"]
        +
        canvas["width"] / 2
    )


    pantalla_y = (
        rect.top
        +
        canvas["top"]
        +
        canvas["height"] / 2
    )


    pantalla_x = int(
        pantalla_x
    )

    pantalla_y = int(
        pantalla_y
    )


    print(
        "[6/8] PUNTO DE PRUEBA"
    )

    print()

    print(
        f"X = {pantalla_x}"
    )

    print(
        f"Y = {pantalla_y}"
    )

    print()


    # --------------------------------------------------------
    # PREPARACIÓN
    # --------------------------------------------------------

    print(
        "======================================"
    )

    print(
        " PREPARACIÓN"
    )

    print(
        "======================================"
    )

    print()

    print(
        "La prueba utilizará SendInput."
    )

    print(
        "Esto genera una entrada real de mouse"
    )

    print(
        "a nivel del sistema Windows."
    )

    print()

    print(
        "SECUENCIA:"
    )

    print(
        "  1. MU se pondrá al frente."
    )

    print(
        "  2. Cursor al centro del canvas."
    )

    print(
        "  3. RMB DOWN."
    )

    print(
        "  4. Cambias de ventana."
    )

    print(
        "  5. Esperamos 15 segundos."
    )

    print(
        "  6. Vuelves a MU."
    )

    print(
        "  7. COMPRUEBAS EL RMB."
    )

    print(
        "  8. Tú pulsas ENTER."
    )

    print(
        "  9. Recién entonces RMB UP."
    )

    print()

    input(
        "Pulsa ENTER para comenzar..."
    )


    # --------------------------------------------------------
    # MU AL FRENTE
    # --------------------------------------------------------

    poner_al_frente(
        hwnd
    )


    # --------------------------------------------------------
    # MOVER CURSOR
    # --------------------------------------------------------

    print(
        "[7/8] MOVIENDO CURSOR..."
    )


    mover_mouse(
        pantalla_x,
        pantalla_y
    )


    time.sleep(
        0.5
    )


    # --------------------------------------------------------
    # RMB DOWN
    # --------------------------------------------------------

    print()
    print(
        "ENVIANDO RMB DOWN..."
    )


    if not rmb_down():

        print(
            "ERROR enviando RMB DOWN."
        )

        return


    print(
        "OK: RMB DOWN enviado."
    )

    print(
        "Estado Windows:",
        rmb_presionado()
    )

    print()

    print(
        "######################################"
    )

    print(
        "# RMB PRESIONADO"
    )

    print(
        "######################################"
    )

    print()

    print(
        ">>> CAMBIA AHORA A OTRA VENTANA <<<"
    )

    print()

    print(
        "NO pulses el botón derecho."
    )

    print(
        "El programa lo mantiene presionado."
    )

    print()


    # --------------------------------------------------------
    # ESPERA FUERA DE MU
    # --------------------------------------------------------

    for segundo in range(
        TIEMPO_FUERA,
        0,
        -1
    ):

        print(
            f"    Fuera de MU: "
            f"{segundo:02d}s"
            f" | RMB Windows = "
            f"{rmb_presionado()}"
        )


        time.sleep(
            1
        )


    # --------------------------------------------------------
    # AHORA NO LIBERAMOS
    # --------------------------------------------------------

    print()
    print(
        "======================================"
    )

    print(
        " AHORA VUELVE A MU"
    )

    print(
        "======================================"
    )

    print()

    print(
        "IMPORTANTE:"
    )

    print(
        "El RMB TODAVÍA ESTÁ PRESIONADO."
    )

    print()

    print(
        "Vuelve a la ventana de MU."
    )

    print()

    print(
        "Comprueba con calma si el juego"
    )

    print(
        "sigue comportándose como si mantuvieras"
    )

    print(
        "presionado el botón derecho."
    )

    print()

    print(
        "Cuando hayas terminado de comprobarlo,"
    )

    print(
        "vuelve a esta consola."
    )

    print()

    input(
        "Pulsa ENTER para LIBERAR RMB..."
    )


    # --------------------------------------------------------
    # RMB UP
    # --------------------------------------------------------

    print()
    print(
        "[8/8] LIBERANDO RMB..."
    )


    resultado = rmb_up()


    print(
        "RMB UP enviado:",
        resultado
    )


    time.sleep(
        0.2
    )


    print(
        "Estado Windows:",
        rmb_presionado()
    )


    print()
    print(
        "======================================"
    )

    print(
        " DIAGNÓSTICO TERMINADO"
    )

    print(
        "======================================"
    )

    print()


# ============================================================
# MAIN
# ============================================================

def main():

    print()
    print(
        "======================================"
    )

    print(
        " MU MAGDALENAS"
    )

    print(
        " DIAGNÓSTICO SENDINPUT RMB"
    )

    print(
        "======================================"
    )

    print()


    juego = buscar_juego()


    if juego is None:

        return


    try:

        ws = conectar_cdp(
            juego
        )

    except Exception as error:

        print(
            "ERROR:",
            error
        )

        return


    try:

        hwnd = buscar_ventana_mu()


        if hwnd is None:

            return


        poner_al_frente(
            hwnd
        )


        ejecutar_prueba(
            ws,
            hwnd
        )


    except KeyboardInterrupt:

        print()
        print(
            "Programa cancelado."
        )

        # Seguridad.
        try:
            rmb_up()
        except Exception:
            pass


    finally:

        # Seguridad adicional.
        try:
            rmb_up()
        except Exception:
            pass


        try:

            ws.close()

        except Exception:

            pass


    print(
        "Programa terminado."
    )


# ============================================================
# INICIO
# ============================================================

if __name__ == "__main__":

    main()
