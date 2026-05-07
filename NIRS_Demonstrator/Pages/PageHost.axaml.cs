using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using NIRS_Demonstrator.Core;
using NIRS_Demonstrator.ViewModels;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NIRS_Demonstrator;

public partial class PageHost : UserControl
{
    #region Dependency (Styled or Direct) Properties

    public static readonly StyledProperty<ApplicationPage> CurrentPageProperty =
    AvaloniaProperty.Register<PageHost, ApplicationPage>(nameof(CurrentPage), defaultValue: default(ApplicationPage));

    public ApplicationPage CurrentPage
    {
        get => GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public static readonly StyledProperty<ViewModelBase> CurrentPageViewModelProperty =
    AvaloniaProperty.Register<PageHost, ViewModelBase>(nameof(CurrentPageViewModel), defaultValue: null);

    public ViewModelBase CurrentPageViewModel
    {
        get => GetValue(CurrentPageViewModelProperty);
        set => SetValue(CurrentPageViewModelProperty, value);
    }

    #endregion

    #region Private Members

    private enum ActivePage
    {
        Page1 = 0,
        Page2 = 1,
    }

    private enum AnimationDirection
    {
        Forward = 0,
        Backward = 1,
    }

    private PageSlide PageSlideAnimation;
    private Carousel _carousel;


    private PageSlide _PageSlide;
    //private Rotate3DTransition _PageSlide;
    private PageSlide _NewPageSlide;

    private ContentControl _Page1;
    private ContentControl _Page2;

    private ActivePage _ActivePage; 

    private bool isNewActive = false;
    #endregion

    #region Constructor
    public PageHost()
    {
        InitializeComponent();
        
        //_carousel.
        
        _carousel = this.Get<Carousel>("carousel");
        
        _Page1 = new ContentControl();
        _Page2 = new ContentControl();
        _Page1.Content = IoC.Application.CurrentPage.ToBasePage(new MainPageViewModel());
        _ActivePage = ActivePage.Page1;
        _carousel.Items.Add(_Page1);
        isNewActive = true;
        //_carousel.Next();
    }

    #endregion
    #region Property Changed Events

    protected override async void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CurrentPageProperty)
        {
            // Get properties
            var currentPage = (ApplicationPage)GetValue(CurrentPageProperty);
            var currentPageViewModel = GetValue(CurrentPageViewModelProperty);

            
            
           
            
            // Get the frames
            ContentControl oldPageFrame;
            if (_ActivePage == ActivePage.Page1)
                oldPageFrame = _Page1;
            else
                oldPageFrame = _Page2;


            if (oldPageFrame.Content is BasePage page &&
                page.ToApplicationPage() == currentPage)
            {
                //
                page.ViewModelObject = currentPageViewModel;

                return;
            }

            // Setup animation
            AnimationDirection direction = AnimationDirection.Forward;
            if ((ApplicationPage)change.OldValue == ApplicationPage.Settings || currentPage == ApplicationPage.Settings)
            {
                _PageSlide = new PageSlide(TimeSpan.FromSeconds(0.25), PageSlide.SlideAxis.Vertical);
                direction = (currentPage == ApplicationPage.Main) ? AnimationDirection.Forward : AnimationDirection.Backward;
            }
            else
            {
                _PageSlide = new PageSlide(TimeSpan.FromSeconds(0.25), PageSlide.SlideAxis.Horizontal);
                direction = (currentPage == ApplicationPage.Main) ? AnimationDirection.Backward : AnimationDirection.Forward;
            }
            _PageSlide.SlideInEasing = new QuadraticEaseIn();
            _PageSlide.SlideOutEasing = new QuadraticEaseOut();
            _carousel.PageTransition = _PageSlide;
            

            var newPageContent = currentPage.ToBasePage(currentPageViewModel);
            switch (_ActivePage)
            {
                case ActivePage.Page1:
                    _Page2 = new ContentControl();
                    _Page2.Content = newPageContent;
                    if(direction == AnimationDirection.Forward)
                    {
                        _carousel.Items.Add(_Page2);
                        _carousel.Next();
                        await Task.Delay(500);
                    }
                    else
                    {
                        _carousel.Items.Insert(0, _Page2);
                        _carousel.SelectedIndex = 1;
                        _carousel.Previous();
                        await Task.Delay(500);
                    }
                    _carousel.Items.Remove(_Page1);
                    _Page1.Content = null;
                    _Page1 = null;
                    _ActivePage = ActivePage.Page2;
                    break; 

                case ActivePage.Page2:
                    _Page1 = new ContentControl();
                    _Page1.Content = newPageContent;
                    if (direction == AnimationDirection.Forward)
                    {
                        _carousel.Items.Add(_Page1);
                        _carousel.Next();
                        await Task.Delay(500);
                    }
                    else
                    {
                        _carousel.Items.Insert(0, _Page1);
                        _carousel.SelectedIndex = 1;
                        _carousel.Previous();
                        await Task.Delay(500);
                    }
                    _carousel.Items.Remove(_Page2);
                    _Page2.Content = null;
                    _Page2 = null;
                    _ActivePage = ActivePage.Page1;
                    break;
                
                default:
                    if (Debugger.IsAttached) Debugger.Break();
                    throw new Exception("_ActivePage is unknown!!!");
            }

            //if (isNewActive)
            //{
            //    var oldPageContent = newPageFrame.Content;
            //    var newPageContent = currentPage.ToBasePage(currentPageViewModel);
            //    oldPageFrame.Content = newPageContent;
            //    CancellationToken token = new CancellationToken();
            //    await _PageSlide.Start(newPageFrame, oldPageFrame, false, token);
            //    newPageFrame.IsVisible = false;
            //    isNewActive = false;
            //    if (oldPageContent is BasePage oldPage)
            //    {
            //        //
            //        oldPage.ShouldAnimateOut = true;
            //        newPageFrame.Content = null;
            //    }
            //}
            //else
            //{
            //    var oldPageContent = oldPageFrame.Content;
            //    var newPageContent = currentPage.ToBasePage(currentPageViewModel);
            //    newPageFrame.Content = newPageContent;
            //    CancellationToken token = new CancellationToken();
            //    await _PageSlide.Start(oldPageFrame, newPageFrame, false, token);
            //    oldPageFrame.IsVisible = false;
            //    //await Task.Delay(250);
            //    if (oldPageContent is BasePage oldPage)
            //    {
            //        //
            //        oldPage.ShouldAnimateOut = true;
            //        oldPageFrame.Content = null;
            //    }

            //    isNewActive = true;
            //}
            /*
            //
            if (newPageFrame.Content is BasePage page &&
                page.ToApplicationPage() == currentPage)
            {
                //
                page.ViewModelObject = currentPageViewModel;

                return;
            }

            //
            var oldPageContent = newPageFrame.Content;

            //
            newPageFrame.Content = null;

            //
            oldPageFrame.Content = oldPageContent;

            //
            var newPageContent = currentPage.ToBasePage(currentPageViewModel);
            newPageFrame.Content = newPageContent;

            CancellationToken token = new CancellationToken();
            await _PageSlide.Start(oldPageFrame, newPageFrame, false, token);
            if (oldPageContent is BasePage oldPage)
            {
                //
                oldPage.ShouldAnimateOut = true;
                oldPageFrame.Content = null;
            }
            */
            //NewPage = null;
            return ;
        }

        
    }

    #endregion
}