using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DuplicateFiles.Models
{
    [Serializable]
    public class CriteriaSettings
    {
        public bool DupCriteriaName { get; set; }
        public bool DupCriteriaSize { get; set; }

        public bool DupCriteriaMD5 { get; set; }

        public string CalculateKey(DuplicateFile file)
        {
            StringBuilder toReturn = new StringBuilder();
            if (DupCriteriaMD5)
                toReturn.Append(file.MD5);
            if (DupCriteriaName)
                toReturn.Append(file.FileName);
            if (DupCriteriaSize)
                toReturn.Append(file.FileSize.ToString());

            return toReturn.ToString();
        }
    }
}
