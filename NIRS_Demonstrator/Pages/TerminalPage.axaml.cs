using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using NIRS_Demonstrator.ViewModels;

namespace NIRS_Demonstrator;

public partial class TerminalPage : BasePage<TerminalPageViewModel>
{
    public TerminalPage() : base()
    {
        InitializeComponent();
    }
    public TerminalPage(TerminalPageViewModel spespecificTesterViewModel = null) : base(spespecificTesterViewModel)
    {
        InitializeComponent();
    }

    private void TextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        TerminalTextBox.CaretIndex = TerminalTextBox.Text?.Length ?? 0;
        //if(TerminalTextBox.Text.Length > 0)
        //{
        //    TerminalTextBox.SelectionStart = TerminalTextBox.Text.Length - 3;
        //    TerminalTextBox.SelectionEnd = TerminalTextBox.Text.Length;
        //    TerminalTextBox.Select = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0x00, 0x00));
        //}
        
    }
}