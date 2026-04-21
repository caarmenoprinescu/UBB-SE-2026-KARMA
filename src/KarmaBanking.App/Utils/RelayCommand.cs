// <copyright file="RelayCommand.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Utils;

using System;
using System.Threading.Tasks;
using System.Windows.Input;

public class RelayCommand : ICommand
{
    private readonly Func<bool>? canExecute;
    private readonly Func<Task> executeAsync;
    private bool isExecuting;

    public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        this.executeAsync = executeAsync;
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return !this.isExecuting && (this.canExecute == null || this.canExecute());
    }

    public async void Execute(object? parameter)
    {
        this.isExecuting = true;
        this.RaiseCanExecuteChanged();
        try
        {
            await this.executeAsync();
        }
        finally
        {
            this.isExecuting = false;
            this.RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

public class RelayCommand<T> : ICommand
{
    private readonly Func<T?, bool>? canExecute;
    private readonly Action<T?> execute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (this.canExecute == null)
        {
            return true;
        }

        return this.canExecute(ConvertParameter(parameter));
    }

    public void Execute(object? parameter)
    {
        this.execute(ConvertParameter(parameter));
    }

    public void RaiseCanExecuteChanged()
    {
        this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private static T? ConvertParameter(object? parameter)
    {
        if (parameter == null)
        {
            return default;
        }

        if (parameter is T value)
        {
            return value;
        }

        return (T?)Convert.ChangeType(parameter, typeof(T));
    }
}