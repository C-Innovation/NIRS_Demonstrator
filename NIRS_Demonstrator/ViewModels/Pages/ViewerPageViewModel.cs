using NIRS_Demonstrator.Core;
using System.Windows.Input;

namespace NIRS_Demonstrator.ViewModels
{
    /// <summary>
    /// 
    /// </summary>
    public class ViewerPageViewModel : ViewModelBase
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members

        #endregion

        #region Public Properties

        #endregion

        #region Public Commands
        public ICommand ReturnToMainCommand { get; set; }
        #endregion

        #region Public Events

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public ViewerPageViewModel()
        {
            ReturnToMainCommand = new RelayCommand(ReturnToMainCommandAction);

            AppConfig.GetInstance().RegisterDisposableObject(this);
        }

        #endregion

        #region Private Callbacks

        #endregion

        #region Command Methods

        private void ReturnToMainCommandAction()
        {
            IoC.Application.GoToPage(ApplicationPage.Main);
        }

        #endregion



        #region Public Methods

        public override void Dispose()
        {
            base.Dispose();
        }
        
        #endregion

        #region Private Methods

        #endregion
    }
}
