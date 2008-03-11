using System;
using System.Collections.Generic;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace BioStream.Micado.User
{
    public class Commands
    {
        private static SettingsUI settingsUI = new SettingsUI();

        [CommandMethod("MicadoSettings")]
        static public void MicadoSettings()
        {
            settingsUI.ShowDialog();
        }
    }
}
