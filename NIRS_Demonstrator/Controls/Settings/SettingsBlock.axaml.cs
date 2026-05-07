using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.ObjectModel;

namespace NIRS_Demonstrator;

public partial class SettingsBlock : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<string> HeaderProperty =
       AvaloniaProperty.Register<SettingsElement, string>(nameof(Header), defaultValue: string.Empty);

    public string Header
    {
        get { return (string)GetValue(HeaderProperty); }
        set { SetValue(HeaderProperty, value); }
    }

    //public static readonly StyledProperty<string> IconProperty =
    //   AvaloniaProperty.Register<SettingsElement, string>(nameof(Icon), defaultValue: string.Empty);

    //public string Icon
    //{
    //    get { return (string)GetValue(IconProperty); }
    //    set { SetValue(IconProperty, value); }
    //}

    private Collection<SettingsElement> _Settings;

    public Collection<SettingsElement> Settings
    {
        get { return _Settings; }
        set { _Settings = value; }
    }



    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change == null) return;

        if (change.Property == HeaderProperty)
        {
            HeaderText.Text = (string)change.NewValue;
        }
        //else if (change.Property == IconProperty)
        //{
        //    IconControl.Source = new Bitmap(AssetLoader.Open(new Uri("avares://GreekWords/Assets/" + (string)change.NewValue))); //
        //}


        base.OnPropertyChanged(change);

    }

    #endregion

    #region Public Events

    public event EventHandler<int> SettingsSelectionChanged;

    #endregion


    public SettingsBlock()
    {
        InitializeComponent();

        Settings = new Collection<SettingsElement>();
        Loaded += SettingsBlock_Loaded;
        Unloaded += SettingsBlock_Unloaded;
    }

    #region Public Methods

    public void SetSelection(int index)
    {
        if (Settings.Count > 0)
            Settings[index].CheckState = true;
    }

    #endregion

    #region Private Callbacks

    private void SettingsBlock_Loaded(object sender, RoutedEventArgs e)
    {
        int index = 0;
        foreach (SettingsElement el in Settings)
        {
            if (!SettingsList.Children.Contains(el))
            {
                SettingsList.Children.Add(el);
                el.CheckStateChangedChanged += El_CheckStateChangedChanged;
                //if (index == (int)AppConfig.GetInstance().AppLanguage)
                //    el.CheckState = true;
                //index++;
            }
        }
        SettingsList.UpdateLayout();

    }

    private void SettingsBlock_Unloaded(object sender, RoutedEventArgs e)
    {
        foreach (SettingsElement el in Settings)
        {
            el.CheckStateChangedChanged -= El_CheckStateChangedChanged;
            SettingsList.Children.Remove(el);

        }
        Settings.Clear();
    }

    private void El_CheckStateChangedChanged(object sender, bool e)
    {
        int index = 0;
        foreach (SettingsElement el in SettingsList.Children)
            el.CheckState = false;
        foreach (SettingsElement el in SettingsList.Children)
        {
            if ((sender as SettingsElement) == el && e)
            {
                SettingsSelectionChanged?.Invoke(this, index);
                el.CheckState = true;
                return;
            }
            index++;
        }
    }

    #endregion
}