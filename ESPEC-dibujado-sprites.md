# Especificación real del dibujado del jugador en Terraria 1.4.5.8

Documento de ingeniería inversa de alcance acotado sobre **cómo dibuja Terraria al jugador**
(sprites, celdas, offsets, orden de capas y reglas de sustitución armadura/vanidad), escrito
para corregir `TerrasavrNative.App/Services/PlayerPreviewRenderer.cs`.

**Regla de este documento**: todo lo que aparece en las secciones 1 a 6 es **lectura directa
del código decompilado real** o **comprobación directa que hice yo mismo sobre un fichero
real de este equipo**, con la cita exacta al lado. Todo lo que es razonamiento mío y no
lectura directa está aislado en la **sección 7 (Inferencias)**. No hay ninguna frase que
mezcle las dos cosas.

**Fuentes reales usadas** (versiones confirmadas, no supuestas):

| Fuente | Ruta real | Versión |
|---|---|---|
| Terraria vanilla decompilado | `C:\Users\adrian\Downloads\tModLoader-Decompiled\TerrariaVanilla\` | 1.4.5.8 |
| Instalación real de Steam | `C:\Program Files (x86)\Steam\steamapps\common\Terraria\` | 1.4.5.8 |

La versión de la instalación de Steam la comprobé yo con
`(Get-Item 'C:\Program Files (x86)\Steam\steamapps\common\Terraria\Terraria.exe').VersionInfo`
→ `FileVersion : 1.4.5.8`, **la misma que el decompilado**. Esto importa: significa que las
tablas de ids y los assets del decompilado y del disco se corresponden 1:1 y no hace falta
ninguna cautela por desfase de versión (que sí haría falta con la carpeta `tModLoader\`,
que es 1.4.4.9).

---

## 0. Conclusión primero: cuál es el bug real

El personaje de prueba **"Eldelgas" NO tiene `skinVariant` 0 ni 4**. Leí su `.plr` real
(`%USERPROFILE%\Documents\My Games\Terraria\Players\Eldelgas.plr`, versión 326) portando a
Node el lector del propio proyecto (`PlrCrypto` + `PlrBodySerializer` + `PlrItemSlot`), y el
byte `Gender` vale **8 = `PlayerVariantID.MaleDress`**. Los otros dos `.plr` reales de esa
carpeta: `Terrariano.plr` → `Gender = 1` (`MaleSticker`), `adrian.plr` → `Gender = 0`
(`MaleStarter`). Es decir: **dos de los tres personajes reales del usuario usan una variante
que el renderer actual no dibuja nunca.**

La vanidad de cuerpo que lleva puesta Eldelgas es el ítem **1820 "Vestido de la Muerte"**,
cuyo `bodySlot` real es **93** (`TerrasavrNative.App/Assets/vanilla_armor_slots.json`,
entrada `"1820": {"b":93}`). Y el `bodySlot` 93 es un caso **especialísimo** en el código
real del juego: aparece citado por nombre en cuatro sitios distintos de `PlayerDrawSet.cs`
y `Player.cs`.

El "tipo de cuerpo delgado" del que habla el usuario **es real y tiene nombre en el código**:
son las variantes de vestido/abrigo `MaleCoat=3`, `FemaleCoat=7`, `MaleDress=8` y
`FemaleDress=9`, que sustituyen TODA la ropa base por una túnica larga y (las tres primeras)
añaden una pieza extra de faldón. Lo verifiqué componiendo los sprites reales: ver
sección 6.5.

**Los cinco fallos reales del renderer actual, en orden de impacto visual:**

1. Con armadura/vanidad de cuerpo puesta, el juego **NO dibuja la ropa base**
   (camiseta/camisa/mangas). `PlayerPreviewRenderer` sí la dibuja, y además la pinta
   **después** de la armadura del torso → la camisa base tapa la vanidad.
2. `bodySlot 93` activa `hidesTopSkin` **y** `hidesBottomSkin`: el juego no dibuja ni la piel
   del torso ni la de las piernas. El renderer las dibuja siempre.
3. `SetMatch(body=93)` **sustituye el `legSlot` que lleve puesto por el 165** (la falda del
   vestido) y activa `wearsRobe`. El renderer no implementa `SetMatch` en absoluto, así que
   dibuja los pantalones/perneras equivocados.
4. La variante de cuerpo (1/2/3/5/6/7/8/9) **sí tiene hojas propias reales en disco** para
   las piezas 4/6/8/11/12/13 (+14 en 3/7/8). El renderer las ignora y usa siempre las de 0/4.
5. El orden hombro/brazo delantero está invertido respecto al real, y con `fullHair` el orden
   casco/pelo también.

---

## 1. Acotar: la cadena real de funciones, de "Player equipado" a "píxeles"

Todas las líneas están verificadas leyendo el fichero real esta misma sesión.

### 1.1 Resolución de qué slot visual está puesto (antes de dibujar nada)

| # | Función real | Fichero:línea | Qué hace |
|---|---|---|---|
| 1 | `Player.GetEffectiveArmor(int slot)` | `Terraria/Player.cs:5678` y `:5684` | Devuelve `armor[slot]`, o el del loadout marcado como favorito si el slot está vacío |
| 2 | `Player.UpdateEquips` (bloque de armadura) | `Terraria/Player.cs:36045-36047` | Calcula `head` / `body` / `legs` reales |
| 3 | `Player.SetMatch(SetMatchRequest, ref bool)` | `Terraria/Player.cs:37458-37694` | Sustituciones de conjunto; **depende de `Male`** |
| 4 | `Player.UpdateVisibleAccessories` / `UpdateVisibleAccessory` | `Terraria/Player.cs:37040-37075` y `:37151` | Accesorios; **`hideVisibleAccessory` solo se consulta aquí** |
| 5 | `Player.Male` (propiedad) | `Terraria/Player.cs:3389-3409` | `get` → `PlayerVariantID.Sets.Male[skinVariant]` |

### 1.2 Preparación del `PlayerDrawSet` (qué celda y qué banderas)

| # | Función real | Fichero:línea | Qué hace |
|---|---|---|---|
| 6 | `PlayerDrawSet.BoringSetup` (bloque de `skinVar`) | `Terraria/DataStructures/PlayerDrawSet.cs:369-386` | `armorAdjust`, `missingHand`, `missingArm`, `skinVar = drawPlayer.skinVariant` |
| 7 | (mismo método, más abajo) | `PlayerDrawSet.cs:1795-1796` | `hidesTopSkin` / `hidesBottomSkin` |
| 8 | `PlayerDrawSet.CreateCompositeData()` | `PlayerDrawSet.cs:1916-2054` | Elige las 5 celdas del frame; **aquí está el `+2` de mujer** |
| 9 | `CreateCompositeData_DetermineShoulderOffsets(int, int)` | `PlayerDrawSet.cs:2056-2245` | Offsets de hombro por armadura |
| 10 | `CreateCompositeFrameRect(Point)` | `PlayerDrawSet.cs:2247-2250` | `new Rectangle(pt.X * 40, pt.Y * 56, 40, 56)` |
| 11 | `UpdateCompositeArm(...)` | `PlayerDrawSet.cs:2252-2281` | Solo si `data.enabled` (brazo animado) |

### 1.3 Orden real de las capas

| # | Función real | Fichero:línea |
|---|---|---|
| 12 | `LegacyPlayerRenderer.DrawPlayer_UseNormalLayers` | `Terraria/Graphics/Renderers/LegacyPlayerRenderer.cs:163-259` |

Dentro de esa lista, las capas que están dentro del alcance ("qué sprite/celda se dibuja y en
qué orden"):

| # | Capa real | Fichero:línea | Llamada desde |
|---|---|---|---|
| 13 | `DrawPlayer_01_BackHair` | `PlayerDrawLayers.cs:199` | `LegacyPlayerRenderer.cs:184` |
| 14 | `DrawPlayer_12_Skin` (envoltorio) | `PlayerDrawLayers.cs:1173-1180` | `LegacyPlayerRenderer.cs:194` |
| 15 | `DrawPlayer_12_Skin_Composite` | `PlayerDrawLayers.cs:1253-1300` | desde el 14 |
| 16 | `DrawPlayer_12_SkinComposite_BackArmShirt` | `PlayerDrawLayers.cs:1302-1438` | `PlayerDrawLayers.cs:1299` |
| 17 | `DrawPlayer_13_Leggings` | `PlayerDrawLayers.cs:1457-1583` | `LegacyPlayerRenderer.cs:198` o `:202` |
| 18 | `DrawPlayer_14_Shoes` | `PlayerDrawLayers.cs:1758-1777` | `LegacyPlayerRenderer.cs:197` o `:203` |
| 19 | `DrawPlayer_15_SkinLongCoat` | `PlayerDrawLayers.cs:1779-1791` | `LegacyPlayerRenderer.cs:206` |
| 20 | `DrawPlayer_16_ArmorLongCoat` | `PlayerDrawLayers.cs:1793-1823` | `LegacyPlayerRenderer.cs:207` |
| 21 | `GetMatchingBodyExtension` | `PlayerDrawLayers.cs:1850-1926` | desde el 20 |
| 22 | `DrawPlayer_17_Torso` (envoltorio) | `PlayerDrawLayers.cs:1928-1987` | `LegacyPlayerRenderer.cs:208` |
| 23 | `DrawPlayer_17_TorsoComposite` | `PlayerDrawLayers.cs:1989-2044` | desde el 22 |
| 24 | `DrawPlayer_21_Head` | `PlayerDrawLayers.cs:2093-...` | `LegacyPlayerRenderer.cs:214` |
| 25 | `DrawPlayer_21_Head_TheFace` | `PlayerDrawLayers.cs:2574` | `PlayerDrawLayers.cs:2098` |
| 26 | `DrawPlayer_28_ArmOverItem` (envoltorio) | `PlayerDrawLayers.cs:3615-3691` | `LegacyPlayerRenderer.cs:239` |
| 27 | `DrawPlayer_28_ArmOverItemComposite` | `PlayerDrawLayers.cs:3693-3857` | desde el 26 |
| 28 | `GetCompositeOffset_BackArm` / `_FrontArm` | `PlayerDrawLayers.cs:4208-4216` | desde 15/16/27 |
| 29 | `DrawCompositeArmorPiece` | `PlayerDrawLayers.cs:39` | desde 16/23/27 |
| 30 | `DrawPlayer_RenderAllLayers` | `PlayerDrawLayers.cs:4303-4342` | `LegacyPlayerRenderer.cs:149` |

**Aquí me detengo**, tal como pide el encargo: `DrawPlayer_RenderAllLayers` solo recorre
`DrawDataCache` y llama a `DrawData.Draw`; a partir de ahí ya es SpriteBatch/shaders, fuera
del alcance de "qué sprite/celda se dibuja y en qué orden". Quedan también deliberadamente
fuera todas las capas de accesorios (alas, capas, globos, monturas, escudos, debuffs) que
`DrawPlayer_UseNormalLayers` intercala, porque el doll de Terrakeep no las dibuja.

---

## 2. La rejilla y las celdas reales

### 2.1 Qué es cada número de pieza

`Terraria/ID/PlayerTextureID.cs` (fichero completo, 16 constantes):

```
Head=0  EyeWhites=1  Eyes=2  TorsoSkin=3  Undershirt=4  Hands=5  Shirt=6  ArmSkin=7
ArmUndershirt=8  ArmHand=9  LegSkin=10  Pants=11  Shoes=12  ArmShirt=13  Extra=14
EyeBlink=15   (Count = 16)
```

### 2.2 Geometría real de cada pieza (comprobación de fichero hecha por mí)

Decodifiqué los `.xnb` reales con `xnb-to-png.js` del proyecto hermano. Dimensiones reales:

| Fichero real | Dimensión real | Forma |
|---|---|---|
| `Player_0_0.xnb` (Head) | **40 x 1118** | tira vertical |
| `Player_0_10/11/12.xnb` (LegSkin/Pants/Shoes) | **40 x 1120** | tira vertical (20 frames de 56) |
| `Player_8_14.xnb` (Extra) | **40 x 1120** | tira vertical |
| `Player_0_3/4/5/6/7/8/9/13.xnb` | **360 x 224** | rejilla 9 x 4 de celdas 40x56 |
| `Player_4_6.xnb`, `Player_9_6.xnb` | **360 x 224** | ídem |
| `Armor_Head_1.xnb`, `Armor_Legs_1.xnb` | **40 x 1120** | tira vertical |
| `Armor/Armor_1.xnb`, `_2`, `_93`, `_106`, `_165` | **360 x 224** | rejilla 9 x 4 |

La rejilla la confirma el código: `CreateCompositeFrameRect` (`PlayerDrawSet.cs:2247-2250`)
devuelve literalmente `new Rectangle(pt.X * 40, pt.Y * 56, 40, 56)`.

### 2.3 Celdas del frame de reposo

En `CreateCompositeData` (`PlayerDrawSet.cs:1916-2054`), con `num = bodyFrame.Y / bodyFrame.Height = 0`:

- `PlayerDrawSet.cs:1931-1935`: `pt = (1,1)` (hombro trasero), `pt2 = (0,1)` (hombro
  delantero), `pt3 = (0,0)` (torso), `frameIndex` y `frameIndex2` a cero.
- `PlayerDrawSet.cs:1960-1963` (`case 0:`): `frameIndex2.X = 2` → brazo delantero **(2,0)**.
- `PlayerDrawSet.cs:2039-2040`: `frameIndex.X = frameIndex2.X; frameIndex.Y = frameIndex2.Y + 2;`
  → brazo trasero **(2,2)**. **Este `+2` no depende del género.**
- `PlayerDrawSet.cs:2043-2048`: `if (!drawPlayer.Male) { pt.Y += 2; pt2.Y += 2; pt3.Y += 2; }`

Resultado (idéntico al que ya tiene el renderer, **confirmado, no hay que tocarlo**):

| Marco | Varón | Mujer |
|---|---|---|
| `compTorsoFrame` | (0,0) | (0,2) |
| `compFrontShoulderFrame` | (0,1) | (0,3) |
| `compBackShoulderFrame` | (1,1) | (1,3) |
| `compFrontArmFrame` | (2,0) | (2,0) |
| `compBackArmFrame` | (2,2) | (2,2) |

`UpdateCompositeArm` (`PlayerDrawSet.cs:2252-2281`) solo reescribe `frameIndex`/`frameIndex2`
si `data.enabled` (brazo animado apuntando); en reposo no interviene.

### 2.4 La armadura usa exactamente esas mismas celdas

Verificado en las tres capas:

- Torso: `PlayerDrawLayers.cs:2008` → `new DrawData(value, vector, drawinfo.compTorsoFrame, ...)`
- Hombro trasero: `PlayerDrawLayers.cs:1351` → `..., drawinfo.compBackShoulderFrame, ...`
- Brazo trasero: `PlayerDrawLayers.cs:1365` → `..., drawinfo.compBackArmFrame, ...`
- Hombro delantero: `PlayerDrawLayers.cs:3743` → `..., drawinfo.compFrontShoulderFrame, ...`
- Brazo delantero: `PlayerDrawLayers.cs:3766` → `..., drawinfo.compFrontArmFrame, ...`

En los cinco casos la textura es `TextureAssets.ArmorBodyComposite[drawPlayer.body]`
(`PlayerDrawLayers.cs:2007`, `:1348`, `:3721`), que `AssetInitializer.cs:478` carga de
`"Images/Armor/Armor_" + n`.

Y esas hojas **sí contienen versión femenina distinta**: analizando `Armor/Armor_1.xnb`
(peto de cobre) celda a celda, la celda (0,0) tiene 248 px opacos y la (0,2) tiene 212 px,
con hashes MD5 distintos — son dos dibujos diferentes. En cambio (0,1) y (0,3) tienen el
mismo hash `6d29f064`: el hombro delantero es idéntico en ambos géneros en esa pieza.

---

## 3. Prioridad vanidad / armadura: la regla real

`Terraria/Player.cs:36045-36047`, literal:

```csharp
head = ((GetEffectiveArmor(10).headSlot >= 0) ? GetEffectiveArmor(10).headSlot : GetEffectiveArmor(0).headSlot);
body = ((GetEffectiveArmor(11).bodySlot >= 0) ? GetEffectiveArmor(11).bodySlot : GetEffectiveArmor(1).bodySlot);
legs = ((GetEffectiveArmor(12).legSlot >= 0) ? GetEffectiveArmor(12).legSlot : GetEffectiveArmor(2).legSlot);
```

Tres cosas reales que se leen de ahí:

1. El criterio es **`slot >= 0` del objeto de vanidad**, no "el slot de vanidad no está
   vacío". Un slot vacío tiene `type = 0` y por tanto `headSlot/bodySlot/legSlot = -1`, así
   que en la práctica coincide con lo que hace hoy `EquipmentAppearanceResolver.Visible`
   (`TerrasavrNative.App/Services/EquipmentAppearanceResolver.cs:54-55`), pero la condición
   fiel es la del slot, no la del hueco.
2. **Esta regla es idéntica para las 12 variantes de cuerpo**: `skinVariant` no aparece en
   ninguna de las tres líneas. Queda confirmado que la prioridad vanidad>armadura **no
   cambia con la variante**.
3. Los índices son `armor[10/11/12]` para vanidad y `armor[0/1/2]` para funcional — el mismo
   reparto que ya usa `PlrLoadout.Social` / `PlrLoadout.Items`.

**`hideVisibleAccessory` no afecta a los tres slots de armadura.** El array es
`bool[10]` (`Player.cs:1691`), se serializa en `HideVisual1`/`HideVisual2`
(`Player.cs:55415-55426`) y se consulta **únicamente** en
`Player.cs:37063-37074`, dentro del bucle de accesorios, para decidir si llamar a
`UpdateVisibleAccessory`. Las líneas 36045-36047 no lo miran. **El "hueco real" que
`EquipmentAppearanceResolver.cs:24-29` documenta como alcance deliberado no existe para los
slots 0/1/2**: el juego real tampoco los oculta.

(Comprobación colateral: `Player.cs:55428` escribe `fileIO.Write((byte)newPlayer.skinVariant)`
justo después de `hideMisc`, exactamente donde `PlrBodySerializer.cs:91` lee `character.Gender`.
Confirma otra vez que `Gender` **es** `skinVariant`.)

---

## 4. Lo que SÍ depende de la variante de cuerpo

### 4.1 Cómo se cargan las 12 variantes

`Terraria/Initializers/PlayerDataInitializer.cs` (fichero completo, 142 líneas). Lo esencial:

- `:12` — `TextureAssets.Players = new Asset<Texture2D>[PlayerVariantID.Count, PlayerTextureID.Count]`
- `:27-33` `LoadVariant(int ID, int[] pieceIDs)` → pide `"Images/Player_" + ID + "_" + pieceIDs[i]`
- `:35-41` `CopyVariant(int to, int from)` → copia **las 16 piezas** de una variante a otra

Tabla real de herencia y sustitución, tal cual está en el fichero:

| Variante | Copia de | Piezas propias reales |
|---|---|---|
| 0 MaleStarter | — | `0..13, 15` (`:45-49`); la 14 es `Asset.Empty` (`:50`) |
| 1 MaleSticker | **0** (`:55`) | `4, 6, 8, 11, 12, 13` (`:56`) |
| 2 MaleGangster | **0** (`:61`) | `4, 6, 8, 11, 12, 13` (`:62`) |
| 3 MaleCoat | **0** (`:67`) | `4, 6, 8, 11, 12, 13, **14**` (`:68`) |
| 4 FemaleStarter | **0** (`:79`) | `3..13` (`:80-84`) |
| 5 FemaleSticker | **4** (`:89`) | `4, 6, 8, 11, 12, 13` (`:90`) |
| 6 FemaleGangster | **4** (`:95`) | `4, 6, 8, 11, 12, 13` (`:96`) |
| 7 FemaleCoat | **4** (`:101`) | `4, 6, 8, 11, 12, 13, **14**` (`:102`) |
| 8 MaleDress | **0** (`:73`) | `4, 6, 8, 11, 12, 13, **14**` (`:74`) |
| 9 FemaleDress | **4** (`:107`) | `4, 6, 8, 11, 12, 13` (`:108`) |
| 10 MaleDisplayDoll | **0** (`:113`) | `0, 2, 3, 5, 7, 9, 10` (`:114`) + remapeos (`:115-124`) |
| 11 FemaleDisplayDoll | **10** (`:129`) | `3, 5, 7, 9, 10` (`:130`) + remapeos (`:131-140`) |

**Comprobación de fichero (la hice yo, listando `Content\Images\`)**: los `.xnb` presentes
en la instalación real coinciden **exactamente** con esa tabla, variante a variante:

```
Var 0:  0 1 2 3 4 5 6 7 8 9 10 11 12 13 15
Var 1:  4 6 8 11 12 13
Var 2:  4 6 8 11 12 13
Var 3:  4 6 8 11 12 13 14
Var 4:  3 4 5 6 7 8 9 10 11 12 13
Var 5:  4 6 8 11 12 13
Var 6:  4 6 8 11 12 13
Var 7:  4 6 8 11 12 13 14
Var 8:  4 6 8 11 12 13 14
Var 9:  4 6 8 11 12 13
Var 10: 0 2 3 5 7 9 10
Var 11: 3 5 7 9 10
```

Es decir: **`Player_1/2/3/5/6/7/8/9_{4,6,8,11,12,13}.xnb` SÍ existen de verdad en el disco**.
El comentario de cabecera actual de `PlayerPreviewRenderer.cs:42-45` y el de
`scripts/extraer-sprites-jugador.js` afirman que "las variantes 1/2/3/5-11 solo sustituyen un
subconjunto y heredan el resto de 0/4"; eso es correcto, pero **el subconjunto que sustituyen
es toda la ropa**: camiseta interior (4), camisa (6), manga interior (8), pantalones (11),
zapatos (12) y manga (13). Es exactamente lo que se ve en el doll.

Lo que el renderer usa hoy (`PlayerPreviewRenderer.cs:119`) es
`string variant = isMale ? "body0" : "body4";` — nunca `body1..3` ni `body5..9`, aunque
`TerrasavrNative.App/Assets/player/body1`, `body2` y `body3` **ya existen extraídos** con seis
piezas cada uno (`armshirt, armundershirt, pants, shirt, shoes, undershirt`) — es decir, ya
hay assets de variante alternativa en el repo que el código no consume.

### 4.2 La pieza 14 (`Extra`) = el faldón del vestido/abrigo

`DrawPlayer_15_SkinLongCoat` (`PlayerDrawLayers.cs:1779-1791`), condición literal de
`:1781`:

```csharp
if ((drawinfo.skinVar == 3 || drawinfo.skinVar == 8 || drawinfo.skinVar == 7)
    && (drawinfo.drawPlayer.body <= 0 || drawinfo.drawPlayer.body >= ArmorIDs.Body.Count)
    && !drawinfo.drawPlayer.invis)
