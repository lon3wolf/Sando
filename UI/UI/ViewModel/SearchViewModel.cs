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
        private bool _isBrowseButtonEnabled;

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

        public ICommand CancelCommand
        {
            get;
            set;
        }

        public ObservableCollection<IndexedFile> IndexedFiles
        {
            get;
            set;
        }

        public List<IndexedFile> ModifiedIndexedFile
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

                if (null != this._indexedFile)
                {
                    this.IsBrowseButtonEnabled = true;
                }
                else
                {
                    this.IsBrowseButtonEnabled = false;
                }
            }
        }

        public bool IsBrowseButtonEnabled
        {
            get
            {
                return this._isBrowseButtonEnabled;
            }
            set
            {
                this._isBrowseButtonEnabled = value;
                OnPropertyChanged("IsBrowseButtonEnabled");
            }
        }

        #endregion


        public SearchViewModel()
        {
            this.ModifiedIndexedFile = new List<IndexedFile>();
            this.IndexedFiles = new ObservableCollection<IndexedFile>();

            this.AddIndexFolderCommand = new RelayCommand(AddIndexFolder);
            this.RemoveIndexFolderCommand = new RelayCommand(RemoveIndexFolder);
            this.ApplyCommand = new RelayCommand(Apply);
            this.CancelCommand = new RelayCommand(Cancel);

            this.IsBrowseButtonEnabled = false;

            this.RegisterSrcMLService();

        }

        /// <summary>
        /// Used by Browse button in the user interface
        /// </summary>
        /// <param name="path"></param>
        public void SetIndexFolderPath(String path)
        {
            if (null != this.SelectedIndexedFile)
            {
                IndexedFile file = new IndexedFile();
                file.FilePath = this.SelectedIndexedFile.FilePath;
                file.OperationStatus = IndexedFile.Status.Modified;
                file.GUID = this.SelectedIndexedFile.GUID;
                this.ModifiedIndexedFile.Add(file);

                this.SelectedIndexedFile.FilePath = path;
            }
        }

        private void AddIndexFolder(IndexedFile file)
        {
            this.IndexedFiles.Add(file);
            this.SelectedIndexedFile = file;
        }

        private void AddIndexFolder(object param)
        {
            IndexedFile file = new IndexedFile();
            file.FilePath = "C:\\";
            file.OperationStatus = IndexedFile.Status.Add;

            this.ModifiedIndexedFile.Add(file);

            this.AddIndexFolder(file);
        }

        private void RemoveIndexFolder(object param)
        {
            if (null != this.SelectedIndexedFile)
            {
                this.SelectedIndexedFile.OperationStatus = IndexedFile.Status.Remove;
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
                    
                }
                else if (index == 0)
                {
                    this.SelectedIndexedFile = null;
                }

                this.ModifiedIndexedFile.Add(this.IndexedFiles[index]);
                this.IndexedFiles.RemoveAt(index);
            }
        }

        private void Apply(object param)
        {
            foreach (var file in this.IndexedFiles)
            {
                file.OperationStatus = IndexedFile.Status.Normal;
            }


            this.ModifiedIndexedFile.Clear();
        }

        private void Cancel(object param)
        {
            foreach (var file in this.ModifiedIndexedFile)
            {
                if (file.OperationStatus == IndexedFile.Status.Add)
                {
                    this.IndexedFiles.Remove(file);
                }
                else if (file.OperationStatus == IndexedFile.Status.Remove)
                {
                    this.IndexedFiles.Add(file);
                }
                else if (file.OperationStatus == IndexedFile.Status.Modified)
                {

                    foreach (var indexedFile in this.IndexedFiles)
                    {
                        if (indexedFile.GUID == file.GUID)
                        {

                            indexedFile.FilePath = file.FilePath;

                        }
                    }
                }
            }

            foreach (var file in this.IndexedFiles)
            {
                file.OperationStatus = IndexedFile.Status.Normal;
            }
            this.ModifiedIndexedFile.Clear();


        }

        private void RegisterSrcMLService()
        {
            ISrcMLGlobalService srcMLService = ServiceLocator.Resolve<ISrcMLGlobalService>();
            srcMLService.DirectoryAdded += (sender, e) =>
            {
                IndexedFile file = new IndexedFile();
                file.FilePath = e.Directory;
                file.OperationStatus = IndexedFile.Status.Normal;
                

                Application.Current.Dispatcher.BeginInvoke(new Action(delegate()
                {

                    this.AddIndexFolder(file);

                }), null);

            };
        }
        

    }

    public class IndexedFile : BaseViewModel
    {

        private String _filePath;

        public IndexedFile()
        {
            this.GUID = Guid.NewGuid();
        }

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

        internal Guid GUID
        {
            get;
            set;
        }

        internal Status OperationStatus
        {
            get;
            set;
        }

        internal enum Status
        {

            Add,
            Remove,
            Modified,
            Normal

        }
    }

    
}
