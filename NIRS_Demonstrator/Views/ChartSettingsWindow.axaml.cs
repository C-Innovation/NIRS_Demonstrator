using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NIRS_Demonstrator.ViewModels;

namespace NIRS_Demonstrator;

public partial class ChartSettingsWindow : Window
{

    public ChartSettingsWindow()
    {
        InitializeComponent();
        this.DataContext = new ChartSettingsWindowViewModel(new ChartsPage(), this);
    }

    public ChartSettingsWindow(ChartsPage chartsPage)
    {
        InitializeComponent();
        this.DataContext = new ChartSettingsWindowViewModel(chartsPage, this);
    }
}