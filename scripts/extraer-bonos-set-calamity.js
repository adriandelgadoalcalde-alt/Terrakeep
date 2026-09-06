// Extrae el texto REAL del bono de set completo de las armaduras de Calamity - pedido
// explicito del usuario tras el arreglo de defensa: "si el arreglo de defensa si pero la
// bonificacion por el set no". Investigacion real hecha antes de escribir esto (no adivinado):
//
// 1. player.setBonus se rellena en UpdateArmorSet() con
//    this.GetLocalization("SetBonus").Format(args...) - la CLAVE real de localizacion es
//    Mods.CalamityMod.{LocalizationCategory}.{NombreDeClase}.SetBonus (confirmado leyendo
//    AerospecHeadMelee.cs + su hjson real).
// 2. El texto de esa clave usa el sistema real de sustitucion de tModLoader
//    ({$Key.Path} y {$Key.Path@N}) - decompilado LanguageManager.ProcessCopyCommandsInTexts,
//    Downloads\tModLoader-Decompiled\tModLoader\Terraria\Localization\LanguageManager.cs:562.
//    Replicado aqui CAMPO A CAMPO (FindKeyInScope busca la clave referenciada subiendo un
//    nivel de la ruta de la clave que la usa cada vez, tomando la primera que exista; @N
//    desplaza los {0}/{1}/... del texto referenciado en N posiciones antes de insertarlo).
// 3. La UNICA parte de estas referencias que NO es texto propio de Calamity (por tanto sin
//    traduccion real disponible) es "{$CommonItemTooltip.X}" - confirmado que es un grupo de
//    tModLoader/Terraria CORE (Language.GetOrRegister("CommonItemTooltip...") sin prefijo de
//    mod, en CalamityGlobalItem.cs), definido en tModLoader.json - y ESE SI tiene traduccion
//    oficial real al español (verificado, Terraria.Localization.Content.es_ES.tModLoader.json).
//    Para esas referencias se usa el texto ESPAÑOL OFICIAL directamente en vez de traducirlo
//    a mano.
// 4. Los numeros reales que rellenan {0}/{1}/... salen de los argumentos reales de .Format(),
//    resueltos contra los campos static reales de cada clase (o de otra clase referenciada,
//    ej. AerospecBreastplate.SetBonusHurtDamageThreshold) y las formulas reales de
//    CalamityUtils.cs (ToPercent/ToStealth/FramesToSeconds/ScaleWithDifficulty), no adivinadas.
//
// Salida (modo por defecto, sin argumentos): TerrasavrNative.App/Assets/calamity/
// set_bonus_en.json - texto ingles/español MIXTO (español real donde CommonItemTooltip lo
// permite, ingles donde es texto propio de Calamity) por clase con UpdateArmorSet, listo para
// revisar antes de traducir a mano el resto y escribirlo en catalog.json (paso 2, manual, ver
// bitacora.md).
//
// AMPLIACION 6-sep-2026 (ronda de traduccion del CONTENIDO del juego): `node
// extraer-bonos-set-calamity.js en` corre EXACTAMENTE la misma resolucion pero registrando
// CommonItemTooltip/Key.* desde el `en_US` oficial de tModLoader en vez del `es_ES` -> texto
// 100% INGLES REAL en `set_bonus_en_real.json`. Ojo al nombre: `set_bonus_en.json` (sin
// `_real`) NO es el bono en ingles, es el volcado MIXTO para revision humana descrito arriba;
// el nombre viene de antes de esta ronda y se respeta para no romper
// aplicar-bonos-set-calamity.js, que lo consume como lista real de sets.
//
// Para Calamity el ingles es la fuente MAS fiable (esta instalacion solo trae localizacion
// en-US real), asi que en el modo `en` no hay NADA traducido a mano: es texto literal del mod.
'use strict';
const fs = require('fs');
const path = require('path');

