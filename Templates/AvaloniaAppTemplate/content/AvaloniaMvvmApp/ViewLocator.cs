using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System;
using System.Diagnostics.CodeAnalysis;
using AvaloniaMvvmApp.Core;

namespace AvaloniaMvvmApp;

/// <summary>
/// Подбирает представление по модели представления по соглашению об именах:
/// <c>SomeViewModel</c> → <c>SomeView</c>.
/// <para>
/// Используется там, где модель представления подставляется как содержимое
/// (элементы списка, <c>ContentControl</c>). Страницы создаются не здесь,
/// а через <c>PageRegistry</c>: там нужны явные фабрики и метаданные перехода.
/// </para>
/// </summary>
[RequiresUnreferencedCode(
    "ViewLocator ищет тип через отражение; при обрезке сборки такой тип может быть удалён.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    /// <inheritdoc />
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        string? viewModelName = param.GetType().FullName;
        if (viewModelName is null)
            return null;

        string viewName = viewModelName.Replace("ViewModel", "View", StringComparison.Ordinal);
        Type? viewType = Type.GetType(viewName);

        if (viewType is null)
            return new TextBlock { Text = "Представление не найдено: " + viewName };

        return (Control?)Activator.CreateInstance(viewType);
    }

    /// <inheritdoc />
    public bool Match(object? data) => data is ViewModelBase;
}
