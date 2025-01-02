using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommuniZEN.ViewModels
{
    public partial class ChatbotintroViewModel : ObservableObject
    {
        [RelayCommand]
        private async Task Continue()
        {
            await Shell.Current.GoToAsync("chatbotpage");
        }



    }
}
