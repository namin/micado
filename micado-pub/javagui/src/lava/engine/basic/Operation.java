package lava.engine.basic;

import lava.vm.VMLocation;

/**
 * Represents an operation that was performed to generate a fluid.
 */
abstract class Operation {
    /**
     * Number of inputs to this.
     */
    abstract int numInputs();

    /**
     * Get input from this
     */
    abstract Object getInput(int i);

    /**
     * Execute, storing any fluidic ouput in location of <fluid>.
     */
    abstract Object executeWithLocation(BasicFluid outputFluid);

    /**
     * Execute, storing any fluidic ouput in a new location.
     */
    abstract Object execute();

    /**
     * Make sure inputs are available.
     */
    void ensureAvailInputs() {
	for (int i=0; i<numInputs(); i++) {
	    if (getInput(i) instanceof BasicFluid) {
		((BasicFluid)getInput(i)).ensureAvail();
	    }
	}
    }
}
