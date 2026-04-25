// <copyright file="RelayCommand.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Utils;

using System;
using System.Threading.Tasks;
using System.Windows.Input;

/// <summary>
/// A command whose sole purpose is to relay its functionality to other objects by invoking delegates.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Func<bool>? canExecute;
    private readonly Func<Task> executeAsync;
    private bool isExecuting;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class.
    /// </summary>
    /// <param name="executeAsync">The execution logic.</param>
    /// <param name="canExecute">The execution status logic.</param>
    public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        this.executeAsync = executeAsync;
        this.canExecute = canExecute;
    }

    /// <summary>
    /// Occurs when changes occur that affect whether or not the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Defines the method that determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
    /// <returns>True if this command can be executed; otherwise, false.</returns>
    public bool CanExecute(object? parameter)
    {
        return !this.isExecuting && (this.canExecute == null || this.canExecute());
    }

    /// <summary>
    /// Defines the method to be called when the command is invoked.
    /// </summary>
    /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
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

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event to instruct the UI to re-evaluate the command execution status.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}