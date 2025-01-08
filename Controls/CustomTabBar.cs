using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Diagnostics;

namespace CommuniZEN.Controls
{
    public class CustomTabBar : ContentView
    {
        private readonly HorizontalStackLayout _tabBarLayout;
        private readonly BoxView _selectedIndicator;
        private readonly double _itemWidth = 100;

        public CustomTabBar()
        {
            Debug.WriteLine("CustomTabBar constructor called");
            BackgroundColor = Colors.White;
            HeightRequest = 60;

            _tabBarLayout = new HorizontalStackLayout
            {
                Spacing = 15,
                HorizontalOptions = LayoutOptions.Center,
                Padding = new Thickness(10, 0)
            };

            _selectedIndicator = new BoxView
            {
                HeightRequest = 3,
                BackgroundColor = Colors.Blue,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.End,
                WidthRequest = _itemWidth,
                TranslationY = -5
            };

            var grid = new Grid
            {
                BackgroundColor = Colors.White,
                HeightRequest = 60
            };
            grid.Add(_tabBarLayout);
            grid.Add(_selectedIndicator);

            Content = grid;
        }

        public void AddTab(string iconName, string label, Command command)
        {
            Debug.WriteLine($"Adding tab: {label} with icon: {iconName}");

            var tab = new VerticalStackLayout
            {
                Spacing = 5,
                Children =
            {
                new Image
                {
                    Source = iconName,
                    HeightRequest = 24,
                    WidthRequest = 24,
                    HorizontalOptions = LayoutOptions.Center
                },
                new Label
                {
                    Text = label,
                    FontSize = 12,
                    TextColor = Colors.Black,
                    HorizontalOptions = LayoutOptions.Center
                }
            }
            };

            var tapGesture = new TapGestureRecognizer { Command = command };
            tab.GestureRecognizers.Add(tapGesture);
            _tabBarLayout.Children.Add(tab);
        }

        public async Task AnimateTabSelection(int index)
        {
            if (_tabBarLayout.Children.Count <= index) return;

            var selectedTab = _tabBarLayout.Children[index];
            double xOffset = 0;

            // Calculate x offset based on previous tabs
            for (int i = 0; i < index; i++)
            {
                xOffset += _itemWidth + _tabBarLayout.Spacing;
            }

            // Animate the indicator
            await _selectedIndicator.TranslateTo(xOffset, _selectedIndicator.TranslationY, 250, Easing.CubicInOut);

            // Update visuals of all tabs
            for (int i = 0; i < _tabBarLayout.Children.Count; i++)
            {
                if (_tabBarLayout.Children[i] is VerticalStackLayout tabLayout)
                {
                    var label = tabLayout.Children.OfType<Label>().FirstOrDefault();
                    if (label != null)
                    {
                        label.TextColor = i == index ? Colors.Blue : Colors.Black;
                    }
                }
            }
        }
    }
}