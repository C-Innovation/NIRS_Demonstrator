using Avalonia.Controls;
using NIRS_Demonstrator.ViewModels;
using System;
using System.Threading.Tasks;

namespace NIRS_Demonstrator;

/// <summary>
/// Представление страницы графиков. Вся логика работы с датчиками и обработки
/// сигналов вынесена в <see cref="ChartsPageViewModel"/>; здесь остаётся только
/// то, что нельзя выразить привязкой: раскладка точек по сериям Chart2D,
/// синхронизация горизонтальной прокрутки и открытие окна настроек.
/// </summary>
public partial class ChartsPage : BasePage<ChartsPageViewModel>
{
    private Series[] _Series740;
    private Series[] _Series850;

    private ChartSettingsWindow _ChartSettingsWindow;

    public ChartsPage() : base()
    {
        InitializeComponent();
        InitializeLocal();
    }

    public ChartsPage(ChartsPageViewModel specificViewModel = null) : base(specificViewModel)
    {
        InitializeComponent();
        InitializeLocal();
    }

    #region Private Methods

    private void InitializeLocal()
    {
        // Порядок обязан совпадать с индексами NirsChartBatch.
        _Series740 = new[]
        {
            Nirs1Series740_1, Nirs1Series740_2, Nirs1Series740_3,
            Nirs1Series740_4, Nirs1Series740_TotalVal, Nirs1Series740_AiVal
        };

        _Series850 = new[]
        {
            Nirs1Series850_1, Nirs1Series850_2, Nirs1Series850_3,
            Nirs1Series850_4, Nirs1Series850_TotalVal, Nirs1Series850_AiVal
        };

        Nirs1Chart740.SetAxisXSize(2);
        Nirs1Chart850.SetAxisXSize(2);

        Nirs1Chart740.OnHorizontalScrollValueChanged += Nirs1Chart740_OnHorizontalScrollValueChanged;
        Nirs1Chart850.OnHorizontalScrollValueChanged += Nirs1Chart850_OnHorizontalScrollValueChanged;

        Nirs1Chart740.HorizontalScroll.Value = Nirs1Chart740.HorizontalScroll.Maximum;
        Nirs1Chart850.HorizontalScroll.Value = Nirs1Chart850.HorizontalScroll.Maximum;

        ViewModel.PointsBatchReady += ViewModel_PointsBatchReady;
        ViewModel.SettingsRequested += ViewModel_SettingsRequested;

        Unloaded += ChartsPage_Unloaded;
    }

    private async void AppendBatchAsync(NirsChartBatch batch)
    {
        for (int i = 0; i < NirsChartBatch.SeriesCount; i++)
        {
            await _Series740[i].AddPointsRangeAsync(batch.Series740[i]);
            await _Series850[i].AddPointsRangeAsync(batch.Series850[i]);
        }
    }

    #endregion

    #region Private Callbacks

    private void ViewModel_PointsBatchReady(object sender, NirsChartBatch batch)
    {
        AppendBatchAsync(batch);
    }

    private void ViewModel_SettingsRequested(object sender, EventArgs e)
    {
        if (_ChartSettingsWindow == null || !_ChartSettingsWindow.IsLoaded)
        {
            _ChartSettingsWindow = new ChartSettingsWindow(this);
            _ChartSettingsWindow.Show();
        }
    }

    private void ChartsPage_Unloaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Без отписки страница осталась бы достижимой из ViewModel.
        ViewModel.PointsBatchReady -= ViewModel_PointsBatchReady;
        ViewModel.SettingsRequested -= ViewModel_SettingsRequested;
    }

    private void Nirs1Chart850_OnHorizontalScrollValueChanged(object sender, double e)
    {
        Nirs1Chart740.HorizontalScroll.Value = e;
    }

    private void Nirs1Chart740_OnHorizontalScrollValueChanged(object sender, double e)
    {
        Nirs1Chart850.HorizontalScroll.Value = e;
    }

    #endregion
}
