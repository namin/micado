public class TemplateMain {
    /**
     * Setup GUI.
     */
    static { 
	TemplateGUI.setupGUI(); 
	//TemplateImaging.monitorImageDirectory();
    }
    
    public static void main(String args[]) {
		//int jp = 0;
		//while (jp < 2) {
	   	//testMixCurve();
	      //testCocktailCurvenormal();
		//testCocktailCurveextended();
		//testCocktailCurvenine();
		//jp = jp + 1;
		//TemplateDriver.doMix(1);
		//}
    }

    /**************************************************************************************/
    /* TESTS / UTILS                                                                      */
    /**************************************************************************************/

    // test curve of glucose concentrations mixed with cocktail for detection
    public static void testCocktailCurvenormal() {
	System.out.println("Start gradient:");

	// input ports for A and B (two things mixed)
	int SAMPLE = 4;
	int BUFFER = 1;
	int COCKTAIL = 5;
	
	// A:B = 1:0
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	// A:B = 0.75:0.25
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	// A:B = 0.5:0.5
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	// A:B = 0.25:0.75
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	// A:B = 0:1
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("Done.");
    }

    public static void testCocktailCurvenine() {
	System.out.println("Start gradient:");

	// input ports for A and B (two things mixed)
	int SAMPLE = 4;
	int BUFFER = 1;
	int COCKTAIL = 5;
	
	// A:B = 1:0
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	// A:B = 0.875:0.125
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	
	// A:B = 0.75:0.25
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	// A:B = 0.625:0.375
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	// A:B = 0.5:0.5
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	// A:B = 0.375:0.625
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	// A:B = 0.25:0.75
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	// A:B = 0.125:0.875
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	// A:B = 0:1
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("Done.");
    }


public static void testCocktailCurveextended() {
	System.out.println("Start gradient extended test:");

	// input ports for A and B (two things mixed)
	int SAMPLE = 4;
	int BUFFER = 1;
	int COCKTAIL = 5;
	
	// A:B = 1:0
	System.out.println("A:B = 1:0");
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	// A:B = 0.75:0.25
	System.out.println("A:B = 0.75:0.25 case 1");
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("A:B = 0.75:0.25 case 2");
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	System.out.println("A:B = 0.75:0.25 case 3");
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("A:B = 0.75:0.25 case 4");
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("A:B = 0.75:0.25 case 5");
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("A:B = 0.75:0.25 case 6");
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	System.out.println("A:B = 0.75:0.25 case 7");
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("A:B = 0.75:0.25 case 8");
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	// A:B = 0.5:0.5
	System.out.println("A:B = 0.5:0.5 case 1");
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("A:B = 0.5:0.5 case 2");
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("A:B = 0.5:0.5 case 3");
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("A:B = 0.5:0.5 case 4");
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	// A:B = 0.25:0.75
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	System.out.println("A:B = 0.25:0.75 case 1");
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("A:B = 0.25:0.75 case 2");
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	System.out.println("A:B = 0.25:0.75 case 3");
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("A:B = 0.25:0.75 case 4");
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("A:B = 0.25:0.75 case 5");
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("A:B = 0.25:0.75 case 6");
	TemplateDriver.doInputToMixRight(SAMPLE);
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	System.out.println("A:B = 0.25:0.75 case 7");
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("A:B = 0.25:0.75 case 8");
	TemplateDriver.doInputToMixLeft(SAMPLE);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	// A:B = 0:1
	TemplateDriver.doInputToMixLeft(BUFFER);
	TemplateDriver.doInputToMixRight(BUFFER);
	TemplateDriver.doInputToMixAllButTopLeft(COCKTAIL);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("Done.");
    }


    // test mixing curve given by 1-to-1 mixers
    public static void testMixCurve() {
	System.out.println("Start gradient:");

	// input ports for A and B (two things mixed)
	int A = 7; // NADH
	int B = 5; // PBS
	int S = 3; // test sample
	
	// A:B = 1:0
	TemplateDriver.doInputToMixLeft(A);
	TemplateDriver.doInputToMixRight(A);
	TemplateImaging.doMeasurement();
	
	// A:B = 0.75:0.25
	TemplateDriver.doInputToMixLeft(A);
	TemplateDriver.doInputToMixRight(B);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(A);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	// A:B = 0.5:0.5
	TemplateDriver.doInputToMixLeft(A);
	TemplateDriver.doInputToMixRight(B);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	// A:B = 0.25:0.75
	TemplateDriver.doInputToMixLeft(A);
	TemplateDriver.doInputToMixRight(B);
	TemplateDriver.doMix(0);
	TemplateDriver.doInputToMixRight(B);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();
	
	// A:B = 0:1
	TemplateDriver.doInputToMixLeft(B);
	TemplateDriver.doInputToMixRight(B);
	TemplateImaging.doMeasurement();

	//A:B 1:10
	TemplateDriver.doInputToMixTopLeft(A);
	TemplateDriver.doInputToMixAllButTopLeft(B);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	//S:B 1:10
	TemplateDriver.doInputToMixTopLeft(S);	//mux pump on sample 1 line
	TemplateDriver.doInputToMixAllButTopLeft(B);
	TemplateDriver.doMix(0);
	TemplateImaging.doMeasurement();

	System.out.println("Done.");
    }

    /**************************************************************************************/
    /* COMPOSITE OPERATIONS                                                               */
    /**************************************************************************************/
}
