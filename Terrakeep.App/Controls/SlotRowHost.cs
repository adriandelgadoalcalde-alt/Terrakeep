using System.Windows.Controls;

namespace Terrakeep.App.Controls;

// Marcador trivial (pregunta a Opus sobre el diseño, quinta pasada) - un Grid normal, solo con
// tipo propio para que los SlotGridPanel de la fila fusionada de Equipamiento (los 3 laterales)
// puedan enlazar ReferenceWidth con RelativeSource AncestorType=SlotRowHost de forma fiable.
// Un RelativeSource AncestorType=Grid a secas encontraria el Grid interno de la plantilla por
// defecto del ScrollViewer (que envuelve al lateral vertical) antes de llegar aqui - mismo
// motivo real por el que AvailableHeight ya usa AncestorType=ScrollViewer en vez de un tipo
// generico.
public sealed class SlotRowHost : Grid;
