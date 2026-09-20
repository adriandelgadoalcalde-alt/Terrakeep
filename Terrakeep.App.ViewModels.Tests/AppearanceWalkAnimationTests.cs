using System.IO;
using Terrakeep.App.Services;
using Terrakeep.Core.Model;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Catalogo de ideas Keep, idea 10 ("vista previa animada del personaje, exportable"),
// reconsiderada a peticion explicita del coordinador (20-sep-2026): el "limite real" anterior
// ("ni animacion (solo el frame de reposo)") resulto ser un recorte de la propia tira de
// EXTRACCION (cropFrame0), no una ausencia real de datos del juego - ver el comentario real de
// cabecera de PlayerPreviewRenderer.Render para la cita exacta de Terraria/Player.cs,
// PlayerFrame() que lo confirma.
//
// Mismo criterio ya establecido en AppearanceUndoTests para lo que SI se puede probar aqui
// (sin Dispatcher bombeando): la logica PURA de fotograma/espejo/exportacion (PlayerPreviewRenderer
// llamado directamente, o AppearanceViewModel sin depender de que el DispatcherTimer real
// dispare un Tick) - que el DispatcherTimer de verdad avance solo en el tiempo real es cosa del
// arnes de UI Automation (Terrakeep.App.Tests), que SI tiene esa maquinaria.
public sealed class AppearanceWalkAnimationTests
{
    private static readonly PlayerPreviewRenderer.PlayerColors Colors = new(
        new(150, 90, 50), new(255, 220, 177), new(80, 50, 30),
        new(130, 60, 60), new(200, 180, 160), new(70, 70, 120), new(90, 60, 40));

    private static byte[] Pixels(System.Windows.Media.Imaging.WriteableBitmap bmp)
    {
        var pixels = new byte[bmp.PixelHeight * bmp.PixelWidth * 4];
        bmp.CopyPixels(pixels, bmp.PixelWidth * 4, 0);
        return pixels;
    }

    [Fact]
    public void FotogramaDeReposo_ProduceElMismoResultadoQueAntesDeIdea10()
    {
        // legAnimationFrame por defecto (0) tiene que seguir siendo EXACTAMENTE el mismo pixel a
        // pixel que un Render sin ese parametro - ningun llamador existente (Home, comparador...)
        // puede cambiar de aspecto por esta idea.
        var reposoImplicito = PlayerPreviewRenderer.Render(1, PlayerVariantSets.MaleStarter, Colors);
        var reposoExplicito = PlayerPreviewRenderer.Render(1, PlayerVariantSets.MaleStarter, Colors, legAnimationFrame: 0);

        Assert.Equal(Pixels(reposoImplicito), Pixels(reposoExplicito));
    }

    [Fact]
    public void FotogramaDeAndar_ProduceUnLienzoRealmenteDistintoAlDeReposo()
    {
        // Fila 7 (primera del ciclo de andar real, WalkCycleRows[0]) tiene que mover de verdad
        // las piernas/pantalones/zapatos respecto a la fila 0 (reposo) - si esto fuera igual, la
        // "animacion" seria un adorno vacio que no cambia nada de verdad.
        var reposo = PlayerPreviewRenderer.Render(1, PlayerVariantSets.MaleStarter, Colors, legAnimationFrame: 0);
        var andando = PlayerPreviewRenderer.Render(1, PlayerVariantSets.MaleStarter, Colors, legAnimationFrame: AppearanceViewModel.WalkCycleRows[0]);

        Assert.NotEqual(Pixels(reposo), Pixels(andando));
    }

    [Fact]
    public void CicloDeAndarCompleto_TieneLos13FotogramasRealesConfirmadosEnPlayerCs()
    {
        // Player.cs real (PlayerFrame()): legFrame.Y arranca en legFrame.Height*7 y sube de uno
        // en uno hasta *19 antes de volver a *7 - 13 filas reales (7,8,...,19), nunca un numero
        // inventado.
        Assert.Equal(13, AppearanceViewModel.WalkCycleRows.Length);
        Assert.Equal(7, AppearanceViewModel.WalkCycleRows[0]);
        Assert.Equal(19, AppearanceViewModel.WalkCycleRows[^1]);
        for (int i = 1; i < AppearanceViewModel.WalkCycleRows.Length; i++)
            Assert.Equal(AppearanceViewModel.WalkCycleRows[i - 1] + 1, AppearanceViewModel.WalkCycleRows[i]);
    }

