using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaMvvmApp.Helpers.Animation
{
    /// <summary>
    /// Анимация произвольного свойства Avalonia от одного значения к другому.
    /// <para>
    /// Для каждого элемента управления заводится свой затвор, поэтому две
    /// анимации одного и того же элемента не наложатся: вторая дождётся первой.
    /// Ожидание асинхронное — потока не занимает.
    /// </para>
    /// </summary>
    public class PropertyAnimator : IDisposable
    {
        /// <summary>
        /// Затворы по элементам управления.
        /// </summary>
        private readonly ConcurrentDictionary<Control, SemaphoreSlim> _gates
            = new ConcurrentDictionary<Control, SemaphoreSlim>();

        private bool _isDisposed;

        /// <summary>
        /// Плавно изменяет свойство элемента управления.
        /// </summary>
        /// <param name="property">Анимируемое свойство.</param>
        /// <param name="control">Элемент управления.</param>
        /// <param name="from">Начальное значение.</param>
        /// <param name="to">Конечное значение.</param>
        /// <param name="durationMs">Длительность в миллисекундах.</param>
        /// <param name="easing">Функция сглаживания. <c>null</c> — квадратичная.</param>
        /// <param name="direction">Направление проигрывания.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        public async Task RunAsync(AvaloniaProperty property,
                                   Control control,
                                   object? from,
                                   object? to,
                                   double durationMs = 300,
                                   Easing? easing = null,
                                   PlaybackDirection direction = PlaybackDirection.Normal,
                                   CancellationToken cancellationToken = default)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (_isDisposed)
                return;

            var gate = _gates.GetOrAdd(control, _ => new SemaphoreSlim(1, 1));

            await gate.WaitAsync(cancellationToken);
            try
            {
                var animation = new Avalonia.Animation.Animation
                {
                    Duration = TimeSpan.FromMilliseconds(durationMs),
                    PlaybackDirection = direction,
                    Easing = easing ?? new QuadraticEaseInOut(),

                    // Без FillMode.Forward значение свойства откатится
                    // к исходному сразу после завершения анимации.
                    FillMode = FillMode.Forward,
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0d),
                            Setters = { new Setter(property, from) },
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1d),
                            Setters = { new Setter(property, to) },
                        },
                    },
                };

                await animation.RunAsync(control, cancellationToken);

                // Закрепляем конечное значение как локальное: дальше свойство
                // можно менять обычным присваиванием.
                control.SetValue(property, to);
            }
            finally
            {
                gate.Release();
            }
        }

        /// <summary>
        /// Снимает элемент управления с учёта.
        /// Вызывайте при выгрузке элемента, иначе он останется в словаре.
        /// </summary>
        public void Forget(Control control)
        {
            if (control != null && _gates.TryRemove(control, out var gate))
                gate.Dispose();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            foreach (var gate in _gates.Values)
                gate.Dispose();

            _gates.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
