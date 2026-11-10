using System;
using System.Collections.Generic;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Модель представления приложения: хранит текущую страницу и историю переходов.
    /// Единственная точка навигации — <see cref="GoToPage"/>.
    /// </summary>
    public class ApplicationViewModel : ViewModelBase
    {
        /// <summary>
        /// История переходов для <see cref="GoBack"/>.
        /// </summary>
        private readonly Stack<(ApplicationPage Page, ViewModelBase? ViewModel)> _history
            = new Stack<(ApplicationPage, ViewModelBase?)>();

        private ApplicationPage _currentPage = ApplicationPage.Main;

        /// <summary>
        /// Текущая страница приложения.
        /// </summary>
        public ApplicationPage CurrentPage
        {
            get => _currentPage;
            private set => SetProperty(ref _currentPage, value);
        }

        private ViewModelBase? _currentPageViewModel;

        /// <summary>
        /// Модель представления, которую получит страница при переходе.
        /// <c>null</c> означает «создай модель по умолчанию».
        /// </summary>
        public ViewModelBase? CurrentPageViewModel
        {
            get => _currentPageViewModel;
            set => SetProperty(ref _currentPageViewModel, value);
        }

        /// <summary>
        /// Есть ли куда возвращаться.
        /// </summary>
        public bool CanGoBack => _history.Count > 0;

        /// <summary>
        /// Возникает после смены страницы.
        /// </summary>
        public event EventHandler<ApplicationPage>? PageChanged;

        /// <summary>
        /// Переходит на указанную страницу.
        /// </summary>
        /// <param name="page">Целевая страница.</param>
        /// <param name="viewModel">
        /// Модель представления страницы. Если <c>null</c>, страница создаст свою.
        /// </param>
        /// <param name="addToHistory">
        /// Запоминать ли текущую страницу для <see cref="GoBack"/>.
        /// </param>
        public void GoToPage(ApplicationPage page, ViewModelBase? viewModel = null, bool addToHistory = true)
        {
            if (addToHistory)
                _history.Push((CurrentPage, CurrentPageViewModel));

            // Порядок важен: модель представления должна быть выставлена
            // до смены страницы, иначе страница откроется с прежней моделью.
            CurrentPageViewModel = viewModel;
            CurrentPage = page;

            OnPropertyChanged(nameof(CanGoBack));
            PageChanged?.Invoke(this, page);
        }

        /// <summary>
        /// Возвращается на предыдущую страницу. Если истории нет — ничего не делает.
        /// </summary>
        /// <returns><c>true</c>, если переход состоялся.</returns>
        public bool GoBack()
        {
            if (_history.Count == 0)
                return false;

            var previous = _history.Pop();
            GoToPage(previous.Page, previous.ViewModel, addToHistory: false);
            return true;
        }

        /// <summary>
        /// Очищает историю переходов.
        /// </summary>
        public void ClearHistory()
        {
            _history.Clear();
            OnPropertyChanged(nameof(CanGoBack));
        }
    }
}
