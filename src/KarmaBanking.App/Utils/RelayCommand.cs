using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KarmaBanking.App.Utils
{
    public class RelayCommand : ICommand
    {
        private readonly Func<Task> executeAsync;
        private readonly Func<bool>? canExecute;
        private bool isExecuting;

        public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            this.executeAsync = executeAsync;
            this.canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return !isExecuting && (canExecute == null || canExecute());
        }

        public async void Execute(object? parameter)
        {
            isExecuting = true;
            RaiseCanExecuteChanged();
            try
            {
                await executeAsync();
            }
            finally
            {
                isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> execute;
        private readonly Func<T?, bool>? canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            if (canExecute == null)
            {
                return true;
            }

            return canExecute(ConvertParameter(parameter));
        }

        public void Execute(object? parameter)
        {
            execute(ConvertParameter(parameter));
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
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
}
