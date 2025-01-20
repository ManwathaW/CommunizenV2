using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;


namespace CommuniZEN.ViewModels
{
    public partial class ChatViewModel : ObservableObject, IDisposable
    {
        private readonly IChatService _chatService;
        private readonly IFirebaseAuthService _authService;
        private readonly IFirebaseDataService _dataService;
        private IDispatcherTimer _typingTimer;
        private bool _isTyping;
        private bool _isInitialized;
        private string _currentUserId;

        [ObservableProperty]
        private ObservableCollection<Models.ChatMessage> messages;

        [ObservableProperty]
        private ObservableCollection<ChatSession> sessions;

        [ObservableProperty]
        private ObservableCollection<ChatSession> filteredSessions;

        [ObservableProperty]
        private ObservableCollection<PracticeProfile> practitioners;

        [ObservableProperty]
        private ObservableCollection<PracticeProfile> filteredPractitioners;

        [ObservableProperty]
        private ChatSession selectedSession;

        [ObservableProperty]
        private string messageToSend;

        [ObservableProperty]
        private string searchText;

        [ObservableProperty]
        private string practitionerSearchText;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private string typingIndicatorText;

        [ObservableProperty]
        private bool isNewChatDialogVisible;

        [ObservableProperty]
        private bool isImageViewVisible;

        [ObservableProperty]
        private string selectedImage;

        private IDisposable _messageSubscription;
        private IDisposable _typingSubscription;
        private IDisposable _sessionSubscription;

        public ChatViewModel(
            IChatService chatService,
            IFirebaseAuthService authService,
            IFirebaseDataService dataService)
        {
            _chatService = chatService;
            _authService = authService;
            _dataService = dataService;

            Messages = new ObservableCollection<CommuniZEN.Models.ChatMessage>();
            Sessions = new ObservableCollection<ChatSession>();
            FilteredSessions = new ObservableCollection<ChatSession>();
            Practitioners = new ObservableCollection<PracticeProfile>();
            FilteredPractitioners = new ObservableCollection<PracticeProfile>();

            InitializeAsync().ConfigureAwait(false);

            // Setup property changed handlers
            this.PropertyChanged += OnPropertyChanged;
        }

        private async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                IsLoading = true;
                _currentUserId = await _authService.GetCurrentUserIdAsync();
                await LoadChatsAsync();
                SetupTypingTimer();
                SubscribeToSessionUpdates();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to initialize chat: {ex.Message}";
                Debug.WriteLine($"Chat initialization error: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchText))
            {
                FilterSessions();
            }
            else if (e.PropertyName == nameof(PractitionerSearchText))
            {
                FilterPractitioners();
            }
        }

        private void FilterSessions()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredSessions = new ObservableCollection<ChatSession>(Sessions);
                return;
            }

