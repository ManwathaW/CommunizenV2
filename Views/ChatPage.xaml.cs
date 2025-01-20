using Syncfusion.Maui.Toolkit.Carousel;
using CommuniZEN.ViewModels;

namespace CommuniZEN.Views;

public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel _viewModel;
    public ChatPage(ChatViewModel viewModel)
    {
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }
}