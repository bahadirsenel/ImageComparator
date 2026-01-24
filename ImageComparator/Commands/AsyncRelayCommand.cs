using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ImageComparator.Commands
{
    /// <summary>
    /// An async command whose sole purpose is to relay its functionality to other
    /// objects by invoking async delegates. Provides execution status tracking.
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object, Task> _execute;
        private readonly Predicate<object> _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Creates a new async command that can always execute.
        /// </summary>
        /// <param name="execute">The async execution logic.</param>
        public AsyncRelayCommand(Func<object, Task> execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Creates a new async command.
        /// </summary>
        /// <param name="execute">The async execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        public AsyncRelayCommand(Func<object, Task> execute, Predicate<object> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Gets whether the command is currently executing.
        /// </summary>
        public bool IsExecuting
        {
            get { return _isExecuting; }
            private set
            {
                _isExecuting = value;
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Raises the CanExecuteChanged event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command.</param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            return !IsExecuting && (_canExecute == null || _canExecute(parameter));
        }

        /// <summary>
        /// Executes the command logic asynchronously.
        /// </summary>
        /// <param name="parameter">Data used by the command.</param>
        public async void Execute(object parameter)
        {
            IsExecuting = true;
            try
            {
                await _execute(parameter);
            }
            finally
            {
                IsExecuting = false;
            }
        }
    }
}
