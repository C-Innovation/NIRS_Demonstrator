using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using NIRS_Demonstrator.Helpers.AI;
using NIRS_Demonstrator.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace NIRS_Demonstrator;

public partial class ViewerPage : BasePage<ViewerPageViewModel>, IDisposable
{
    public List<List<double>> NirsData;
    private double _AxisXSize = 4000;
    private CsvEditWindow _CsvEditWindow;
    private string _OpenedFile;

    private bool _IsDisposed;

    public ViewerPage() : base()
    {
        InitializeComponent();
        InitializeLocal();
    }
    public ViewerPage(ViewerPageViewModel spespecificTesterViewModel = null) : base(spespecificTesterViewModel)
    {
        InitializeComponent();
        InitializeLocal();
    }

    #region Public Methods

    public async Task UpdateCharts()
    {
        Nirs1Chart740.SetAxisXSizeForViewer(_AxisXSize, NirsData[0].Count);
        Nirs1Chart850.SetAxisXSizeForViewer(_AxisXSize, NirsData[0].Count);

        for (int i = 0; i < NirsData.Count; i++)
            await UpdateSeries(i);

    }

    public async Task UpdateSeries(int index)
    {
        if (index > NirsData.Count)
            return;

        Point[] points = new Point[NirsData[index].Count];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new Point(i, NirsData[index][i]);
        }
        switch (index)
        {
            case 0:
                await Nirs1Series740_1.ClearPoints();
                await Nirs1Series740_1.AddPointsRangeAsync(points);
                break;
            case 1:
                await Nirs1Series740_2.ClearPoints();
                await Nirs1Series740_2.AddPointsRangeAsync(points);
                break;
            case 2:
                await Nirs1Series740_3.ClearPoints();
                await Nirs1Series740_3.AddPointsRangeAsync(points);
                break;
            case 3:
                await Nirs1Series740_4.ClearPoints();
                await Nirs1Series740_4.AddPointsRangeAsync(points);
                break;
            case 4:
                await Nirs1Series850_1.ClearPoints();
                await Nirs1Series850_1.AddPointsRangeAsync(points);
                break;
            case 5:
                await Nirs1Series850_2.ClearPoints();
                await Nirs1Series850_2.AddPointsRangeAsync(points);
                break;
            case 6:
                await Nirs1Series850_3.ClearPoints();
                await Nirs1Series850_3.AddPointsRangeAsync(points);
                break;
            case 7:
                await Nirs1Series850_4.ClearPoints();
                await Nirs1Series850_4.AddPointsRangeAsync(points);
                break;
            case 8:
                await Nirs1Series740_TotalVal.ClearPoints();
                await Nirs1Series740_TotalVal.AddPointsRangeAsync(points);
                await Nirs1Series850_TotalVal.ClearPoints();
                await Nirs1Series850_TotalVal.AddPointsRangeAsync(points);
                break;
            case 9:
                //throw new NotImplementedException();
                break;
            default: break;
        }