```

Y dibuja (`:1788`) `TextureAssets.Players[skinVar, 14]` sobre `drawPlayer.legFrame`, tintado
con `drawinfo.colorShirt`.

Tres cosas reales:

- Solo aplica a `skinVar` **3, 8 y 7** — **`9` (FemaleDress) NO está**, coherente con que
  `Player_9_14.xnb` no exista en el disco (lo comprobé arriba).
- El tinte es el de la **camisa** (`colorShirt`), no el de los pantalones.
- **Se dibuja solo si NO hay armadura de cuerpo puesta.** Con armadura/vanidad de cuerpo, el
  juego no pinta el faldón.

### 4.3 `SetMatch`: el legSlot y el headSlot cambian según el conjunto Y según el género

`Player.cs:36052-36092` llama a `SetMatch` **tres veces** en cadena, y cada resultado
sobrescribe `legs` o `head`:

```csharp
wearsRobe = false;
int num = SetMatch(new SetMatchRequest { ..., Male = Male, ArmorSlotRequested = 1 }, ref wearsRobe);
if (num != -1) legs = num;                       // :36063-36066
num = SetMatch(new SetMatchRequest { ..., ArmorSlotRequested = 2 }, ref somethingSpecial);
if (num != -1) legs = num;                       // :36076-36079
num = SetMatch(new SetMatchRequest { ..., ArmorSlotRequested = 0 }, ref somethingSpecial);
if (num != -1) head = num;                       // :36089-36092
```

`SetMatch` está en `Player.cs:37458-37694`. Extracto de las entradas **que dependen del
género** (`bool male = request.Male;`, `:37462`):

**`ArmorSlotRequested == 0` (head → head), `:37470-37473`:**

| `head` | resultado |
|---|---|
| 201 | `male ? 201 : 202` (salvo montura 54) |

**`ArmorSlotRequested == 1` (body → legs), `:37474-37584`** — las que dependen del género:

| `body` | resultado |
|---|---|
| 165 | `!male ? 99 : 118` (`:37512-37514`) |
| 166 | `!male ? 100 : 119` (`:37515-37518`), **y además `flag = false`** → no marca `wearsRobe` |
| 167 | `male ? 101 : 102` (`:37519-37521`) |
| 183 | `male ? 136 : 123` (`:37528-37530`) |

Las que **no** dependen del género (mismo bloque): 15→88, 36→89, 41→97, 42→90, 58→91, 59→92,
60→93, 61→94, 62→95, 63→96, 77→121, 180→115, 181→116, 191→131, **93→165**, 90→166, 88→168,
81→169 (solo si `request.Legs` es −1 o 0), 213→187, 215→189, 219→196, 221→199, 223→204,
231→214, 232→215, 233→216, 241→229, 256→244.

`:37580-37583`: `if (num2 != -1) somethingSpecial = flag;` — con `flag = true` por defecto
(`:37476`). Es decir, **cualquier `body` de esa lista salvo el 166 pone `wearsRobe = true`**.

**`ArmorSlotRequested == 2` (legs → legs), `:37585-37692`** — casi todo depende del género:

| `legs` | resultado |
|---|---|
| 83 | 117 solo si `male` |
| 84 | 120 solo si `male` |
| 132 | 135 solo si `male` |
| 57 | 137 solo si `male` |
| 158 | 157 solo si `male` |
| 180 | 179 solo si `!male` |
| 184 | 183 solo si `!male` |
| 191 | 192 solo si `!male` |
| 193 | 194 solo si `!male` |
| 197 | 198 solo si `!male` |
| 203 | 202 solo si `!male` |
| 208 | 207 solo si `!male` |
| 219 | 220 solo si `!male` |
| 232 | 233 solo si `!male` |
| 236 | 248 solo si `!male` |
| 249 | 250 solo si `!male` |
| 146 | `male ? 146 : 147` |
| 154 | `male ? 155 : 154` |

### 4.4 `wearsRobe` invierte el orden de zapatos y perneras

`LegacyPlayerRenderer.cs:195-204`, literal:

```csharp
if (drawInfo.drawPlayer.wearsRobe && drawInfo.drawPlayer.body != 166) {
    PlayerDrawLayers.DrawPlayer_14_Shoes(ref drawInfo);
    PlayerDrawLayers.DrawPlayer_13_Leggings(ref drawInfo);
} else {
    PlayerDrawLayers.DrawPlayer_13_Leggings(ref drawInfo);
    PlayerDrawLayers.DrawPlayer_14_Shoes(ref drawInfo);
}
```

Y en `DrawPlayer_13_Leggings:1540`, `wearsRobe` también relaja la condición de que los zapatos
tapen las perneras.

### 4.5 `GetMatchingBodyExtension`: el faldón largo de la ARMADURA, con variante por género

`PlayerDrawLayers.cs:1850-1926`. Mapea un `bodySlot` a un `legSlot` extra que se dibuja en
`DrawPlayer_16_ArmorLongCoat` (`:1793-1823`) usando `TextureAssets.ArmorLeg[...]` sobre
`legFrame`. Entradas dependientes del género:

| `body` | `legSlot` extra | Cita |
|---|---|---|
| 52 | `!Male ? 172 : 171` | `:1883` |
| 53 | `!Male ? 176 : 175` | `:1892` |
| 210 | `!Male ? 177 : 178` | `:1895` |
| 211 | `!Male ? 181 : 182` | `:1898` |
| 222 | `!Male ? 200 : 201` | `:1904` |

Independientes del género: 200→149, 202→151, 201→150, 209→160, 207→161, 198→162, 182→163,
168→164, 73→170, 187→173, 205→174, 218→195, 225→206, 236→221, 237→223, 89→186, 81→169,
251→238. Y `GetMatchingBodyExtensionBack` (`:1840-1848`): `body 251 → 239`.

### 4.6 Zapatos: sustitución masculino→femenino

`Player.cs:37196-37199`:

```csharp
if (item.shoeSlot > 0) {
    shoe = item.shoeSlot;
    if (!Male && ArmorIDs.Shoe.Sets.MaleToFemaleID[shoe] > 0)
        shoe = (sbyte)ArmorIDs.Shoe.Sets.MaleToFemaleID[shoe];
}
```

`ArmorIDs.cs:1869`: `MaleToFemaleID = Factory.CreateIntSet(-1, 25, 26);` → un único par real:
`shoeSlot 25 → 26` en femenino. (Fuera del alcance del doll, que no dibuja accesorios de pie,
pero lo dejo anotado porque es otra dependencia real del género.)

### 4.7 Capa trasera de la armadura, también con variante femenina

`Player.cs:36111`:

```csharp
sbyte b = (sbyte)(Male ? ArmorIDs.Body.Sets.IncludedCapeBack : ArmorIDs.Body.Sets.IncludedCapeBackFemale)[body];
```

`ArmorIDs.cs:651`: `IncludedCapeBackFemale = Factory.CreateIntSet(-1, 207, 13, 206, 12, 205, 11, 185, 17, 96, 18, 94, 19, 80, 21, 217, 23, 24, 29, 238, 32);`
(Fuera del alcance actual del doll, que no dibuja capas.)

### 4.8 Lo que NO depende de la variante

- **El tintado.** Los colores que se pasan a cada `DrawData` (`colorBodySkin`, `colorShirt`,
  `colorUnderShirt`, `colorPants`, `colorShoes`, `colorHair`, `colorArmorBody`,
  `colorArmorHead`, `colorArmorLegs`) son los mismos campos en todas las llamadas, sin
  ninguna rama por `skinVar` en las capas leídas (15, 16, 17, 21, 23, 27). La armadura
  siempre va con `colorArmorBody`/`colorArmorHead`/`colorArmorLegs`, **nunca teñida por los
  colores del personaje** — que es lo que ya hace el renderer al pasar `null` como tinte.
- **La prioridad vanidad/armadura** (sección 3).
- **La rejilla 9x4 y las 5 celdas del frame de reposo** — el `+2` femenino es el único ajuste
  y ya está implementado.
- **`hidesTopSkin`/`hidesBottomSkin`** (`PlayerDrawSet.cs:1795-1796`): dependen solo de
  `body` y `legs`, no de `skinVariant`.

---

## 5. El camino de framing ANTIGUO es código muerto en 1.4.5.8

`ArmorIDs.cs:673` define `Body.Sets.UsesNewFramingCode` como un bool-set con `false` por
defecto y `true` para los ids **1..106** y **165..261**. Los ids 107..164 usarían el camino
antiguo (`DrawPlayer_17_Torso:1934-1964` con `TextureAssets.ArmorBody[]` o
`TextureAssets.FemaleBody[]`, cargados en `AssetInitializer.cs:474` y `:470` desde
`"Images/Armor_Body_"` y `"Images/Female_Body_"`).

**Comprobación de fichero que hice yo:**

- `Content\Images\Armor\Armor_N.xnb` existe **exactamente** para `N ∈ {1..106} ∪ {165..261}` —
  el mismo conjunto que `UsesNewFramingCode`, ni uno más ni uno menos.
- **`Armor_Body_N.xnb`, `Female_Body_N.xnb` y `Armor_Arm_N.xnb` NO existen en absoluto** en
  la instalación real (`ls Content\Images\ | grep -c '^Armor_Body_'` → 0; ídem los otros dos).
- Ningún ítem de `vanilla_armor_slots.json` usa un `bodySlot` en 107..164 (lo verifiqué
  recorriendo el JSON: 170 bodySlots distintos, ninguno en ese rango).

**Conclusión real: para vanilla 1.4.5.8 la hoja compuesta `Images/Armor/Armor_{n}.xnb` es
siempre la correcta.** La suposición actual del renderer (`LoadArmorCell` para todo) es
válida, y no hay que implementar `FemaleBody[]`.

---

## 6. El caso real que falla: "Eldelgas"

### 6.1 Datos reales del `.plr`

Leídos por mí de `Eldelgas.plr` (versión 326):

```
Gender (skinVariant) = 8        HairStyle = 20      HairDye = 0
hideVisual1 = 248  hideVisual2 = 1  hideMisc = 3
armadura [0..2] = 2763, 2764, 2765   (Casco / Coraza / Perneras de fulguración solar)
vanidad  [0..2] = 1819, 1820, 0      (Caperuza de la Muerte / Vestido de la Muerte / vacío)
colores: shirt=(201,194,0) under=(102,135,191) pants=(183,175,255) shoes=(60,81,160)
```

`hideVisual1 = 248 = 0b11111000` → los bits puestos son 3..7, y `hideVisual2 = 1` → bit 0 del
segundo byte = slot 8. Los slots **0, 1 y 2 no están ocultos**, y de todas formas
(sección 3) eso no afectaría a la armadura.

Slots visuales resultantes: `head = 131` (de 1819), `body = 93` (de 1820),
`legs =` legSlot de 2765 (que **no está** en `vanilla_armor_slots.json`, así que hoy
`EquipmentAppearanceResolver.ResolveLegs` devuelve `null` y el doll no pinta perneras).

### 6.2 `bodySlot 93` es un caso especial citado por nombre cuatro veces

| Efecto real | Cita |
|---|---|
| `hidesTopSkin = ... \|\| drawPlayer.body == 93 \|\| ...` | `PlayerDrawSet.cs:1795` |
| `hidesBottomSkin = drawPlayer.body == 93 \|\| ...` | `PlayerDrawSet.cs:1796` |
| `if (body == 93) { shield = 0; handoff = 0; }` | `Player.cs:36093-36097` |
| `case 93: num2 = 165;` en `SetMatch(ArmorSlotRequested=1)` | `Player.cs:37534-37536` |

Y `missingArm = (body != 83)` → **true** (`PlayerDrawSet.cs:377-385`); `missingHand` →
**false**, porque 93 no está en la lista larga de `:373`.

Además, la celda `(2,2)` (brazo trasero) de `Armor/Armor_93.xnb` mide, según mi medición
directa, **540 px opacos con bounding box x[10..37] y[2..53]** — casi la celda entera. No es
un brazo: es la túnica larga completa. Compárese con la celda (2,2) de `Armor/Armor_1.xnb`
(peto de cobre): 32 px, bbox x[28..31] y[34..43].

### 6.3 Qué dibuja el juego real, paso a paso

Con `skinVar = 8`, `Male = true`, `body = 93`, `head = 131`, `legs = 165` (tras `SetMatch`),
`wearsRobe = true`, `hidesTopSkin = true`, `hidesBottomSkin = true`, `missingArm = true`,
`missingHand = false`:

1. `DrawPlayer_12_Skin_Composite:1255` — `if (!hidesTopSkin && ...)` → **no dibuja TorsoSkin**.
   `:1285` — `if (!hidesBottomSkin && ...)` → **no dibuja LegSkin**.
2. `DrawPlayer_12_SkinComposite_BackArmShirt`:
   - `:1318-1322` → `flag = true`, `flag2 = true`, `flag3 = true`, `flag5 = !hidesTopSkin = false`.
   - `:1326` → `flag &= missingHand` → **`flag = false`**.
   - `:1327-1345` → `flag2 && missingArm` es cierto, pero `flag5` es falso: **no dibuja ni
     piel de brazo (7) ni mano (5)**; deja `flag2 = false`.
   - `:1351` → armadura en `compBackShoulderFrame`. `:1365` → armadura en `compBackArmFrame`.
   - `:1379` `if (flag)` es falso → **no dibuja ArmUndershirt (8) ni ArmShirt (13)**.
3. `wearsRobe` → `DrawPlayer_14_Shoes` antes que `DrawPlayer_13_Leggings`
   (`LegacyPlayerRenderer.cs:196-199`). `DrawPlayer_13_Leggings:1540` → `legs = 165 > 0` →
   dibuja `TextureAssets.ArmorLeg[165]` (la falda). **No** llega a `:1576`, así que **no
   dibuja Pants (11) ni Shoes (12) de la piel**.
4. `DrawPlayer_15_SkinLongCoat:1781` → `body > 0` → **no dibuja la pieza 14 de la variante 8**.
5. `DrawPlayer_16_ArmorLongCoat` → `GetMatchingBodyExtension(93)` no está en la tabla → −1,
   nada que dibujar.
6. `DrawPlayer_17_TorsoComposite:2002-2011` → `flag = true`, dibuja la armadura en
   `compTorsoFrame`. `:2022` `if (!flag && ...)` es falso → **no dibuja Undershirt (4) ni
   Shirt (6), ni en el hombro trasero ni en el torso**.
7. `DrawPlayer_21_Head` → cara/ojos + casco 131 (+ pelo según `GetHairSettings`).
8. `DrawPlayer_28_ArmOverItemComposite:3711` `num = true`; `:3713-3716`
   `num2 = 1`, `num3 = 0`, `num4 = 0`, `flag2 = !hidesTopSkin = false`.
   - `i = 0`: `:3724` `(i == num4) & flag2` → falso → **sin piel de brazo/mano**;
     `:3764` `i == num3` → **armadura en `compFrontArmFrame`**.
   - `i = 1`: `:3741` `i == num2` → **armadura en `compFrontShoulderFrame`**.
   - `:3782` `else` no se ejecuta → **sin ArmUndershirt/ArmShirt/Shirt delanteros**.

**En total el juego real dibuja, de cuerpo: solo cinco recortes de `Armor/Armor_93` + la
falda `Armor_Legs_165` + cabeza/ojos/pelo + casco `Armor_Head_131`. Nada de piel de torso,
piel de piernas, camisa, camiseta, pantalones ni zapatos.**

### 6.4 Verificación empírica que hice yo

Reimplementé en Node los dos caminos (el actual de `PlayerPreviewRenderer.Render` y el real
según lo de arriba) sobre los sprites reales, con los colores reales de Eldelgas, y comparé
las dos imágenes a escala 6x:

- **Render actual (A)**: se ve una camisa **amarilla** (`shirt = 201,194,0`) pintada encima
  del pecho de la túnica, pantalones **lilas** y zapatos **azules** por debajo, y una mano de
  piel asomando. La túnica del Segador solo se ve en los bordes.
- **Render "juego real" (B)**: la figura completa del Segador, túnica morada de arriba abajo,
  guadaña, capucha, sin ningún resto de ropa base.

La imagen A reproduce **exactamente** el síntoma reportado ("la ropa de vanidad no se muestra
correctamente"). La B es la correcta. Los cuatro PNG están en el scratchpad de esta sesión
(`A-terrakeep-actual.png`, `B-juego-real.png`, `C-variante8-sin-armadura-real.png`,
`D-variante0-sin-armadura-terrakeep.png`); el guion que los genera es `render-comparar.js`.

### 6.5 De dónde sale el "cuerpo delgado"

También compuse la variante 8 **sin armadura** (C) frente a la variante 0 (D), con el mismo
pelo y los mismos colores:

- **C (skinVar 8, MaleDress, real)**: un vestido/túnica largo oliva que llega hasta los pies,
  silueta estrecha y continua.
- **D (skinVar 0, lo que hace hoy Terrakeep)**: camiseta corta y pantalones, silueta ancha
  de dos bloques.

Es decir: **"el tipo de cuerpo delgado" es la variante de vestido/abrigo real del juego**
(3 MaleCoat, 7 FemaleCoat, 8 MaleDress, 9 FemaleDress), y Terrakeep la colapsa hoy siempre a
la 0/4. La hipótesis de partida del encargo era correcta en el fondo: el usuario describía el
síntoma visual de un `skinVariant` distinto de 0/4. El matiz es que el mecanismo que rompe la
vanidad no es el recorte de celda (ese está bien), sino las banderas de `body = 93` y
`SetMatch`.

---

## 7. Especificación: qué tiene que hacer `PlayerPreviewRenderer`

### 7.1 Entradas que necesita y hoy no tiene

| Entrada | De dónde sale hoy | Qué hace falta |
|---|---|---|
| `skinVariant` completo (0..11) | `PlrCharacter.Gender` (ya está) | pasarlo entero, no solo `IsMale` |
| `bodySlot` visual real | `EquipmentAppearanceResolver` lo resuelve a ruta, no a id | exponer también el **id** de `bodySlot` |
| `legSlot` visual real | ídem | exponer también el **id** de `legSlot` |
| `headSlot` visual real | ya se expone (`EquippedArmor.HeadSlot`) | sin cambios |

### 7.2 Reglas derivadas que hay que calcular antes de dibujar

Con `body` = bodySlot visual (0 si no hay), `legs` = legSlot visual (0 si no hay),
`head` = headSlot visual, `v` = skinVariant, `male = PlayerVariantSets.IsMale(v)`:

```
1) legs = SetMatch_Body(body, male, legs)  ?? legs      // Player.cs:37474-37584
   wearsRobe = (ese SetMatch dio resultado) && body != 166
