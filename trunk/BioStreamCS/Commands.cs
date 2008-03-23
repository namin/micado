using System;
using System.Collections.Generic;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using System.Runtime.InteropServices; 

namespace BioStream.Micado.User
{
    public class Commands
    {
        [DllImport("acad.exe", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?acgsRemoveAnonymousGraphics@@YAHH@Z")]
        extern public static int acgsRemoveAnonymousGraphics(int vportNum);
        [CommandMethod("micado_clear_marks")]
        static public void ClearMarks()
        {
            acgsRemoveAnonymousGraphics(Convert.ToInt32(Application.GetSystemVariable("CVPORT")));
        }

        private static SettingsUI settingsUI = new SettingsUI();

        [CommandMethod("MicadoSettings")]
        static public void MicadoSettings()
        {
            settingsUI.ShowDialog();
        }
    }
}
