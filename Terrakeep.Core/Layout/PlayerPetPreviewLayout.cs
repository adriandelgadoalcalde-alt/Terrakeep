using Terrakeep.Core.Data;

namespace Terrakeep.Core.Layout;

// GapAnalysis ParidadPersonaje (26-sep-2026, requirement 480a9bdd-6d5f-4fa6-935d-46f895e97514) -
// arquitecto-keep investigo y diseño esta clase por completo antes de este arreglo (patron de 2
// fases: este fichero es la SEGUNDA fase, aplicacion directa del diseño ya verificado, no una
// reinvestigacion). Motor PURO de geometria "personaje+mascota" para la vista previa de
// Terrakeep (tarjetas de Inicio, banner hero "Continuar con X") - SIN ningun tipo de WPF,
// testeable con xUnit sin levantar ninguna ventana (por eso vive en Terrakeep.Core, no en
// Terrakeep.App).
//
// CAUSA RAIZ real del bug "Chester parece un suelo/plataforma bajo los pies" (reabierto por el
// usuario tras el arreglo anterior de PetPositionConverters.cs, PortSeleccion Encargo3/
// correccion 26-sep-2026): la formula de POSICION relativa mascota-vs-doll YA era correcta
// (`left = 20.0 * canvasScale` en PetPositionConverters.cs no tenia ningun error aritmetico) - el
// problema real es de CONTENEDOR: el "lienzo" que aloja el doll (40x56 nativo, el mismo
// PlayerPreviewRenderer.Width/Height) nunca reservo el colchon de espacio vacio que el propio
// juego SI tiene alrededor del jugador (su viewport real de UICharacter es 59x58, mucho mas
// ancho que el sprite de 40) para que mascotas grandes sobresalgan sin recortarse contra el
// borde del propio Grid/tarjeta. Con mascotas pequeñas (Chester, 46px de ancho nativo) el hueco
// disponible bastaba; con las mas grandes del catalogo (ver PlayerPetPreviewCatalogWidthTests.cs
// en Terrakeep.App.ViewModels.Tests, medicion real de las 63 mascotas) no.
//
// Constantes reales, citadas del decompilado (TerrariaVanilla, nunca inventadas):
//   ViewportWidth=59f/ViewportHeight=58f      -> Terraria/GameContent/UI/Elements/UICharacter.cs:43-44
//   HitboxWidth=20f/HitboxHeight=42f          -> Terraria/Player.cs:56699-56700 (constructor: width=20;height=42;)
//   SpriteFrameWidth=40f/SpriteFrameHeight=56f-> Terraria/Player.cs:56702-56703 (bodyFrame.Width=40;.Height=56;)
//   SpriteVerticalFudge=4f                    -> Terraria/DataStructures/PlayerDrawLayers.cs:1991/1187 ("+4f" en la
//                                                 formula real de dibujo del torso/piel: "...height - bodyFrame.Height + 4f")
//   PetAnchorOffsetX=20f                      -> Terraria/GameContent/UI/Elements/UICharacter.cs:139 (DrawPets,
//                                                 "new Vector2(20f, 0f)")
//
// Todo en unidades NATIVAS de Terraria (los mismos numeros que el decompilado real) - el
// canvasScale de Terrakeep (1.3 en la tarjeta pequeña de Inicio, 2.6 en el banner hero) NUNCA
// entra en esta clase: se aplica UNA sola vez fuera, sobre el resultado YA COMPUESTO
// (PlayerPetPreviewControl), nunca dentro de estos calculos - asi el propio motor es 100%
// independiente de donde se use, y el caso oracle de abajo (Chester) se puede verificar sin
// ningun factor de escala de por medio.
public static class PlayerPetPreviewLayout
{
    public const double ViewportWidth = 59.0;
    public const double ViewportHeight = 58.0;
    public const double HitboxWidth = 20.0;
    public const double HitboxHeight = 42.0;
    public const double SpriteFrameWidth = 40.0;
    public const double SpriteFrameHeight = 56.0;
    public const double SpriteVerticalFudge = 4.0;
    public const double PetAnchorOffsetX = 20.0;

    // Ancho de reserva REAL, permanente, de la columna del preview (jugador+mascota) - decision
    // final del usuario (26-sep-2026, tras una ronda de refinamiento: primero "cubrir el 90%
    // real, resto contra hueco vacio", despues "expandir solo en hover", y finalmente "mas
    // simple: agrandar la tarjeta de forma PERMANENTE lo bastante para que quepan TODAS las
    // mascotas del catalogo sin recortar ninguna, igual en reposo y en hover, sin estados
    // especiales"). PEOR CASO REAL medido en PlayerPetPreviewCatalogWidthTests.cs (Terrakeep.
    // App.ViewModels.Tests, sobre las 63 mascotas reales de Assets/pet_animations.json + el
    // bounding box alpha-visible real de cada Assets/pets/*.png, no el rectangulo de fotograma
    // crudo con relleno transparente): item 4816 (Frog Leg, proyectil 900 real - ver Terraria/
    // ID/ProjectileID.cs) da un CompositeBounds nativo de 90.0 unidades exactas de ancho (ver esa
    // prueba para el desglose completo, incluido el percentil 90 real = 79.6, documentado para
    // el historial aunque la decision final del usuario use el maximo, no el percentil). +2.0 de
    // margen de seguridad (no arbitrario - la propia tolerancia de redondeo a sub-pixel de WPF al
    // multiplicar por un canvasScale no entero, 1.3/2.6), nunca un numero redondeado a ojo.
    public const double ReserveColumnWidthNative = 92.0;

