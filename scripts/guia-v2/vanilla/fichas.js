'use strict';
// Guía v2 vanilla (F1): fichas de «Estoy perdido» (problemas) y «He encontrado algo raro» (hallazgos).
// Mecánicas comprobadas contra el código decompilado de tModLoader 1.4.4.9 (Main.cs, NPC.cs, WorldGen.cs,
// Player.cs, ItemDropDatabase.cs) y terraria.wiki.gg. Los nombres de objetos y NPC van siempre con
// [[Nombre inglés]] o {o:}/{n:}: nunca se escribe a mano el nombre español.

const W = (pagina) => ({ wiki: 'terraria', pagina });
const par = (texto) => [{ tipo: 'parrafo', texto }];
const P = (n, titulo, texto, paradas, zonas, paginas) => ({
  id: 'problema' + n, titulo, bloques: par(texto), paradas, zonas, fuentes: paginas.map(W),
});
const H = (n, titulo, texto, paradas, zonas, paginas) => ({
  id: 'hallazgo' + n, titulo, bloques: par(texto), paradas, zonas, fuentes: paginas.map(W),
});

module.exports = {
  articulos: [
    {
      id: 'estructuras', titulo: 'He encontrado algo raro',
      subtitulo: 'Una ficha corta para cada estructura, bloque brillante o personaje atado que te sorprenda',
      icono: 'hallazgo',
      bloques: [
        { tipo: 'parrafo', texto: 'Terraria está lleno de cosas que parecen decoración y no lo son: altares, cofres cerrados, santuarios, vecinos atados bajo tierra. Cada ficha sigue el mismo orden: **lo que ves**, **qué es** y **qué hacer**. Busca la que se parezca a lo que tienes delante; no hace falta leerlas todas.' },
        { tipo: 'aviso', estilo: 'suave', texto: 'Antes de romper o activar algo que no reconoces, pasa el cursor por encima: el juego enseña su nombre. Y si es un cofre con candado, necesita una llave concreta, no un pico.' },
        { tipo: 'lista', items: [
          'Cada ficha enlaza con la parada de la ruta donde te conviene ocuparte de ello.',
          'Lo que está en zonas grandes tiene su propio artículo: {a:mazmorra}, {a:templo}, {a:inframundo}, {a:jungla} y {a:eter}.',
          'Si algo no coincide con ninguna ficha, apunta dónde está y sigue con tu ruta: la guía no te pide encontrarlo todo.',
        ] },
      ],
    },
    {
      id: 'problemas', titulo: 'Estoy perdido',
      subtitulo: 'Cuando algo no sale como esperabas: síntoma y qué comprobar primero',
      icono: 'perdido',
      bloques: [
        { tipo: 'parrafo', texto: 'Casi todos los bloqueos de Terraria tienen una causa simple: una condición que no se cumple, un pico demasiado flojo, un jefe que falta por derrotar. Cada ficha empieza por el **síntoma** tal y como lo notas en el juego y te dice **qué comprobar primero**, en el orden en que suele estar la causa.' },
        { tipo: 'aviso', estilo: 'destacado', texto: 'Si no sabes por dónde seguir, abre «Mi guía»: te muestra siempre la siguiente parada pendiente, con lo que necesitas y qué hacer en orden. Aplaza lo opcional si no te apetece y vuelve cuando quieras.' },
        { tipo: 'parrafo', texto: 'Las casillas marcadas como «sin datos» o «no comprobable» no son un error: significan que aún no has cargado el personaje o el mundo, o que la app no puede saberlo nunca. Márcalas tú a mano cuando las hayas hecho.' },
      ],
    },
  ],

  problemas: [
    P(1, 'No sé qué hacer ahora',
      'Abre «Mi guía»: la siguiente parada pendiente es tu siguiente paso, con lo que necesitas y las tareas en orden. Si es opcional (lleva su etiqueta), puedes aplazarla. Si aún no ves nada claro, recuerda el orden general: casa para vecinos, vida y armadura, primer jefe, Mazmorra y Muro carnoso. Lo demás se ordena a partir de ahí.',
      ['inicio'], [], ['Guide:Walkthrough']),

    P(2, 'No llega ningún vecino',
      'Los vecinos se comprueban cada pocos minutos (unos 7.200 ticks del juego, dos minutos reales a velocidad normal), así que espera con la casa ya hecha. El vecino [[Guide]] llega solo si no hay uno en el mundo; los demás necesitan su condición concreta (mira las fichas siguientes) y una casa válida que esté libre. Que no haya ninguna casa libre es la causa más habitual.',
      ['pueblo'], ['superficie'], ['Housing']),

    P(3, 'Los vecinos no se mudan o mi casa no es válida',
      'Una casa válida es una habitación cerrada con pared de fondo, de **60 a 750 casillas**, con **silla, mesa, luz y puerta**. Un hueco grande en el techo o la pared, un suelo sin superficie plana o una sala demasiado grande la invalidan. Además, con 50 o más casillas de Corrupción, Carmesí o Sagrado dentro, puntúa peor y puede fallar. Usa el icono de vivienda del menú para ver si el juego la acepta.',
      ['pueblo'], ['superficie'], ['Housing']),

    P(4, 'No llega el Mercader',
      'Para que llegue el vecino [[Merchant]] tienes que llevar en el inventario **50 monedas de plata** (5.000 de cobre) como mínimo. Cuenta solo lo que está en las casillas del inventario, no lo guardado en una hucha o un cofre. Pon una casa libre, espera y vuelve a mirar.',
      ['pueblo'], [], ['Merchant']),

    P(5, 'No llega la Enfermera',
      'El vecino [[Nurse]] necesita dos cosas: que el vecino [[Merchant]] ya viva en tu mundo y que algún jugador haya usado al menos un objeto [[Life Crystal]]. Con las dos cosas cumplidas y una casa libre, llegará en cuanto se haga la siguiente comprobación.',
      ['pueblo'], ['cavernas'], ['Nurse']),

    P(6, 'No llegan el Demoledor ni el Traficante de armas',
      'El vecino [[Demolitionist]] exige llevar un explosivo en el inventario (bomba, dinamita...) y que el vecino [[Merchant]] ya esté instalado. El vecino [[Arms Dealer]] exige llevar encima munición de balas o un arma que las use. En ambos casos solo cuenta lo que hay en las casillas del inventario.',
      ['pueblo'], [], ['Demolitionist', 'Arms Dealer']),

    P(7, 'No llega la Driada',
      'El vecino [[Dryad]] llega cuando se ha derrotado un jefe, y sirve cualquiera de los tres primeros: el jefe [[Eye of Cthulhu]], el jefe [[Eater of Worlds]] o [[Brain of Cthulhu]], o el jefe [[Skeletron]]. Si ya has derrotado alguno, solo falta una casa libre.',
      ['pueblo', 'eye'], [], ['Dryad']),

    P(8, 'No llega el Ropero',
      'El vecino [[Clothier]] solo llega después de derrotar al jefe [[Skeletron]]. Antes de eso está maldito: es el vecino [[Old Man]], que merodea por la entrada de la Mazmorra. Aparte de la casa, no necesita nada más.',
      ['skeletron', 'pueblo'], ['mazmorra'], ['Clothier', 'Old Man']),

    P(9, 'No llegan el Mecánico, el Mago, el Duende, el Estilista o el Pescador',
      'Estos vecinos no llegan solos: primero hay que **rescatarlos**. Están atados o dormidos por el mundo y cada uno aparece con su propia condición (por ejemplo, el vecino [[Mechanic]] solo se encuentra tras derrotar al jefe [[Skeletron]], dentro de la Mazmorra). Mira la ficha de vecinos atados en «He encontrado algo raro».',
      ['pueblo', 'mazmorra'], ['mazmorra', 'cavernas'], ['Mechanic', 'Goblin Tinkerer', 'Wizard', 'Stylist', 'Angler']),

    P(10, 'Faltan vecinos del modo difícil',
      'Tras entrar en modo difícil, algunos vecinos piden un hito concreto: [[Steampunker]] llega al derrotar a cualquier jefe mecánico; [[Cyborg]] exige modo difícil y haber derrotado a [[Plantera]]; [[Witch Doctor]] pide haber derrotado a [[Queen Bee]]; [[Pirate]] pide haber derrotado la Invasión pirata. Todos necesitan además una casa libre.',
      ['hardstart', 'pirates', 'plantera'], [], ['Housing']),

    P(11, 'Me faltan vecinos aunque cumplo las condiciones',
      'Algunos vecinos dependen de **cuántos vecinos ya tienes**: [[Painter]] necesita 8 o más, [[Dye Trader]] necesita 4 o más y haber llevado encima algún tinte, y [[Party Girl]] solo tiene una probabilidad baja de llegar cada comprobación cuando hay 14 o más. Amplía el pueblo con casas libres y deja pasar el tiempo.',
      ['pueblo'], [], ['Housing']),

    P(12, 'No aparece el Ojo de Cthulhu',
      'Por sí solo aparece al empezar una noche, con probabilidad de 1 entre 3, si algún jugador ha usado **al menos 5 cristales de vida**, tiene **más de 10 de defensa** y hay **4 o más vecinos** en el mundo; además no puede estar derrotado ya. Para adelantarlo, usa de noche el objeto [[Suspicious Looking Eye]] en la superficie. La ficha del objeto dice cómo conseguirlo.',
      ['eye', 'pueblo'], ['superficie'], ['Eye of Cthulhu']),

    P(13, 'No puedo picar la piedra del Inframundo, la corrupta ni el meteorito',
      'Es cuestión de la potencia del pico. El objeto [[Meteorite]] necesita **50**; [[Demonite Ore]] y [[Crimtane Ore]], bajo la superficie, **55**; las piedras malignas y la [[Hellstone]], **65**. En modo difícil: cobalto y paladio, 100; mithril y oricalco, 110; adamantita y titanio, 150. Mejora el pico antes de perder tiempo.',
      ['evil', 'meteorito', 'inframundo'], ['meteorito', 'corrupcion', 'carmesi', 'inframundo'], ['Pickaxe power']),

    P(14, 'No cae ningún meteorito',
      'El meteorito solo puede caer **después de derrotar al jefe del mal del mundo**: [[Eater of Worlds]] o [[Brain of Cthulhu]]. Desde entonces hay una probabilidad de 1 entre 50 al empezar cada noche. No se puede forzar: sigue con otras paradas y mira el mapa de vez en cuando.',
      ['meteorito', 'evil'], ['meteorito'], ['Meteorite']),

    P(15, 'No sale el Devoramundos ni el Cerebro de Cthulhu',
      'Estos jefes se invocan **rompiendo orbes sombríos o corazones carmesíes**. Al romper el tercero, aparece el jefe: [[Eater of Worlds]] en la Corrupción o [[Brain of Cthulhu]] en el Carmesí. Si hay otro de los dos vivo, el contador espera. Comprueba que estás en el mal que corresponde a tu mundo, que solo tiene uno de los dos.',
      ['evil'], ['corrupcion', 'carmesi'], ['Eater of Worlds', 'Brain of Cthulhu']),

    P(16, 'No encuentro la Mazmorra',
      'Está en **uno de los lados del mapa**, entre el Océano y el centro. Mira el mapa o usa la marca de «Mi guía». En su entrada, de noche, merodea el vecino [[Old Man]]: hablar con él invoca al jefe [[Skeletron]]. No bajes a ella antes de derrotarlo.',
      ['mazmorra', 'skeletron'], ['mazmorra'], ['Dungeon']),

    P(17, 'El Guardián de la Mazmorra me mata',
      'El jefe [[Dungeon Guardian]] aparece de forma natural dentro de la Mazmorra **mientras no hayas derrotado a [[Skeletron]]**. No está pensado para vencerlo: sal y vuelve cuando Skeletron haya caído. Si ya estás dentro, huye por donde entraste.',
      ['skeletron', 'mazmorra'], ['mazmorra'], ['Dungeon Guardian']),

    P(18, 'Esqueletrón no aparece',
      'No se invoca con un objeto: habla con el vecino [[Old Man]] en la entrada de la Mazmorra **de noche**. Si es de día, espera. Prepara antes una arena con plataformas. Una vez derrotado, el vecino queda curado y ya no puede invocarlo.',
      ['skeletron'], ['mazmorra'], ['Skeletron', 'Old Man']),

    P(19, 'No tengo el Muñeco vudú del Guía',
      'El objeto [[Guide Voodoo Doll]] lo suelta el enemigo [[Voodoo Demon]], del Inframundo. Ojo: para usarlo hay que tener a un vecino [[Guide]] vivo, porque al caer el muñeco en la lava el Guía muere en el acto y se invoca al jefe. Si no tienes el muñeco, caza demonios en el Inframundo.',
      ['wall', 'inframundo'], ['inframundo'], ['Guide Voodoo Doll']),

    P(20, 'No puedo invocar el Muro carnoso',
      'Hace falta el objeto [[Guide Voodoo Doll]], un vecino [[Guide]] vivo y **lava del Inframundo**: el muñeco tiene que caer en la lava a la profundidad del propio Inframundo, y no puede haber otro Muro ya activo. Si el Guía ha muerto, espera a que vuelva a llegar a su casa.',
      ['wall', 'inframundo'], ['inframundo'], ['Wall of Flesh']),

    P(21, 'El Muro carnoso me alcanza o no puedo con él',
      'El jefe [[Wall of Flesh]] avanza hacia ti por el Inframundo y se acelera según pierde vida. Necesitas un **puente largo y recto** de plataformas sobre la lava, pociones de curación y un arma a distancia. Si tu daño no basta, vuelve más fuerte: no hay prisa, el Muro no se pierde.',
      ['wall', 'armapre'], ['inframundo'], ['Wall of Flesh']),

    P(22, 'El mundo se llena de Corrupción o Sagrado en modo difícil',
      'Cuando cae el jefe [[Wall of Flesh]], el mal y Lo Sagrado se propagan más deprisa. No se evita del todo, pero se contiene: cava un **foso de al menos 3 casillas de ancho** que corte el avance, usa el objeto [[Purification Powder]] sobre lo ya infectado o un [[Clentaminator]] con su solución. Protege primero tu pueblo.',
      ['hardstart', 'sagrado'], ['sagrado', 'corrupcion', 'carmesi'], ['Corruption', 'Clentaminator']),

    P(23, 'No puedo romper los altares',
      'Los altares demoníacos y carmesíes solo se rompen en **modo difícil** y con un martillo de **80 de potencia o más**, como el objeto [[Pwnhammer]]. Cada tres altares rotos aparece un mineral nuevo en el mundo: cobalto o paladio, luego mithril u oricalco y luego adamantita o titanio.',
      ['hardstart'], ['altares'], ['Altar']),

    P(24, 'No salen frutas de vida',
      'El objeto [[Life Fruit]] crece en la **jungla subterránea**, sobre hierba de jungla y bajo tierra, y solo si ya has derrotado **al menos un jefe mecánico**. Sale de forma aleatoria y poco frecuente: recorre la jungla con buena luz y busca plantas de hoja ancha.',
      ['frutas', 'mech1'], ['jungla_subterranea', 'jungla'], ['Life Fruit']),

    P(25, 'No aparecen bulbos de Plantera',
      'El bulbo solo empieza a crecer cuando están **derrotados los tres jefes mecánicos**, en modo difícil. Sale en la jungla subterránea, con probabilidad baja en cada comprobación. Cuando lo veas, no lo rompas hasta estar preparado: invoca al jefe [[Plantera]].',
      ['plantera', 'mech3'], ['jungla_subterranea'], ['Plantera']),

    P(26, 'No tengo la llave del Templo',
      'El objeto [[Temple Key]] lo suelta el jefe [[Plantera]]: en Clásico, directamente; en Experto y Maestro, dentro de su bolsa del tesoro. Una vez abierta, la puerta queda abierta para todos los jugadores del mundo.',
      ['plantera', 'templo'], ['templo'], ['Jungle Temple', 'Temple Key']),

    P(27, 'No puedo picar los ladrillos del Templo',
      'Los ladrillos lihzahrd solo se rompen con el objeto [[Picksaw]], que suelta el jefe [[Golem]]. Antes de eso, la forma normal de entrar es la puerta, que se abre con la llave que suelta [[Plantera]].',
      ['templo', 'golem'], ['templo'], ['Jungle Temple', 'Picksaw']),

    P(28, 'El Eclipse no llega',
      'Solo puede ocurrir en modo difícil y con **al menos un jefe mecánico** derrotado. Al empezar cada día hay 1 probabilidad entre 20. Para forzarlo existe el objeto [[Solar Tablet]], que se crea con fragmentos de tablilla. Mientras tanto, sigue con otras paradas.',
      ['eclipse', 'mech1'], [], ['Solar Eclipse']),

    P(29, 'No consigo activar a Golem',
      'El jefe [[Golem]] se invoca en el altar lihzahrd, dentro del Templo, con el objeto [[Lihzahrd Power Cell]]. La célula no se fabrica: la suelta con poca probabilidad (1 entre 50) cada enemigo lagarto del Templo. Prepara la arena antes de usarla.',
      ['golem', 'templo'], ['templo'], ['Golem', 'Lihzahrd Power Cell']),

    P(30, 'El Sectario no aparece',
      'El jefe [[Lunatic Cultist]] solo puede aparecer cuando [[Skeletron]] y [[Golem]] están derrotados. Entonces un grupo de sectarios se reúne a la entrada de la Mazmorra; al matar a los cuatro, el jefe sale de la tablilla. Si te teletransportas durante la animación, tendrás que esperar unas 12 horas del juego.',
      ['cultist', 'golem'], ['mazmorra'], ['Lunatic Cultist']),

    P(31, 'Las columnas no bajan su escudo',
      'Cada Torre celestial exige **matar 100 enemigos propios** para bajar su escudo (50 si ya has derrotado al jefe [[Moon Lord]]). Solo cuentan los de esa columna, que aparecen a su alrededor: lucha cerca de ella. Con el escudo abajo, la torre se vuelve vulnerable.',
      ['pillars'], [], ['Celestial Pillars']),

    P(32, 'El Señor de la Luna me derrite',
      'Es el jefe final. Llega con la vida al máximo y el mejor equipo de fragmentos que tengas, una arena larga de plataformas, pociones y curación a mano. Si no avanzas, vuelve a las paradas de equipo y mejoras permanentes antes de insistir.',
      ['moonlord', 'pillars', 'mejoras'], [], ['Moon Lord']),

    P(33, 'Me quedo sin dinero',
      'Vende lo que sobra a los vecinos: gemas, barras, objetos de cofres. Los cofres, los jefes y los eventos sueltan monedas. No gastes en reforjar hasta tener el arma definitiva, porque reforjar cuesta cada vez que cambias de arma.',
      ['inicio', 'pueblo'], [], ['Money']),

    P(34, 'Me faltan huecos de accesorio',
      'Hay **5 huecos de base** en todos los modos. En Experto y Maestro, el objeto [[Demon Heart]] desbloquea un sexto (lo da la bolsa del tesoro del jefe [[Wall of Flesh]] si aún no lo has usado). En Maestro hay un séptimo que se activa solo. En Clásico y Viaje no hay más de cinco.',
      ['wall', 'mejoras'], [], ['Accessories']),

    P(35, 'No sé cuál es mi clase',
      'Elige según el arma con la que más daño haces: espada o lanza (cuerpo a cuerpo), arco o arma de fuego (distancia), vara o libro (magia) o sirvientes (invocación). En «Mi guía» puedes cambiarla cuando quieras; la guía solo la usa para mostrar la escalera de equipo y consejos.',
      ['inicio'], [], ['Guide:Class setups']),

    P(36, 'La guía marca una tarea como no comprobable',
      'Hay dos motivos. «Sin datos»: aún no has cargado el personaje o el mundo, y se resuelve al hacerlo. «No comprobable»: la app no puede saberlo nunca con lo que guarda el juego. Márcala a mano cuando la hayas hecho; ese progreso manual se guarda con tu personaje.',
      ['inicio'], [], []),

    P(37, 'No encuentro el Éter',
      'El Éter está siempre **en el lado del mundo opuesto a la Mazmorra**, muy cerca del borde (el juego lo coloca en el 11 % exterior del mapa), bajo la capa de subsuelo. Tiene un gran lago de «fulgor», un líquido brillante. La guía marca su zona aproximada en el mapa; baja con luz y gancho. Es opcional.',
      ['eter'], ['eter'], ['Aether']),

    P(38, 'No encuentro la colmena',
      'La colmena está en la **jungla subterránea**, con paredes de abeja y miel. Entra por arriba o abre un túnel; al romper la larva se invoca al jefe [[Queen Bee]], y también puedes usar el objeto [[Abeemination]]. Prepara antes una arena amplia.',
      ['bee', 'jungla'], ['colmena', 'jungla_subterranea'], ['Queen Bee', 'Hive']),

    P(39, 'No encuentro el Templo',
      'El Templo está en la **jungla subterránea**, una gran estructura de ladrillos lihzahrd. Sin la llave de [[Plantera]] no se abre, pero ubícalo ya desde fuera. Mira el artículo {a:templo}.',
      ['templo', 'mazmorra2'], ['templo', 'jungla_subterranea'], ['Jungle Temple']),

    P(40, 'No tengo vida suficiente para el siguiente jefe',
      'Usa el objeto [[Life Crystal]] hasta llegar a 400 de vida (15 cristales en total) y, en modo difícil, el objeto [[Life Fruit]] hasta 500 (20 frutas). Mira la «vida objetivo» de la parada: es una guía, no un muro.',
      ['hardstart', 'frutas'], ['cavernas'], ['Life Crystal', 'Life Fruit']),

    P(41, 'Un cofre está cerrado y no se abre con el pico',
      'Los cofres con candado necesitan una **llave concreta**: el objeto [[Golden Key]] para los cofres cerrados de la Mazmorra, el objeto [[Shadow Key]] para los del Inframundo y las llaves de bioma para los cofres de bioma de la Mazmorra (estas solo funcionan tras derrotar a [[Plantera]]).',
      ['mazmorra', 'inframundo', 'mazmorra2'], ['mazmorra', 'inframundo'], ['Biome Chest', 'Shadow Key']),

    P(42, 'Un enemigo no me da el objeto que busco',
      'Abre primero la ficha del objeto: dice de qué enemigo o jefe sale y con qué probabilidad. Algunos exigen un bioma, un evento o haber derrotado antes a un jefe. Si la probabilidad es baja, ve a donde haya más enemigos de ese tipo (un evento o su bioma) y ten paciencia.',
      ['inicio'], [], ['Drop']),

    P(43, 'El Tabernero o el Duque Fishron no aparecen',
      'El vecino [[Tavernkeep]] no llega solo: hay que **rescatarlo** inconsciente, bajo tierra, tras derrotar a [[Eater of Worlds]] o [[Brain of Cthulhu]]. Al jefe [[Duke Fishron]] se le invoca en el Océano con el objeto [[o:Truffle Worm]], ya en modo difícil. Ambas paradas son opcionales.',
      ['dd2t1', 'fishron'], ['oceano'], ['Tavernkeep', 'Duke Fishron']),

    P(44, 'Me equivoqué al marcar una parada',
      'No pasa nada: el progreso manual se guarda aparte del juego. Desmarca la tarea o la parada y la app recalcula la siguiente. Lo automático se recalcula solo al recargar el personaje o el mundo.',
      ['inicio'], [], []),

    P(45, 'No sé si me dejo algo opcional',
      'Las paradas opcionales mejoran tu equipo o abren vecinos, pero no son obligatorias. Aplázalas y sigue la ruta principal; en «Mi guía» las encontrarás cuando vuelvas. Al acabar, repasa las que te hayan quedado antes del Señor de la Luna.',
      ['postmoon', 'mejoras'], [], ['Guide:Walkthrough']),
  ],

  hallazgos: [
    H(1, 'Una bola morada o roja brillante bajo tierra',
      '**Qué es:** el objeto [[Shadow Orb]] si estás en la Corrupción, o un corazón carmesí ([[Crimson Heart]]) si estás en el Carmesí. Dan un objeto cada vez que los rompes y, al romper el **tercero**, invocan al jefe [[Eater of Worlds]] o [[Brain of Cthulhu]]. **Qué hacer:** rómpelos con un martillo o explosivos cuando estés listo para el jefe, no antes.',
      ['evil'], ['corrupcion', 'carmesi'], ['Shadow Orb', 'Crimson Heart']),

    H(2, 'Un altar de piedra con símbolos demoníacos o carmesíes',
      '**Qué es:** un altar demoníaco o carmesí, fijo en el mundo. En modo difícil, romperlo con un martillo de 80 de potencia o más (como el objeto [[Pwnhammer]]) hace aparecer un mineral nuevo. **Qué hacer:** no lo rompas hasta querer el mineral; cada tres altares rotos sale una tanda nueva (cobalto o paladio, mithril u oricalco, adamantita o titanio).',
      ['hardstart', 'evil'], ['altares'], ['Altar']),

    H(3, 'Un cofre con candado en la Mazmorra o en el Inframundo',
      '**Qué es:** un cofre cerrado. Los de la Mazmorra se abren con el objeto [[Golden Key]] (la suelta el enemigo [[Dungeon Slime]]) y los del Inframundo con el objeto [[Shadow Key]], que a veces sale (1 de cada 3) de la caja de seguridad {o:Terraria/LockBox}. **Qué hacer:** busca la llave; el pico no sirve de nada contra el candado.',
      ['mazmorra', 'inframundo'], ['mazmorra', 'inframundo'], ['Shadow Key', 'Golden Key']),

    H(4, 'Una espada clavada en una piedra bajo tierra',
      '**Qué es:** un santuario de espada encantada, una pequeña estructura de las Cavernas con un objeto dentro ([[o:Enchanted Sword]]). No invoca nada. **Qué hacer:** recoge el objeto y sigue; no está en todos los mundos, depende de cómo se generó el tuyo.',
      ['subsuelo'], ['cavernas'], ['Enchanted Sword Shrine']),

    H(5, 'Un gran árbol hueco en la superficie',
      '**Qué es:** un árbol vivo, una estructura del bosque con el interior hueco. **Qué hacer:** no invoca ni esconde ningún jefe. Si quieres usarlo de casa, tiene que cumplir las reglas de vivienda (60 a 750 casillas, silla, mesa, luz y puerta); compruébalo con el icono de vivienda.',
      ['pueblo'], ['superficie'], ['Living Tree']),

    H(6, 'Una pirámide en el desierto',
      '**Qué es:** una pirámide del Desierto, con cámaras interiores y un cofre con botín. No invoca ningún jefe. **Qué hacer:** entra con luz y vida de sobra y vacía el cofre. Anota dónde está por si quieres volver con mejor equipo.',
      ['subsuelo'], ['desierto', 'desierto_subterraneo'], ['Pyramid']),

    H(7, 'Cuevas llenas de telarañas y paredes grises',
      '**Qué es:** un Nido de arañas, un bioma subterráneo con enemigos como [[Wall Creeper]] y [[Black Recluse]] y, a veces, un vecino [[Webbed Stylist]] atrapado. **Qué hacer:** entra con luz y arma de sobra; si ves a un vecino atado en la telaraña, rescátalo y ten lista una casa.',
      ['pueblo'], ['cavernas'], ['Spider Nest']),

    H(8, 'Zonas de piedra oscura y púrpura, o de piedra blanca con columnas',
      '**Qué es:** los biomas de Granito y de Mármol, mini-biomas de las Cavernas con enemigos propios: [[Granite Golem]] en el granito y [[Medusa]] en el mármol, en modo difícil. Se reconocen por los bloques [[Granite Block]] y [[Marble Block]]. **Qué hacer:** son opcionales; explora con vida alta y luz.',
      ['subsuelo'], ['granito', 'marmol'], ['Granite', 'Marble']),

    H(9, 'Islas flotantes en el cielo',
      '**Qué es:** islas del Cielo con cofres Skyware, que guardan objetos como el objeto [[Starfury]], el objeto [[Shiny Red Balloon]] y el objeto [[Lucky Horseshoe]]. **Qué hacer:** llega con gancho o con cohetes, entra por el interior de la isla y vacía el cofre. Cuidado con los enemigos [[Harpy]] que la patrullan.',
      ['movilidad'], ['cielo'], ['Sky Island']),

    H(10, 'Un lago brillante y multicolor bajo tierra',
      '**Qué es:** el Éter, con su líquido llamado fulgor. Se encuentra en el quinto más externo del mundo, en el mismo lado que la jungla. **Qué hacer:** es opcional; baja con luz y no tires objetos importantes al líquido, porque lo transforma.',
      ['eter'], ['eter'], ['Aether']),

    H(11, 'Un cofre que se mueve y muerde',
      '**Qué es:** un enemigo [[Mimic]], propio del modo difícil, que finge ser un cofre. Suelta objetos como [[Dual Hook]], [[Magic Dagger]], [[Philosopher\'s Stone]], [[Titan Glove]], [[Star Cloak]] o [[Cross Necklace]]. **Qué hacer:** pelea con espacio y vida alta: el botín merece la pena.',
      ['hardstart'], ['cavernas'], ['Mimic']),

    H(12, 'Una planta rosa brillante en la jungla subterránea',
      '**Qué es:** un bulbo de Plantera. Solo aparece tras derrotar a los tres jefes mecánicos, en modo difícil, sobre hierba de jungla. **Qué hacer:** prepara una arena amplia y rómpelo cuando estés listo: invoca al jefe [[Plantera]].',
      ['plantera'], ['jungla_subterranea'], ['Plantera']),

    H(13, 'Un cristal rojo en las cavernas o una fruta en la jungla',
      '**Qué es:** el objeto [[Life Crystal]] (en las cavernas) o el objeto [[Life Fruit]] (en la jungla subterránea, solo tras un jefe mecánico). Cada uno aumenta tu vida máxima de forma permanente. **Qué hacer:** recógelos y úsalos: no se gastan ni se pierden al morir.',
      ['subsuelo', 'frutas'], ['cavernas', 'jungla_subterranea'], ['Life Crystal', 'Life Fruit']),

    H(14, 'Cofres especiales dentro de la Mazmorra',
      '**Qué es:** cofres cerrados de bioma, uno por bioma: jungla, Corrupción, Carmesí, Sagrado, hielo y desierto. Se abren con su llave ([[Jungle Key]], [[Corruption Key]], [[Crimson Key]], [[Hallowed Key]], [[Frozen Key]] y [[o:Desert Key]]), que sueltan con muy poca probabilidad los enemigos de ese bioma en modo difícil, y solo funcionan tras derrotar a [[Plantera]]. **Qué hacer:** guarda la llave para abrir el cofre y llevarte su arma única.',
      ['mazmorra2'], ['mazmorra'], ['Biome Chest']),

    H(15, 'Un grupo de lápidas con música propia',
      '**Qué es:** muchas lápidas ([[Tombstone]]) juntas forman el bioma de Cementerio, con su música y sus enemigos. **Qué hacer:** no hace falta para progresar; es una curiosidad.',
      ['pueblo'], ['superficie'], ['Graveyard']),

    H(16, 'Un vecino atado o dormido bajo tierra',
      '**Qué es:** un vecino por rescatar. [[Bound Goblin]] aparece bajo tierra tras vencer la Invasión duende; [[Bound Wizard]], bajo tierra en modo difícil; [[Bound Mechanic]], en la Mazmorra tras derrotar a [[Skeletron]]; [[Webbed Stylist]], en un Nido de arañas; [[Sleeping Angler]], dormido en la playa del Océano. El vecino [[n:Tavernkeep]] sale inconsciente tras vencer a [[Eater of Worlds]] o [[Brain of Cthulhu]]. **Qué hacer:** háblales para rescatarlos y dales una casa libre.',
      ['pueblo', 'goblins', 'mazmorra'], ['cavernas', 'mazmorra', 'oceano'], ['Bound Goblin', 'Bound Mechanic', 'Webbed Stylist']),
  ],
};
