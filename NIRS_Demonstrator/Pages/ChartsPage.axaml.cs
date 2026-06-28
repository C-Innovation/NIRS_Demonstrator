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
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NIRS_Demonstrator;

public class NirsChartData
{
    public Thread HandlePointsThread;
    public Thread PrintPointsThread;
    public bool HandlePointsThreadStarted = false;
    public NirsSensorDevice NirsSensor;
    public Queue<NirsSignalData> NirsSignalQueue;
    public ReportsStreamerCsv StreamerCsvNirs;
    public bool StreamerCsvNirsStarted;
}

public partial class ChartsPage : BasePage<ChartsPageViewModel>, IDisposable
{
    private const uint HEADER = 0x234E5253;

    private int _chart1_cnt = 0;
    private int _chart2_cnt = 0;
    private int _chart3_cnt = 0;
    private int _chart4_cnt = 0;

    private List<string> CSV_STREAMER_HEADERS = new List<string>()
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

    private NirsChartData NirsChartData1;
    private NirsChartData NirsChartData2;

    public Thread RecordThread;
    public bool RecordThreadStarted = false;


    private bool _WriteCsvEn = false;
    private GpioSeviseRpi gpioServiceRpi;
        
    private double _dTotalVal1 = 0.0;

    private UsbSerialPort _AiDevPort;
    private bool _IsAiDevEnable = false;
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
        NirsChartData1 = new NirsChartData();
        NirsChartData2 = new NirsChartData();

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
        
        string modelPath = "D:\\workspace_PyCharm\\NIRS_NeuroNet\\model_exports\\model.onnx";
        Serializer serializer = new Serializer(HEADER);
        using (var nn = new TestONNX(modelPath))
        {
            NirsSignalProcessing NirsSignalProcessing1 = new NirsSignalProcessing();
            while (NirsChartData1.HandlePointsThreadStarted)
            {
                List<NirsSensorData> nirsData = NirsChartData1.NirsSensor.GetAvailebleData();
                foreach (NirsSensorData data in nirsData)
                {
                    double time = data.TimeMesSec + ((double)data.TimeMesUSec / 1000000.0);
                    if (NirsChartData1.NirsSensor.TimeStart == 0)
                        NirsChartData1.NirsSensor.TimeStart = time;
                    time -= NirsChartData1.NirsSensor.TimeStart;

                    NirsSignalData nirsDataFlt = NirsSignalProcessing1.GetNirsSignalData(data);
                    if (_IsAiDevEnable)
                    {
                        //byte[] rb = new byte[56];
                        //List<byte> bytes = new List<byte>(56);
                        NirsSensorData datatest = new NirsSensorData()
                        {
                            Led740_1 = (ushort)((float)nirsDataFlt.Led740Ch1_Flt / 5.0f * 4095.0f),
                            Led740_2 = (ushort)((float)nirsDataFlt.Led740Ch2_Flt / 5.0f * 4095.0f),
                            Led740_3 = (ushort)((float)nirsDataFlt.Led740Ch3_Flt / 5.0f * 4095.0f),
                            Led740_4 = (ushort)((float)nirsDataFlt.Led740Ch4_Flt / 5.0f * 4095.0f),
                            Led850_1 = (ushort)((float)nirsDataFlt.Led850Ch1_Flt / 5.0f * 4095.0f),
                            Led850_2 = (ushort)((float)nirsDataFlt.Led850Ch2_Flt / 5.0f * 4095.0f),
                            Led850_3 = (ushort)((float)nirsDataFlt.Led850Ch3_Flt / 5.0f * 4095.0f),
                            Led850_4 = (ushort)((float)nirsDataFlt.Led850Ch4_Flt / 5.0f * 4095.0f),
                        };
                        var bytes = serializer.Serialize(datatest.ToByteArray());
                        //NirsSensorData datatest = bytes.ToArray().ToNirsSensorData();
                        await _AiDevPort.WriteAsync(bytes.ToArray());
                    }
                    nirsDataFlt.TotalVal = GetTrigDetection(NirsSignalProcessing1, nirsDataFlt) ? 5.0 : 0.0;
                    float[] inputs = { (float)nirsDataFlt.Led740Ch1_Flt / 5.0f,
                                        (float)nirsDataFlt.Led740Ch2_Flt / 5.0f,
                                        (float)nirsDataFlt.Led740Ch3_Flt / 5.0f,
                                        (float)nirsDataFlt.Led740Ch4_Flt / 5.0f,
                                        (float)nirsDataFlt.Led850Ch1_Flt / 5.0f,
                                        (float)nirsDataFlt.Led850Ch2_Flt / 5.0f,
                                        (float)nirsDataFlt.Led850Ch3_Flt / 5.0f,
                                        (float)nirsDataFlt.Led850Ch4_Flt / 5.0f};

                    //float[] inputs = { (float)nirsDataFlt.Led740Ch1 / 5.0f,
                    //                    (float)nirsDataFlt.Led740Ch2 / 5.0f,
                    //                    (float)nirsDataFlt.Led740Ch3 / 5.0f,
                    //                    (float)nirsDataFlt.Led740Ch4 / 5.0f,
                    //                    (float)nirsDataFlt.Led850Ch1 / 5.0f,
                    //                    (float)nirsDataFlt.Led850Ch2 / 5.0f,
                    //                    (float)nirsDataFlt.Led850Ch3 / 5.0f,
                    //                    (float)nirsDataFlt.Led850Ch4 / 5.0f};
                    //float[] inputs = { 0.899f, 0.871f, 0.083f, 0.055f, 0.907f, 0.878f, 0.150f, 0.108f };
                    nirsDataFlt.AiVal = nn.Predict(inputs) * 5.0;
                    lock (NirsChartData1.NirsSignalQueue)
                    {
                        NirsChartData1.NirsSignalQueue.Enqueue(nirsDataFlt);
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



                    if (NirsChartData1.StreamerCsvNirsStarted)
                    {
                        NirsChartData1.StreamerCsvNirs.Write(vals);
                    }
                }
                await Task.Delay(5);
            }
        }



    }

