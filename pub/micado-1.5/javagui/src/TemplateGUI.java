import java.io.*;

import java.awt.*;
import java.awt.event.*;
import javax.swing.event.*;
import java.awt.image.*;

public class TemplateGUI {
    /**
     * Image name and dimensions.
     */
    protected static final String IMAGE_FILENAME = "template.png";
    protected static final int IMAGE_WIDTH = 756;
    protected static final int IMAGE_HEIGHT = 771;
    /**
     * Set this variable to true when you would like to record a new
     * array of port locations (to be cut-and-pasted where indicated
     * below).  When running in recording mode, use the left mouse
     * button to add a location to the current port index, and the
     * right mouse button to advance to the next index.
     *
     * For example, first left-click on all the locations that should
     * be associated with valve 1.  Then right click.  Then left-click
     * on all locations to be associated with valve 2.  Then right
     * click.  Etc.
     */
    static final boolean RECORDING_MODE = false;
    /**
     * (x,y) locations of each valve.
     */
    public static final int[][][] portLoc = 
    {
	{ },
	/*************************************************************/
	/* CUT-AND-PASTE THE RECORDED PORT LOCATIONS TO THE AREA BELOW
	/*************************************************************/
	// BEGIN port locations
        {
	    {354, 374}
	},
	{
	    {341, 404}
	},
	{
	    {328, 410}
	},
	{
	    {341, 417}
	},
	{
	    {341, 445}
	},
	{
	    {353, 506}
	},
	{
	    {360, 517}
	},
	{
	    {365, 506}
	},
	{
	    {366, 374}
	},
	{
	    {360, 363}
	},
	{
	    {319, 348},
	    {332, 347},
	    {343, 348},
	    {354, 348}
	},
	{
	    {365, 337},
	    {376, 337},
	    {388, 337},
	    {400, 336},
	},
	{
	    {319, 324},
	    {330, 324},
	    {365, 325},
	    {377, 325}
	},
	{
	    {342, 313},
	    {354, 314},
	    {389, 314},
	    {399, 314}
	},
	{
	    {320, 302},
	    {342, 303},
	    {366, 303},
	    {388, 303}
	},
	{
	    {332, 290},
	    {355, 291},
	    {377, 291},
	    {399, 291}
	},
	// END port locations
	/*************************************************************/
	/* CUT-AND-PASTE THE RECORDED PORT LOCATIONS TO THE AREA ABOVE
	/*************************************************************/
	// elapsed time display
	{{IMAGE_WIDTH - 125, 20}},
	// lock down
	{{IMAGE_WIDTH - 20, 40}},
	// open all
	{{IMAGE_WIDTH - 20, 60}},
	// mix pump delay
	{{IMAGE_WIDTH - 70, 80}}
    };
    // the following variables are names for key indices in your port locations
    
    static final int FIRST_CONTROL_LINE = 1;
	static final int FIRST_INSTRUCTION = TemplateDriver.CONTROL_LINES+1;
	static int index = 4 + TemplateDriver.CONTROL_LINES + TemplateDriver.INSTRUCTIONS;
	
    static final int INPUT_TO_MIX_TOP_LEFT = -2;//(index+=TemplateDriver.CONTROL_LINES);
    static final int INPUT_TO_MIX_LEFT = -2;//(index+=TemplateDriver.INPUTS);
    static final int INPUT_TO_MIX_RIGHT = -2; //(index+=TemplateDriver.INPUTS);
    static final int INPUT_TO_MIX_ALL_BUT_TOP_LEFT = -2;//(index+=TemplateDriver.INPUTS);
    static final int MIX = -2;//(index+=TemplateDriver.INPUTS);
    static final int LOCK_DOWN = index-2;//(++index);
    static final int OPEN_ALL  = index-1;//(++index);
    static final int PERI_PUMP = -2;//(++index);
    static final int SET_MIX_PUMP_DELAY = index;//(++index);
    static final int SET_PERI_DELAY = -2;//(++index);
    static final int TIME_DISPLAY = index-3;//(++index);
    static final int TAKE_MEASUREMENT = -2;//(++index);

