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

    private List<Point> _pointsCh1;
    private List<Point> _pointsCh2;
    private List<Point> _pointsCh3;
    private List<Point> _pointsCh4;

    private Thread _handlePointsThread;
    private bool _handlePointsThreadStarted = false;

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
        if(_handlePointsThreadStarted)
        {
            _handlePointsThreadStarted = false;
            _handlePointsThread.Join();
        }
    }

    private void InitializeLocal()
    {
        ChartChannel1.SetAxisXSize(1000);
        ChartChannel2.SetAxisXSize(1000);
        ChartChannel3.SetAxisXSize(1000);
        ChartChannel4.SetAxisXSize(1000);
        
        AppConfig.GetInstance().RegisterDisposableObject(this);
        _pointsCh1 = new List<Point>();
        _pointsCh2 = new List<Point>();
        _pointsCh3 = new List<Point>();
        _pointsCh4 = new List<Point>();
        _handlePointsThreadStarted = true;
        _handlePointsThread = new Thread(HandlePointsThreadAction);
        _handlePointsThread.Start();
    }

    private void OnChartDataReceived(object? sender, ChartData e)
    {
        switch (e.Channel)
        {
            case 0:
                _pointsCh1.Add(new Point(_chart1_cnt, e.Y));
                _chart1_cnt++;
                break;

            case 1:
                _pointsCh2.Add(new Point(_chart2_cnt, e.Y));
                _chart2_cnt++;
                break;
            case 2:
                _pointsCh3.Add(new Point(_chart3_cnt, e.Y));
                _chart3_cnt++;
                break;
            case 3:
                _pointsCh4.Add(new Point(_chart4_cnt, e.Y));
                _chart4_cnt++;
                break;

            default: break;
        }
    }

    private async void HandlePointsThreadAction()
    {
        Point[] _pCh1;
        Point[] _pCh2;
        Point[] _pCh3;
        Point[] _pCh4;

        while (_handlePointsThreadStarted)
        {
            int __cnt = _pointsCh1.Count;
            if (__cnt > 0)
            {
                _pCh1 = new Point[__cnt];
                _pointsCh1.CopyTo(0, _pCh1, 0, __cnt);
                _pointsCh1.RemoveRange(0, __cnt);
                await SeriesChannel1.AddPointsRangeAsync(_pCh1);
            }
            __cnt = _pointsCh2.Count;
            if (__cnt > 0)
            {
                _pCh2 = new Point[__cnt];
                _pointsCh2.CopyTo(0, _pCh2, 0, __cnt);
                _pointsCh2.RemoveRange(0, __cnt);
                await SeriesChannel2.AddPointsRangeAsync(_pCh2);
            }

            __cnt = _pointsCh3.Count;
            if (__cnt > 0)
            {
                _pCh3 = new Point[__cnt];
                _pointsCh3.CopyTo(0, _pCh3, 0, __cnt);
                _pointsCh3.RemoveRange(0, __cnt);
                await SeriesChannel3.AddPointsRangeAsync(_pCh3);
            }
            __cnt = _pointsCh4.Count;
            if (__cnt > 0)
            {
                _pCh4 = new Point[__cnt];
                _pointsCh4.CopyTo(0, _pCh4, 0, __cnt);
                _pointsCh4.RemoveRange(0, __cnt);
                await SeriesChannel4.AddPointsRangeAsync(_pCh4);
            }
            await Task.Delay(33);
        }
    }
}