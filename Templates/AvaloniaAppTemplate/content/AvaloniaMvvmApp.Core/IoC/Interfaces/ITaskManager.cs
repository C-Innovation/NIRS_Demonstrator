using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Запуск фоновых работ с протоколированием исключений.
    /// Исключение не глушится: оно записывается в журнал и пробрасывается дальше.
    /// </summary>
    public interface ITaskManager
    {
        Task Run(Action action,
                 [CallerMemberName] string origin = "",
                 [CallerFilePath] string filePath = "",
                 [CallerLineNumber] int lineNumber = 0);

        Task Run(Action action,
                 CancellationToken cancellationToken,
                 [CallerMemberName] string origin = "",
                 [CallerFilePath] string filePath = "",
                 [CallerLineNumber] int lineNumber = 0);

        Task Run(Func<Task> function,
                 [CallerMemberName] string origin = "",
                 [CallerFilePath] string filePath = "",
                 [CallerLineNumber] int lineNumber = 0);

        Task Run(Func<Task> function,
                 CancellationToken cancellationToken,
                 [CallerMemberName] string origin = "",
                 [CallerFilePath] string filePath = "",
                 [CallerLineNumber] int lineNumber = 0);

        Task<TResult> Run<TResult>(Func<TResult> function,
                                   [CallerMemberName] string origin = "",
                                   [CallerFilePath] string filePath = "",
                                   [CallerLineNumber] int lineNumber = 0);

        Task<TResult> Run<TResult>(Func<TResult> function,
                                   CancellationToken cancellationToken,
                                   [CallerMemberName] string origin = "",
                                   [CallerFilePath] string filePath = "",
                                   [CallerLineNumber] int lineNumber = 0);

        Task<TResult> Run<TResult>(Func<Task<TResult>> function,
                                   [CallerMemberName] string origin = "",
                                   [CallerFilePath] string filePath = "",
                                   [CallerLineNumber] int lineNumber = 0);

        Task<TResult> Run<TResult>(Func<Task<TResult>> function,
                                   CancellationToken cancellationToken,
                                   [CallerMemberName] string origin = "",
                                   [CallerFilePath] string filePath = "",
                                   [CallerLineNumber] int lineNumber = 0);
    }
}
