using System;
using System.Windows.Input;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Команда с параметром, выполняющая <see cref="Action{Object}"/>.
    /// </summary>
    public class RelayParameterizedCommand : ICommand
    {
        private readonly Action<object?> _action;
        private readonly Func<object?, bool>? _canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayParameterizedCommand(Action<object?> action, Func<object?, bool>? canExecute = null)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
                _action(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
