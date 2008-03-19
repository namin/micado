using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace BioStream.Micado.User
{
    [XmlRootAttribute(ElementName = "Settings", IsNullable = false)]
    public class Settings
    {

        private static string CurrentFilePath =
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
          + "\\micado\\current-settings.xml";

        public static Settings Current = ImportCurrentSettings();

        /// <summary>
        /// To convert a Byte Array of Unicode values (UTF-8 encoded) to a complete String.
        /// </summary>
        /// <param name="characters">Unicode Byte Array to be converted to String</param>
        /// <returns>String converted from Unicode Byte Array</returns>
        private static String UTF8ByteArrayToString(Byte[] characters)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            String constructedString = encoding.GetString(characters);
            return (constructedString);
        }

        /// <summary>
        /// Converts the String to UTF8 Byte array and is used in De serialization
        /// </summary>
        /// <param name="pXmlString"></param>
        /// <returns></returns>
        private static Byte[] StringToUTF8ByteArray(String pXmlString)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            Byte[] byteArray = encoding.GetBytes(pXmlString);
            return byteArray;
        }

        /// <summary>
        /// Method to convert a custom Object to XML string
        /// </summary>
        /// <param name="pObject">Object that is to be serialized to XML</param>
        /// <returns>XML string</returns>
        private static String SerializeObject(Object pObject)
        {
            try
            {
                String XmlizedString = null;
                MemoryStream memoryStream = new MemoryStream();
                XmlSerializer xs = new XmlSerializer(typeof(Settings));
                XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);

                xs.Serialize(xmlTextWriter, pObject);
                memoryStream = (MemoryStream)xmlTextWriter.BaseStream;
                XmlizedString = UTF8ByteArrayToString(memoryStream.ToArray());
                return XmlizedString;
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e);
                return null;
            }
        }

        /// <summary>
        /// Method to reconstruct an Object from XML string
        /// </summary>
        /// <param name="pXmlizedString"></param>
        /// <returns></returns>
        private static Object DeserializeObject(String pXmlizedString)
        {
            XmlSerializer xs = new XmlSerializer(typeof(Settings));
            MemoryStream memoryStream = new MemoryStream(StringToUTF8ByteArray(pXmlizedString));
            XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);

            return xs.Deserialize(memoryStream);
        }

        internal static void ExportSettings(Settings settings, string filepath)
        {
            StreamWriter SW = File.CreateText(filepath);
            SW.Write(SerializeObject(settings));
            SW.Close();
        }

        internal static Settings ImportSettings(string filepath)
        {
            StreamReader SR = File.OpenText(filepath);
            String str = SR.ReadToEnd();
            SR.Close();
            return (Settings)DeserializeObject(str);
        }

        internal static void ExportCurrentSettings(Settings settings)
        {
            FileInfo currentFileInfo = new FileInfo(CurrentFilePath);
            if (!currentFileInfo.Directory.Exists)
            {
                Directory.CreateDirectory(currentFileInfo.DirectoryName);
            }
            ExportSettings(settings, CurrentFilePath);
        }

        internal static Settings ImportCurrentSettings()
        {
            if (File.Exists(CurrentFilePath))
            {
                return ImportSettings(CurrentFilePath);
            }
            else
            {
                return new Settings();
            }
        }


        private string[] flowLayers = new string[] {"Flow"};
        private string[] controlLayers = new string[] {"Control"};

        private int punchBarNumber = 8;
        private double punchBarWidth = 0.12;
        private double punchRadius = 0.4;

        private double valveRelativeWidth = 1.5;
        private double valveRelativeHeight = 1.0;

        private double resolution = 0.300;
        private double connectionWidth = 0.05;
        private double flowExtraWidth = 0.08;
        private double valveExtraWidth = 0.08;
        private double controlLineExtraWidth = 0.08;
        private double punch2Line = 1.0;


        public string[] FlowLayers
        {
            get
            {
                return flowLayers;
            }
            set
            {
                flowLayers = value;
            }
        }

        public string[] ControlLayers
        {
            get
            {
                return controlLayers;
            }
            set
            {
                controlLayers = value;
            }
        }

        public int PunchBarNumber
        {
            get
            {
                return punchBarNumber;
            }
            set
            {
                punchBarNumber = value;
            }
        }

        public double PunchBarWidth
        {
            get
            {
                return punchBarWidth;
            }
            set
            {
                punchBarWidth = value;
            }
        }

        public double PunchRadius
        {
            get
            {
                return punchRadius;
            }
            set
            {
                punchRadius = value;
            }
        }

        public double ValveRelativeWidth
        {
            get
            {
                return valveRelativeWidth;
            }
            set
            {
                valveRelativeWidth = value;
            }
        }

        public double ValveRelativeHeight
        {
            get
            {
                return valveRelativeHeight;
            }
            set
            {
                valveRelativeHeight = value;
            }
        }

        public double Resolution
        {
            get
            {
                return resolution;
            }
            set
            {
                resolution = value;
            }
        }

        public double ConnectionWidth
        {
            get
            {
                return connectionWidth;
            }
            set
            {
                connectionWidth = value;
            }
        }

        public double FlowExtraWidth
        {
            get
            {
                return flowExtraWidth;
            }
            set
            {
                flowExtraWidth = value;
            }
        }

        public double ValveExtraWidth
        {
            get
            {
                return valveExtraWidth;
            }
            set
            {
                valveExtraWidth = value;
            }
        }

        public double ControlLineExtraWidth
        {
            get
            {
                return controlLineExtraWidth;
            }
            set
            {
                controlLineExtraWidth = value;
            }
        }

        public double Punch2Line
        {
            get
            {
                return punch2Line;
            }
            set
            {
                punch2Line = value;
            }
        }

    }
}
