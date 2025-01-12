using CommuniZEN.ViewModels;

namespace CommuniZEN.Views;

public partial class PractitionerProfilePage : ContentPage
{
    private readonly PractitionerProfileViewModel _viewModel;

    public PractitionerProfilePage(PractitionerProfileViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }
}