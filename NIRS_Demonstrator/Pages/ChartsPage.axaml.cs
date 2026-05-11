using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CInnovation.SignalProcessing.Filters.BiQuad;
using NIRS_Demonstrator.ViewModels;
using System;
using System.Collections.Generic;
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
        Nirs1Chart740.SetAxisXSize(500);
        Nirs1Chart850.SetAxisXSize(500);
        //Nirs2Chart740.SetAxisXSize(500);
        //Nirs2Chart850.SetAxisXSize(500);

        Nirs1Chart740.HorizontalScroll.Value = Nirs1Chart740.HorizontalScroll.Maximum;
        Nirs1Chart850.HorizontalScroll.Value = Nirs1Chart850.HorizontalScroll.Maximum;
        //Nirs2Chart740.HorizontalScroll.Value = Nirs2Chart740.HorizontalScroll.Maximum;
        //Nirs2Chart850.HorizontalScroll.Value = Nirs2Chart850.HorizontalScroll.Maximum;
        AppConfig.GetInstance().RegisterDisposableObject(this);
        //_handlePointsThreadStarted = true;
        //_handlePointsThread = new Thread(HandlePointsThreadAction);
        //_handlePointsThread.Start();

        RefreshComPortsList();
    }

    private async void HandlePointsThreadAction()
    {
        double[] results = new double[4];
        SlipMid[] slipMids = new SlipMid[8]
        {
            new SlipMid(10),
            new SlipMid(10),
            new SlipMid(10),
            new SlipMid(10),
            new SlipMid(10),
            new SlipMid(10),
            new SlipMid(10),
            new SlipMid(10)
        };
        LowpassFilter[] lowpassFilters = new LowpassFilter[4]
        {
            new LowpassFilter(100, 10.0),
            new LowpassFilter(100, 10.0),
            new LowpassFilter(100, 10.0),
            new LowpassFilter(100, 10.0)
        };
        while (_handlePointsThreadStarted)
        {
            List<NirsSensorData> data = NirsSensor1.GetAvailebleData();
            foreach (NirsSensorData dataData in data)
            {
                results[0] = RemoveLedBackground(dataData.Led740_1, dataData.Led740_Bgd_1).ToVoltage5V(12);
                results[1] = RemoveLedBackground(dataData.Led740_2, dataData.Led740_Bgd_2).ToVoltage5V(12);
                results[2] = RemoveLedBackground(dataData.Led740_3, dataData.Led740_Bgd_3).ToVoltage5V(12);
                results[3] = RemoveLedBackground(dataData.Led740_4, dataData.Led740_Bgd_4).ToVoltage5V(12);

                results[2] = slipMids[0].Process(results[2]);
                results[3] = slipMids[1].Process(results[3]);
                await Nirs1Series740_1.AddPointAsync(new Point(_chart1_cnt, results[0]));
                await Nirs1Series740_2.AddPointAsync(new Point(_chart1_cnt, results[1]));
                await Nirs1Series740_3.AddPointAsync(new Point(_chart1_cnt, results[2]));
                await Nirs1Series740_4.AddPointAsync(new Point(_chart1_cnt, results[3]));
                _chart1_cnt++;

                results[0] = RemoveLedBackground(dataData.Led850_3, dataData.Led850_Bgd_3).ToVoltage5V(12);
                results[1] = RemoveLedBackground(dataData.Led850_4, dataData.Led850_Bgd_4).ToVoltage5V(12);
                results[2] = RemoveLedBackground(dataData.Led850_3, dataData.Led850_Bgd_3).ToVoltage5V(12);
                results[3] = RemoveLedBackground(dataData.Led850_4, dataData.Led850_Bgd_4).ToVoltage5V(12);

                //results[2] = lowpassFilters[2].Process(results[2]);
                //results[3] = lowpassFilters[3].Process(results[3]);
                results[2] = slipMids[2].Process(results[2]);
                results[3] = slipMids[3].Process(results[3]);
                Dispatcher.UIThread.Invoke(() =>
                {
                    Nirs1ValueText850_1.Text = $"{results[0]:0.000} V";
                    Nirs1ValueText850_2.Text = $"{results[1]:0.000} V";
                    Nirs1ValueText850_3.Text = $"{results[2]:0.000} V";
                    Nirs1ValueText850_4.Text = $"{results[3]:0.000} V";
                });
                
                await Nirs1Series850_1.AddPointAsync(new Point(_chart2_cnt, results[0]));
                await Nirs1Series850_2.AddPointAsync(new Point(_chart2_cnt, results[1]));
                await Nirs1Series850_3.AddPointAsync(new Point(_chart2_cnt, results[2]));
                await Nirs1Series850_4.AddPointAsync(new Point(_chart2_cnt, results[3]));
                _chart2_cnt++;
            }
/*
            data = NirsSensor2.GetAvailebleData();
            foreach (NirsSensorData dataData in data)
            {
                results[0] = RemoveLedBackground(dataData.Led740_3, dataData.Led740_Bgd_3).ToVoltage5V(12);
                results[1] = RemoveLedBackground(dataData.Led740_4, dataData.Led740_Bgd_4).ToVoltage5V(12);
                results[2] = RemoveLedBackground(dataData.Led740_3, dataData.Led740_Bgd_3).ToVoltage5V(12);
                results[3] = RemoveLedBackground(dataData.Led740_4, dataData.Led740_Bgd_4).ToVoltage5V(12);

                results[2] = slipMids[4].Process(results[2]);
                results[3] = slipMids[5].Process(results[3]);

                //await Nirs2Series740_1.AddPointAsync(new Point(_chart3_cnt, results[0]));
                //await Nirs2Series740_2.AddPointAsync(new Point(_chart3_cnt, results[1]));
                await Nirs2Series740_3.AddPointAsync(new Point(_chart3_cnt, results[2]));
                await Nirs2Series740_4.AddPointAsync(new Point(_chart3_cnt, results[3]));
                _chart3_cnt++;

                results[0] = RemoveLedBackground(dataData.Led850_3, dataData.Led850_Bgd_3).ToVoltage5V(12);
                results[1] = RemoveLedBackground(dataData.Led850_4, dataData.Led850_Bgd_4).ToVoltage5V(12);
                results[2] = RemoveLedBackground(dataData.Led850_3, dataData.Led850_Bgd_3).ToVoltage5V(12);
                results[3] = RemoveLedBackground(dataData.Led850_4, dataData.Led850_Bgd_4).ToVoltage5V(12);

                results[2] = slipMids[6].Process(results[2]);
                results[3] = slipMids[7].Process(results[3]);
                //await Nirs2Series850_1.AddPointAsync(new Point(_chart4_cnt, results[0]));
                //await Nirs2Series850_2.AddPointAsync(new Point(_chart4_cnt, results[1]));
                await Nirs2Series850_3.AddPointAsync(new Point(_chart4_cnt, results[2]));
                await Nirs2Series850_4.AddPointAsync(new Point(_chart4_cnt, results[3]));
                _chart4_cnt++;
            }
*/
            await Task.Delay(1);
        }
    }

    private void RefreshComPortsList()
    {
        NirsComPortSelector1.Items.Clear();
        //NirsComPortSelector2.Items.Clear();
        string[] names = (string[])UsbSerialPort.GetPortNames();
        foreach (string name in names)
        {
            NirsComPortSelector1.Items.Add(name);
            //NirsComPortSelector2.Items.Add(name);
        }

        NirsComPortSelector1.SelectedIndex = 0;
        //NirsComPortSelector2.SelectedIndex = 0;
    }

    private void StartComPortsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(NirsComPortSelector1.SelectedItem.ToString()) /*|| string.IsNullOrEmpty(NirsComPortSelector2.SelectedItem.ToString())*/)
            return;

        //if(NirsComPortSelector1.SelectedItem.ToString() == NirsComPortSelector2.SelectedItem.ToString())
        //    return;

        if (NirsSensor1 == null)
            NirsSensor1 = new NirsSensorDevice(NirsComPortSelector1.SelectedItem.ToString());

        if (NirsSensor1.IsStarted)
            return;

        //if (NirsSensor2 == null)
        //    NirsSensor2 = new NirsSensorDevice(NirsComPortSelector2.SelectedItem.ToString());

        //if (NirsSensor2.IsStarted)
        //    return;

        _handlePointsThreadStarted = true;
        _handlePointsThread = new Thread(HandlePointsThreadAction);
        _handlePointsThread.Start();

        NirsSensor1.Start();
        //NirsSensor2.Start();
    }
    private void StopComPortsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (NirsSensor1 != null && NirsSensor1.IsStarted)
            NirsSensor1.Stop();

        //if (NirsSensor2 != null && NirsSensor2.IsStarted)
        //    NirsSensor2.Stop();

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

    ushort RemoveLedBackground(ushort val, ushort bgd)
    {
        if (val < bgd)
            return 0;

        return (ushort)(val - bgd);
    }
}