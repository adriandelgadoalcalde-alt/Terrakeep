namespace Terrakeep.Core.Model;

// Que tipo(s) de slot restringido acepta un objeto real - pedido explicito 2-sep-2026 ("los
// slots de tintes, gancho, vagoneta, montura y mascota solo deberian poderse equipar sus
// respectivos items, nunca un arma"). Un solo enum de flags en vez de 9 catalogos sueltos
// (consulta a Opus, sexta pasada) - los bits DEBEN coincidir 1:1 con
// scripts/extraer-slot-kind-vanilla.py (comentario de cabecera de ese script tiene el
// detalle real archivo:linea de cada bit).
//
// None (0) = sin restriccion - un slot con AcceptedKind=None acepta cualquier objeto, igual
// que el comportamiento de siempre (Inventario/Banco/... se quedan sin restringir).
//
// Un slot puede aceptar MAS de un tipo (ej. el campo "Index" a mano no tiene por que
// restringirse a un solo bit) pero en la practica cada slot real de la app usa exactamente
// un bit (ver MainViewModel.AddContainer/EquipmentGroupViewModel).
//
// ArmorHead/ArmorBody/ArmorLegs/Accessory (pedido explicito 2-sep-2026, ampliacion de la
// misma sexta pasada: "las armaduras y los accesorios, si los quiero [restringidos] arriba" -
// arriba = la rejilla de Equipamiento) - campos reales headSlot/bodySlot/legSlot/accessory de
// Item.cs, IGUAL para objetos funcionales y de vanidad (vanity=true no cambia el equip type,
// solo si cuenta para las estadisticas - un casco de vanidad sigue teniendo headSlot!=-1, asi
// que este bit vale igual para la vista Armadura/Accesorios Y la vista Vanidad).
[Flags]
public enum SlotKind
{
    None = 0,
    Ammo = 1,
    Coin = 2,
    Dye = 4,
    Hook = 8,
    Mount = 16,
    Cart = 32,
    VanityPet = 64,
    LightPet = 128,
    ArmorHead = 256,
    ArmorBody = 512,
    ArmorLegs = 1024,
    Accessory = 2048,
}
