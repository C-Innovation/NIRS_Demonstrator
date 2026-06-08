using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CInnovation.SignalProcessing.Filters.BiQuad;
using NIRS_Demonstrator.Helpers.AI;
using NIRS_Demonstrator.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NIRS_Demonstrator;

public partial class ChartsPage : BasePage<ChartsPageViewModel>, IDisposable
{

    private int _chart1_cnt = 0;
    private int _chart2_cnt = 0;
    private int _chart3_cnt = 0;
    private int _chart4_cnt = 0;

    private  List<string> CSV_STREAMER_HEADERS = new List<string>()
    {
        "Time",
        "Led740Ch1",
        "Led740Ch2",
        "Led740Ch3",
        "Led740Ch4",
        "Led740Ch1_Flt",
        "Led740Ch2_Flt",
        "Led740Ch3_Flt",
        "Led740Ch4_Flt",
        "Led850Ch1",
        "Led850Ch2",
        "Led850Ch3",
        "Led850Ch4",
        "Led850Ch1_Flt",
        "Led850Ch2_Flt",
        "Led850Ch3_Flt",
        "Led850Ch4_Flt"
    };

    private bool _WriteCsvEn = false;
    private GpioSeviseRpi gpioServiceRpi;

    private Thread _handlePointsThreadNirs1;
    private Thread _handlePointsThreadNirs2;

    private Thread _printPointsThreadNirs1;
    private Thread _printPointsThreadNirs2;

    private bool _handlePoints1ThreadStarted = false;
    private bool _handlePoints2ThreadStarted = false;

    private NirsSensorDevice NirsSensor1;
    private NirsSensorDevice NirsSensor2;

    private Queue<NirsSignalData> NirsSignalQueue1;
    private Queue<NirsSignalData> NirsSignalQueue2;

    ReportsStreamerCsv StreamerCsvNirs1;
    ReportsStreamerCsv StreamerCsvNirs2;

    private double _dTotalVal1 = 0.0;

    public ChartsPage() : base()
    {
        InitializeComponent();
        InitializeLocal();
    }
    public ChartsPage(ChartsPageViewModel spespecificTesterViewModel = null) : base(spespecificTesterViewModel)
    {
        InitializeComponent();
        InitializeLocal();
    }

    public void Dispose()
    {

        Stop();
    }

    private void InitializeLocal()
    {
        Nirs1Chart740.SetAxisXSize(2);
        Nirs1Chart850.SetAxisXSize(2);
        //Nirs2Chart740.SetAxisXSize(1000);
        //Nirs2Chart850.SetAxisXSize(1000);

        _WriteCsvEn = OperatingSystem.IsWindows();

        Nirs1Chart740.HorizontalScroll.Value = Nirs1Chart740.HorizontalScroll.Maximum;
        Nirs1Chart850.HorizontalScroll.Value = Nirs1Chart850.HorizontalScroll.Maximum;

        if (OperatingSystem.IsLinux())
        {

            gpioServiceRpi = new GpioSeviseRpi();
            gpioServiceRpi?.SetGpioState(6, false);
            gpioServiceRpi?.SetGpioState(13, false);
            gpioServiceRpi?.SetGpioState(5, true);
            gpioServiceRpi?.SetGpioState(12, true);


        }

        //Nirs2Chart740.HorizontalScroll.Value = Nirs2Chart740.HorizontalScroll.Maximum;
        //Nirs2Chart850.HorizontalScroll.Value = Nirs2Chart850.HorizontalScroll.Maximum;
        AppConfig.GetInstance().RegisterDisposableObject(this);
        //_handlePointsThreadStarted = true;
        //_handlePointsThread = new Thread(HandlePointsThreadAction);
        //_handlePointsThread.Start();

        
        //FilterSolutions filterSolutions = new FilterSolutions("D:\\FilterSolutions\\Butterwoth_LP_SR100_O5_F3.dat");

        RefreshComPortsList();
    }

