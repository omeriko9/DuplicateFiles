using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace DuplicateFiles.Models
{
    public class DuplicateCollector
    {
        public event Action<string> FileAdded;
        public event Action<List<DuplicateFile>> DuplicateFileFound;

        public SerializableDictionary<string, List<DuplicateFile>> AllFiles { get; set; }

        public Dictionary<string, List<DuplicateFile>> DuplicateByFolder = new Dictionary<string, List<DuplicateFile>>();

        public Dictionary<string, List<DuplicateFile>> AllDuplicated = new Dictionary<string, List<DuplicateFile>>();

        CriteriaSettings _Criteria;

        string _RootFolder;
        long _CountAllFiles;

        public DuplicateCollector()
        {
            AllFiles = new SerializableDictionary<string, List<DuplicateFile>>();
        }

        public DuplicateCollector(string pRootFolder, CriteriaSettings pCriteria)
            : this()
        {
            _RootFolder = pRootFolder;
            _Criteria = pCriteria;
        }


        public string LoadFromFile(string filePath)
        {
            var saved = BinarySerializationAssistor.Deserialize(filePath);
            AllFiles = saved.Duplicates;
            _Criteria = saved.Settings;
            return saved.SelectedPath;
            //SerializableDictionary<string, List<DuplicateFile>> tmp = new SerializableDictionary<string, List<DuplicateFile>>();
            //tmp.ReadXml(XmlReader.Create(new FileStream(filePath, FileMode.Open)));
            //Duplicates = tmp;
        }


        public long CountAllFiles()
        {
            return (_CountAllFiles = Directory.EnumerateFiles(_RootFolder, "*.*", SearchOption.AllDirectories).Count());
        }


        public SerializableDictionary<string, List<DuplicateFile>> ScanFileInfo()
        {
            ScanFileInfo(_RootFolder);
            return AllFiles;
        }
        private void ScanFileInfo(string folder)
        {
            var dirInfo = new DirectoryInfo(folder);

            foreach (var file in dirInfo.GetFiles())
            {
                try
                {
                    var item = new DuplicateFile
                    {
                        FileName = file.Name,
                        FullPath = file.FullName,
                        FileSize = file.Length,
                        FileChanged = file.LastWriteTime,
                        FileCreated = file.CreationTime
                    };
                    if (_Criteria.DupCriteriaMD5)
                    {
                        item.MD5 = GetFileMD5(item.FullPath);
                    }

                    string key = _Criteria.CalculateKey(item);

                    if (AllFiles.ContainsKey(key))
                    {
                        AllFiles[key].Add(item);
                    }
                    else
                    {
                        AllFiles.Add(key, new List<DuplicateFile>() { item });
                    }

                    if (FileAdded != null)
                        FileAdded(item.FullPath);

                    var fileFolder = Path.GetDirectoryName(item.FullPath);

                    if (DuplicateByFolder.ContainsKey(fileFolder))
                    {
                        DuplicateByFolder[fileFolder].Add(item);
                    }
                    else
                    {
                        DuplicateByFolder.Add(fileFolder, new List<DuplicateFile>() { item });
                    }
                }
                catch (Exception ex)
                {

                }
            }

            foreach (var directory in dirInfo.GetDirectories())
            {
                ScanFileInfo(directory.FullName);
            }
        }

        private string GetFileMD5(string fileFullPath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileFullPath))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }

        public void CalculateDuplicate()
        {
            // var nulls = Duplicates.Where(x => x.Value == null).ToList();
            // var p = 123;

            //Parallel.ForEach(Duplicates, x =>
            foreach (var x in AllFiles)
            {
                if (x.Value.Count <= 1)
                    continue;

                if (DuplicateFileFound != null)
                    DuplicateFileFound(x.Value);

                if (x.Value.Count > 1)
                    AllDuplicated.Add(x.Key, x.Value);

            }
        }


    }

    public class DuplicateFile
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public long FileSize { get; set; }

        [XmlIgnore]
        public string FileSizeString
        {
            get
            {
                return (string.Format("{0:N3}kb",
                    (double)((double)FileSize / (double)1000)).ToString());
            }
        }
        public DateTime FileCreated { get; set; }
        public DateTime FileChanged { get; set; }

        public string MD5 { get; set; }
    }
}
