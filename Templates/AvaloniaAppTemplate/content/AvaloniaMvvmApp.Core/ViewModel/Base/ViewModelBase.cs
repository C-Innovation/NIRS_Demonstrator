using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Базовый класс модели представления.
    /// <para>
    /// Реализует <see cref="INotifyPropertyChanged"/> без привязки к сторонним
    /// MVVM-фреймворкам: единственная зависимость — BCL.
    /// </para>
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        private bool _isDisposed;

        /// <summary>
        /// Возникает при изменении любого свойства модели представления.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Уведомляет об изменении свойства. Имя подставляется компилятором,
        /// поэтому из тела сеттера вызывается без аргументов.
        /// </summary>
        public void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Присваивает полю новое значение и уведомляет об изменении,
        /// если значение действительно изменилось.
        /// </summary>
        /// <returns><c>true</c>, если значение изменилось.</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(name);
            return true;
        }

        /// <summary>
        /// Присваивает полю новое значение, уведомляет об изменении и
        /// дополнительно уведомляет о зависимых вычисляемых свойствах.
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, string[] alsoNotify, [CallerMemberName] string? name = null)
        {
            if (!SetProperty(ref field, value, name))
                return false;

            if (alsoNotify != null)
            {
                foreach (string dependent in alsoNotify)
                    OnPropertyChanged(dependent);
            }

            return true;
        }

        /// <summary>
        /// Освобождает ресурсы модели представления.
        /// Переопределяйте <see cref="Dispose(bool)"/>, а не этот метод.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Точка расширения для освобождения ресурсов наследниками.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