// "en" -> texto 100% ingles real; cualquier otra cosa (o nada) -> modo mixto historico.
// `globalThis.process`, no `process` a secas: este mismo fichero define mas abajo una funcion
// llamada `process(clave)` (la resolucion de {$Key@N}) y el hoisting de esa declaracion TAPA el
// objeto global dentro de todo el modulo - `process.argv` sale undefined aunque el codigo este
// escrito antes.
const MODO_INGLES = globalThis.process.argv[2] === 'en';
const LOC_LANG = MODO_INGLES ? 'en_US' : 'es_ES';
// Marcador real de "esto lo sustituye el juego por la tecla que tenga configurada el jugador":
// no hay binding por defecto que copiar sin inventarlo, mismo criterio que "[tecla]"/"[key]"
// de extraer-tooltips-vanilla.py.
const TEXTO_TECLA = MODO_INGLES ? '(configured key)' : '(tecla configurada)';

const CALAMITY_SRC = 'C:\\Users\\adrian\\Downloads\\tModLoader-Decompiled\\CalamityMod';
const TMOD_PATH = 'C:\\Users\\adrian\\Documents\\My Games\\Terraria\\tModLoader\\Mods\\2026.6CalamityMod.tmod';
const TMOD_EXTRACT = 'C:\\Users\\adrian\\Downloads\\Terrasavr-Win\\Terrasavr-Calamity-Beta\\resources\\app\\tmod-extract.js';
const VANILLA_LOC_ES = `C:\\Users\\adrian\\Downloads\\tModLoader-Decompiled\\tModLoader\\Terraria.Localization.Content.${LOC_LANG}.tModLoader.json`;
const VANILLA_LOC_ES_MAIN = `C:\\Users\\adrian\\Downloads\\tModLoader-Decompiled\\tModLoader\\Terraria.Localization.Content.${LOC_LANG}.Main.json`;
const OUT_PATH = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets', 'calamity',
    MODO_INGLES ? 'set_bonus_en_real.json' : 'set_bonus_en.json');

// ---------- 1) Parser hjson minimo (solo lo que Calamity usa de verdad: claves sin comillas,
// objetos anidados con {}, valores de una linea, bloques '''...''' multilinea, comentarios //).
function parseHjson(text) {
    let i = 0;
    const n = text.length;
    function skipWs() {
        while (i < n) {
            const c = text[i];
            if (c === ' ' || c === '\t' || c === '\r' || c === '\n') { i++; continue; }
            if (c === '/' && text[i + 1] === '/') { while (i < n && text[i] !== '\n') i++; continue; }
            break;
        }
    }
    function readKey() {
        skipWs();
        let start = i;
        while (i < n && !/[\s:{}]/.test(text[i])) i++;
        return text.substring(start, i);
    }
    function readValue() {
        skipWs();
        if (text.startsWith("'''", i)) {
            i += 3;
            let start = i;
            let end = text.indexOf("'''", i);
            let block = text.substring(start, end);
            i = end + 3;
            const lines = block.split('\n').map(l => l.trim()).filter((l, idx, arr) => !(idx === 0 && l === '') && !(idx === arr.length - 1 && l === ''));
            return lines.join('\n');
        }
        if (text[i] === '"') {
            i++;
            let start = i;
            while (i < n && text[i] !== '"') i++;
            const v = text.substring(start, i);
            i++;
            return v;
        }
        if (text[i] === '{') {
            return readObject();
        }
        // Valor sin comillas hasta fin de linea.
        let start = i;
        while (i < n && text[i] !== '\n') i++;
        return text.substring(start, i).trim();
    }
    function readObject() {
        const obj = {};
        skipWs();
        if (text[i] === '{') i++;
        while (true) {
            skipWs();
            if (i >= n) break;
            if (text[i] === '}') { i++; break; }
            const key = readKey();
            skipWs();
            if (text[i] === ':') i++;
            const value = readValue();
            obj[key] = value;
        }
        return obj;
    }
    return readObject();
}

function flatten(obj, prefix, out) {
    for (const [k, v] of Object.entries(obj)) {
        const key = prefix ? prefix + '.' + k : k;
        if (typeof v === 'string') out[key] = v;
        else flatten(v, key, out);
    }
}

// ---------- 2) Cargar TODO el hjson real de Calamity (en-US) en un unico mapa plano
// clave-completa -> texto (con los {0}/{1}/{$...} tal cual, sin resolver todavia).
const { readTmod } = require(TMOD_EXTRACT);
const mod = readTmod(TMOD_PATH);
const registry = {}; // clave completa -> texto crudo
for (const [name, buf] of mod.files) {
    if (!name.startsWith('Localization/en-US/Mods.CalamityMod.') || !name.endsWith('.hjson')) continue;
    const categoria = name.replace('Localization/en-US/Mods.CalamityMod.', '').replace(/\.hjson$/, '');
    const parsed = parseHjson(buf.toString('utf8'));
    flatten(parsed, 'Mods.CalamityMod.' + categoria, registry);
}
console.log(`Registro real de Calamity: ${Object.keys(registry).length} claves de texto (en-US).`);