    // sanity check
    static {
	if (index!=portLoc.length-1) {
	    System.err.println("WARNING!  Index mismatch in GUI setup (portloc.length-1=" + (portLoc.length-1) + ", index=" + index + ")");
	    System.err.println("This means that your index variables do not match your index array.");
	}
    }

    /**************************************************************************/
    /**************************************************************************/
    /**************************************************************************/

    /**
     * Frame and canvas for GUI.
     */
    public static Frame GUI_FRAME;
    protected static ImageCanvas canvas;
    /**
     * Font for GUI
     */
    protected static Font GUI_FONT = new Font("SansSerif", Font.PLAIN, 20);
    protected static Font CONTENTS_FONT = new Font("SansSerif", Font.PLAIN, 16);
    /**
     * Sets up the GUI.
     */
    public static void setupGUI() {
	GUI_FRAME = new Frame("Microfluidics GUI");
	GUI_FRAME.setLayout(new BorderLayout());
	canvas = new ImageCanvas(IMAGE_FILENAME);
	GUI_FRAME.add("Center", canvas);
	GUI_FRAME.setSize(IMAGE_WIDTH+5,IMAGE_HEIGHT+35); // need some boundaries for it all to be displayed
	GUI_FRAME.addWindowListener(new WindowAdapter() {
		public void windowClosing(WindowEvent evt) {
		    System.exit(0);
		}
	    });
	GUI_FRAME.setVisible(true);

	// draw the automatic buttons
	Graphics g = canvas.getBufferGraphics();
	g.setFont(GUI_FONT);

	// draw "lock down"
	if (LOCK_DOWN>=0) {
	g.drawString("lock down", portLoc[LOCK_DOWN][0][0]-100, portLoc[LOCK_DOWN][0][1]+5);
	}
	// draw "open all"
	if (OPEN_ALL>=0) {
	g.drawString("open all", portLoc[OPEN_ALL][0][0]-80, portLoc[OPEN_ALL][0][1]+5);
	}
	// draw "mux pump"
	if (PERI_PUMP>=0) {
	g.drawString("mux pump", portLoc[PERI_PUMP][0][0]-95, portLoc[PERI_PUMP][0][1]+5);
	}
	canvas.repaint();

	// setup the lock down, open all buttons
	if (LOCK_DOWN>=0) {
	setPortOnGUI(LOCK_DOWN, 1);
	}
	if (OPEN_ALL>=0) {
	setPortOnGUI(OPEN_ALL, 1);
	}
	if (PERI_PUMP>=0) {
	setPortOnGUI(PERI_PUMP, 1);
	}
	setMixPumpDelay(TemplateDriver.MIX_BETWEEN_PUMP_MILLIS);
	setPeriPumpDelay(TemplateDriver.PERI_PUMP_MILLIS);
    }

    /**
     * Inputs a logical port to set to a given value, and toggles the
     * physical port.  Assumes that logical ports are layed out as follows:
     *
     *  LOGICAL             PHYSICAL PORT  PHYSICAL LINE
     *  1                   A              0                     
     *  2                   A              1
     *  3                   A              2                     
     *  4                   A              3
     *  5                   A              4                     
     *  6                   A              5
     *  7                   A              6                     
     *  8                   B              7
     *  9                   B              0                     
     *  10                  B              1
     *  11                  B              2                     
     *  12                  B              3
     *  13                  B              4                     
     *  14                  B              5
     *  15                  B              6                     
     *  16                  B              7
     */
    static int setPort(int logicalPort, int value) {
	// might ignore some ports if not connected
	for (int i=0; i<TemplateDriver.LOGICAL_PORTS_TO_IGNORE.length; i++) {
	    if (logicalPort==TemplateDriver.LOGICAL_PORTS_TO_IGNORE[i]) {
		return 0;
	    }
	}
	setPortOnGUI(logicalPort, value);
	if (TemplateDriver.SIMULATION_MODE) {
	    return 0;
	} else {
	    return MITNative.setLine(logToPhyPort(logicalPort), logToPhyLine(logicalPort), value);
	}
    }
    static int[][] logToPhys = new TemplateMapping().logicalPort; //int[20][2]; // {port, line}
    private static int logToPhyPort(int logicalPort) {
	return logToPhys[logicalPort][0];
	//return (logicalPort-1) / 8;
    }
    private static int logToPhyLine(int logicalPort) {
	return logToPhys[logicalPort][1];
	//return (logicalPort-1) % 8;
    }

