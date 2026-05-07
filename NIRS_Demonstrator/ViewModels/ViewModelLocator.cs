
using NIRS_Demonstrator.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class ViewModelLocator
    {
        #region Public Properties

        /// <summary>
        /// 
        /// </summary>
        public static ViewModelLocator Instance { get; private set; } = new ViewModelLocator();





        /// <summary>
        /// 
        /// </summary>
        public static ApplicationViewModel ApplicationViewModel => IoC.Application;

        /// <summary>
        /// 
        /// </summary>
        //public static SettingsViewModel SettingsViewModel => IoC.Settings;


        /// <summary>
        /// 
        /// </summary>
        //public static MainPageViewModel MainViewModel => IoC.Main;

        /// <summary>
        /// 
        /// </summary>
        //public static EsTesterMainViewModel EsTesterMainViewModel => IoC.EsTester;
        #endregion
    }
}
