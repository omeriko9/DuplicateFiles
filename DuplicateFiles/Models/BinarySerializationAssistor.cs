using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace DuplicateFiles.Models
{
    internal class BinarySerializationAssistor
    {
        public static void Serialize(SavedSession Session, string fileName)
        {
            // SerializableDictionary<string, List<DuplicateFile>> obj
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(SavedSession),
                                 new XmlRootAttribute() { ElementName = "Saved_Session" });

                StringBuilder sb = new StringBuilder();
                using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings() { ConformanceLevel = ConformanceLevel.Auto }))
                {
                    serializer.Serialize(writer, Session);
                    writer.Flush();
                }

                File.WriteAllText(fileName, sb.ToString());

            }
            catch (Exception ex)
            {

            }

        }

        public static SavedSession Deserialize(string fileName)
        {
            SavedSession toReturn = new SavedSession();

            var str = new StringReader(File.ReadAllText(fileName));

            XmlSerializer serializer = new XmlSerializer(typeof(SavedSession),
                                 new XmlRootAttribute() { ElementName = "Saved_Session" });

            using (XmlReader reader = XmlReader.Create(str))
            {
                toReturn = (SavedSession)serializer.Deserialize(reader);
            }

            return toReturn;
        }
    }
}
