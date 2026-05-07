using Avalonia.Controls;
using NIRS_Demonstrator.Core;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class MainMenuItemControlViewModel : ViewModelBase
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        protected UserControl mUserControl;

        #endregion

        #region Private Members

        private bool _PointerEntered;

        #endregion

        #region Public Properties

        public bool PointerEntered
        {
            get => _PointerEntered;
            set
            {
                _PointerEntered = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Public Commands

        #endregion

        #region Public Events

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public MainMenuItemControlViewModel(UserControl userControl)
        {
            mUserControl = userControl;
            //mUserControl.PointerEntered += Control_PointerEntered;
            //mUserControl.PointerExited += Control_PointerExited;

        }



        #endregion

        #region Private Callbacks

        private void Control_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            PointerEntered = false;
        }


        private void Control_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            PointerEntered = true;
        }

        #endregion

        #region Command Methods

        #endregion

        #region Public Methods

        #endregion

        #region Private Methods

        #endregion
    }
}
