using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Models;
using CommuniZEN.Interfaces;


namespace CommuniZEN.ViewModels
{
    public partial class ChatbotViewModel : ObservableObject
    {
        private readonly IChatbotService _chatbotService;

        [ObservableProperty]
        private string _userMessage;

        [ObservableProperty]
        private ObservableCollection<ChatMessage> _chatMessages = new();

        [ObservableProperty]
        private bool _isBusy;

        public ChatbotViewModel(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
            _chatMessages = new ObservableCollection<ChatMessage>();
        }

        [RelayCommand]
        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(UserMessage))
                return;

            IsBusy = true;
            var userMessageText = UserMessage;

            ChatMessages.Add(new ChatMessage
            {
                IsUser = true,
                Content = userMessageText,
                Timestamp = DateTime.Now
            });

            UserMessage = string.Empty;

            try
            {
                var response = await _chatbotService.GetResponseAsync(userMessageText);
                ChatMessages.Add(new ChatMessage
                {
                    IsUser = false,
                    Content = response,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                ChatMessages.Add(new ChatMessage
                {
                    IsUser = false,
                    Content = $"Error: {ex.Message}",
                    Timestamp = DateTime.Now
                });
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public class ChatMessage
    {
        public bool IsUser { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
