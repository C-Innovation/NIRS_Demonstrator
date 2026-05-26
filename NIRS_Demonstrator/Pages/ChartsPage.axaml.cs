using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CInnovation.SignalProcessing.Filters.BiQuad;
using NIRS_Demonstrator.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NIRS_Demonstrator;

public partial class ChartsPage : BasePage<ChartsPageViewModel>, IDisposable
{

    private int _chart1_cnt = 0;
    private int _chart2_cnt = 0;
    private int _chart3_cnt = 0;
    private int _chart4_cnt = 0;

    
    private GpioSeviseRpi gpioServiceRpi;

    private Thread _handlePointsThread;
    private bool _handlePointsThreadStarted = false;

    private NirsSensorDevice NirsSensor1;
    private NirsSensorDevice NirsSensor2;

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

        if(NirsSensor1 != null && NirsSensor1.IsStarted)
            NirsSensor1.Stop();

        //if (NirsSensor2 != null && NirsSensor2.IsStarted)
        //    NirsSensor2.Stop();

        if (_handlePointsThreadStarted)
        {
            
            _handlePointsThreadStarted = false;
            _handlePointsThread.Join();
        }
    }

    private void InitializeLocal()
    {
        Nirs1Chart740.SetAxisXSize(2);
        Nirs1Chart850.SetAxisXSize(2);
        //Nirs2Chart740.SetAxisXSize(1000);
        //Nirs2Chart850.SetAxisXSize(1000);

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

    private async void HandlePointsThreadAction()
    {
        
        NirsSignalProcessing NirsSignalProcessing1 = new NirsSignalProcessing();
        NirsSignalProcessing NirsSignalProcessing2 = new NirsSignalProcessing();
        string path = Path.Combine(AppConfig.GetInstance().ReportsDirectoryPath, (DataHelpers.GetCurrentDateTimeStr()));
        string path1 = path + "_Nirs1.csv";
        string path2 = path + "_Nirs2.csv";
        ReportsStreamerCsv streamerCsv = new ReportsStreamerCsv(path1);
        ReportsStreamerCsv streamerCsv2 = new ReportsStreamerCsv(path2);
        await streamerCsv.WriteHeaderAsync(new List<string>()
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
        });
        while (_handlePointsThreadStarted)
        {
            List<NirsSensorData> data = NirsSensor1.GetAvailebleData();

            foreach (NirsSensorData dataData in data)
            {
                double time = dataData.TimeMesSec + ((double)dataData.TimeMesUSec / 1000000.0);
                if (NirsSensor1.TimeStart == 0)
                    NirsSensor1.TimeStart = time;
                time -= NirsSensor1.TimeStart;
                NirsSignalData nirsData = NirsSignalProcessing1.GetNirsSignalData(dataData);
                List<double> vals = nirsData.ToList();
                vals.Insert(0, time);
                SlipMidSmartData slipMidSmartData6 = NirsSignalProcessing1.GetSlipMidSmartData(6);
                SlipMidSmartData slipMidSmartData7 = NirsSignalProcessing1.GetSlipMidSmartData(7);
                if (!OperatingSystem.IsLinux())
                {
                    await Nirs1Series740_1.AddPointAsync(new Point((double)_chart1_cnt / 100.0, nirsData.Led740Ch3_Flt));
                    await Nirs1Series740_2.AddPointAsync(new Point((double)_chart1_cnt / 100.0, nirsData.Led740Ch4_Flt));
                }
                //await Nirs1Series740_3.AddPointAsync(new Point(_chart1_cnt, nirsData.Led740Ch3_Flt));
                //await Nirs1Series740_4.AddPointAsync(new Point(_chart1_cnt, nirsData.Led740Ch4_Flt));
                _chart1_cnt++;
                if (OperatingSystem.IsLinux())
                {
                    gpioServiceRpi?.SetGpioState(13, !slipMidSmartData7.MidCalcEn);
                }
                Dispatcher.UIThread.Invoke(() =>
                {
                    Nirs1ValueText850_1.Text = $"{nirsData.Led850Ch3_Flt:0.000} V; (MidEN: {(slipMidSmartData6.MidCalcEn ? "TRUE" : "FALSE")})";
                    Nirs1ValueText850_2.Text = $"{nirsData.Led850Ch4_Flt:0.000} V; (MidEN: {(slipMidSmartData7.MidCalcEn ? "TRUE" : "FALSE")})";
                    //Nirs1ValueText850_3.Text = $"{nirsData.Led850Ch3_Flt:0.000} V; (MidEN: {(slipMidSmartData6.MidCalcEn ? "TRUE" : "FALSE")})";
                    //Nirs1ValueText850_4.Text = $"{nirsData.Led850Ch4_Flt:0.000} V; (MidEN: {(slipMidSmartData7.MidCalcEn ? "TRUE" : "FALSE")})";
                });

                if (!OperatingSystem.IsLinux())
                    await Nirs1Series850_1.AddPointAsync(new Point((double)_chart2_cnt / 100.0, nirsData.Led850Ch3_Flt));
                await Nirs1Series850_2.AddPointAsync(new Point((double)_chart2_cnt / 100.0, nirsData.Led850Ch4_Flt));

                //await Nirs1Series850_1.AddPointAsync(new Point(_chart2_cnt, nirsData.Led850Ch3));
                //await Nirs1Series850_2.AddPointAsync(new Point(_chart2_cnt, nirsData.Led850Ch4));

                //await Nirs1Series850_3.AddPointAsync(new Point(_chart2_cnt, nirsData.Led850Ch3_Flt));
                //await Nirs1Series850_4.AddPointAsync(new Point(_chart2_cnt, nirsData.Led850Ch4_Flt));

                //await Nirs1Series850_1.AddPointAsync(new Point(_chart2_cnt, nirsData.Led850Ch3));
                //await Nirs1Series850_2.AddPointAsync(new Point(_chart2_cnt, nirsData.Led850Ch4));

                //await Nirs1Series850_3.AddPointAsync(new Point(_chart2_cnt, nirsData.Led850Ch3_Flt));
                //await Nirs1Series850_4.AddPointAsync(new Point(_chart2_cnt, nirsData.Led850Ch4_Flt));

                _chart2_cnt++;

                streamerCsv.Write(vals);
            }

            data = NirsSensor2.GetAvailebleData();
            foreach (NirsSensorData dataData in data)
            {
                double time = dataData.TimeMesSec + ((double)dataData.TimeMesUSec / 1000000.0);
                if (NirsSensor2.TimeStart == 0)
                    NirsSensor2.TimeStart = time;
                time -= NirsSensor2.TimeStart;
                NirsSignalData nirsData = NirsSignalProcessing2.GetNirsSignalData(dataData);
                List<double> vals = nirsData.ToList();
                vals.Insert(0, time);
                SlipMidSmartData slipMidSmartData6 = NirsSignalProcessing2.GetSlipMidSmartData(6);
                SlipMidSmartData slipMidSmartData7 = NirsSignalProcessing2.GetSlipMidSmartData(7);
                //await Nirs2Series740_1.AddPointAsync(new Point(_chart3_cnt, nirsData.Led740Ch1_Flt));
                //await Nirs2Series740_2.AddPointAsync(new Point(_chart3_cnt, nirsData.Led740Ch2_Flt));
                //await Nirs1Series740_1.AddPointAsync(new Point(_chart1_cnt, nirsData.Led740Ch1_Flt));
                //await Nirs1Series740_2.AddPointAsync(new Point(_chart1_cnt, nirsData.Led740Ch2_Flt));
                //await Nirs2Series740_3.AddPointAsync(new Point(_chart3_cnt, nirsData.Led740Ch3_Flt));
                //await Nirs2Series740_4.AddPointAsync(new Point(_chart3_cnt, nirsData.Led740Ch4_Flt));
                if (!OperatingSystem.IsLinux())
                {
                    await Nirs1Series740_3.AddPointAsync(new Point((double)_chart3_cnt / 100.0, nirsData.Led740Ch3_Flt));
                    await Nirs1Series740_4.AddPointAsync(new Point((double)_chart3_cnt / 100.0, nirsData.Led740Ch4_Flt));
                
                }
                _chart3_cnt++;
                if (OperatingSystem.IsLinux())
                {
                    gpioServiceRpi?.SetGpioState(6, !slipMidSmartData7.MidCalcEn);
                }
                
                Dispatcher.UIThread.Invoke(() =>
                {
                    //Nirs1ValueText850_1.Text = $"{nirsData.Led850Ch3_Flt:0.000} V; (MidEN: {(slipMidSmartData6.MidCalcEn ? "TRUE" : "FALSE")})";
                    //Nirs1ValueText850_2.Text = $"{nirsData.Led850Ch4_Flt:0.000} V; (MidEN: {(slipMidSmartData7.MidCalcEn ? "TRUE" : "FALSE")})";
                    Nirs1ValueText850_3.Text = $"{nirsData.Led850Ch3_Flt:0.000} V; (MidEN: {(slipMidSmartData6.MidCalcEn ? "TRUE" : "FALSE")})";
                    Nirs1ValueText850_4.Text = $"{nirsData.Led850Ch4_Flt:0.000} V; (MidEN: {(slipMidSmartData7.MidCalcEn ? "TRUE" : "FALSE")})";
                });
                //await Nirs2Series850_1.AddPointAsync(new Point(_chart4_cnt, nirsData.Led850Ch3));
                //await Nirs2Series850_2.AddPointAsync(new Point(_chart4_cnt, nirsData.Led850Ch4));
                //await Nirs2Series850_3.AddPointAsync(new Point(_chart4_cnt, nirsData.Led850Ch3_Flt));
                //await Nirs2Series850_4.AddPointAsync(new Point(_chart4_cnt, nirsData.Led850Ch4_Flt));
                if (!OperatingSystem.IsLinux())
                    await Nirs1Series850_3.AddPointAsync(new Point((double)_chart4_cnt / 100.0, nirsData.Led850Ch3_Flt));
                await Nirs1Series850_4.AddPointAsync(new Point((double)_chart4_cnt / 100.0, nirsData.Led850Ch4_Flt));

                //await Nirs1Series850_3.AddPointAsync(new Point(_chart4_cnt, nirsData.Led850Ch3));
                //await Nirs1Series850_4.AddPointAsync(new Point(_chart4_cnt, nirsData.Led850Ch4));
                _chart4_cnt++;

                streamerCsv2.Write(vals);
            }

            await Task.Delay(1);
        }
        streamerCsv.Dispose();
    }

    private void RefreshComPortsList()
    {
        NirsComPortSelector1.Items.Clear();
        NirsComPortSelector2.Items.Clear();
        string[] names = (string[])UsbSerialPort.GetPortNames();
        foreach (string name in names)
        {
            NirsComPortSelector1.Items.Add(name);
            NirsComPortSelector2.Items.Add(name);
        }

        NirsComPortSelector1.SelectedIndex = 0;
        NirsComPortSelector2.SelectedIndex = 0;
    }

    private void StartComPortsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(NirsComPortSelector1.SelectedItem.ToString()) || string.IsNullOrEmpty(NirsComPortSelector2.SelectedItem.ToString()))
            return;

        if (NirsComPortSelector1.SelectedItem.ToString() == NirsComPortSelector2.SelectedItem.ToString())
            return;

        if (NirsSensor1 == null)
            NirsSensor1 = new NirsSensorDevice(NirsComPortSelector1.SelectedItem.ToString());

        if (NirsSensor1.IsStarted)
            return;

        if (NirsSensor2 == null)
            NirsSensor2 = new NirsSensorDevice(NirsComPortSelector2.SelectedItem.ToString());

        if (NirsSensor2.IsStarted)
            return;

        _handlePointsThreadStarted = true;
        _handlePointsThread = new Thread(HandlePointsThreadAction);
        _handlePointsThread.Start();

        NirsSensor1.Start();
        NirsSensor2.Start();
    }
    private void StopComPortsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (NirsSensor1 != null && NirsSensor1.IsStarted)
            NirsSensor1.Stop();

        if (NirsSensor2 != null && NirsSensor2.IsStarted)
            NirsSensor2.Stop();

        if (_handlePointsThreadStarted)
        {
            _handlePointsThreadStarted = false;
            _handlePointsThread.Join();
        }
    }
    private void RefreshComPortsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        RefreshComPortsList();
    }

    
}