using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NIRS_Demonstrator.ViewModels;

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

        if (_handlePointsThreadStarted)
        {
            
            _handlePointsThreadStarted = false;
            _handlePointsThread.Join();
        }
    }

    private void InitializeLocal()
    {
        Nirs1Chart740.SetAxisXSize(1000);
        Nirs1Chart850.SetAxisXSize(1000);
        Nirs2Chart740.SetAxisXSize(1000);
        Nirs2Chart850.SetAxisXSize(1000);

        AppConfig.GetInstance().RegisterDisposableObject(this);
        //_handlePointsThreadStarted = true;
        //_handlePointsThread = new Thread(HandlePointsThreadAction);
        //_handlePointsThread.Start();

        RefreshComPortsList();
    }

    private async void HandlePointsThreadAction()
    {

        while (_handlePointsThreadStarted)
        {
            List<NirsSensorData> data = NirsSensor1.GetAvailebleData();
            foreach (NirsSensorData dataData in data)
            {
                await Nirs1Series740_1.AddPointAsync(new Point(_chart1_cnt, RemoveLedBackground(dataData.Led740_1, dataData.Led740_Bgd_1).ToVoltage5V(12)));
                await Nirs1Series740_2.AddPointAsync(new Point(_chart1_cnt, RemoveLedBackground(dataData.Led740_2, dataData.Led740_Bgd_2).ToVoltage5V(12)));
                await Nirs1Series740_3.AddPointAsync(new Point(_chart1_cnt, RemoveLedBackground(dataData.Led740_3, dataData.Led740_Bgd_3).ToVoltage5V(12)));
                await Nirs1Series740_4.AddPointAsync(new Point(_chart1_cnt, RemoveLedBackground(dataData.Led740_4, dataData.Led740_Bgd_4).ToVoltage5V(12)));
                _chart1_cnt++;                                                                                   
                await Nirs1Series850_1.AddPointAsync(new Point(_chart2_cnt, RemoveLedBackground(dataData.Led850_1, dataData.Led850_Bgd_1).ToVoltage5V(12)));
                await Nirs1Series850_2.AddPointAsync(new Point(_chart2_cnt, RemoveLedBackground(dataData.Led850_2, dataData.Led850_Bgd_2).ToVoltage5V(12)));
                await Nirs1Series850_3.AddPointAsync(new Point(_chart2_cnt, RemoveLedBackground(dataData.Led850_3, dataData.Led850_Bgd_3).ToVoltage5V(12)));
                await Nirs1Series850_4.AddPointAsync(new Point(_chart2_cnt, RemoveLedBackground(dataData.Led850_4, dataData.Led850_Bgd_4).ToVoltage5V(12)));
                _chart2_cnt++;
            }
           
            await Task.Delay(1);
        }
    }

    private void RefreshComPortsList()
    {
        NirsComPortSelector1.Items.Clear();
        string[] names = (string[])UsbSerialPort.GetPortNames();
        foreach (string name in names)
        {
            NirsComPortSelector1.Items.Add(name);
        }

        NirsComPortSelector1.SelectedIndex = 0;
    }

    private void StartComPortsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(NirsComPortSelector1.SelectedItem.ToString()))
            return;

        if (NirsSensor1 == null)
            NirsSensor1 = new NirsSensorDevice(NirsComPortSelector1.SelectedItem.ToString());

        if (NirsSensor1.IsStarted)
            return;

        _handlePointsThreadStarted = true;
        _handlePointsThread = new Thread(HandlePointsThreadAction);
        _handlePointsThread.Start();

        NirsSensor1.Start();
    }
    private void StopComPortsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (NirsSensor1 != null && NirsSensor1.IsStarted)
            NirsSensor1.Stop();

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