    // sets a port on the GUI to <value>.  Does not apply to
    // lock-down, open-all.
    static void setPortOnGUI(int logicalPort, int value) {
	final int defaultWidth = 14;
	setPortOnGUI(logicalPort, value, defaultWidth);
    }
    static void setPortOnGUILarge(int logicalPort, int value) {
	final int defaultWidth = 20;
	setPortOnGUI(logicalPort, value, defaultWidth);
    }

    // the current state of each port, as known by the gui.
    private static final int[] portState = new int[portLoc.length];
    static void setPortOnGUI(int logicalPort, int value, int width) {
	// remember state
	portState[logicalPort] = value;

	// update both screen graphics and image graphics
	Graphics g1 = canvas.getGraphics();
	Graphics g2 = canvas.getBufferGraphics();

	// set color
	if (value==0) {
	    // OPEN
	    g1.setColor(Color.BLUE);
	    g2.setColor(Color.BLUE);
	} else {
	    // CLOSED
	    g1.setColor(Color.RED);
	    g2.setColor(Color.RED);
	}

	// draw ports
	int[][] loc = portLoc[logicalPort];
	for (int i=0; i<loc.length; i++) {
	    //g.setClip(loc[i][0]-width/2, loc[i][1]-width/2, width, width);
	    g1.fillOval(loc[i][0]-width/2, loc[i][1]-width/2, width, width);
	    g2.fillOval(loc[i][0]-width/2, loc[i][1]-width/2, width, width);
	}
    }
    /**
     * Returns current state of port.
     */
    static int currentState(int logicalPort) {
	return portState[logicalPort];
    }
    /**
     * Switches logical port to its opposite value as currently
     * displayed.  Does not apply to lock-down, open-all.
     */
    static void switchPort(int logicalPort) {
	setPort(logicalPort, 1-portState[logicalPort]);
    }

    /**
     * Sets the delay between valve closings in mixer to <delay>
     * milliseconds and updates the screen.
     */
    static void setMixPumpDelay(int delay) {
	TemplateDriver.MIX_BETWEEN_PUMP_MILLIS = delay;
	if (SET_MIX_PUMP_DELAY >= 0) {
	drawString(SET_MIX_PUMP_DELAY, "mix delay = " + delay + "ms");
    }
	}

    static void setPeriPumpDelay(int delay2) {
	TemplateDriver.PERI_PUMP_MILLIS = delay2;
	if (SET_PERI_DELAY >= 0) {
	drawString(SET_PERI_DELAY, "Mux peri = " + delay2 + "ms");
	}
    }

    static void drawString(int portLocIndex, String str) {
	// update both screen graphics and image graphics
	Graphics g1 = canvas.getGraphics();
	Graphics g2 = canvas.getBufferGraphics();

	// bottom left corner
	int x = portLoc[portLocIndex][0][0]-100;
	int y = portLoc[portLocIndex][0][1]+15;

	g1.setFont(GUI_FONT);
	g2.setFont(GUI_FONT);

	// clear space
	g1.setColor(Color.WHITE);
	g2.setColor(Color.WHITE);

	g1.fillRect(x, y-22, 170, 30);
	g2.fillRect(x, y-22, 170, 30);

	// draw strings
	g1.setColor(Color.BLACK);
	g2.setColor(Color.BLACK);

	g1.drawString(str, x, y);
	g2.drawString(str, x, y);
    }

