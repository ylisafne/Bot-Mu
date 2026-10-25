
@echo off
setlocal
cd /d "%~dp0"
title Autoclicker

echo ========================================
echo       INICIANDO AUTOCLICKER
echo ========================================
echo.

if not exist ".venv\Scripts\python.exe" (
    echo ERROR: No se encuentra el entorno virtual:
    echo %CD%\.venv\Scripts\python.exe
    goto error
)

echo [1/3] Entorno virtual encontrado.
echo.

echo [2/3] Verificando dependencias...
".venv\Scripts\python.exe" -m pip install pyautogui keyboard rich

if errorlevel 1 (
    echo.
    echo ERROR: No se pudieron instalar las dependencias.
    goto error
)

echo.
echo [3/3] Iniciando autoclicker...
echo Pulsa F8 para activar/desactivar.
echo Pulsa F9 para cerrar el programa.
echo.

".venv\Scripts\python.exe" -u autoclick.py

set "CODIGO=%ERRORLEVEL%"
echo.
echo El programa ha terminado con codigo: %CODIGO%
goto fin

:error
echo.
echo Se ha producido un error. Revisa los mensajes anteriores.

:fin
echo.
echo Pulsa una tecla para cerrar esta ventana.
pause >nul
endlocal