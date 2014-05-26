using Sando.UI.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sando.UI.View
{
    /// <summary>
    /// Interaction logic for SearchView.xaml
    /// </summary>
    public partial class SearchView : UserControl
    {
        public SearchView()
        {
            InitializeComponent();
        }

        private void IndexingList_KeyDown(object sender, KeyEventArgs e)
        {
            CurrentlyIndexingFoldersPopup.IsOpen = true;
        }

        private void IndexingList_MouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            CurrentlyIndexingFoldersPopup.IsOpen = true;
        }

        private void BrowserButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                //if (MonitoredFiles.Count > 0)
                //    dialog.SelectedPath = MonitoredFiles.First().Id;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (System.Windows.Forms.DialogResult.OK.Equals(result))
                {
                    //MonitoredFiles.Add(new CheckedListItem(dialog.SelectedPath));
                    SearchViewModel vm = this.DataContext as SearchViewModel;
                    if (null != vm)
                    {
                        vm.SetIndexFolderPath(dialog.SelectedPath);
                    }
                }
            }
            
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentlyIndexingFoldersPopup.IsOpen = false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentlyIndexingFoldersPopup.IsOpen = false;
        }

        
    }
}
