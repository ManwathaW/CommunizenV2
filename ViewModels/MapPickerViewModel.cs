using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.ViewModels
{
    public partial class MapPickerViewModel : ObservableObject
    {
        private readonly IGeocoding _geocoding;

        [ObservableProperty]
        private string selectedAddress;

        [ObservableProperty]
        private Microsoft.Maui.Devices.Sensors.Location selectedLocation;

        public event Action<Microsoft.Maui.Devices.Sensors.Location, string> LocationSelected;

        public MapPickerViewModel()
        {
            _geocoding = Geocoding.Default;
        }

        public async Task UpdateSelectedLocationAsync(double latitude, double longitude)
        {
            try
            {
                SelectedLocation = new Microsoft.Maui.Devices.Sensors.Location(latitude, longitude);
                var placemarks = await _geocoding.GetPlacemarksAsync(latitude, longitude);
                var placemark = placemarks?.FirstOrDefault();
                if (placemark != null)
                {
                    SelectedAddress = $"{placemark.Thoroughfare} {placemark.SubThoroughfare}, {placemark.Locality}";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting address: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ConfirmLocation()
        {
            if (SelectedLocation != null)
            {
                var navigationParameter = new Dictionary<string, object>
            {
                { "Latitude", SelectedLocation.Latitude },
                { "Longitude", SelectedLocation.Longitude },
                { "Address", SelectedAddress }
            };
                await Shell.Current.GoToAsync("..", navigationParameter);
            }
        }
    }
}
