using Terrakeep.Core.Data;
using Terrakeep.Core.Model;

namespace Terrakeep.Core.Calamity;

// Resuelve una entrada de builds.json/builds_calamity.json (BuildItemRef, solo nombres/"pid")
// a un GameItem real listo para colocar en un slot - usado por "Auto-equipar" en el panel
// Builds. Un "pid" con "/" es de Calamity (mod/nombreInterno); sin "/" es vanilla (nombre
// interno tal cual, mismo formato que los PID de investigacion vanilla).
public static class BuildItemResolver
{
    public static GameItem? Resolve(
        BuildItemRef itemRef,
        VanillaItemCatalog vanillaCatalog,
        CalamityCatalog calamityCatalog,
        VanillaPrefixCatalog vanillaPrefixCatalog)
    {
        if (string.IsNullOrEmpty(itemRef.Pid)) return null;

        int slash = itemRef.Pid.IndexOf('/');
        int id;
        if (slash >= 0)
        {
            string mod = itemRef.Pid[..slash];
            string internalName = itemRef.Pid[(slash + 1)..];
            var entry = calamityCatalog.ByModAndInternal(mod, internalName);
            if (entry == null) return null;
            id = entry.SyntheticId;
        }
        else
        {
            var vanillaId = vanillaCatalog.GetIdByKey(itemRef.Pid);
            if (vanillaId == null) return null;
            id = vanillaId.Value;
        }

        var prefix = ItemPrefix.None;
        if (itemRef.PrefixId is int syntheticPrefixId)
        {
            prefix = ItemPrefix.CalamitySynthetic(syntheticPrefixId);
        }
        else if (!string.IsNullOrEmpty(itemRef.Prefix))
        {
            var prefixEntry = vanillaPrefixCatalog.ByInternal(itemRef.Prefix);
            if (prefixEntry != null) prefix = ItemPrefix.Vanilla((byte)prefixEntry.Id);
        }

        return new GameItem { Id = id, Count = 1, Prefix = prefix };
    }
}
