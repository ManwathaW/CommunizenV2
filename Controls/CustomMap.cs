using Microsoft.Maui.Controls.Maps;
using System.Collections.ObjectModel;
using Map = Microsoft.Maui.Controls.Maps.Map;
using Location = Microsoft.Maui.Devices.Sensors.Location;
using IMap = Microsoft.Maui.Maps.IMap;
using Microsoft.Maui.Maps;

namespace CommuniZEN.Controls
{
    public class CustomMap : ContentView
    {
        private readonly Map _map;

        public event EventHandler<MapClickedEventArgs> MapClicked;

        public static readonly BindableProperty CustomPinsProperty = BindableProperty.Create(
            propertyName: nameof(CustomPins),
            returnType: typeof(ObservableCollection<CustomPin>),
            declaringType: typeof(CustomMap),
            defaultValue: null,
            propertyChanged: OnCustomPinsPropertyChanged);

        public static readonly BindableProperty IsShowingUserProperty = BindableProperty.Create(
            propertyName: nameof(IsShowingUser),
            returnType: typeof(bool),
            declaringType: typeof(CustomMap),
            defaultValue: false,
            propertyChanged: OnIsShowingUserPropertyChanged);

        public static readonly BindableProperty MapTypeProperty = BindableProperty.Create(
            propertyName: nameof(MapType),
            returnType: typeof(MapType),
            declaringType: typeof(CustomMap),
            defaultValue: MapType.Street,
            propertyChanged: OnMapTypePropertyChanged);

        public ObservableCollection<CustomPin> CustomPins
        {
            get => (ObservableCollection<CustomPin>)GetValue(CustomPinsProperty);
            set => SetValue(CustomPinsProperty, value);
        }

        public bool IsShowingUser
        {
            get => (bool)GetValue(IsShowingUserProperty);
            set => SetValue(IsShowingUserProperty, value);
        }

        public MapType MapType
        {
            get => (MapType)GetValue(MapTypeProperty);
            set => SetValue(MapTypeProperty, value);
        }

        public CustomMap()
        {
            _map = new Map();
            _map.MapClicked += Map_MapClicked;
            Content = _map;
        }

        private static void OnIsShowingUserPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CustomMap customMap)
            {
                customMap._map.IsShowingUser = (bool)newValue;
            }
        }

        private static void OnMapTypePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CustomMap customMap)
            {
                customMap._map.MapType = (MapType)newValue;
            }
        }

        private void Map_MapClicked(object sender, MapClickedEventArgs e)
        {
            MapClicked?.Invoke(this, e);

            foreach (var pin in _map.Pins)
            {
                var distance = Location.CalculateDistance(
                    e.Location,
                    pin.Location,
                    DistanceUnits.Kilometers);

                if (distance < 0.1) // Within 100 meters
                {
                    HandlePinClick(pin);
                }
            }
        }

        private void HandlePinClick(Pin pin)
        {
            var customPin = CustomPins?.FirstOrDefault(p =>
                p.Name == pin.Label &&
                p.Location.Latitude == pin.Location.Latitude &&
                p.Location.Longitude == pin.Location.Longitude);

            customPin?.RaiseClicked();
        }

        private static void OnCustomPinsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CustomMap customMap)
            {
                customMap._map.Pins.Clear();

                if (newValue is ObservableCollection<CustomPin> pins)
                {
                    foreach (var customPin in pins)
                    {
                        var pin = new Pin
                        {
                            Label = customPin.Name,
                            Address = customPin.Specialization,
                            Location = customPin.Location,
                            Type = PinType.Place
                        };
                        customMap._map.Pins.Add(pin);
                    }
                }
            }
        }

        public void MoveToRegion(MapSpan region)
        {
            _map.MoveToRegion(region);
        }
    }

    public class CustomPin
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Specialization { get; set; }
        public string Address { get; set; }
        public Location Location { get; set; }
        public string ImageUrl { get; set; }

        public event EventHandler Clicked;

        internal void RaiseClicked()
        {
            Clicked?.Invoke(this, EventArgs.Empty);
        }
    }
}