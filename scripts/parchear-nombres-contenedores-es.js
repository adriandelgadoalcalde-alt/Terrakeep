// Parchea Assets/tile_names.json con las traducciones al español que el generador original
// (generar-tile-names.js, en el repo hermano Terrasavr-Calamity-Beta) NO consigue cruzar solos.
//
// Bug real reportado por el usuario (6-sep-2026, captura): en Exploracion -> Cofres -> "Por tipo
// de cofre" salia "Wooden Chest 45" en INGLES en medio de una interfaz entera en español, junto a
// "Cofre de oro", "Cofre de agua", etc.
//
// Causa real: el generador cruza el nombre INGLES que TEdit le da a cada variante de sprite
// (Data/tiles.json) contra la seccion ItemName de Terraria.Localization.Content.es-ES.Items.json.
// Ese cruce es por coincidencia EXACTA de texto, asi que falla en tres situaciones concretas, y
// todas caen justo en la familia de contenedores:
//   1. TEdit llama a la variante por su nombre COLOQUIAL y el juego no: el cofre normal es
//      "Wooden Chest" para TEdit y ItemName.Chest = "Cofre" para el juego (mismo tile 21, frame
//      0,0). Igual con el aparador de madera: "Wooden Dresser" vs ItemName.Dresser = "Aparador".
//   2. TEdit tiene una errata en su propio dato: "Web Coverd Chest" (falta la 'e' de "Covered"),
//      asi que no casa con ItemName.WebCoveredChest = "Cofre cubierto de telarañas".
//   3. Los nombres BASE de tile ("Chests", "Dressers", "Chests (Group 2)") son CATEGORIAS de
//      TEdit, no objetos del juego - no existen como ItemName y nunca podrian cruzarse. Se usan
//      como respaldo cuando un cofre trae un frame que el catalogo no conoce (p.ej. un cofre de
//      un mod sobre el tile 21), asi que tambien tienen que estar en español.
//
// Cada valor de la tabla de abajo viene de una de estas dos fuentes REALES, nunca inventado:
//   - ItemName.<clave> de Terraria.Localization.Content.es-ES.Items.json (tModLoader-Decompiled\
//     TerrariaVanilla\), citada en el comentario de cada linea.
//   - El patron que el propio catalogo ya aplica a la familia "Trapped X" ("Cofre de oro
//     atrapado" para "Trapped Gold Chest", 441@36,0) o el plural gramatical directo del nombre
//     base ya traducido.
//
// Idempotente y re-ejecutable: solo escribe donde falta name_es, nunca pisa una traduccion que ya
// venga del generador. Volver a pasarlo despues de regenerar tile_names.json.
//
//   node scripts/parchear-nombres-contenedores-es.js

const fs = require('fs');
const path = require('path');

const RUTA = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'tile_names.json');

// tile -> { base: 'nombre base es', frames: { 'u,v': 'nombre es' } }
const PARCHES = {
    21: {
        base: 'Cofres', // categoria de TEdit ("Chests"), plural de ItemName.Chest
        frames: {
            '0,0': 'Cofre',                          // ItemName.Chest
            '540,0': 'Cofre cubierto de telarañas',  // ItemName.WebCoveredChest (TEdit: "Web Coverd Chest", errata suya)
        },
    },
    88: {
        base: 'Aparadores', // categoria de TEdit ("Dressers"), plural de ItemName.Dresser
        frames: {
            '0,0': 'Aparador', // ItemName.Dresser
        },
    },
    467: {
        base: 'Cofres (grupo 2)', // categoria de TEdit ("Chests (Group 2)")
        frames: {},
    },
    441: {
        base: 'Cofres atrapados', // categoria de TEdit ("Trapped Chests")
        frames: {
            // Mismo patron que el catalogo ya usa en esta familia (441@36,0 "Trapped Gold Chest"
            // -> "Cofre de oro atrapado"), incluido el sufijo "(unused - DO NOT USE)" que el
            // generador tampoco traduce en 441@144,0 - es una nota de TEdit, no texto de juego.
            '180,0': 'Barril atrapado (unused - DO NOT USE)',          // ItemName.Barrel
            '216,0': 'Cubo de basura atrapado (unused - DO NOT USE)',  // ItemName.TrashCan
            '540,0': 'Cofre cubierto de telarañas atrapado',           // ItemName.WebCoveredChest
        },
    },
    468: {
        base: 'Cofres atrapados (grupo 2)', // categoria de TEdit ("Trapped Chests (Group 2)")
        frames: {
            '144,0': 'Cofre del hombre muerto atrapado (unused - DO NOT USE)', // gemelo de 467@144,0
        },
    },
};

const datos = JSON.parse(fs.readFileSync(RUTA, 'utf8'));
let puestos = 0, yaEstaban = 0, ausentes = 0;

for (const [id, parche] of Object.entries(PARCHES)) {
    const tile = datos.tiles[id];
    if (!tile) { console.log(`AVISO: el tile ${id} no existe en tile_names.json - omitido`); ausentes++; continue; }
    if (parche.base) {
        if (tile.name_es) yaEstaban++;
        else { tile.name_es = parche.base; puestos++; console.log(`tile ${id} base: "${tile.name}" -> "${parche.base}"`); }
    }
    for (const [uv, nombre] of Object.entries(parche.frames)) {
        const frame = (tile.frames || {})[uv];
        if (!frame) { console.log(`AVISO: el frame ${id}@${uv} no existe - omitido`); ausentes++; continue; }
        if (frame.name_es) { yaEstaban++; continue; }
        frame.name_es = nombre;
        puestos++;
        console.log(`tile ${id}@${uv}: "${frame.name}" -> "${nombre}"`);
    }
}

if (puestos > 0) fs.writeFileSync(RUTA, JSON.stringify(datos), 'utf8');
console.log(`\n${puestos} traduccion(es) puesta(s), ${yaEstaban} ya estaban, ${ausentes} sin destino real.`);
