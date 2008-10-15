package lava.engine.basic;

import lava.vm.*;

/**
 * Represents a mix operation that was performed to generate a fluid.
 */
class MixOperation extends Operation {
    /**
     * The inputs to this mix.
     */
    private BasicFluid[] input;
    /**
     * The mixing ratios.
     */
    private int[] ratio;
    /**
     * Engine we're using to help with execution.
     */
    private BasicEngine engine;
    
    MixOperation(BasicEngine engine, BasicFluid[] input, int[] ratio) {
	this.engine = engine;
	this.input = input;
	this.ratio = ratio;
    }

    int numInputs() {
	return input.length;
    }

    Object getInput(int i) {
	return input[i];
    }

    int getRatio(int i) {
	return ratio[i];
    }

    Object executeWithLocation(BasicFluid fluid) {
	// make sure inputs are available, and build list of input
	// locations
 	VMInputLocation[] inputLoc = new VMInputLocation[input.length];
	for (int i=0; i<input.length; i++) {
	    input[i].ensureAvail();
	    inputLoc[i] = input[i].getLocation();
	}

	// invoke mixer
	engine.getVM().mixAndStore(engine.getMixer(), inputLoc, fluid.getLocation());

	// mark inputs as consumed
	for (int i=0; i<input.length; i++) {
	    input[i].consume();
	}

	// return resulting fluid
	return fluid;
    }

    Object execute() {
	ensureAvailInputs();
	BasicFluid fluid = new BasicFluid(engine, engine.allocateLocation(), this);
	return executeWithLocation(fluid);
    }
}
