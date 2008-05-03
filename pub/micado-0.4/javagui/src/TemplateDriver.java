public class TemplateDriver {
    /***********************************************************************************************/
    /* MAJOR CONFIG                                                                                */
    /***********************************************************************************************/

    /**
     * Number of control lines, instructions, storage cells, and inputs
     */
    public static final int CONTROL_LINES = 16;
	public static final int INSTRUCTIONS = 0;
    public static final int STORAGE_CELLS = 0;
    public static final int INPUTS = 0;
	
	public static final boolean[][] INSTRUCTION_TABLE = 
	{
	// BEGIN instructions
	// END instructions
	};
	
	public static final int[][] INSTRUCTION_PUMPS = 
	{
	// BEGIN instruction pumps
	// END instruction pumps
	};
	
    /**
     * Timing estimates (in millis)
     */

    // time between each pump in mix					    
    protected static int MIX_BETWEEN_PUMP_MILLIS = 5; 

    // time between each pump in mix					    
    protected static int PERI_PUMP_MILLIS = 50; 


    // amount of time to mix per click on mixer
    protected static int MIX_TOTAL_MILLIS = 10000;
    protected static int MUX_TOTAL_MILLIS = 10000;
    // how long to wait for non-culture input to flow through mixer
    protected static int INPUT_TO_MIX_DELAY = 2000;
    // how long to feed input against latch
    protected static int COMPRESS_MILLIS = 2000;

    /***********************************************************************************************/
    /* MINOR CONFIG                                                                                */
    /***********************************************************************************************/

    /**
     * Whether to run in simulation mode only (do not make native
     * calls).
     */
    protected static final boolean SIMULATION_MODE = true;
    /**
     * Fast mode, speeds up simulation by a given factor.
     */
    private static boolean FAST_MODE = true;
    private static int FAST_SPEEDUP_FACTOR = 10; // speedup factor
    /**
     * Ports to close instead of open in openAll.
     */
    protected static int[] REVERSE_OPS = {};
    /**
     * If any ports are not connected to anything, put them here.
     */
    protected static int[] LOGICAL_PORTS_TO_IGNORE = {};
    /**
     * An ordered list of the control lines around the mixer path.
     */
    protected static int[][] MIX_PATH = {{8, 6, 5, 4, 2, 1, 9},{16,14,11}};
    
    /**************************************************************************************/
    /* APPLICATION-SPECIFIC CONFIG                                                        */
    /**************************************************************************************/
    // input port for air
    private static final int AIR = 0;
    // input port for PBS
    private static final int PBS = 1;

    /**************************************************************************************/
    /* BASIC OPS                                                                          */
    /**************************************************************************************/

    // should only be for culture samples
    public static void doInputToMixTopLeft(int input) {
	lockDown();
	
	// open path
	openPort(10);
	openPort(1);
	openPort(2);
	openPort(3);
	
	// send through sample
	selectInput(input);
	wait(INPUT_TO_MIX_DELAY);

	lockDown();
    }

    // should only be for calibration (PBS, G/P/L)
    public static void doInputToMixLeft(int input) {
	lockDown();

	// open path
	selectInput(input);

	openPort(10);
	openPort(1);
	openPort(2);
	openPort(4);
	openPort(5);
	openPort(6);
	openPort(7);

	// wait to run
	wait(INPUT_TO_MIX_DELAY);

	lockDown();
    }

    // should only be for calibration (PBS, G/P/L)
    public static void doInputToMixRight(int input) {
	lockDown();

	// open path
	selectInput(input);

	openPort(10);
	openPort(9);
	openPort(8);
	openPort(7);

	// wait to run
	wait(INPUT_TO_MIX_DELAY);

	lockDown();
    }

    // should only be G, P, or L
    public static void doInputToMixAllButTopLeft(int input) {
	lockDown();

	// open path
	selectInput(input);

	openPort(10);
	openPort(9);
	openPort(8);
	openPort(6);
	openPort(5);
	openPort(4);
	openPort(3);

	// wait to run
	wait(INPUT_TO_MIX_DELAY);

	lockDown();
    }

    // just clear the way for mixing
    public static void doSetupMix(int mixer) {
	lockDown();
	if (mixer==0){
	openPort(1);
	openPort(2);
	openPort(4);
	openPort(5);
	openPort(6);
	openPort(8);
	openPort(9);
	}
	else {
	openPort(15);
	openPort(14);
	openPort(11);
	openPort(10);
	openPort(1);
	openPort(2);
	openPort(3);	
	}
    }

    // mix for allotted time
    public static void doMix(int mixer) {
	long orig = System.currentTimeMillis();
	if (mixer==0){ 
	do {
	    doMixStep(mixer);
	} while (System.currentTimeMillis() - orig < MIX_TOTAL_MILLIS);
	}
	else {
	doSetupMix(mixer);

	do {
	   	doMixStep(mixer);
	} while (System.currentTimeMillis() - orig < MUX_TOTAL_MILLIS);
	}
    }

    // do a single step actuation of each valve on mix path
    public static void doMixStep(int mixer) {
	if (mixer == 0)
	  {
	    pump(MIX_PATH[mixer], MIX_PATH[mixer].length);
        }
	else
	  {
	    pump1(MIX_PATH[mixer], MIX_PATH[mixer].length);
        }
    }






    /**
     * Set the input multiplexors to select a given input.
     */
    protected static void selectInput(int input) {
	// special to this chip -- if inputting from air, then turn on
	// the air port
	//if (input==AIR) { closePort(17); }

	// mux ports
	int bit1 = (input / 1) % 2;
	int bit2 = (input / 2) % 2;
	int bit3 = (input / 4) % 2;
	
	// first port closes every other line
	setPort(16, 1-bit1);
	setPort(15, bit1);
	setPort(14, 1-bit2);
	setPort(13, bit2);
	setPort(12, 1-bit3); 
	setPort(11, bit3);
	// last port closes half the lines at a time
    }

    /**
     * Waits for sample to compress.
     */
    private static void compress() {
	wait(COMPRESS_MILLIS);
    }

    /**************************************************************************************/
    /* UTIL OPS                                                                           */
    /**************************************************************************************/

	/**
	* Run an instruction according to its table. 
	* If the instruction has pumps, will do the pumping for pumpRunTime with a delay of MIX_BETWEEN_PUMP_MILLIS between pumps.
	*/
	public static void RunInstruction(int index, int pumpRunTime) {
		for (int c=0; c < CONTROL_LINES; c++) {
			if (TemplateDriver.INSTRUCTION_TABLE[index][c]) {
				openPort(c+1);
			} else {
				closePort(c+1);
			}
		}
		if (INSTRUCTION_PUMPS[index] != null) {
			RunInstructionPump(index, pumpRunTime);
		}
	}
	
	/**
	* Run an instruction according to its table. 
	* If the instruction has pumps, will do the pumping for MIX_TOTAL_MILLIS with a delay of MIX_BETWEEN_PUMP_MILLIS between pumps.
	*/
	public static void RunInstruction(int index) {
		RunInstruction(index, MIX_TOTAL_MILLIS);
	}
	
	/**
	* Runs the pumps of an instruction for a cycle (assumes the instruction has pumps).
	*/
	public static void RunInstructionPumpStep(int index) {
	    pump(INSTRUCTION_PUMPS[index], INSTRUCTION_PUMPS[index].length);
    }
	
	/**
	* Runs the pumps of an instruction for the given time (assumes the instruction has pumps).
	*/
	public static void RunInstructionPump(int index, int runTime) {
		long orig = System.currentTimeMillis();
		do {
			RunInstructionPumpStep(index);
		} while (System.currentTimeMillis() - orig < runTime);
	}
	
	/**
	* Runs the pumps of an instruction for MIX_TOTAL_MILLIS time (assumes the instruction has pumps).
	*/
	public static void RunInstructionPump(int index) {
		RunInstructionPump(index, MIX_TOTAL_MILLIS);
	}
	
    /**
     * Does peristaltic pumping with the given ports for the given
     * number of pumps (counted as the number of times a valve
     * closes).  Can call with any set of the ports open or closed
     * (although all open is usually bad for things that aren't
     * supposed to move); will return with all ports closed.
     */
    public static void pump(int[] ports, int numPumps) {
	int i=0;
	for (int p=0; p<numPumps; p++) {
	    wait(MIX_BETWEEN_PUMP_MILLIS);
	    closePort(ports[(i+1) % ports.length]);
	    wait(MIX_BETWEEN_PUMP_MILLIS);
	    openPort(ports[i]);
	    i = (i+1) % ports.length;		
	}
    }

    public static void pump1(int[] ports, int numPumps) {
	int i=0;
	for (int p=0; p<numPumps; p++) {
	    wait(PERI_PUMP_MILLIS);
	    closePort(ports[(i+1) % ports.length]);
	    wait(PERI_PUMP_MILLIS);
	    openPort(ports[i]);
	    i = (i+1) % ports.length;		
	}
    }


    /**
     * Waits for <millis> milliseconds and then returns.
     */
    public static void wait(int millis) {
	if (FAST_MODE) { millis = millis / FAST_SPEEDUP_FACTOR; }
	try {
	    Thread.sleep(millis);
	} catch (InterruptedException e) {}
    }

    /**
     * Closes all valves.
     */
    protected static void lockDown() {
	setAllPorts(1);
    }

    // opens all valves
    protected static void openAll() {
	setAllPorts(0);
    }

    // sets all ports to <value>
    private static void setAllPorts(int value) {
	for (int i=1; i<=CONTROL_LINES; i++) {
	    boolean reverse = false;
	    for (int j=0; j<REVERSE_OPS.length; j++) {
		if (REVERSE_OPS[j]==i) {
		    reverse = true;
		    break;
		}
	    }
	    if (reverse) {
		setPort(i, 1-value);
	    } else {
		setPort(i, value);
	    }
	}
    }

    private static int setPort(int logicalPort, int value) {
	return TemplateGUI.setPort(logicalPort, value);
    }

    private static void switchPort(int logicalPort) {
	TemplateGUI.switchPort(logicalPort);
    }

    private static int openPort(int logicalPort) {
	return setPort(logicalPort, 0);
    }

    private static int closePort(int logicalPort) {
	return setPort(logicalPort, 1);
    }
	
	// BEGIN instruction shortcuts
	// END instruction shortcuts
}
