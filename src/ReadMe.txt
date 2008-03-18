micado README

micado is developed using Visual Studio. 
Open the solution BiostreamSolution.sln in Visual Studio to access all parts of the project.

*** PROJECTS ***

The solutions contain the following projects:
- BioStreamFS
  This project in F# provides most higher-level functionality, including most user-end AutoCAD commands.
  
  The BioStreamFS contains the scripts
  
  * build.bat -- which generates the developer-level documentation 
                (and incidentally, the .dll too, which is handy sometimes 
                 when Visual Studio doesn't report certain errors or warnings.)
                 
  * UpdateBuild.py -- which is a very elementary script to update the list of source files in build.bat
                in accordance with the BioStreamFS.fsharpp project.
                (Remember that F# requires the source files to be listed in order of dependencies.)
                Run it if you add files to the BioStreamFS project.
                  
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

You can run micado directly from the Visual Studio debugger by attaching AutoCAD as an external program.

First, generate micado-debug.scr with the python script bin\GenerateScrLoader.py.
* From a command prompt, cd to the bin directory, and type:
  python GenerateScrLoader.py --help
  to get help on running the script.
* If you just type
  python GenerateScrLoader.py
  micado-debug.scr will be generated in C:\Program Files\Autodesk\Acade 2008\UserDataCache\

If you generated micado-debug.scr in the default location, you're all set.
Just make sure that BioStreamFS is the default startup project.

To configure a project as the default startup project, 
Right-click on it in the Solution Explorer and then click on Set as StartUp Project.

If you generated micado-debug.scr in a custom location, 
configure BioStreamCS as the default startup project and set the debugging options as follows:

- Right-Click on the BioStreamCS project & select Properties. Click the Debug tab.

- Choose the option Start external program, and put in the path to your AutoCAD application.
  (e.g. C:\Program Files\Autodesk\Acade 2008\acad.exe)
  
- In Start Options, add the following command line arguments
  /b  "GENERATE_DIRECTORY\micado-debug"
  where GENERATE_DIRECTORY is the directory in which you generated micado-debug.scr
  
- For Working Directory, typically use GENERATE_DIRECTORY as well.
