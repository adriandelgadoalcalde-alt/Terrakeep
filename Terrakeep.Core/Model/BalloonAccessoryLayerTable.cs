namespace Terrakeep.Core.Model;

// GapAnalysis Encargo C (25-sep-2026): item.balloonSlot se clasifica en 2 canales reales
// (Terraria/Player.cs decompilado real, UpdateVisibleAccessory, Player.cs:37232-37241):
//
//   if (item.balloonSlot > 0)
//   {
//       if (ArmorIDs.Balloon.Sets.DrawInFrontOfBackArmLayer[item.balloonSlot])
//           balloonFront = item.balloonSlot;
//       else
//           balloon = item.balloonSlot;
//   }
//
// Tabla real transcrita LITERAL de Terraria/ID/ArmorIDs.cs (decompilado real,
// Downloads\tModLoader-Decompiled\TerrariaVanilla\), clase Balloon.Sets:
//
//   ArmorIDs.cs:2252 - DrawInFrontOfBackArmLayer = Factory.CreateBoolSet(false, 18);
//
// SetFactory.CreateBoolSet(bool defaultState, params int[] types) pone los INDICES de "types"
// a !defaultState (SetFactory.cs:97-109, confirmado leyendo el codigo real) - un unico indice
// real, 18 = RoyalScepter (item vanilla 5076, Item.cs:45311-45319: "balloonSlot = 18;
// vanity = true;"), el UNICO balloonSlot real con "true" entre las 19 variantes de globo
// reales del juego (ArmorIDs.Balloon.Count=20, slots 1..19 asignados por items reales).
//
// Dato real relevante para el renderer (ver PlayerPreviewRenderer.LoadBalloonFrame/
// DrawAccessory): ArmorIDs.cs:2254, "UsesTorsoFraming = Factory.CreateBoolSet(false, 18)" -
// EL MISMO unico indice (18). Confirmado ademas contra los sprites reales ya extraidos
// (Assets/player/acc_balloon/*.png, scripts/extraer-sprites-accesorios-vanilla.js): SOLO
// acc_balloon/18.png mide 40x1120 (tira de 20 filas, la MISMA convencion "alineada al lienzo"
// que Waist/Neck/HandOn/HandOff/Back/Face/Shoe - DrawAccessory ya vale para el/DrawPlayer_
// 12_1_BalloonFronts real, rama UsesTorsoFraming=true, PlayerDrawLayers.cs:1114-1120); los
// otros 13 sprites reales miden 52x224 (4 fotogramas propios de 56px, animacion temporal real
// - el globo "flotando", rama UsesTorsoFraming=false/posicion propia, PlayerDrawLayers.cs:
// 1121-1137, ver LoadBalloonFrame para la formula real completa). En la practica, para el
// juego vanilla real, "BalloonFront" es SIEMPRE la rama alineada al lienzo (unico item real,
// RoyalScepter) y "Balloon" normal es SIEMPRE la rama de posicion propia (los otros 18) - la
// tabla de abajo sigue transcribiendo la condicion REAL (DrawInFrontOfBackArmLayer), no esta
// coincidencia, por si el juego real añadiera en el futuro otro balloonFront sin
// UsesTorsoFraming (no se da hoy, documentado con honestidad).
public static class BalloonAccessoryLayerTable
{
    // ArmorIDs.cs:2252, valor real EXACTO (no inventado) - unico indice con "true".
    private static readonly HashSet<int> FrontBalloonSlots = [18];

    public static bool IsFrontLayer(int balloonSlot) => FrontBalloonSlots.Contains(balloonSlot);
}
