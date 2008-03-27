===========
Quick Steps
===========

Here are the steps to create a GUI application called AppName:

1. In Micado, export to a GUI, saving the PNG image as AppName.png and the Java data file as AppName.dat, in this directory.

2. In a command prompt, cd to this directory, and type:
   build.bat AppName
   This script will launch the GUI.
   If necessary, it will generate and compile all the *.java files using the application Template as a base.

=======
Details
=======

This remaining of this file contains directions on how to update a microfluidics GUI.

I. REQUIRED SOFTWARE
--------------------

You will need to install the following software to use the program:

1. Java JDK
   http://java.sun.com/javase/downloads/index_jdk5.jsp

2. If using the Carl Zeiss microscope, may need Carl Zeiss program (?)

3. Python
   http://www.python.org/download/
   
For the software to run correctly, do the following (one-time) setup:

1. Find the "ij.jar" file at "..\lib\ij.jar".  
   Add this to the Java
   CLASSPATH environment variable as follows:
   - Right-click on My Computer
   - Click "Properties", then "Advanced", then "Environment Variables"
   - If there is a "CLASSPATH" variable in the top list, select it and
     "Edit" it, otherwise make a new variable by this name
   - set the variables value to be something like:
     .;c:\j2sdk1.5.2_05\jre\lib\rt.jar;c:\micado\javagui\lib\ij.jar
	 
     where "j2sdk1.5.2_05" is replaced with the name of your Java
     directory and "c:\micado" is replaced with your path to the
     micado directory.

2. Possibly configure Universal Library or Carl Zeiss -- TBD

II. JAVA BASICS
---------------

It will be useful to understand some basics about Java.  Running a
Java program is a two-step process:

1. Compile the source (.java) file into bytecodes (.class files):
   javac -source 1.4 -nowarn Template*.java MITNative.java

   You have to recompile whenever you modify the program.  This is the
   step where any errors are detected in the code.

2. Run the bytecodes in the Java virtual machine:
   java TemplateMain

III. Generate GUI with python script MakeApp.py

In a command prompt, type
python MakeApp.py
for usage information.

This python script is aimed to be used in conjunction with the export to GUI feature of Micado.

IV. OVERVIEW OF THE SOURCE CODE
--------------------------------

The program is split into a few files:

TemplateMain
 - the toplevel file to call
 - can optionally execute scripts (measuring a gradient, etc.)

TemplateGUI
 - draws and updates the GUI on the screen
 - keeps track of which valves are in auto-toggle mode
 - translates mouse clicks to either individual valve toggles or
   calls to the TemplateDriver (for composite operations)

TemplateDriver 
 - translates composite operations ( mixing, storing, etc.) into
   a sequence of underlying valve actuations

TemplateMapping
 - maps the logical ports on the chip to the physical ports on the
   card

TemplateImaging
 - performs periodic image capture using Carl Zeiss microscope utility
 - always writes images to disk and deletes them if not needed
 - a call to doMeasurement() causes an image to be preserved


V. UPDATING THE GUI BY HAND
---------------------------

1. Since you usually want to preserve the old GUI, copy the old files
   into new files.  This can be done like this:

   copy Template*.java NewName*.java

   where "NewName" is the name of your new version (e.g., "Oocyte4").

2. Search and replace "Template" to "NewName" within each of the files.

3. Use MS Paint or equivalent to get a PNG image of your chip and any
   desired annotations.  (You can hit CTRL-PRINTSCREEN to get a screen
   capture from Adobe Illustrator, then paste into Paint and edit).
   Save the PNG image in the same directory as the .java files.

4. Modify the top of NewNameGUI.java to indicate the name and
   dimensions of the PNG image.

5. At the top of NewNameGUI.java, set RECORDING_MODE = true.

6. Recompile and run the application:

   javac NewName*.java
   java NewNameMain

7. Use the mouse to indicate the desired locations for actuating
   valves and valve configurations.  The interface allows multiple
   locations to be associated with a given configuration, or "index".
   Each left click will add a new location to the current index, while
   a right click will advance to the next index.  Any click within a
   given index will have the same effect in the GUI.

   For example, first use the left mouse button to indicate all valves
   for control line 1.  Then right click to advance to control line 2.

   The program will output an array listing to the screen.
   Cut-and-paste this listing into the indicated section at the top of
   NewName.java.

8. If you are adding or removing a composite operation (e.g., mixing,
   loading, storing, etc.) then do the following:

   A. Adjust the index constants immediately below the array you just
      pasted into NewNameGUI.java.  These indices refer to the start
      of implicit boundaries in the array that you just created.  For
      example, the first 10 indices might refer to control lines; the
      next 8 might refer to storage cells, and so on.  You can name
      these boundaries whatever you want -- they just make the next
      step easier.

   B. Adjust the "processLeftClick" method at the bottom of TemplateGUI
      to carry out the behavior of clicking at a given index.  The
      "index" parameter indicates that a given index in your array was
      clicked.  You should test what category the index is in, and
      dispatch to the appropriate implementation.

9. Open NewNameDriver.java and adjust the following:

   - CONTROL_LINES, STORAGE_CELLS, INPUTS, REVERSE_OPS and MIX_PATH

   - The implementation of all the procedures in the "BASIC OPS"
     category.  These indicate which ports to open and close to carry
     out various configurations.

10. All done!  But before running it on a device, you should test your
    setup by setting SIMULATION_MODE=true in NewNameDriver (also don't
    forget to reset RECORDING_MODE=false in NewNameGUI).

    Under the simulation mode, the GUI will show which valves are
    being toggled, but will not output any commands to the actual
    chip.

    You might also benefit from FAST_MODE (a constant in
    NewNameDriver) to speedup the delays in your simulation.
