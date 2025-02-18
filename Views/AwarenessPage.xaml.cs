using SkiaSharp.Extended.UI.Controls;

namespace CommuniZEN.Views;

public partial class AwarenessPage : ContentPage
{
	public AwarenessPage()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Add entry animations for frames
        var frames = this.GetVisualTreeDescendants().OfType<Frame>().Take(4);
        int delay = 0;

        foreach (var frame in frames)
        {
            frame.Opacity = 0;
            frame.Scale = 0.8;

            // Run animations asynchronously
            await Task.WhenAll(
                frame.FadeTo(1, 500, Easing.CubicOut),
                frame.ScaleTo(1, 500, Easing.CubicOut)
            );

            await Task.Delay(100); // Add delay between each frame animation
        }
    }
}

