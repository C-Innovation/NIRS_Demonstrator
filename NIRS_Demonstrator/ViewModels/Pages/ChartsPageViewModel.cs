using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NIRS_Demonstrator.Core;

namespace NIRS_Demonstrator.ViewModels
{
    public class ChartsPageViewModel : ViewModelBase
    {

        #region Public Commands

        public ICommand ReturnToMainCommand { get; set; }

        #endregion

        public ChartsPageViewModel()
        {
            ReturnToMainCommand = new RelayCommand(ReturnToMainCommandAction);

            AppConfig.GetInstance().RegisterDisposableObject(this);
        }

        #region Command Methods
        private void ReturnToMainCommandAction()
        {
            IoC.Application.GoToPage(ApplicationPage.Main);
        }

        #endregion

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