2) legs = SetMatch_Legs(legs, male)        ?? legs      // Player.cs:37585-37692
3) head = SetMatch_Head(head, male)        ?? head      // Player.cs:37470-37473
4) hidesTopSkin    = body ∈ {21, 22, 82, 83, 93}        // PlayerDrawSet.cs:1795
   hidesBottomSkin = body == 93 || legs ∈ {20, 21, 214, 215, 216}   // :1796
5) missingArm  = (body != 83)                            // PlayerDrawSet.cs:378-385
   missingHand = body ∈ {la lista larga de PlayerDrawSet.cs:373}
6) bodyExtension = GetMatchingBodyExtension(body, male)  // PlayerDrawLayers.cs:1850-1926
7) hasBody = (body > 0)
```

(Para el doll en reposo no hacen falta `hideCompositeShoulders`, `compShoulderOverFrontArm`
ni los offsets de hombro: ver 7.4.)

### 7.3 Orden EXACTO de capas para el frame de reposo

Traducido literalmente de `LegacyPlayerRenderer.cs:184-239` + el cuerpo de cada capa. `V(n)`
= pieza `n` de la variante `v` **con la herencia de la sección 4.1 resuelta**; `A` =
`Armor/Armor_{body}`; celdas según la tabla de 2.3.

```
 1. si backHairDraw y no hideHair:  pelo completo (capa trasera)        [01_BackHair]
                                                                        [12_Skin_Composite]
 2. si !hidesTopSkin:               V(3)  @ Torso        tinte piel
 3. si !hidesBottomSkin:            V(10) @ frame0       tinte piel
                                                     [12_SkinComposite_BackArmShirt]
 4. si hasBody:
      4a. si missingArm && !hidesTopSkin:   V(7) @ BackArm   tinte piel
      4b. si missingArm && !hidesTopSkin:   V(5) @ BackArm   tinte piel
      4c.                                   A    @ BackShoulder   sin tinte
      4d.                                   A    @ BackArm        sin tinte
    si !hasBody:
      4e. si !hidesTopSkin:                 V(7) @ BackArm   tinte piel
      4f. si !hidesTopSkin:                 V(5) @ BackArm   tinte piel
      4g.                                   V(8) @ BackArm   tinte camiseta
      4h.                                   V(13)@ BackArm   tinte camisa
 5. si wearsRobe:  zapatos, luego perneras.  Si no: perneras, luego zapatos.
      perneras: si legs > 0        -> Armor_Legs_{legs} @ frame0, sin tinte
                si no              -> V(11) @ frame0 tinte pantalones
                                      V(12) @ frame0 tinte zapatos
      zapatos:  (shoeSlot, fuera del alcance del doll)
 6. si v ∈ {3,7,8} y !hasBody:      V(14) @ frame0       tinte CAMISA   [15_SkinLongCoat]
 7. si bodyExtension != -1:         Armor_Legs_{bodyExtension} @ frame0 [16_ArmorLongCoat]
                                                                        [17_TorsoComposite]
 8. si hasBody:                     A    @ Torso         sin tinte
    si no:                          V(4) @ BackShoulder  tinte camiseta
                                    V(6) @ BackShoulder  tinte camisa
                                    V(4) @ Torso         tinte camiseta
                                    V(6) @ Torso         tinte camisa
 9. cabeza/ojos/pelo/casco (ver 7.5)                                    [21_Head]
                                                            [28_ArmOverItemComposite]
