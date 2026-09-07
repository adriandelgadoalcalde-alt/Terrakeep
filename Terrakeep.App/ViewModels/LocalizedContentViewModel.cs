using System.ComponentModel;
using Terrakeep.App.Services;

namespace Terrakeep.App.ViewModels;

// Ronda de idioma del 6-sep-2026. Base comun de los ViewModels que muestran CONTENIDO bilingue
// (Novedades del juego/Calamity, registro de cambios del propio editor). No son texto de
// interfaz, asi que no pueden bindear `Loc[clave]` - su texto vive en los .json de datos, con un
// campo por idioma (ver Core/Data/LocalizedContent).
//
// Por que hace falta esto y no basta con leer el idioma una vez: `Loc[clave]` se refresca solo
// porque LocalizationService avisa con "Item[]" y WPF reevalua CUALQUIER binding indexado sobre
// ese objeto. Un binding a una propiedad normal de otro objeto (Text, Summary...) no se entera
// de nada - se quedaria con el idioma que hubiera al construirse, que es exactamente el limite
// que la ronda anterior dejo documentado y el usuario reporto como bug real.
//
// La suscripcion va por PropertyChangedEventManager (evento DEBIL) a proposito, no con `+=`:
// LocalizationService.Instance es un singleton que vive lo que la aplicacion, y `dotnet test`
// construye cientos de MainViewModel - con una suscripcion fuerte cada uno de ellos (y todo su
// arbol de entradas de Novedades) quedaria vivo para siempre colgando del singleton.
public abstract class LocalizedContentViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected LocalizedContentViewModel()
        => PropertyChangedEventManager.AddHandler(LocalizationService.Instance, OnIdiomaCambiado, "Item[]");

    // Idioma activo real, ya normalizado por LocalizationService ("es"/"en", nunca otra cosa).
    protected static string Idioma => LocalizationService.Instance.Language;

    private void OnIdiomaCambiado(object? sender, PropertyChangedEventArgs e) => RefrescarTextos();

    // Cada subclase avisa de SUS propiedades de texto. Un `null` como nombre de propiedad
    // (string.Empty en la convencion de WPF) valdria para "todas", pero se enumeran a mano para
    // que cualquiera que añada una propiedad nueva se vea obligado a decidir si es de idioma.
    protected abstract void RefrescarTextos();

    protected void Avisar(string propiedad)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
}