// CommonItemTooltip (grupo real de tModLoader/Terraria CORE, sin prefijo de mod) - se registra
// TANTO en ingles (para que Exists() lo encuentre igual que el juego real) COMO en español
// real oficial (para poder usarlo directamente en la traduccion final).
// El .json real de tModLoader lleva comentarios de linea ("// Main Menu") - JSON estricto no
// los admite, se limpian las lineas que son SOLO un comentario antes de parsear. Los ficheros
// `en_US.*` traen ADEMAS comas finales reales antes de `}`/`]` (el juego los lee con
// Newtonsoft, que las tolera; JSON.parse no) - se quitan igual, respetando cadenas y escapes
// (gemelo JS de scripts/lang_vanilla.py, ver el docstring de ese modulo).
function quitarComasFinales(texto) {
    let out = '', i = 0, enCadena = false;
    while (i < texto.length) {
        const c = texto[i];
        if (enCadena) {
            out += c;
            if (c === '\\' && i + 1 < texto.length) { out += texto[i + 1]; i += 2; continue; }
            if (c === '"') enCadena = false;
            i++;
            continue;
        }
        if (c === '"') { enCadena = true; out += c; i++; continue; }
        if (c === ',') {
            let j = i + 1;
            while (j < texto.length && ' \t\r\n'.includes(texto[j])) j++;
            if (j < texto.length && (texto[j] === '}' || texto[j] === ']')) { i++; continue; }
        }
        out += c;
        i++;
    }
    return out;
}

function leerLocJson(ruta) {
    const raw = fs.readFileSync(ruta, 'utf8')
        .split('\n').filter(l => !l.trim().startsWith('//')).join('\n');
    try { return JSON.parse(raw); } catch (_) { return JSON.parse(quitarComasFinales(raw)); }
}

const vanillaEs = leerLocJson(VANILLA_LOC_ES);
const commonTooltipEs = vanillaEs.CommonItemTooltip || {};
for (const [k, v] of Object.entries(commonTooltipEs)) {
    registry['CommonItemTooltip.' + k] = v; // se registra YA en español - ver Process() abajo.
}
console.log(`+ ${Object.keys(commonTooltipEs).length} claves reales CommonItemTooltip (español oficial de tModLoader).`);

// Key.UP/Key.DOWN (referenciadas por CalamityUtils.GetArmorSetBonusKey, via MarniteArchitectHeadgear) -
// mismo grupo vanilla CORE, real, oficial, esta vez en Main.json.
const vanillaEsMain = leerLocJson(VANILLA_LOC_ES_MAIN);
for (const [k, v] of Object.entries(vanillaEsMain.Key || {})) registry['Key.' + k] = v;
console.log(`+ ${Object.keys(vanillaEsMain.Key || {}).length} claves reales Key.* (español oficial de tModLoader).`);

// ---------- 3) Resolucion real de {$Key@N} - calco exacto de
// LanguageManager.ProcessCopyCommandsInTexts (ver comentario de cabecera).
const referenceRegex = /\{\$([\w.]+)(?:@(\d+))?\}/g;
const argRemappingRegex = /(\d+)(?=\})/g; // version simplificada real (Calamity no usa el formato "{^N:...}" en estos textos, solo "{N}")

function exists(key) { return Object.prototype.hasOwnProperty.call(registry, key); }

function findKeyInScope(key, scope) {
    if (exists(key)) return key;
    const parts = scope.split('.');
    for (let num = parts.length - 1; num >= 0; num--) {
        const candidate = parts.slice(0, num + 1).join('.') + '.' + key;
        if (exists(candidate)) return candidate;
    }
    return null;
}

