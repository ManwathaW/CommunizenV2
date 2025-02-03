using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using Firebase.Database;
using Firebase.Database.Query;
using System.Diagnostics;
using System.Reactive.Linq;

namespace CommuniZEN.Services
{

    public class FirebaseChatService : IChatService
    {
        private readonly FirebaseClient _firebaseClient;
        private readonly string _currentUserId;
        private readonly string _currentUserRole;
        private readonly string _currentUserName;

        public FirebaseChatService(
            FirebaseClient firebaseClient,
            string currentUserId,
            string currentUserRole,
            string currentUserName)
        {
            Debug.WriteLine($"Initializing FirebaseChatService for user: {currentUserId}");

            // Validate input parameters
            if (firebaseClient == null) throw new ArgumentNullException(nameof(firebaseClient));
          
            if (string.IsNullOrEmpty(currentUserId)) throw new ArgumentException("User ID cannot be null or empty", nameof(currentUserId));
            if (string.IsNullOrEmpty(currentUserRole)) throw new ArgumentException("User role cannot be null or empty", nameof(currentUserRole));
            if (string.IsNullOrEmpty(currentUserName)) throw new ArgumentException("User name cannot be null or empty", nameof(currentUserName));

            _firebaseClient = firebaseClient;
            _currentUserId = currentUserId;
            _currentUserRole = currentUserRole;
            _currentUserName = currentUserName;

            Debug.WriteLine("FirebaseChatService initialized successfully");

            // Initialize database structure asynchronously
            Task.Run(async () =>
            {
                try
                {
                    await InitializeDatabaseStructureAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error initializing database structure: {ex.Message}");
                }
            });
        }


        private async Task InitializeDatabaseStructureAsync()
        {
            try
            {
                Debug.WriteLine("Initializing database structure...");
                var structures = new[] { "chat_sessions", "chat_messages", "typing_status", "user_status" };
                foreach (var structure in structures)
                {
                    var exists = await _firebaseClient.Child(structure).OnceSingleAsync<object>();
                    if (exists == null)
                    {
                        await _firebaseClient.Child(structure).PutAsync("");
                        Debug.WriteLine($"Created {structure} structure");
                    }
                }
                Debug.WriteLine("Database structure initialization complete");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing database structure: {ex.Message}");
                throw; // Rethrow to handle in calling code
            }
        }

        // Session Management Methods
        public async Task<ChatSession> CreateChatSessionAsync(string practitionerId, string clientId, string practitionerName, string clientName)
        {
            try
            {
                // Check if session already exists
                if (await DoesChatSessionExistAsync(practitionerId, clientId))
                {
                    throw new InvalidOperationException("Chat session already exists");
                }

                var session = new ChatSession
                {
                    Id = Guid.NewGuid().ToString(),
                    PractitionerId = practitionerId,
                    PractitionerName = practitionerName,
                    ClientId = clientId,
                    ClientName = clientName,
                    CreatedAt = DateTime.UtcNow,
                    Timestamp = DateTime.UtcNow.Ticks,
                    IsActive = true,
                    UnreadCount = 0
                };

                await _firebaseClient
                    .Child("chat_sessions")
                    .Child(session.Id)
                    .PutAsync(session);

                return session;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating chat session: {ex.Message}");
            }
        }

        public async Task<List<ChatSession>> GetChatSessionsAsync()
        {
            try
            {
                var sessions = await _firebaseClient
                    .Child("chat_sessions")
                    .OnceAsync<ChatSession>();

                return sessions
                    .Select(item =>
                    {
                        var session = item.Object;
                        session.Id = item.Key; // Ensure ID is set from Firebase key
                        return session;
                    })
                    .Where(session =>
                        session.PractitionerId == _currentUserId ||
                        session.ClientId == _currentUserId)
                    .OrderByDescending(x => x.Timestamp)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting chat sessions: {ex.Message}");
                return new List<ChatSession>();
            }
        }

        public async Task<List<ChatSession>> GetActiveChatSessionsAsync()
        {
            try
            {
                var sessions = await GetChatSessionsAsync();
                return sessions.Where(s => s.IsActive).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting active chat sessions: {ex.Message}");
                return new List<ChatSession>();
            }
        }

        public async Task ArchiveChatSessionAsync(string sessionId)
        {
            try
            {
                await _firebaseClient
                    .Child("chat_sessions")
                    .Child(sessionId)
                    .Child("isActive")
                    .PutAsync(false);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error archiving chat session: {ex.Message}");
            }
        }

