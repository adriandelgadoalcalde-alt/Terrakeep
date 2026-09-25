namespace Terrakeep.Core.Model;

// Wings Encargo1 (25-sep-2026): tabla real de posicion/recorte de la capa BASE de alas para el
// doll estatico de cuerpo completo - transcripcion + derivacion algebraica de
// Terraria.DataStructures.PlayerDrawLayers.DrawPlayer_09_Wings (decompilado real,
// Downloads\Keep\tModLoader-Decompiled\TerrariaVanilla\Terraria\DataStructures\
// PlayerDrawLayers.cs:655-1105 - version 1.4.5.8, la misma que la instalacion de Steam), releida
// linea a linea al escribir este fichero (no copiada de un resumen previo).
//
// ALCANCE DELIBERADO de este Encargo1 (documentado, no oculto): capa base ESTATICA, un unico
// fotograma fijo (fotograma 0, "reposo") por ala, SIN ninguna de las capas/efectos secundarios
// reales que el juego real dibuja ADEMAS del sprite base - ninguna excepcion, para las 51 alas
// vanilla:
//   - Aleteo/animacion real (wingFrame cambia con el vuelo) - solo se muestra el fotograma 0.
//   - Particulas/estelas (llamas del id 22, nube de plumas del id 40, estela arcoiris del 45,
//     polvo del 34/9/29) - nunca se dibujan.
//   - Glow masks/overlays adicionales sobre el sprite base (43 doble-glow, 44 shimmer via
//     RenderTarget2D, 47 glow de huesos, 27/30/32/36/38 glow simple) - nunca se dibujan, solo el
//     DrawData base (colorArmorBody/color9, y aqui SIEMPRE sin tinte - "la armadura real NUNCA se
//     tinta con los colores del personaje", mismo criterio ya establecido en
//     PlayerPreviewRenderer.cs).
//   - Colores/alpha que pulsan con el tiempo (31 Stardust: Color.Lerp con miscCounter; 36
//     Bat/glow con seno de shadowPos; 51 Luna: GetLunaGlowColor con tiempo real) - se dibuja con
//     el color BASE fijo (Color.White, sin tinte), nunca el valor animado.
//   - HALLAZGO REAL nuevo, no anticipado por la investigacion previa (confirmado leyendo
//     Player.cs real, ShouldDrawWingsThatAreAlwaysAnimated(), linea ~30967): las IDs 22/28/34/39/
//     45/48 en el JUEGO REAL solo se dibujan cuando "velocity.Y != 0f" (jugador en el aire) - un
//     personaje de pie/parado (como el doll, siempre en "reposo") NO las dibuja EN ABSOLUTO en el
//     juego real, con lo que replicar el gate al pie de la letra dejaria 6 de las 51 alas
//     invisibles en la vista previa. DELIBERATE DIFFERENCE: aqui se ignora ese gate a proposito y
//     se dibuja SIEMPRE el fotograma de reposo - fiel al PROPOSITO del doll (mostrar que alas
//     llevas puestas), no al gate de animacion del juego real.
//   - Caso especial de Calamity (documentado aqui, no en el juego vanilla): "WingsofRebirth"
//     tiene su PROPIA PlayerDrawLayer en CalamityMod (CalPlayer/DrawLayers/
//     WingsofRebirthLayer.cs) que dibuja una textura EXTRA encima del sprite base normal de
//     alas - fuera de alcance de este Encargo1, solo se dibuja la capa base generica (igual que
//     cualquier otra ala de Calamity, tabla por defecto).
//
// Formula real (fallback generico, PlayerDrawLayers.cs:932-996 - la rama que cubre CUALQUIER
// wingId no listado en los 17 "case" especiales de abajo, incluido cualquier id modded/
// Calamity):
//   vector = Position - screenPosition + (width/2, height - bodyFrame.Height/2) + (0,7)
//   num12 = Y-tweak (0 por defecto), num13 = X-tweak (0 por defecto), num14 = divisor (4 por
//     defecto) - los 6 unicos ids reales con tweak (43/44/5/27/41/12, linea 935-962) tocan
//     num12/num13/num14 sin salir de esta rama "generica" (sin bloque propio).
//   vector18 = vector + (num13 - 9, num12 + 2) * Directions   [Directions = (direction, gravDir)]
//   item = Draw(Wings[id], vector18, origin=(texW/2, texH/num14/2))   [frame 0: sourceRect Y=0]
//
// Constante real K = "Position - screenPosition" en el espacio propio de este renderer: NO es un
// dato que Terrakeep modele aparte (el doll no tiene camara) - se despeja ALGEBRAICAMENTE de la
// MISMA formula base que ya usan Waist/Neck/HandOn/HandOff/Back/Face (DrawAccessory, ver el
// comentario real de PlayerPreviewRenderer.Render/LoadBalloonFrame para la cita completa y la
// derivacion ya validada pixel a pixel en produccion: K=(10,10), con width=Player.defaultWidth=
// 20, height=Player.defaultHeight=42, bodyFrame=40x56=Width/Height de este renderer). Verificado
// aqui de NUEVO, independientemente, con la formula real de DrawPlayer_20_NeckAcc
// (PlayerDrawLayers.cs:2083-2091): despejando K de "posicion - origen = (0,0)" (la condicion real
// que hace que NeckAcc quede alineado 1:1 con el lienzo) da K=(10,10) igual, confirmando el
// mismo valor por una via independiente.
//
// Con K=(10,10): vector = (10,10) + (10, 42-28) + (0,7) = (20,31). vector18 (generico, num12=
// num13=0) = (20,31) + (-9,2)*(1,1) = (11,33) - el AnchorX/AnchorY reales para CUALQUIER wingId
// sin entrada en Overrides (incluidos los que solo cambian de COLOR/alpha en el juego real -
// 6/9/10/11/29/31/32/36/38 - la posicion de esos ids es la generica, solo el tinte cambiaba, y
// aqui se ignora el tinte a proposito, ver el ALCANCE DELIBERADO de arriba).
//
// Los 17 ids con override real (6 con tweak num12/13/14 dentro de la rama generica + 11 con
// bloque propio, "early return", cada uno con su PROPIA formula base - ver el comentario de cada
// entrada de Overrides para la cita linea a linea) dan un AnchorX/AnchorY/Divisor distintos,
// derivados con el MISMO metodo K=(10,10) aplicado a la formula real de cada bloque. Todos los
// "-2" de recorte anti-sangrado que el juego real resta a Width/Height del rectangulo de origen
// (rectangle.Width -= 2; rectangle.Height -= 2, presente en 28/34/39/40/45/48) se ignoran aqui a
// proposito (aproximacion documentada, un margen de 1-2px en el recorte del frame, mismo criterio
// de aproximacion ya aceptado en SliceShieldRow/SliceBalloonFrame0).
public readonly record struct WingFrame(int AnchorX, int AnchorY, int Divisor, int FrameIndex = 0);