    private async void HandlePointsNirs2ThreadAction()
    {

        NirsSignalProcessing NirsSignalProcessing2 = new NirsSignalProcessing();

        while (NirsChartData2.HandlePointsThreadStarted)
        {
            List<NirsSensorData> nirsData = NirsChartData2.NirsSensor.GetAvailebleData();
            foreach (NirsSensorData data in nirsData)
            {
                double time = data.TimeMesSec + ((double)data.TimeMesUSec / 1000000.0);
                if (NirsChartData2.NirsSensor.TimeStart == 0)
                    NirsChartData2.NirsSensor.TimeStart = time;
                time -= NirsChartData2.NirsSensor.TimeStart;
                NirsSignalData nirsDataFlt = NirsSignalProcessing2.GetNirsSignalData(data);
                lock (NirsChartData2.NirsSignalQueue)
                {
                    NirsChartData2.NirsSignalQueue.Enqueue(nirsDataFlt);
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
                if (NirsChartData2.StreamerCsvNirsStarted)
                {
                    NirsChartData2.StreamerCsvNirs.Write(vals);
                }
            }
            await Task.Delay(5);
        }

    }

    private async void PrintPointsNirs1ThreadAction()
    {
        Point[] points = new Point[0];
        Point[] pointsTotal = new Point[0];
        Point[] pointsAi = new Point[0];
        while (NirsChartData1.HandlePointsThreadStarted)
        {
            lock (NirsChartData1.NirsSignalQueue)
            {
                int count = NirsChartData1.NirsSignalQueue.Count;
                points = new Point[count];
                pointsTotal = new Point[count];
                pointsAi = new Point[count];
                for (int i = 0; i < count; i++)
                {
                    NirsSignalData nirsData = NirsChartData1.NirsSignalQueue.Dequeue();
                    points[i] = new Point((double)_chart1_cnt / 100.0, nirsData.Led850Ch4_Flt);
                    pointsTotal[i] = new Point((double)_chart1_cnt / 100.0, nirsData.TotalVal);
                    pointsAi[i] = new Point((double)_chart1_cnt / 100.0, nirsData.AiVal);
                    //await Nirs1Series740_3.AddPointAsync(points[i]);
                    _chart1_cnt++;
                }
            }
            if (points.Length > 0)
                await Nirs1Series740_3.AddPointsRangeAsync(points);
            if (pointsTotal.Length > 0)
                await Nirs1Series740_1.AddPointsRangeAsync(pointsTotal);
            if (pointsAi.Length > 0)
                await Nirs1Series740_2.AddPointsRangeAsync(pointsAi);
            await Task.Delay(40);
        }
    }

    private async void PrintPointsNirs2ThreadAction()
    {
        Point[] points = new Point[0];
        while (NirsChartData2.HandlePointsThreadStarted)
        {
            lock (NirsChartData2.NirsSignalQueue)
            {
                int count = NirsChartData2.NirsSignalQueue.Count;
                points = new Point[count];
                for (int i = 0; i < count; i++)
                {
                    NirsSignalData nirsData = NirsChartData2.NirsSignalQueue.Dequeue();
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
        HardwareAiDeviceSelector.Items.Clear();
        string[] names = (string[])UsbSerialPort.GetPortNames();
        NirsComPortSelector1.Items.Add("Disabled");
        NirsComPortSelector2.Items.Add("Disabled");
        HardwareAiDeviceSelector.Items.Add("Disabled");
        foreach (string name in names)
        {
            NirsComPortSelector1.Items.Add(name);
            NirsComPortSelector2.Items.Add(name);
            HardwareAiDeviceSelector.Items.Add(name);
        }

        NirsComPortSelector1.SelectedIndex = 0;
        NirsComPortSelector2.SelectedIndex = 0;
        HardwareAiDeviceSelector.SelectedIndex = 0;
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(NirsComPortSelector1.SelectedItem.ToString())
            && NirsComPortSelector1.SelectedItem.ToString() != "Disabled")
        {
            if (NirsChartData1.NirsSensor == null)
                NirsChartData1.NirsSensor = new NirsSensorDevice(NirsComPortSelector1.SelectedItem.ToString());

            if (NirsChartData1.NirsSensor.IsStarted)
                return;

            NirsChartData1.NirsSignalQueue = new Queue<NirsSignalData>();

            NirsChartData1.HandlePointsThreadStarted = true;

            NirsChartData1.HandlePointsThread = new Thread(HandlePointsNirs1ThreadAction);
            NirsChartData1.HandlePointsThread.Start();

            NirsChartData1.PrintPointsThread = new Thread(PrintPointsNirs1ThreadAction);
            NirsChartData1.PrintPointsThread.Start();

            NirsChartData1.NirsSensor.Start();
        }

        if (!string.IsNullOrEmpty(NirsComPortSelector2.SelectedItem.ToString())
            && NirsComPortSelector2.SelectedItem.ToString() != "Disabled"
            && NirsComPortSelector1.SelectedItem.ToString() != NirsComPortSelector2.SelectedItem.ToString())
        {
            if (NirsChartData2.NirsSensor == null)
                NirsChartData2.NirsSensor = new NirsSensorDevice(NirsComPortSelector2.SelectedItem.ToString());

            if (NirsChartData2.NirsSensor.IsStarted)
                return;

            NirsChartData2.NirsSignalQueue = new Queue<NirsSignalData>();

            NirsChartData2.HandlePointsThreadStarted = true;

            NirsChartData2.HandlePointsThread = new Thread(HandlePointsNirs2ThreadAction);
            NirsChartData2.HandlePointsThread.Start();

            NirsChartData2.PrintPointsThread = new Thread(PrintPointsNirs2ThreadAction);
            NirsChartData2.PrintPointsThread.Start();

            NirsChartData2.NirsSensor.Start();
        }

        if (!string.IsNullOrEmpty(HardwareAiDeviceSelector.SelectedItem.ToString())
            && HardwareAiDeviceSelector.SelectedItem.ToString() != "Disabled")
        {
            _AiDevPort = new UsbSerialPort(HardwareAiDeviceSelector.SelectedItem.ToString(), 8000000);
            _IsAiDevEnable = _AiDevPort.Start();
        }
    }

    private void Stop()
    {
        if (NirsChartData1.NirsSensor != null && NirsChartData1.NirsSensor.IsStarted)
            NirsChartData1.NirsSensor.Stop();

        if (NirsChartData2.NirsSensor != null && NirsChartData2.NirsSensor.IsStarted)
            NirsChartData2.NirsSensor.Stop();

        if (NirsChartData1.StreamerCsvNirsStarted)
        {
            NirsChartData1.StreamerCsvNirsStarted = false;
            NirsChartData1.StreamerCsvNirs.Dispose();
        }

        if (NirsChartData2.StreamerCsvNirsStarted)
        {
            NirsChartData2.StreamerCsvNirsStarted = false;
            NirsChartData2.StreamerCsvNirs.Dispose();
        }

        if (RecordThreadStarted)
        {
            RecordThreadStarted = false;
            RecordThread.Join();
        }

        if (NirsChartData1.HandlePointsThreadStarted)
        {
            NirsChartData1.HandlePointsThreadStarted = false;
            NirsChartData1.HandlePointsThread.Join();
            NirsChartData1.PrintPointsThread.Join();
        }

        if (NirsChartData2.HandlePointsThreadStarted)
        {
            NirsChartData2.HandlePointsThreadStarted = false;
            NirsChartData2.HandlePointsThread.Join();
            NirsChartData2.PrintPointsThread.Join();
        }

        if (_IsAiDevEnable)
        {
            _AiDevPort.Stop();
            _IsAiDevEnable = false;
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

    private async void RecordThreadAction()
    {
        while (RecordThreadStarted)
        {
            double opacity = 1.0;
            while(opacity > 0.0)
            {
                opacity -= 0.05;
                if(opacity < 0.0)
                    opacity = 0.0;

                Dispatcher.UIThread.Invoke(() =>
                {
                    StartRecordButton.Opacity = opacity;
                });
                await Task.Delay(2);
            }
            while (opacity < 1.0)
            {
                opacity += 0.05;
                if (opacity > 1.0)
                    opacity = 1.0;

                Dispatcher.UIThread.Invoke(() =>
                {
                    StartRecordButton.Opacity = opacity;
                });
                await Task.Delay(2);
            }
        }
        Dispatcher.UIThread.Invoke(() =>
        {
            StartRecordButton.Opacity = 1.0;
        });
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

    private void StartRecordButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if(!NirsChartData1.StreamerCsvNirsStarted)
        {
            if(NirsChartData1.HandlePointsThreadStarted)
            {
                string path = Path.Combine(AppConfig.GetInstance().ReportsDirectoryPath, (DataHelpers.GetCurrentDateTimeStr()));
                string path1 = path + "_Nirs1.csv";
                NirsChartData1.StreamerCsvNirs = new ReportsStreamerCsv(path1);
                NirsChartData1.StreamerCsvNirsStarted = true;
                //await StreamerCsvNirs1.WriteHeaderAsync(CSV_STREAMER_HEADERS);
            }
        }
        else
        {
            NirsChartData1.StreamerCsvNirsStarted = false;
            NirsChartData1.StreamerCsvNirs.Dispose();
        }

        if (!NirsChartData2.StreamerCsvNirsStarted)
        {
            if (NirsChartData2.HandlePointsThreadStarted)
            {
                string path = Path.Combine(AppConfig.GetInstance().ReportsDirectoryPath, (DataHelpers.GetCurrentDateTimeStr()));
                string path1 = path + "_Nirs1.csv";
                NirsChartData2.StreamerCsvNirs = new ReportsStreamerCsv(path1);
                NirsChartData2.StreamerCsvNirsStarted = true;
                //await StreamerCsvNirs1.WriteHeaderAsync(CSV_STREAMER_HEADERS);
            }
        }
        else
        {
            NirsChartData2.StreamerCsvNirsStarted = false;
            NirsChartData2.StreamerCsvNirs.Dispose();
        }

        RecordThreadStarted = NirsChartData1.StreamerCsvNirsStarted || NirsChartData2.StreamerCsvNirsStarted;
        if(RecordThreadStarted)
        {
            RecordThread = new Thread(RecordThreadAction);
            RecordThread.Start();
        }
    }

    private void SettingsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
    #endregion

}