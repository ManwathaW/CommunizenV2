using Microsoft.Maui.Controls.Shapes;
using CommuniZEN.Controls;

namespace CommuniZEN.Controls
{
    public partial class AudioVisualizer : ContentView
    {
        public static readonly BindableProperty AudioLevelProperty =
            BindableProperty.Create(nameof(AudioLevel), typeof(float), typeof(AudioVisualizer), 0f,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    ((AudioVisualizer)bindable).UpdateBars();
                });

        public float AudioLevel
        {
            get => (float)GetValue(AudioLevelProperty);
            set => SetValue(AudioLevelProperty, value);
        }

        private readonly int numberOfBars = 30;
        private readonly List<Rectangle> bars = new();
        private readonly Random random = new();

        public AudioVisualizer()
        {
            InitializeComponent();
            InitializeBars();
        }

        private void InitializeBars()
        {
            // Clear existing bars
            MainGrid.Children.Clear();
            MainGrid.ColumnDefinitions.Clear();
            bars.Clear();

            // Create column definitions
            for (int i = 0; i < numberOfBars; i++)
            {
                MainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            }

            // Create bars
            for (int i = 0; i < numberOfBars; i++)
            {
                var bar = new Rectangle
                {
                    Fill = new SolidColorBrush(Color.FromArgb("#4B89DC")),
                    HeightRequest = 3,
                    VerticalOptions = LayoutOptions.End
                };

                bars.Add(bar);
                MainGrid.Add(bar, i);
            }
        }

        private void UpdateBars()
        {
            foreach (var bar in bars)
            {
                var height = 5 + (45 * AudioLevel * (0.5 + random.NextDouble()));
                bar.HeightRequest = height;
            }
        }
    }
}