            var filtered = Sessions.Where(s =>
                (s.PractitionerName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.LastMessage?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();

            FilteredSessions = new ObservableCollection<ChatSession>(filtered);
        }

        private void FilterPractitioners()
        {
            if (string.IsNullOrWhiteSpace(PractitionerSearchText))
            {
                FilteredPractitioners = new ObservableCollection<PracticeProfile>(Practitioners);
                return;
            }

            var filtered = Practitioners.Where(p =>
                (p.Name?.Contains(PractitionerSearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Specialization?.Contains(PractitionerSearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.PracticeName?.Contains(PractitionerSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();

            FilteredPractitioners = new ObservableCollection<PracticeProfile>(filtered);
        }

        private void SetupTypingTimer()
        {
            _typingTimer = Application.Current.Dispatcher.CreateTimer();
            _typingTimer.Interval = TimeSpan.FromSeconds(2);
            _typingTimer.Tick += async (s, e) => await StopTypingAsync();
        }

        [RelayCommand]
        private async Task LoadChatsAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var chatSessions = await _chatService.GetActiveChatSessionsAsync();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Sessions.Clear();
                    FilteredSessions.Clear();
                    foreach (var session in chatSessions.OrderByDescending(s => s.LastMessageTimestamp))
                    {
                        Sessions.Add(session);
                        FilteredSessions.Add(session);
                    }
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading chats: {ex.Message}";
                Debug.WriteLine($"Load chats error: {ex}");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task ShowNewChatDialog()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var allPractitioners = await _dataService.GetAllPractitionersAsync();
                Debug.WriteLine($"Found {allPractitioners?.Count ?? 0} practitioners");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Practitioners.Clear();
                    FilteredPractitioners.Clear();

                    if (allPractitioners != null)
                    {
                        foreach (var practitioner in allPractitioners)
                        {
                            Practitioners.Add(practitioner);
                            FilteredPractitioners.Add(practitioner);
                        }
                    }

                    IsNewChatDialogVisible = true;
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading practitioners: {ex.Message}";
                Debug.WriteLine($"Load practitioners error: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SelectPractitioner(PracticeProfile practitioner)
        {
            if (practitioner == null) return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var existingSession = Sessions.FirstOrDefault(s =>
                    s.PractitionerId == practitioner.UserId);

                if (existingSession != null)
                {
                    await SelectSession(existingSession);
                }
                else
                {
                    await CreateNewChatSessionAsync(
                        practitioner.UserId,
                        _currentUserId,
                        practitioner.Name,
                        "Client"
                    );
                }

                IsNewChatDialogVisible = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error starting chat: {ex.Message}";
                Debug.WriteLine($"Start chat error: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SelectSession(ChatSession session)
        {
            if (session == null) return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                UnsubscribeFromCurrentSession();
                SelectedSession = session;

                var messages = await _chatService.GetChatMessagesAsync(session.Id);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Messages.Clear();
                    foreach (var message in messages.OrderBy(m => m.Timestamp))
                    {
                        Messages.Add(message);
                    }
                });

                await MarkMessagesAsReadAsync(session.Id);
                SubscribeToSessionEvents(session.Id);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading messages: {ex.Message}";
                Debug.WriteLine($"Select session error: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(MessageToSend) || SelectedSession == null)
                return;

            var messageText = MessageToSend;
            MessageToSend = string.Empty;

            try
            {
                ErrorMessage = string.Empty;
                await _chatService.SendMessageAsync(SelectedSession.Id, messageText);
                await StopTypingAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error sending message: {ex.Message}";
                MessageToSend = messageText;
                Debug.WriteLine($"Send message error: {ex}");
            }
        }

        [RelayCommand]
        private void ViewImage(string imageUrl)
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                SelectedImage = imageUrl;
                IsImageViewVisible = true;
            }
        }

        [RelayCommand]
        private void CloseImageView()
        {
            IsImageViewVisible = false;
            SelectedImage = null;
        }

        private void SubscribeToSessionEvents(string sessionId)
        {
            _messageSubscription = _chatService.SubscribeToMessages(
                sessionId,
                message => MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Messages.Add(message);
                })
            );

            _typingSubscription = _chatService.SubscribeToTypingStatus(
                sessionId,
                status => MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (status.IsTyping && status.UserId != _currentUserId)
                    {
                        TypingIndicatorText = $"{status.UserName} is typing...";
                    }
                    else
                    {
                        TypingIndicatorText = string.Empty;
                    }
                })
            );
        }

        private void SubscribeToSessionUpdates()
        {
            _sessionSubscription = _chatService.SubscribeToSessionUpdates(
                session => MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var existingSession = Sessions.FirstOrDefault(s => s.Id == session.Id);
                    if (existingSession != null)
                    {
                        var index = Sessions.IndexOf(existingSession);
                        Sessions[index] = session;
                    }
                    else
                    {
                        Sessions.Add(session);
                    }
                })
            );
        }

        private async Task CreateNewChatSessionAsync(string practitionerId, string clientId, string practitionerName, string clientName)
        {
            try
            {
                var session = await _chatService.CreateChatSessionAsync(
                    practitionerId,
                    clientId,
                    practitionerName,
                    clientName
                );

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Sessions.Add(session);
                    SelectedSession = session;
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error creating chat session: {ex.Message}";
                Debug.WriteLine($"Create chat session error: {ex}");
                throw;
            }
        }

        private async Task MarkMessagesAsReadAsync(string sessionId)
        {
            try
            {
                await _chatService.MarkMessagesAsReadAsync(sessionId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error marking messages as read: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task HandleTyping()
        {
            if (!_isTyping && SelectedSession != null)
            {
                _isTyping = true;
                await _chatService.SetTypingStatusAsync(SelectedSession.Id, true);
                _typingTimer.Stop();
                _typingTimer.Start();
            }
        }

        private async Task StopTypingAsync()
        {
            if (_isTyping && SelectedSession != null)
            {
                _isTyping = false;
                await _chatService.SetTypingStatusAsync(SelectedSession.Id, false);
            }
        }

        private void UnsubscribeFromCurrentSession()
        {
            _messageSubscription?.Dispose();
            _typingSubscription?.Dispose();
            _messageSubscription = null;
            _typingSubscription = null;
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await LoadChatsAsync();
        }

        public void OnNavigatingTo()
        {
            if (!_isInitialized)
            {
                InitializeAsync().ConfigureAwait(false);
            }
        }

        public void OnNavigatingFrom()
        {
            UnsubscribeFromCurrentSession();
            _sessionSubscription?.Dispose();
            _typingTimer?.Stop();
        }

        public void Dispose()
        {
            UnsubscribeFromCurrentSession();
            _sessionSubscription?.Dispose();
            _typingTimer?.Stop();
        }
    }
}