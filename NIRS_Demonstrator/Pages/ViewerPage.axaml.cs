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
    private double _AxisXSize = 1000;
    private CsvEditWindow _CsvEditWindow;
    private string _OpenedFile;
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
                throw new NotImplementedException();
                break;
            default: break;
        }
    }
    public void Dispose()
    {
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
        Point[] points = new Point[(NirsData[0].Count / 5) * 5];
        int pcnt = 0;
        string modelPath = "D:\\workspace_PyCharm\\NIRS_NeuroNetMult\\model_exports\\model.onnx";
        using (var predictor = new SequenceOnnxPredictor(modelPath))
        {
            for(int i=0; i < NirsData[0].Count - 5; i += 5)
            {
                float[] input = new float[40];
                for (int j = 0; j < 5; j++)
                {
                    for(int k = 0; k < NirsData.Count - 1; k++)
                    {
                        input[8 * j + k] = (float)(NirsData[k][i + j] / 5.0f);
                    }
                }
                float[] output = predictor.Predict(input);
                for(int j = 0;j < output.Length; j++)
                {
                    points[pcnt] = new Point(pcnt, output[j] * 5.0);
                    pcnt++;
                }

            }
        }
        await Nirs1Series740_AiVal.AddPointsRangeAsync(points);

        await Nirs1Series850_AiVal.ClearPoints();
        points = new Point[NirsData[0].Count];
        pcnt = 0;
        modelPath = "D:\\workspace_PyCharm\\NIRS_NeuroNet\\model_exports\\model.onnx";
        using (var predictor = new TestONNX(modelPath))
        {
            for (int i = 0; i < NirsData[0].Count; i ++)
            {
                float[] input = new float[8];
                for (int j = 0; j < NirsData.Count - 1; j++)
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

    #endregion


    #region Private Methods

    private void InitializeLocal()
    {
        Nirs1Chart740.SetAxisXSize(1000);
        Nirs1Chart850.SetAxisXSize(1000);

        Nirs1Chart740.HorizontalScroll.Value = 0;

        Nirs1Chart740.OnHorizontalScrollValueChanged += Nirs1Chart740_OnHorizontalScrollValueChanged;
        Nirs1Chart850.OnHorizontalScrollValueChanged += Nirs1Chart850_OnHorizontalScrollValueChanged;

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