    private async void HandlePointsNirs1ThreadAction()
    {
        if (_WriteCsvEn)
        {
            string path = Path.Combine(AppConfig.GetInstance().ReportsDirectoryPath, (DataHelpers.GetCurrentDateTimeStr()));
            string path1 = path + "_Nirs1.csv";
            StreamerCsvNirs1 = new ReportsStreamerCsv(path1);
            //await StreamerCsvNirs1.WriteHeaderAsync(CSV_STREAMER_HEADERS);
        }
        string modelPath = "D:\\workspace_PyCharm\\NIRS_NeuroNet\\model_exports\\model.onnx";
        using (var nn = new TestONNX(modelPath))
        {
            NirsSignalProcessing NirsSignalProcessing1 = new NirsSignalProcessing();
            while (_handlePoints1ThreadStarted)
            {
                List<NirsSensorData> nirsData = NirsSensor1.GetAvailebleData();
                foreach (NirsSensorData data in nirsData)
                {
                    double time = data.TimeMesSec + ((double)data.TimeMesUSec / 1000000.0);
                    if (NirsSensor1.TimeStart == 0)
                        NirsSensor1.TimeStart = time;
                    time -= NirsSensor1.TimeStart;
                    NirsSignalData nirsDataFlt = NirsSignalProcessing1.GetNirsSignalData(data);
                    //nirsDataFlt.TotalVal = GetTrigDetection(NirsSignalProcessing1, nirsDataFlt) ? 5.0 : 0.0;
                    float[] inputs = { (float)nirsDataFlt.Led740Ch1_Flt / 5.0f,
                                        (float)nirsDataFlt.Led740Ch2_Flt / 5.0f,
                                        (float)nirsDataFlt.Led740Ch3_Flt / 5.0f,
                                        (float)nirsDataFlt.Led740Ch4_Flt / 5.0f,
                                        (float)nirsDataFlt.Led850Ch1_Flt / 5.0f,
                                        (float)nirsDataFlt.Led850Ch2_Flt / 5.0f,
                                        (float)nirsDataFlt.Led850Ch3_Flt / 5.0f,
                                        (float)nirsDataFlt.Led850Ch4_Flt / 5.0f};
                    nirsDataFlt.TotalVal = nn.Predict(inputs) * 5.0;
                    lock (NirsSignalQueue1)
                    {
                        NirsSignalQueue1.Enqueue(nirsDataFlt);
                    }
                    List<double> vals = nirsDataFlt.ToFltList();
                    //vals.Insert(0, time);

                    //SlipMidSmartData slipMidSmartData6 = NirsSignalProcessing1.GetSlipMidSmartData(6);
                    //SlipMidSmartData slipMidSmartData7 = NirsSignalProcessing1.GetSlipMidSmartData(7);

                    //if (OperatingSystem.IsLinux())
                    //    gpioServiceRpi?.SetGpioState(13, !slipMidSmartData7.MidCalcEn);

                    //Dispatcher.UIThread.Invoke(() =>
                    //{
                    //    Nirs1ValueText850_1.Text = $"{nirsDataFlt.Led850Ch3_Flt:0.000} V; (MidEN: {(slipMidSmartData6.MidCalcEn ? "TRUE" : "FALSE")})";
                    //    Nirs1ValueText850_2.Text = $"{nirsDataFlt.Led850Ch4_Flt:0.000} V; (MidEN: {(slipMidSmartData7.MidCalcEn ? "TRUE" : "FALSE")})";
                    //});



                    if (_WriteCsvEn)
                    {
                        StreamerCsvNirs1.Write(vals);
                    }
                }
                await Task.Delay(5);
            }
        }

        
        if (_WriteCsvEn)
            StreamerCsvNirs1.Dispose();
    }

