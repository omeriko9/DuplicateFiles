using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateFiles.Models
{
    public class ItemProvider : INotifyPropertyChanged
    {
        public ObservableCollection<Item> Items { get; set; }

        public ItemProvider()
        {
            Items = new ObservableCollection<Item>();
        }

        public ItemProvider(string Path)
            : base()
        {
            GetItems(Path);
        }


        public void GetItems(string path)
        {
            Items = new ObservableCollection<Item>(GetItems(path, 0));
        }

        public List<Item> GetItems(string path, int depth)
        {
            if (depth == 3)
                return default(List<Item>);

            var items = new List<Item>();

            var dirInfo = new DirectoryInfo(path);

            foreach (var directory in dirInfo.GetDirectories())
            {
                try
                {
                    var item = new DirectoryItem
                    {
                        Name = directory.Name,
                        Path = directory.FullName,
                        Items = GetItems(directory.FullName, depth + 1)
                    };

                    items.Add(item);

                    OnPropertyChanged("Items");

                }
                catch (Exception ex)
                {

                }
            }

            //if (depth == 0)
            //    foreach (var file in dirInfo.GetFiles())
            //    {
            //        try
            //        {
            //            var item = new FileItem
            //            {
            //                Name = file.Name,
            //                Path = file.FullName
            //            };

            //            items.Add(item);
            //            // OnPropertyChanged("Items");
            //        }
            //        catch (Exception ex)
            //        {

            //        }
            //    }

            return items;
        }

        private void OnPropertyChanged(params string[] props)
        {
            foreach (var prop in props)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
