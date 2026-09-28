using System.Windows;
using System.Windows.Controls;

namespace Terrakeep.App.Views;

// FASE D del responsive global (28-sep-2026): tarjetas de carpeta raiz compartidas por las 3 superficies
// de la familia "catalogo con categorias" - ver el comentario de TarjetasCategoriasRaiz.xaml.
public partial class TarjetasCategoriasRaiz : UserControl
{
    // true en la Libreria de buffs: el recuento se lee "N buff(s)" (CategoryNodeViewModel.BuffCountLabel).
    public static readonly DependencyProperty EsBuffsProperty = DependencyProperty.Register(
        nameof(EsBuffs), typeof(bool), typeof(TarjetasCategoriasRaiz), new PropertyMetadata(false));

    public bool EsBuffs { get => (bool)GetValue(EsBuffsProperty); set => SetValue(EsBuffsProperty, value); }

    public TarjetasCategoriasRaiz()
    {
        InitializeComponent();
    }
}