    static void drawContentsString(int x, int y, String str) {
	// update both screen graphics and image graphics
	Graphics g1 = canvas.getGraphics();
	Graphics g2 = canvas.getBufferGraphics();

	g1.setFont(CONTENTS_FONT);
	g2.setFont(CONTENTS_FONT);

	// clear space
	g1.setColor(Color.WHITE);
	g2.setColor(Color.WHITE);

	g1.fillRect(x, y-14, 13, 16);
	g2.fillRect(x, y-14, 13, 16);

	// draw strings
	g1.setColor(Color.BLACK);
	g2.setColor(Color.BLACK);

	g1.drawString(str, x, y);
	g2.drawString(str, x, y);
    }

    static private long lastMillis = System.currentTimeMillis();
    /**
     * Registers a clock tick between valve events.
     */
    static void registerTime() {
	long newMillis = System.currentTimeMillis();
	displayTime(newMillis - lastMillis);
	lastMillis = newMillis;
    }
    /**
     * Displays a time of <millis> milliseconds
     */
    static void displayTime(long millis) {
	// update both screen graphics and image graphics
	Graphics g1 = canvas.getGraphics();
	Graphics g2 = canvas.getBufferGraphics();

	// bottom left corner
	int x = portLoc[TIME_DISPLAY][0][0];
	int y = portLoc[TIME_DISPLAY][0][1]+5;

	double f = ((double)millis)/1000.0;
	String fs = (f+"");
	if (fs.length()>4) { fs = fs.substring(0,4); }

	String string = "time = " + fs + " s";

	g1.setFont(GUI_FONT);
	g2.setFont(GUI_FONT);

	// clear space
	g1.setColor(Color.WHITE);
	g2.setColor(Color.WHITE);

	g1.fillRect(x, y-20, 150, 20);
	g2.fillRect(x, y-20, 150, 20);

	// draw strings
	g1.setColor(Color.BLACK);
	g2.setColor(Color.BLACK);

	g1.drawString(string, x, y);
	g2.drawString(string, x, y);
    }
    /**
     * Returns the closest port to coordinates (x,y).  Will possibly
     * return lock-down, open-all.
     */
    static int closestPort(int x, int y) {
	double best = Double.MAX_VALUE;
	int bestLoc = -1;
	for (int i=0; i<portLoc.length; i++) {
	    for (int j=0; j<portLoc[i].length; j++) {
		int locX = portLoc[i][j][0];
		int locY = portLoc[i][j][1];
		double dist = Math.sqrt(Math.pow(locX-x, 2) + Math.pow(locY-y, 2));
		if (dist<best) {
		    best = dist;
		    bestLoc = i;
		}
	    }
	}
	assert bestLoc >= 0;
	return bestLoc;
    }
}

/**
 * based on http://www.rgagnon.com/javadetails/java-0229.html
 */
class ImageCanvas extends Canvas {
    Image image;
    Image buffer;
    
    public ImageCanvas(String name) {
	MediaTracker media = new MediaTracker(this);
	image = Toolkit.getDefaultToolkit().getImage(name);
	media.addImage(image, 0);
	try {
	    media.waitForID(0);  
	}
	catch (Exception e) {}
	addMouseListener(new ToggleListener());
    }
    
    public ImageCanvas(ImageProducer imageProducer) {
	image = createImage(imageProducer);
    }
    
    public void paint(Graphics g) {
	if (buffer==null) {
	    buffer = createImage(TemplateGUI.IMAGE_WIDTH, TemplateGUI.IMAGE_HEIGHT);
	    buffer.getGraphics().drawImage(image, 0, 0, this);
	} 
	g.drawImage(buffer,0,0,this);
    }
    
    public Graphics getBufferGraphics() {
	if (buffer==null) {
	    buffer = createImage(TemplateGUI.IMAGE_WIDTH, TemplateGUI.IMAGE_HEIGHT);
	    buffer.getGraphics().drawImage(image, 0, 0, this);
	}
	return buffer.getGraphics();
    }
}

class ToggleListener extends MouseInputAdapter {
    static Thread[] owner = new Thread[TemplateGUI.portLoc.length];

