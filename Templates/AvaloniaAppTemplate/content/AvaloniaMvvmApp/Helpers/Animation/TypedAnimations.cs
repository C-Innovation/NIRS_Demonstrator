using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaMvvmApp.Helpers.Animation
{
    /// <summary>
    /// Анимация числовых свойств: прозрачности, ширины, высоты, угла поворота.
    /// </summary>
    public class DoubleAnimation : PropertyAnimator
    {
        /// <summary>
        /// Плавно изменяет числовое свойство.
        /// </summary>
        public Task RunAsync(double? valueStart,
                             double? valueStop,
                             AvaloniaProperty property,
                             Control control,
                             double durationMs = 300,
                             Easing? easing = null,
                             PlaybackDirection direction = PlaybackDirection.Normal,
                             CancellationToken cancellationToken = default)
        {
            return RunAsync(property, control, valueStart, valueStop, durationMs, easing, direction, cancellationToken);
        }
    }

    /// <summary>
    /// Анимация кистей: фона, обводки, цвета текста.
    /// </summary>
    public class BrushAnimation : PropertyAnimator
    {
        /// <summary>
        /// Плавно изменяет кисть.
        /// </summary>
        public Task RunAsync(IBrush? brushStart,
                             IBrush? brushStop,
                             AvaloniaProperty property,
                             Control control,
                             double durationMs = 300,
                             Easing? easing = null,
                             PlaybackDirection direction = PlaybackDirection.Normal,
                             CancellationToken cancellationToken = default)
        {
            return RunAsync(property, control, brushStart, brushStop, durationMs, easing, direction, cancellationToken);
        }
    }

    /// <summary>
    /// Анимация отступов: <c>Margin</c>, <c>Padding</c>, <c>BorderThickness</c>.
    /// </summary>
    public class ThicknessAnimation : PropertyAnimator
    {
        /// <summary>
        /// Плавно изменяет отступы.
        /// </summary>
        public Task RunAsync(Thickness thicknessStart,
                             Thickness thicknessStop,
                             AvaloniaProperty property,
                             Control control,
                             double durationMs = 300,
                             Easing? easing = null,
                             PlaybackDirection direction = PlaybackDirection.Normal,
                             CancellationToken cancellationToken = default)
        {
            return RunAsync(property, control, thicknessStart, thicknessStop, durationMs, easing, direction, cancellationToken);
        }
    }
}
