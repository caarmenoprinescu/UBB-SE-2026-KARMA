// <copyright file="DialogService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

public class DialogService
{
    public async Task<ContentDialogResult> ShowConfirmDialogAsync(
        string title,
        string message,
        string primaryButtonText,
        string closeButtonText,
        XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText,
            XamlRoot = xamlRoot,
        };

        var result = await WindowsRuntimeSystemExtensions.AsTask(dialog.ShowAsync());
        return result;
    }

    public async Task ShowErrorDialogAsync(
        string title,
        string message,
        XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = xamlRoot,
        };

        await WindowsRuntimeSystemExtensions.AsTask(dialog.ShowAsync());
    }

    public async Task<(ContentDialogResult Result, string InputText)> ShowInputDialogAsync(
        string title,
        string placeholder,
        string primaryButtonText,
        string closeButtonText,
        XamlRoot xamlRoot)
    {
        var inputTextBox = new TextBox
        {
            PlaceholderText = placeholder,
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = inputTextBox,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText,
            XamlRoot = xamlRoot,
        };

        var result = await WindowsRuntimeSystemExtensions.AsTask(dialog.ShowAsync());
        return (result, inputTextBox.Text);
    }

    public async Task ShowInfoDialogAsync(
        string title,
        string message,
        XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = xamlRoot,
        };

        await WindowsRuntimeSystemExtensions.AsTask(dialog.ShowAsync());
    }
}