    // process a mouse click if we are in recording mode
    private static int numIndices = 0; // number of indices processed
    private static int numLocations = 0; // number of locations in current index
    public void recordingModeClick(MouseEvent e) {
	// if right mouse button, close the current index
	if (e.getButton()!=MouseEvent.BUTTON1) {
	    System.out.println("\n        },");
	    numLocations = 0;
	} else {
	    // otherwise, left click
	    // first time, print brace for list of locations in this index
	    if (numLocations == 0) {
		System.out.println("        // index " + (++numIndices));
		System.out.println("        {");
	    }
	    // to save space, have new line only every 6 locations
	    if (numLocations%6 == 0) {
		System.out.print("\n            ");
	    }
	    // add location to index
	    System.out.print("{" + e.getX() + "," + e.getY() + "}, ");
	    numLocations++;
	}
    }
    
    // stops all parallel threads (toggle operations, mix operations, etc.)
    public void stopParallelOps() {
	// each thread is monitoring the "owner" array to see if they
	// should still be controlling a given valve.  By resetting
	// the owner, the threads will know to stop.
	for (int i=0; i<owner.length; i++) {
	    owner[i] = null;
	}
    }

    // starts a parallel operation to toggle the given valve on and off
    public void toggleValveInParallel(final int valve) {
	Thread thread = new Thread() { public void run() {
	    // right mouse button click -- only toggle valve
	    // locations, not composite operations
	    if (valve >= TemplateGUI.FIRST_CONTROL_LINE &&
		valve <  TemplateGUI.FIRST_CONTROL_LINE+TemplateDriver.CONTROL_LINES) {
		// right button: toggle mode...
		// mark as owner thread
		owner[valve] = Thread.currentThread();
		try {
		    // wait for a multiple of 500 in millis,
		    // to try to synchronize different threads
		    Thread.sleep(1000+500*(1-TemplateGUI.currentState(valve))-(System.currentTimeMillis()%1000));
		    while (true) {
			// don't toggle if you're not the owner thread
			if (owner[valve]!=Thread.currentThread()) { return; }
			TemplateGUI.switchPort(valve);
			Thread.sleep(500);
		    }
		} catch (InterruptedException ex) {}
	    }
	} };
	thread.start();
    }

    // starts a parallel operation to pump around the mixer valves
    public void mixInParallel(final int mixer) {
	Thread thread = new Thread() { public void run() {
	    TemplateDriver.doSetupMix(mixer);
	    
	    // claim the mixer valves as being owned by this parallel
	    // mix operation
	    Thread current = Thread.currentThread();
	    int[] mixPath = TemplateDriver.MIX_PATH[mixer];
	    for (int i=0; i< mixPath.length; i++) {
		owner[mixPath[i]] = current;
	    }
	    // do mixing so long as we own the valves
	    while (true) {
		for (int i=0; i<mixPath.length; i++) {
		    if (owner[mixPath[i]]!=current) {
			return;
		    }
		}
		TemplateDriver.doMix(mixer);
	    }
	} };
	thread.start();
    }

    // opens a dialog to set the delay on the mixing pump
    public void setMixPumpDelay() {
	Thread thread = new Thread() { public void run() {
	    String str = (String)javax.swing.JOptionPane.showInputDialog(TemplateGUI.GUI_FRAME,
									 "Enter new mixer delay (ms)");
	    try {
		int delay = Integer.parseInt(str);
		TemplateGUI.setMixPumpDelay(delay);
	    } catch (NumberFormatException ex) {
	    }
	} };
	thread.start();
    }

    public void setPeriPumpDelay() {
	Thread thread = new Thread() { public void run() {
	    String str = (String)javax.swing.JOptionPane.showInputDialog(TemplateGUI.GUI_FRAME,
									 "Enter new peri delay (ms)");
	    try {
		int delay = Integer.parseInt(str);
		TemplateGUI.setPeriPumpDelay(delay);
	    } catch (NumberFormatException ex) {
	    }
	} };
	thread.start();
    }

