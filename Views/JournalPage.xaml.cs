using CommunityToolkit.Maui.Core;
using CommuniZEN.ViewModels;
using CommuniZEN.Models;
using Microsoft.Maui.Controls;
using System;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;


namespace CommuniZEN.Views;

public partial class JournalPage : ContentPage
{
    private readonly JournalViewModel _viewModel;
    public JournalPage(JournalViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadEntries();
    }

    // Event handlers with exact names matching the XAML
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
            await ShowToast("Recording saved");
        }
    }

    private async void OnSaveNote(object sender, EventArgs e)
    {
        if (_viewModel.SaveTextNoteCommand.CanExecute(null))
        {
            _viewModel.SaveTextNoteCommand.Execute(null);
            await ShowToast("Note saved");
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
        await toast.Show();
    }
}
