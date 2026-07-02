using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
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

    }

    #endregion

    #region Private Callbacks

    private async void OpenResearchButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        string path = await OpenFileAsync();
        if (string.IsNullOrEmpty(path))
            return;
        path = path.Substring(8);
        NirsData = CsvParser.ParseCsvColumns(path);

        if (NirsData != null)
        {
            await UpdateCharts();
        }
    }

    private void ViewRawDataButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
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