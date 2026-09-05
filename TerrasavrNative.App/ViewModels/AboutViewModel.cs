using System.Reflection;
using TerrasavrNative.App.Services;

namespace TerrasavrNative.App.ViewModels;

// Panel "Sobre esta version" - identidad propia de esta app ("Terrakeep"), separada de
// Terrasavr/YellowAfterlife: esta es una reescritura nativa desde cero, con su propio motor
// de formato (ver TerrasavrNative.Core), no una copia ni un fork del editor original.
public sealed class AboutViewModel
{
    public string AppName => "Terrakeep";
    public string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";

    // Auditoria final de Opus (5-sep-2026): estas tres cadenas eran las ultimas de la cara
    // publica de la app que seguian en español duro - con el idioma en ingles, la pestaña
    // "Acerca de" (y el encabezado de Inicio, que reusa Tagline) se veian enteras en español.
    // Detectado por el barrido nuevo A10-IDIOMA-BARRIDO del arnes, no por build/test. El XAML
    // enlaza ahora directamente a Loc[clave] (unica via real de refresco EN VIVO: AboutViewModel
    // no es observable, un binding a About.Tagline nunca se volveria a preguntar al cambiar el
    // idioma) - estas propiedades se quedan como la fuente unica del texto para cualquier otro
    // consumidor.
    public string Tagline => LocalizationService.Instance["about_tagline"];

    // C-18 (informe de pulido final, cierra N2): bloque PROPIO, no una frase enterrada en
    // CreditsText - pedido explicito del usuario, credito personal como autor. El nombre va
    // LITERAL: "IncrediBad", con I y B mayusculas y el resto en minusculas - no se "corrige" a
    // Incredibad ni a INCREDIBAD en ningun sitio, ni aqui, ni en el XAML, ni en el .csproj
    // (<Authors>), ni en ningun comentario.
    public string AuthorName => "IncrediBad";
    public string AuthorText => LocalizationService.Instance["about_author_text"];

    public string CreditsText => LocalizationService.Instance["about_credits_text"];
}
