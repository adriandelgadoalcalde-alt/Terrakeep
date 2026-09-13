namespace Terrakeep.App.ViewModels;

// Filas del Comparador de personajes (encargo del usuario, 13-sep-2026: "seleccionar dos
// personajes/builds y ver lado a lado sus diferencias - equipo, stats, prefijos, inventario -
// resalta lo que cambia"). Tres formas de fila, una por bloque del panel, las tres con
// IsDifferent para que la plantilla XAML resalte con el mismo borde de acento en los tres
// bloques. Label ya viene REDACTADO (no una clave) - CompareViewModel.RebuildRows reconstruye
// las tres colecciones enteras al cambiar de idioma (misma idea que LibraryViewModel.
// RefrescarIdioma, pero aqui reconstruir es mas simple que mantener vivo un indexador Loc[clave]
// por fila, porque el rotulo de Accesorio ya lleva el numero incrustado - Format, no una clave
// suelta).

// Bloque "Estadisticas" (dificultad, vida, mana, muertes, tiempo jugado...) - los dos valores ya
// vienen como texto REDACTADO (numero + unidad si aplica), igual que el resto de la app compone
// frases en la capa App y nunca en Core.
public sealed class CompareStatRowViewModel(string label, string valueA, string valueB, bool isDifferent)
{
    public string Label { get; } = label;
    public string ValueA { get; } = valueA;
    public string ValueB { get; } = valueB;
    public bool IsDifferent { get; } = isDifferent;
}

// Bloque "Equipo" - una fila por ranura real (armadura/accesorios/mascota-montura-gancho...),
// con el tinte puesto en esa ranura (si tiene sentido real en ella) como un icono pequeño
// aparte, nunca fingido para ranuras que no lo llevan.
public sealed class CompareEquipmentRowViewModel(string label, CompareItemViewModel itemA, CompareItemViewModel? dyeA,
    CompareItemViewModel itemB, CompareItemViewModel? dyeB, bool isDifferent)
{
    public string Label { get; } = label;
    public CompareItemViewModel ItemA { get; } = itemA;
    public CompareItemViewModel? DyeA { get; } = dyeA;
    public CompareItemViewModel ItemB { get; } = itemB;
    public CompareItemViewModel? DyeB { get; } = dyeB;
    public bool IsDifferent { get; } = isDifferent;
}

// Bloque "Inventario" - una celda por slot real (50 por personaje), en rejilla, no en lista -
// mismo lenguaje visual que el resto de la app (SlotGridPanel). Solo lleva su propio item (nunca
// los dos a la vez): CompareViewModel construye dos colecciones paralelas, una por lado, y las
// dos comparten el mismo IsDifferent por indice para que las dos rejillas resalten la MISMA
// posicion a la vez al mirar "lado a lado".
public sealed class CompareInventorySlotViewModel(int slotIndex, CompareItemViewModel item, bool isDifferent)
{
    public int SlotIndex { get; } = slotIndex;
    public CompareItemViewModel Item { get; } = item;
    public bool IsDifferent { get; } = isDifferent;
}