        public async Task<ChatSession> GetChatSessionAsync(string sessionId)
        {
            try
            {
                var session = await _firebaseClient
                    .Child("chat_sessions")
                    .Child(sessionId)
                    .OnceSingleAsync<ChatSession>();

                if (session != null)
                    session.Id = sessionId;

                return session;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting chat session: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DoesChatSessionExistAsync(string practitionerId, string clientId)
        {
            try
            {
                var sessions = await GetChatSessionsAsync();
                return sessions.Any(s =>
                    s.PractitionerId == practitionerId &&
                    s.ClientId == clientId &&
                    s.IsActive);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking chat session existence: {ex.Message}");
                return false;
            }
        }

        // Message Management Methods - No changes needed to these methods as they're working correctly
        public async Task<List<ChatMessage>> GetChatMessagesAsync(string sessionId)
        {
            try
            {
                var messages = await _firebaseClient
                    .Child("chat_messages")
                    .Child(sessionId)
                    .OnceAsync<ChatMessage>();

                return messages
                    .Select(item =>
                    {
                        var msg = item.Object;
                        msg.Id = item.Key;
                        return msg;
                    })
                    .OrderBy(x => x.Timestamp)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting chat messages: {ex.Message}");
                return new List<ChatMessage>();
            }
        }

        // ... Rest of your existing methods remain the same ...

        // Helper Methods
        private async Task IncrementUnreadCount(string sessionId)
        {
            try
            {
                var session = await GetChatSessionAsync(sessionId);
                if (session != null)
                {
                    var currentCount = session.UnreadCount;
                    await _firebaseClient
                        .Child("chat_sessions")
                        .Child(sessionId)
                        .Child("unreadCount")
                        .PutAsync(currentCount + 1);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error incrementing unread count: {ex.Message}");
            }
        }


        public async Task<ChatMessage> SendMessageAsync(string sessionId, string content)
        {
            try
            {
                var chatMessage = new ChatMessage
                {
                    SenderId = _currentUserId,
                    SenderName = _currentUserName,
                    Content = content,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    IsRead = false
                };

                await _firebaseClient
                    .Child("chat_messages")
                    .Child(sessionId)
                    .Child(chatMessage.Id)
                    .PutAsync(chatMessage);

                await _firebaseClient
                    .Child("chat_sessions")
                    .Child(sessionId)
                    .Child("lastMessage")
                    .PutAsync(content);

                await _firebaseClient
                    .Child("chat_sessions")
                    .Child(sessionId)
                    .Child("lastMessageTimestamp")
                    .PutAsync(chatMessage.Timestamp);

                await IncrementUnreadCount(sessionId);

                return chatMessage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending message: {ex.Message}");
                throw;
            }
        }







        public async Task<List<ChatMessage>> GetMessagesByTimeRangeAsync(string sessionId, DateTime startTime, DateTime endTime)
        {
            try
            {
                var messages = await GetChatMessagesAsync(sessionId);
                return messages
                    .Where(m => m.CreatedAt >= startTime && m.CreatedAt <= endTime)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting messages by time range: {ex.Message}");
                return new List<ChatMessage>();
            }
        }

        public async Task DeleteMessageAsync(string sessionId, string messageId)
        {
            try
            {
                await _firebaseClient
                    .Child("chat_messages")
                    .Child(sessionId)
                    .Child(messageId)
                    .DeleteAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting message: {ex.Message}");
            }
        }

        public async Task<ChatMessage> EditMessageAsync(string sessionId, string messageId, string newContent)
        {
            try
            {
                var message = await _firebaseClient
                    .Child("chat_messages")
                    .Child(sessionId)
                    .Child(messageId)
                    .OnceSingleAsync<ChatMessage>();

                if (message.SenderId != _currentUserId)
                {
                    throw new UnauthorizedAccessException("Cannot edit another user's message");
                }

                message.Content = newContent;
                message.IsEdited = true;
                message.EditedAt = DateTime.UtcNow;

                await _firebaseClient
                    .Child("chat_messages")
                    .Child(sessionId)
                    .Child(messageId)
                    .PutAsync(message);

                return message;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error editing message: {ex.Message}");
            }
        }

        public async Task MarkMessagesAsReadAsync(string sessionId)
        {
            try
            {
                var messages = await _firebaseClient
                    .Child("chat_messages")
                    .Child(sessionId)
                    .OnceAsync<ChatMessage>();

                foreach (var message in messages)
                {
                    if (!message.Object.IsRead && message.Object.SenderId != _currentUserId)
                    {
                        await _firebaseClient
                            .Child("chat_messages")
                            .Child(sessionId)
                            .Child(message.Key)
                            .Child("isRead")
                            .PutAsync(true);
                    }
                }

                await _firebaseClient
                    .Child("chat_sessions")
                    .Child(sessionId)
                    .Child("unreadCount")
                    .PutAsync(0);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error marking messages as read: {ex.Message}");
            }
        }

        public async Task<int> GetUnreadMessageCountAsync(string sessionId)
        {
            try
            {
                var messages = await GetChatMessagesAsync(sessionId);
                return messages.Count(m => !m.IsRead && m.SenderId != _currentUserId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting unread message count: {ex.Message}");
                return 0;
            }
        }

        public async Task<Dictionary<string, int>> GetUnreadCountsForAllSessionsAsync()
        {
            try
            {
                var sessions = await GetChatSessionsAsync();
                var unreadCounts = new Dictionary<string, int>();

                foreach (var session in sessions)
                {
                    unreadCounts[session.Id] = await GetUnreadMessageCountAsync(session.Id);
                }

                return unreadCounts;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting unread counts: {ex.Message}");
                return new Dictionary<string, int>();
            }
        }

        public IDisposable SubscribeToMessages(string sessionId, Action<ChatMessage> onMessageReceived)
        {
            return _firebaseClient
                .Child("chat_messages")
                .Child(sessionId)
                .AsObservable<ChatMessage>()
                .Where(m => m.Object != null)
                .Subscribe(message => onMessageReceived(message.Object));
        }

        public IDisposable SubscribeToTypingStatus(string sessionId, Action<TypingStatus> onTypingStatusChanged)
        {
            return _firebaseClient
                .Child("typing_status")
                .Child(sessionId)
                .AsObservable<TypingStatus>()
                .Where(t => t.Object != null)
                .Subscribe(status => onTypingStatusChanged(status.Object));
        }

        public IDisposable SubscribeToSessionUpdates(Action<ChatSession> onSessionUpdated)
        {
            return _firebaseClient
                .Child("chat_sessions")
                .AsObservable<ChatSession>()
                .Where(s => s.Object != null)
                .Subscribe(session => onSessionUpdated(session.Object));
        }

        public async Task<bool> IsUserTypingAsync(string sessionId, string userId)
        {
            try
            {
                var status = await _firebaseClient
                    .Child("typing_status")
                    .Child(sessionId)
                    .Child(userId)
                    .OnceSingleAsync<TypingStatus>();

                return status?.IsTyping ?? false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking typing status: {ex.Message}");
                return false;
            }
        }

        public async Task SetTypingStatusAsync(string sessionId, bool isTyping)
        {
            try
            {
                var status = new TypingStatus
                {
                    UserId = _currentUserId,
                    UserName = _currentUserName,
                    IsTyping = isTyping,
                    Timestamp = DateTime.UtcNow
                };

                await _firebaseClient
                    .Child("typing_status")
                    .Child(sessionId)
                    .Child(_currentUserId)
                    .PutAsync(status);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting typing status: {ex.Message}");
            }
        }

        public async Task<List<string>> GetCurrentlyTypingUsersAsync(string sessionId)
        {
            try
            {
                var typingUsers = await _firebaseClient
                    .Child("typing_status")
                    .Child(sessionId)
                    .OnceAsync<TypingStatus>();

                return typingUsers
                    .Where(t => t.Object.IsTyping && t.Object.UserId != _currentUserId)
                    .Select(t => t.Object.UserName)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting typing users: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<bool> IsUserOnlineAsync(string userId)
        {
            try
            {
                var status = await _firebaseClient
                    .Child("user_status")
                    .Child(userId)
                    .OnceSingleAsync<UserStatus>();

                return status?.IsOnline ?? false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking user online status: {ex.Message}");
                return false;
            }
        }

        public async Task SetUserOnlineStatusAsync(bool isOnline)
        {
            try
            {
                var status = new UserStatus
                {
                    IsOnline = isOnline,
                    LastSeen = DateTime.UtcNow
                };

                await _firebaseClient
                    .Child("user_status")
                    .Child(_currentUserId)
                    .PutAsync(status);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting user online status: {ex.Message}");
            }
        }

        public async Task<DateTime?> GetLastSeenAsync(string userId)
        {
            try
            {
                var status = await _firebaseClient
                    .Child("user_status")
                    .Child(userId)
                    .OnceSingleAsync<UserStatus>();

                return status?.LastSeen;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting last seen: {ex.Message}");
                return null;
            }
        }

        public Task<ChatMessage> SendMessageAsync(string sessionId, string message, string content, string imageUrl = null)
        {
            throw new NotImplementedException();
        }
    }
}
