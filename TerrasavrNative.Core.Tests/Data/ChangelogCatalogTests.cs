using TerrasavrNative.Core.Data;
using Xunit;

namespace TerrasavrNative.Core.Tests.Data;

// Prueba de humo contra Assets/changelog.json real (contenido real derivado de bitacora.md +
// el historial de commits, ver la Fase 7 del rework en bitacora.md) - se salta sola si la
// carpeta no existe, mismo patron que el resto de *RealFileTests del proyecto.
public class ChangelogCatalogTests
{
    private const string AssetsDir =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets";

    [Fact]
    public void RealFile_HasEntriesInDescendingOrderWithNoEmptyFields()
    {
        string path = Path.Combine(AssetsDir, "changelog.json");
        if (!File.Exists(path)) return;

        var catalog = ChangelogCatalog.LoadFromFile(path);

        Assert.NotEmpty(catalog.Entries);
        foreach (var entry in catalog.Entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Version));
            Assert.False(string.IsNullOrWhiteSpace(entry.Summary));
            Assert.True(entry.Added.Count > 0 || entry.Fixed.Count > 0);
        }

        // Mas reciente primero (mismo orden que whats_new.json real).
        var versions = catalog.Entries.Select(e => new Version(e.Version)).ToList();
        var sorted = versions.OrderByDescending(v => v).ToList();
        Assert.Equal(sorted, versions);
    }
}
