using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terrakeep.Core.Data;

// Animacion real de la mascota equipada en la vista previa de Inicio (hover) - pedido explicito
// del usuario (21-sep-2026), comparando en vivo con Terraria vanilla: "la mascota sale como
// sprite estatico, en vanilla se anima de verdad mientras el raton esta encima".
//
// Fuente real (decompilado, sin inventar nada):
//   - Terraria/GameContent/UI/Elements/UICharacter.cs (55-66, DrawPets): cada tick real llama
//     "ProjectileID.Sets.CharacterPreviewAnimations[projectile.type].ApplyTo(projectile,
//     _animated)" - _animated=true durante el hover (walking=true en la firma real de
//     SettingsForCharacterPreview.ApplyTo).
//   - Terraria/ID/ProjectileID.cs (34-37, CharacterPreviewAnimations): tabla real por TIPO de
//     proyectil, "SimpleLoop(startFrame, frameCount, delayPerFrame).WhenSelected(...)" - el
//     ".WhenSelected(...)" (cuando existe) SOBREESCRIBE el fotograma/cadencia real que se usa
//     DURANTE el hover (walking=true); si no existe, el hover usa los mismos valores que
//     SimpleLoop ya establecio. SelStart/SelCount/SelDelay aqui son SIEMPRE los valores reales
//     ya resueltos para el caso "walking=true" (portar la logica completa de WhenSelected/
//     WhenNotSelected/SimpleLoop no hacia falta - solo interesa el estado de hover).
//   - Terraria/DataStructures/SettingsForCharacterPreview.cs (19-45, SelectionBasedSettings.
//     ApplyTo): DelayPerFrame esta en TICKS de juego reales (60/s) - el frame real avanza cada
//     "DelayPerFrame" ticks, ciclando por FrameCount fotogramas empezando en StartFrame.
//   - Terraria/Main.cs (projFrames[id] = N): numero REAL de filas de la hoja de sprite del
//     proyectil (Projectile_{shoot}.xnb) - necesario para saber la altura real de CADA
//     fotograma (altura total de la hoja / projFrames), dato que "SimpleLoop" no lleva consigo.
//   - Terraria/Item.cs (DefaultToVanitypet(shoot, buffType) y las asignaciones "shoot = N;
//     buffType = M;" manuales de los objetos mas antiguos): que objeto de mascota dispara que
//     TIPO de proyectil real.
//
// Generado UNA vez con scripts/extraer-sprites-mascotas.js (que lee este mismo fichero para
// saber que hojas de sprite extraer) - ver bitacora.md 21-sep-2026 para el detalle completo de
// la extraccion (combinar_catalogo_mascotas.py, script de un solo uso, no forma parte del
// repositorio - el resultado real es este JSON).
//
// ALCANCE DELIBERADO, documentado y no oculto: cubre 63 de las ~82 mascotas reales de vanidad/
// luz conocidas (Main.cs, vanityPet[]/lightPet[] - ver el comentario real de
// EquipmentAppearanceResolver.ResolvePet). Las que faltan aqui (objetos sin "shoot"/buffType
// resoluble por texto en Item.cs, o con FrameCount=0 real - esas ULTIMAS SI son fieles, Terraria
// tampoco las anima) caen al icono estatico ya existente, nunca a un crash ni a un dato
// inventado.
//
// OffsetX/OffsetY/SpriteDirection (25-sep-2026, PortSeleccion Encargo4): ademas de la formula
// GENERICA de posicion mascota-vs-personaje (MainWindow.xaml, capa aparte), Terraria real aplica
// un offset ADICIONAL propio de cada mascota y un espejo horizontal, via
// Terraria/DataStructures/SettingsForCharacterPreview.cs (ApplyTo, linea 64:
// "proj.position += Offset"; lineas 65-66: "proj.spriteDirection = SpriteDirection" - casi
// siempre -1, espejo respecto al frame base del sprite sheet). Tabla real completa en
// Terraria/ID/ProjectileID.cs (34-37, CharacterPreviewAnimations), ".WithOffset(x, y)"/
// ".WithSpriteDirection(d)" por TIPO de proyectil, cruzada aqui por el mismo "shoot" que ya usa
// el catalogo. Las 63 mascotas catalogadas SI tienen entrada explicita en la tabla real (ninguna
// cae al valor por defecto) - el valor por defecto real de Terraria cuando un proyectil no esta
// en la tabla es Offset=(0,0)/SpriteDirection=1 (SettingsForCharacterPreview.cs, campos sin
// inicializar salvo SpriteDirection=1 por defecto de la clase), documentado aqui por si el
// catalogo crece en el futuro con una mascota sin entrada real.
public sealed class PetAnimationEntry
{
    [JsonPropertyName("shoot")] public required int Shoot { get; init; }
    [JsonPropertyName("light")] public bool Light { get; init; }
    [JsonPropertyName("selStart")] public required int SelStart { get; init; }
    [JsonPropertyName("selCount")] public required int SelCount { get; init; }
    [JsonPropertyName("selDelay")] public required int SelDelay { get; init; }
    [JsonPropertyName("totalFrames")] public required int TotalFrames { get; init; }
    [JsonPropertyName("offsetX")] public double OffsetX { get; init; }
    [JsonPropertyName("offsetY")] public double OffsetY { get; init; }
    [JsonPropertyName("spriteDirection")] public int SpriteDirection { get; init; } = 1;

    // PortSeleccion Encargo5 (26-sep-2026): nombre REAL del delegado custom de
    // Terraria/DelegateMethods.cs (clase CharacterPreview) que ".WithCode(...)" asigna a este
    // proyectil en la tabla real (Terraria/ID/ProjectileID.cs, CharacterPreviewAnimations) -
    // null cuando la entrada NO tiene ningun WithCode (la mayoria: solo ciclan de frame, ya
    // cubierto por SelStart/SelCount/SelDelay). Ver PetCustomAnimationCode.Evaluate para el
    // significado real de cada valor y la cita exacta del decompilado que lo respalda -
    // constantes de esa clase, nunca un texto libre inventado aqui.
    [JsonPropertyName("code")] public string? Code { get; init; }
}

public sealed class PetAnimationCatalog
{
    private readonly Dictionary<int, PetAnimationEntry> _byItemId;

    private PetAnimationCatalog(Dictionary<int, PetAnimationEntry> byItemId) => _byItemId = byItemId;

    public PetAnimationEntry? ByItemId(int itemId) => _byItemId.TryGetValue(itemId, out var entry) ? entry : null;

    public static PetAnimationCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static PetAnimationCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, PetAnimationEntry>>(stream)
            ?? throw new InvalidDataException("pet_animations.json invalido.");
        var byId = new Dictionary<int, PetAnimationEntry>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;
        return new PetAnimationCatalog(byId);
    }
}
