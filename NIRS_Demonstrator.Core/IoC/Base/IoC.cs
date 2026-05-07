using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NIRS_Demonstrator.Core
{
   
    /// <summary>
    /// 
    /// </summary>
    public static class IoC
    {
        #region Public Properties

        /// <summary>
        /// 
        /// </summary>
        public static IKernel Kernel { get; private set; } = new StandardKernel();

        /// <summary>
        /// 
        /// </summary>
        public static IUIManager UI => IoC.Get<IUIManager>();

        /// <summary>
        /// 
        /// </summary>
        public static ILogFactory Logger => IoC.Get<ILogFactory>();

        /// <summary>
        /// 
        /// </summary>
        public static IFileManager File => IoC.Get<IFileManager>();

        /// <summary>
        /// 
        /// </summary>
        public static ITaskManager Task => IoC.Get<ITaskManager>();

        /// <summary>
        /// Main <see cref="ApplicationViewModel"/> object re-translation
        /// </summary>
        public static ApplicationViewModel Application => Get<ApplicationViewModel>();

        /// <summary>
        /// Main <see cref="SettingsViewModel"/> object re-translation
        /// </summary>
        //public static SettingsViewModel Settings => Get<SettingsViewModel>();

        /// <summary>
        /// Main <see cref="MainTesterViewModel"/> object re-translation
        /// </summary>
        //public static MainPageViewModel Main => Get<MainPageViewModel>();

        /// <summary>
        /// Main <see cref="EsTesterMainViewModel"/> object re-translation
        /// </summary>
        //public static EsTesterMainViewModel EsTester => Get<EsTesterMainViewModel>();

        #endregion

        #region Construction

        /// <summary>
        /// 
        /// </summary>
        public static void Setup()
        {
            //
            BindViewModels();
        }
        
        /// <summary>
        /// 
        /// </summary>
        private static void BindViewModels()
        {
            //
            Kernel.Bind<ApplicationViewModel>().ToConstant(new ApplicationViewModel());

            //
            //Kernel.Bind<SettingsViewModel>().ToConstant(new SettingsViewModel());

            //
            //Kernel.Bind<MainTesterViewModel>().ToConstant(new MainTesterViewModel());

            //
            //Kernel.Bind<EsTesterMainViewModel>().ToConstant(new EsTesterMainViewModel());
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Get<T>()
        {
            return Kernel.Get<T>();
        }
    }
}