const processed = new Map(); // clave -> texto ya resuelto
function process(key) {
    if (processed.has(key)) return processed.get(key);
    if (!exists(key)) { processed.set(key, null); return null; }
    processed.set(key, registry[key]); // marca provisional para cortar ciclos reales
    const resolved = registry[key].replace(referenceRegex, (full, refKey, offsetStr) => {
        const foundKey = findKeyInScope(refKey, key);
        if (!foundKey) { console.log(`  AVISO: no se encontro la clave referenciada "${refKey}" desde "${key}"`); return full; }
        let text = process(foundKey);
        if (text == null) return full;
        if (offsetStr) {
            const offset = parseInt(offsetStr, 10);
            text = text.replace(argRemappingRegex, (m) => String(parseInt(m, 10) + offset));
        }
        return text;
    });
    processed.set(key, resolved);
    return resolved;
}

// ---------- 4) Indice real de ficheros .cs por nombre de clase (para resolver referencias
// cruzadas ClaseX.CampoY) - mismo patron que extraer-defensa-calamity.js.
function indexarFuentes(dir, indice) {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const full = path.join(dir, entry.name);
        if (entry.isDirectory()) indexarFuentes(full, indice);
        else if (entry.isFile() && entry.name.endsWith('.cs')) {
            const nombre = entry.name.slice(0, -3);
            if (!indice.has(nombre)) indice.set(nombre, []);
            indice.get(nombre).push(full);
        }
    }
}
const indiceClases = new Map();
indexarFuentes(CALAMITY_SRC, indiceClases);

function leerFuenteClase(nombreClase) {
    const candidatos = indiceClases.get(nombreClase);
    if (!candidatos) return null;
    for (const filePath of candidatos) {
        const src = fs.readFileSync(filePath, 'utf8');
        if (new RegExp(`class ${nombreClase}\\b`).test(src)) return src;
    }
    return fs.readFileSync(candidatos[0], 'utf8');
}

// Campo/propiedad static real: "public static TIPO Nombre = valor;" o
// "public static TIPO Nombre => valor;" (expression-bodied) - agarra el valor LITERAL crudo
// (puede incluir su propia llamada .ScaleWithDifficulty() etc, resuelta aparte).
function leerCampoStatic(src, nombreCampo) {
    const re = new RegExp(`public static [\\w<>]+\\s+${nombreCampo}\\s*(?:=>|=)\\s*([^;]+);`);
    const m = src.match(re);
    return m ? m[1].trim() : null;
}

// ---------- 5) Formulas REALES de CalamityUtils.cs (ver cabecera) - no aproximadas.
function round(n, decimals) {
    const factor = Math.pow(10, decimals);
    const r = Math.round((n + Number.EPSILON) * factor) / factor;
    return r.toString();
}
function toPercentFloat(v) { return round(v * 100, 1); }
function toStealth(v) { return round(v * 100, 0); }
function framesToSeconds(v) { return round(v / 60, 2); }
function toRegenPerSecond(v) { return round(v * 0.5, 2); }
function toJumpSpeedPercent(v) { return round(v * 20, 2); }
function toTiles(v) { return round(v / 16, 4); }
function getChanceFromDenominator(v) { return toPercentFloat(1 / v); }
function scaleWithDifficulty(v) { return v; } // modo normal (x1) - ver comentario en cabecera del script hermano
function secondsToFrames(v) { return Number.isInteger(v) ? v * 60 : Math.round(v * 60); }

// Metodos de extension reales (ClassName.cs) que producen TEXTO (no un numero encadenable) -
// ver cabecera. Cada uno recibe el valor numerico ya resuelto y devuelve la cadena final real.
const METODOS_TEXTO = {
    ToPercent: toPercentFloat, ToStealth: toStealth, FramesToSeconds: framesToSeconds,
    ToRegenPerSecond: toRegenPerSecond, ToJumpSpeedPercent: toJumpSpeedPercent, ToTiles: toTiles,
    GetChanceFromDenominator: getChanceFromDenominator,
    ScaleWithDifficulty: (v) => String(scaleWithDifficulty(v)),
    Round: (v) => round(v, 4),
};

// Casos reales sin valor numerico fijo (dependen del jugador/config real - keybind asignada,
// color animado) - se sustituyen por un texto honesto en vez de fingir un valor concreto.
// "color.HexN()" se detecta aparte (ver stripColorTags en la seccion de traduccion).
const VALOR_NO_RESOLUBLE = Symbol('no resoluble');

