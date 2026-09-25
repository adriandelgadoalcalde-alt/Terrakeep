#!/usr/bin/env python3
"""ParidadVisual - automatizacion del lado "vanilla" real (ParidadPersonaje Fase0).

Arranca Terraria 1.4.5.8 real (Steam), navega los menus REALES con clics de verdad
(win32api.mouse_event - pywinauto.mouse.click/click_input NO funciona con esta ventana XNA/FNA,
confirmado empiricamente: el juego ignora el evento sintetico de alto nivel, solo responde a
SetCursorPos + mouse_event de bajo nivel) hasta la pantalla "Seleccionar jugador"
(Terraria.GameContent.UI.States.UICharacterSelectMenu, que instancia UICharacter/
UICharacterListItem - Elements/UICharacterListItem.cs:66, Width=59/Height=58/characterScale=1)
y vuelca la ventana entera a PNG con PIL.ImageGrab (funciona sin problemas en modo ventana, a
diferencia de otros juegos del catalogo Keep con superficie DirectX exclusiva).

Uso tipico (ver README.md de esta carpeta para el flujo completo de un caso):

    from capturar_vanilla import SesionTerraria

    with SesionTerraria() as sesion:
        sesion.ir_a_seleccionar_personaje()
        sesion.capturar_ventana_completa("paso1.png")     # <- verificar SIEMPRE con Read antes
                                                            #    de seguir, ver aviso mas abajo
        sesion.crear_personaje_nuevo("ZZCaso1Real")
        sesion.capturar_ventana_completa("paso2.png")     # <- verificar de nuevo
        ruta = sesion.capturar_ventana_completa("evidencia.png")
        recortar_bbox_personaje(str(ruta), 340, 395, 430, 470, "personaje.png")

AVISO REAL (25-sep-2026, ver bitacora.md "ParidadPersonaje Fase0" para el detalle completo):
esta clase esta VERIFICADA paso a paso (cada metodo individual, con captura+inspeccion visual
real entre medias, fue el mecanismo real usado para las evidencias del Caso 1). Encadenar VARIOS
metodos SEGUIDOS sin capturar+revisar entre cada uno NO esta verificado como fiable - un intento
de correrlo asi did fallar (un clic aterrizo en una pantalla de menu distinta a la esperada,
probablemente por variacion real de timing entre transiciones de pantalla). Patron recomendado
para cualquier caso nuevo: llamar a UN metodo, capturar con capturar_ventana_completa, e
inspeccionar la imagen (con la herramienta Read del agente, o a ojo) ANTES de decidir las
coordenadas del siguiente clic - exactamente como se hizo aqui, nunca una cadena ciega larga.

Requiere (ya confirmados presentes en esta maquina, 25-sep-2026): pywinauto 0.6.9, pillow,
pywin32 (win32api/win32gui). Instalacion no invasiva ya hecha, ver bitacora.md.
"""
from __future__ import annotations

import json
import shutil
import subprocess
import time
from pathlib import Path
from typing import Optional

import win32api
import win32con
import win32gui
from PIL import Image, ImageGrab
from pywinauto import Desktop

TERRARIA_EXE = r"C:\Program Files (x86)\Steam\steamapps\common\Terraria\Terraria.exe"
TERRARIA_CWD = r"C:\Program Files (x86)\Steam\steamapps\common\Terraria"
CONFIG_PATH = Path(r"C:\Users\adrian\Documents\My Games\Terraria\config.json")
PLAYERS_DIR = Path(r"C:\Users\adrian\Documents\My Games\Terraria\Players")

# Resolucion/UIScale fijas para que las coordenadas de clic y el tamano en pixeles del doll
# (UICharacter, 59x58 "unidades" reales) sean deterministas entre corridas. UIScale=1.0 hace que
# esas unidades sean pixeles 1:1 dentro del backbuffer (antes de cualquier escalado de ventana
# de Windows - ver README.md, seccion "limites conocidos", para el caso de que el monitor tenga
# escalado de PC != 100%).
RESOLUCION_VENTANA = (1280, 720)
UI_SCALE = 1.0


