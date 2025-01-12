using CommuniZEN.ViewModels;
using Syncfusion.Maui.Toolkit.Carousel;
namespace CommuniZEN.Views;

public partial class ClientAppointmentsPage : ContentPage
{

    private readonly ClientAppointmentsViewModel _viewModel;

    public ClientAppointmentsPage(ClientAppointmentsViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

 
}