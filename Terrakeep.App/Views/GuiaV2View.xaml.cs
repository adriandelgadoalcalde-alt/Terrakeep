using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Terrakeep.App.ViewModels;

namespace Terrakeep.App.Views;

// Guia v2 (F2, 02-oct-2026): solo comportamiento de vista (sin logica de guia) - llevar el scroll de
// pagina arriba al cambiar de seccion/parada, desplazar hasta la zona o ficha pedida desde un
// enlace, y cerrar la ficha de objeto con Escape o pulsando fuera de ella.
public partial class GuiaV2View : UserControl
{
    private GuiaV2ViewModel? _vm;

    public GuiaV2View()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Enganchar();
        Loaded += (_, _) => Enganchar();
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && _vm?.HayFicha == true) { _vm.CerrarFicha(); e.Handled = true; }
        };
        GuiaFichaObjeto.MouseLeftButtonDown += (_, e) =>
        {
            // Clic en el velo (fuera de la tarjeta) = cerrar, como un dialogo modal.
            if (e.OriginalSource == GuiaFichaObjeto) _vm?.CerrarFicha();
        };
    }

    private void Enganchar()
    {
        var nuevo = (DataContext as MainViewModel)?.GuiaV2;
        if (ReferenceEquals(nuevo, _vm)) return;
        if (_vm != null)
        {
            _vm.SeccionCambiada -= AlCambiarSeccion;
            _vm.ZonaEnfocada -= AlEnfocarZona;
            _vm.FichaEnfocada -= AlEnfocarFicha;
        }
        _vm = nuevo;
        if (_vm != null)
        {
            _vm.SeccionCambiada += AlCambiarSeccion;
            _vm.ZonaEnfocada += AlEnfocarZona;
            _vm.FichaEnfocada += AlEnfocarFicha;
        }
    }

    private void AlCambiarSeccion() => Dispatcher.BeginInvoke(() => GuideContentScroll.ScrollToTop(), System.Windows.Threading.DispatcherPriority.Loaded);

    private void AlEnfocarZona(string id) => EnfocarEn(GuiaZonas, o => o is ZonaV2ViewModel z && z.Id == id);

    private void AlEnfocarFicha(string id) =>
        EnfocarEn(_vm?.IsRaro == true ? GuiaHallazgos : GuiaProblemas, o => o is FichaV2ViewModel f && f.Id == id);

    private void EnfocarEn(ItemsControl lista, Func<object, bool> es)
    {
        Dispatcher.BeginInvoke(() =>
        {
            foreach (var item in lista.Items)
            {
                if (!es(item)) continue;
                if (lista.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement contenedor)
                {
                    var origen = contenedor.TransformToAncestor(GuideContentScroll).Transform(new Point(0, 0));
                    GuideContentScroll.ScrollToVerticalOffset(GuideContentScroll.VerticalOffset + origen.Y - 12);
                }
                break;
            }
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }
}
