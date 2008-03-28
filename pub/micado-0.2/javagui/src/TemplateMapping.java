class TemplateMapping {
    public TemplateMapping() {
	setup();
	// MODIFY MAPPING BELOW ----------------------
	//logicalPort[1]  = physicalPort(PORT_A, LINE_0);
	//logicalPort[2]  = physicalPort(PORT_A, LINE_1);
	//logicalPort[3]  = physicalPort(PORT_A, LINE_2);
	//logicalPort[4]  = physicalPort(PORT_A, LINE_3);
	//logicalPort[5]  = physicalPort(PORT_A, LINE_4);
	//logicalPort[6]  = physicalPort(PORT_A, LINE_5);
	//logicalPort[7]  = physicalPort(PORT_A, LINE_6);
	//logicalPort[8]  = physicalPort(PORT_A, LINE_7);
	//logicalPort[9]  = physicalPort(PORT_C, LINE_0);
	//logicalPort[10] = physicalPort(PORT_C, LINE_1);
	//logicalPort[11] = physicalPort(PORT_C, LINE_2);
	//logicalPort[12] = physicalPort(PORT_C, LINE_3);
	//logicalPort[13] = physicalPort(PORT_C, LINE_4);
	//logicalPort[14] = physicalPort(PORT_C, LINE_5);
	//logicalPort[15] = physicalPort(PORT_C, LINE_6);
	//logicalPort[16] = physicalPort(PORT_C, LINE_7);

	// --------------------------------------------
    }
    
    ///////////////////////////////////////////////
    ///////////////////////////////////////////////
	
    // IGNORE WHAT'S BELOW
    
    public int[][] logicalPort = new int[TemplateDriver.CONTROL_LINES+1][];

    // port/line constructor
    private static final int[] physicalPort(int port, int line) {
	return new int[] {port, line};
    }

    // port/line constructor
    private void setup() {
	// set all ports to -1 by default
	for (int i=0; i<logicalPort.length; i++) {
	    logicalPort[i] = new int[] {-1, -1};
	}
    }

    // ports
    private static final int PORT_A = 0;
    private static final int PORT_B = 1;
    private static final int PORT_C = 2;
    private static final int PORT_D = 3;

    // lines
    private static final int LINE_0 = 0;
    private static final int LINE_1 = 1;
    private static final int LINE_2 = 2;
    private static final int LINE_3 = 3;
    private static final int LINE_4 = 4;
    private static final int LINE_5 = 5;
    private static final int LINE_6 = 6;
    private static final int LINE_7 = 7;
    private static final int LINE_8 = 8;
    private static final int LINE_9 = 9;
    private static final int LINE_10 = 10;
    private static final int LINE_11 = 11;
    private static final int LINE_12 = 12;
    private static final int LINE_13 = 13;
    private static final int LINE_14 = 14;
    private static final int LINE_15 = 15;
    private static final int LINE_16 = 16;
    private static final int LINE_17 = 17;
    private static final int LINE_18 = 18;
    private static final int LINE_19 = 19;
    private static final int LINE_20 = 20;
    private static final int LINE_21 = 21;
    private static final int LINE_22 = 22;
    private static final int LINE_23 = 23;
    private static final int LINE_24 = 24;
}
