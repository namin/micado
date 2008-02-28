micado README

micado is developed using Visual Studio. 
Open the solution BiostreamSolution.sln in Visual Studio to access all parts of the project.

*** PROJECTS ***

The solutions contain the following projects:
- BioStreamFS
  This project in F# provides most higher-level functionality, including most user-end AutoCAD commands.
  
- BioStreamCS
  This project in C# provides the user-end UIs & settings, and various internal helpers.
  
- BioStreamDB
  This project in C++ provides custom AutoCAD objects for the microfluidics primitives valves and punches.

- BioStreamMg
  This project provides managed wrappers around the custom AutoCAD objects of BioStreamDB.
  
- MgCS2  
  Min-Cost Flow Library
  The project provides a simple managed wrapper around 
  a (single-commodity) (linear) Min Cost Flow (MCF) problem solver, 
  implemented using an efficient Cost-Scaling, Push-Relabel algorithm,
  obtained from http://www.di.unipi.it/optimize/Software/MCF.html#CS2
  
- PluginTesting
  This project provides additional AutoCAD commands for testing purposes.

*** RUNNING ***

You can run micado directly from the Visual Studio debugger by attaching AutoCAD as an external program:

- Right-Click on the BioStreamCS project & select Properties. Click the Debug tab.

- Choose the option Start external program, and put in the path to your AutoCAD application.
  (e.g. C:\Program Files\Autodesk\Acade 2008\acad.exe)
  
- In Start Options, add the following command line arguments
  (replacing MICADO-DIR with the actual full path to your minicad source directory).
  /ld MICADO-DIR\src\debug\BioStreamDB.dbx" /b  MICADO-DIR\src\debug\micado"
  You'll also need to change all the full paths in the file ..\src\debug\micado.scr.
  This will load all the necessary minicad extensions. 
  
- For Working Directory, I have C:\Program Files\Autodesk\Acade 2008\UserDataCache\
