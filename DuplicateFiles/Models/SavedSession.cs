using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DuplicateFiles.Models
{
    [Serializable]
    public class SavedSession
    {

        public string SelectedPath { get; set; }

        public CriteriaSettings Settings { get; set; }

        public SerializableDictionary<string, List<DuplicateFile>> Duplicates { get; set; }


    }
}
