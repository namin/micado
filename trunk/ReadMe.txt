micado src README

=============
Visual Studio
=============

Micado is developed using Visual Studio. 
Open the solution BiostreamSolution.sln    in Visual Studio 2008 to access the managed   projects
 and the solution BioStreamSolutionCpp.sln in Visual Studio 2005 to access the unmanaged projects.
The managed projects depend on the unmanaged projects.

If your path to AutoCAD is not at C:\Program Files\Autodesk\Acade 2008,
you might need to adjust the links to the acdbmgd.dll and acmgd.dll.

------------
VS Projects
------------
The solutions contain the following projects:

- ***BioStreamFS***
  This project in F# provides most higher-level functionality, including most user-end AutoCAD commands.
                
  For F# Interactive, this is how I load the AutoCAD assemblies and the other Micado assemblies:
  
  #I @"C:\Program Files\Autodesk\Acade 2008" // the path on your system may vary
  #r "acdbmgd.dll"
  #r "acmgd.dll"

  #I @"C:\Documents and Settings\Nada AMIN\Mes documents\projects\micado\trunk\debug" // your path to MICADO_CHECKOUT\trunk\debug
  #r "MgCS2.dll"
  #r "BioStreamMg.dll"
  #r "BioStreamCS.dll"
  #r "biostreamfs.dll"
                
- ***BioStreamCS***
  This project in C# provides the user-end UIs & settings.
  
- ***BioStreamDB***
  This project in C++ provides custom AutoCAD objects for the microfluidics primitives valves and punches.

- ***BioStreamMg***
  This project provides managed wrappers around the custom AutoCAD objects of BioStreamDB.
  
- ***MgCS2***  
  Min-Cost Flow Library
  The project provides a simple managed wrapper around 
  a (single-commodity) (linear) Min Cost Flow (MCF) problem solver, 
  implemented using an efficient Cost-Scaling, Push-Relabel algorithm,
  obtained from http://www.di.unipi.it/optimize/Software/MCF.html#CS2
  
- ***PluginTesting***
  This project provides additional AutoCAD commands for testing purposes.
  The commands are very useful for debugging, but this assembly should typically not be part of a public release.

--------------------
Running / Debugging
--------------------
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

(TODO: This tip doesn't quite seem to work. Investigate.

If you generated micado-debug.scr in a custom location, 
configure BioStreamCS as the default startup project and set the debugging options as follows:

- Right-Click on the BioStreamCS project & select Properties. Click the Debug tab.

- Choose the option Start external program, and put in the path to your AutoCAD application.
  (e.g. C:\Program Files\Autodesk\Acade 2008\acad.exe)
  
- In Start Options, add the following command line arguments
  /b  "GENERATE_DIRECTORY\micado-debug"
  where GENERATE_DIRECTORY is the directory in which you generated micado-debug.scr
  
- For Working Directory, typically use GENERATE_DIRECTORY as well.

)

------------------
Creating a Release
------------------
(Note: this release process could be automated by a script.)

The src\micado-pub directory contains the basis for a release.

* Simply compile the source code in the Release configuration,
  adding the produced assemblies to a copy of micado-pub.

* You'll also need to generate the .src file
  by running the python script bin\GenerateScrLoader.py.

A release can be installed as documented in \src\micado-pub\INSTALL.txt

----------------
Lessons Learned
----------------

* LIFETIME MANAGEMENT ISSUE OF DBOBJECTS

  It is imperative to Dispose of all DBObjects NOT open in a transaction
  (DBObjects added to the database counts as open). Failing to do so
  results in heisenbugs where AutoCAD fails with a read/write access error.
  
  (It is optional to Dispose of DBObjects open in a transaction, but when I wasn't sure
  I went ahead and made some datatypes, like Chip, IDisposable just to make sure they'd 
  Dispose of their DBObjects properly. Though in hindsight it isn't necessarily, I suppose
  it can only improve performance.)
  
  See SVN r128 to r138.
  
  