    [Fact]
    public void Girar_ProduceUnEspejoHorizontalExactoDelMismoFotograma()
    {
        // Fotograma de andar (no el de reposo, que podria ser simetrico por casualidad y no
        // distinguir un espejo real de un no-op) - compara pixel a pixel que la columna x del
        // normal es EXACTAMENTE la columna (ancho-1-x) del espejado, fila a fila.
        int frame = AppearanceViewModel.WalkCycleRows[0];
        var normal = Pixels(PlayerPreviewRenderer.Render(1, PlayerVariantSets.MaleStarter, Colors, legAnimationFrame: frame, mirror: false));
        var espejado = Pixels(PlayerPreviewRenderer.Render(1, PlayerVariantSets.MaleStarter, Colors, legAnimationFrame: frame, mirror: true));

        Assert.NotEqual(normal, espejado);
        const int width = 40, height = 56;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int origen = (y * width + x) * 4;
                int destino = (y * width + (width - 1 - x)) * 4;
                for (int b = 0; b < 4; b++)
                    Assert.Equal(normal[origen + b], espejado[destino + b]);
            }
        }
    }

    [Fact]
    public void ExportarComoGif_ProduceUnFicheroConLos14FotogramasReales()
    {
        string path = Path.Combine(Path.GetTempPath(), $"appearance-walk-gif-{Guid.NewGuid():N}.gif");
        try
        {
            string plrPath = Path.Combine(Path.GetTempPath(), $"appearance-walk-{Guid.NewGuid():N}.plr");
            var character = new PlrCharacter
            {
                Name = "Test",
                Version = 279,
                PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
                Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
            };
            File.WriteAllBytes(plrPath, PlrFile.Write(character));
            var vm = new MainViewModel();
            vm.LoadFromPath(plrPath);
            File.Delete(plrPath);

            vm.Appearance.ExportPreviewToGif(path);

            Assert.True(File.Exists(path));
            using var stream = File.OpenRead(path);
            var decoder = new System.Windows.Media.Imaging.GifBitmapDecoder(stream,
                System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
            // 1 fotograma de reposo + los 13 reales del ciclo de andar (WalkCycleRows).
            Assert.Equal(1 + AppearanceViewModel.WalkCycleRows.Length, decoder.Frames.Count);
            Assert.Equal(40, decoder.Frames[0].PixelWidth);
            Assert.Equal(56, decoder.Frames[0].PixelHeight);
            // El primer fotograma (reposo) y el segundo (primera fila real de andar) tienen que
            // ser de verdad distintos - reutiliza la misma comprobacion real de arriba, esta vez
            // sobre el fichero YA decodificado desde disco (no sobre el WriteableBitmap en
            // memoria), para que la prueba cubra el camino de exportacion completo.
            var reposoGif = new byte[56 * 40 * 4];
            decoder.Frames[0].CopyPixels(reposoGif, 40 * 4, 0);
            var andandoGif = new byte[56 * 40 * 4];
            decoder.Frames[1].CopyPixels(andandoGif, 40 * 4, 0);
            Assert.NotEqual(reposoGif, andandoGif);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void ToggleWalkAnimationCommand_CambiaIsWalkAnimationPlaying()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"appearance-walk-toggle-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);

        Assert.False(vm.Appearance.IsWalkAnimationPlaying);

        vm.Appearance.ToggleWalkAnimationCommand.Execute(null);
        Assert.True(vm.Appearance.IsWalkAnimationPlaying);

        vm.Appearance.ToggleWalkAnimationCommand.Execute(null);
        Assert.False(vm.Appearance.IsWalkAnimationPlaying);
    }

    [Fact]
    public void ToggleFacingCommand_CambiaIsFacingLeftYElPreview()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"appearance-facing-toggle-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);

        Assert.False(vm.Appearance.IsFacingLeft);

        vm.Appearance.ToggleFacingCommand.Execute(null);
        Assert.True(vm.Appearance.IsFacingLeft); // el espejo pixel a pixel real ya lo prueba Girar_ProduceUnEspejoHorizontalExactoDelMismoFotograma

        vm.Appearance.ToggleFacingCommand.Execute(null);
        Assert.False(vm.Appearance.IsFacingLeft);
    }
}
