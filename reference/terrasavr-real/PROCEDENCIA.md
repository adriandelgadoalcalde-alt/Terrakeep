# Procedencia de `script.beautified.js`

Volcado legible (vía `js-beautify`) del `script.js` real y minificado de
Terrasavr-Calamity-Beta, para poder consultar la lógica real del motor
(clases `app.TabInventory`/`app.TabEdit`/`app.TabLibrary`/`app.TabEquips`,
fórmulas de tooltip, etc.) sin descompactar nada a mano cada vez.

- Origen: `Terrasavr-Calamity-Beta\resources\app\local-site\script.js`
  (fecha real del archivo en el momento de generar este volcado: 31-ago-2026).
- 10472 líneas, generado con `js-beautify` (paquete npm global).

## Cómo regenerarlo si `script.js` cambia

```
cd Terrasavr-Calamity-Beta\resources\app\local-site
js-beautify script.js -o ..\..\..\..\Terrasavr-Native\reference\terrasavr-real\script.beautified.js
```

Actualizar también la fecha de este fichero tras regenerar.
