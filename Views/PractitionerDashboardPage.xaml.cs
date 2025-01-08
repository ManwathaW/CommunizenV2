using CommuniZEN.ViewModels;

namespace CommuniZEN.Views;

public partial class PractitionerDashboardPage : ContentPage
{

    private readonly PractitionerDashboardViewModel _viewModel;
    public PractitionerDashboardPage(PractitionerDashboardViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }
}