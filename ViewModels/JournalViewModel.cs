using CommunityToolkit.Mvvm.ComponentModel;
using CommuniZEN.Models;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Storage;
using Plugin.Maui.Audio;
using System.Collections.ObjectModel;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Firebase.Database;
using Firebase.Storage;
using Plugin.Maui.Audio;
using System.IO;

public class JournalViewModel : ObservableObject
{
    private readonly IAudioManager _audioManager;
    private IAudioRecorder _audioRecorder;
    private readonly FirebaseClient _firebaseClient;
    private readonly FirebaseStorage _firebaseStorage;
    private ObservableCollection<JournalEntry> _entries;
    private bool _isRecording;
    private string _currentText;

    public JournalViewModel(IAudioManager audioManager, FirebaseClient firebaseClient, FirebaseStorage firebaseStorage)
    {
        _audioManager = audioManager;
        _audioRecorder = _audioManager.CreateRecorder();
        _firebaseClient = firebaseClient;
        _firebaseStorage = firebaseStorage;
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
            using (var memoryStream = new MemoryStream())
            {
                await audioSource.GetAudioStream().CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var fileName = $"audio_{DateTime.Now:yyyyMMddHHmmss}.m4a";
                var audioUrl = await _firebaseStorage
                    .Child("audio_notes")
                    .Child(fileName)
                    .PutAsync(memoryStream);

                var entry = new JournalEntry
                {
                    Type = JournalEntryType.Audio,
                    Content = audioUrl,
                    Timestamp = DateTime.Now
                };

                var postResult = await _firebaseClient
                    .Child("journal_entries")
                    .PostAsync("{\"type\":\"" + entry.Type + "\",\"content\":\"" + entry.Content + "\",\"timestamp\":\"" + entry.Timestamp.ToString("o") + "\"}");

                entry.Id = postResult.Key;
                Entries.Add(entry);
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
            var entry = new JournalEntry
            {
                Type = JournalEntryType.Text,
                Content = CurrentText,
                Timestamp = DateTime.Now
            };

            var postResult = await _firebaseClient
                .Child("journal_entries")
                .PostAsync("{\"type\":\"" + entry.Type + "\",\"content\":\"" + entry.Content + "\",\"timestamp\":\"" + entry.Timestamp.ToString("o") + "\"}");

            entry.Id = postResult.Key;
            Entries.Add(entry);
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
            var entries = await _firebaseClient
                .Child("journal_entries")
                .OnceAsync<JournalEntry>();

            Entries.Clear();
            foreach (var entry in entries)
            {
                var journalEntry = entry.Object;
                journalEntry.Id = entry.Key;
                Entries.Add(journalEntry);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", "Failed to load entries: " + ex.Message, "OK");
        }
    }
}