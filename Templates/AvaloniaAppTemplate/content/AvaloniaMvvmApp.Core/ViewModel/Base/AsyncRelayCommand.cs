using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Асинхронная команда.
    /// <para>
    /// Нужна, чтобы обработчик не писался как <c>async void</c>: исключение внутри
    /// <c>async void</c> уходит в необработанные и роняет приложение. Здесь задача
    /// ожидается, ошибки протоколируются, а повторный запуск блокируется до
    /// завершения предыдущего.
    /// </para>
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Выполняется ли команда в данный момент.
        /// </summary>
        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                _isExecuting = value;
                RaiseCanExecuteChanged();
            }
        }

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute == null || _canExecute());
        }

        public void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            _ = ExecuteAsync();
        }

        /// <summary>
        /// Запускает команду и возвращает её задачу — удобно в тестах
        /// и при вызове одной команды из другой.
        /// </summary>
        public async Task ExecuteAsync()
        {
            IsExecuting = true;
            try
            {
                await _execute();
            }
            catch (Exception ex)
            {
                IoC.Logger.Log($"Необработанная ошибка в асинхронной команде: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                IsExecuting = false;
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
