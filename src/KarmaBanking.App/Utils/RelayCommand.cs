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
}
