using CommuniZEN.ViewModels;
namespace CommuniZEN.Views;

public partial class BookingsPage : ContentPage
{

    private readonly BookingsViewModel _viewModel;
    public BookingsPage(BookingsViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }
}