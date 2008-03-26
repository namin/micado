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

        private static String SerializeObject(Object pObject)
        {
            return BioStream.Micado.Common.Serialization.SerializeObject(typeof(Settings), pObject);
        }

        private static Object DeserializeObject(String pXmlizedString)
        {
            return BioStream.Micado.Common.Serialization.DeserializeObject(typeof(Settings), pXmlizedString);
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
