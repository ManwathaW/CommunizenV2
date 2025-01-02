using CommuniZEN.ViewModels;
using Microsoft.Maui.Controls.Maps;

namespace CommuniZEN.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel _viewModel;

        public MainPage(MainPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

       


    }
}