using System;
using System.Windows;
using System.Threading;
using System.Collections.ObjectModel;
using System.Linq;
// Toolkit namespace
using SimpleMvvmToolkit;

// Toolkit extension methods
using SimpleMvvmToolkit.ModelExtensions;
using DuplicateFiles.Locators;
using System.Windows.Controls;
using DuplicateFiles.Models;
using System.Windows.Documents;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;


namespace DuplicateFiles
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// Use the <strong>mvvmprop</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// </summary>
    public class MainPageViewModel : ViewModelBase<MainPageViewModel>
    {
        private string tenResultsText = "Show All Results";
        private string tenResultsTextAlt = "Show First 10 Results";
        private string startFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        #region Initialization and Cleanup

        // Default ctor
        public MainPageViewModel()
        {
            _MainUCDataContext = new FilesViewModel();
            AllDuplicates = new ObservableCollection<KeyValuePair<string, List<DuplicateFile>>>();
            MainUC = new FilesView() { DataContext = _MainUCDataContext };
            _MainUCDataContext.FoundDuplicate += x =>
            {

            };

            RegisterToReceiveMessages(Messages.WorkingChanged, (x, y) =>
            {
                Working(bool.Parse(y.Message));

            });

            RegisterToReceiveMessages(Messages.NotifyString, (x, y) =>
            {
                Log(y.Message);
            });

            RegisterToReceiveMessages(Messages.FileAdded, (x, y) =>
            {
                SetStatusCurrentFile(y.Message);
            });

            RegisterToReceiveMessages(Messages.FilesCount, (x, y) =>
            {
                FilesCount = y.Message;
                ProgressMinimum = 0;
                ProgressMaximum = int.Parse(FilesCount);
                CurrentProgress = 0;
                NotifyPropertyChanged(z => z.ProgressMaximum);
                NotifyPropertyChanged(z => z.ProgressMinimum);
                NotifyPropertyChanged(z => z.CurrentProgress);
            });

            RegisterToReceiveMessages(Messages.UserSelectedFolderFromTree, (x, y) =>
            {
                SelectedPath = y.Message;
                NotifyPropertyChanged(p => p.SelectedPath);
                NotifyPropertyChanged(p => p.Criteria);
            });

            RegisterToReceiveMessages(Messages.UserSelectedBackDirectory, (x, y) =>
            {
                var parent = Path.GetDirectoryName(SelectedPath);
                if (!String.IsNullOrEmpty(parent))
                    SelectedPath = parent;
            });

            RegisterToReceiveMessages(Messages.FilesCompleted, (x, y) =>
            {
                //int counter = 0;
                Disp(() =>
                {
                    AllDuplicates = new ObservableCollection<KeyValuePair<string, List<DuplicateFile>>>();
                    TruelyAllDuplicates = new ObservableCollection<KeyValuePair<string, List<DuplicateFile>>>();
                });

                var nulls = _MainUCDataContext.AllDuplicates.Where(z => z.Value == null).ToList();

                foreach (var a in _MainUCDataContext.AllDuplicates)
                {

                    TruelyAllDuplicates.Add(a);
                    //if (counter++ == 400)
                    //{
                    //    NotifyPropertyChanged(z => z.AllDuplicates);
                    //    counter = 0;
                    //}

                }
                TakeTen();

                if (TruelyAllDuplicates.Count > 9)
                {
                    Disp(() =>
                    {
                        ShowAllResultsText = tenResultsText;
                        Show10Results = true;
                        HasMoreThanTen = true;
                    });
                    NotifyPropertyChanged(z => z.ShowAllResultsText);
                    NotifyPropertyChanged(z => z.HasMoreThanTen);
                    NotifyPropertyChanged(z => z.Show10Results);
                }
                else
                {
                    HasMoreThanTen = false;
                    NotifyPropertyChanged(z => z.HasMoreThanTen);
                }

                //var nulls2 = AllDuplicates.Where(p => p.Value == null).ToList();
                NotifyPropertyChanged(z => z.AllDuplicates);

                Working(false);

            });

            Criteria = new CriteriaSettings()
            {
                DupCriteriaMD5 = true,
                DupCriteriaName = false,
                DupCriteriaSize = false
            };

            CurrentProgress = 0;
            CurrentButtonText = "Start";
            SelectedPath = (startFolder);
            IsWorking = false;
        }

        #endregion

        #region Notifications

        // TODO: Add events to notify the view or obtain data from the view
        public event EventHandler<NotificationEventArgs<Exception>> ErrorNotice;

        #endregion

        #region Properties

        // Add properties using the mvvmprop code snippet

        public FilesView MainUC { get; set; }
        private FilesViewModel _MainUCDataContext;

        private Thread _CurrentThread;


        private int FileCount = 0;
        public string FilesCount { get; private set; }
        public string Status { get; private set; }

        //public Dictionary<string, List<DuplicateFile>> AllDuplicates { get; set; }
        private ObservableCollection<KeyValuePair<string, List<DuplicateFile>>> TruelyAllDuplicates;
        public ObservableCollection<KeyValuePair<string, List<DuplicateFile>>> AllDuplicates { get; set; }
        public int CurrentProgress { get; set; }
        public int ProgressMaximum { get; set; }
        public int ProgressMinimum { get; set; }

        public bool HasMoreThanTen { get; set; }
        public bool Show10Results { get; set; }
        public string ShowAllResultsText { get; set; }
        public CriteriaSettings Criteria { get; set; }

        public string CurrentButtonText { get; set; }

        private string bannerText = "Duplicate Files";
        public string BannerText
        {
            get
            {
                return bannerText;
            }
            set
            {
                bannerText = value;
                NotifyPropertyChanged(m => m.BannerText);
            }
        }

        private string _SelectedPath;
        public string SelectedPath
        {
            get
            {
                return _SelectedPath;
            }
            set
            {
                _SelectedPath = value;
                SetSelectedPath(SelectedPath);
            }
        }

        public string Percentage { get; private set; }
        public bool IsWorking { get; set; }

        #endregion

        #region Methods

        public void Log(string txt)
        {
            Disp(() =>
            {
                if (MainWindow.Main != null)
                    MainWindow.Main.AppendText(txt + Environment.NewLine);

                SetStatus(txt);
            });
        }

        private void SetStatus(string status)
        {
            Status = status;
            NotifyPropertyChanged(x => x.Status);
            NotifyPropertyChanged(x => x.CurrentProgress);
        }

        private void SetStatusCurrentFile(string fullPath = "")
        {
            if (!String.IsNullOrEmpty(fullPath))
            {
                SetStatus("Current file: " + FileCount.ToString() + "/" + FilesCount + ": " + fullPath);
                FileCount++;
                Percentage = string.Format("{0:N2}%", ((double)(((double)FileCount / (double)int.Parse(FilesCount))) * 100));
                NotifyPropertyChanged(x => x.Percentage);
            }
            else
                FileCount = 0;
            CurrentProgress = FileCount;
        }
        private void Working(bool pIsWorking)
        {
            Disp(() =>
            {
                IsWorking = pIsWorking;
                CurrentButtonText = IsWorking ? "Cancel" : "Start";
            });

            NotifyPropertyChanged(x => x.CurrentButtonText);
            NotifyPropertyChanged(x => x.IsWorking);
        }

        public DelegateCommand ShowAllResultsCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    if (Show10Results)
                    {
                        Act(() =>
                        {
                            Disp(() =>
                            {
                                Show10Results = false;
                                ShowAllResultsText = tenResultsTextAlt;
                                HasMoreThanTen = false;
                            });
                            NotifyPropertyChanged(z => z.Show10Results);
                            NotifyPropertyChanged(z => z.ShowAllResultsText);
                            NotifyPropertyChanged(z => z.HasMoreThanTen);

                            Disp(() =>
                            {
                                AllDuplicates = TruelyAllDuplicates;
                            });
                            NotifyPropertyChanged(x => x.AllDuplicates);
                        });
                    }
                    else
                    {
                        Act(() =>
                        {
                            Disp(() =>
                            {
                                Show10Results = true;
                                HasMoreThanTen = true;
                                ShowAllResultsText = tenResultsText;
                            });
                            NotifyPropertyChanged(z => z.Show10Results);
                            NotifyPropertyChanged(z => z.ShowAllResultsText);
                            NotifyPropertyChanged(z => z.HasMoreThanTen);
                            TakeTen();
                        });
                    }
                });
            }
        }
        public DelegateCommand SortByAmountCommand
        {
            get
            {
                return new DelegateCommand(() =>
                   {
                       Sort(TruelyAllDuplicates.OrderByDescending(y => y.Value.Count).ToList());
                   });
            }
        }
        public DelegateCommand SortBySizeCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    Sort(TruelyAllDuplicates.OrderByDescending(y => y.Value[0].FileSize).ToList());
                });
            }
        }


        public DelegateCommand SortByDateCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    Sort(TruelyAllDuplicates.OrderByDescending(y => y.Value[0].FileChanged).ToList());
                });
            }
        }

        private void Sort(List<KeyValuePair<string, List<DuplicateFile>>> sortedList)
        {
            Act(() =>
            {
                if (AllDuplicates == null || AllDuplicates.Count == 0)
                    return;

                var tmp = new ObservableCollection<KeyValuePair<string, List<DuplicateFile>>>();

                foreach (var item in sortedList)
                {
                    tmp.Add(item);
                }

                TruelyAllDuplicates = tmp;
                TakeTen();

            });
        }

        public DelegateCommand ExitCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    Environment.Exit(0);
                });
            }
        }

        public DelegateCommand SaveResultsCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    Act(() =>
                    {
                        var res = Microsoft.VisualBasic.Interaction.InputBox("Enter file name:", "Save Results", FilesViewModel.DefaultDuplicateFilesFilename);

                        if (String.IsNullOrEmpty(res))
                            return;

                        var SerializationObject = new SavedSession() { Duplicates = _MainUCDataContext.AllFiles, SelectedPath = SelectedPath, Settings = Criteria };
                        BinarySerializationAssistor.Serialize(SerializationObject, res);
                        SendMessage(Messages.NotifyString, new NotificationEventArgs("Saved to file: " + res));
                    });
                });
            }
        }

        public DelegateCommand LoadResultsCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    Act(() =>
                    {

                        var res = Microsoft.VisualBasic.Interaction.InputBox("Enter file to load:", "Load saved file", FilesViewModel.DefaultDuplicateFilesFilename);

                        if (String.IsNullOrEmpty(res))
                            return;

                        if (!File.Exists(res))
                        {
                            MessageBox.Show("File \"" + res + "\" does not exist!", "Error - no such file", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        _MainUCDataContext.LoadFromFile(res);
                    });
                });
            }
        }

        public DelegateCommand<DuplicateFile> KeepCommand
        {
            get
            {
                return new DelegateCommand<DuplicateFile>((x) =>
                {
                    Working(true);

                    new Thread(() =>
                    {

                        var key = Criteria.CalculateKey(x);
                        var a1 = AllDuplicates.Where(t => t.Key == key).ToList();
                        var b2 = a1.Select(p => p.Value).ToList();
                        var b3 = b2.First();
                        var b4 = b3.Where(y => y.FullPath != x.FullPath).ToList();

                        //var a = AllDuplicates.Where(z => z.Key == x.MD5).Select(p => p.Value).First().Where(y => y.FullPath != x.FullPath).ToList();
                        var res = MessageBox.Show("The following files will be deleted: " + Environment.NewLine + string.Join(Environment.NewLine, b4.Select(z => z.FullPath)), "Warning - Deleting Files", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                        if (res == MessageBoxResult.OK)
                        {
                            foreach (var b in b4)
                            {
                                File.Delete(b.FullPath);
                                Log("File \"" + b.FullPath + "\" deleted.");
                            }
                        }

                        Disp(() =>
                        {
                            AllDuplicates.Remove(a1[0]);
                            TruelyAllDuplicates.Remove(a1[0]);
                        });

                        NotifyPropertyChanged(xq => xq.AllDuplicates);

                        Working(false);

                    }) { IsBackground = true }.Start();
                });
            }
        }

        public DelegateCommand<DuplicateFile> OpenLocationCommand
        {
            get
            {
                return new DelegateCommand<DuplicateFile>((x) =>
                {
                    new Thread(() =>
                    {
                        Process.Start(Path.GetDirectoryName(x.FullPath));
                    }) { IsBackground = true }.Start();
                });
            }
        }

        public DelegateCommand SelectPath
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    Ookii.Dialogs.Wpf.VistaFolderBrowserDialog dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                    var res = dialog.ShowDialog();
                    if (res.HasValue && res.Value)
                    {
                        SelectedPath = (dialog.SelectedPath);
                    }

                });
            }
        }

        public DelegateCommand<string> EnterSelectedPathCommand
        {
            get
            {
                return new DelegateCommand<string>(x =>
                {
                    SelectedPath = x;
                });
            }
        }
        public DelegateCommand StartCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    if (!IsWorking)
                    {
                        Act(() =>
                        {


                            if (!Directory.Exists(SelectedPath))
                            {
                                MessageBox.Show("Folder \"" + SelectedPath + "\" does not exist!", "Error - no such folder", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            if (!Criteria.DupCriteriaMD5 && !Criteria.DupCriteriaName && !Criteria.DupCriteriaSize)
                            {
                                MessageBox.Show("You must select at least one duplication criteria (MD5, File Name or File Size)", "Error - No Criteria Selected",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            Disp(() =>
                            {
                                AllDuplicates.Clear();
                                HasMoreThanTen = false;
                                Show10Results = false;
                            });
                            NotifyPropertyChanged(x => x.AllDuplicates);
                            NotifyPropertyChanged(x => x.HasMoreThanTen);
                            NotifyPropertyChanged(x => x.Show10Results);

                            FileCount = 0;
                            SetStatusCurrentFile();
                            _MainUCDataContext.SearchForDuplicates(SelectedPath, Criteria);
                        }, "");
                    }
                    else
                    {
                        if (_CurrentThread != null)
                            _CurrentThread.Abort();
                        Working(false);
                        Log("Cancelled.");
                    }

                });
            }
        }

        #endregion

        #region Completion Callbacks

        // TODO: Optionally add callback methods for async calls to the service agent

        #endregion

        #region Helpers

        private void TakeTen()
        {
            Disp(() =>
            {
                AllDuplicates.Clear();
                Show10Results = true;
                HasMoreThanTen = true;
                ShowAllResultsText = tenResultsText;
                NotifyPropertyChanged(x => x.Show10Results);
                NotifyPropertyChanged(x => x.HasMoreThanTen);
                NotifyPropertyChanged(x => x.ShowAllResultsText);
            });
            var firstTen = TruelyAllDuplicates.Take(10).ToList();
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var a in firstTen)
                    AllDuplicates.Add(a);
            }));
            NotifyPropertyChanged(z => z.AllDuplicates);
        }

        private void Disp(Action x)
        {
            // Always fire the event on the UI thread
            if (Application.Current.Dispatcher.CheckAccess())
            {
                x();

            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => x()));
            }
        }

        private void Act(Action x, string Message = "")
        {
           
            _CurrentThread = new Thread(() =>
            {
                try
                {
                    Working(true);
                    Log(String.Format("Performing Operation {0}...", Message));
                    x();
                }
                catch (Exception ex)
                {
                    if (!(ex is ThreadAbortException))
                        Log(String.Format("Exception Occured:{0}{1}{0}{2}", Environment.NewLine,
                            ex.Message, ex.StackTrace));
                }
                finally { Working(false); Log("Done."); }
            }) { IsBackground = true };

            _CurrentThread.Start();

        }

        private void SetSelectedPath(string path)
        {
            if (!Directory.Exists(path))
            {
                MessageBox.Show("Folder \"" + path + "\" does not exist!", "Error - no such folder", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //SelectedPath = path;
            NotifyPropertyChanged(x => x.SelectedPath);
            Log("Selected path changed: " + path);
            Working(true);
            NotifyPropertyChanged(x => x.IsWorking);
            SendMessage(Messages.GetFiles, new NotificationEventArgs(SelectedPath));
        }
        // Helper method to notify View of an error
        private void NotifyError(string message, Exception error)
        {
            // Notify view of an error
            Notify(ErrorNotice, new NotificationEventArgs<Exception>(message, error));
        }

        #endregion
    }
}