class SesionTerraria:
    """Backup/restore de config.json real del usuario + lanzar/cerrar Terraria + helpers de
    clic y captura. SIEMPRE usar como contexto (`with SesionTerraria() as s:`) para garantizar
    que el config.json del usuario se restaura y el proceso se cierra aunque algo falle a medio
    camino - este script toca el guardado REAL del usuario (Documents\\My Games\\Terraria), no
    una copia de pruebas."""

    def __init__(self, resolucion: tuple[int, int] = RESOLUCION_VENTANA, ui_scale: float = UI_SCALE):
        self.resolucion = resolucion
        self.ui_scale = ui_scale
        self._config_backup: Optional[str] = None
        self._proceso: Optional[subprocess.Popen] = None
        self.ventana = None

    # ---- ciclo de vida -----------------------------------------------------------------
    def __enter__(self) -> "SesionTerraria":
        if win32gui.FindWindow(None, None) is None:
            pass  # no-op, solo para dejar claro que no dependemos de nada previo
        self._backup_config()
        self._parchear_config()
        self._lanzar()
        self._esperar_ventana()
        return self

    def __exit__(self, exc_type, exc, tb) -> None:
        self._cerrar()
        self._restaurar_config()

    def _backup_config(self) -> None:
        self._config_backup = CONFIG_PATH.read_text(encoding="utf-8")

    def _parchear_config(self) -> None:
        datos = json.loads(self._config_backup)
        datos["Fullscreen"] = False
        datos["WindowMaximized"] = False
        datos["WindowBorderless"] = False
        datos["DisplayWidth"] = self.resolucion[0]
        datos["DisplayHeight"] = self.resolucion[1]
        datos["UIScale"] = self.ui_scale
        CONFIG_PATH.write_text(json.dumps(datos, indent=2), encoding="utf-8")

    def _restaurar_config(self) -> None:
        if self._config_backup is not None:
            CONFIG_PATH.write_text(self._config_backup, encoding="utf-8")

    def _lanzar(self) -> None:
        self._proceso = subprocess.Popen([TERRARIA_EXE], cwd=TERRARIA_CWD)

    def _esperar_ventana(self, timeout_s: float = 30.0) -> None:
        inicio = time.time()
        while time.time() - inicio < timeout_s:
            for w in Desktop(backend="win32").windows():
                titulo = w.window_text()
                if "terraria" in titulo.lower() and titulo != "GDI+ Window (Terraria.exe)":
                    self.ventana = w
                    time.sleep(12.0)  # carga de assets/splash - medido empiricamente, ver bitacora
                    return
            time.sleep(0.5)
        raise TimeoutError("Terraria no abrio ninguna ventana real a tiempo")

    def _cerrar(self) -> None:
        if self._proceso is not None:
            try:
                self._proceso.terminate()
            except Exception:
                pass
        subprocess.run(["taskkill", "/IM", "Terraria.exe", "/F"], capture_output=True)

    # ---- entrada de bajo nivel ----------------------------------------------------------
    def _rect(self):
        return self.ventana.rectangle()

    def _foreground(self) -> None:
        """SetForegroundWindow puro puede fallar con pywintypes.error "No error message is
        available" si Windows aplica el bloqueo de foco de primer plano (el proceso llamador no
        acaba de recibir input real del usuario - confirmado 25-sep-2026, pasa justo tras lanzar
        el juego con subprocess.Popen, no pasaba en las pruebas manuales porque cada invocacion
        de consola SI contaba como "input reciente"). Workaround real: un toque de ALT sintetico
        via keybd_event libera ese bloqueo (truco conocido de la Win32 API), con
        BringWindowToTop+ShowWindow como red de seguridad adicional antes de reintentar."""
        try:
            win32gui.SetForegroundWindow(self.ventana.handle)
            return
        except Exception:
            pass
        win32api.keybd_event(win32con.VK_MENU, 0, 0, 0)
        win32api.keybd_event(win32con.VK_MENU, 0, win32con.KEYEVENTF_KEYUP, 0)
        win32gui.BringWindowToTop(self.ventana.handle)
        win32gui.ShowWindow(self.ventana.handle, win32con.SW_SHOW)
        win32gui.SetForegroundWindow(self.ventana.handle)

    def click(self, rx: int, ry: int, espera_s: float = 1.0) -> None:
        """Clic en coordenadas RELATIVAS a la ventana (mismo sistema que un PNG capturado con
        capturar_ventana_completa). Usa SetCursorPos + mouse_event de bajo nivel a proposito -
        pywinauto.mouse.click/w.click_input NO son recogidos por este juego (confirmado 25-sep-2026,
        el clic se registra como hover pero nunca como "press" real)."""
        self._foreground()
        time.sleep(0.3)
        r = self._rect()
        x, y = r.left + rx, r.top + ry
        win32api.SetCursorPos((x, y))
        time.sleep(0.15)
        win32api.mouse_event(win32con.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0)
        time.sleep(0.08)
        win32api.mouse_event(win32con.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0)
        time.sleep(espera_s)

    def escribir(self, texto: str) -> None:
        from pywinauto.keyboard import send_keys
        send_keys(texto, pause=0.05)

    def scroll(self, rx: int, ry: int, pasos: int, hacia_abajo: bool = True, espera_s: float = 1.0) -> None:
        self._foreground()
        time.sleep(0.2)
        r = self._rect()
        win32api.SetCursorPos((r.left + rx, r.top + ry))
        time.sleep(0.15)
        delta = -120 if hacia_abajo else 120
        for _ in range(pasos):
            win32api.mouse_event(win32con.MOUSEEVENTF_WHEEL, 0, 0, delta, 0)
            time.sleep(0.03)
        time.sleep(espera_s)

    # ---- captura --------------------------------------------------------------------------
    def capturar_ventana_completa(self, salida_png: str) -> Path:
        r = self._rect()
        img = ImageGrab.grab(bbox=(r.left, r.top, r.right, r.bottom))
        ruta = Path(salida_png)
        ruta.parent.mkdir(parents=True, exist_ok=True)
        img.save(ruta)
        return ruta

    # ---- flujos de alto nivel (menu real) --------------------------------------------------
    def ir_a_seleccionar_personaje(self) -> None:
        """Menu principal -> "Un jugador". Coordenadas medidas a 1280x720/UIScale=1.0 (fila
        vertical de opciones centrada, "Un jugador" es la 1a - ver _debug_menu1.png del 25-sep-2026
        para la captura real usada al calibrar esto)."""
        self.click(655, 320, 0.5)  # hover
        self.click(655, 320, 2.0)  # clic real (el primer clic tras el foco solo deja hover)

    def crear_personaje_nuevo(self, nombre: str, dificultad_ya_clasico: bool = True) -> None:
        """Desde "Seleccionar jugador": clic en "Nuevo", escribe el nombre, Enviar, Crear (deja
        "Clasico" ya preseleccionado por defecto - vale para comparacion de apariencia, la
        dificultad no afecta al render). Produce un .plr REAL, 100% valido para el juego (evita
        a proposito el escritor .plr de Terrakeep para el lado vanilla - ver README.md, seccion
        "limite real conocido: ParidadVisual.exe preparar-caso"). Encadena 4 clics seguidos SIN
        capturar entre medias - ver el AVISO REAL de cabecera del modulo: funciono de forma
        fiable en la sesion real que genero la evidencia del Caso 1 pero un reintento posterior
        (mismo dia) SI fallo a mitad. Tras llamarlo, capturar SIEMPRE y confirmar visualmente que
        de verdad se llego a "Seleccionar jugador" con el personaje nuevo en la lista antes de
        continuar - si algo fallo, este metodo puede haber dejado el juego en cualquier otra
        pantalla del menu."""
        self.click(822, 722, 1.5)       # "Nuevo"
        self.click(655, 420, 0.5)       # campo Nombre
        self.escribir(nombre)
        time.sleep(0.3)
        self.click(655, 418, 1.5)       # "Enviar"
        self.click(786, 606, 2.5)       # "Crear"

    def volver_atras(self) -> None:
        self.click(488, 722, 1.5)


