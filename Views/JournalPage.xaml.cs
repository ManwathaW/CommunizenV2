using Microsoft.Maui.Controls;
using System;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommuniZEN.Models;
using CommuniZEN.ViewModels;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace CommuniZEN.Views
{
    public partial class JournalPage : ContentPage
    {
        private readonly JournalViewModel _viewModel;

        public JournalPage(JournalViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            // Subscribe to recording state changes to update UI accordingly
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(JournalViewModel.IsRecording))
                {
                    UpdateRecordingUI(_viewModel.IsRecording);
                }
            };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadEntries();
        }

        private void UpdateRecordingUI(bool isRecording)
        {
            // Update UI elements based on recording state
            RecordButton.IsVisible = !isRecording;
            StopButton.IsVisible = isRecording;
            TextEditor.IsEnabled = !isRecording;
            SaveTextButton.IsEnabled = !isRecording;
        }

        private async void OnStartRecording(object sender, EventArgs e)
        {
            if (_viewModel.StartRecordingCommand.CanExecute(null))
            {
                _viewModel.StartRecordingCommand.Execute(null);
                await ShowToast("Recording started...");
            }
        }

        private async void OnStopRecording(object sender, EventArgs e)
        {
            if (_viewModel.StopRecordingCommand.CanExecute(null))
            {
                _viewModel.StopRecordingCommand.Execute(null);
                await ShowToast("Recording saved successfully");
            }
        }

        private async void OnSaveNote(object sender, EventArgs e)
        {
            if (_viewModel.SaveTextNoteCommand.CanExecute(null))
            {
                _viewModel.SaveTextNoteCommand.Execute(null);
                await ShowToast("Note saved successfully");
            }
        }

        private async void OnShareNote(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is JournalEntry entry)
            {
                if (_viewModel.ShareNoteCommand.CanExecute(entry))
                {
                    _viewModel.ShareNoteCommand.Execute(entry);
                }
            }
        }

        private async Task ShowToast(string message)
        {
            var toast = Toast.Make(message, ToastDuration.Short, 14);
            await toast.Show(new CancellationTokenSource().Token);
        }
    }
}