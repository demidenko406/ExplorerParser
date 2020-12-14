using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Explorer.Models;

namespace Explorer.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IDirectoryHistory history;

        private readonly CollectionHistory collectionHistory;

        private DeleteHisotry deletedHisotry;

        public string FilePath { get; set; }

        public ObservableCollection<FileEntityViewModel> MyFiles { get; set; } =
            new ObservableCollection<FileEntityViewModel>();

        public ObservableCollection<FileEntityViewModel> DirectoriesAndFiles { get; set; } =
            new ObservableCollection<FileEntityViewModel>();

        public FileEntityViewModel SelectedFileEntity { get; set; }

        public ICommand OpenCommand { get; }
        public ICommand CompressCommand { get; }
        public ICommand DecompressCommand { get; }
        public ICommand DeleteFileCommand { get; }
        public ICommand AddToCollectionCommand { get; }
        public ICommand CompressCollectionCommand { get; }
        public DelegateCommand MoveBackCommand { get; }
        public DelegateCommand MoveForwardCommand { get; }

        public MainViewModel()
        {
            var configuration = Configuration.GetConfiguration("defaultconfig.json");
            Config.CollectionTarget = configuration.CollectionConnectionString;
            Config.DeletedTarget = configuration.DeletedConnectionString;
            collectionHistory = new CollectionHistory();
            deletedHisotry = new DeleteHisotry();
            deletedHisotry.DeletedHistory = new List<string>();
            MyFiles.Clear();
            history = new DirectoryHistory(FilePath);
            foreach (var logicalDrive in Directory.GetLogicalDrives())
            {
                DirectoriesAndFiles.Add(new DirectoryViewModel(logicalDrive));
            }

            history.HistoryChanged += History_HistoryChanged;

            CompressCollectionCommand = new DelegateCommand(CompressCollection);
            OpenCommand = new DelegateCommand(Open);
            CompressCommand = new DelegateCommand(Compress);
            DecompressCommand = new DelegateCommand(Decompress);
            DeleteFileCommand = new DelegateCommand(DeleteFile);
            AddToCollectionCommand = new DelegateCommand(AddToCollection);
            MoveBackCommand = new DelegateCommand(MoveBack, CanMoveBack);
            MoveForwardCommand = new DelegateCommand(MoveForward, CanMoveForward);
            MoveForwardCommand = new DelegateCommand(MoveForward, CanMoveForward);

        }

        private void CompressCollection(object collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            foreach (var file in MyFiles)
            {
                Compress(file);
            }
            MyFiles.Clear();
        }

        private void AddToCollection(object passedFile)
        {
            if (passedFile == null)
            {
                throw new ArgumentNullException(nameof(passedFile));
            }

            if (passedFile is FileViewModel fileViewModel)
            {
                MyFiles.Add(fileViewModel);
                collectionHistory.Collection += MyFiles.Last().FullName;
                collectionHistory.Collection += "\n";
                collectionHistory.UpdateCollection();
            }
        }

        private void History_HistoryChanged(object? sender, EventArgs e)
        {
            MoveBackCommand?.RaiseCanExecuteChanged();
            MoveForwardCommand?.RaiseCanExecuteChanged();
        }

        private bool CanMoveBack(object obj)
        {
            return history.CanMoveBack;
        }

        private bool CanMoveForward(object obj)
        {
            return history.CanMoveForward;
        }

        private void MoveForward(object obj)
        {
            history.MoveForward();
            var current = history.Current;
            FilePath = current.DirectoryPath;
            OpenDirectory();
        }

        private void MoveBack(object obj)
        {
            history.MoveBack();
            var current = history.Current;
            FilePath = current.DirectoryPath;
            OpenDirectory();
        }

        private void Open(object passedObject)
        {
            //null checking
            if (passedObject is DirectoryViewModel directoryViewModel)
            {
                //Writing filePath
                FilePath = directoryViewModel.FullName;
                history.Add(FilePath);
                OpenDirectory();
            }
        }

        private void OpenDirectory()
        {
            DirectoriesAndFiles.Clear();

            var directoryInfo = new DirectoryInfo(FilePath);

            foreach (var directory in directoryInfo.GetDirectories())
            {
                if (!directory.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    DirectoriesAndFiles.Add(new DirectoryViewModel(directory));
                }

            }

            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                DirectoriesAndFiles.Add(new FileViewModel(fileInfo));
            }
        }

        private void Compress(object passedObject)
        {
            if (passedObject == null)
            {
                throw new ArgumentNullException(nameof(passedObject));
            }
            else if (passedObject is FileViewModel fileViewModel)
            {
                FileOptionsModel.Compress(fileViewModel.FullName, fileViewModel.FullName + ".gz");
            }
            else if (passedObject is DirectoryViewModel directoryViewModel)
            {
                FileOptionsModel.FolderCompress(directoryViewModel.FullName,
                    "C://Workspace/ArchivatedFiles/" + directoryViewModel.Name);

            }
            else if (passedObject is ObservableCollection<FileEntityViewModel> filesCollection)
            {
                foreach (var file in filesCollection)
                    FileOptionsModel.Compress(file.FullName, file.FullName + "archive" + ".gz");

            }
        }

        private void Decompress(object passedObject)
        {
            if (passedObject is FileViewModel fileViewModel && fileViewModel.FullName.EndsWith('z'))
            {
                FileOptionsModel.Decompress(fileViewModel.FullName,
                    "C://Workspace/ArchivatedFiles/" + fileViewModel.Name.Remove(fileViewModel.Name.Length - 3));
            }
            else if (passedObject is FileViewModel fileViewModel2)
            {
                FileOptionsModel.FolderDecompress(fileViewModel2.FullName,
                    "C://Workspace/ToArchive/" + fileViewModel2.Name);
            }
            else
            {
                throw new ArgumentNullException(nameof(passedObject));
            }
        }

        private void DeleteFile(object passedObject)
        {
            if (passedObject == null)
            {
                throw new ArgumentNullException(nameof(passedObject));
            }
            if (passedObject is FileViewModel fileViewModel)
            {
                FileOptionsModel.Delete(fileViewModel.FullName);
                deletedHisotry.DeletedHistory.Add(fileViewModel.FullName);
                deletedHisotry.UpdateBin();
            }
        }

    }
}