using System.Windows;
using System.Windows.Controls.Primitives;

namespace Terrakeep.App.Controls;

// FASE D del responsive global, correccion D-01/D-03 del revisor visual (28-sep-2026): boton que abre y
// cierra un Popup "ligero" (StaysOpen=False) - el desplegable de subcategorias de NavegadorCategorias y el
// desplegable "Prefijo" del panel Editar en Compacto. Uso: controls:Desplegable.Popup="{Binding
// ElementName=MiPopup}" en el boton.
// Problema real que resuelve: con StaysOpen=False, pulsar el MISMO boton con el popup abierto hace que el
// popup se cierre en el MouseDown (clic "fuera") y el Click posterior lo vuelva a abrir - el boton no
// servia para cerrar. Se recuerda cuando se cerro y un clic que llega en ese mismo gesto no lo reabre.
public static class Desplegable
{
    public static readonly DependencyProperty PopupProperty = DependencyProperty.RegisterAttached(
        "Popup", typeof(Popup), typeof(Desplegable), new PropertyMetadata(null, OnPopupCambiado));

    public static Popup? GetPopup(DependencyObject d) => (Popup?)d.GetValue(PopupProperty);
    public static void SetPopup(DependencyObject d, Popup? v) => d.SetValue(PopupProperty, v);

    private static readonly DependencyProperty CerradoEnProperty = DependencyProperty.RegisterAttached(
        "CerradoEn", typeof(long), typeof(Desplegable), new PropertyMetadata(0L));

    private static void OnPopupCambiado(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ButtonBase boton) return;
        boton.Click -= OnClick;
        boton.IsVisibleChanged -= OnBotonVisibilidad;
        if (e.OldValue is Popup viejo) viejo.Closed -= OnCerrado;
        if (e.NewValue is Popup nuevo)
        {
            boton.Click += OnClick;
            boton.IsVisibleChanged += OnBotonVisibilidad;
            nuevo.Closed -= OnCerrado;
            nuevo.Closed += OnCerrado;
        }
    }

    // Solo cuenta un cierre provocado por un botón del raton pulsado (el clic "fuera" de StaysOpen=False):
    // un cierre por codigo (elegir una subcategoria, cambiar de tamaño...) no debe bloquear el siguiente clic.
    // Revisor visual r2, M-01: con el desplegable abierto, si su boton deja de verse (p.ej. la ventana cruza a
    // Amplio y Editar pasa a barra lateral con el selector en linea, o se pliega la Libreria), el Popup se quedaba
    // abierto flotando sobre otra cosa. Un desplegable sin su boton visible se cierra.
    private static void OnBotonVisibilidad(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is false && sender is DependencyObject d && GetPopup(d) is { IsOpen: true } popup) popup.IsOpen = false;
    }

    private static void OnCerrado(object? sender, EventArgs e)
    {
        if (sender is not Popup p) return;
        bool porRaton = System.Windows.Input.Mouse.LeftButton == System.Windows.Input.MouseButtonState.Pressed
                        || System.Windows.Input.Mouse.RightButton == System.Windows.Input.MouseButtonState.Pressed;
        p.SetValue(CerradoEnProperty, porRaton ? Environment.TickCount64 : 0L);
    }

    private static void OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not DependencyObject d || GetPopup(d) is not { } popup) return;
        if (popup.IsOpen) { popup.IsOpen = false; return; }
        long cerradoEn = (long)popup.GetValue(CerradoEnProperty);
        if (Environment.TickCount64 - cerradoEn < 250) return; // este mismo clic acaba de cerrarlo
        popup.IsOpen = true;
    }
}