// ---------- 5b) Parser recursivo real de expresiones C# de argumento (no regex-only - hacia
// falta para "(1f + Campo).Round()", "(A / B).FramesToSeconds()", "Clase.Metodo(N)" etc,
// encontrados de verdad en el codigo real al intentar resolver los 69 sets).
function evaluarExpresion(expr, srcClaseActual, nombreClaseActual) {
    expr = expr.trim();
    let i = 0;
    function peek() { return expr[i]; }
    function skipWs() { while (i < expr.length && /\s/.test(expr[i])) i++; }

    function parseIdentifier() {
        skipWs();
        let start = i;
        while (i < expr.length && /[\w]/.test(expr[i])) i++;
        return expr.substring(start, i);
    }

    function parseArgListaLiteral() {
        // Solo se usan literales numericos dentro de llamadas static tipo SecondsToFrames(1.5f).
        skipWs();
        if (peek() === ')') return [];
        const args = [];
        while (true) {
            args.push(parseAdditive());
            skipWs();
            if (peek() === ',') { i++; continue; }
            break;
        }
        return args;
    }

    function parsePrimary() {
        skipWs();
        // Cast real "(float)"/"(int)" - se ignora, solo afecta al tipo, no al valor.
        if (peek() === '(') {
            const save = i;
            i++;
            skipWs();
            const castId = parseIdentifier();
            skipWs();
            if ((castId === 'float' || castId === 'int' || castId === 'double') && peek() === ')') {
                i++;
                return parsePrimary();
            }
            i = save;
        }
        if (peek() === '(') {
            i++;
            const v = parseAdditive();
            skipWs();
            if (peek() === ')') i++;
            return v;
        }
        skipWs();
        // Numero literal real (con sufijo f opcional).
        const numMatch = /^-?\d+(\.\d+)?f?/.exec(expr.slice(i));
        if (numMatch) { i += numMatch[0].length; return parseFloat(numMatch[0]); }

        // Cadena de identificadores reales Clase.Campo.Campo... o llamada static Clase.Metodo(args).
        let ident = parseIdentifier();
        const cadena = [ident];
        skipWs();
        while (peek() === '.') {
            i++;
            cadena.push(parseIdentifier());
            skipWs();
        }
        skipWs();
        if (peek() === '(') {
            // Llamada static real: ULTIMO segmento de la cadena es el metodo, el resto la clase.
            i++;
            const args = parseArgListaLiteral();
            skipWs();
            if (peek() === ')') i++;
            const metodo = cadena[cadena.length - 1];
            if (metodo === 'SecondsToFrames') return secondsToFrames(args[0]);
            throw new Error(`metodo static desconocido "${cadena.join('.')}(...)"`);
        }
        // Identificador/campo real - 1 segmento = campo de la clase actual; 2+ = Clase.Campo
        // (encadenado, ej. SilvaArmor.SetBonusRegenBoost).
        let srcActual = srcClaseActual, claseActual = nombreClaseActual;
        let valor = null;
        for (let seg = 0; seg < cadena.length; seg++) {
            const nombre = cadena[seg];
            if (seg === 0 && cadena.length === 1) {
                const raw = leerCampoStatic(srcActual, nombre);
                if (raw == null) throw new Error(`no se encontro el campo "${nombre}" en "${claseActual}" (posible variable LOCAL, no static - ver bitacora.md)`);
                valor = evaluarExpresion(raw, srcActual, claseActual);
            } else if (seg === 0) {
                // Primer segmento de una cadena de 2+: nombre de OTRA clase.
                const otraSrc = leerFuenteClase(nombre);
                if (!otraSrc) throw new Error(`no se encontro la clase "${nombre}"`);
                srcActual = otraSrc; claseActual = nombre;
            } else {
                const raw = leerCampoStatic(srcActual, nombre);
                if (raw == null) throw new Error(`no se encontro el campo "${nombre}" en "${claseActual}"`);
                valor = evaluarExpresion(raw, srcActual, claseActual);
                if (seg < cadena.length - 1) { srcActual = null; claseActual = nombre; }
            }
        }
        return valor;
    }

    function parseUnary() {
        skipWs();
        if (peek() === '-') { i++; return -parsePrimary(); }
        return parsePrimary();
    }

    function parseMultiplicative() {
        let v = parseUnary();
        skipWs();
        while (peek() === '*' || peek() === '/') {
            const op = expr[i]; i++;
            const rhs = parseUnary();
            v = op === '*' ? v * rhs : v / rhs;
            skipWs();
        }
        return v;
    }

    function parseAdditive() {
        let v = parseMultiplicative();
        skipWs();
        while (peek() === '+' || peek() === '-') {
            const op = expr[i]; i++;
            const rhs = parseMultiplicative();
            v = op === '+' ? v + rhs : v - rhs;
            skipWs();
        }
        return v;
    }

    return parseAdditive();
}

