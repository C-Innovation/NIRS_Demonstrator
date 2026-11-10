using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Реализация <see cref="ITaskManager"/> по умолчанию.
    /// </summary>
    public class TaskManager : ITaskManager
    {
        /// <inheritdoc />
        public Task Run(Action action,
                        [CallerMemberName] string origin = "",
                        [CallerFilePath] string filePath = "",
                        [CallerLineNumber] int lineNumber = 0)
            => Guard(() => System.Threading.Tasks.Task.Run(action), origin, filePath, lineNumber);

        /// <inheritdoc />
        public Task Run(Action action,
                        CancellationToken cancellationToken,
                        [CallerMemberName] string origin = "",
                        [CallerFilePath] string filePath = "",
                        [CallerLineNumber] int lineNumber = 0)
            => Guard(() => System.Threading.Tasks.Task.Run(action, cancellationToken), origin, filePath, lineNumber);

        /// <inheritdoc />
        public Task Run(Func<Task> function,
                        [CallerMemberName] string origin = "",
                        [CallerFilePath] string filePath = "",
                        [CallerLineNumber] int lineNumber = 0)
            => Guard(() => System.Threading.Tasks.Task.Run(function), origin, filePath, lineNumber);

        /// <inheritdoc />
        public Task Run(Func<Task> function,
                        CancellationToken cancellationToken,
                        [CallerMemberName] string origin = "",
                        [CallerFilePath] string filePath = "",
                        [CallerLineNumber] int lineNumber = 0)
            => Guard(() => System.Threading.Tasks.Task.Run(function, cancellationToken), origin, filePath, lineNumber);

        /// <inheritdoc />
        public Task<TResult> Run<TResult>(Func<TResult> function,
                                          [CallerMemberName] string origin = "",
                                          [CallerFilePath] string filePath = "",
                                          [CallerLineNumber] int lineNumber = 0)
            => Guard(() => System.Threading.Tasks.Task.Run(function), origin, filePath, lineNumber);

        /// <inheritdoc />
        public Task<TResult> Run<TResult>(Func<TResult> function,
                                          CancellationToken cancellationToken,
                                          [CallerMemberName] string origin = "",
                                          [CallerFilePath] string filePath = "",
                                          [CallerLineNumber] int lineNumber = 0)
            => Guard(() => System.Threading.Tasks.Task.Run(function, cancellationToken), origin, filePath, lineNumber);

        /// <inheritdoc />
        public Task<TResult> Run<TResult>(Func<Task<TResult>> function,
                                          [CallerMemberName] string origin = "",
                                          [CallerFilePath] string filePath = "",
                                          [CallerLineNumber] int lineNumber = 0)
            => Guard(() => System.Threading.Tasks.Task.Run(function), origin, filePath, lineNumber);

        /// <inheritdoc />
        public Task<TResult> Run<TResult>(Func<Task<TResult>> function,
                                          CancellationToken cancellationToken,
                                          [CallerMemberName] string origin = "",
                                          [CallerFilePath] string filePath = "",
                                          [CallerLineNumber] int lineNumber = 0)
            => Guard(() => System.Threading.Tasks.Task.Run(function, cancellationToken), origin, filePath, lineNumber);

        /// <summary>
        /// Запускает работу, протоколируя исключение и пробрасывая его дальше.
        /// Отмена не считается ошибкой.
        /// </summary>
        private static async Task Guard(Func<Task> start, string origin, string filePath, int lineNumber)
        {
            try
            {
                await start();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, origin, filePath, lineNumber);
                throw;
            }
        }

        private static async Task<TResult> Guard<TResult>(Func<Task<TResult>> start, string origin, string filePath, int lineNumber)
        {
            try
            {
                return await start();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogError(ex, origin, filePath, lineNumber);
                throw;
            }
        }

        private static void LogError(Exception ex, string origin, string filePath, int lineNumber)
        {
            if (!IoC.IsConfigured)
                return;

            IoC.Logger.Log("Ошибка в фоновой задаче: " + ex.Message, LogLevel.Error, origin, filePath, lineNumber);
        }
    }
}
