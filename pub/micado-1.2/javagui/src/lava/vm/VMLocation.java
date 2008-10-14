package lava.vm;

/**
 * Represents a cell of a fluidic storage unit.
 */
public class VMLocation implements VMInputLocation, VMOutputLocation {
    /**
     * Represents that a fluid is not currently stored anywhere.
     */
    public static final VMLocation INVALID = new VMLocation(null, 0);
    /**
     * The store where this is held.
     */
    public final VMStore store;
    /**
     * The index in the store where this is held.
     */
    public final int index;

    public VMLocation(VMStore store, int index) {
	this.store = store;
	this.index = index;
    }

    public String toString() {
	return store.getName() + "[" + index + "]";
    }

    public int getIndex() {
	return index;
    }
}
