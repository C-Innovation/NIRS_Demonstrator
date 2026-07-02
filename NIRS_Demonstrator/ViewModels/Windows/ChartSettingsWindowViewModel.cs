using Avalonia.Threading;
using NIRS_Demonstrator.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NIRS_Demonstrator.ViewModels
{
    /// <summary>
    /// 
    /// </summary>
    public class ChartSettingsWindowViewModel : ViewModelBase, IDisposable
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members

        private ChartsPage _ChartsPage;
        private ChartSettingsWindow _ChartsSettinsWindow;
        private Thread _UpdateThread;
        private bool _UpdateThreadStarted;
        #endregion

        #region Public Properties

        #endregion

        #region MVVM Properties

        private bool _ConfigEn = false;

        public bool ConfigEn
        {
            get => _ConfigEn;
            set
            {
                if (value != _ConfigEn)
                {
                    _ConfigEn = value;
                    LoadValues(_ConfigEn);
                    OnPropertyChanged();
                }
            }
        }


        private double _PosValueChnnel1;

        public double PosValueChannel1
        {
            get => _PosValueChnnel1;
            set
            {
                if (value != _PosValueChnnel1)
                {
                    _PosValueChnnel1 = value;
                    _ChartsPage.NirsChartData1.NirsSignalProcessing.SetPosTrigLevel(0, _PosValueChnnel1);
                    OnPropertyChanged();
                }
            }
        }

        private double _NegValueChnnel1;

        public double NegValueChannel1
        {
            get => _NegValueChnnel1;
            set
            {
                if (value != _NegValueChnnel1)
                {
                    _NegValueChnnel1 = value;
                    _ChartsPage.NirsChartData1.NirsSignalProcessing.SetNegTrigLevel(0, _NegValueChnnel1);
                    OnPropertyChanged();
                }
            }
        }

        private double _PosValueChnnel2;

        public double PosValueChannel2
        {
            get => _PosValueChnnel2;
            set
            {
                if (value != _PosValueChnnel2)
                {
                    _PosValueChnnel2 = value;
                    _ChartsPage.NirsChartData1.NirsSignalProcessing.SetPosTrigLevel(1, _PosValueChnnel2);
                    OnPropertyChanged();
                }
            }
        }

        private double _NegValueChnnel2;

        public double NegValueChannel2
        {
            get => _NegValueChnnel2;
            set
            {
                if (value != _NegValueChnnel2)
                {
                    _NegValueChnnel2 = value;
                    _ChartsPage.NirsChartData1.NirsSignalProcessing.SetNegTrigLevel(1, _NegValueChnnel2);
                    OnPropertyChanged();
                }
            }
        }

        private double _PosValueChnnel3;

        public double PosValueChannel3
        {
            get => _PosValueChnnel3;
            set
            {
                if (value != _PosValueChnnel3)
                {
                    _PosValueChnnel3 = value;
                    _ChartsPage.NirsChartData1.NirsSignalProcessing.SetPosTrigLevel(2, _PosValueChnnel3);
                    OnPropertyChanged();
                }
            }
        }

        private double _NegValueChnnel3;

        public double NegValueChannel3
        {
            get => _NegValueChnnel3;
            set
            {
                if (value != _NegValueChnnel3)
                {
                    _NegValueChnnel3 = value;
                    _ChartsPage.NirsChartData1.NirsSignalProcessing.SetNegTrigLevel(2, _NegValueChnnel3);
                    OnPropertyChanged();
                }
            }
        }


        private double _PosValueChnnel4;

        public double PosValueChannel4
        {
            get => _PosValueChnnel4;
            set
            {
                if (value != _PosValueChnnel4)
                {
                    _PosValueChnnel4 = value;
                    _ChartsPage.NirsChartData1.NirsSignalProcessing.SetPosTrigLevel(3, _PosValueChnnel4);
                    OnPropertyChanged();
                }
            }
        }

        private double _NegValueChnnel4;

        public double NegValueChannel4
        {
            get => _NegValueChnnel4;
            set
            {
                if (value != _NegValueChnnel4)
                {
                    _NegValueChnnel4 = value;
                    _ChartsPage.NirsChartData1.NirsSignalProcessing.SetNegTrigLevel(3, _NegValueChnnel4);
                    OnPropertyChanged();
                }
            }
        }
        // =========================== Section 850 nm =============================== //

        private double _PosValueChnnel5;

        public double PosValueChannel5
        {
            get => _PosValueChnnel5;
            set
            {
                if (value != _PosValueChnnel5)
                {
                    _PosValueChnnel5 = value;
                    _ChartsPage.NirsChartData1.NirsSignalProcessing.SetPosTrigLevel(4, _PosValueChnnel5);
                    OnPropertyChanged();
                }
            }
        }

        private double _NegValueChnnel5;

        public double NegValueChannel5
        {
            get => _NegValueChnnel5;
            set
            {
                if (value != _NegValueChnnel5)
                {
                    _NegValueChnnel5 = value;
                    _ChartsPage.NirsChartData1.NirsSignalProcessing.SetNegTrigLevel(4, _NegValueChnnel5);
                    OnPropertyChanged();
                }
            }
        }

        private double _PosValueChnnel6;

        public double PosValueChannel6
        {
            get => _PosValueChnnel6;
            set
            {
                if (value != _PosValueChnnel6)
                {
                    _PosValueChnnel6 = value;
                    _ChartsPage.NirsChartData1.NirsSignalProcessing.SetPosTrigLevel(5, _PosValueChnnel6);
                    OnPropertyChanged();
                }
            }
        }

        private double _NegValueChnnel6;

        public double NegValueChannel6
        {
            get => _NegValueChnnel6;
            set
            {
                if (value != _NegValueChnnel6)
                {
                    _NegValueChnnel6 = value;
                    _ChartsPage.NirsChartData1.NirsSignalProcessing.SetNegTrigLevel(5, _NegValueChnnel6);
                    OnPropertyChanged();
                }
            }
        }

        private double _PosValueChnnel7;

        public double PosValueChannel7
        {
            get => _PosValueChnnel7;
            set
            {
                if (value != _PosValueChnnel7)
                {
                    _PosValueChnnel7 = value;
                    _ChartsPage.NirsChartData1.NirsSignalProcessing.SetPosTrigLevel(6, _PosValueChnnel7);
                    OnPropertyChanged();
                }
            }
        }

        private double _NegValueChnnel7;

        public double NegValueChannel7
        {
            get => _NegValueChnnel7;
            set
            {
                if (value != _NegValueChnnel7)
                {
                    _NegValueChnnel7 = value;
                    _ChartsPage.NirsChartData1.NirsSignalProcessing.SetNegTrigLevel(6, _NegValueChnnel7);
                    OnPropertyChanged();
                }
            }
        }


        private double _PosValueChnnel8;

        public double PosValueChannel8
        {
            get => _PosValueChnnel8;
            set
            {
                if (value != _PosValueChnnel8)
                {
                    _PosValueChnnel8 = value;
                    _ChartsPage.NirsChartData1.NirsSignalProcessing.SetPosTrigLevel(7, _PosValueChnnel8);
                    OnPropertyChanged();
                }
            }
        }

        private double _NegValueChnnel8;

        public double NegValueChannel8
        {
            get => _NegValueChnnel8;
            set
            {
                if (value != _NegValueChnnel8)
                {
                    _NegValueChnnel8 = value;
                    _ChartsPage.NirsChartData1.NirsSignalProcessing.SetNegTrigLevel(7, _NegValueChnnel8);
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region Public Commands

        #endregion

        #region Public Events

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public ChartSettingsWindowViewModel(ChartsPage chartsPage, ChartSettingsWindow window)
        {
            _ChartsPage = chartsPage;
            _ChartsSettinsWindow = window;
            _ChartsSettinsWindow.Closed += _ChartsSettinsWindow_Closed;
            ConfigEn = _ChartsPage.NirsChartData1.HandlePointsThreadStarted;
            _UpdateThreadStarted = true;
            _UpdateThread = new Thread(UpdateThreadAction);
            _UpdateThread.Start();
            AppConfig.GetInstance().RegisterDisposableObject(this);
        }



        #endregion

        #region Private Callbacks

        private void _ChartsSettinsWindow_Closed(object? sender, EventArgs e)
        {
            Dispose();
        }

        #endregion

        #region Command Methods

        #endregion

        #region Public Methods


        public override void Dispose()
        {
            if(_UpdateThreadStarted)
            {
                _UpdateThreadStarted = false;
                _UpdateThread.Join();
            }
            ClearMarkers();
            base.Dispose();
        }

        #endregion

        #region Private Methods

        private async void UpdateThreadAction()
        {
            while(_UpdateThreadStarted)
            {
                ConfigEn = _ChartsPage.NirsChartData1.HandlePointsThreadStarted;
                UpdateMarkers();
                await Task.Delay(33);
            }
        }

        private async void LoadValues(bool en)
        {
            if (en)
            {
                while (_ChartsPage.NirsChartData1.NirsSignalProcessing == null)
                    await Task.Delay(100);
                PosValueChannel1 = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigLevel(0);
                NegValueChannel1 = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigLevel(0);

                PosValueChannel2 = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigLevel(1);
                NegValueChannel2 = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigLevel(1);

                PosValueChannel3 = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigLevel(2);
                NegValueChannel3 = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigLevel(2);

                PosValueChannel4 = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigLevel(3);
                NegValueChannel4 = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigLevel(3);

                PosValueChannel5 = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigLevel(4);
                NegValueChannel5 = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigLevel(4);

                PosValueChannel6 = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigLevel(5);
                NegValueChannel6 = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigLevel(5);

                PosValueChannel7 = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigLevel(6);
                NegValueChannel7 = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigLevel(6);

                PosValueChannel8 = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigLevel(7);
                NegValueChannel8 = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigLevel(7);

                ClearMarkers();
                Dispatcher.UIThread.Invoke(() =>
                {
                    HorizontalMarker marker = new HorizontalMarker()
                    {
                        Stroke = _ChartsPage.Nirs1Series740_1.Stroke,
                        Opacity = 0.5,
                        StrokeThickness = 1,
                        Level = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigTotalLevel(0)
                    };
                    _ChartsPage.Nirs1Chart740.HorizontalMarkers.Add(marker);
                    marker = new HorizontalMarker()
                    {
                        Stroke = _ChartsPage.Nirs1Series740_1.Stroke,
                        Opacity = 0.5,
                        StrokeThickness = 1,
                        Level = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigTotalLevel(0)
                    };
                    _ChartsPage.Nirs1Chart740.HorizontalMarkers.Add(marker);

                    marker = new HorizontalMarker()
                    {
                        Stroke = _ChartsPage.Nirs1Series740_2.Stroke,
                        Opacity = 0.5,
                        StrokeThickness = 1,
                        Level = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigTotalLevel(1)
                    };
                    _ChartsPage.Nirs1Chart740.HorizontalMarkers.Add(marker);

                    marker = new HorizontalMarker()
                    {
                        Stroke = _ChartsPage.Nirs1Series740_2.Stroke,
                        Opacity = 0.5,
                        StrokeThickness = 1,
                        Level = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigTotalLevel(1)
                    };
                    _ChartsPage.Nirs1Chart740.HorizontalMarkers.Add(marker);

                    marker = new HorizontalMarker()
                    {
                        Stroke = _ChartsPage.Nirs1Series740_3.Stroke,
                        Opacity = 0.5,
                        StrokeThickness = 1,
                        Level = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigTotalLevel(2)
                    };
                    _ChartsPage.Nirs1Chart740.HorizontalMarkers.Add(marker);

                    marker = new HorizontalMarker()
                    {
                        Stroke = _ChartsPage.Nirs1Series740_3.Stroke,
                        Opacity = 0.5,
                        StrokeThickness = 1,
                        Level = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigTotalLevel(2)
                    };
                    _ChartsPage.Nirs1Chart740.HorizontalMarkers.Add(marker);

                    marker = new HorizontalMarker()
                    {
                        Stroke = _ChartsPage.Nirs1Series740_4.Stroke,
                        Opacity = 0.5,
                        StrokeThickness = 1,
                        Level = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigTotalLevel(3)
                    };
                    _ChartsPage.Nirs1Chart740.HorizontalMarkers.Add(marker);

                    marker = new HorizontalMarker()
                    {
                        Stroke = _ChartsPage.Nirs1Series740_4.Stroke,
                        Opacity = 0.5,
                        StrokeThickness = 1,
                        Level = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigTotalLevel(3)
                    };
                    _ChartsPage.Nirs1Chart740.HorizontalMarkers.Add(marker);


                    marker = new HorizontalMarker()
                    {
                        Stroke = _ChartsPage.Nirs1Series850_1.Stroke,
                        Opacity = 0.5,
                        StrokeThickness = 1,
                        Level = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigTotalLevel(4)
                    };
                    _ChartsPage.Nirs1Chart850.HorizontalMarkers.Add(marker);
                    marker = new HorizontalMarker()
                    {
                        Stroke = _ChartsPage.Nirs1Series850_1.Stroke,
                        Opacity = 0.5,
                        StrokeThickness = 1,
                        Level = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigTotalLevel(4)
                    };
                    _ChartsPage.Nirs1Chart850.HorizontalMarkers.Add(marker);

                    marker = new HorizontalMarker()
                    {
                        Stroke = _ChartsPage.Nirs1Series850_2.Stroke,
                        Opacity = 0.5,
                        StrokeThickness = 1,
                        Level = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigTotalLevel(5)
                    };
                    _ChartsPage.Nirs1Chart850.HorizontalMarkers.Add(marker);

                    marker = new HorizontalMarker()
                    {
                        Stroke = _ChartsPage.Nirs1Series850_2.Stroke,
                        Opacity = 0.5,
                        StrokeThickness = 1,
                        Level = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigTotalLevel(5)
                    };
                    _ChartsPage.Nirs1Chart850.HorizontalMarkers.Add(marker);

                    marker = new HorizontalMarker()
                    {
                        Stroke = _ChartsPage.Nirs1Series850_3.Stroke,
                        Opacity = 0.5,
                        StrokeThickness = 1,
                        Level = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigTotalLevel(6)
                    };
                    _ChartsPage.Nirs1Chart850.HorizontalMarkers.Add(marker);

                    marker = new HorizontalMarker()
                    {
                        Stroke = _ChartsPage.Nirs1Series850_3.Stroke,
                        Opacity = 0.5,
                        StrokeThickness = 1,
                        Level = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigTotalLevel(6)
                    };
                    _ChartsPage.Nirs1Chart850.HorizontalMarkers.Add(marker);

                    marker = new HorizontalMarker()
                    {
                        Stroke = _ChartsPage.Nirs1Series850_4.Stroke,
                        Opacity = 0.5,
                        StrokeThickness = 1,
                        Level = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigTotalLevel(7)
                    };
                    _ChartsPage.Nirs1Chart850.HorizontalMarkers.Add(marker);

                    marker = new HorizontalMarker()
                    {
                        Stroke = _ChartsPage.Nirs1Series850_4.Stroke,
                        Opacity = 0.5,
                        StrokeThickness = 1,
                        Level = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigTotalLevel(7)
                    };
                    _ChartsPage.Nirs1Chart850.HorizontalMarkers.Add(marker);
                });
                
            }
        }

        private void UpdateMarkers()
        {
            
            Dispatcher.UIThread.Invoke(() =>
            {
                if (_ChartsPage.Nirs1Chart740.HorizontalMarkers.Count == 0)
                    return;
                _ChartsPage.Nirs1Chart740.HorizontalMarkers[0].Level
                    = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigTotalLevel(0);
                _ChartsPage.Nirs1Chart740.HorizontalMarkers[1].Level
                    = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigTotalLevel(0);

                _ChartsPage.Nirs1Chart740.HorizontalMarkers[2].Level
                    = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigTotalLevel(1);
                _ChartsPage.Nirs1Chart740.HorizontalMarkers[3].Level
                    = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigTotalLevel(1);

                _ChartsPage.Nirs1Chart740.HorizontalMarkers[4].Level
                    = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigTotalLevel(2);
                _ChartsPage.Nirs1Chart740.HorizontalMarkers[5].Level
                    = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigTotalLevel(2);

                _ChartsPage.Nirs1Chart740.HorizontalMarkers[6].Level
                    = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigTotalLevel(3);
                _ChartsPage.Nirs1Chart740.HorizontalMarkers[7].Level
                    = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigTotalLevel(3);

                if (_ChartsPage.Nirs1Chart850.HorizontalMarkers.Count == 0)
                    return;
                _ChartsPage.Nirs1Chart850.HorizontalMarkers[0].Level
                    = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigTotalLevel(4);
                _ChartsPage.Nirs1Chart850.HorizontalMarkers[1].Level
                    = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigTotalLevel(4);

                _ChartsPage.Nirs1Chart850.HorizontalMarkers[2].Level
                    = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigTotalLevel(5);
                _ChartsPage.Nirs1Chart850.HorizontalMarkers[3].Level
                    = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigTotalLevel(5);

                _ChartsPage.Nirs1Chart850.HorizontalMarkers[4].Level
                    = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigTotalLevel(6);
                _ChartsPage.Nirs1Chart850.HorizontalMarkers[5].Level
                    = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigTotalLevel(6);

                _ChartsPage.Nirs1Chart850.HorizontalMarkers[6].Level
                    = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetPosTrigTotalLevel(7);
                _ChartsPage.Nirs1Chart850.HorizontalMarkers[7].Level
                    = _ChartsPage.NirsChartData1.NirsSignalProcessing.GetNegTrigTotalLevel(7);
            });
            
        }

        void ClearMarkers()
        {
            int cnt = _ChartsPage.Nirs1Chart740.HorizontalMarkers.Count;
            for (int i = 0; i < cnt; i++)
                _ChartsPage.Nirs1Chart740.HorizontalMarkers.RemoveAt(0);

            cnt = _ChartsPage.Nirs1Chart850.HorizontalMarkers.Count;
            for (int i = 0; i < cnt; i++)
                _ChartsPage.Nirs1Chart850.HorizontalMarkers.RemoveAt(0);
        }

        #endregion
    }
}