    public static double ReserveColumnWidth(double canvasScale) => ReserveColumnWidthNative * canvasScale;

    // GetPlayerPosition real (UICharacter.cs:122-130): centra el HITBOX (20 de ancho/42 de alto,
    // no el sprite visual de 40x56) dentro del viewport (59x58), y retranquea -10 en X cuando hay
    // mascota equipada ("if (_petProjectiles.Length != 0) result.X -= 10f;") - NUNCA en Y.
    public static PointD PlayerHitboxOrigin(bool hasPet)
    {
        double x = ViewportWidth * 0.5 - HitboxWidth * 0.5;
        double y = ViewportHeight * 0.5 - HitboxHeight * 0.5;
        if (hasPet) x -= 10.0;
        return new PointD(x, y);
    }

    // Formula real de dibujo del sprite (PlayerDrawLayers.cs:1991/1187, DrawPlayer_*_Skin*):
    // bordeIzq = hitboxOrigin.X - bodyFrame.Width/2 + player.width/2
    // bordeSup = hitboxOrigin.Y + player.height - bodyFrame.Height + 4f (SpriteVerticalFudge)
    public static RectD PlayerSpriteBounds(PointD hitboxOrigin)
    {
        double left = hitboxOrigin.X - SpriteFrameWidth * 0.5 + HitboxWidth * 0.5;
        double top = hitboxOrigin.Y + HitboxHeight - SpriteFrameHeight + SpriteVerticalFudge;
        return new RectD(left, top, SpriteFrameWidth, SpriteFrameHeight);
    }

    // DrawPets real (UICharacter.cs:132-149): ancla la mascota al borde INFERIOR del jugador
    // (playerPosition + (0, player.height)) desplazada PetAnchorOffsetX a la derecha, menos su
    // propia altura de fotograma (para que su borde INFERIOR, no el superior, quede pegado al
    // borde inferior del jugador) - mas el offset ADICIONAL propio de cada mascota
    // (SettingsForCharacterPreview.ApplyTo real: "proj.position += Offset",
    // DataStructures/SettingsForCharacterPreview.cs:64).
    public static RectD PetBounds(PointD hitboxOrigin, double offsetX, double offsetY, double petFrameWidth, double petFrameHeight)
    {
        double baseX = hitboxOrigin.X + PetAnchorOffsetX;
        double baseY = hitboxOrigin.Y + HitboxHeight - petFrameHeight;
        return new RectD(baseX + offsetX, baseY + offsetY, petFrameWidth, petFrameHeight);
    }

    // Sobrecarga de comodidad para medir directamente sobre una entrada real del catalogo
    // (PetAnimationEntry.OffsetX/OffsetY) - usada por PlayerPetPreviewCatalogWidthTests.cs para
    // medir las 63 mascotas reales sin duplicar la extraccion de esos dos campos.
    public static RectD PetBounds(PointD hitboxOrigin, PetAnimationEntry entry, double petFrameWidth, double petFrameHeight) =>
        PetBounds(hitboxOrigin, entry.OffsetX, entry.OffsetY, petFrameWidth, petFrameHeight);

    // Bounding box minimo real que contiene jugador+mascota - el ancho/alto de ESTE rectangulo
    // (no una aproximacion) es lo que la columna del preview tiene que reservar de verdad para no
    // recortar nada, ver ReserveColumnWidthNative arriba.
    public static RectD CompositeBounds(RectD playerBounds, RectD? petBounds)
    {
        if (petBounds is not { } pet) return playerBounds;
        double left = System.Math.Min(playerBounds.Left, pet.Left);
        double top = System.Math.Min(playerBounds.Top, pet.Top);
        double right = System.Math.Max(playerBounds.Right, pet.Right);
        double bottom = System.Math.Max(playerBounds.Bottom, pet.Bottom);
        return new RectD(left, top, right - left, bottom - top);
    }
}

// Sin ningun tipo de WPF (Terrakeep.Core compila tambien para net8.0, para el mod de
// tModLoader - ver la cabecera de Terrakeep.Core.csproj - System.Windows.Point/Rect no estarian
// disponibles ahi) - estructuras propias, minimas, solo con lo que este motor necesita.
public readonly record struct PointD(double X, double Y);

public readonly record struct RectD(double X, double Y, double Width, double Height)
{
    public double Left => X;
    public double Top => Y;
    public double Right => X + Width;
    public double Bottom => Y + Height;
}