// Casos reales, no numericos, que dependen del jugador (tecla configurada) o son puramente
// cosmeticos (color animado en el texto) - ver investigacion en la cabecera del script.
function esCasoNoResoluble(expr) {
    if (/\.Hex\d*\(\)$/.test(expr)) return 'color'; // color.Hex3() - cosmetico, se retira del texto
    if (/TooltipHotkeyString\(\)$/.test(expr) || expr.includes('GetArmorSetBonusKey')) return 'tecla';
    return null;
}

// Evalua una expresion real de argumento de .Format(...) - punto de entrada real: primero
// comprueba los casos no numericos conocidos (tecla/color), luego usa el parser real de
// arriba para todo lo demas (numeros, campos static, aritmetica, metodos de extension reales).
function evaluarArgumento(expr, srcClaseActual, nombreClaseActual) {
    expr = expr.trim();
    const especial = esCasoNoResoluble(expr);
    if (especial === 'color') return { texto: '', especial: 'color' };
    if (especial === 'tecla') return { texto: TEXTO_TECLA, especial: 'tecla' };

    // Variable LOCAL real (no static) del propio metodo UpdateArmorSet, ej.
    // "int num = (int)((float)StormManaCost * player.manaCost);" (ForbiddenCirclet) o
    // "string text = CalamityKeybinds.X.TooltipHotkeyString();" (GodSlayerHeadMelee y otros) -
    // se busca su declaracion real en el propio fichero y se reevalua la parte derecha.
    if (/^[a-z]\w*$/.test(expr)) {
        const declMatch = srcClaseActual.match(new RegExp(`\\b(?:int|string|float|double)\\s+${expr}\\s*=\\s*([^;]+);`));
        if (declMatch) {
            let rhs = declMatch[1].trim();
            // Caso real puntual (LunicCorpsHelmet, unico en los 69 sets): texto extra que solo
            // aparece en Modo Revancha (CalamityWorld.revenge) - se usa el caso base real (modo
            // normal, world.revenge=false), que es literalmente "" en el propio codigo fuente.
            if (/CalamityWorld\.revenge\s*\?/.test(rhs)) return { texto: '' };
            const especialRhs = esCasoNoResoluble(rhs);
            if (especialRhs === 'color') return { texto: '', especial: 'color' };
            if (especialRhs === 'tecla') return { texto: TEXTO_TECLA, especial: 'tecla' };
            // player.manaCost real: multiplicador de coste de mana, 1 en la base (sin ninguna
            // otra reduccion activa) - se usa ese valor base real, documentado aqui, no una
            // simulacion completa del jugador.
            rhs = rhs.replace(/player\.manaCost/g, '1');
            return evaluarArgumento(rhs, srcClaseActual, nombreClaseActual);
        }
    }

    // Separa la cadena de metodos de extension reales del final (.ToPercent(), etc) del resto
    // de la expresion (que puede llevar parentesis/aritmetica real).
    const m = expr.match(/^(.*?)((?:\.\w+\(\))*)$/s);
    const base = m[1];
    const metodos = [...m[2].matchAll(/\.(\w+)\(\)/g)].map(x => x[1]);

    let valor;
    try {
        valor = evaluarExpresion(base, srcClaseActual, nombreClaseActual);
    } catch (e) {
        return { error: `${e.message} (en "${expr}")` };
    }
    if (typeof valor !== 'number' || Number.isNaN(valor)) return { error: `no se pudo resolver a un numero real "${expr}"` };

    let texto = null;
    for (const metodo of metodos) {
        const fn = METODOS_TEXTO[metodo];
        if (!fn) return { error: `metodo desconocido ".${metodo}()" en "${expr}"` };
        texto = fn(valor);
        valor = parseFloat(texto);
    }
    if (texto == null) texto = String(valor);
    return { valorNumerico: valor, texto };
}

