using CommuniZEN.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
namespace CommuniZEN.Views;
using CommuniZEN.Models;

public partial class BookingsPage : ContentPage
{
    private readonly BookingsViewModel _viewModel;

    public BookingsPage(BookingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.MapSpan != null)
        {
            mapControl.MoveToRegion(_viewModel.MapSpan);
        }
    }

    private void OnPinClicked(object sender, Microsoft.Maui.Controls.Maps.PinClickedEventArgs e)
    {
        var pin = sender as Microsoft.Maui.Controls.Maps.Pin;
        if (pin?.BindingContext is PracticeProfile practitioner)
        {
            _viewModel.SelectPractitionerCommand.Execute(practitioner);
        }
        e.HideInfoWindow = true;
    }
}