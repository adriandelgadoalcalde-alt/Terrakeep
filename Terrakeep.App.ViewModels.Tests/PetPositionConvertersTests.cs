using System.Globalization;
using System.Windows.Media.Imaging;
using Terrakeep.App.Converters;

namespace Terrakeep.App.ViewModels.Tests;

// Investigacion agente investigador-bug (patron de 2 fases, TASK CONTEXT sesion
// d38ffe35-118f-4719-b326-0ca425888fe7, 26-sep-2026): reporte real del usuario, captura marcada a
// mano, "forma dorada/amarilla redondeada bajo los pies" en la tarjeta hero "Continuar con
// Terrariano" de Inicio.
//
// CAUSA RAIZ CONFIRMADA con datos reales del propio personaje del usuario
// (Documents\My Games\Terraria\Players\Terrariano.plr, EquipmentItems[0]=id 5098 "Eye Bone" ->
// PetAnimationCatalog resuelve proyectil 960, la mascota "Chester" - un cofre viviente
// dorado/marron, redondeado - confirmado contra Terraria.Localization.Content.en-US.Items.json
// real: "ChesterPetItem": "Eye Bone" / "ChesterPetItem": "Summons a living chest"):
// PetBottomAlignMarginConverter.Convert (Converters/PetPositionConverters.cs, linea 63) usa
// `left = 10.0 * canvasScale`, pero la formula real decompilada (Terraria/GameContent/UI/
// Elements/UICharacter.cs, GetPlayerPosition:122-130 + DrawPets:132-149) - re-derivada aqui campo
// a campo desde ese decompilado - da como resultado NETO +20px nativos desde el borde izquierdo
// del lienzo (canvas) del doll, no +10:
//   - GetPlayerPosition centra el HITBOX real (20 de ancho) dentro del cuadro de la UI, y lo
//     retranquea -10 cuando hay mascota: hitboxLeft(retranqueado) = centro_UI - 20.
//   - El sprite VISUAL (40 de ancho, el mismo lienzo nativo 40x56 de PlayerPreviewRenderer) se
//     dibuja SIEMPRE centrado en el centro del hitbox (20 de ancho) que se le pasa a DrawPlayer -
//     con el hitbox ya retranquecido, spriteLeft_final = centro_UI - 30.
//   - DrawPets ancla la mascota en playerPosition.X + 20 = centro_UI - 20 + 20 = centro_UI.
//   - Neto: pet.X - spriteLeft_final = centro_UI - (centro_UI - 30) = 30 nativos DESDE EL BORDE
//     DEL SPRITE YA RETRANQUEADO, que equivale a 20 nativos desde el borde IZQUIERDO DEL PROPIO
//     GRID/LIENZO (el Margin del pet es relativo al Grid, no al sprite ya desplazado) - ver el
//     razonamiento completo linea a linea en el informe de la sesion.
// El comentario de cabecera de PetPositionConverters.cs (lineas 9-41) YA ADMITE la simplificacion
// que causa el hueco ("Terrakeep no modela un hitbox aparte del sprite... el jugador es
// directamente el propio lienzo del doll") pero esa simplificacion pierde el hueco real de 10px
// entre el hitbox (20 ancho) y el sprite visual (40 ancho, centrado sobre el hitbox) - de ahi el
// error de exactamente 10 nativos (10 vs 20).
//
// EVIDENCIA VISUAL real (no solo matematica): reconstruida con los assets REALES de produccion +
// el .plr real del usuario en un arnes aislado de sesion (scratchpad, DiagPetDorado) - con
// left=10 (codigo actual) "Chester" queda oculto casi entero detras del doll, dejando solo un
// borde curvo dorado/marron asomando bajo los pies (EXACTO al reporte); con left=20 (la formula
// re-derivada) "Chester" se ve reconocible al lado de los pies, coherente con el comentario de
// cabecera de UICharacter.cs.
//
// CORROBORACION independiente ya existente en el propio repo: el canario
// Terrakeep.App.Tests/CanarioHomeBannerMascota.cs (HOMEBANNER_SOLO=1, bloque C/D,
// VerificarFormulaRealMascota linea 211) YA calcula `offsetXEsperado = 20.0 * canvasScale +
// petOffsetX` citando el MISMO decompilado - y el propio mensaje del commit que introdujo el bug
// (7058c42b, "Inicio: sustituye el Margin empirico de la mascota por la formula real de
// Terraria") dice explicitamente "la mascota se ancla al borde inferior del doll desplazada 20px
// a la derecha" - o sea, el 10 en el codigo contradice tanto el propio canario visual como el
// propio mensaje de commit de quien lo escribio. Esa es una prueba UI/Automation pesada (lanza
// una MainWindow real, mide con TransformToAncestor, solo se invoca a mano via variable de
// entorno) - este fichero cierra el mismo hueco con una prueba PURA, headless, determinista,
// que corre en cada `dotnet test` normal (el gate real que ya usa este proyecto, ver la cabecera
// de Terrakeep.App.ViewModels.Tests.csproj, "134/134... nunca ninguna linea de la capa App pasaba
// por un test real" - un converter WPF puro es exactamente ese hueco) sin depender de lanzar
// ninguna ventana ni de que exista un personaje real con mascota en este equipo.
//
// Este test se espera EN ROJO ahora mismo (la causa NO se ha corregido en produccion - fase de
// investigacion pura, este agente no toca PetPositionConverters.cs) y debe pasar a verde sin
// tocar este fichero en cuanto `aplicador-fix` corrija la linea 63 real.
public sealed class PetPositionConvertersTests
{
    private static WriteableBitmap MascotaFicticia(int pixelHeight) =>
        new(10, pixelHeight, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);