// {^N:singular;plural} real (ver VariableText/argRemappingRegex decompilado) - elige la forma
// segun si el valor resuelto en la posicion N es exactamente 1. Tiene que resolverse ANTES de
// la sustitucion posicional simple porque consume el propio "{N:...}" entero.
function resolverPlurales(texto, argsInfo) {
    // El "^" real es opcional (ver argRemappingRegex decompilado, "{\^?N..." ) - encontrado de
    // verdad en varios sets (AstralHelm, WulfrumHat...), sin el ^ no matcheaba nunca.
    return texto.replace(/\{\^?(\d+):([^;{}]+);([^{}]+)\}/g, (full, idxStr, singular, plural) => {
        const idx = parseInt(idxStr, 10);
        const info = argsInfo[idx];
        if (!info || info.valorNumerico == null) return full;
        return info.valorNumerico === 1 ? singular : plural;
    });
}

// "[c/{1}:texto real]" real (color animado en juego, ej. BrimflameCowl) - en un ToolTip de
// texto plano no hay como pintar ese color, asi que se retira la envoltura y se deja solo el
// texto real de dentro (mejor que enseñar "[c/:texto]" roto tras sustituir un hueco vacio).
function stripColorTags(texto) {
    return texto.replace(/\[c\/[0-9a-fA-F]*:([^\]]*)\]/g, '$1');
}

function splitArgsTopLevel(s) {
    const args = [];
    let depth = 0, start = 0;
    for (let i = 0; i < s.length; i++) {
        if (s[i] === '(') depth++;
        else if (s[i] === ')') depth--;
        else if (s[i] === ',' && depth === 0) { args.push(s.substring(start, i).trim()); start = i + 1; }
    }
    if (start < s.length) args.push(s.substring(start).trim());
    return args.filter(a => a.length > 0);
}

// Resuelve la clave real Mods.CalamityMod.{LocalizationCategory real de esa clase}.{clase}.{clave}
// para una clase CUALQUIERA (la propia o, en los patrones B/C de abajo, otra referenciada) -
// necesita leer el LocalizationCategory real de ESA clase, no asumir el de quien la llama.
function resolverClaveDeClase(nombreClase, claveLoc, srcClaseConocida) {
    const src = srcClaseConocida || leerFuenteClase(nombreClase);
    if (!src) return { error: `no se encontro el fichero fuente de "${nombreClase}"` };
    const locCatMatch = src.match(/LocalizationCategory\s*=>\s*"([^"]+)"/);
    if (!locCatMatch) return { error: `"${nombreClase}" no tiene LocalizationCategory real` };
    const claveCompleta = `Mods.CalamityMod.${locCatMatch[1]}.${nombreClase}.${claveLoc}`;
    const texto = process(claveCompleta);
    if (texto == null) return { error: `no se pudo resolver la clave real "${claveCompleta}"` };
    return { texto, claveCompleta };
}

// ---------- 6) Recorrer los ficheros reales con UpdateArmorSet + player.setBonus.
const resultados = [];
function buscarArchivosArmor(dir, out) {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const full = path.join(dir, entry.name);
        if (entry.isDirectory()) buscarArchivosArmor(full, out);
        else if (entry.name.endsWith('.cs')) out.push(full);
    }
}
const archivosArmor = [];
buscarArchivosArmor(path.join(CALAMITY_SRC, 'CalamityMod', 'Items', 'Armor'), archivosArmor);

