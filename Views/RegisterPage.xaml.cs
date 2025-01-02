using Syncfusion.Maui.Toolkit.Carousel;
using CommuniZEN.ViewModels;
using CommuniZEN.Services;
using CommuniZEN.ViewModels;
using CommuniZEN.Interfaces;
using CommuniZEN.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CommuniZEN.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
		InitializeComponent();
        BindingContext = new RegisterViewModel(
             App.ServiceProvider.GetService<IFirebaseAuthService>(),
             App.ServiceProvider.GetService<IFirebaseDataService>()
         );

    }


    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Add this debug message
        if (BindingContext is RegisterViewModel vm)
        {
            vm.ErrorMessage = "Page loaded successfully";
        }
    }

}