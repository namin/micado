package lava.vm;

/**
 * Represents a fluidic mixing unit.
 */

public class VMMixer extends VMElement {
    /**
     * Mixing ratio provided by this mixer.
     */
    private final int[] ratio;

    public VMMixer(String name, int[] ratio) {
	super(name);
	// copy over ratio array
	this.ratio = new int[ratio.length];
	for (int i=0; i<ratio.length; i++) {
	    this.ratio[i] = ratio[i];
	}
    }

    /**
     * Return number of inputs to mixer.
     */
    public int numInputs() {
	return ratio.length;
    }

    /**
     * Return i'th ratio.
     */
    public int getRatio(int i) {
	return ratio[i];
    }
    
}
