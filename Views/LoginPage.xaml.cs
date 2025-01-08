namespace CommuniZEN.Views;
using CommuniZEN.ViewModels;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
        BindingContext = App.ServiceProvider.GetService<LoginViewModel>();
    }


}