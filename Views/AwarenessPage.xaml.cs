using SkiaSharp.Extended.UI.Controls;

namespace CommuniZEN.Views;

public partial class AwarenessPage : ContentPage
{
	public AwarenessPage()
	{
		InitializeComponent();
	}



    private async void OnMoreInformationClicked(object sender, EventArgs e)
    {
        await Browser.OpenAsync("https://www.ul.ac.za/student-counselling/");
    }

    private async void OnAnxietyClicked(object sender, EventArgs e)
    {
        await Browser.OpenAsync("https://youtu.be/_looGAGzJGY?si=D8OmUD-xwggAZaDP");

    }
    private async void OnDepressionClicked(object sender, EventArgs e)
    {
        await Browser.OpenAsync("https://youtu.be/ybnzd4zu8xs?si=X4VVXa64jKe5J4b6");
    }
    private async void OnStressClicked(object sender, EventArgs e)
    {
        await Browser.OpenAsync("https://youtu.be/7S_BB7R8NMU?si=EpZ6qxviZSkHx0Or");
    }
    private async void OnWellBeingClicked(object sender, EventArgs e)
    {
        await Browser.OpenAsync("https://youtu.be/CMspjZ26XhQ?si=zyehjsvbG1vCppxI");
    }
    private async void OnPhoneNumberTapped(object sender, EventArgs e)
    {
        string phoneNumber = (string)((TappedEventArgs)e).Parameter; if (PhoneDialer.Default.IsSupported)
        {
            PhoneDialer.Default.Open(phoneNumber);
        }
        else
        {
            await DisplayAlert("Error", "Phone dialing is not supported on this device.", "OK");
        }
    }
    private async void OnEmailTapped(object sender, EventArgs e)
    {
        string email = (string)((TappedEventArgs)e).Parameter;

        await Launcher.OpenAsync($"mailto:{email}");
    }
    private async void OnSmsTapped(object sender, EventArgs e)
    {
        string phoneNumber = (string)((TappedEventArgs)e).Parameter;
        await Launcher.OpenAsync($"sms:{phoneNumber}");
    }


    //frame animation

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

