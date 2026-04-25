// <copyright file="BaseViewModel.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty] private string errorMessage = string.Empty;

    [ObservableProperty] private bool isLoading;

    public bool HasError => !string.IsNullOrEmpty(this.ErrorMessage);

    partial void OnErrorMessageChanged(string value)
    {
        this.OnPropertyChanged(nameof(this.HasError));
    }
}