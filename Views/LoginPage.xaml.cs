namespace CommuniZEN.Views;
using CommuniZEN.ViewModels;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
        BindingContext = App.ServiceProvider.GetService<LoginViewModel>();
    }

    private void OnTogglePasswordButtonClicked(object sender, EventArgs e)
    {
        // Ensure PasswordEntry is properly referenced
        if (PasswordEntry == null)
        {
            throw new InvalidOperationException("PasswordEntry is not initialized.");
        }

        // Toggle the IsPassword property
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;

        // Update the eye icon based on the state
        if (PasswordEntry.IsPassword)
        {
            ((ImageButton)sender).Source = "closeeye.png"; // Hidden password
        }
        else
        {
            ((ImageButton)sender).Source = "eye.png"; // Visible password
        }
    }


}