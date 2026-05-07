using Avalonia.Controls;
using Avalonia.Interactivity;
using NIRS_Demonstrator.Core;
using NIRS_Demonstrator.ViewModels;
using System;
using System.Threading.Tasks;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class BasePage : UserControl
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members

        /// <summary>
        /// The View Model associated with this page
        /// </summary>
        private object mViewModel;

        #endregion

        #region Public Properties
        /// <summary>
        /// The animation the play when the page is first loaded
        /// </summary>
        //public PageAnimation PageLoadAnimation { get; set; } = PageAnimation.SlideAndFadeInFromRight;

        /// <summary>
        /// The animation the play when the page is unloaded
        /// </summary>
        //public PageAnimation PageUnloadAnimation { get; set; } = PageAnimation.SlideAndFadeOutToLeft;

        /// <summary>
        /// The time any slide animation takes to complete
        /// </summary>
        public float SlideSeconds { get; set; } = 0.4f;

        /// <summary>
        /// A flag to indicate if this page should animate out on load.
        /// Useful for when we are moving the page to another frame
        /// </summary>
        public bool ShouldAnimateOut { get; set; }

        /// <summary>
        /// The View Model associated with this page
        /// </summary>
        public object ViewModelObject
        {
            get => mViewModel;
            set
            {
                // If nothing has changed, return
                if (mViewModel == value)
                    return;

                // Update the value
                mViewModel = value;

                // Fire the view model changed method
                OnViewModelChanged();


                // Set the data context for this page
                DataContext = mViewModel;
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
        public BasePage()
        {
            // Don't bother animating in design time
            //if (DesignerProperties.GetIsInDesignMode(this))
            //    return;

            // If we are animating in, hide to begin with
            //if (PageLoadAnimation != PageAnimation.None)
            //    Visibility = Visibility.Collapsed;

            // Listen out for the page loading
            Loaded += OnLoadedAsync;
        }

        private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
        {
            // If we are setup to animate out on load
            if (ShouldAnimateOut)
                // Animate out the page
                await AnimateOutAsync();
            // Otherwise...
            else
                // Animate the page in
                await AnimateInAsync();
        }

        #endregion

        #region Private Callbacks

        #endregion

        #region Command Methods

        #endregion

        #region Animation Load / Unload

        /// <summary>
        /// Once the page is loaded, perform any required animation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BasePage_LoadedAsync(object sender, RoutedEventArgs e)
        {
            
        }

        /// <summary>
        /// Animates the page in
        /// </summary>
        /// <returns></returns>
        public async Task AnimateInAsync()
        {
            // Make sure we have something to do
            //if (PageLoadAnimation == PageAnimation.None)
            //    return;

            //switch (PageLoadAnimation)
            //{
            //    case PageAnimation.SlideAndFadeInFromRight:

            //        // Start the animation
            //        await this.SlideAndFadeInAsync(AnimationSlideInDirection.Right, false, SlideSeconds, size: (int)Application.Current.MainWindow.Width);

            //        break;

            //    case PageAnimation.SlideAndFadeInFromLeft:

            //        // Start the animation
            //        await this.SlideAndFadeInAsync(AnimationSlideInDirection.Left, false, SlideSeconds, size: (int)Application.Current.MainWindow.Width);

            //        break;
            //}
        }

        /// <summary>
        /// Animates the page out
        /// </summary>
        /// <returns></returns>
        public async Task AnimateOutAsync()
        {
            // Make sure we have something to do
            //if (PageUnloadAnimation == PageAnimation.None)
            //    return;

            //switch (PageUnloadAnimation)
            //{
            //    case PageAnimation.SlideAndFadeOutToLeft:

            //        // Start the animation
            //        await this.SlideAndFadeOutAsync(AnimationSlideInDirection.Left, SlideSeconds);

            //        break;

            //    case PageAnimation.SlideAndFadeOutToRight:

            //        // Start the animation
            //        await this.SlideAndFadeOutAsync(AnimationSlideInDirection.Right, SlideSeconds);

            //        break;
            //}
        }

        /// <summary>
        /// Fired when the view model changes
        /// </summary>
        protected virtual void OnViewModelChanged()
        {
        }

        #endregion

        #region Public Methods

        #endregion

        #region Private Methods

        #endregion
    }

    /// <summary>
    /// A base page with added ViewModel support
    /// </summary>
    public class BasePage<VM> : BasePage
        where VM : ViewModelBase, new()
    {


        #region Public Properties

        /// <summary>
        /// 
        /// </summary>
        public VM ViewModel
        {
            get => (VM)ViewModelObject;
            set => ViewModelObject = value;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasePage() : base()
        {

            // Create a default view model
            ViewModel = IoC.Get<VM>();
        }


        /// <summary>
        /// Default constructor
        /// </summary>
        public BasePage(VM specificViewModel = null) : base()
        {
            if (specificViewModel != null)
                ViewModel = specificViewModel;
            else
                // Create a default view model
                ViewModel = IoC.Get<VM>();
        }

        #endregion


    }

}
