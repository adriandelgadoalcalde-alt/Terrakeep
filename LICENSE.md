# Licencia de Terrakeep

Decisión tomada tras la auditoría final de Opus (5-sep-2026), antes de publicar Terrakeep en
público: licencia abierta con atribución obligatoria (MIT), siguiendo el mismo criterio que
[TEdit](https://github.com/TEdit/Terraria-Map-Editor) - editor de mundos de Terraria muy
establecido cuyo formato ya se usó como referencia real en este proyecto (ver `CLAUDE.md`).

## Alcance real - qué cubre esta licencia y qué NO

Esta licencia cubre **únicamente el código fuente propio de Terrakeep**: los proyectos C#/XAML
(`TerrasavrNative.App`, `TerrasavrNative.Core` y sus proyectos de test), los scripts en
`scripts/`, y la documentación propia del repositorio.

**NO cubre, porque no es propiedad de este proyecto**:

- Los sprites e imágenes de Terraria/Calamity Mod bajo `TerrasavrNative.App/Assets/vanilla/`,
  `Assets/npc_heads/`, `Assets/npc_icons/`, `Assets/player/` y `Assets/calamity/` (icons) -
  propiedad de Re-Logic, el equipo de tModLoader y el equipo de CalamityMod respectivamente. Ver
  el aviso ya presente en la pestaña "Acerca de" de la propia app.
- Cualquier dato de juego derivado (nombres, estadísticas, IDs) presente en `Assets/*.json` -
  igualmente propiedad de sus respectivos autores; Terrakeep solo los organiza y presenta.
- El contenido de `reference/terrasavr-real/` (volcado legible del `script.js` compilado de
  Terrasavr, de YellowAfterlife) - se conserva ahí únicamente como material de estudio interno,
  nunca se redistribuye como parte de la app ni del instalador.

Terrakeep no está afiliado con Re-Logic, el equipo de tModLoader, el equipo de CalamityMod ni
con YellowAfterlife.

## Texto de la licencia (MIT)

Copyright (c) 2026 IncrediBad

Por la presente se concede permiso, de forma gratuita, a cualquier persona que obtenga una copia
de este software y de los archivos de documentación asociados (el "Software"), a utilizar el
Software sin restricción, incluyendo sin limitación los derechos de usar, copiar, modificar,
fusionar, publicar, distribuir, sublicenciar y/o vender copias del Software, y a permitir a las
personas a las que se les proporcione el Software a hacer lo mismo, sujeto a las siguientes
condiciones:

El aviso de copyright anterior y este aviso de permiso se incluirán en todas las copias o partes
sustanciales del Software.

EL SOFTWARE SE PROPORCIONA "TAL CUAL", SIN GARANTÍA DE NINGÚN TIPO, EXPRESA O IMPLÍCITA,
INCLUYENDO PERO NO LIMITADO A GARANTÍAS DE COMERCIALIZACIÓN, IDONEIDAD PARA UN PROPÓSITO
PARTICULAR E INCUMPLIMIENTO. EN NINGÚN CASO LOS AUTORES O TITULARES DEL COPYRIGHT SERÁN
RESPONSABLES DE NINGUNA RECLAMACIÓN, DAÑO U OTRA RESPONSABILIDAD, YA SEA EN UNA ACCIÓN DE
CONTRATO, AGRAVIO O CUALQUIER OTRO MOTIVO, DERIVADA DE, FUERA DE O EN CONEXIÓN CON EL SOFTWARE O
EL USO U OTRO TIPO DE ACCIONES EN EL SOFTWARE.
