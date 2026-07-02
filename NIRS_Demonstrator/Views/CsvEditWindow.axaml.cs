using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NIRS_Demonstrator.ViewModels;

namespace NIRS_Demonstrator;

public partial class CsvEditWindow : Window
{
    
    public CsvEditWindow()
    {
        InitializeComponent();
    }

    public CsvEditWindow(ViewerPage viewerPage)
    {
        InitializeComponent();
        DataContext = new CsvEditWindowViewModel(viewerPage, this);
        
    }
}