        // Значения выделенных областей берутся из NirsData, поэтому после
        // правки данных их нужно пересобрать.
        ViewModel.RefreshSelectionData(NirsData);
    }
    public void Dispose()
    {
        if (_IsDisposed)
            return;

        _IsDisposed = true;

        Nirs1Chart740.SelectionChanging -= Chart_SelectionChanging;
        Nirs1Chart850.SelectionChanging -= Chart_SelectionChanging;
        Nirs1Chart740.SelectionCompleted -= Chart_SelectionCompleted;
        Nirs1Chart850.SelectionCompleted -= Chart_SelectionCompleted;
        Nirs1Chart740.SelectionRemoveRequested -= Chart_SelectionRemoveRequested;
        Nirs1Chart850.SelectionRemoveRequested -= Chart_SelectionRemoveRequested;

        if (ViewModel != null)
        {
            ViewModel.SelectionCleared -= ViewModel_SelectionCleared;
            ViewModel.SelectionAreaRemoved -= ViewModel_SelectionAreaRemoved;
        }

        if (_CsvEditWindow != null)
            if (_CsvEditWindow.IsLoaded)
                _CsvEditWindow.Close();
    }

    #endregion

    #region Private Callbacks

    private async void OpenResearchButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _OpenedFile = await OpenFileAsync();
        if (string.IsNullOrEmpty(_OpenedFile))
            return;
        _OpenedFile = _OpenedFile.Substring(8);

        // Выделения указывают на строки предыдущего исследования.
        ViewModel.ClearSelectionAreas();

        NirsData = CsvParser.ParseCsvColumns(_OpenedFile);

        if (NirsData != null)
        {
            await UpdateCharts();
        }
    }

    private void ViewRawDataButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_OpenedFile))
            return;

        if (_CsvEditWindow != null)
        {
            if (!_CsvEditWindow.IsLoaded)
            {
                _CsvEditWindow = new CsvEditWindow(this);
                _CsvEditWindow.Show();
            }
        }
        else
        {
            _CsvEditWindow = new CsvEditWindow(this);
            _CsvEditWindow.Show();
        }
    }

    private async void SaveResarchButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_OpenedFile))
            return;

        File.Delete(_OpenedFile);
        using (ReportsStreamerCsv streamerCsv = new ReportsStreamerCsv(_OpenedFile))
        {
            for (int i = 0; i < NirsData[0].Count; i++)
            {
                List<double> vals = new List<double>();
                for (int j = 0; j < NirsData.Count; j++)
                    vals.Add(NirsData[j][i]);

                streamerCsv.Write(vals);
            }
        }

    }

    private async void RunAiButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if(string.IsNullOrEmpty(_OpenedFile))
            return;
        await Nirs1Series740_AiVal.ClearPoints();
        Point[] points = new Point[NirsData[0].Count];
        int pcnt = 0;
        string modelPath = Path.Combine(AppConfig.GetInstance().AiDirectoryPath, "gru_pre.onnx");

        using (var predictor = new NirsOnnxModel(modelPath))
        {
            for (int i = 0; i < NirsData[0].Count; i++)
            {
                float[] input = new float[8];
                for (int j = 0; j < NirsData.Count - 2; j++)
                {
                    input[j] = (float)(NirsData[j][i] / 5.0f);
                }
                float output = predictor.Predict(input);
                points[pcnt] = new Point(pcnt, output * 5.0);
                pcnt++;
            }
        }
        await Nirs1Series740_AiVal.AddPointsRangeAsync(points);

        await Nirs1Series850_AiVal.ClearPoints();
        points = new Point[NirsData[0].Count];
        pcnt = 0;
        //_NirsOnnxModel1 = new NirsOnnxModel(Path.Combine(AppConfig.GetInstance().AiDirectoryPath, "gru_pre.onnx"));
        modelPath = Path.Combine(AppConfig.GetInstance().AiDirectoryPath, "gru_pre.onnx");

        using (var predictor = new NirsOnnxModel(modelPath))
        {
            for (int i = 0; i < NirsData[0].Count; i ++)
            {
                float[] input = new float[8];
                for (int j = 0; j < NirsData.Count - 2; j++)
                {
                    input[j] = (float)(NirsData[j][i] / 5.0f);
                }
                float output = predictor.Predict(input);
                points[pcnt] = new Point(pcnt, output * 5.0);
                pcnt++;
            }
        }
        await Nirs1Series850_AiVal.AddPointsRangeAsync(points);
    }

    private void Nirs1Chart850_OnHorizontalScrollValueChanged(object? sender, double e)
    {
        Nirs1Chart740.HorizontalScroll.Value = e;
    }

    private void Nirs1Chart740_OnHorizontalScrollValueChanged(object? sender, double e)
    {
        Nirs1Chart850.HorizontalScroll.Value = e;
    }

    /// <summary>
    /// Пока правая кнопка удерживается, оба графика показывают одну и ту же
    /// протягиваемую область — независимо от того, на каком из них начали.
    /// </summary>
    private void Chart_SelectionChanging(object? sender, ChartSelectionRangeEventArgs e)
    {
        Nirs1Chart740.ShowPendingSelection(e.StartValue, e.StopValue);
        Nirs1Chart850.ShowPendingSelection(e.StartValue, e.StopValue);
    }

    private void Chart_SelectionCompleted(object? sender, ChartSelectionRangeEventArgs e)
    {
        Nirs1Chart740.HidePendingSelection();
        Nirs1Chart850.HidePendingSelection();

        // Без загруженного исследования выделять нечего.
        if (NirsData == null || NirsData.Count == 0)
            return;

        ChartSelectionArea area = new ChartSelectionArea(e.StartValue, e.StopValue, NirsData);

        if (area.IsEmpty)
            return;

        Nirs1Chart740.AddSelection(e.StartValue, e.StopValue);
        Nirs1Chart850.AddSelection(e.StartValue, e.StopValue);

        ViewModel.AddSelectionArea(area);
    }

    private void Chart_SelectionRemoveRequested(object? sender, int index)
    {
        ViewModel.RemoveSelectionArea(index);
    }

    private void ViewModel_SelectionCleared(object? sender, EventArgs e)
    {
        Nirs1Chart740.ClearSelections();
        Nirs1Chart850.ClearSelections();
    }

    private void ViewModel_SelectionAreaRemoved(object? sender, int index)
    {
        Nirs1Chart740.RemoveSelectionAt(index);
        Nirs1Chart850.RemoveSelectionAt(index);
    }

    #endregion


    #region Private Methods

    private void InitializeLocal()
    {
        Nirs1Chart740.SetAxisXSize(10000);
        Nirs1Chart850.SetAxisXSize(10000);

        Nirs1Chart740.HorizontalScroll.Value = 0;

        Nirs1Chart740.OnHorizontalScrollValueChanged += Nirs1Chart740_OnHorizontalScrollValueChanged;
        Nirs1Chart850.OnHorizontalScrollValueChanged += Nirs1Chart850_OnHorizontalScrollValueChanged;

        // Выделение, начатое на любом из графиков, отображается на обоих.
        Nirs1Chart740.SelectionChanging += Chart_SelectionChanging;
        Nirs1Chart850.SelectionChanging += Chart_SelectionChanging;
        Nirs1Chart740.SelectionCompleted += Chart_SelectionCompleted;
        Nirs1Chart850.SelectionCompleted += Chart_SelectionCompleted;
        Nirs1Chart740.SelectionRemoveRequested += Chart_SelectionRemoveRequested;
        Nirs1Chart850.SelectionRemoveRequested += Chart_SelectionRemoveRequested;

        ViewModel.SelectionCleared += ViewModel_SelectionCleared;
        ViewModel.SelectionAreaRemoved += ViewModel_SelectionAreaRemoved;

        AppConfig.GetInstance().RegisterDisposableObject(this);
    }

    private async Task<string> OpenFileAsync()
    {
        // 1. Get the TopLevel window reference from the current control
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return string.Empty;

        // 2. Configure dialog options
        var options = new FilePickerOpenOptions
        {
            Title = "Select .csv research file",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("CSV Research Files (*.csv)")
                {
                    Patterns = new[] { "*.csv" }
                },

            }
        };

        // 3. Trigger the asynchronous file picker
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

        // 4. Process the selected file
        if (files.Count > 0)
        {
            // Open a managed stream to read the file safely across platforms
            //await using var stream = await files[0].OpenReadAsync();
            //using var reader = new StreamReader(stream);
            //string fileContent = await reader.ReadToEndAsync();
            return files[0].Path.AbsoluteUri;
        }
        return string.Empty;
    }

    





    #endregion


}