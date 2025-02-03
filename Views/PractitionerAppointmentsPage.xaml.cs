using CommuniZEN.ViewModels;
using Syncfusion.Maui.Toolkit.Carousel;
namespace CommuniZEN.Views;

public partial class PractitionerAppointmentsPage : ContentPage
{
    private readonly PractitionerAppointmentsViewModel _viewModel;
    public PractitionerAppointmentsPage(PractitionerAppointmentsViewModel viewModel)

    {
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }
}