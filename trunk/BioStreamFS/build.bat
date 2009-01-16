@ECHO OFF
SET param=%1
IF (%param%) == (clean) GOTO CLEAN

:BUILD
IF NOT EXIST biostreamfs mkdir biostreamfs
"C:\Program Files\FSharp-1.9.6.2\bin\fsc.exe" -o biostreamfs.dll --doc biostreamfs.xml --generatehtml --htmlcss "msdn.css" -a -g --noframework -r C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\Accessibility.dll -r ..\debug\acdbmgd.dll -r ..\debug\acmgd.dll -r ..\debug\BioStreamCS.dll -r ..\debug\BioStreamMg.dll -r "C:\Program Files\FSharp-1.9.6.2\\bin\FSharp.PowerPack.dll" -r ..\debug\MgCS2.dll -r C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\Microsoft.Build.Framework.dll -r C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\Microsoft.Build.Utilities.dll -r C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\Microsoft.VisualC.dll -r C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\System.Configuration.dll -r C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\System.Deployment.dll -r C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\System.dll -r C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\System.Drawing.dll -r C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\System.Runtime.Serialization.Formatters.Soap.dll -r C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\System.Security.dll -r C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\System.Windows.Forms.dll --target library --warn 3 --warnaserror 76 --vserrors --fullpaths graph.fs geometry.fs datatypes.fs flow.fs creation.fs database.fs editor.fs bridge.fs chip.fs routing.fs flow-representation.fs instructions.fs control-inference.fs debug.fs export-gui.fs commands.fs legacy.fs 
GOTO DONE

:CLEAN
IF EXIST biostreamfs.dll del biostreamfs.dll
IF EXIST biostreamfs.pdb del biostreamfs.pdb
IF EXIST biostreamfs\BioStream.Micado.Plugin.html del biostreamfs\*.html
IF EXIST namespaces.html del namespaces.html

:DONE


