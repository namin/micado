// Setup variables
var appName = "micado";
var appPrettyName = "Micado";

var cui = appName + ".cui";
var cuiOrig = cui + ".orig";

var appScr = appName;
var scr = appScr + ".scr";
var scrOrig = scr + ".orig";

var defaultDir = "C:\\" + appName;

// the path of the directory relative to the AutoCAD directory, where the AutoCAD shortcut should be started in
var autocadStartRelativeDir = "UserDataCache";

// the registry key containing the path to the AutoCAD program
var autocadKey = "HKEY_CLASSES_ROOT\\AutoCAD.Drawing.17\\protocol\\StdFileEditing\\server\\";

// Common utilities
var Shell = new ActiveXObject("WScript.Shell");
var ForReading = 1;
var ForWriting = 2;
var File = WScript.CreateObject("Scripting.FileSystemObject");

// Creates file newFile as a copy of file oldFile with all instances of oldStr replaced by newStr
function replaceSave(oldFile, newFile, oldStr, newStr) {
	var fr = File.OpenTextFile(oldFile, ForReading);
	oldText = fr.ReadAll();
	fr.close();
	
	newText = oldText.replace(new RegExp(oldStr, "g"), newStr);
	
	var fw = File.OpenTextFile(newFile, ForWriting, true);
	fw.Write(newText);
	fw.close();
	WScript.Echo("Created file " + newFile);
}

// Replaces all instances of backward slahes \ to forward slahes /
function forwardSlashes(path) {
	return path.replace(/\\/g, "/");
}
// Double the backward slashes so they can be used in regular expressions
function doubleBackwardSlashes(path) {
	return path.replace(/\\/g, "\\\\");
}

// returns the current directory
function getCurrentDirectory() {
	return File.GetAbsolutePathName(".");
}

// Script

var currentDir = getCurrentDirectory();
WScript.Echo(appPrettyName + " installation directory is " + currentDir);

replaceSave(scrOrig, scr, forwardSlashes(defaultDir), forwardSlashes(currentDir));
replaceSave(cuiOrig, cui, doubleBackwardSlashes(defaultDir), currentDir);

var autocadProgram = Shell.RegRead(autocadKey);
WScript.Echo("AutoCAD program is " + autocadProgram);

var autocadDir = File.GetParentFolderName(autocadProgram);
var autocadStartDir = autocadDir + "\\" + autocadStartRelativeDir;

if (!(File.FolderExists(autocadStartDir))) {
	autocadStarDir = autocadDir;
}

WScript.Echo("AutoCAD starting directory is " + autocadStartDir);

var appLink = appName + ".lnk";

var link = Shell.CreateShortcut(appLink);
link.Arguments = "/b \"" + currentDir + "\\" + appScr + "\"";
link.Description = "Launch AutoCAD with " + appPrettyName + " Plug-In";
link.TargetPath = autocadProgram;
link.WorkingDirectory = autocadStartDir;
link.Save();

WScript.Echo("Created " + appLink);