10. si hasBody:
      10a. si missingArm  && !hidesTopSkin:  V(7) @ FrontArm  tinte piel
      10b. si missingHand && !hidesTopSkin:  V(9) @ FrontArm  tinte piel
      10c.                                   A    @ FrontArm       sin tinte
      10d.                                   A    @ FrontShoulder  sin tinte
    si !hasBody:
      10e. si !hidesTopSkin:  V(7) @ FrontArm  tinte piel
      10f.                    V(8) @ FrontArm  tinte camiseta
      10g.                    V(13)@ FrontArm  tinte camisa
      10h.                    V(6) @ FrontArm  tinte camisa
      10i. si !hidesTopSkin:  V(7) @ FrontShoulder tinte piel
      10j.                    V(8) @ FrontShoulder tinte camiseta
      10k.                    V(13)@ FrontShoulder tinte camisa
      10l.                    V(6) @ FrontShoulder tinte camisa
```

Citas de los puntos no obvios:

- **4a/4b**: `PlayerDrawLayers.cs:1326-1345`. `flag &= missingHand` (`:1326`); el bloque
  `if (flag2 && missingArm)` (`:1327`) dibuja 7 y 5 solo si `flag5 = !hidesTopSkin` (`:1329`,
  `:1336`).
- **4c/4d**: `:1351` (hombro trasero) y `:1365` (brazo trasero), en ese orden.
- **4g/4h**: `:1399-1404`, `if (!flag3)` — es decir **solo si no hay armadura de cuerpo**.
- **6**: `:1781` (condición) y `:1788` (`colorShirt`).
- **8**: `:2002-2011` (armadura) y `:2022-2027` (`if (!flag)`: los cuatro dibujos de la ropa
  base, en el orden hombro-trasero → torso).
- **10c antes que 10d**: `num2 = 1`, `num3 = 0` con `compShoulderOverFrontArm = true`
  (`PlayerDrawSet.cs:1937`, y el `case 0:` de `:1960-1963` no lo cambia). El bucle
  `for (int i = 0; i < 2; i++)` de `:3722` ejecuta primero `i == num3` (brazo, `:3764`) y
  luego `i == num2` (hombro, `:3741`). **Esto es lo contrario de lo que hace el renderer hoy**
  (`PlayerPreviewRenderer.cs:195-205`, hombro y después brazo).
- **10e..10l**: `:3784-3826`, mismo bucle, mismo orden brazo→hombro; el hombro añade también
  la pieza 6 (`:3799`) y el brazo también (`:3824`).
- **10b usa la pieza 9 (`ArmHand`), no la 5**: `:3735`. **La pieza 9 no está extraída hoy**
  (`scripts/extraer-sprites-jugador.js` no la incluye en su array `PIECES`).

### 7.4 Offsets: por qué el renderer actual acierta al componer todo en (0,0)

- `GetCompositeOffset_BackArm` = `(6, 2)` y `GetCompositeOffset_FrontArm` = `(-5, 0)`
  (`PlayerDrawLayers.cs:4208-4216`, sin efectos de espejo).
- Pero en **las tres capas de brazo**, el mismo offset se suma a la posición **y al origen**:
  `PlayerDrawLayers.cs:1313-1316` (`vector3 += offset; bodyVect += offset;`) y `:3702-3704`
  (`bodyVect += offset; vector += offset;`). Como `DrawData` dibuja en `posición − origen`,
  **el desplazamiento neto es cero**.
- El offset de cabecera: `vector2 = Main.OffsetsPlayerHeadgear[0]` con `vector2.Y -= 2f`
  (`:1259-1261`, `:1305-1307`, `:1991-1993`, `:3696-3698`). `Main.cs:531` da
  `OffsetsPlayerHeadgear[0] = new Vector2(0f, 2f)` → **neto (0,0)** para el frame de reposo.
- `backShoulderOffset` / `frontShoulderOffset` (`:1315`, `:3705`) solo son distintos de cero
  para `body ∈ {55, 71, 101, 183, 201, 204, 207}` (`PlayerDrawSet.cs:2056-2082`) **y solo
  para `targetFrameNumber` entre 6 y 19** — nunca para el 0 del reposo (el `switch` de
  `:2086` y siguientes no tiene `case 0`).
- `torsoOffset` (`:1258`, `:1308`) solo se altera con monturas.

**Conclusión: para el frame de reposo, todas las celdas se superponen en el mismo origen. El
comentario actual de `PlayerPreviewRenderer.cs:37-40` es correcto y no hay que cambiarlo.**

### 7.5 Cabeza y pelo

`DrawPlayer_21_Head` (`PlayerDrawLayers.cs:2093`+):

- `:2098` dibuja primero la cara (`DrawPlayer_21_Head_TheFace`, `:2574`).
- `:2143-2161` **`fullHair`: casco PRIMERO (`:2152`), pelo DESPUÉS (`:2157`)**. El renderer
  actual hace lo contrario (`PlayerPreviewRenderer.cs:181-190`: pelo y luego casco).
- `:2162-2167` **`hatHair`: `PlayerHairAlt` primero**, el casco va después, en la cadena
  `else if` de más abajo. Aquí el renderer actual sí coincide.
- El caso "casco completo sin pelo" y el caso "sin casco, pelo normal" también coinciden.
- `hairFrontFrame.Height = 26` cuando `backHairDraw` (`PlayerDrawSet.cs:1790-1794`) — ya
  implementado en `CompositeTopRows`.

### 7.6 Assets que hay que extraer y hoy no están

Todas estas comprobaciones las hice yo cruzando el contenido real de
`TerrasavrNative.App/Assets/player/` con `Content\Images\`:

1. **Variantes de cuerpo 1, 2, 3, 5, 6, 7, 8, 9.** Faltan por completo `body5..body9`;
   `body1/2/3` existen pero solo con seis piezas y **sin la 14**. Hay que extraer, por cada
   variante, `Player_{v}_{4,6,8,11,12,13}.xnb` (+ `_14` para 3, 7 y 8) y resolver la herencia
   de la sección 4.1 (5/6/7/9 caen a `body4`; 1/2/3/8 caen a `body0`).
2. **Pieza 9 (`ArmHand`)** de las variantes 0 y 4 — no está en el array `PIECES` de
   `extraer-sprites-jugador.js`. Hace falta para el punto 10b.
3. **`armor_legs` sintéticos de `SetMatch` y `GetMatchingBodyExtension`.** Hoy hay 143
   ficheros, todos correspondientes a ítems reales. **Faltan los que solo produce el código**:
   - de `SetMatch(body→legs)`: 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 99, 100, 101, 102,
     115, 116, 118, 119, 121, 123, 131, 136, 165, 166, 168, 169, 187, 189, 196, 199, 204,
     214, 215, 216, 229, 244
   - de `SetMatch(legs→legs)`: 117, 120, 135, 137, 147, 155, 157, 179, 183, 192, 194, 198,
     202, 207, 220, 233, 248, 250
   - de `GetMatchingBodyExtension`: 149, 150, 151, 160, 161, 162, 163, 164, 169, 170, 171,
     172, 173, 174, 175, 176, 177, 178, 181, 182, 186, 195, 200, 201, 206, 221, 223, 238, 239

   **Comprobé que los `Armor_Legs_{n}.xnb` de los tres grupos existen todos en la instalación
   real** — no hay ni uno que falte en disco. Solo hay que ampliar el conjunto de ids que
   recorre `extraer-sprites-armadura-vanilla.js` (hoy solo los que salen de
   `vanilla_armor_slots.json`).
4. **`armor_head/202.png`** (el único headSlot sintético de `SetMatch`, `Player.cs:37472`).
   `Armor_Head_202.xnb` existe en disco.

### 7.7 Cambios concretos en `PlayerPreviewRenderer.cs`

1. **Firma**: `Render(int hairStyle, byte skinVariant, PlayerColors colors, EquippedArmor armor)`
   en lugar de `bool isMale`. `isMale` se sigue derivando con
   `PlayerVariantSets.IsMale(skinVariant)` para elegir las celdas (2.3), pero la carpeta de
   sprites pasa a ser la variante real con su herencia (4.1).
2. **`EquippedArmor`**: añadir `int? BodySlot` y `int? LegsSlot` además de las rutas, para
   poder aplicar `SetMatch`, `hidesTopSkin`, `hidesBottomSkin`, `missingArm`, `missingHand`
   y `GetMatchingBodyExtension`. `EquipmentAppearanceResolver` ya calcula esos ids
   internamente en `ResolveVanillaPath` (`EquipmentAppearanceResolver.cs:89-94`).
3. **Tablas nuevas en `TerrasavrNative.Core`** (mismo patrón que `HairDrawProfile`): las
   cuatro tablas de `SetMatch` (secciones 4.3), la de `GetMatchingBodyExtension` (4.5), las
   listas de `hidesTopSkin`/`hidesBottomSkin` (7.2 punto 4) y la lista de `missingHand`
   (`PlayerDrawSet.cs:373`).
4. **Reescribir el cuerpo de `Render`** siguiendo literalmente el orden de 7.3. Los tres
   cambios de mayor impacto: no dibujar la ropa base cuando `hasBody`, respetar
   `hidesTopSkin`/`hidesBottomSkin`, y elegir perneras-de-armadura **o** pantalones+zapatos,
   nunca los dos.
5. **Invertir hombro/brazo delantero** (`:195-205`) → brazo primero, hombro después.
6. **Invertir casco/pelo en el caso `fullHair`** (`:181-190`).
7. **Quitar el dibujo de la armadura en `backShoulderCell` del paso 4 actual**
   (`PlayerPreviewRenderer.cs:172`): esa capa existe, pero pertenece al paso del **brazo
   trasero** (punto 4c de 7.3), no al del torso.
8. **Calamity**: `CalamityIds`/`CalamityCatalog` no da `bodySlot`/`legSlot` vanilla, así que
   para una pieza de Calamity no se pueden evaluar `SetMatch` ni las banderas. El
   comportamiento fiel-por-defecto es tratarla como `hasBody = true` con
   `hidesTopSkin = hidesBottomSkin = false` y sin `SetMatch` — que es lo que ya hace hoy el
   camino de Calamity.

---

## 8. INFERENCIAS mías (no son lectura directa de código ni comprobación de fichero)

Todo lo anterior está citado. Lo que sigue es razonamiento propio y hay que tratarlo como tal:

1. **Que el usuario llame "cuerpo delgado" a la variante de vestido/abrigo** es una inferencia
   mía, apoyada en dos hechos verificados (Eldelgas tiene `skinVariant = 8`; la composición C
   frente a la D muestra una silueta claramente más estrecha), pero el usuario nunca dijo
   "variante 8" y yo no he visto su captura de pantalla.
2. **Que el síntoma concreto de la captura sea la camisa amarilla sobre la túnica** es
   inferencia: mi render A lo reproduce, pero no puedo contrastarlo con la imagen real.
3. **Que la variante alternativa no se pierda al cargar** (lo que dice
   `PlayerVariantSets.cs:19-25`) lo he dado por bueno leyendo ese comentario; no he
   re-verificado `AppearanceViewModel.OnIsMaleChanged` a fondo. Si el selector Chico/Chica
   dispara al cargar aunque el valor no cambie, un `skinVariant = 8` se degradaría a 0 al
   guardar — **conviene comprobarlo antes de implementar**, porque tocaría datos del usuario.
4. **Que ningún ítem de Calamity use un `bodySlot` vanilla que active `SetMatch`**: no lo he
   comprobado. Calamity define sus propios equip slots por registro en tModLoader, así que en
   principio no colisionan con los ids vanilla, pero no lo he verificado en el código de
   Calamity.
5. **Que el orden brazo→hombro delantero se note visualmente** en el frame de reposo: es
   inferencia. Lo correcto es implementarlo fiel al código real, pero no he medido si con las
   celdas de reposo (donde el hombro delantero de muchas piezas está vacío, como en
   `Armor_1`) la diferencia llega a ser visible.
6. **Que `wearsRobe` no afecte al doll más allá del orden zapatos/perneras**: el doll no
   dibuja `shoeSlot`, así que el intercambio de orden del punto 5 de 7.3 no tiene efecto
   visual hoy. Lo incluyo en la especificación por fidelidad, no porque cambie nada ahora.
7. **Que el `legSlot` del ítem 2765 (Perneras de fulguración solar) exista de verdad y solo
   falte en `vanilla_armor_slots.json`**: no lo he comprobado. Solo verifiqué que ese id no
   está en el JSON del proyecto, no por qué.

---

## 9. Huecos y limitaciones reales de esta investigación

- **No he ejecutado Terraria.** Todo el "dinámico" es simulación razonada sobre el código real
  más composición manual de los sprites reales con `pngjs`. La comparación A/B/C/D es una
  reimplementación mía en Node, no una captura del juego.
- **Solo el frame de reposo** (`bodyFrame.Y = 0`, `direction = 1`, sin montura, sin objeto en
  mano, sin sentarse, `gravDir = 1`). `CreateCompositeData` tiene 20 casos de `num`
  (`PlayerDrawSet.cs:1958-2030`) y `DrawPlayer_13_Leggings` tiene toda una rama de
  `isSitting`; nada de eso está cubierto ni hace falta para el doll.
- **No he abierto `ArmorIDs.Body.Sets`** más allá de `UsesNewFramingCode` (`:673`),
  `IncludedCapeBackFemale` (`:651`) y las referencias citadas. Hay más sets
  (`shouldersAreAlwaysInTheBack`, `DisableHandOnAndOffAccDraw`, `IncludeCapeFrontAndBack`,
  `HideHandOn/Off`...) que afectan a capas fuera del alcance del doll.
- **No he investigado `GetHairSettings`** de nuevo: doy por bueno lo ya implementado en
  `HairDrawProfile` (H6-07), salvo el orden casco/pelo de `fullHair`, que sí verifiqué.
- **Accesorios, alas, capas, objeto en mano y tintes de tinte (`shader`/`cBody`/`cHead`)**
  siguen fuera de alcance, como ya estaba documentado.
- **No hubo ningún bloqueo de herramienta** en esta investigación: `xnb-to-png.js`, `pngjs`,
  el lector `.plr` portado y el acceso al decompilado funcionaron a la primera. No hay nada
  que anotar en `bitacora.md` por ese lado.
