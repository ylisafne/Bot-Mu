
import time
import random
import threading
import pyautogui
import keyboard

from collections import deque
from rich.console import Console
from rich.live import Live
from rich.panel import Panel
from rich.table import Table

# =================================
# CONFIGURACION
# =================================

MIN_MINUTOS = 1
MAX_MINUTOS = 5

PAUSA_MIN = 0.5
PAUSA_MAX = 3.0

# =================================
# ESTADO DEL PROGRAMA
# =================================

activo = False
ejecutando = True
bloqueo = threading.Lock()

console = Console()
logs = deque(maxlen=5)

estado = "DESACTIVADO"
tiempo_restante = 0
ciclo_minutos = 0
ultimo_evento = "Esperando activacion"


def registrar(mensaje):
    logs.append(f"[{time.strftime('%H:%M:%S')}] {mensaje}")

import subprocess

def notificar(titulo, mensaje):
    try:
        subprocess.Popen([
            "notify-send",
            "-a", "AUTOCLICKER",
            "-t", "3000",
            titulo,
            mensaje
        ])
    except Exception as e:
        registrar(f"Error al notificar: {e}")

def alternar():
    global activo, estado

    with bloqueo:
        activo = not activo
        estado = "ACTIVO" if activo else "DESACTIVADO"

        if activo:
            registrar("Autoclicker activado con F8")
            titulo = "Autoclicker activado"
            mensaje = "El boton derecho se mantendra presionado por ciclos."
        else:
            registrar("Desactivado con F8")
            titulo = "Autoclicker desactivado"
            mensaje = "El autoclicker se ha detenido."
        notificar(titulo, mensaje)


def salir():
    global ejecutando, activo, estado

    with bloqueo:
        ejecutando = False
        activo = False
        estado = "DETENIDO"
        registrar("Programa finalizado")

    # Liberar el boton derecho al cerrar
    pyautogui.mouseUp(button="right")


def crear_panel():
    tabla = Table.grid(expand=True)
    tabla.add_column()

    with bloqueo:
        estado_actual = estado
        restante = tiempo_restante
        duracion = ciclo_minutos
        evento = ultimo_evento
        mensajes = list(logs)

    color = "green" if estado_actual == "ACTIVO" else "red"

    tabla.add_row(
        f"[bold {color}]Estado: {estado_actual}[/bold {color}]"
    )

    if estado_actual == "ACTIVO":
        tabla.add_row(
            f"Duracion del ciclo: {duracion:.2f} minutos"
        )
        tabla.add_row(
            f"Tiempo restante: {max(0, restante):.1f} segundos"
        )

    tabla.add_row(f"Evento: {evento}")
    tabla.add_row("")
    tabla.add_row("[bold]Ultimos 5 mensajes:[/bold]")

    for linea in mensajes:
        tabla.add_row(linea)

    tabla.add_row("")
    tabla.add_row("[cyan]F8: activar/desactivar[/cyan]")
    tabla.add_row("[yellow]F9: cerrar programa[/yellow]")

    return Panel(
        tabla,
        title="[bold cyan]AUTOCLICKER[/bold cyan]",
        border_style="cyan"
    )


def trabajador():
    global tiempo_restante, ciclo_minutos
    global ultimo_evento, activo

    while True:
        with bloqueo:
            if not ejecutando:
                break

            encendido = activo

        if not encendido:
            time.sleep(0.05)
            continue

        # Elegir una duracion aleatoria
        minutos = random.uniform(MIN_MINUTOS, MAX_MINUTOS)
        duracion = minutos * 60
        inicio = time.monotonic()

        with bloqueo:
            ciclo_minutos = minutos
            registrar(f"Nuevo ciclo: {minutos:.2f} minutos")

        # Mantener presionado el boton derecho
        pyautogui.mouseDown(button="right")

        try:
            while time.monotonic() - inicio < duracion:
                with bloqueo:
                    continuar = ejecutando and activo

                    if continuar:
                        tiempo_restante = duracion - (
                            time.monotonic() - inicio
                        )
                        ultimo_evento = "Boton derecho PRESIONADO"

                if not continuar:
                    break

                time.sleep(0.05)

        finally:
            # Soltar siempre el boton derecho
            pyautogui.mouseUp(button="right")

        with bloqueo:
            continuar = ejecutando and activo

            if not continuar:
                tiempo_restante = 0
                ultimo_evento = "Boton derecho liberado"

        if not continuar:
            continue

        # Pausa aleatoria entre 0.5 y 3 segundos
        pausa = random.uniform(PAUSA_MIN, PAUSA_MAX)

        with bloqueo:
            ultimo_evento = f"Pausa de {pausa:.2f} segundos"
            tiempo_restante = 0
            registrar(
                f"Boton liberado; pausa de {pausa:.2f} segundos"
            )

        fin_pausa = time.monotonic() + pausa

        while time.monotonic() < fin_pausa:
            with bloqueo:
                if not ejecutando or not activo:
                    break

            time.sleep(0.05)


def main():
    keyboard.add_hotkey("f8", alternar)
    keyboard.add_hotkey("f9", salir)

    registrar("Programa iniciado")
    registrar("F8: activar/desactivar | F9: salir")

    hilo = threading.Thread(target=trabajador, daemon=True)
    hilo.start()

    try:
        with Live(
            crear_panel(),
            console=console,
            refresh_per_second=5,
            screen=True
        ) as live:
            while True:
                with bloqueo:
                    if not ejecutando:
                        break

                live.update(crear_panel())
                time.sleep(0.1)

    except KeyboardInterrupt:
        salir()

    finally:
        pyautogui.mouseUp(button="right")
        keyboard.unhook_all()
        console.print("[yellow]Autoclicker cerrado.[/yellow]")


if __name__ == "__main__":
    main()