    private async void HandlePointsNirs2ThreadAction()
    {
        if (_WriteCsvEn)
        {
            string path = Path.Combine(AppConfig.GetInstance().ReportsDirectoryPath, (DataHelpers.GetCurrentDateTimeStr()));
            string path1 = path + "_Nirs2.csv";
            StreamerCsvNirs2 = new ReportsStreamerCsv(path1);
            await StreamerCsvNirs2.WriteHeaderAsync(CSV_STREAMER_HEADERS);
        }
        NirsSignalProcessing NirsSignalProcessing2 = new NirsSignalProcessing();
        
        while (_handlePoints2ThreadStarted)
        {
            List<NirsSensorData> nirsData = NirsSensor2.GetAvailebleData();
            foreach (NirsSensorData data in nirsData)
            {
                double time = data.TimeMesSec + ((double)data.TimeMesUSec / 1000000.0);
                if (NirsSensor2.TimeStart == 0)
                    NirsSensor2.TimeStart = time;
                time -= NirsSensor2.TimeStart;
                NirsSignalData nirsDataFlt = NirsSignalProcessing2.GetNirsSignalData(data);
                lock (NirsSignalQueue2)
                {
                    NirsSignalQueue2.Enqueue(nirsDataFlt);
                }
                List<double> vals = nirsDataFlt.ToList();
                vals.Insert(0, time);

                SlipMidSmartData slipMidSmartData6 = NirsSignalProcessing2.GetSlipMidSmartData(6);
                SlipMidSmartData slipMidSmartData7 = NirsSignalProcessing2.GetSlipMidSmartData(7);

                if (OperatingSystem.IsLinux())
                    gpioServiceRpi?.SetGpioState(6, !slipMidSmartData7.MidCalcEn);


                Dispatcher.UIThread.Invoke(() =>
                {
                    Nirs1ValueText850_3.Text = $"{nirsDataFlt.Led850Ch3_Flt:0.000} V; (MidEN: {(slipMidSmartData6.MidCalcEn ? "TRUE" : "FALSE")})";
                    Nirs1ValueText850_4.Text = $"{nirsDataFlt.Led850Ch4_Flt:0.000} V; (MidEN: {(slipMidSmartData7.MidCalcEn ? "TRUE" : "FALSE")})";
                });
                if (_WriteCsvEn)
                {
                    StreamerCsvNirs2.Write(vals);
                }
            }
            await Task.Delay(5);
        }
        if (_WriteCsvEn)
            StreamerCsvNirs2.Dispose();
    }

    private async void PrintPointsNirs1ThreadAction()
    {
        Point[] points = new Point[0];
        Point[] pointsTotal = new Point[0];
        while (_handlePoints1ThreadStarted)
        {
            lock (NirsSignalQueue1) 
            { 
                int count = NirsSignalQueue1.Count;
                points = new Point[count];
                pointsTotal = new Point[count];
                for (int i = 0; i < count; i++)
                {
                    NirsSignalData nirsData = NirsSignalQueue1.Dequeue();
                    points[i] = new Point((double)_chart1_cnt / 100.0, nirsData.Led850Ch4_Flt);
                    pointsTotal[i] = new Point((double)_chart1_cnt / 100.0, nirsData.TotalVal);
                    //await Nirs1Series740_3.AddPointAsync(points[i]);
                    _chart1_cnt++;
                }
            }
            if(points.Length > 0) 
                await Nirs1Series740_3.AddPointsRangeAsync(points);
            if (pointsTotal.Length > 0)
                await Nirs1Series740_1.AddPointsRangeAsync(pointsTotal);
            await Task.Delay(40);
        }
    }

    private async void PrintPointsNirs2ThreadAction()
    {
        Point[] points = new Point[0];
        while (_handlePoints2ThreadStarted)
        {
            lock (NirsSignalQueue2)
            {
                int count = NirsSignalQueue2.Count;
                points = new Point[count];
                for (int i = 0; i < count; i++)
                {
                    NirsSignalData nirsData = NirsSignalQueue2.Dequeue();
                    points[i] = new Point((double)_chart2_cnt / 100.0, nirsData.Led850Ch4_Flt);
                    _chart2_cnt++;
                }
            }
            if (points.Length > 0)
                await Nirs1Series850_3.AddPointsRangeAsync(points);
            await Task.Delay(40);
        }
    }

