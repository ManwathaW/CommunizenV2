using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using System.Collections.ObjectModel;
using CommuniZEN.Interfaces;
using Plugin.Maui.Audio;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using CommunityToolkit.Maui.Alerts;

namespace CommuniZEN.ViewModels
{
    public class JournalViewModel : ObservableObject
    {
        private readonly IAudioManager _audioManager;
        private readonly IFirebaseDataService _firebaseService;
        private readonly IFirebaseAuthService _authService;
        private IAudioRecorder _audioRecorder;
        private ObservableCollection<JournalEntry> _entries;
        private bool _isRecording;
        private string _currentText;

        public JournalViewModel(
            IAudioManager audioManager,
            IFirebaseDataService firebaseService,
            IFirebaseAuthService authService)
        {
            _audioManager = audioManager;
            _firebaseService = firebaseService;
            _authService = authService;
            _audioRecorder = _audioManager.CreateRecorder();
            _entries = new ObservableCollection<JournalEntry>();

            StartRecordingCommand = new Command(async () => await StartRecording());
            StopRecordingCommand = new Command(async () => await StopRecording());
            SaveTextNoteCommand = new Command(async () => await SaveTextNote());
            ShareNoteCommand = new Command<JournalEntry>(async (entry) => await ShareNote(entry));
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

        public Command StartRecordingCommand { get; }
        public Command StopRecordingCommand { get; }
        public Command SaveTextNoteCommand { get; }
        public Command<JournalEntry> ShareNoteCommand { get; }

        private async Task StartRecording()
        {
            try
            {
                IsRecording = true;
                await _audioRecorder.StartAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to start recording: " + ex.Message, "OK");
            }
        }

        private async Task StopRecording()
        {
            try
            {
                if (!IsRecording) return;

                IsRecording = false;
                var audioFile = await _audioRecorder.StopAsync();
                await SaveAudioNote(audioFile);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to stop recording: " + ex.Message, "OK");
            }
        }

        private async Task SaveAudioNote(IAudioSource audioSource)
        {
            try
            {
                var userId = await _authService.GetCurrentUserIdAsync();

                // Create a temporary entry to get an ID
                var entry = new JournalEntry
                {
                    Type = JournalEntryType.Audio,
                    Timestamp = DateTime.Now,
                    Content = "Uploading..." // Temporary content
                };

                var entryId = await _firebaseService.CreateJournalEntryAsync(userId, entry);

                // Upload the audio file
                using (var memoryStream = new MemoryStream())
                {
                    await audioSource.GetAudioStream().CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    var audioUrl = await _firebaseService.UploadJournalAudioAsync(userId, entryId, memoryStream);

                    // Update the entry with the audio URL
                    entry.Id = entryId;
                    entry.Content = audioUrl;
                    await _firebaseService.UpdateJournalEntryAsync(userId, entryId, entry);

                    // Update the local collection
                    Entries.Insert(0, entry);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to save audio note: " + ex.Message, "OK");
            }
        }

        private async Task SaveTextNote()
        {
            if (string.IsNullOrWhiteSpace(CurrentText))
                return;

            try
            {
                var userId = await _authService.GetCurrentUserIdAsync();

                var entry = new JournalEntry
                {
                    Type = JournalEntryType.Text,
                    Content = CurrentText,
                    Timestamp = DateTime.Now
                };

                var entryId = await _firebaseService.CreateJournalEntryAsync(userId, entry);
                entry.Id = entryId;

                Entries.Insert(0, entry);
                CurrentText = string.Empty;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to save text note: " + ex.Message, "OK");
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
                else
                {
                    await Share.RequestAsync(new ShareFileRequest
                    {
                        File = new ShareFile(entry.Content),
                        Title = $"Audio Note - {entry.Timestamp:g}"
                    });
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to share note: " + ex.Message, "OK");
            }
        }

        public async Task LoadEntries()
        {
            try
            {
                var userId = await _authService.GetCurrentUserIdAsync();
                var entries = await _firebaseService.GetUserJournalEntriesAsync(userId);

                Entries.Clear();
                foreach (var entry in entries)
                {
                    Entries.Add(entry);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to load entries: " + ex.Message, "OK");
            }
        }
    }
}