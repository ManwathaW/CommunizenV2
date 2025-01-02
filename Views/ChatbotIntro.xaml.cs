using CommuniZEN.ViewModels;

namespace CommuniZEN.Views;

public partial class ChatbotIntro : ContentPage
{
    private readonly ChatbotintroViewModel _viewModel;

    public ChatbotIntro(ChatbotintroViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }
}