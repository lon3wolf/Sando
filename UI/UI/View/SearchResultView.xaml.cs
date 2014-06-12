using Sando.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sando.UI.View
{
    /// <summary>
    /// Interaction logic for SearchResultView.xaml
    /// </summary>
    public partial class SearchResultView : UserControl
    {

        private SearchResultViewModel _viewModel;

        public SearchResultView()
        {
            InitializeComponent();

            this.DataContextChanged += SearchResultView_DataContextChanged;
        }

        private void SearchResultView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this._viewModel = this.DataContext as SearchResultViewModel;

            this._viewModel.SearchResults.CollectionChanged += SearchResults_CollectionChanged;
        }

        private void SearchResults_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                this.searchResultListbox.Focus();
            }
        }

        private void TypeHeaderText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Control Open/Close behaviour of Popup here. This is a trade-off implementation.
            TypeColumnHeaderViewModel vm = ((TextBlock)sender).DataContext as TypeColumnHeaderViewModel;
            vm.IsPopupOpen = !vm.IsPopupOpen;
        }

    
    }
}
