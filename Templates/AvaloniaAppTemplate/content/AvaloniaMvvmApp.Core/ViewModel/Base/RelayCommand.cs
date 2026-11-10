using System;
using System.Windows.Input;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Команда без параметра, выполняющая <see cref="Action"/>.
    /// Поддерживает условие доступности через <c>canExecute</c>.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _action;
        private readonly Func<bool>? _canExecute;

        /// <summary>
        /// Возникает при изменении результата <see cref="CanExecute"/>.
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action action, Func<bool>? canExecute = null)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
                _action();
        }

        /// <summary>
        /// Просит элементы управления перечитать <see cref="CanExecute"/>.
        /// Вызывайте из сеттеров свойств, влияющих на доступность команды.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
