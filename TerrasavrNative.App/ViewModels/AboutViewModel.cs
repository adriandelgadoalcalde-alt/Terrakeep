using System.Reflection;

namespace TerrasavrNative.App.ViewModels;

// Panel "Sobre esta version" - identidad propia de esta app ("Terrakeep"), separada de
// Terrasavr/YellowAfterlife: esta es una reescritura nativa desde cero, con su propio motor
// de formato (ver TerrasavrNative.Core), no una copia ni un fork del editor original.
public sealed class AboutViewModel
{
    public string AppName => "Terrakeep";
    public string Tagline => "Editor de personajes de Terraria, nativo y sin Electron - vanilla y Calamity Mod.";
    public string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";

    // C-18 (informe de pulido final, cierra N2): bloque PROPIO, no una frase enterrada en
    // CreditsText - pedido explicito del usuario, credito personal como autor. El nombre va
    // LITERAL: "IncrediBad", con I y B mayusculas y el resto en minusculas - no se "corrige" a
    // Incredibad ni a INCREDIBAD en ningun sitio, ni aqui, ni en el XAML, ni en el .csproj
    // (<Authors>), ni en ningun comentario.
    public string AuthorName => "IncrediBad";
    public string AuthorText => "Terrakeep está diseñado y desarrollado por IncrediBad.";

    public string CreditsText =>
        "Terrakeep es una reescritura nativa (C#/.NET, WPF) de un editor de personajes de " +
        "Terraria - sin Chromium ni Electron. El formato de archivo (.plr/.tplr, NBT, cifrado) " +
        "se investigo y verifico de forma independiente, directamente contra el juego real y " +
        "tModLoader, sin depender de codigo de terceros.\n\n" +
        "Inspirado en Terrasavr, de YellowAfterlife (yal.cc) - un editor excelente al que " +
        "este proyecto debe la idea original. Terrakeep no es una copia ni un fork de ese " +
        "codigo (el motor de YellowAfterlife esta compilado, nunca se tuvo acceso a su fuente): " +
        "es un programa distinto, escrito desde cero, con su propia base de codigo.\n\n" +
        "Terraria, tModLoader y Calamity Mod son propiedad de sus respectivos autores " +
        "(Re-Logic, el equipo de tModLoader y el equipo de CalamityMod). Terrakeep no esta " +
        "afiliado con ninguno de ellos.";
}
