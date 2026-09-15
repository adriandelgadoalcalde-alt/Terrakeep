// Sincronizacion de la Guia (Fase B, 15-sep-2026): convierte la seccion "Guia" de los ficheros
// de localizacion REALES de TerrakeepMod (hjson, tModLoader) a un diccionario plano JSON
// clave->texto, mismas claves punteadas que ya usa el mod en runtime (Idiomas.Texto("Guia.Paso."
// + clave + ".Titulo")). Terrakeep (escritorio) no tiene ningun parser de hjson propio ni
// depende de tModLoader - se resuelve aqui, UNA VEZ, con la libreria "hjson" real de npm (misma
// libreria de referencia del formato, no un parser hecho a mano que pueda desviarse del real).
//
// Uso: node hjson-guia-a-json.js <ruta-hjson-entrada> <ruta-json-salida>
//
// Por que NO se guarda el hjson entero: el resto de secciones (Panel/Personaje/Libreria/...) son
// UI de TerrakeepMod, sin equivalente en Terrakeep - solo la sub-clave "Guia" tiene textos que la
// guia de escritorio necesita, y son las unicas que este script aplana.
"use strict";

const fs = require("fs");
const path = require("path");
const Hjson = require(path.join(
	"C:\\Users\\adrian\\Downloads\\dev-tools\\node-v24.20.0-win-x64\\node_modules", "hjson"
));

const [, , rutaEntrada, rutaSalida] = process.argv;
if (!rutaEntrada || !rutaSalida) {
	console.error("Uso: node hjson-guia-a-json.js <entrada.hjson> <salida.json>");
	process.exit(1);
}

const texto = fs.readFileSync(rutaEntrada, "utf8");
const raiz = Hjson.parse(texto);

if (!raiz || typeof raiz.Guia !== "object") {
	console.error(`"${rutaEntrada}" no tiene una clave de nivel superior "Guia" - nada que sincronizar.`);
	process.exit(1);
}

// Aplana { Req: { CristalesVida: "..." } } -> { "Guia.Req.CristalesVida": "..." }, recursivo,
// EXACTAMENTE el mismo convenio de puntos que usa tModLoader.Localization para resolver una
// clave anidada de hjson a una cadena.
const salida = {};
function aplanar(nodo, prefijo) {
	for (const clave of Object.keys(nodo)) {
		const valor = nodo[clave];
		const claveCompleta = prefijo + "." + clave;
		if (valor !== null && typeof valor === "object" && !Array.isArray(valor)) {
			aplanar(valor, claveCompleta);
		} else {
			salida[claveCompleta] = String(valor);
		}
	}
}
aplanar(raiz.Guia, "Guia");

fs.writeFileSync(rutaSalida, JSON.stringify(salida, null, 2) + "\n", "utf8");
console.log(`OK: ${Object.keys(salida).length} claves "Guia.*" escritas en ${rutaSalida}`);
