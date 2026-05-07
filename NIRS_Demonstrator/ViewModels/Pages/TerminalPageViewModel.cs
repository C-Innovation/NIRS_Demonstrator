using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NIRS_Demonstrator.Core;

namespace NIRS_Demonstrator.ViewModels
{
    public class TerminalPageViewModel : ViewModelBase
    {

        #region Private Members

        private bool _recordIsStrated = false;
        private Thread _recordThread;

        #endregion

        private double _terminalHeight;
       
        public double TerminalHeight 
        { 
            get => _terminalHeight;
            set
            {
                if (_terminalHeight != value)
                {
                    _terminalHeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _terminalWidth;
        public double TerminalWidth
        {
            get => _terminalWidth;
            set
            {
                if (_terminalWidth != value)
                {
                    _terminalWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _terminalContent;
        public string TerminalContent
        {
            get => _terminalContent;
            set
            {
                if (_terminalContent != value)
                {
                    _terminalContent = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _recordButtonOpacity = 1.0;
        public double RecordButtonOpacity
        {
            get => _recordButtonOpacity;
            set
            {
                _recordButtonOpacity = value;
                OnPropertyChanged();
            }
        }

        #region Public Commands

        public ICommand ReturnToMainCommand { get; set; }
        public ICommand StartRecordCommand { get; set; }
        public ICommand ClearTerminalCommand { get; set; }

        #endregion

        public TerminalPageViewModel()
        {
            ReturnToMainCommand = new RelayCommand(ReturnToMainCommandAction);
            StartRecordCommand = new RelayCommand(StartRecordCommandAction);
            ClearTerminalCommand = new RelayCommand(ClearTerminalCommandAction);
            AppConfig.GetInstance().MainViewSizeChanged += MainViewSizeChanged;
            TerminalHeight = AppConfig.GetInstance().MainViewSize.Height - 200;
            TerminalWidth = AppConfig.GetInstance().MainViewSize.Width - 200;
            
            AppConfig.GetInstance().RegisterDisposableObject(this);
        }

        

        ~TerminalPageViewModel()
        {
            
        }
        private void MainViewSizeChanged(object? sender, Avalonia.Controls.SizeChangedEventArgs e)
        {
            TerminalHeight = e.NewSize.Height - 200;
            TerminalWidth = e.NewSize.Width - 200;
        }

        #region Command Methods

        private void ReturnToMainCommandAction()
        {
            IoC.Application.GoToPage(ApplicationPage.Main);
        }

        private void StartRecordCommandAction()
        {
            _recordIsStrated ^= true;
            if (_recordIsStrated)
            {
                
                _recordThread = new Thread(RecordProcessTask);
                _recordThread.Start();
            }
            else
            {
                
                _recordThread.Join();
                RecordButtonOpacity = 1.0;
            }
        }

        private void ClearTerminalCommandAction()
        {
            
        }

        public override void Dispose()
        {
            if (_recordIsStrated)
            {
                _recordIsStrated = false;
                
                _recordThread.Join();
                RecordButtonOpacity = 1.0;
            }
            base.Dispose();
        }
        #endregion

        #region Private Methods

        private void RecordProcessTask()
        {
            while(_recordIsStrated)
            {
                for (int i = 0; i < 25; i++)
                {
                    RecordButtonOpacity -= 0.04;
                    Thread.Sleep(1);
                    if (!_recordIsStrated)
                        break;
                }
                for (int i = 0; i < 25; i++)
                {
                    RecordButtonOpacity += 0.04;
                    Thread.Sleep(1);
                    if (!_recordIsStrated)
                        break;
                }
            }
        }

        #endregion
    }
}