public static class WingDrawTable
{
    // Fallback generico real (PlayerDrawLayers.cs:932-934, num12=num13=0/num14=4) - cubre 34 de
    // los 51 ids vanilla reales (23 puramente genericos + 7 que solo cambian de color/alpha aqui
    // ignorado + los que quedan tras los 17 overrides) y CUALQUIER id de Calamity (numeracion
    // propia no compartida, WingSlot siempre null - ver EquipmentAppearanceResolver.ResolveWing,
    // fiel-por-defecto, mismo criterio ya establecido para BodySlot/LegsSlot/etc en esta clase).
    private static readonly WingFrame Default = new(11, 33, 4);

    private static readonly Dictionary<int, WingFrame> Overrides = new()
    {
        // ---- Los 6 ids con tweak num12/num13/num14 dentro de la rama generica (linea 935-962) ----
        // 5 (Butterfly Wings, id real 749): num13=4, num12=0-4=-4 -> anchor=(11+4,33-4).
        [5] = new(15, 29, 4),
        // 12 (Steampunk Wings, id real 948): num13=-1, num12=-1 -> anchor=(11-1,33-1).
        [12] = new(10, 32, 4),
        // 27 (Mothron Wings, id real 2770, con GlowMask[92] extra ignorado aqui): num13=3 ->
        // anchor=(11+3,33).
        [27] = new(14, 33, 4),
        // 41 (Safemans Wings, id real 4746): num13=-1 -> anchor=(11-1,33).
        [41] = new(10, 33, 4),
        // 43 (Grox the Greats Wings, id real 4754, con GlowMask[272] x2 extra ignorado aqui):
        // num13=-5, num12=-7, num14=7 -> anchor=(11-5,33-7), divisor=7.
        [43] = new(6, 26, 7),
        // 44 (Rainbow Wings, id real 4823, shimmer RenderTarget2D extra ignorado aqui): num14=7
        // (sin tweak de posicion) -> anchor=(11,33), divisor=7.
        [44] = new(11, 33, 7),

        // ---- Los 11 ids con bloque propio ("early return", formula base DISTINTA cada uno) ----
        // 22 (Hoverboard, id real 1866, llamas extra ignoradas): vector3 = vector + (-9,26) ->
        // anchor=(20-9,31+26)=(11,57), divisor=7 (Height()/7).
        [22] = new(11, 57, 7),
        // 28 (Bejeweled Valkyrie Wing, id real 3228, Frame(1,4,...)): vec = vector + (0,19) ->
        // anchor=(20,50), divisor=4.
        [28] = new(20, 50, 4),
        // 34 (Jim's Wings, id real 3582, Frame(1,6,...), base = Position+Size/2, no "vector"):
        // vec3 = K + Size/2 - UnitX*direction*4 = (20,31) - (4,0) -> anchor=(16,31), divisor=6.
        [34] = new(16, 31, 6),
        // 39 (Leinfor's Wings, id real 3928, Frame(1,6,...)): vec9 = vector + (-6,-7) ->
        // anchor=(14,24), divisor=6.
        [39] = new(14, 24, 6),
        // 40 (Ghostar's Wings, id real 4730, particulas puras - sin sprite base real
        // distinguible: DELIBERATE DIFFERENCE, se toma UN fotograma representativo, el mismo que
        // el juego real usa para un jugador QUIETO en el suelo, "num7=8" cuando velocity.Y==0f -
        // Frame(1,14,...)): vector14 = vector + (-4,0), posicion final = vector14 +
        // (direction*3,0) = (20-4+3,31) -> anchor=(19,31), divisor=14, frameIndex=8 (el frame
        // real de "reposo en el suelo" de este id concreto, NO el 0 - los otros 16 terminos del
        // bucle real, tiempo/velocidad-dependientes, se ignoran).
        [40] = new(19, 31, 14, 8),
        // 45 (Long Rainbow Trail Wings, id real 4954, estela arcoiris extra ignorada): vec2 =
        // vector + (0,22) -> anchor=(20,53), divisor=6 (Height()/6).
        [45] = new(20, 53, 6),
        // 47 (Chicken Bones Wings, sin item real en esta version de Item.cs - solo alcanzable
        // via wingSlot editado a mano, ver el limite documentado en VanillaAccessorySlotEntry.
        // Wing -, glow pulsante extra ignorado): OffsetsPlayerHeadgear[0]=(0,2) en reposo, Y-=2
        // -> (0,0); vector9=(1,1)+(0,0)=(1,1). vec5 = vector + vector9 - (4,0) = (20+1-4,31+1) ->
        // anchor=(17,32), divisor=11.
        [47] = new(17, 32, 11),
        // 48 (sin item real en esta version de Item.cs, mismo limite que 47/49/50/51, Frame(1,8,
        // ...), base = Position+Size/2): vec7 = K + Size/2 + (4,0) - (4,0) = (20,31) ->
        // anchor=(20,31), divisor=8.
        [48] = new(20, 31, 8),
        // 49 (sin item real en esta version de Item.cs, mismo bloque que 47 sin el glow extra):
        // misma anchor=(17,32), divisor=11.
        [49] = new(17, 32, 11),
        // 50 (sin item real en esta version de Item.cs, overlay de inmunidad duplicado
        // ignorado): vec10 = vector - (4,0) = (16,31) -> anchor=(16,31), divisor=11 (num11=11).
        [50] = new(16, 31, 11),
        // 51 (sin item real en esta version de Item.cs, glow pulsante extra ignorado): vec4 =
        // vector + (0,6) - (4,0) [Directions.Y=gravDir=1, nunca <0 en este renderer sin gravedad
        // invertida, asi que siempre la rama "6", nunca "8"] -> anchor=(20+0-4,31+6)=(16,30) [ojo:
        // base = K+(10,14) = (20,31), no "vector" con el +(0,7) de la rama generica -
        // PlayerDrawLayers.cs:780 usa la MISMA subformula (width/2,height-bodyFrame.Height/2) sin
        // el +(0,7) extra], divisor=8.
        [51] = new(16, 30, 8),
    };

    // Devuelve la posicion/recorte real para cualquier wingId - el valor generico (Default) para
    // cualquier id sin entrada en Overrides, incluidos los IDs 0/negativos (sin alas) y
    // CUALQUIER id de Calamity (WingSlot vanilla siempre null ahi, ver
    // EquipmentAppearanceResolver.ResolveWing) - el llamador (PlayerPreviewRenderer.LoadWingFrame)
    // ya comprueba que WingFile no sea null antes de invocar esto, Resolve() nunca decide si se
    // dibuja o no, solo COMO.
    public static WingFrame Resolve(int wingId) => Overrides.TryGetValue(wingId, out var frame) ? frame : Default;
}
