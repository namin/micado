micado README

micado is developed using Visual Studio. 
Open the solution BiostreamSolution.sln in Visual Studio to access all parts of the project.

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