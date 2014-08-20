using Sando.Core.Tools;
using Sando.DependencyInjection;
using Sando.UI.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Sando.UI.ViewModel
{
    public class MenuViewModel : BaseViewModel
    {


        public MenuViewModel()
        {

            this.OpenSandoOptionsCommand = new RelayCommand(OpenSandoOptions);
            this.ClearSearchHistoryCommand = new RelayCommand(ClearSearchHistory);


        }

        #region Properties

        public ICommand OpenSandoOptionsCommand
        {
            get;
            set;
        }

        public ICommand ClearSearchHistoryCommand
        {
            get;
            set;
        }


        #endregion

        private void OpenSandoOptions(object parameter)
        {
            var uiPackage = ServiceLocator.Resolve<UIPackage>();
            if (uiPackage != null)
                uiPackage.OpenSandoOptions();
        }

        private void ClearSearchHistory(object parameter)
        {
            var history = ServiceLocator.Resolve<SearchHistory>();
            history.ClearHistory();
        }
    }
}
