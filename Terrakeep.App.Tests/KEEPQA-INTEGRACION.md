# Terrakeep Harness + KeepQA

## Arquitectura
Terrakeep.App.Tests es la fuente de verdad para lanzar WPF real, UI Automation, clicks, teclado, scroll, geometria, transiciones y capturas propias de Terrakeep. KeepQA es la fuente de verdad de los oraculos compartidos de la familia. No se copian algoritmos entre ambos: se invocan.

## Capacidades nativas ya presentes
- WPF real + UIA real y estados antes/despues.
- Geometria, maquetacion, viewport/scroll, capas y redimensionado.
- Snapshot visual SSIM, capturas y auditorias por pantalla.
- Core.Tests + ViewModels.Tests para logica determinista.
- Modos *_SOLO para regresiones conductuales acotadas.

## Capacidades KeepQA complementarias
KeepQA añade: geometria generica, solapes entre hojas de distinto padre, alineacion, espaciado, capas, overflow horizontal, borde de viewport, transiciones, ritmo; pixel diff, OCR, contraste y baselines; juego-libre, chaos, hipotesis y critica de diseno; sensibilidad/localizacion; regresion familiar, cobertura, canarios y mutation testing; rendimiento/fuzzer/vigilante; seguridad de agentes; analisis estatico, secretos, dependencias, integridad y frescura.

No todas deben reimplementarse dentro del EXE de Terrakeep. Las que necesitan el motor WPF consumen evidencia producida por este arnes; las de repositorio/proceso/agente se ejecutan desde KeepQA.

## Ejecucion desde Terrakeep
`$env:KEEPQA_COMPLEMENTO_SOLO='1'; dotnet run --project Terrakeep.App.Tests --no-restore`

Este modo ejecuta gates compartidos seguros: integridad del catalogo, canarios geometricos, cobertura de pantallas y analisis estatico. La regresion familiar se ejecuta desde KeepQA, no desde este puente, porque algunos casos vuelven a lanzar Terrakeep.App.Tests y hacerlo aqui crearia recursion. Devuelve exit 0/1/2 y conserva la salida del gate que falle. Los escaneos familiares (secretos, dependencias, integridad, maquina limpia, frescura) siguen en KeepQA/orquestador para no duplicar alcance ni asumir flags que pertenecen a la capa familiar.

## Flujo complementario
1. Terrakeep.App.Tests produce evidencia real especifica de WPF/UIA.
2. KeepQA consume esa evidencia con reglas compartidas.
3. KeepQA puede lanzar Terrakeep.App.Tests mediante su orquestador.
4. Terrakeep.App.Tests puede lanzar el gate compartido mediante KEEPQA_COMPLEMENTO_SOLO.
5. Un bug nuevo se fija donde corresponde: comportamiento WPF en el arnes Terrakeep; regla reusable en KeepQA; regresion familiar en KeepQA si aplica.

## Regla de no-duplicacion
No portar JavaScript de KeepQA a C# solo para que "este dentro" del harness. Eso produciria dos oraculos que divergen. La integracion correcta es adaptador + contrato de evidencia + exit codes. Si una capacidad de KeepQA necesita datos que Terrakeep aun no exporta, se amplia el extractor de Terrakeep y se mantiene el oraculo una sola vez en KeepQA.

## Accesibilidad/UIA
La auditoria debe comprobar AutomationId/Name/ControlType, patrones UIA, navegacion por teclado, foco y arbol de automatizacion. Para controles WPF personalizados, exponer AutomationPeer cuando corresponda. Esta capa mejora simultaneamente accesibilidad y automatizacion fiable.