def recortar_bbox_personaje(imagen_png: str, x0: int, y0: int, x1: int, y1: int,
                             salida_png: str, color_fondo_rgb: tuple[int, int, int] = (44, 56, 103)) -> Path:
    """Recorta la region [x0,y0,x1,y1) (coordenadas de ventana, las mismas que click()) de una
    captura completa y ajusta el bbox exacto del personaje descartando el fondo del panel
    (heuristica de color - el panel es un degradado azul oscuro, el personaje no lo es, ver
    bitacora.md "ParidadPersonaje Fase0" para el detalle exacto y sus limites conocidos)."""
    img = Image.open(imagen_png).convert("RGB")
    recorte = img.crop((x0, y0, x1, y1))
    px = recorte.load()
    w, h = recorte.size

    def es_fondo(rgb):
        r, g, b = rgb
        br, bg, bb = color_fondo_rgb
        return b > r + 15 and b > g + 5 and r < 140

    minx, miny, maxx, maxy = w, h, 0, 0
    for y in range(h):
        for x in range(w):
            if not es_fondo(px[x, y]):
                minx, maxx = min(minx, x), max(maxx, x)
                miny, maxy = min(miny, y), max(maxy, y)
    if minx > maxx or miny > maxy:
        raise ValueError("No se encontro ningun pixel de personaje en el recorte dado")
    final = recorte.crop((minx, miny, maxx + 1, maxy + 1))
    ruta = Path(salida_png)
    ruta.parent.mkdir(parents=True, exist_ok=True)
    final.save(ruta)
    return ruta