    [Theory]
    [InlineData("1.3")] // tarjeta pequeña de Inicio (MainWindow.xaml ~1556)
    [InlineData("2.6")] // tarjeta hero "Continuar con X" (MainWindow.xaml ~2685)
    public void Convert_MargenIzquierdo_CoincideConLaFormulaRealDeUICharacterCs_20NativosNo10(string canvasScale)
    {
        var mascota = MascotaFicticia(pixelHeight: 30);
        double scale = double.Parse(canvasScale, CultureInfo.InvariantCulture);

        var margen = (System.Windows.Thickness)PetBottomAlignMarginConverter.Instance.Convert(
            mascota, typeof(System.Windows.Thickness), canvasScale, CultureInfo.InvariantCulture);

        // Formula real re-derivada de UICharacter.cs (GetPlayerPosition/DrawPets, ver el
        // comentario de cabecera completo): 20 nativos * canvasScale, NUNCA 10 - mismo valor
        // exacto que ya exige CanarioHomeBannerMascota.cs (HOMEBANNER_SOLO, linea 211) y que el
        // propio mensaje del commit 7058c42b prometia ("desplazada 20px a la derecha").
        double esperado = 20.0 * scale;
        Assert.Equal(esperado, margen.Left, precision: 3);
    }

    [Theory]
    [InlineData("1.3")]
    [InlineData("2.6")]
    public void Convert_MargenSuperior_AnclaElBordeInferiorDeLaMascotaAlBordeInferiorDelLienzo(string canvasScale)
    {
        // Este SI coincide con la formula real (no es la causa del bug, se deja documentado y
        // cubierto para no dejar el converter entero sin probar) - PlayerPreviewRenderer.Height
        // (56 nativo) * canvasScale menos el alto nativo real del bitmap de la mascota.
        int altoMascota = 30;
        var mascota = MascotaFicticia(altoMascota);
        double scale = double.Parse(canvasScale, CultureInfo.InvariantCulture);

        var margen = (System.Windows.Thickness)PetBottomAlignMarginConverter.Instance.Convert(
            mascota, typeof(System.Windows.Thickness), canvasScale, CultureInfo.InvariantCulture);

        double esperado = 56.0 * scale - altoMascota;
        Assert.Equal(esperado, margen.Top, precision: 3);
    }

    [Fact]
    public void Convert_SinBitmapDeMascota_MargenSuperiorEsCero_PeroElIzquierdoSigueLaFormulaRealDe20()
    {
        // value=null (todavia no hay WriteableBitmap resuelto) - el "top" de anclaje al borde
        // inferior no se puede calcular sin el alto real de la mascota (se queda en 0), pero el
        // "left" NO depende del bitmap en si, solo del canvasScale - misma formula, mismo
        // hallazgo que el test de arriba.
        var margen = (System.Windows.Thickness)PetBottomAlignMarginConverter.Instance.Convert(
            null, typeof(System.Windows.Thickness), "1.3", CultureInfo.InvariantCulture);

        Assert.Equal(0, margen.Top);
        Assert.Equal(20.0 * 1.3, margen.Left, precision: 3);
    }

    [Fact]
    public void Convert_ParametroDeEscalaInvalido_DevuelveMargenCero()
    {
        var margen = (System.Windows.Thickness)PetBottomAlignMarginConverter.Instance.Convert(
            MascotaFicticia(30), typeof(System.Windows.Thickness), "no-numero", CultureInfo.InvariantCulture);
        Assert.Equal(new System.Windows.Thickness(0), margen);
    }
}
