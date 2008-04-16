@ECHO OFF
SET param=%1
IF (%param%) == (clean) GOTO CLEAN

:BUILD
IF NOT EXIST biostreamfs mkdir biostreamfs
fsc -o biostreamfs.dll -doc biostreamfs.xml --generate-html --html-css "msdn.css" --fullpaths --progress -a -Ooff -g -I "C:\Program Files\Autodesk\Acade 2008" -r acdbmgd.dll -r "acmgd.dll" -I "..\debug" -r "MgCS2.dll" -r "BioStreamMg.dll" -r "BioStreamCS.dll" graph.fs field-converters.fs csv.fs geometry.fs datatypes.fs flow.fs creation.fs database.fs editor.fs bridge.fs chip.fs routing.fs flow-representation.fs instructions.fs control-inference.fs debug.fs export-gui.fs commands.fs legacy.fs biostreamfs.fs
GOTO DONE

:CLEAN
IF EXIST biostreamfs.dll del biostreamfs.dll
IF EXIST biostreamfs.pdb del biostreamfs.pdb
IF EXIST biostreamfs\BioStream.Micado.Plugin.html del biostreamfs\*.html
IF EXIST namespaces.html del namespaces.html

:DONE


