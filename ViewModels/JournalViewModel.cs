using CommuniZEN.Interfaces;
using Plugin.Maui.Audio;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommuniZEN.Models;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommuniZEN.ViewModels
{
    public class JournalViewModel : ObservableObject
    {
        private readonly IAudioManager _audioManager;
        private readonly IFirebaseDataService _firebaseService;
        private readonly IFirebaseAuthService _authService;
        private IAudioRecorder _audioRecorder;
        private IAudioPlayer _audioPlayer;
        private ObservableCollection<JournalEntry> _entries;
        private bool _isRecording;
        private string _currentText;
        private string _currentlyPlayingEntryId;
        private float _audioLevel;
        private bool _isLoading;
        private Random _random = new Random(); // For demo visualization
        private IDispatcherTimer _recordingTimer;

        public JournalViewModel(
            IAudioManager audioManager,
            IFirebaseDataService firebaseService,
            IFirebaseAuthService authService)
        {
            _audioManager = audioManager;
            _firebaseService = firebaseService;
            _authService = authService;
            InitializeAudio();
            _entries = new ObservableCollection<JournalEntry>();

            StartRecordingCommand = new Command(async () => await StartRecording());
            StopRecordingCommand = new Command(async () => await StopRecording());
            SaveTextNoteCommand = new Command(async () => await SaveTextNote());
            ShareNoteCommand = new Command<JournalEntry>(async (entry) => await ShareNote(entry));
            PlayAudioCommand = new Command<JournalEntry>(async (entry) => await PlayAudio(entry));
        }

        public ObservableCollection<JournalEntry> Entries
        {
            get => _entries;
            set => SetProperty(ref _entries, value);
        }

        public string CurrentText
        {
            get => _currentText;
            set => SetProperty(ref _currentText, value);
        }

        public bool IsRecording
        {
            get => _isRecording;
            set => SetProperty(ref _isRecording, value);
        }

        public float AudioLevel
        {
            get => _audioLevel;
            set => SetProperty(ref _audioLevel, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsPlaying => _currentlyPlayingEntryId != null && _audioPlayer?.IsPlaying == true;

        public string CurrentlyPlayingEntryId
        {
            get => _currentlyPlayingEntryId;
            private set
            {
                if (SetProperty(ref _currentlyPlayingEntryId, value))
                {
                    OnPropertyChanged(nameof(IsPlaying));
                }
            }
        }

        public ICommand StartRecordingCommand { get; }
        public ICommand StopRecordingCommand { get; }
        public ICommand SaveTextNoteCommand { get; }
        public ICommand ShareNoteCommand { get; }
        public ICommand PlayAudioCommand { get; }

        private void InitializeAudio()
        {
            try
            {
                _audioRecorder = _audioManager.CreateRecorder();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Audio initialization error: {ex.Message}");
            }
        }

        private void StartVisualization()
        {
            _recordingTimer = Application.Current.Dispatcher.CreateTimer();
            _recordingTimer.Interval = TimeSpan.FromMilliseconds(100);
            _recordingTimer.Tick += (s, e) =>
            {
                // In a real implementation, you'd get actual audio levels
                AudioLevel = (float)_random.NextDouble();
            };
            _recordingTimer.Start();
        }

        private void StopVisualization()
        {
            _recordingTimer?.Stop();
            AudioLevel = 0;
        }

        private async Task StartRecording()
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    await Shell.Current.DisplayAlert("Permission Required",
                        "Microphone permission is required to record audio.", "OK");
                    return;
                }

                IsRecording = true;
                await _audioRecorder.StartAsync();
                StartVisualization();
            }
            catch (Exception ex)
            {
                IsRecording = false;
                Debug.WriteLine($"Recording error: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to start recording: " + ex.Message, "OK");
            }
        }

        private async Task StopRecording()
        {
            try
            {
                if (!IsRecording) return;

                IsRecording = false;
                StopVisualization();
                var audioFile = await _audioRecorder.StopAsync();
                await SaveAudioNote(audioFile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Stop recording error: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to stop recording: " + ex.Message, "OK");
            }
        }

        private async Task SaveAudioNote(IAudioSource audioSource)
        {
            try
            {
                IsLoading = true;
                var userId = await _authService.GetCurrentUserIdAsync();

                // Convert audio to base64 string for database storage
                using var memoryStream = new MemoryStream();
                var audioStream = audioSource.GetAudioStream();
                await audioStream.CopyToAsync(memoryStream);
                var audioData = Convert.ToBase64String(memoryStream.ToArray());

                var entry = new JournalEntry
                {
                    Type = JournalEntryType.Audio,
                    Content = audioData,
                    Timestamp = DateTime.UtcNow
                };

                // Save to Firebase Realtime Database
                var entryId = await _firebaseService.CreateJournalEntryAsync(userId, entry);
                entry.Id = entryId;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Entries.Insert(0, entry);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Save audio error: {ex}");
                await Shell.Current.DisplayAlert("Error",
                    "Failed to save audio note: " + ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveTextNote()
        {
            if (string.IsNullOrWhiteSpace(CurrentText))
                return;

            try
            {
                IsLoading = true;
                var userId = await _authService.GetCurrentUserIdAsync();
                var entry = new JournalEntry
                {
                    Type = JournalEntryType.Text,
                    Content = CurrentText,
                    Timestamp = DateTime.UtcNow
                };

                var entryId = await _firebaseService.CreateJournalEntryAsync(userId, entry);
                entry.Id = entryId;

                Entries.Insert(0, entry);
                CurrentText = string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Save text error: {ex}");
                await Shell.Current.DisplayAlert("Error",
                    "Failed to save text note: " + ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PlayAudio(JournalEntry entry)
        {
            if (entry?.Type != JournalEntryType.Audio || string.IsNullOrEmpty(entry.Content))
                return;

            try
            {
                // If this entry is already playing, stop it
                if (_currentlyPlayingEntryId == entry.Id && _audioPlayer?.IsPlaying == true)
                {
                    _audioPlayer?.Stop();
                    CurrentlyPlayingEntryId = null;
                    return;
                }

                // Stop any currently playing audio
                if (_audioPlayer?.IsPlaying == true)
                {
                    _audioPlayer.Stop();
                }

                // Convert base64 string back to stream
                byte[] audioBytes = Convert.FromBase64String(entry.Content);
                var stream = new MemoryStream(audioBytes);

                _audioPlayer = _audioManager.CreatePlayer(stream);
                CurrentlyPlayingEntryId = entry.Id;

                _audioPlayer.PlaybackEnded += (s, e) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        CurrentlyPlayingEntryId = null;
                    });
                };

                _audioPlayer.Play();
            }
            catch (Exception ex)
            {
                CurrentlyPlayingEntryId = null;
                Debug.WriteLine($"Play audio error: {ex}");
                await Shell.Current.DisplayAlert("Error", "Failed to play audio: " + ex.Message, "OK");
            }
        }

        private async Task ShareNote(JournalEntry entry)
        {
            try
            {
                if (entry.Type == JournalEntryType.Text)
                {
                    await Share.RequestAsync(new ShareTextRequest
                    {
                        Text = entry.Content,
                        Title = $"Journal Entry - {entry.Timestamp:g}"
                    });
                }
                else if (entry.Type == JournalEntryType.Audio)
                {
                    // For audio entries, create a temporary file and share that
                    var base64Data = entry.Content;
                    byte[] audioBytes = Convert.FromBase64String(base64Data);

                    var tempFile = Path.Combine(FileSystem.CacheDirectory,
                        $"audio_{DateTime.Now:yyyyMMddHHmmss}.m4a");
                    await File.WriteAllBytesAsync(tempFile, audioBytes);

                    await Share.RequestAsync(new ShareFileRequest
                    {
                        Title = $"Audio Note - {entry.Timestamp:g}",
                        File = new ShareFile(tempFile)
                    });

                    // Clean up the temporary file after sharing
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Share error: {ex}");
                await Shell.Current.DisplayAlert("Error",
                    "Failed to share note: " + ex.Message, "OK");
            }
        }

        public async Task LoadEntries()
        {
            try
            {
                IsLoading = true;
                var userId = await _authService.GetCurrentUserIdAsync();
                var entries = await _firebaseService.GetUserJournalEntriesAsync(userId);

                Entries.Clear();
                foreach (var entry in entries.OrderByDescending(e => e.Timestamp))
                {
                    Entries.Add(entry);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Load entries error: {ex}");
                await Shell.Current.DisplayAlert("Error",
                    "Failed to load entries: " + ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}