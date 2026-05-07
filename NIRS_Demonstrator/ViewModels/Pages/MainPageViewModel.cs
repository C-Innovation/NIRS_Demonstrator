using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NIRS_Demonstrator.Core;

namespace NIRS_Demonstrator.ViewModels
{
    /// <summary>
    /// 
    /// </summary>
    public class MainPageViewModel : ViewModelBase
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members

        #endregion

        #region Public Properties

        public string Greeting { get; set; }

        #endregion

        #region Properties for binding

        
        #endregion

        #region Public Commands

        public ICommand RunSettingsCommand { get; set; }
        public ICommand RunTerminalCommand { get; set; }
        public ICommand RunChartsCommand { get; set; }

        #endregion

        #region Public Events

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public MainPageViewModel()
        {
          

            RunSettingsCommand = new RelayCommand(RunSettingsCommandAction);
            RunTerminalCommand = new RelayCommand(RunTerminalCommandAction);
            RunChartsCommand = new RelayCommand(RunChartsCommandAction);

            AppConfig.GetInstance().RegisterDisposableObject(this);
        }

        

        ~MainPageViewModel()
        {
            
        }


        #endregion

        #region Private Callbacks



        #endregion

        public override void Dispose()
        {
            
            base.Dispose();
        }

        #region Command Methods


        private void RunSettingsCommandAction()
        {
            IoC.Application.GoToPage(ApplicationPage.Settings);
        }

        private void RunTerminalCommandAction()
        {
            IoC.Application.GoToPage(ApplicationPage.Terminal);
        }

        private void RunChartsCommandAction()
        {
            IoC.Application.GoToPage(ApplicationPage.Charts);
        }
        

        #endregion

        #region Public Methods

        #endregion

        #region Private Methods

       

        #endregion
    }
}
