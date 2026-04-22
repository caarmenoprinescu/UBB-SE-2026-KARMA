// <copyright file="App.xaml.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App;

using Microsoft.UI.Xaml;

public partial class App : Application
{
    private Window? window;

    public App()
    {
        this.InitializeComponent();
    }

    public static Window MainAppWindow { get; private set; } = null!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        this.window = new MainWindow();
        MainAppWindow = this.window;
        this.window.Activate();
    }
}