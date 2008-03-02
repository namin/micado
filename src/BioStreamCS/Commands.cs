using System;
using System.Collections.Generic;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace BioStreamCS
{
    public class Commands
    {
        [CommandMethod("HelloWorldCs")]
        static public void HelloWorldCs()
        {
            Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;
            editor.WriteMessage("Hello World!");
        }
    }
}
