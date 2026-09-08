// Extrae las etiquetas REALES en español de la Libreria de Terrasavr - namespace "lib.item"
// de Terrasavr.es-ES.json (dentro de local-site/lang/lang.zip real) - pedido explicito
// 2-sep-2026 ("tienes que traducirla al español todas y sus subramas tambien... fielmente
// como esta en terrsav"). Confirmado real leyendo el propio script.js: la clase base de los
// nodos del arbol de la Libreria (app.Shelf/"ha", de la que heredan Dir/Items - ub/mb) SI
// traduce sus nombres via `l.loc("lib.item", this.enName, this.enName)` en su updateLang() -
// no es una etiqueta inventada, es el mismo mecanismo real de idioma que ya usa el resto de
// la app (confirmado tambien contra ha.rxPage/rxPages/rxAuto/rxNum, las 4 formas reales en
// que ese nombre puede llegar antes de la traduccion - ver LibraryLabelTranslator.cs, que
// replica esa misma logica exacta en C#).
//
// AÑADIDO 8-sep-2026: a esas etiquetas reales de Terrasavr se le suman las de las carpetas
// NUEVAS que crea la reorganizacion por subtipo de la rama "Categories" (Espadas, Arcos, Picos,
// Bloques, Estandartes...). Terrasavr no las tiene porque esas carpetas no existian en el, asi
// que salen de la tabla `labels` de vanilla_library_subtypes.json, donde viven al lado de la
// regla que crea cada grupo. Se añaden con la misma forma de PLANTILLA que usa el propio
// Terrasavr para todo lo que lleva recuento ("Melee damage ($1)"), que es lo que espera
// LibraryLabelCatalog.Translate.
//
// Uso: node scripts/extraer-etiquetas-libreria-es.js
//      (despues de python scripts/extraer-subtipos-libreria-vanilla.py)
// Salida: Terrakeep.App/Assets/vanilla_library_labels_es.json

const fs = require("fs");
const path = require("path");
const { execFileSync } = require("child_process");
const os = require("os");

const langZipPath = path.resolve(__dirname, "..", "..", "Terrasavr-Calamity-Beta", "resources", "app", "local-site", "lang", "lang.zip");
const assetsDir = path.resolve(__dirname, "..", "Terrakeep.App", "Assets");
const outPath = path.join(assetsDir, "vanilla_library_labels_es.json");
const subtypesPath = path.join(assetsDir, "vanilla_library_subtypes.json");

const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), "libreria-es-"));
try {
  // tar.exe de Git Bash interpreta "C:\..." como "host:ruta" (sintaxis remota estilo ssh) y
  // falla - Expand-Archive de PowerShell es fiable en este Windows real para .zip.
  execFileSync("powershell.exe", ["-NoProfile", "-Command",
    `Expand-Archive -Path "${langZipPath}" -DestinationPath "${tmpDir}" -Force`]);
  const data = JSON.parse(fs.readFileSync(path.join(tmpDir, "Terrasavr.es-ES.json"), "utf8"));
  const libItem = data["lib.item"];
  if (!libItem) throw new Error('El namespace "lib.item" no existe en Terrasavr.es-ES.json real.');

  const count = Object.keys(libItem).length;
  console.log(`lib.item real: ${count} etiquetas.`);
  for (const key of ["Materials", "Melee damage ($1)", "Page $1", "Items by ID"]) {
    if (!(key in libItem)) throw new Error(`Spot-check fallido: falta la clave real "${key}".`);
    console.log(`  "${key}" -> "${libItem[key]}"`);
  }

  if (!fs.existsSync(subtypesPath)) {
    throw new Error(`Falta ${subtypesPath}. Ejecuta antes: python scripts/extraer-subtipos-libreria-vanilla.py`);
  }
  const subtipos = JSON.parse(fs.readFileSync(subtypesPath, "utf8"));
  let nuevas = 0;
  for (const [clave, textos] of Object.entries(subtipos.labels)) {
    const plantilla = `${textos.en} ($1)`;
    if (plantilla in libItem) continue;   // si Terrasavr ya traduce ese nombre, manda el suyo
    libItem[plantilla] = `${textos.es} ($1)`;
    nuevas++;
  }
  console.log(`+${nuevas} etiquetas de las carpetas nuevas por subtipo (total ${Object.keys(libItem).length}).`);

  fs.writeFileSync(outPath, JSON.stringify(libItem));
  console.log(`Guardado: ${outPath}`);
} finally {
  fs.rmSync(tmpDir, { recursive: true, force: true });
}
