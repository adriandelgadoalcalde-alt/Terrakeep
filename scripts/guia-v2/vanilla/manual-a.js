'use strict';
// Guia vanilla (F1) - manual A: inicio, mapa, Mazmorra, Templo, Inframundo, jungla.
// Los nombres de objetos/NPC van siempre como [[English Name]] (los resuelve construir-guia-vanilla.js).
module.exports = { articulos: [

// ---------------------------------------------------------------------------------------------
{ id: 'inicio-guia', titulo: 'Empieza aquí', subtitulo: 'Cómo leer la guía, tu clase y tu modo de juego', icono: 'inicio', bloques: [
  { tipo: 'aviso', estilo: 'destacado', texto: '**Tu próxima acción siempre está en la Ruta.** La guía lee tu personaje y tu mundo, marca sola todo lo que puede comprobar y te enseña la primera parada sin completar. Lo demás lo marcas tú a mano.' },
  { tipo: 'titulo', texto: 'Cómo funciona la Ruta' },
  { tipo: 'parrafo', texto: 'La Ruta es una lista de paradas ordenadas por eras: antes del primer jefe, antes del modo difícil, modo difícil, tras Plantera y el final. Las paradas marcadas como opcionales (un evento, un jefe que puedes saltarte) se pueden aplazar sin perder el hilo. Cada parada tiene siempre las mismas secciones, en este orden.' },
  { tipo: 'flujo', items: ['Prepárate', 'Haz esto', 'Durante el combate', 'Lo que se desbloquea', 'Listo para seguir cuando…', 'Conserva'] },
  { tipo: 'cajas', bloques: [
    { tipo: 'caja', titulo: 'Prepárate', bloques: [{ tipo: 'parrafo', texto: 'Vida, defensa, equipo y consumibles que conviene llevar. Sirve para todas las clases; si una clase necesita algo distinto, se dice aparte.' }] },
    { tipo: 'caja', titulo: 'Haz esto', bloques: [{ tipo: 'parrafo', texto: 'Las tareas, en orden. Las que la guía puede comprobar (objetos que llevas, jefes derrotados, vecinos en tu mundo) se tachan solas. Las demás las marcas tú, y esa marca se guarda con tu personaje.' }] },
    { tipo: 'caja', titulo: 'Durante el combate', bloques: [{ tipo: 'parrafo', texto: 'Qué hace el jefe o el evento y cómo reaccionar. Frases cortas: no hace falta leerlo todo en medio de la pelea.' }] },
    { tipo: 'caja', titulo: 'Lo que se desbloquea', bloques: [{ tipo: 'parrafo', texto: 'Qué cambia en el mundo al terminar: vecinos nuevos, biomas accesibles, recetas, objetos que ya puedes fabricar.' }] },
    { tipo: 'caja', titulo: 'Listo para seguir cuando…', bloques: [{ tipo: 'parrafo', texto: 'La comprobación final antes de pasar a la siguiente parada. Léela: evita llegar a un jefe sin el pico, la estación o la vida que hacía falta.' }] },
    { tipo: 'caja', titulo: 'Conserva', bloques: [{ tipo: 'parrafo', texto: 'Lo que no debes vender ni tirar porque se usa en recetas posteriores. Guárdalo en un cofre aparte.' }] }
  ] },
  { tipo: 'aviso', estilo: 'suave', texto: 'Si ya llevas partida, no empieces desde el principio: mira qué jefes has derrotado y sitúate en la parada siguiente. La guía hace esa comparación por ti con las banderas de tu mundo.' },

  { tipo: 'titulo', texto: 'Las cuatro clases' },
  { tipo: 'parrafo', texto: 'Tu clase la define el tipo de daño de tus armas, y a la vez qué armadura y accesorios te dan bonificaciones. **No se mezclan**: la bonificación de daño de una armadura de cuerpo a cuerpo no sirve a un arma de distancia. Puedes llevar piezas sueltas de otro conjunto por su defensa, pero perderás el bonus de conjunto.' },
  { tipo: 'tabla', cabeceras: ['Clase', 'Qué la define', 'Cuidado con…'], filas: [
    ['Cuerpo a cuerpo', 'Espadas, lanzas, yoyós y boomerangs. Mucha vida y defensa, alcance corto. Algunos ataques de espada lanzan proyectiles pero siguen contando como cuerpo a cuerpo.', 'Te expones al contacto. Los enemigos voladores y los jefes rápidos castigan si no tienes movilidad.'],
    ['Distancia', 'Arcos, pistolas, escopetas y lanzadores con munición. El daño depende del arma más la munición, y se mejora con armadura y accesorios de distancia.', 'La munición es un gasto continuo: llévala siempre a mano en su casilla de munición.'],
    ['Magia', 'Báculos, libros de hechizos y varitas que gastan maná. Se apoya en maná máximo y en reducir su coste.', 'Sin maná no atacas. Lleva pociones de maná y accesorios que lo recuperen.'],
    ['Invocación', 'Los esbirros y centinelas combaten por ti; el látigo marca objetivos y mejora a tus invocaciones. Cada esbirro ocupa una casilla de esbirros.', 'Necesitas casillas de esbirros suficientes.']
  ] },
  { tipo: 'parrafo', texto: 'Esta guía admite las cuatro clases. En cada parada, la sección de equipo te indica la mejor opción disponible para la tuya. Elige la clase una vez y cámbiala cuando quieras: tu progreso manual no se borra.' },

  { tipo: 'titulo', texto: 'Clásico, Experto y Maestro' },
  { tipo: 'parrafo', texto: 'El modo se decide al crear el mundo y no se puede cambiar después. La guía lee el modo real de tu mundo y te enseña solo los avisos que le tocan.' },
  { tipo: 'tabla', cabeceras: ['Modo', 'Qué cambia de verdad'], filas: [
    ['Clásico', 'El juego base. Es la referencia: todas las cifras de la guía parten de aquí.'],
    ['Experto', 'Los enemigos tienen el doble de vida y hacen el doble de daño. Los jefes sueltan una bolsa del tesoro en vez de su botín normal, con un accesorio exclusivo de Experto. Los efectos negativos duran más.'],
    ['Maestro', 'Todo lo de Experto, a más. Vida y daño de los enemigos se triplican y los efectos negativos duran todavía más. Además, cada jefe deja una reliquia, y existen objetos y mascotas exclusivos.'],
    ['Viaje', 'Un modo creativo para probar: puedes duplicar objetos sacrificándolos y ajustar la dificultad de los enemigos. Útil para experimentar con builds.']
  ] },
  { tipo: 'aviso', estilo: 'nota', texto: 'Las cifras (doble en Experto, triple en Maestro) son los multiplicadores base del juego para vida y daño de los enemigos. Algunos jefes y proyectiles tienen sus propios ajustes encima, así que úsalas como orden de magnitud, no como valor exacto de cada ataque.' },
  { tipo: 'lista', items: [
    'Las **bolsas del tesoro** (Experto y Maestro) se abren con clic derecho. Cada jefe da la suya, con su accesorio exclusivo; consérvala el tiempo justo, no la vendas sin abrir.',
    'El [[Demon Heart]] te da una casilla de accesorio extra. Solo se puede usar en Experto o Maestro. Lo suelta la bolsa del tesoro del [[n:Wall of Flesh]] si todavía no has usado uno.',
    'Muchos accesorios exclusivos de Experto compensan la subida de dificultad: son parte de tu equipo desde el primer jefe, no un extra.',
    'El modo Maestro no es mejor «porque sí»: aprende los patrones en Experto antes de saltar a él.'
  ] },
  { tipo: 'titulo', texto: 'Configuración útil' },
  { tipo: 'lista', items: [
    'En solitario puedes pausar el juego desde el menú (Esc): úsalo para leer la guía sin que te maten a mitad de pelea.',
    'Abre los controles y asigna a teclas cómodas el gancho, la montura y el uso rápido de pociones.',
    'Usa un cofre de componentes reservados junto a tu base: la sección «Conserva» de cada parada te dice qué meter.',
    'Para una primera partida, un personaje normal (no hardcore) te deja aprender sin perder el personaje al morir.'
  ] },
  { tipo: 'fuentes', fuentes: [{ wiki: 'terraria', pagina: 'Game Modes' }, { wiki: 'terraria', pagina: 'Expert Mode' }, { wiki: 'terraria', pagina: 'Master Mode' }, { wiki: 'terraria', pagina: 'Journey Mode' }, { wiki: 'terraria', pagina: 'Classes' }] }
] },

// ---------------------------------------------------------------------------------------------
{ id: 'mapa', titulo: 'Mapa y biomas', subtitulo: 'Capas del mundo y qué hay en cada bioma', icono: 'mapa', bloques: [
  { tipo: 'aviso', estilo: 'destacado', texto: 'En esta guía el mapa es el de **tu mundo real**. Terrakeep lo muestra en Exploración, y TerrakeepMod en el mapa del propio juego, con la siguiente parada marcada.' },
  { tipo: 'titulo', texto: 'Las capas del mundo' },
  { tipo: 'parrafo', texto: 'Todos los mundos se dividen en capas horizontales, de arriba abajo. Ir más profundo suele significar mejor botín y más peligro. Si dudas de dónde estás, el medidor de profundidad te dice si vas por el subsuelo o más abajo.' },
  { tipo: 'tabla', cabeceras: ['Capa', 'Qué hay', 'Para qué te sirve'], filas: [
    ['{z:cielo|Cielo}', 'Islas flotantes con cofres y casas de cielo, por encima de la superficie.', 'Botín de las islas flotantes, como la [[Starfury]] y el [[Shiny Red Balloon]].'],
    ['{z:superficie|Superficie}', 'El terreno visible, donde nacen árboles y vecinos.', 'Tu base y todo lo del principio.'],
    ['{z:subsuelo|Subsuelo}', 'Tierra y piedra, minas y cuevas pequeñas.', 'Cristales de vida, los primeros minerales y el primer botín de cofres.'],
    ['{z:cavernas|Cavernas}', 'Cuevas grandes, lagos y más minerales.', 'Minerales avanzados, la mayoría de biomas subterráneos y santuarios.'],
    ['{z:inframundo|Inframundo}', 'El fondo del mundo: lava, ruinas y piedra infernal.', 'Piedra infernal y el Muro carnoso. Mira {a:inframundo}.']
  ] },
  { tipo: 'titulo', texto: 'Biomas y estructuras' },
  { tipo: 'parrafo', texto: 'Un bioma «se activa» cuando juntas bastantes bloques propios a tu alrededor, y entonces cambian la música, el fondo y los enemigos que salen. Los biomas están repartidos de forma que algunos quedan siempre en lados opuestos del mundo: la Mazmorra y la jungla nunca están en el mismo lado.' },
  { tipo: 'tabla', cabeceras: ['Bioma', 'Qué hay', 'Cuándo ir'], filas: [
    ['{z:nieve|Nieve}', 'Hielo, nieve y enemigos helados. Subsuelo helado con cuevas y cofres.', 'Pronto: es seguro y sirve para fabricar equipo de frío. Más tarde la Legión de escarcha y la Ventisca.'],
    ['{z:desierto|Desierto}', 'Arena, cactus y sol. Debajo, el desierto subterráneo con fósiles.', 'Cuando tengas armadura de hierro o similar. La Tormenta de arena ocurre aquí.'],
    ['{z:desierto_subterraneo|Desierto subterráneo}', 'Cuevas de arena endurecida con fósiles y pirámides.', 'Para fósiles y la pirámide: [[Flying Carpet]] está en su cofre.'],
    ['{z:jungla|La jungla}', 'Barro, hiedra y árboles de caoba. Enemigos fuertes desde el principio.', 'Antes del modo difícil para la Abeja reina. Mira {a:jungla}.'],
    ['{z:jungla_subterranea|Jungla subterránea}', 'Colmenas, el Templo lihzahrd, santuarios con cofres de hiedra.', 'Tras el Esqueletrón para el botín; el templo, solo tras Plantera. Mira {a:templo}.'],
    ['{z:corrupcion|La Corrupción}', 'Terreno oscuro con escamas de las sombras y abismos. Orbes de las sombras.', 'Pronto si tu mundo la tiene: ahí te esperan el Devoramundos y el equipo de demonita.'],
    ['{z:carmesi|El Carmesí}', 'El equivalente rojo de la Corrupción: tejido y corazones carmesíes.', 'Igual que la Corrupción. Tu mundo tiene una de las dos, no ambas.'],
    ['{z:sagrado|Lo Sagrado}', 'Un bioma brillante y de colores que aparece al derrotar al Muro carnoso.', 'En modo difícil, para bloques y almas. Vigila que no invada tu base.'],
    ['{z:setas|Champiñón (bioma de champiñones)}', 'Hongos brillantes, suelo de barro cubierto de micelio y enemigos propios.', 'En modo difícil, para el vecino que vive en él; antes, para setas y botín.'],
    ['{z:mazmorra|La Mazmorra}', 'Ladrillos azules, verdes o rosas. Bloqueada por la maldición del Anciano.', 'Tras derrotar al Esqueletrón. Mira {a:mazmorra}.'],
    ['{z:templo|El Templo}', 'Estructura lihzahrd en la jungla subterránea, protegida por una puerta.', 'Tras derrotar a Plantera. Mira {a:templo}.'],
    ['{z:oceano|Océano}', 'Arena, agua y playa. Hay uno a cada lado del mundo.', 'Para pesca y para el Duque Fishron. Un lado queda junto a la Mazmorra.'],
    ['{z:granito|Granito}', 'Cuevas de piedra oscura y azul con enemigos propios.', 'Cuando quieras su botín y su minería. No es peligroso al principio.'],
    ['{z:marmol|Mármol}', 'Cuevas de piedra blanca con estatuas y enemigos propios.', 'Igual: son opcionales y de poco riesgo.'],
    ['{z:colmena|Colmena}', 'Bloques de abeja y larvas dentro de la jungla subterránea.', 'Para la Abeja reina.'],
    ['{z:meteorito|Meteorito}', 'Un cráter que cae de noche: el primero, al vencer al jefe del mal de tu mundo; después, de vez en cuando.', 'Cuando quieras mineral de meteorito.'],
    ['{z:altares|Altares}', 'Altares en la Corrupción o el Carmesí.', 'Los altares de demonios y carmesíes sirven para fabricar; en modo difícil se rompen con el [[Pwnhammer]] y dan minerales nuevos.'],
    ['{z:eter|Éter}', 'Un lago de fulgor en el subsuelo.', 'Cuando quieras el fulgor. Mira {a:eter}.']
  ] },
  { tipo: 'aviso', estilo: 'suave', texto: 'Los biomas de la Corrupción y el Carmesí se **extienden** con el tiempo, y mucho más en modo difícil. Si cavas un foso de tres casillas de ancho entre ellos y tu base, tardarán más en llegar.' },
  { tipo: 'titulo', texto: 'Cómo orientarte' },
  { tipo: 'lista', numerada: true, items: [
    'Primero, mira dónde está la Mazmorra: te dice dónde está el lado contrario con la jungla.',
    'El Inframundo está siempre abajo del todo, bajo la capa de cavernas.',
    'La Corrupción o el Carmesí aparecen en zonas visibles desde la superficie.',
    'Si te falta un bioma, la guía te lo marca en el mapa en cuanto toca visitarlo.'
  ] },
  { tipo: 'fuentes', fuentes: [{ wiki: 'terraria', pagina: 'World' }, { wiki: 'terraria', pagina: 'Biomes' }, { wiki: 'terraria', pagina: 'Underground' }] }
] },

// ---------------------------------------------------------------------------------------------
{ id: 'mazmorra', titulo: 'La Mazmorra', subtitulo: 'El Anciano, la maldición, el botín y lo que cambia tras Plantera', icono: 'hallazgo', bloques: [
  { tipo: 'parrafo', texto: 'La Mazmorra es una estructura enorme de ladrillos al borde del mundo. Hay una por mundo. Es el mejor sitio para botín antes del modo difícil, pero está **bloqueada** hasta que derrotes al Esqueletrón.' },
  { tipo: 'titulo', texto: 'El Anciano y la maldición' },
  { tipo: 'parrafo', texto: 'Cerca de la entrada vive el [[Old Man]]. De noche, al hablar con él, te ofrece la maldición: aceptarla invoca al [[Skeletron]]. De día, te dice que vuelvas por la noche. Tu parada es {p:skeletron}: allí tienes la preparación del combate. Aquí cuenta lo que pasa antes y después.' },
  { tipo: 'aviso', estilo: 'destacado', texto: 'Si bajas por la Mazmorra antes de derrotar al [[n:Skeletron]], aparecen [[n:Dungeon Guardian]]. Matan casi de un toque y no se pueden derrotar. No lo intentes: sal o sigue en la superficie.' },
  { tipo: 'titulo', texto: 'Qué encuentras' },
  { tipo: 'tabla', cabeceras: ['Elemento', 'Qué es', 'Consejo'], filas: [
    ['Ladrillos de la Mazmorra', 'Los muros son de uno de tres colores: azul, verde o rosa. Cambia el aspecto, no el contenido.', 'Los muros esconden trampas. Avanza con cuidado.'],
    ['Cofres normales', 'Esparcidos por las habitaciones. A veces contienen una [[Golden Key]].', 'Abre todos los que veas.'],
    ['Cofres dorados cerrados', 'Necesitan una [[Golden Key]], que se gasta al usarla. Cada uno contiene un objeto principal.', 'Consigue varias llaves. Un [[n:Dungeon Slime]] la suelta siempre; otros enemigos y las macetas, a veces.'],
    ['Estanterías', 'Libros entre los estantes. Una pequeña parte esconde un [[Water Bolt]].', 'Rompe todos los libros que encuentres, o míralos con el cursor.'],
    ['[[Bound Mechanic|Mecánico atado]]', 'Un NPC encerrado en las profundidades. Al hablar con ella, la liberas.', 'Después se muda a una casa vacía de tu mundo y vende cable y herramientas.']
  ] },
  { tipo: 'titulo', texto: 'Objetos clave de los cofres dorados' },
  { tipo: 'parrafo', texto: 'Cada cofre dorado cerrado da UNO de estos objetos como principal, con la misma probabilidad. Los primeros cofres que encuentres siguen un orden fijo del mundo.' },
  { tipo: 'cajas', bloques: [
    { tipo: 'caja', titulo: 'Cuerpo a cuerpo', bloques: [{ tipo: 'lista', items: ['[[Muramasa]]: espada.', '[[Blue Moon]]: maza con cadena (flail).', '[[Valor]]: yoyó.'] }] },
    { tipo: 'caja', titulo: 'Distancia', bloques: [{ tipo: 'lista', items: ['[[Handgun]]: pistola.'] }] },
    { tipo: 'caja', titulo: 'Magia', bloques: [{ tipo: 'lista', items: ['[[Aqua Scepter]] y [[Magic Missile]]: dos armas mágicas de maná.', '[[Water Bolt]]: libro de hechizos escondido en las estanterías (no sale de los cofres).'] }] },
    { tipo: 'caja', titulo: 'Defensa', bloques: [{ tipo: 'lista', items: ['[[Cobalt Shield]]: accesorio que evita el retroceso.'] }] }
  ] },
  { tipo: 'titulo', texto: 'Objetos que se suman' },
  { tipo: 'lista', items: [
    'La [[Shadow Key]] sale en algunos cofres dorados cerrados, y siempre en el primero que genera el mundo. Abre los cofres de las sombras del Inframundo y no se gasta al usarla. Mira {a:inframundo}.',
    'El [[Bone Welder]], de los mismos cofres, es una estación de fabricación.',
    'Los objetos raros de la Mazmorra pueden formar parte de recetas más adelante: lee la sección «Conserva» de cada parada antes de vender.'
  ] },
  { tipo: 'aviso', estilo: 'suave', texto: 'El [[Clothier]] se muda a tu base cuando derrotas al [[n:Skeletron]], y la Mazmorra es el sitio donde empieza a ser útil: vende ropa de adorno. La guía te dice cuándo explorar la Mazmorra en {p:mazmorra}.' },
  { tipo: 'titulo', texto: 'Tras derrotar a Plantera' },
  { tipo: 'parrafo', texto: 'Cuando caiga [[n:Plantera]], el mundo te avisa: la Mazmorra cambia. Aparecen enemigos nuevos y mucho más fuertes, según el tipo de pared donde estés (ladrillo, losa o azulejo), y sueltan [[Ectoplasm]], el material de la armadura espectral. Además se pueden abrir los **cofres de bioma**.' },
  { tipo: 'tabla', cabeceras: ['Llave', 'Cofre que abre', 'Dónde conseguirla'], filas: [
    ['[[Jungle Key]]', 'Cofre de la jungla', 'Enemigos de la jungla.'],
    ['[[Corruption Key]]', 'Cofre de la Corrupción', 'Enemigos de la Corrupción.'],
    ['[[Crimson Key]]', 'Cofre del Carmesí', 'Enemigos del Carmesí.'],
    ['[[Hallowed Key]]', 'Cofre sagrado', 'Enemigos de Lo Sagrado.'],
    ['[[Frozen Key]]', 'Cofre de hielo', 'Enemigos de la nieve.'],
    ['[[Desert Key]]', 'Cofre del desierto', 'Enemigos del desierto.']
  ] },
  { tipo: 'aviso', estilo: 'nota', texto: 'Los cofres de bioma están en la Mazmorra. Cada llave cae de enemigos del bioma que le corresponde, con una probabilidad de 1 entre 2.500, y no se puede usar hasta derrotar a Plantera. Cada cofre contiene un arma propia, así que ten paciencia.' },
  { tipo: 'parrafo', texto: 'La parada para todo esto es {p:mazmorra2}. Para el combate de antes, {p:skeletron} y {p:mazmorra}.' },
  { tipo: 'fuentes', fuentes: [{ wiki: 'terraria', pagina: 'Dungeon' }, { wiki: 'terraria', pagina: 'Old Man' }, { wiki: 'terraria', pagina: 'Locked Gold Chest' }, { wiki: 'terraria', pagina: 'Golden Key' }, { wiki: 'terraria', pagina: 'Biome Chest' }, { wiki: 'terraria', pagina: 'Water Bolt' }] }
] },

// ---------------------------------------------------------------------------------------------
{ id: 'templo', titulo: 'El Templo lihzahrd', subtitulo: 'La puerta, las trampas, la Célula de poder y el Eclipse', icono: 'hallazgo', bloques: [
  { tipo: 'parrafo', texto: 'El Templo lihzahrd es una estructura de ladrillos en la jungla subterránea. Todos los mundos tienen uno. Es peligroso y no se puede abrir hasta derrotar a Plantera. Es la parada de {p:templo}.' },
  { tipo: 'titulo', texto: 'Cómo encontrarlo' },
  { tipo: 'lista', numerada: true, items: [
    'Ve a la jungla subterránea, la parte profunda de {z:jungla|La jungla}. Está siempre en el lado contrario a la Mazmorra.',
    'Busca una gran estructura de [[Lihzahrd Brick]] enterrada bajo el barro.',
    'La entrada está en una esquina superior. Es una [[Lihzahrd Door]] que solo se abre con la llave.',
    'Si no ves el templo, usa el mapa a pantalla completa: la guía lo marca cuando toca.'
  ] },
  { tipo: 'aviso', estilo: 'destacado', texto: 'Los ladrillos lihzahrd **no se pueden picar** hasta que tengas un [[Picksaw]], que sale del [[n:Golem]]. Antes de Plantera no hay forma normal de entrar.' },
  { tipo: 'titulo', texto: 'La llave' },
  { tipo: 'parrafo', texto: 'La [[Temple Key]] la suelta [[n:Plantera]]. Se usa en la [[Lihzahrd Door]] de la entrada. Si no la tienes, no entras: vuelve a por Plantera con la parada {p:plantera}.' },
  { tipo: 'titulo', texto: 'Trampas y peligros' },
  { tipo: 'parrafo', texto: 'El templo está lleno de trampas: lanzallamas, lanzas, [[Spiky Ball]] y [[Super Dart Trap]]. Hay muchas más que en la Mazmorra y hacen mucho daño. Un gancho y una buena armadura marcan la diferencia.' },
  { tipo: 'lista', items: [
    'Las placas de presión lihzahrd solo se activan con el jugador, no con los enemigos. Salta o rodéalas.',
    'Los [[n:Lihzahrd]] y los [[n:Flying Snake]] aparecen en el interior y cada uno puede soltar una [[Lihzahrd Power Cell]].',
    'Sigue siempre el pasillo hacia abajo: acaba en la sala del altar. No hace falta explorar todo.'
  ] },
  { tipo: 'titulo', texto: 'El altar y el Gólem' },
  { tipo: 'parrafo', texto: 'En la cámara final está el [[Lihzahrd Altar]]. Para invocar al [[n:Golem]] necesitas una [[Lihzahrd Power Cell]] y haber derrotado a Plantera en modo difícil. Se gasta una célula por intento. Cada cofre lihzahrd contiene una, así que casi siempre hay de sobra.' },
  { tipo: 'flujo', items: ['Derrota a Plantera', 'Consigue la Temple Key', 'Abre la puerta', 'Baja hasta el altar', 'Usa la Power Cell', 'Golem'] },
  { tipo: 'titulo', texto: 'Solar Tablet Fragment y el Eclipse' },
  { tipo: 'parrafo', texto: 'Los [[Solar Tablet Fragment]] se encuentran en los cofres del templo. Se combinan en una [[Solar Tablet]], que sirve para empezar un Eclipse a propósito. El Eclipse también puede ocurrir solo en modo difícil, tras un jefe mecánico, con una probabilidad de una entre veinte cada amanecer.' },
  { tipo: 'lista', items: [
    'Dura todo el día, y los enemigos que aparecen son muy agresivos.',
    'El [[n:Mothron]] aparece después de Plantera y suelta la [[Broken Hero Sword]].',
    'La parada opcional es {p:eclipse}.'
  ] },
  { tipo: 'aviso', estilo: 'nota', texto: 'Los fragmentos y la tableta no son necesarios para llegar al final del juego. Úsalos cuando quieras el botín del Eclipse.' },
  { tipo: 'cajas', bloques: [
    { tipo: 'caja', titulo: 'Lo que tienes que llevar', bloques: [{ tipo: 'lista', items: ['[[Temple Key]]', 'Pociones de curación', 'Un gancho de buena distancia', 'Plataformas para saltar sobre trampas'] }] },
    { tipo: 'caja', titulo: 'Lo que te llevas', bloques: [{ tipo: 'lista', items: ['[[Lihzahrd Power Cell]]', '[[Lihzahrd Furnace]]', '[[Solar Tablet Fragment]]', 'Acceso al [[n:Golem]]'] }] }
  ] },
  { tipo: 'fuentes', fuentes: [{ wiki: 'terraria', pagina: 'Jungle Temple' }, { wiki: 'terraria', pagina: 'Lihzahrd Power Cell' }, { wiki: 'terraria', pagina: 'Temple Key' }, { wiki: 'terraria', pagina: 'Solar Eclipse' }, { wiki: 'terraria', pagina: 'Golem' }] }
] },

// ---------------------------------------------------------------------------------------------
{ id: 'inframundo', titulo: 'El Inframundo', subtitulo: 'Cómo bajar, piedra infernal, cofres de las sombras y el puente del Muro carnoso', icono: 'hallazgo', bloques: [
  { tipo: 'parrafo', texto: 'El Inframundo es la capa más profunda del mundo: lava, ruinas y piedra infernal. Empieza a una profundidad que depende del tamaño del mundo: unas 650 casillas en uno pequeño, 1.000 en uno mediano y 1.500 en uno grande. Aquí se acaba la fase anterior al modo difícil.' },
  { tipo: 'titulo', texto: 'Cómo bajar: el pozo vertical' },
  { tipo: 'parrafo', texto: 'A la forma de bajar en línea recta se le llama **hellevator** (pozo al Inframundo). Cava desde cerca de tu base hasta la capa final.' },
  { tipo: 'lista', numerada: true, items: [
    'Elige un punto lejos de la Corrupción, el Carmesí y la Mazmorra.',
    'Cava recto hacia abajo, con un hueco lo bastante ancho para moverte sin chocar.',
    'Coloca plataformas o cuerdas para poder volver a subir.',
    'Antes de abrir el último tramo, prepara la protección contra la lava (siguiente sección).'
  ] },
  { tipo: 'aviso', estilo: 'destacado', texto: 'No caigas a ciegas. Llega con una [[Obsidian Skin Potion]] activa o con un accesorio que proteja de la lava, como el [[Lava Charm]] o la [[Obsidian Skull]].' },
  { tipo: 'titulo', texto: 'Piedra infernal' },
  { tipo: 'parrafo', texto: 'La [[Hellstone]] hace falta para la armadura y las armas de la siguiente etapa. Quema si la tocas sin protección. Para picarla necesitas un pico de **65 % de potencia como mínimo**. Antes del modo difícil, los que llegan son el [[Nightmare Pickaxe]] (con demonita, de la Corrupción) y el [[Deathbringer Pickaxe]] (con carmesita, del Carmesí). El [[Molten Pickaxe]] se fabrica con la propia piedra infernal, así que no te sirve para la primera extracción.' },
  { tipo: 'parrafo', texto: 'Para fundir la barra necesitas una [[Hellforge]] y obsidiana: la receta de [[Hellstone Bar]] lleva tres de piedra infernal y una de [[Obsidian]]. La obsidiana se forma al juntar agua y lava.' },
  { tipo: 'titulo', texto: 'Casas en ruinas, cofres de las sombras' },
  { tipo: 'parrafo', texto: 'Las **casas en ruinas** son torres sueltas de obsidiana y ladrillo infernal repartidas por el Inframundo. Dentro hay muebles propios, una [[Hellforge]] y, lo más importante, **cofres de las sombras**. Cada uno requiere una [[Shadow Key]], que no se gasta al usarla.' },
  { tipo: 'tabla', cabeceras: ['Qué necesitas', 'Qué te llevas'], filas: [
    ['[[Shadow Key]] (de los cofres dorados de la Mazmorra, mira {a:mazmorra})', 'Un arma principal de entre [[Sunfury]], [[Flamelash]], [[Dark Lance]], [[Flower of Fire]], [[Unholy Trident]] y [[Hellwing Bow]], más pociones, barras y monedas.']
  ] },
  { tipo: 'aviso', estilo: 'suave', texto: 'La llave no se gasta, así que puedes abrir todos los cofres de las sombras que encuentres con la misma.' },
  { tipo: 'titulo', texto: 'El puente del Muro carnoso' },
  { tipo: 'parrafo', texto: 'Para invocar al [[n:Wall of Flesh]] tira un [[Guide Voodoo Doll]] a la lava del Inframundo. Lo sueltan los [[n:Voodoo Demon]] y requiere que el [[n:Guide]] esté vivo. Cuando el Guía muere en la lava, el jefe también aparece.' },
  { tipo: 'lista', items: [
    'Construye antes un puente largo de plataformas que cruce todo el mundo, de lado a lado.',
    'Las plataformas dejan pasar tus proyectiles. Los bloques sólidos paran los láseres del jefe pero te estorban.',
    'El jefe aparece lejos de ti, pero se acerca rápido. Ten movilidad y curación.',
    'La parada es {p:wall}, con la preparación completa.'
  ] },
  { tipo: 'titulo', texto: 'Qué cambia en modo difícil' },
  { tipo: 'parrafo', texto: 'Al derrotar al [[n:Wall of Flesh]] empieza el modo difícil. Lo sagrado y la maldad se extienden. Tras derrotar a los jefes mecánicos aparecen aquí enemigos nuevos, como el [[n:Lava Bat]] y el [[n:Red Devil]]. El Inframundo sigue siendo relativamente tranquilo comparado con otros biomas.' },
  { tipo: 'aviso', estilo: 'nota', texto: 'En modo difícil la piedra infernal se puede destruir con explosivos. Antes, no. Si quieres ir rápido, mira {p:inframundo} para el orden recomendado.' },
  { tipo: 'fuentes', fuentes: [{ wiki: 'terraria', pagina: 'The Underworld' }, { wiki: 'terraria', pagina: 'Hellstone' }, { wiki: 'terraria', pagina: 'Shadow Chest' }, { wiki: 'terraria', pagina: 'Wall of Flesh' }, { wiki: 'terraria', pagina: 'Guide Voodoo Doll' }] }
] },

// ---------------------------------------------------------------------------------------------
{ id: 'jungla', titulo: 'La jungla', subtitulo: 'Superficie y subsuelo, colmenas, Frutas de vida y el santuario de Plantera', icono: 'mapa', bloques: [
  { tipo: 'parrafo', texto: 'La jungla es un bioma de barro, hiedra y árboles de caoba. Cada mundo tiene una, siempre en el lado contrario a la Mazmorra. En la superficie los enemigos ya son más duros que en otros biomas, así que no la visites con equipo de principio. Tu parada es {p:jungla}.' },
  { tipo: 'titulo', texto: 'Superficie frente a subsuelo' },
  { tipo: 'tabla', cabeceras: ['', 'Superficie', 'Subsuelo'], filas: [
    ['Terreno', 'Césped de jungla sobre barro, con caoba y estanques con bambú.', 'Barro, lianas y cavernas con musgo. Aquí están la colmena y el templo.'],
    ['Enemigos', 'Enemigos con más vida y daño que los de otros biomas de superficie.', 'Más fuertes: los [[n:Man Eater]] cuelgan de las hiedras y los [[n:Moss Hornet]] aparecen en las cuevas.'],
    ['Qué buscar', '[[Jungle Spores]] y [[Stinger]].', 'Colmenas, santuarios de hiedra y, en modo difícil, [[Life Fruit]].']
  ] },
  { tipo: 'aviso', estilo: 'nota', texto: 'Para considerarse jungla hace falta una cantidad mínima de bloques propios cerca (unos 140). Entonces cambian la música y los enemigos.' },
  { tipo: 'titulo', texto: 'Materiales de la jungla' },
  { tipo: 'lista', items: [
    '[[Jungle Spores]]: crecen en el césped de jungla, sobre todo en el subsuelo. Sirven para fabricar equipo y accesorios.',
    '[[Stinger]]: suele soltarlo un enemigo con aguijón. Con él se fabrican accesorios y armas.',
    '[[Vine]]: se corta de las lianas colgantes y se usa en recetas.',
    '[[Chlorophyte Ore]]: aparece en el barro de la jungla subterránea cuando llegas al modo difícil. Es el material de la gran armadura de la siguiente etapa.',
    '[[Rich Mahogany]]: la madera de la zona.'
  ] },
  { tipo: 'titulo', texto: 'Colmenas y la Abeja reina' },
  { tipo: 'parrafo', texto: 'En el subsuelo hay **colmenas**: cuevas de bloques de panal con larvas. Al romper una larva aparece la [[n:Queen Bee]]. Es un jefe opcional pero útil antes del modo difícil. Tu parada es {p:bee}.' },
  { tipo: 'lista', items: [
    'Dentro de la colmena el espacio es estrecho. Abre el sitio con un pico y prepara una arena a la medida.',
    'Su botín, como la [[Bee Wax]], se usa para crear equipo de abeja.',
    'Al derrotarla, el [[Witch Doctor]] puede mudarse a tu base. Según la wiki necesita derrotar antes a la Abeja reina y una casa libre.'
  ] },
  { tipo: 'titulo', texto: 'Santuarios de la jungla' },
  { tipo: 'parrafo', texto: 'En la jungla subterránea hay pequeñas estructuras de ladrillo con un **cofre de hiedra** ([[Ivy Chest]]). Se abren sin llave y suelen contener botín de la zona. Revisa cada uno que veas en tus recorridos.' },
  { tipo: 'titulo', texto: 'En modo difícil' },
  { tipo: 'cajas', bloques: [
    { tipo: 'caja', titulo: 'Frutas de vida', bloques: [{ tipo: 'parrafo', texto: 'Aparecen en el césped de la jungla subterránea (o más abajo) en cuanto derrotas **un** jefe mecánico. Cada [[Life Fruit]] da 5 de vida máxima, y solo se pueden usar cuando ya tienes 400 de vida por los [[Life Crystal]]. Con 20 llegas a 500. No hace falta pico para cogerlas. Mira {p:frutas}.' }] },
    { tipo: 'caja', titulo: 'Bulbos de Plantera', bloques: [{ tipo: 'parrafo', texto: 'Tras derrotar a los **tres** jefes mecánicos empiezan a crecer bulbos en el césped de la jungla subterránea. Romper uno invoca a [[n:Plantera]], si estás cerca. Salen uno a uno y reaparecen. Tu parada es {p:plantera}.' }] },
    { tipo: 'caja', titulo: 'Enemigos nuevos', bloques: [{ tipo: 'parrafo', texto: 'La [[n:Giant Tortoise]] y el [[n:Angry Trapper]] vuelven la jungla mucho más peligrosa. Lleva armadura del modo difícil.' }] }
  ] },
  { tipo: 'aviso', estilo: 'destacado', texto: 'Los bulbos solo valen para Plantera. El Templo, en cambio, necesita su [[Temple Key]]. Mira {z:templo} y {a:templo}.' },
  { tipo: 'titulo', texto: 'El Witch Doctor' },
  { tipo: 'parrafo', texto: 'Se muda cuando hay una casa libre y has derrotado a la Abeja reina. Vende, entre otras cosas, la [[Imbuing Station]]. Con el tiempo, y sobre todo tras Plantera, amplía mucho su tienda: las [[Leaf Wings]] solo salen en modo difícil, de noche y en la jungla.' },
  { tipo: 'fuentes', fuentes: [{ wiki: 'terraria', pagina: 'Jungle' }, { wiki: 'terraria', pagina: 'Underground Jungle' }, { wiki: 'terraria', pagina: 'Queen Bee' }, { wiki: 'terraria', pagina: 'Life Fruit' }, { wiki: 'terraria', pagina: "Plantera's Bulb" }, { wiki: 'terraria', pagina: 'Witch Doctor' }] }
] }

] };
