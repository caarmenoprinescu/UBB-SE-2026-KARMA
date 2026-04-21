// <copyright file="ChatView.xaml.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Views;

using KarmaBanking.App.Models;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

public sealed partial class ChatView : Page
{
    public ChatView()
    {
        this.InitializeComponent();
        this.ViewModel = ChatViewModel.Instance;
        this.DataContext = this.ViewModel;
    }

    public ChatViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        this.ViewModel.EnsureSession();
        this.SessionsListView.SelectedItem = this.ViewModel.CurrentSession;
    }

    private async void PresetQuestionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Content is string question)
        {
            await this.ViewModel.AskPresetQuestionAsync(question);
            this.SessionsListView.SelectedItem = this.ViewModel.CurrentSession;
        }
    }

    private void SessionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (this.SessionsListView.SelectedItem is ChatSession session)
        {
            this.ViewModel.SelectSession(session);
        }
    }

    private void ContactTeamButton_Click(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(ChatRoutingView));
    }
}