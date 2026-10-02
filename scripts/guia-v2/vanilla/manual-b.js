'use strict';
// Guia v2 vanilla (F1) - manual, parte B: maldad, eventos, vecinos, vida, pesca, materiales, eter, fuentes.
// Referencia: Terraria 1.4.4.9. Mecanicas comprobadas contra el codigo decompilado de tModLoader 1.4.4.9
// (Main.cs, NPC.cs, Player.cs, WorldGen.cs, Chest.cs, Projectile.cs, Item.cs, ItemDropDatabase.cs, DD2Event.cs,
// ShopHelper.cs, ShimmerTransforms.cs, TeleportPylonsSystem.cs). Lo que solo consta en terraria.wiki.gg se indica en el texto.
module.exports = { articulos: [

  // ------------------------------------------------------------------ MALDAD
  { id: 'maldad', titulo: 'La Corrupción y El Carmesí', subtitulo: 'Qué cambia entre las dos, qué sueltan sus orbes y cómo frenarlas', icono: 'mapa', bloques: [
    { tipo: 'parrafo', texto: 'Todo mundo trae una maldad: {z:corrupcion} o {z:carmesi}. Se decide al crear el mundo. Las dos juegan igual y te piden lo mismo; cambian el aspecto, los enemigos y los objetos. Esta guía vale para ambas, y donde difieren lo verás en una tabla. Tu ruta para este jefe está en {p:evil}.' },
    { tipo: 'tabla', cabeceras: ['', 'La Corrupción', 'El Carmesí'], filas: [
      ['Mineral', '[[Demonite Ore]]', '[[Crimtane Ore]]'],
      ['Barra (3 minerales en un horno)', '[[Demonite Bar]]', '[[Crimtane Bar]]'],
      ['Jefe', '[[n:Eater of Worlds]]', '[[Brain of Cthulhu]]'],
      ['Invocación manual (en un altar)', '[[Worm Food]]: 30 [[Vile Powder]] y 15 [[Rotten Chunk]]', '[[Bloody Spine]]: 30 [[Vicious Powder]] y 15 [[Vertebra]]'],
      ['Material que suelta el jefe', '[[Shadow Scale]]', '[[Tissue Sample]]'],
      ['Armadura de ejemplo', '[[Shadow Scalemail]]: 25 [[Demonite Bar]] y 20 [[Shadow Scale]]', '[[Crimson Scalemail]]: 25 [[Crimtane Bar]] y 20 [[Tissue Sample]]'],
      ['Piedra propia', '[[Ebonstone Block]]', '[[Crimstone Block]]'],
      ['Lo que hay que romper', 'Orbes sombríos', 'Corazones carmesíes']
    ] },
    { tipo: 'titulo', texto: 'Orbes sombríos y corazones carmesíes' },
    { tipo: 'parrafo', texto: 'Están en las cavidades de la maldad, bajo tierra, y se rompen con un martillo. Cada uno que rompes tiene tres consecuencias distintas. Conviene conocerlas antes de empezar a destrozarlos.' },
    { tipo: 'cajas', bloques: [
      { tipo: 'caja', titulo: 'Qué suelta cada uno', bloques: [
        { tipo: 'parrafo', texto: 'El primero que rompes en todo el mundo suelta siempre lo mismo: [[Musket]] y 100 [[Musket Ball]] en la Corrupción, [[The Undertaker]] y 100 [[Musket Ball]] en el Carmesí.' },
        { tipo: 'parrafo', texto: 'Desde el segundo, el botín sale al azar entre cinco opciones. Corrupción: [[Musket]], [[Vilethorn]], [[Ball O\' Hurt]], [[Shadow Orb]] y [[Band of Starpower]]. Carmesí: [[The Undertaker]], [[Crimson Rod]], [[The Rotted Fork]], [[Crimson Heart]] y [[Panic Necklace]].' }
      ] },
      { tipo: 'caja', titulo: 'Cada tercer orbe llama al jefe', bloques: [
        { tipo: 'parrafo', texto: 'Al romper el tercero, el juego invoca al jefe de la maldad sobre el jugador más cercano (si no hay ya uno vivo). Tras el primero y el segundo recibes un aviso por chat. Si aún no estás listo, para en dos.' },
        { tipo: 'parrafo', texto: 'También puedes llamarlo tú cuando quieras con el objeto de invocación de la tabla, que se fabrica en un altar ({z:altares}).' }
      ] },
      { tipo: 'caja', titulo: 'Efectos en el mundo', bloques: [
        { tipo: 'lista', items: [
          '**Invasión duende.** En cuanto rompes cualquier orbe o corazón, cada amanecer hay 1 posibilidad entre 3 de que llegue, hasta que la derrotes una vez. Solo empieza si algún jugador tiene 5 [[Life Crystal]] usados (200 de vida). Su tamaño es 80 más 40 por cada jugador que cumpla eso.',
          '**Meteorito.** Cuando derrotas al jefe de la maldad, el juego programa una caída de {z:meteorito}: siempre la primera vez y con un 50 % las siguientes. Además, desde entonces cada noche tiene 1 posibilidad entre 50 de traer otra. Recibes un aviso por chat.'
        ] }
      ] }
    ] },
    { tipo: 'aviso', estilo: 'destacado', texto: 'Romper el primer orbe ya activa las invasiones duende. Si aún no tienes 200 de vida no pasa nada (no empiezan), pero en cuanto los tengas puede llegar una al amanecer. Ten a mano {p:goblins} antes de pasar de esa vida.' },
    { tipo: 'titulo', texto: 'Los jefes' },
    { tipo: 'cajas', bloques: [
      { tipo: 'caja', titulo: '[[n:Eater of Worlds]]', bloques: [
        { tipo: 'parrafo', texto: 'Un gusano largo, hecho de segmentos. Cuando lo cortas, cada trozo sigue vivo por su cuenta. Pelea en un tramo amplio y abierto, con armas que alcancen lejos y plataformas bajo los pies; en un túnel estrecho te estorba él solo.' }
      ] },
      { tipo: 'caja', titulo: '[[Brain of Cthulhu]]', bloques: [
        { tipo: 'parrafo', texto: 'Empieza rodeado de pequeños enemigos y, a mitad del combate, cambia de fase y se teletransporta hacia ti. Necesita un suelo despejado: construye una pista larga de plataformas y acaba antes con sus secuaces.' }
      ] }
    ] },
    { tipo: 'parrafo', texto: 'Los minerales de la maldad exigen un pico de al menos 55 de potencia (bajo la superficie). Qué pico abre cada mineral está en {a:materiales}.' },
    { tipo: 'titulo', texto: 'Modo difícil: la maldad se expande' },
    { tipo: 'parrafo', texto: 'Al derrotar al [[Wall of Flesh]] empieza el modo difícil: aparece {z:sagrado} y las zonas de maldad y de Lo Sagrado empiezan a crecer. Según la wiki, antes del modo difícil solo se extiende la hierba de cada bioma; en modo difícil convierten los bloques que tienen cerca (hasta 3 casillas de distancia), así que cruzan piedra, arena y hielo.' },
    { tipo: 'lista', items: [
      '**Barrera.** Un tramo de tres casillas de ancho, vacío o de bloques que no se convierten, frena el avance (wiki). Un túnel o un foso de ese ancho entre tu base y el borde de la zona basta.',
      '**Altares.** Romper un altar solo funciona en modo difícil y suelta mineral: cobalto o paladio, luego mitrilo u oricalco, luego adamantita o titanio, según cuántos altares hayas roto (la variante de cada tier se decide por mundo). Ver {z:altares}.',
      '**Piratas.** Con algún altar roto, la Invasión pirata puede empezar al amanecer (ver {a:eventos}).'
    ] },
    { tipo: 'tabla', cabeceras: ['Herramienta', 'Quién la da', 'Qué hace'], filas: [
      ['[[Purification Powder]]', '[[Dryad]]', 'Limpia la maldad de la zona a la que lo lanzas. Lleva un buen puñado.'],
      ['[[Clentaminator]]', '[[Steampunker]]', 'Crea y destruye biomas al rociar. Necesita solución de color como munición.'],
      ['[[Green Solution]]', '[[Steampunker]]', 'Con el Clentaminator, extiende el terreno puro: es la que sirve para limpiar. La vende de normal.'],
      ['[[Blue Solution]]', '[[Steampunker]]', 'Es la que te vende si estás en Lo Sagrado, y extiende Lo Sagrado: no sirve para limpiar.'],
      ['[[Red Solution]] / [[Purple Solution]]', '[[Steampunker]]', 'Solo las vende en una Luna de Sangre o un Eclipse: la roja en mundo carmesí y la morada en mundo corrupto. Extienden la maldad. No las compres.']
    ] },
    { tipo: 'parrafo', texto: 'El [[Holy Water]] y el [[Unholy Water]] convierten bloques al salpicar, a Lo Sagrado y a la maldad respectivamente: no los lances donde no quieras ese bioma.' },
    { tipo: 'flujo', items: ['Mira por dónde avanza la zona', 'Corta con un túnel de 3 o más casillas', 'Limpia el borde con [[Purification Powder]]', 'Con el [[Clentaminator]], rocía [[Green Solution]]'] },
    { tipo: 'titulo', texto: 'Almas de luz y de noche' },
    { tipo: 'parrafo', texto: 'En modo difícil, los enemigos que matas en el subsuelo y las cavernas de Lo Sagrado sueltan [[Soul of Light]]. Los que matas en la Corrupción o el Carmesí subterráneos sueltan [[Soul of Night]]. En ambos casos, con una probabilidad de 1 entre 5 por enemigo. Las necesitas para muchas armas, alas y piezas de la etapa, así que no limpies la zona del todo: déjate un rincón donde cultivarlas.' },
    { tipo: 'aviso', estilo: 'nota', texto: 'El Clentaminator también extiende zonas. Si rocías [[Blue Solution]] estás creando Lo Sagrado allí donde apuntas, aunque sea en mitad de tu mundo.' },
    { tipo: 'fuentes', fuentes: [{ wiki: 'terraria', pagina: 'The Corruption' }, { wiki: 'terraria', pagina: 'The Crimson' }, { wiki: 'terraria', pagina: 'Shadow Orb' }, { wiki: 'terraria', pagina: 'Biome spread' }, { wiki: 'terraria', pagina: 'Clentaminator' }] }
  ] },

  // ------------------------------------------------------------------ EVENTOS
  { id: 'eventos', titulo: 'Eventos e invasiones', subtitulo: 'Cuándo llegan, cómo provocarlos y qué dejan', icono: 'evento', bloques: [
    { tipo: 'parrafo', texto: 'Un evento es algo que pasa en el mundo durante un rato: unos llegan solos y otros se provocan con un objeto. Casi todos sirven para algo concreto, ya sea un vecino, un material o una moneda propia. Los que no necesites puedes saltártelos: la ruta marca cuáles son opcionales.' },
    { tipo: 'tabla', cabeceras: ['Evento', 'Cuándo', 'Cómo provocarlo', 'Qué deja'], filas: [
      ['Luna de Sangre', 'Al anochecer, 1 vez de cada 9 (nunca con luna nueva), si algún jugador tiene 2 [[Life Crystal]] usados (120 de vida).', '[[Bloody Tear]].', 'Enemigos propios. Pescando con la captura más rara, 1 posibilidad entre 3 de [[Advanced Combat Techniques]] (si aún no lo usaste). En modo difícil, el [[Skeleton Merchant]] vende [[Slap Hand]]. Ver {p:lunasangre}.'],
      ['Lluvia de slime', 'De día, por la mañana, sin lluvia: con 3 [[Life Crystal]] usados y más de 8 de defensa, o en Experto y superior sin ese requisito.', 'No se invoca.', 'A los 150 slimes muertos (75 si ya derrotaste al Rey slime) aparece [[King Slime]]. La [[Slime Crown]] es la forma manual de llamarlo.'],
      ['Tormenta de arena', 'En {z:desierto}, sola, cuando sopla viento fuerte.', 'No se invoca.', 'Enemigos propios del desierto.'],
      ['Ventisca', 'En {z:nieve}, sola, durante la lluvia (según la wiki).', 'No se invoca.', 'Enemigos propios de la nieve.'],
      ['Fiesta', 'De día, si vive en el pueblo la [[Party Girl]]: 1 posibilidad entre 10 al día (tras 5 a 10 días de espera) y con al menos 5 vecinos que puedan celebrar.', '[[Party Center]].', 'Tarta gratis y vecinos de fiesta.'],
      ['Invasión duende', 'Al amanecer: 1 entre 3 mientras no la hayas vencido y hayas roto algún orbe; después, 1 entre 30 (1 entre 60 en modo difícil). Requiere 5 [[Life Crystal]] usados.', '[[Goblin Battle Standard]]: 10 [[Tattered Cloth]] y 5 de cualquier madera en un [[Loom]].', 'Tras vencerla aparece atado en las cavernas el [[Goblin Tinkerer]]. Ver {p:goblins}.'],
      ['Ejército del Antiguo (niveles 1, 2 y 3)', 'Solo si lo inicias tú.', '[[o:Eternia Crystal]] sobre un [[Eternia Crystal Stand]]. Los vende el [[Tavernkeep]]: 25 de plata al principio, 1 de oro tras un jefe mecánico y 4 de oro tras el Gólem.', 'El nivel lo decide tu progreso: el 1 por defecto, el 2 con un jefe mecánico derrotado en modo difícil y el 3 con el Gólem. Nivel 1: 5 oleadas. Niveles 2 y 3: 7 oleadas, y en el 3 sale [[Betsy]] en la última. Das [[Defender Medal]] y [[Etherian Mana]]. Ver {p:dd2t1}.'],
      ['Invasión pirata', 'Al amanecer, en modo difícil y con algún altar roto: 1 entre 30 (1 entre 60 tras vencerla una vez).', '[[Pirate Map]]: 1 de cada 100 de los enemigos de la superficie en los océanos, en modo difícil.', 'Con ella derrotada se muda el [[Pirate]]. Puede aparecer el [[Flying Dutchman]]. Ver {p:pirates}.'],
      ['Legión de escarcha', 'Solo si la inicias tú, en modo difícil.', '[[Snow Globe]]: 1 de cada 15 en un [[Present]], en modo difícil.', 'Con ella derrotada, [[Santa Claus]] se muda en Navidad. Ver {p:frostlegion}.'],
      ['Eclipse', 'Al amanecer, 1 entre 20, en modo difícil y con un jefe mecánico derrotado.', '[[Solar Tablet]]: 8 [[Solar Tablet Fragment]] en un yunque de mitrilo u oricalco. Solo de día y en modo difícil. Los fragmentos los suelta el {z:templo} (1 de cada 7 de sus lagartos).', 'Enemigos propios, más duros tras Plantera. Ver {p:eclipse}.'],
      ['Luna calabaza', 'Solo si la inicias tú, de noche.', '[[Pumpkin Moon Medallion]]: 30 [[Pumpkin]], 5 [[Ectoplasm]] y 10 [[Hallowed Bar]] en un yunque de mitrilo u oricalco.', '15 oleadas, con [[Mourning Wood]] y [[Pumpking]]. Ver {p:pumpkin}.'],
      ['Luna Gélida', 'Solo si la inicias tú, de noche.', '[[Naughty Present]]: 20 [[Silk]], 5 [[Ectoplasm]] y 5 [[Soul of Fright]] en el mismo yunque.', '15 oleadas, con [[Everscream]], [[Santa-NK1]] y [[Ice Queen]]. Ver {p:frostmoon}.'],
      ['Locura marciana', 'En modo difícil con el Gólem vencido: una [[Martian Probe]] te detecta en la superficie.', 'Ningún objeto: la sonda tiene que verte.', 'Enemigos marcianos y el [[Martian Saucer]]. Ver {p:martians}.'],
      ['Torres celestiales', 'Tras derrotar al [[Lunatic Cultist]]. También con el [[Celestial Sigil]] si el Gólem está vencido y no hay peligro cerca.', '[[Celestial Sigil]]: 12 de cada fragmento (solar, vórtice, nebulosa y polvo estelar) en el [[Ancient Manipulator]].', 'Cuatro torres. Cuando caen las cuatro, llega el [[Moon Lord]]. Ver {p:pillars}.']
    ] },
    { tipo: 'titulo', texto: 'Cómo sacarles partido' },
    { tipo: 'cajas', bloques: [
      { tipo: 'caja', titulo: 'Arena de combate', bloques: [
        { tipo: 'lista', items: [
          'Plataformas largas y a cierta altura, con espacio libre alrededor para ver llegar a los enemigos.',
          'Fogatas y farolillos de corazón cerca, para regenerar vida entre oleadas.',
          'Una cama a un salto de la arena, para reaparecer sin perder el sitio.',
          'Nada de túneles estrechos: tu movilidad es tu defensa.'
        ] }
      ] },
      { tipo: 'caja', titulo: 'Oleadas y puntuación', bloques: [
        { tipo: 'parrafo', texto: 'En las invasiones y las lunas, cada enemigo muerto suma puntos y al llegar a cierta cantidad empieza la siguiente oleada; algunos enemigos puntúan más que otros, así que mata primero el más gordo si lo tienes a tiro. En el Ejército del Antiguo, entre oleada y oleada hay una pausa: aprovéchala para reponer defensas. Si el cristal cae, pierdes el evento.' }
      ] },
      { tipo: 'caja', titulo: 'Antes de provocarlo', bloques: [
        { tipo: 'lista', numerada: true, items: [
          'Comprueba que cumples el requisito de vida de la tabla.',
          'Lleva pociones de curación de sobra y alguna de resistencia.',
          'Si lo provocas tú, elige el sitio: una arena que ya conozcas.'
        ] }
      ] }
    ] },
    { tipo: 'aviso', estilo: 'nota', texto: 'La Luna calabaza y la Luna Gélida piden [[Ectoplasm]] para fabricar su objeto: es decir, haber pasado Plantera. No te toca todavía si aún no has llegado ahí.' },
    { tipo: 'fuentes', fuentes: [{ wiki: 'terraria', pagina: 'Events' }, { wiki: 'terraria', pagina: 'Blood Moon' }, { wiki: 'terraria', pagina: 'Goblin Army' }, { wiki: 'terraria', pagina: "Old One's Army" }, { wiki: 'terraria', pagina: 'Pirate Invasion' }, { wiki: 'terraria', pagina: 'Solar Eclipse' }, { wiki: 'terraria', pagina: 'Pumpkin Moon' }, { wiki: 'terraria', pagina: 'Frost Moon' }, { wiki: 'terraria', pagina: 'Martian Madness' }] }
  ] },

  // ------------------------------------------------------------------ VECINOS
  { id: 'vecinos', titulo: 'Vecinos y felicidad', subtitulo: 'La casa que vale, quién se muda y cuándo, y cómo contentarlos', icono: 'vecinos', bloques: [
    { tipo: 'parrafo', texto: 'Los vecinos te dan tiendas, descuentos y servicios. Casi todos llegan por una condición concreta, no por azar, y cada uno necesita una casa vacía y válida. Esto es lo que comprueba el juego de verdad.' },
    { tipo: 'titulo', texto: 'Una casa válida' },
    { tipo: 'lista', items: [
      '**Tamaño.** Al menos 60 casillas y menos de 750, contando el marco.',
      '**Muebles.** Una luz (antorcha, lámpara, candelabro...), una mesa, una silla y una puerta. Sirve cualquier variante de cada tipo. Según la wiki, una plataforma también cuenta como puerta.',
      '**Paredes.** Recinto cerrado, sin huecos y con paredes de fondo colocadas por ti: las naturales de una cueva no valen (wiki).',
      '**Suelo.** Al menos una casilla de suelo con 3 casillas libres encima (wiki).',
      '**Lugar.** A 10 casillas o más del borde del mundo, y sin demasiada maldad alrededor: si hay Corrupción o Carmesí de sobra, la casa deja de valer.'
    ] },
    { tipo: 'aviso', estilo: 'suave', texto: 'Si dudas de una casa, usa el icono de casas del inventario: te dice qué le falta. Es una casa por vecino; deja alguna libre de más para que el siguiente que llegue ya tenga dónde vivir.' },
    { tipo: 'titulo', texto: 'Cómo llega cada vecino' },
    { tipo: 'tabla', cabeceras: ['NPC', 'Cómo llega'], filas: [
      ['[[Guide]]', 'Ya vive en el mundo al empezar.'],
      ['[[Merchant]]', 'Cuando un jugador lleva 50 de plata en su inventario (suma de todas sus monedas).'],
      ['[[Nurse]]', 'Con el [[Merchant]] ya en el pueblo y al menos un jugador que haya usado un [[Life Crystal]].'],
      ['[[Demolitionist]]', 'Con el [[Merchant]] en el pueblo y un explosivo en el inventario de un jugador.'],
      ['[[Dye Trader]]', 'Con un tinte o un ingrediente de tinte en el inventario, y al menos 4 vecinos ya instalados.'],
      ['[[Arms Dealer]]', 'Con balas, o un arma que las dispare, en el inventario.'],
      ['[[Dryad]]', 'Tras derrotar al [[Eye of Cthulhu]], al jefe de la maldad o a [[Skeletron]] (cualquiera de los tres).'],
      ['[[Clothier]]', 'Tras derrotar a [[Skeletron]].'],
      ['[[Witch Doctor]]', 'Tras derrotar a la [[Queen Bee]].'],
      ['[[Painter]]', 'Cuando ya hay 8 o más vecinos en el pueblo.'],
      ['[[Party Girl]]', 'Una posibilidad entre 40 cada vez que se comprueba, y con 14 o más vecinos.'],
      ['[[Zoologist]]', 'Con un 10 % del Bestiario completo.'],
      ['[[Tavernkeep]]', 'Según la wiki, hay que encontrarlo inconsciente bajo tierra después de derrotar al jefe de la maldad, y despertarlo.'],
      ['[[Goblin Tinkerer]]', 'Tras vencer la Invasión duende, puede aparecer atado en las cavernas: habla con él.'],
      ['[[Mechanic]]', 'Atada en la {z:mazmorra}: habla con ella.'],
      ['[[Stylist]]', 'Atada en un nido de arañas: habla con ella.'],
      ['[[Angler]]', 'Sentado en el {z:oceano}: habla con él.'],
      ['[[Golfer]]', 'En {z:desierto_subterraneo}: habla con él.'],
      ['[[Wizard]]', 'Atado en las cavernas, en modo difícil: habla con él.'],
      ['[[Tax Collector]]', 'En modo difícil, según la wiki, curando con [[Purification Powder]] a un alma atormentada del {z:inframundo}.'],
      ['[[Truffle]]', 'En modo difícil, con una casa en un bioma de champiñones de la superficie.'],
      ['[[Pirate]]', 'Tras derrotar la Invasión pirata.'],
      ['[[Steampunker]]', 'Tras derrotar a un jefe mecánico.'],
      ['[[Cyborg]]', 'En modo difícil, tras derrotar a [[Plantera]].'],
      ['[[Princess]]', 'Cuando ya viven en el pueblo todos los demás vecinos de esta lista (no cuentan las mascotas ni [[Santa Claus]]).'],
      ['[[Santa Claus]]', 'Con la Legión de escarcha derrotada y en Navidad.'],
      ['[[Traveling Merchant]]', 'Visita sola, de día y por la mañana, cuando hay al menos 2 vecinos. Se va al atardecer.'],
      ['[[Skeleton Merchant]]', 'Rara vez, en las cavernas (wiki).']
    ] },
    { tipo: 'titulo', texto: 'Felicidad: lo que sí es seguro' },
    { tipo: 'parrafo', texto: 'Un vecino contento te hace precio. El descuento máximo es del 25 % y el recargo máximo, del 50 %. Cada preferencia suma: un bioma o un vecino que le gusta o le encanta baja el precio; uno que le disgusta o le repugna lo sube. Si el vecino no tiene casa, está lejos de ella o tú estás en una zona de maldad cuando le compras, el precio sube al máximo.' },
    { tipo: 'lista', items: [
      '**Aglomeración.** Más de 3 vecinos pegados a una misma casa suben el precio, un poco más con cada uno. Con 7 o más ya se queja de verdad.',
      '**Espacio.** Hasta 2 vecinos cerca de su casa y menos de 4 en todo el pueblo dan un pequeño descuento.',
      '**Preferencias.** Cada vecino tiene biomas y vecinos favoritos y otros que no soporta. No las copies de memoria: están en la wiki, y el propio juego las muestra al hablar con cada uno.',
      '**La [[Princess]]** es distinta: no soporta estar sola y necesita a otros vecinos cerca.'
    ] },
    { tipo: 'titulo', texto: 'Pilones' },
    { tipo: 'parrafo', texto: 'Un pilón te lleva de un pilón a otro, y cada bioma tiene el suyo. Para comprar uno, el vecino tiene que estar contento (su precio, al 90 % o menos), tienes que estar en el bioma que toca, con al menos 2 vecinos cerca, y fuera de la Corrupción y del Carmesí. Para usarlo hay que estar junto a otro pilón; el destino necesita al menos 2 vecinos cerca (menos el pilón universal) y estar en su bioma; no puede haber un jefe ni un evento activos; y los pilones del {z:templo} no funcionan hasta derrotar a [[Plantera]].' },
    { tipo: 'titulo', texto: 'La [[n:Zoologist]] y el Bestiario' },
    { tipo: 'parrafo', texto: 'El Bestiario se rellena al ver, matar y sacar botín a los enemigos. Al llegar al 10 % se muda la [[Zoologist]]. Explorar zonas nuevas y matar de todo es la manera más rápida de llenarlo.' },
    { tipo: 'fuentes', fuentes: [{ wiki: 'terraria', pagina: 'Housing' }, { wiki: 'terraria', pagina: 'NPCs' }, { wiki: 'terraria', pagina: 'Happiness' }, { wiki: 'terraria', pagina: 'Pylons' }, { wiki: 'terraria', pagina: 'Bestiary' }] }
  ] },

  // ------------------------------------------------------------------ VIDA
  { id: 'vida', titulo: 'Vida, maná y mejoras permanentes', subtitulo: 'Todo lo que sube tu personaje para siempre', icono: 'vida', bloques: [
    { tipo: 'parrafo', texto: 'Hay dos familias de mejoras. Las de vida y maná, que sirven a todas las clases, y las permanentes de la versión 1.4.4, que se usan una sola vez y duran siempre. Tu progreso en ellas se ve en {p:mejoras}.' },
    { tipo: 'tabla', cabeceras: ['Objeto', 'Efecto', 'Máximo', 'Cómo se consigue'], filas: [
      ['[[Life Crystal]]', '+20 de vida', '15 usados: de 100 a 400', 'Está bajo tierra, en las cavernas: se rompe y suelta el objeto. La [[Nurse]] llega cuando ya has usado uno.'],
      ['[[Life Fruit]]', '+5 de vida', '20 usados: de 400 a 500', 'Solo se puede usar con los 15 cristales ya usados. Crece en la hierba de {z:jungla_subterranea} en modo difícil, cuando ya has derrotado a un jefe mecánico. Ver {p:frutas}.'],
      ['[[Mana Crystal]]', '+20 de maná', '9 usados: de 20 a 200', 'Se fabrica con 5 [[Fallen Star]], sin estación.']
    ] },
    { tipo: 'aviso', estilo: 'destacado', texto: 'Usa los cristales según los encuentres: no te quitan sitio y la vida se queda para siempre. Los 15 cristales hacen falta antes del primer [[Life Fruit]].' },
    { tipo: 'titulo', texto: 'Mejoras permanentes de la 1.4.4' },
    { tipo: 'parrafo', texto: 'Cada una se consume al usarla y solo se puede usar una vez. Las que salen del fulgor se consiguen tirando un objeto concreto al líquido del {z:eter} (ver {a:eter}); las demás tienen su propio origen.' },
    { tipo: 'tabla', cabeceras: ['Objeto', 'Qué hace', 'Cómo se consigue'], filas: [
      ['[[Demon Heart]]', 'Abre una ranura extra de accesorio. Solo funciona en Experto y superior.', 'En la [[Treasure Bag (Wall of Flesh)]] de Experto o Maestro, mientras no hayas usado ya la mejora.'],
      ['[[Torch God\'s Favor]]', 'Permite que las antorchas que colocas tomen el tipo del bioma en el que estás.', 'Con más de 100 antorchas cerca bajo tierra y sin llevar ya el objeto, puede empezar el evento del Dios de las antorchas. Si aguantas sus ataques casi hasta el final, lo suelta. Ver {p:antorchas}.'],
      ['[[Artisan Loaf]]', 'Alcanzas las estaciones de fabricación desde más lejos: +4 casillas en horizontal y en vertical.', 'Lo vende el [[Skeleton Merchant]] en las tres fases de luna alrededor de la luna nueva, mientras no lo hayas comido.'],
      ['[[Vital Crystal]]', 'Tu vida se regenera más rápido.', 'Tirando un [[Life Crystal]] al fulgor.'],
      ['[[Aegis Fruit]]', '+4 de defensa.', 'Tirando una [[Life Fruit]] al fulgor.'],
      ['[[Arcane Crystal]]', 'Tu maná se regenera más rápido.', 'Tirando un [[Mana Crystal]] al fulgor.'],
      ['[[Galaxy Pearl]]', '+0,03 de suerte.', 'Tirando una [[Pink Pearl]] al fulgor.'],
      ['[[Gummy Worm]]', '+3 de poder de pesca.', 'Tirando un [[o:Gold Worm]] al fulgor. El gusano sale, 1 de cada 20 veces, de un [[Can Of Worms]], o se caza con red.'],
      ['[[Ambrosia]]', '+5 % de velocidad al minar y al construir.', 'Tirando una fruta al fulgor: [[Apple]], [[Apricot]], [[Pomegranate]] y otras.'],
      ['[[Minecart Upgrade Kit]]', 'Más velocidad en las vagonetas y una sonda defensiva. Incluye una [[Mechanical Cart]] gratis.', 'Se fabrica con [[Mechanical Wheel Piece]], [[Mechanical Wagon Piece]] y [[Mechanical Battery Piece]] en un yunque de mitrilo u oricalco. Las piezas salen de las bolsas de tesoro de los tres jefes mecánicos, solo en Experto y Maestro.'],
      ['[[Advanced Combat Techniques]]', 'Tus vecinos pelean mejor: +20 % de daño y +6 de defensa. Es una mejora del mundo, no tuya.', 'Pescando la captura más rara en una Luna de Sangre, 1 vez de cada 3, si aún no la usaste.'],
      ['[[Advanced Combat Techniques: Volume Two]]', 'Lo mismo otra vez: otros +20 % de daño y +6 de defensa para los vecinos.', 'Tirando un [[Spell Tome]] al fulgor. El [[Wizard]] vende el tomo.'],
      ['[[Peddler\'s Satchel]]', 'Aumenta lo que vende el [[Traveling Merchant]]. También es del mundo.', 'Tirando un [[Peddler\'s Hat]] al fulgor. El sombrero lo suelta el comerciante al morir.']
    ] },
    { tipo: 'aviso', estilo: 'nota', texto: 'Cada mejora se usa una sola vez: para las del fulgor te basta con un objeto de cada. Si te sobran cristales o frutas una vez usados los máximos, esos son los que tiras. El [[Artisan Loaf]] y el favor del Dios de las antorchas se pueden hacer mucho antes.' },
    { tipo: 'fuentes', fuentes: [{ wiki: 'terraria', pagina: 'Life Crystal' }, { wiki: 'terraria', pagina: 'Life Fruit' }, { wiki: 'terraria', pagina: 'Mana Crystal' }, { wiki: 'terraria', pagina: 'Permanent boosters' }, { wiki: 'terraria', pagina: "Torch God's Favor" }] }
  ] },

  // ------------------------------------------------------------------ PESCA
  { id: 'pesca', titulo: 'Pesca y el Pescador', subtitulo: 'Poder de pesca, cebos, cañas, cajas y misiones', icono: 'pesca', bloques: [
    { tipo: 'parrafo', texto: 'La pesca da pociones, cajas con botín de bioma y, con el [[Angler]], objetos que no encuentras en otro sitio. Se pesca en agua, miel o lava, con una caña y un cebo. Lo demás es subir poder. La ruta está en {p:pesca}.' },
    { tipo: 'titulo', texto: 'Poder de pesca' },
    { tipo: 'parrafo', texto: 'El poder de pesca sale de la caña, el cebo y tus mejoras (pociones, accesorios, armadura). Cuanto más alto, mejores capturas y más cajas. El agua también cuenta: con menos de 75 casillas de agua conectadas el juego no te deja pescar, y con un lago poco profundo pierdes parte del poder. Pesca en un lago grande y profundo.' },
    { tipo: 'tabla', cabeceras: ['Caña', 'Poder'], filas: [
      ['[[Wood Fishing Pole]]', '5'],
      ['[[Reinforced Fishing Pole]]', '15'],
      ['[[Fisher of Souls]]', '20'],
      ['[[Fleshcatcher]]', '22'],
      ['[[Fiberglass Fishing Pole]]', '30'],
      ['[[Mechanic\'s Rod]]', '35'],
      ['[[Sitting Duck\'s Fishing Pole]]', '40'],
      ['[[Hotline Fishing Hook]]', '45; pesca en lava'],
      ['[[Golden Fishing Rod]]', '50']
    ] },
    { tipo: 'tabla', cabeceras: ['Cebo', 'Poder'], filas: [
      ['[[Apprentice Bait]]', '15'],
      ['[[Journeyman Bait]]', '30'],
      ['[[Master Bait]]', '50'],
      ['[[o:Gold Worm]]', '50; es un bicho que se caza con red'],
      ['[[o:Truffle Worm]]', '666, pero no sirve para pescar normal: invoca al Duque Fishron, y solo en el mar, junto a los bordes del mundo']
    ] },
    { tipo: 'cajas', bloques: [
      { tipo: 'caja', titulo: 'Sube el poder', bloques: [
        { tipo: 'lista', items: [
          '[[Fishing Potion]] y el resto de pociones de pesca.',
          'Accesorios de pesca: [[Tackle Box]], [[Angler Earring]], [[High Test Fishing Line]], [[Fishing Bobber]].',
          'Con un lago grande y profundo, y [[Master Bait]] o mejor, llegas a casi todo.'
        ] }
      ] },
      { tipo: 'caja', titulo: 'Lava y miel', bloques: [
        { tipo: 'parrafo', texto: 'En lava solo pescas con una caña que lo permita (el [[Hotline Fishing Hook]]), con un accesorio de pesca en lava o con un cebo preparado para ello. En miel pescas con cualquier caña. En ambos casos, el depósito tiene que ser grande.' }
      ] },
      { tipo: 'caja', titulo: 'Cajas', bloques: [
        { tipo: 'parrafo', texto: 'Las cajas salen aparte, con su propia probabilidad, y su tipo depende del bioma y del poder. Ábrelas siempre: son una de las fuentes de dinero y de objetos más estables.' }
      ] }
    ] },
    { tipo: 'titulo', texto: 'El Pescador' },
    { tipo: 'parrafo', texto: 'El [[Angler]] te da una misión de pesca al día, y cambia al amanecer. Te dice qué pez y en qué zona. Se lo entregas y te paga con un objeto. En ciertas cuentas el premio es fijo:' },
    { tipo: 'tabla', cabeceras: ['Misiones hechas', 'Premio fijo'], filas: [
      ['5', '[[Fuzzy Carrot]]'],
      ['10', '[[Angler Hat]]'],
      ['15', '[[Angler Vest]]'],
      ['20', '[[Angler Pants]]'],
      ['25', '[[Bottomless Water Bucket]]'],
      ['30', '[[Golden Fishing Rod]]']
    ] },
    { tipo: 'parrafo', texto: 'En el resto de misiones, el premio es al azar, y cuantas más hayas hecho, menos raro sale lo bueno. Entre lo posible: [[Tackle Box]], [[Angler Earring]], [[High Test Fishing Line]], [[Fisherman\'s Pocket Guide]], [[Weather Radio]], [[Sextant]], [[Fishing Bobber]], [[Golden Bug Net]], [[Seashell Hairpin]], [[Fish Hook]] y la [[Super Absorbant Sponge]]. En modo difícil y pasadas 10 misiones, [[Fin Wings]]; y pasadas 25, [[Hotline Fishing Hook]]. Si el pez es el [[Bumblebee Tuna]], puede tocarte [[Bottomless Honey Bucket]] o [[Honey Absorbant Sponge]].' },
    { tipo: 'aviso', estilo: 'nota', texto: 'No hace falta hacer cada misión el mismo día: si el pez cae lejos, déjala. Pero no acumules: la misión nueva te llega igual al amanecer, y las cuentas de arriba solo suben cuando entregas.' },
    { tipo: 'fuentes', fuentes: [{ wiki: 'terraria', pagina: 'Fishing' }, { wiki: 'terraria', pagina: 'Angler' }, { wiki: 'terraria', pagina: 'Fishing quests' }, { wiki: 'terraria', pagina: 'Fishing poles' }, { wiki: 'terraria', pagina: 'Bait' }] }
  ] },

  // ------------------------------------------------------------------ MATERIALES
  { id: 'materiales', titulo: 'Materiales y estaciones', subtitulo: 'Qué necesitas para fabricar, con qué pico se mina cada mineral y qué no vender', icono: 'materiales', bloques: [
    { tipo: 'parrafo', texto: 'Casi todo en Terraria se fabrica junto a una estación. Si una receta no te sale, mira primero qué estación falta: es lo que más se olvida.' },
    { tipo: 'tabla', cabeceras: ['Estación', 'Cómo se consigue', 'Para qué'], filas: [
      ['[[Work Bench]]', '10 de madera.', 'Mobiliario, paredes y objetos de madera.'],
      ['[[Furnace]]', '20 de piedra, 4 de madera y 3 antorchas, junto a un banco de trabajo.', 'Barras de mineral y cristal.'],
      ['[[Iron Anvil]] / [[Lead Anvil]]', '5 barras de hierro o de plomo, junto a un banco de trabajo.', 'Herramientas, armas y armaduras.'],
      ['[[Hellforge]]', 'No se fabrica: según la wiki se encuentra en casas en ruinas del {z:inframundo} y en cajas de obsidiana y de piedra infernal.', 'Barras de piedra infernal y obsidiana.'],
      ['[[Tinkerer\'s Workshop]]', 'La vende el [[Goblin Tinkerer]].', 'Combinar accesorios.'],
      ['[[Alchemy Table]]', 'Sale en la {z:mazmorra} o se pesca allí.', 'Pociones, con posibilidad de no gastar ingredientes.'],
      ['[[Mythril Anvil]] / [[Orichalcum Anvil]]', '10 [[Mythril Bar]] o 12 [[Orichalcum Bar]], junto a un yunque de hierro o de plomo.', 'Armas, alas y objetos de modo difícil.'],
      ['[[Adamantite Forge]] / [[Titanium Forge]]', '30 de [[Adamantite Ore]] o [[Titanium Ore]] y una [[Hellforge]], junto a un yunque de mitrilo.', 'Barras de adamantita y de titanio.'],
      ['[[Ancient Manipulator]]', 'Siempre lo suelta el [[Lunatic Cultist]].', 'Fragmentos lunares y todo lo que sale de ellos.']
    ] },
    { tipo: 'titulo', texto: 'Minerales y pico necesario' },
    { tipo: 'parrafo', texto: 'Cada mineral pide un pico con una potencia mínima: con menos, el golpe no hace nada. Los de los primeros pasos (cobre, hierro, plata, oro...) se minan con cualquier pico.' },
    { tipo: 'tabla', cabeceras: ['Material', 'Potencia mínima del pico'], filas: [
      ['[[Meteorite]]', '50'],
      ['[[Demonite Ore]] / [[Crimtane Ore]] (bajo la superficie)', '55'],
      ['[[Obsidian]]', '55'],
      ['[[Hellstone]] (y la [[Hellforge]] colocada, en el {z:inframundo})', '65'],
      ['[[Ebonstone Block]] / [[Crimstone Block]] / [[Pearlstone Block]]', '65'],
      ['Ladrillos de la {z:mazmorra}', '100'],
      ['[[Cobalt Ore]] / [[Palladium Ore]]', '100'],
      ['[[Mythril Ore]] / [[Orichalcum Ore]]', '110'],
      ['[[Adamantite Ore]] / [[Titanium Ore]]', '150'],
      ['[[Chlorophyte Ore]]', '200'],
      ['Ladrillos y altar del {z:templo}', '210']
    ] },
    { tipo: 'aviso', estilo: 'destacado', texto: 'No vendas sin pensarlo: [[Shadow Scale]] y [[Tissue Sample]] (armadura de la maldad), las barras, [[Soul of Light]] y [[Soul of Night]] (muchas armas y alas de modo difícil) ni los fragmentos lunares. Guárdalos en un cofre aparte de componentes reservados.' },
    { tipo: 'fuentes', fuentes: [{ wiki: 'terraria', pagina: 'Crafting stations' }, { wiki: 'terraria', pagina: 'Pickaxe' }, { wiki: 'terraria', pagina: 'Ores' }] }
  ] },

  // ------------------------------------------------------------------ ETER
  { id: 'eter', titulo: 'El Éter y el fulgor', subtitulo: 'Dónde está, qué transforma y qué consigues con él', icono: 'secreto', bloques: [
    { tipo: 'parrafo', texto: 'El {z:eter} es un bioma pequeño con un líquido propio, el fulgor, que aparece una sola vez por mundo. Con él puedes transformar objetos, deshacer recetas y conseguir las mejoras permanentes que no se encuentran de otro modo.' },
    { tipo: 'titulo', texto: 'Dónde está' },
    { tipo: 'parrafo', texto: 'Se genera en el subsuelo o las cavernas, cerca del borde del mundo, y siempre en el lado contrario a la {z:mazmorra}: si la Mazmorra está a la izquierda, el Éter está a la derecha, y al revés. Es decir, en la misma mitad que {z:jungla}.' },
    { tipo: 'flujo', items: ['Mira de qué lado está la Mazmorra', 'Ve al lado contrario, cerca del borde', 'Baja al subsuelo y a las cavernas', 'Busca el líquido que brilla'] },
    { tipo: 'titulo', texto: 'Qué hace con los objetos' },
    { tipo: 'parrafo', texto: 'Lo que tires al fulgor flota unos instantes y se transforma. Qué pasa depende del objeto:' },
    { tipo: 'cajas', bloques: [
      { tipo: 'caja', titulo: 'Se transforma', bloques: [
        { tipo: 'parrafo', texto: 'Algunos objetos se convierten en otro concreto. Ejemplos: [[Life Crystal]] en [[Vital Crystal]], [[Angel Statue]] en [[Aether Monolith]], [[Torch]] en [[Aether Torch]], [[Campfire]] en [[Aether Campfire]], [[Star Cloak]] en [[Chromatic Cloak]] y [[Flare]] en [[Shimmer Flare]]. Algunos pares funcionan en los dos sentidos.' }
      ] },
      { tipo: 'caja', titulo: 'Se deshace', bloques: [
        { tipo: 'parrafo', texto: 'Casi todo lo fabricado vuelve a sus ingredientes. Pero hay candados: las recetas que llevan [[Bone]] no se deshacen hasta derrotar a [[Skeletron]], y las que llevan [[Lihzahrd Brick]], hasta derrotar al Gólem.' }
      ] },
      { tipo: 'caja', titulo: 'Monedas: suerte', bloques: [
        { tipo: 'parrafo', texto: 'Las monedas no se transforman: se gastan y te dan suerte, que va bajando poco a poco. Una de cobre vale 1, una de plata 100, una de oro 10.000 y una de platino llena el contador.' }
      ] },
      { tipo: 'caja', titulo: 'Tras el Señor de la Luna', bloques: [
        { tipo: 'parrafo', texto: 'Con el [[Moon Lord]] derrotado, [[Rod of Discord]] pasa a [[Rod of Harmony]] y [[Clentaminator]] a [[Terraformer]]. Además, [[Bottomless Water Bucket]] y [[Bottomless Shimmer Bucket]] se convierten el uno en el otro.' }
      ] }
    ] },
    { tipo: 'titulo', texto: 'Si te metes tú' },
    { tipo: 'parrafo', texto: 'Si metes la cabeza en el fulgor, según la wiki te afecta un efecto de fulgor: caes despacio y atraviesas todos los bloques hasta llegar a un hueco. Puede dejarte en un sitio que no esperabas, así que no te bañes a la ligera. Los vecinos que se mojan cambian de aspecto, y es solo eso.' },
    { tipo: 'titulo', texto: 'Mejoras permanentes que salen del fulgor' },
    { tipo: 'tabla', cabeceras: ['Tiras', 'Recibes', 'Efecto'], filas: [
      ['[[Life Crystal]]', '[[Vital Crystal]]', 'Regeneración de vida más rápida.'],
      ['[[Life Fruit]]', '[[Aegis Fruit]]', '+4 de defensa.'],
      ['[[Mana Crystal]]', '[[Arcane Crystal]]', 'Regeneración de maná más rápida.'],
      ['[[Pink Pearl]]', '[[Galaxy Pearl]]', '+0,03 de suerte.'],
      ['[[o:Gold Worm]]', '[[Gummy Worm]]', '+3 de poder de pesca.'],
      ['Una fruta ([[Apple]], [[Apricot]]...)', '[[Ambrosia]]', '+5 % de velocidad al minar y al construir.'],
      ['[[Spell Tome]]', '[[Advanced Combat Techniques: Volume Two]]', 'Mejora el daño y la defensa de los vecinos.'],
      ['[[Peddler\'s Hat]]', '[[Peddler\'s Satchel]]', 'Amplía la tienda del Mercader viajero.']
    ] },
    { tipo: 'aviso', estilo: 'nota', texto: 'El [[Terragrim]] no viene del fulgor. Según la wiki, sale de una espada clavada en piedra (un santuario de las cavernas): al romperla, 1 posibilidad entre 30 de [[Terragrim]] en lugar de [[o:Enchanted Sword]].' },
    { tipo: 'aviso', estilo: 'suave', texto: 'Cada mejora de la tabla se usa una sola vez: tira un objeto de cada y no más. Primero usa tus 15 cristales de vida y tus frutos; solo te sobran los que no puedas usar. Tu ruta está en {p:eter}.' },
    { tipo: 'fuentes', fuentes: [{ wiki: 'terraria', pagina: 'Shimmer' }, { wiki: 'terraria', pagina: 'Aether' }, { wiki: 'terraria', pagina: 'Permanent boosters' }] }
  ] },

  // ------------------------------------------------------------------ FUENTES
  { id: 'fuentes', titulo: 'Fuentes de esta guía', subtitulo: 'De dónde sale cada dato y cómo se une a tu partida', icono: 'fuentes', bloques: [
    { tipo: 'parrafo', texto: 'Esta guía es de Terraria sin mods. La versión de referencia es **Terraria 1.4.4.9**, la que usa tModLoader 1.4.4.9. Los nombres de objetos, enemigos y vecinos son los oficiales de la localización en español de España que lleva el propio juego. Las recetas, el botín y las tiendas se han leído del código decompilado.' },
    { tipo: 'lista', items: [
      '**Código del juego.** Condiciones de aparición, botín, recetas y tiendas: las clases principales del juego, el catálogo de botín y el código de las invasiones.',
      '**Localización oficial.** Los nombres en español de cada objeto y vecino vienen del archivo de idioma incrustado en el juego; no se escriben a mano.',
      '**terraria.wiki.gg.** Para las guías generales y los detalles que no se leen bien en el código. Donde un dato viene solo de la wiki, el texto lo dice.'
    ] },
    { tipo: 'tabla', cabeceras: ['Dónde mirar', 'Enlace'], filas: [
      ['Ruta general', 'https://terraria.wiki.gg/wiki/Guide:Walkthrough'],
      ['Equipo por clase', 'https://terraria.wiki.gg/wiki/Guide:Class_setups'],
      ['Eventos', 'https://terraria.wiki.gg/wiki/Events'],
      ['Jefes', 'https://terraria.wiki.gg/wiki/Bosses'],
      ['Vecinos', 'https://terraria.wiki.gg/wiki/NPCs'],
      ['Pesca', 'https://terraria.wiki.gg/wiki/Fishing'],
      ['Fulgor', 'https://terraria.wiki.gg/wiki/Shimmer']
    ] },
    { tipo: 'aviso', estilo: 'destacado', texto: 'La guía lee tu partida: tu personaje y tu mundo. Marca lo que ya has hecho y te dice qué sigue. Si algo no cuadra con lo que ves en pantalla, fíate de lo que ves y abre {a:problemas}.' },
    { tipo: 'fuentes', fuentes: [{ wiki: 'terraria', pagina: 'Guide:Walkthrough' }, { wiki: 'terraria', pagina: 'Guide:Class setups' }, { wiki: 'terraria', pagina: 'Version history' }] }
  ] }
] };
