using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Асинхронный аналог <c>lock</c>: позволяет дождаться завершения работ,
    /// обращающихся к одному и тому же ресурсу, не блокируя поток.
    /// <para>
    /// Семафоры хранятся по ключу. Ключ — произвольная строка, обычно
    /// имя ресурса (путь к файлу, идентификатор устройства).
    /// </para>
    /// </summary>
    public static class AsyncAwaiter
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores
            = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Выполняет работу, вернув результат, под замком указанного ключа.
        /// </summary>
        /// <param name="key">Ключ ресурса.</param>
        /// <param name="task">Выполняемая работа.</param>
        /// <param name="maxAccessCount">
        /// Сколько работ допускается одновременно. Учитывается только при
        /// первом обращении к данному ключу.
        /// </param>
        public static async Task<T> AwaitResultAsync<T>(string key, Func<Task<T>> task, int maxAccessCount = 1)
        {
            var semaphore = _semaphores.GetOrAdd(key, _ => new SemaphoreSlim(maxAccessCount, maxAccessCount));

            await semaphore.WaitAsync();
            try
            {
                return await task();
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Выполняет работу под замком указанного ключа.
        /// </summary>
        public static async Task AwaitAsync(string key, Func<Task> task, int maxAccessCount = 1)
        {
            var semaphore = _semaphores.GetOrAdd(key, _ => new SemaphoreSlim(maxAccessCount, maxAccessCount));

            await semaphore.WaitAsync();
            try
            {
                await task();
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
