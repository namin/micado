import lava.vm.*;

import java.io.*;

/**
 * An interface to the native MIT chip.
 */
public class MITNative {
    /**
     * Whether we have the ni-daq libraries (otherwise measurement
     * computing.)
     */
    private static final boolean NI_DAQ = false;
    /**
     * Whether or not external calls have debugging output.
     */
    private static final int EXTERNAL_DEBUG = 1;
    /**
     * Whether or not internal testing has debugging output.
     */
    private static final int INTERNAL_DEBUG = 1;

    static { 
	if (NI_DAQ) {
	    System.loadLibrary("MITNative"); 
	} else {
	    System.loadLibrary("MITNative-UL");
	}
    }
    /**
     * Sets <line> (0-7) of <port> (0-3) to <value> (0-1).  Returns 0
     * for success, otherwise an error code.  If debug is 1, then
     * prints debug info.
     */
    private static native int setLine(int port, int line, int value, int debug);

    /**
     * Call set line without debug info.
     */
    public static int setLine(int port, int line, int value) {
	return setLine(port, line, value, EXTERNAL_DEBUG);
    }

    /**
     * Waits for <millis> milliseconds and then returns.
     */
    private static void wait(int millis) {
	long start = System.currentTimeMillis();
	while (start+millis > System.currentTimeMillis());
    }

    /**
     * Tests the native interface by toggling all lines of all ports.
     *
     * Requires 3 arguments.  If the third argument is 0, sets all
     * ports off.  If the third argument is 1, turns all ports on.  If
     * the third argument is 3, then toggles back and forth between 0
     * and 1 for the port and line specified by the first two
     * arguments.
     *
     * 4 as third argument - turns off set port
     * 5 as third argument - turns on set port
     * 6 as third argument - cycles through all lines on given port
     * */
    public static void main(String[] args) throws IOException {

	// if there are args, pass directly
	if (args.length==3) {
	    try {
		int port = Integer.valueOf(args[0]).intValue();
		int line = Integer.valueOf(args[1]).intValue();
		int value = Integer.valueOf(args[2]).intValue();
		System.out.println("Setting port " + port + ", line " + line + " to " + value + "... ");
		int err;
		if (value==4) {
		    err = setLine(port, line, 0, INTERNAL_DEBUG);
		    return;
		}
		if (value==5) {
		    err = setLine(port, line, 1, INTERNAL_DEBUG);
		    return;
		}
		if (value==3) {
		    while (true) {
			err = setLine(port, line, 0, INTERNAL_DEBUG);
			wait(500);
			err = setLine(port, line, 1, INTERNAL_DEBUG);
			wait(500);
		    }
		} if (value==6) {
		    // toggle all ports
		    while (true) {
			for (int i=0; i<8; i++) {
			    err = setLine(port, i, 0, INTERNAL_DEBUG);
			    wait(500);
			    err = setLine(port, i, 1, INTERNAL_DEBUG);
			    wait(500);
			}
		    }
		} else {
		    for (int i=0; i<4; i++) {
			for (int j=0; j<8; j++) {
			    err = setLine(i, j, value, INTERNAL_DEBUG);
			}
		    }
		}
		//System.out.println("done.   (return code=" + err + ")\n");
	    } catch (Exception e) {
		e.printStackTrace();
	    }
	    System.exit(0);
	}

	BufferedReader in = new BufferedReader(new InputStreamReader(System.in));

	System.out.println("This program will test the native interface to the NI board.\n" +
			   "Press enter to continue.");
	in.readLine();

	System.out.println("Press enter to clear all ports.");
	in.readLine();
	
	for (int i=0; i<4; i++) {
	    for (int j=0; j<8; j++) {
		System.out.println("  clearing port " + i + ", line " + j + "... ");
		int err = setLine(i, j, 0, INTERNAL_DEBUG);
		System.out.println("  done.   (return code=" + err + ")\n");
	    }
	}

	System.out.println("Press enter to start setting ports.");

	for (int i=0; i<4; i++) {
	    for (int j=0; j<8; j++) {
		in.readLine();
		System.out.println("  setting port " + i + ", line " + j + "... ");
		int err = setLine(i, j, 1, INTERNAL_DEBUG);
		System.out.println("  done.   (return code=" + err + ")\n");
	    }
	}

	System.out.println("\nAll done.");
    }

}
