using System.Windows;
using System.Windows.Controls;

namespace Terrakeep.App.Views;

// FASE D del responsive global (28-sep-2026): navegacion de categorias compartida por la Libreria de
// objetos, la Libreria de buffs e Investigacion - ver el comentario de NavegadorCategorias.xaml. Toda la
// logica de la ruta vive en CatalogBrowserViewModel (RutaCategoria/MigasCategoria/NodoSubcategorias);
// aqui solo se abre y se cierra el desplegable de subcategorias.
public partial class NavegadorCategorias : UserControl
{
    public NavegadorCategorias()
    {
        InitializeComponent();
    }

    private void OnAbrirSubcategorias(object sender, RoutedEventArgs e) => PopupSubcategorias.IsOpen = !PopupSubcategorias.IsOpen;

    // El comando de la pastilla (SelectCommand del propio nodo) ya elige la carpeta; el desplegable se
    // cierra para que el usuario vea el resultado. Si la carpeta elegida tiene a su vez hijos, el boton
    // del desplegable pasa a ofrecerlos (NodoSubcategorias) sin mas clics que ese.
    private void OnSubcategoriaElegida(object sender, RoutedEventArgs e) => PopupSubcategorias.IsOpen = false;
}
