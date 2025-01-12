using CommuniZEN.ViewModels;
using Syncfusion.Maui.Toolkit.Carousel;
namespace CommuniZEN.Views;

public partial class PractitionerAppointmentsPage : ContentPage
{
    private readonly PractitionerDashboardViewModel _viewModel;
    public PractitionerAppointmentsPage(PractitionerDashboardViewModel viewModel)

    {
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }
}