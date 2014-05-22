using ABB.SrcML.VisualStudio.SrcMLService;
using Sando.DependencyInjection;
using Sando.UI.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Sando.UI.ViewModel
{
    public class SearchViewModel : BaseViewModel
    {
        #region Properties

        private IndexedFile _indexedFile;

        public ICommand AddIndexFolderCommand
        {
            get;
            set;
        }

        public ICommand RemoveIndexFolderCommand
        {
            get;
            set;
        }

        public ICommand ApplyCommand
        {
            get;
            set;
        }

        public ObservableCollection<IndexedFile> IndexedFiles
        {
            get;
            set;
        }

        public IndexedFile SelectedIndexedFile
        {
            get
            {
                return this._indexedFile;
            }
            set
            {
                this._indexedFile = value;
                OnPropertyChanged("SelectedIndexedFile");
            }
        }

        #endregion


        public SearchViewModel()
        {

            this.IndexedFiles = new ObservableCollection<IndexedFile>();

            this.AddIndexFolderCommand = new RelayCommand(AddIndexFolder);
            this.RemoveIndexFolderCommand = new RelayCommand(RemoveIndexFolder);
            this.ApplyCommand = new RelayCommand(Apply);

            this.RegisterSrcMLService();

        }

        public void AddIndexFolder(String path)
        {
            IndexedFile file = new IndexedFile();
            file.FilePath = path;
            this.IndexedFiles.Add(file);
            this.SelectedIndexedFile = file;
        }

        private void AddIndexFolder(object param)
        {
            AddIndexFolder("C:\\");
        }

        private void RemoveIndexFolder(object param)
        {
            if (null != this.SelectedIndexedFile)
            {
                int index = this.IndexedFiles.IndexOf(this.SelectedIndexedFile);

                if (index > 0)
                {

                    if (index != this.IndexedFiles.Count - 1)
                    {
                        this.SelectedIndexedFile = this.IndexedFiles[index + 1];
                    }
                    else
                    {
                        this.SelectedIndexedFile = this.IndexedFiles[index - 1];
                    }

                    this.IndexedFiles.RemoveAt(index);
                }
                else if (index == 0)
                {

                    this.SelectedIndexedFile = null;

                    this.IndexedFiles.RemoveAt(index);
                }
                 
            }
        }

        private void Apply(object param)
        {

        }

        private void RegisterSrcMLService()
        {
            ISrcMLGlobalService srcMLService = ServiceLocator.Resolve<ISrcMLGlobalService>();
            srcMLService.DirectoryAdded += (sender, e) =>
            {
                IndexedFile file = new IndexedFile();
                file.FilePath = e.Directory;
                this.SelectedIndexedFile = file;

                Application.Current.Dispatcher.BeginInvoke(new Action(delegate()
                {

                    this.IndexedFiles.Add(file);

                }), null);

            };
        }
        

    }

    public class IndexedFile : BaseViewModel
    {

        private String _filePath;

        public String FilePath
        {
            get
            {
                return this._filePath;
            }
            set
            {
                this._filePath = value;
                OnPropertyChanged("FilePath");
            }
        }

    }

    
}