    private void RefreshComPortsList()
    {
        NirsComPortSelector1.Items.Clear();
        NirsComPortSelector2.Items.Clear();

        string[] names = (string[])UsbSerialPort.GetPortNames();
        NirsComPortSelector1.Items.Add("Disabled");
        NirsComPortSelector2.Items.Add("Disabled");
        foreach (string name in names)
        {
            NirsComPortSelector1.Items.Add(name);
            NirsComPortSelector2.Items.Add(name);
        }

        NirsComPortSelector1.SelectedIndex = 0;
        NirsComPortSelector2.SelectedIndex = 0;
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(NirsComPortSelector1.SelectedItem.ToString())
            && NirsComPortSelector1.SelectedItem.ToString() != "Disabled")
        {
            if (NirsSensor1 == null)
                NirsSensor1 = new NirsSensorDevice(NirsComPortSelector1.SelectedItem.ToString());

            if (NirsSensor1.IsStarted)
                return;

            NirsSignalQueue1 = new Queue<NirsSignalData>();

            _handlePoints1ThreadStarted = true;

            _handlePointsThreadNirs1 = new Thread(HandlePointsNirs1ThreadAction);
            _handlePointsThreadNirs1.Start();

            _printPointsThreadNirs1 = new Thread(PrintPointsNirs1ThreadAction);
            _printPointsThreadNirs1.Start();

            NirsSensor1.Start();
        }

        if (!string.IsNullOrEmpty(NirsComPortSelector2.SelectedItem.ToString())
            && NirsComPortSelector2.SelectedItem.ToString() != "Disabled"
            && NirsComPortSelector1.SelectedItem.ToString() != NirsComPortSelector2.SelectedItem.ToString())
        {
            if (NirsSensor2 == null)
                NirsSensor2 = new NirsSensorDevice(NirsComPortSelector2.SelectedItem.ToString());

            if (NirsSensor2.IsStarted)
                return;

            NirsSignalQueue2 = new Queue<NirsSignalData>();

            _handlePoints2ThreadStarted = true;

            _handlePointsThreadNirs2 = new Thread(HandlePointsNirs2ThreadAction);
            _handlePointsThreadNirs2.Start();

            _printPointsThreadNirs2 = new Thread(PrintPointsNirs2ThreadAction);
            _printPointsThreadNirs2.Start();

            NirsSensor2.Start();
        }
    }

    private void Stop()
    {
        if (NirsSensor1 != null && NirsSensor1.IsStarted)
            NirsSensor1.Stop();

        if (NirsSensor2 != null && NirsSensor2.IsStarted)
            NirsSensor2.Stop();

        if (_handlePoints1ThreadStarted)
        {
            _handlePoints1ThreadStarted = false;
            _handlePointsThreadNirs1.Join();
            _printPointsThreadNirs1.Join();
        }

        if (_handlePoints2ThreadStarted)
        {
            _handlePoints2ThreadStarted = false;
            _handlePointsThreadNirs2.Join();
            _printPointsThreadNirs2.Join();
        }
    }

    private bool GetTrigDetection(NirsSignalProcessing signalProc, NirsSignalData signalData)
    {
        int detCount = 0;
        for (int i = 0; i < 8; i++)
        {
            detCount += (signalProc.GetTrigDetection(signalData, i) ? 1 : 0);
        }
        return detCount > 0;
    }

    #region Buttons Callbacks


    private void StartComPortsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Start();

    }
    private void StopComPortsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Stop();
    }
    private void RefreshComPortsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        RefreshComPortsList();
    }

    #endregion

}