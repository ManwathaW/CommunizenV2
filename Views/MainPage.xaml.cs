using CommuniZEN.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
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

            // Setup indicator
            horizontalScroll.Scrolled += OnScrollViewScrolled;
            indicatorView.Count = 4; // Number of cards
        }

        private void OnScrollViewScrolled(object sender, ScrolledEventArgs e)
        {
            var scrollView = (ScrollView)sender;
            var scrollingSpace = scrollView.ContentSize.Width - scrollView.Width;
            if (scrollingSpace <= 0) return;

            var scrollPercentage = e.ScrollX / scrollingSpace;
            var position = (int)Math.Round(scrollPercentage * (indicatorView.Count - 1));
            indicatorView.Position = Math.Max(0, Math.Min(position, indicatorView.Count - 1));
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            indicatorView.Count = 4; // Update if number of cards changes
        }

    }
}