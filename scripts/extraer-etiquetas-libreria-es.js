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

  // Correccion ortografica (FASE D del responsive global, 28-sep-2026): el es-ES de Terrasavr trae las
  // etiquetas SIN tildes y con alguna errata ("Pre-Modo Dificil", "Pagina $1", "Objectos por ID",
  // "Accessorios", "Librera"...). Con el arbol lateral retirado, NavegadorCategorias las enseña a la
  // vista (desplegable de subcategorias, migas de la ruta, subpastillas de "Ver todo") y el barrido
  // T-E-TILDES del arnes las marca como FALLO. Son textos de la interfaz (etiquetas de carpeta), no
  // nombres de objeto del juego: se corrigen aqui, en la unica fuente del JSON, para que una
  // regeneracion futura no las devuelva a su forma sin tildes. Solo tildes/eñes y erratas evidentes, sin
  // retraducir nada.
  const correcciones = {
    "Pre-Hardmode": "Pre-Modo Difícil", "Hardmode": "Modo Difícil",
    "Page $1": "Página $1", "Pages $1+": "Páginas $1+",
    "Items by ID": "Objetos por ID", "Categories": "Categorías",
    "Quest fish": "Misión de Pez", "Potions (regeneration)": "Pociones (regeneración)",
    "Accessories ($1)": "Accesorios ($1)", "Magic damage ($1)": "Daño Mágico ($1)",
    "Summon damage ($1)": "Daño de Invocación ($1)", "Summoner whips ($1)": "Látigos de Invocador ($1)",
    "Fishing poles ($1)": "Cañas de Pescar ($1)", "Bookcase": "Librerías", "Bathtub": "Bañeras",
    "Lamp": "Lámparas", "Chandelier": "Lámparas de Araña", "Sofa": "Sofás",
    "Ebonwood": "Madera de Ébano", "Shadewood": "Madera Sombría", "Dynasty": "Dinástica",
    "Spooky wood": "Madera Tétrica", "Plants & Organic": "Plantas & Orgánico",
    "Marble": "Mármol", "Vortex": "Vórtice", "Lesion": "Lesión",
    // Segunda revision visual (L-01): terminologia OFICIAL de Terraria contrastada con
    // es-ES.Items.json de lang.zip (Mineral carmesí, Lingote de clorofita / piñonita, Madera perlada,
    // Bambú, Fragmento de nebulosa, Silla de champiñón / de oro, Vagoneta, Madera de caoba rica) y un
    // solo termino por mueble, el del juego: Fregadero (66 objetos), Retrete (65; "Inodoro" 1), Linterna
    // (72; "Farolillo" 2), Librería (63), Aparador (64), Banco de trabajo, Candelabro. "Glass" se queda
    // en "Vidrio" a proposito: el juego dice "cristal" tanto para Glass como para Crystal, y las dos
    // carpetas conviven en la misma rama.
    "Demonite & Crimtane": "Mineral Endemoniado & Mineral Carmesí",
    "Hallowed & Chlorophyte": "Sagrado & Clorofita", "Shroomite & Ectoplasm": "Piñonita & Ectoplasma",
    "Nebula": "Nebulosa", "Pearlwood": "Madera Perlada", "Bamboo": "Bambú", "Mushroom": "Champiñón",
    "Golden": "Oro", "Mahogany": "Madera de Caoba Rica", "Minecarts": "Vagonetas",
    "Sink": "Fregaderos", "Toilet": "Retretes", "Dresser": "Aparadores", "Candelabra": "Candelabros",
    "Workbench": "Bancos de Trabajo", "Bookcases ($1)": "Librerías ($1)", "Lanterns ($1)": "Linternas ($1)",
  };
  let corregidas = 0;
  for (const [clave, texto] of Object.entries(correcciones)) {
    if (!(clave in libItem)) throw new Error(`Correccion ortografica: la clave "${clave}" ya no existe en lib.item.`);
    if (libItem[clave] !== texto) { libItem[clave] = texto; corregidas++; }
  }
  console.log(`${corregidas} etiqueta(s) corregida(s) (tildes/erratas).`);

  fs.writeFileSync(outPath, JSON.stringify(libItem));
  console.log(`Guardado: ${outPath}`);
} finally {
  fs.rmSync(tmpDir, { recursive: true, force: true });
}