    public void mouseClicked(final MouseEvent e) {
	// register the time so that we can print the elapsed time
	TemplateGUI.registerTime();

	// find the predefined index that is closest to the mouse click
	final int index = TemplateGUI.closestPort(e.getX(), e.getY());

	// if we're in recording mode, output information about where
	// the mouse was pressed
	if (TemplateGUI.RECORDING_MODE) {
	    recordingModeClick(e);
	    return;
	}

	if (e.getButton()!=MouseEvent.BUTTON1) {
	    // if right mouse button clicked, then toggle the given valve
	    toggleValveInParallel(index);
	} else {
	    // otherwise, left mouse button clicked -- process accordingly
	    processLeftClick(index);
	}
    }

    // process a click on the given index, where <index> indexes the
    // array of locations defined at the top of the file
    public void processLeftClick(int index) {
	if (index >= TemplateGUI.FIRST_CONTROL_LINE &&
	    index <  TemplateGUI.FIRST_CONTROL_LINE+TemplateDriver.CONTROL_LINES) {
	    // stop any parallel activities on this port
	    owner[index] = Thread.currentThread();
	    // switch a control line
	    TemplateGUI.switchPort(index);
	} else if (index >= TemplateGUI.FIRST_INSTRUCTION &&
			   index < TemplateGUI.FIRST_INSTRUCTION + TemplateDriver.INSTRUCTIONS) {
			   
		TemplateDriver.RunInstruction(index-TemplateGUI.FIRST_INSTRUCTION);
	
	} else if (index==TemplateGUI.LOCK_DOWN) { 
	    // close all
	    stopParallelOps();
	    TemplateDriver.lockDown();

	} else if (index==TemplateGUI.OPEN_ALL) {
	    // open all
	    stopParallelOps();
	    TemplateDriver.openAll();

	} else if (index==TemplateGUI.PERI_PUMP) {
	    mixInParallel(1);

	} else if (index==TemplateGUI.MIX) {
	    mixInParallel(0);

	} else if (index==TemplateGUI.SET_MIX_PUMP_DELAY) {
	    setMixPumpDelay();

	} else if (index==TemplateGUI.SET_PERI_DELAY) {
	    setPeriPumpDelay();

	} else if (index==TemplateGUI.TIME_DISPLAY) {
	    // do nothing

	} else if (index>=TemplateGUI.INPUT_TO_MIX_TOP_LEFT && 
		   index< TemplateGUI.INPUT_TO_MIX_TOP_LEFT+TemplateDriver.INPUTS) {
	    // input -> mixer
	    stopParallelOps();
	    int input = index - TemplateGUI.INPUT_TO_MIX_TOP_LEFT;
	    TemplateDriver.doInputToMixTopLeft(input);

	} else if (index>=TemplateGUI.INPUT_TO_MIX_LEFT && 
		   index< TemplateGUI.INPUT_TO_MIX_LEFT+TemplateDriver.INPUTS) {
	    // input -> mixer
	    stopParallelOps();
	    int input = index - TemplateGUI.INPUT_TO_MIX_LEFT;
	    TemplateDriver.doInputToMixLeft(input);

	} else if (index>=TemplateGUI.INPUT_TO_MIX_RIGHT && 
		   index< TemplateGUI.INPUT_TO_MIX_RIGHT+TemplateDriver.INPUTS) {
	    // input -> mixer
	    stopParallelOps();
	    int input = index - TemplateGUI.INPUT_TO_MIX_RIGHT;
	    TemplateDriver.doInputToMixRight(input);

	} else if (index>=TemplateGUI.INPUT_TO_MIX_ALL_BUT_TOP_LEFT && 
		   index< TemplateGUI.INPUT_TO_MIX_ALL_BUT_TOP_LEFT+TemplateDriver.INPUTS) {
	    // input -> mixer
	    stopParallelOps();
	    int input = index - TemplateGUI.INPUT_TO_MIX_ALL_BUT_TOP_LEFT;
	    TemplateDriver.doInputToMixAllButTopLeft(input);

	} else if (index==TemplateGUI.TAKE_MEASUREMENT) {
	    TemplateImaging.doMeasurement();
	}
    }
}
