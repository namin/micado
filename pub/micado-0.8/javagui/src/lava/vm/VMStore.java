package lava.vm;

/**
 * Represents a fluidic storage unit.
 */

public class VMStore extends VMElement {
    /**
     * Number of storage cells.
     */
    private final int size;;

    public VMStore(String name, int size) {
	super(name);
	this.size = size;
    }

    /**
     * Returns number of cells in this store.
     */
    public int getSize() {
	return this.size;
    }
}