for (const filePath of archivosArmor) {
    const src = fs.readFileSync(filePath, 'utf8');
    if (!src.includes('player.setBonus')) continue;
    const claseMatch = src.match(/public class (\w+)\s*:/);
    if (!claseMatch) continue;
    const nombreClase = claseMatch[1];

    let texto = null, argsRaw = null, claveCompleta = null;

    // Patron A (el mas comun, 62/69): this.GetLocalization("X").Format(args) o
    // this.GetLocalizedValue("X") sin args.
    let m = src.match(/player\.setBonus\s*=\s*this\.(GetLocalization|GetLocalizedValue)\("(\w+)"\)(\.Format\(([^;]+)\))?;/s);
    if (m) {
        const r = resolverClaveDeClase(nombreClase, m[2], src);
        if (r.error) { console.log(`FALLO: ${r.error} (${nombreClase})`); continue; }
        texto = r.texto; claveCompleta = r.claveCompleta; argsRaw = m[4];
    }

    // Patron B (Victide x5): this.GetLocalizedValue("X") + "\n" +
    // CalamityUtils.GetTextValueFromModItem<OtraClase>("Y") - concatenacion real de dos claves,
    // la propia y la de otra pieza del mismo set (union hardcodeada de dos frases reales, sin
    // args de .Format en ninguna de las dos).
    if (!m) {
        m = src.match(/player\.setBonus\s*=\s*this\.GetLocalizedValue\("(\w+)"\)\s*\+\s*"\\n"\s*\+\s*CalamityUtils\.GetTextValueFromModItem<(\w+)>\("(\w+)"\);/);
        if (m) {
            const r1 = resolverClaveDeClase(nombreClase, m[1], src);
            const r2 = resolverClaveDeClase(m[2], m[3], null);
            if (r1.error || r2.error) { console.log(`FALLO: ${[r1.error, r2.error].filter(Boolean).join(' | ')} (${nombreClase})`); continue; }
            texto = r1.texto + '\n' + r2.texto; claveCompleta = r1.claveCompleta + ' + ' + r2.claveCompleta;
        }
    }

    // Patron C (Statigel x2): CalamityUtils.GetTextFromModItem<OtraClase>("X").Format(args) -
    // el bono vive en OTRA clase del set (la pieza de cuerpo, no la de cabeza).
    if (!m) {
        m = src.match(/player\.setBonus\s*=\s*CalamityUtils\.GetTextFromModItem<(\w+)>\("(\w+)"\)(\.Format\(([^;]+)\))?;/);
        if (m) {
            const r = resolverClaveDeClase(m[1], m[2], null);
            if (r.error) { console.log(`FALLO: ${r.error} (${nombreClase})`); continue; }
            texto = r.texto; claveCompleta = r.claveCompleta; argsRaw = m[4];
        }
    }

    if (!m) { console.log(`AVISO: ${nombreClase} tiene player.setBonus pero no coincide con NINGUN patron real conocido - revisar a mano.`); continue; }

    let argsInfo = [];
    if (argsRaw) {
        const args = splitArgsTopLevel(argsRaw);
        for (const arg of args) {
            const r = evaluarArgumento(arg, src, nombreClase);
            argsInfo.push({ expr: arg, ...r });
        }
        if (argsInfo.some(a => a.error)) {
            console.log(`FALLO argumentos en ${nombreClase}: ${argsInfo.filter(a => a.error).map(a => a.error).join(' | ')}`);
        } else {
            texto = resolverPlurales(texto, argsInfo);
            argsInfo.forEach((a, idx) => { texto = texto.split(`{${idx}}`).join(a.texto); });
            texto = stripColorTags(texto);
        }
    }

    // Miembros del set - de IsArmorSet(Item head, Item body, Item legs), ModContent.ItemType<X>().
    const isSetMatch = src.match(/IsArmorSet\([^)]*\)\s*\{([\s\S]*?)\n\t\}/);
    const miembros = isSetMatch ? [...isSetMatch[1].matchAll(/ModContent\.ItemType<(\w+)>\(\)/g)].map(m => m[1]) : [];

    resultados.push({ clase: nombreClase, claveLoc: claveCompleta, argsInfo, textoFinal: texto, miembrosSet: [nombreClase, ...miembros] });
}

console.log(`\nTotal sets reales procesados: ${resultados.length}`);
const conFallo = resultados.filter(r => r.textoFinal.includes('{') && /\{\d+\}/.test(r.textoFinal));
console.log(`Con placeholders sin resolver (revisar a mano): ${conFallo.length}`);
if (conFallo.length > 0) console.log(conFallo.map(r => r.clase).join(', '));

fs.writeFileSync(OUT_PATH, JSON.stringify(resultados, null, 2) + '\n', 'utf8');
console.log(`\nSalida real -> ${OUT_PATH}`);

// Muestra real de los primeros 3 para revision manual antes de traducir.
for (const r of resultados.slice(0, 3)) {
    console.log(`\n=== ${r.clase} (set: ${r.miembrosSet.join(', ')}) ===`);
    console.log(r.textoFinal);
}
