namespace TerrasavrNative.Core.Model;

// H6-02 (Opus, sexta pasada): "PlrCharacter.Gender NO es un booleano - es Player.skinVariant
// (0-11), confirmado leyendo Terraria.ID.PlayerVariantID.cs decompilado real: MaleStarter=0,
// MaleSticker=1, MaleGangster=2, MaleCoat=3, FemaleStarter=4, FemaleSticker=5,
// FemaleGangster=6, FemaleCoat=7, MaleDress=8, FemaleDress=9, MaleDisplayDoll=10,
// FemaleDisplayDoll=11. La app trataba Gender==1 como 'macho real' (PlayerVariantID.Sets.
// Male = CreateBoolSet(0,1,2,3,8,10) real) - un varon normal (skinVariant=0, el caso mas
// comun con diferencia) se leia y se mostraba como 'Chica', y marcar 'Chico' a mano escribia
// literalmente 1 (MaleSticker), corrompiendo el byte real guardado en el .plr del usuario.
public static class PlayerVariantSets
{
    // Variantes masculinas reales (PlayerVariantID.Sets.Male) - el resto (4,5,6,7,9,11) son
    // femeninas.
    private static readonly HashSet<byte> Male = [0, 1, 2, 3, 8, 10];

    public static bool IsMale(byte skinVariant) => Male.Contains(skinVariant);

    // El selector de la app (Chico/Chica) es deliberadamente binario - no expone las 10
    // variantes de vestuario base alternativo (Sticker/Gangster/Coat/Dress/DisplayDoll), que
    // exigirian un selector visual propio fuera del alcance de esta pasada (ver
    // scripts/extraer-sprites-jugador.js). Cambiar de genero a mano colapsa a la variante
    // "Starter" real de ese genero (0 varon / 4 mujer) - si el personaje YA tenia una variante
    // alternativa del MISMO genero cargada del .plr, se conserva intacta mientras no se toque
    // el selector (OnIsMaleChanged solo dispara con un cambio real de valor).
    public const byte MaleStarter = 0;
    public const byte FemaleStarter = 4;
}
