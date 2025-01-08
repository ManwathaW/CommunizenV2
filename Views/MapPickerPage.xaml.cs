using CommuniZEN.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Diagnostics;

namespace CommuniZEN.Views;

public partial class MapPickerPage : ContentPage
{
    private readonly MapPickerViewModel _viewModel;


    public MapPickerPage(MapPickerViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        locationMap.MapClicked += OnMapClicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync();
            if (location != null)
            {
                var mapSpan = MapSpan.FromCenterAndRadius(
                    new Location(location.Latitude, location.Longitude),
                    Distance.FromKilometers(1));
                locationMap.MoveToRegion(mapSpan); 
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting location: {ex.Message}");
        }
    }

    private async void OnMapClicked(object sender, MapClickedEventArgs e)
    {
        locationMap.Pins.Clear();

        var pin = new Pin
        {
            Label = "Selected Location",
            Type = PinType.Place,
            Location = new Location(e.Location.Latitude, e.Location.Longitude)
        };
        locationMap.Pins.Add(pin);

        await _viewModel.UpdateSelectedLocationAsync(e.Location.Latitude, e.Location.Longitude);
    }
}