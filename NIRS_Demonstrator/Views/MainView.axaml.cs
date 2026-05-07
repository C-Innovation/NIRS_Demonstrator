using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace NIRS_Demonstrator.Views;

public partial class MainView : UserControl, IDisposable
{
    public MainView()
    {
        InitializeComponent();
        SizeChanged += MainView_SizeChanged;

    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        //var insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;

        //if (insetsManager != null)
        //{
        //    insetsManager.DisplayEdgeToEdge = true;
        //    insetsManager.IsSystemBarVisible = false;
        //}

        AppConfig.GetInstance().NotifyMainViewLoaded(this);

        AppConfig.GetInstance().RegisterDisposableObject(this);
    }

    private void MainView_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var size = e.NewSize;
        AppConfig.GetInstance().NotifyMainViewSizeChanged(this, e);
    }

    public async void Dispose()
    {
        
    }
}