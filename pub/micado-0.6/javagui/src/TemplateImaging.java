import java.awt.*;
import java.io.*;
import java.util.*;

import ij.plugin.*;
import ij.*;
import ij.io.*;
import ij.process.*;
import ij.gui.*;
import ij.measure.*;

// CURRENT LIMITATION: requires image filenames to have same number of
// characters.  otherwise the lexicographic sorting might not be
// chronological sorting.
public class TemplateImaging implements PlugIn {
    // directory where images are dumped
    static final String IMAGE_DIR = "C:\\Documents and Settings\\JP\\My Documents\\oocyte\\07-03-16-nadh-loading";
    // prefix of raw image files
    static final String RAW_IMAGE_PREFIX = "RAW_";
    // prefix of measured image files
    static final String MEASURED_IMAGE_PREFIX = "MEASURED_";
    // suffix of images we're dealing with
    static final String IMAGE_SUFFIX = ".TIF";
    // exposure delay -- number of seconds between scope opening and
    // taking a picture, to allow time for camera to adjust contrast
    static final double EXPOSURE_DELAY = 2;
    // whether or not to beep when a picture is taken
    static final boolean BEEP_ON_CAPTURE = true;

    // how many files we've processed
    static int NUM_MEASURED = 0;
    // person holding the lock can delete images
    Object lock = new Object();

    // dummy method needed to implement an ImageJ plugin
    public void run(String str) {}

    // Takes a measurement on a new raw image saved in IMAGE_DIR.
    // Prints the measurement to stdout and renames the image to form
    // a sequence of measured images.
    public synchronized static void doMeasurement() {
	// open up the scope
	openScope();
	
	// delay for display to compensate
	try { Thread.sleep((long)(1000.0*EXPOSURE_DELAY)); } catch (InterruptedException e) { e.printStackTrace(); }

	// get file to operate on
	String pictureFile = IMAGE_DIR + "\\" + takePictureToFile();

	// close the scope
	closeScope();

	// do ImageJ operations
        IJ.run("Open...", "path='" + pictureFile + "'");
	IJ.makeRectangle(180, 200, 350, 70);  //x1 y2 dx dy
        IJ.run("Set Measurements...", "area mean standard decimal=3");
        IJ.run("Measure");
        IJ.run("Close");

	// rename file
	String newFilename = IMAGE_DIR + "\\" + MEASURED_IMAGE_PREFIX + (++NUM_MEASURED) + IMAGE_SUFFIX;
	new File(pictureFile).renameTo(new File(newFilename));
    }

    // Coordinates taking a picture of the device and storing it in a
    // file.  Returns the short name (no path) of the filename
    // containing the result.
    private static String takePictureToFile() {
	// delete all previous pictures
	deleteImages();
	// wait until a new picture shows up
	String[] filenames;
	do {
	    filenames = new File(IMAGE_DIR).list(new RawImageFiles());
	    // sleep a little to avoid pulling down the machine
	    try { Thread.sleep(50); } catch (InterruptedException e) { e.printStackTrace(); }
	} while (filenames.length == 0);
	// acknowledge that we found an image file
	beep();
	// return oldest file
	Arrays.sort(filenames);
	return filenames[0];
    }

    // emit a beep if desired
    private static void beep() {
	if (BEEP_ON_CAPTURE) {
	    Toolkit.getDefaultToolkit().beep(); 
	}
    }

    // start a new thread to delete old images in the image directory
    public static void monitorImageDirectory() {
	Thread thread = new Thread() { public void run() { 
	    // how often to delete files (secs)
	    int DELETION_FREQUENCY = 2;
	    while (true) {
		// test that the image directory is there
		File imageDir = new File(IMAGE_DIR);
		if (!imageDir.exists()) {
		    System.err.println("Warning, image directory does not exist: " + IMAGE_DIR);
		    System.err.println("Measurements disabled.");
		    return;
		}
		// actually delete files
		deleteImages();
		// sleep until next time
		try {
		    Thread.sleep(1000*DELETION_FREQUENCY);
		} catch (InterruptedException e) { e.printStackTrace(); }
	    }
	} };
	thread.start();
    }
    
    // deletes all the RAW images from the image directory
    private synchronized static void deleteImages() {
	// get listing of raw image files in the directory
	String[] filenames = new File(IMAGE_DIR).list(new RawImageFiles());
	for (int i=0; i<filenames.length; i++) {
	    new File(IMAGE_DIR + "\\" + filenames[i]).delete();
	}
    }

    // opens the shutters, lights, etc on the scope
    private static void openScope() {
	runCommand("cmd /c scope-on.exe");
    }
    
    // closes the shutters, lights, etc on the scope
    private static void closeScope() {
	runCommand("cmd /c scope-off.exe");
    }

    // executes a command at the DOS prompt
    private static void runCommand(String command) {
	try {
	    int status = Runtime.getRuntime().exec(command).waitFor();
	} catch (InterruptedException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

}

// filter out all images except for raw ones taken by the camera
class RawImageFiles implements FilenameFilter {
    public boolean accept(File dir, String name) {
	// accept a file if it starts with the raw image prefix
	String filename = name.toLowerCase();
	String prefixName = TemplateImaging.RAW_IMAGE_PREFIX.toLowerCase();
	return filename.startsWith(prefixName);
    }
}
