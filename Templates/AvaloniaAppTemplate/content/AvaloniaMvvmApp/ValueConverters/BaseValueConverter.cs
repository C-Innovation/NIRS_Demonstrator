using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using System;
using System.Globalization;

namespace AvaloniaMvvmApp.ValueConverters
{
    /// <summary>
    /// Базовый преобразователь значений, который можно применять в XAML напрямую,
    /// без объявления в ресурсах.
    /// <para>
    /// Благодаря наследованию от <see cref="MarkupExtension"/> запись выглядит так:
    /// <c>{Binding IsBusy, Converter={local:InverseBooleanConverter}}</c>.
    /// Экземпляр создаётся один на тип и переиспользуется.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Тип самого преобразователя.</typeparam>
    public abstract class BaseValueConverter<T> : MarkupExtension, IValueConverter
        where T : class, new()
    {
        private static T? _converter;

        /// <inheritdoc />
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _converter ??= new T();
        }

        /// <summary>
        /// Преобразует значение источника в значение для элемента управления.
        /// </summary>
        public abstract object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

        /// <summary>
        /// Преобразует значение элемента управления обратно в значение источника.
        /// </summary>
        public abstract object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);
    }
}
