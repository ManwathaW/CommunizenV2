using Syncfusion.Maui.Toolkit.Carousel;
using CommuniZEN.ViewModels;
using System.Diagnostics;

namespace CommuniZEN.Views;

public partial class ChatbotPage : ContentPage
{

    private readonly ChatbotViewModel _viewModel;
    public ChatbotPage(ChatbotViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        Debug.WriteLine("ChatbotPage initialized with ViewModel");
    }
}