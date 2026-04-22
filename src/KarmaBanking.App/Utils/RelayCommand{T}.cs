// <copyright file="RelayCommand{T}.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Utils;

using System;
using System.Windows.Input;

/// <summary>
/// A generic command whose sole purpose is to relay its functionality to other objects by invoking delegates.
/// </summary>
/// <typeparam name="T">The type of the command parameter.</typeparam>
public class RelayCommand<T> : ICommand
{
    private readonly Func<T?, bool>? canExecute;
    private readonly Action<T?> execute;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand{T}"/> class.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <param name="canExecute">The execution status logic.</param>
    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    /// <summary>
    /// Occurs when changes occur that affect whether or not the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Defines the method that determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">Data used by the command.</param>
    /// <returns>True if this command can be executed; otherwise, false.</returns>
    public bool CanExecute(object? parameter)
    {
        if (this.canExecute == null)
        {
            return true;
        }

        return this.canExecute(ConvertParameter(parameter));
    }

    /// <summary>
    /// Defines the method to be called when the command is invoked.
    /// </summary>
    /// <param name="parameter">Data used by the command.</param>
    public void Execute(object? parameter)
    {
        this.execute(ConvertParameter(parameter));
    }

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event to instruct the UI to re-evaluate the command execution status.
    /// </summary>
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