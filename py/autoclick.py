import pyautogui
import keyboard

activo = False

def alternar():
    global activo
    activo = not activo

    if activo:
        pyautogui.mouseDown(button="right")
        print("Clic derecho PRESIONADO")
    else:
        pyautogui.mouseUp(button="right")
        print("Clic derecho LIBERADO")

keyboard.add_hotkey("F8", alternar)

print("F8: activar/desactivar clic derecho")
print("Ctrl + C: terminar programa")

try:
    while True:
        keyboard.wait("F8")
except KeyboardInterrupt:
    pass
finally:
    pyautogui.mouseUp(button="right")
    keyboard.unhook_all()
    print("\nPrograma terminado. Clic derecho liberado.")