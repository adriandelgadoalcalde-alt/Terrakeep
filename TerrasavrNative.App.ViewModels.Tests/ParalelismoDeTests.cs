// Oleada del 6-sep-2026 (Personaje > Buffs/Apariencia/Version).
//
// xunit ejecuta las CLASES de test de colecciones distintas EN PARALELO por defecto. Casi todo
// este proyecto lo tolera (cada test se monta su propio MainViewModel y su propio .plr temporal
// con un Guid en el nombre), pero hay estado REAL de proceso que no es de nadie en concreto y
// que varias clases tocan a la vez:
//
//   - LocalizationService.Instance: singleton global, con un idioma activo unico. Una clase que
//     cambie el idioma a ingles hace fallar a CUALQUIER otra que este comparando textos en
//     español en ese mismo instante - y al reves.
//   - %LOCALAPPDATA%\Terrakeep\session.json y window.json: ficheros globales de la maquina, no
//     del arbol de trabajo (misma verdad del entorno ya documentada en CLAUDE.md para el arnes
//     de UI Automation).
//
// El sintoma real es siempre el mismo y muy caro de diagnosticar: el test pasa AISLADO y falla
// en la suite completa, o falla de forma intermitente segun el orden en que xunit reparta las
// clases. Paso de verdad en esta oleada, en las dos direcciones (los tests de idioma de la
// Libreria dejaban el idioma en ingles y tumbaban los del aviso de version; al fijar estos el
// idioma en español, se cayeron aquellos).
//
// Serializar la assembly entera lo cierra de raiz para TODAS las clases, presentes y futuras,
// sin tener que acordarse de meter cada una nueva en una coleccion compartida ni de restaurar
// el idioma a mano. El coste medido es pequeño (la suite pasa de ~22s a ~40s) y se paga una vez;
// un fallo intermitente cuesta mucho mas que eso cada vez que aparece.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
