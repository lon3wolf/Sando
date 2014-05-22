using Sando.UI.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sando.UI.ViewModel
{
    public class SandoMainViewModel : BaseViewModel
    {

        #region Properties

        public MenuViewModel MenuViewModel
        {
            get;
            private set;
        }

        public SearchViewModel SearchViewModel
        {
            get;
            private set;
        }

        public SearchResultViewModel SearchResultViewModel
        {
            get;
            private set;
        }

        #endregion

        public SandoMainViewModel()
        {

            //Since sando is not a heavy UI application, we just load view models here.
            this.MenuViewModel = new MenuViewModel();
            this.SearchResultViewModel = new SearchResultViewModel();
            this.SearchViewModel = new SearchViewModel();



        }


    }
}
