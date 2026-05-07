namespace NIRS_Demonstrator.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class ApplicationViewModel : ViewModelBase
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members

        #endregion

        #region Public Properties

        /// <summary>
        /// Текущая страница приложения
        /// </summary>
        public ApplicationPage CurrentPage { get; private set; } = ApplicationPage.Main;

        /// <summary>
        /// The view model to use for the current page when GoToPage is called
        /// </summary>
        public ViewModelBase CurrentPageViewModel { get; set; }

        #endregion

        #region Public Commands

        #endregion

        #region Public Events

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApplicationViewModel()
        {

        }

        #endregion

        #region Private Callbacks

        #endregion

        #region Command Methods

        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="viewModel"></param>
        public void GoToPage(ApplicationPage page, ViewModelBase viewModel = null)
        {
            //
            //SettingsMenuVisible = false;

            //
            CurrentPageViewModel = viewModel;
            OnPropertyChanged(nameof(CurrentPageViewModel));


            //
            CurrentPage = page;

            //
            OnPropertyChanged(nameof(CurrentPage));

            //
            //SideMenuVisible = page == ApplicationPage.Chat;

        }
        #endregion

        #region Private Methods

        #endregion
    }
}
