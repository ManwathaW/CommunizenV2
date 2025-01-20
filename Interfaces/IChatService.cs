using System;
using CommuniZEN.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Interfaces
{
    public interface IChatService
    {
        // Session Management
        Task<ChatSession> CreateChatSessionAsync(string practitionerId, string clientId, string practitionerName, string clientName);
        Task<List<ChatSession>> GetChatSessionsAsync();
        Task<List<ChatSession>> GetActiveChatSessionsAsync();
        Task ArchiveChatSessionAsync(string sessionId);
        Task<ChatSession> GetChatSessionAsync(string sessionId);
        Task<bool> DoesChatSessionExistAsync(string practitionerId, string clientId);

        // Message Management
        Task<List<ChatMessage>> GetChatMessagesAsync(string sessionId);
        Task<ChatMessage> SendMessageAsync(string sessionId, string message, string content, string imageUrl = null);
        Task<List<ChatMessage>> GetMessagesByTimeRangeAsync(string sessionId, DateTime startTime, DateTime endTime);
        Task DeleteMessageAsync(string sessionId, string messageId);
        Task<ChatMessage> EditMessageAsync(string sessionId, string messageId, string newContent);

        // Read Status
        Task MarkMessagesAsReadAsync(string sessionId);
        Task<int> GetUnreadMessageCountAsync(string sessionId);
        Task<Dictionary<string, int>> GetUnreadCountsForAllSessionsAsync();

        // Real-time Subscriptions
        IDisposable SubscribeToMessages(string sessionId, Action<ChatMessage> onMessageReceived);
        IDisposable SubscribeToTypingStatus(string sessionId, Action<TypingStatus> onTypingStatusChanged);
        IDisposable SubscribeToSessionUpdates(Action<ChatSession> onSessionUpdated);

        // Typing Status
        Task<bool> IsUserTypingAsync(string sessionId, string userId);
        Task SetTypingStatusAsync(string sessionId, bool isTyping);
        Task<List<string>> GetCurrentlyTypingUsersAsync(string sessionId);

        // Session Status
        Task<bool> IsUserOnlineAsync(string userId);
        Task SetUserOnlineStatusAsync(bool isOnline);
        Task<DateTime?> GetLastSeenAsync(string userId);




        Task<ChatMessage> SendMessageAsync(string sessionId, string content);


    }

}



