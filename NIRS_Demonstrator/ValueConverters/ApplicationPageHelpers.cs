using NIRS_Demonstrator.Core;
using NIRS_Demonstrator.ViewModels;
using System;
using System.Diagnostics;
using System.Globalization;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// Converts the<see cref="ApplicationPage"/> to an actual view/page
    /// </summary>
    public static class ApplicationPageHelpers
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        public static BasePage ToBasePage(this ApplicationPage page, object viewModel = null)
        {
            //throw new NotImplementedException();
            switch (page)
            {

                case ApplicationPage.Main:
                    return new MainPage(viewModel as MainPageViewModel);

                case ApplicationPage.Settings:
                    return new SettingsPage(viewModel as SettingsPageViewModel);

                case ApplicationPage.Terminal:
                    return new TerminalPage(viewModel as TerminalPageViewModel);

                case ApplicationPage.Charts:
                    return new ChartsPage(viewModel as ChartsPageViewModel);

                default:
                    Debugger.Break();
                    return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public static ApplicationPage ToApplicationPage(this BasePage page)
        {

            if (page is MainPage)
                return ApplicationPage.Main;

            if (page is SettingsPage)
                return ApplicationPage.Settings;

            if (page is TerminalPage)
                return ApplicationPage.Terminal;

            if (page is ChartsPage)
                return ApplicationPage.Charts;

            Debugger.Break();
            return default(ApplicationPage);
        }
    }
}
