using System;
using System.Windows;
using System.Windows.Input;
using System.Threading;
using System.Collections.ObjectModel;

// Toolkit namespace
using SimpleMvvmToolkit;

// Toolkit extension methods
using SimpleMvvmToolkit.ModelExtensions;
using DuplicateFiles.Models;
using DuplicateFiles.Locators;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Xml;


namespace DuplicateFiles
{
    /// <summary>
    /// This class extends ViewModelDetailBase which implements IEditableDataObject.
    /// <para>
    /// Specify type being edited <strong>DetailType</strong> as the second type argument
    /// and as a parameter to the seccond ctor.
    /// </para>
    /// <para>
    /// Use the <strong>mvvmprop</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// </summary>
    public class FilesViewModel : ViewModelDetailBase<FilesViewModel, ItemProvider>
    {
        public ItemProvider ItemsProvider { get; private set; }
        public const string DefaultDuplicateFilesFilename = "DuplicateFiles.xml";

        public event Action<List<DuplicateFile>> FoundDuplicate;
        public Dictionary<string, List<DuplicateFile>> AllDuplicates = new Dictionary<string, List<DuplicateFile>>();
        public SerializableDictionary<string, List<DuplicateFile>> AllFiles { get; private set; }
        public string DataFile { get; set; }

        private bool IsIterating = false;

        private Item _SelectedFolder;
        public Item SelectedFolder
        {
            get { return _SelectedFolder; }
            set
            {
                _SelectedFolder = value;
                if (_SelectedFolder is DirectoryItem && !IsIterating)
                {
                    SendMessage(Messages.UserSelectedFolderFromTree, new NotificationEventArgs(_SelectedFolder.Path));
                }
            }
        }

        // Default ctor
        public FilesViewModel()
        {
            DataFile = DefaultDuplicateFilesFilename;
            RegisterToReceiveMessages(Messages.GetFiles, GetFilesFromItemProvider);
            ItemsProvider = new ItemProvider();
        }

        public DelegateCommand BackCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    SendMessage(Messages.UserSelectedBackDirectory, new NotificationEventArgs());
                });
            }
        }

        public void GetFilesFromItemProvider(object sender, NotificationEventArgs e)
        {
            new Thread(() =>
            {
                IsIterating = true;
                ItemsProvider.GetItems(e.Message);
                NotifyPropertyChanged(x => x.ItemsProvider.Items);
                new Thread(() =>
                {
                    Thread.Sleep(500);
                    IsIterating = false;
                }) { IsBackground = true }.Start();

                SendMessage(Messages.WorkingChanged, new NotificationEventArgs("false"));
            }) { IsBackground = true }.Start();

        }

        public void LoadFromFile(string fileName)
        {

            var collector = new DuplicateCollector();
            var newPath = collector.LoadFromFile(fileName);

            SendMessage(Messages.NotifyString, new NotificationEventArgs("Loaded."));
            SendMessage(Messages.UserSelectedFolderFromTree, new NotificationEventArgs(newPath));
            SendMessage(Messages.NotifyString, new NotificationEventArgs("Looking for duplicates..."));
            if (collector.AllFiles == null)
            {
                SendMessage(Messages.NotifyString, new NotificationEventArgs("Saved file contain no files."));
                return;
            }
            collector.CalculateDuplicate();
            AllDuplicates = collector.AllDuplicated;
            SendMessage(Messages.NotifyString, new NotificationEventArgs("Done."));
            
            SendMessage(Messages.NotifyString, new NotificationEventArgs(String.Format("Found {0} duplicated files.", collector.AllDuplicated.Count)));
            //SendMessage(Messages.WorkingChanged, new NotificationEventArgs("false"));
            SendMessage(Messages.FilesCompleted, new NotificationEventArgs());

        }

        public Dictionary<string, List<DuplicateFile>> GetAllFiles()
        {
            return AllFiles;
        }
        public void SearchForDuplicates(string rootFolder, CriteriaSettings pCriteria)
        {
            DuplicateCollector collector = new DuplicateCollector(rootFolder, pCriteria);
            collector.FileAdded += x =>
            {
                SendMessage(Messages.FileAdded, new NotificationEventArgs(x));
            };
            collector.DuplicateFileFound += x =>
            {
                if (FoundDuplicate != null)
                    FoundDuplicate(x);
            };

            SendMessage(Messages.NotifyString, new NotificationEventArgs("Starting count of files..."));
            var count = collector.CountAllFiles();
            SendMessage(Messages.FilesCount, new NotificationEventArgs(count.ToString()));
            SendMessage(Messages.NotifyString, new NotificationEventArgs("Total of: " + count.ToString() + " files found."));

            SendMessage(Messages.NotifyString, new NotificationEventArgs("Starting MD5 Calculation..."));
            AllFiles = collector.ScanFileInfo();
            SendMessage(Messages.NotifyString, new NotificationEventArgs("MD5 Calculation Completed."));
            SendMessage(Messages.NotifyString, new NotificationEventArgs(String.Format(@"Saving results to ""{0}""", DataFile)));

            if (File.Exists(DataFile))
                File.Delete(DataFile);
            var SerializationObject = new SavedSession() { Duplicates = AllFiles, SelectedPath = rootFolder, Settings = pCriteria };
            BinarySerializationAssistor.Serialize(SerializationObject, DataFile);
            SendMessage(Messages.NotifyString, new NotificationEventArgs("Saved."));

            SendMessage(Messages.NotifyString, new NotificationEventArgs("Looking for duplicates..."));
            collector.CalculateDuplicate();
            AllDuplicates = collector.AllDuplicated;
            SendMessage(Messages.NotifyString, new NotificationEventArgs("Done."));
            SendMessage(Messages.NotifyString, new NotificationEventArgs(String.Format("Found {0} duplicated files.", collector.AllDuplicated.Count)));
            SendMessage(Messages.WorkingChanged, new NotificationEventArgs("false"));
            SendMessage(Messages.FilesCompleted, new NotificationEventArgs());

        }

    }
}