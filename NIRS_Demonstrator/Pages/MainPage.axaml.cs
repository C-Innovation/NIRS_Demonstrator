using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using NIRS_Demonstrator.ViewModels;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NIRS_Demonstrator;

public partial class MainPage : BasePage<MainPageViewModel>
{

    public MainPage() : base()
    {
        InitializeComponent();

    }
        

    /// <summary>
    /// Extended Constructor
    /// </summary>
    /// <param name="specificTesterViewModel"></param>
    public MainPage(MainPageViewModel specificTesterViewModel = null) : base(specificTesterViewModel)
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }


    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        
    }

}