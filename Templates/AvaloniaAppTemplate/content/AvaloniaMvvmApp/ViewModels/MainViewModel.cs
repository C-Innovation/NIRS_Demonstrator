using AvaloniaMvvmApp.Core;

namespace AvaloniaMvvmApp.ViewModels
{
    /// <summary>
    /// Модель представления корневого окна (или корневого представления
    /// на однооконных платформах).
    /// <para>
    /// Содержимым окна управляет <c>PageHost</c> через
    /// <see cref="ApplicationViewModel"/>, поэтому здесь остаётся только то,
    /// что относится к окну целиком: заголовок, строка состояния и подобное.
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Навигация и текущая страница.
        /// </summary>
        public ApplicationViewModel Application => IoC.Application;
    }
}
