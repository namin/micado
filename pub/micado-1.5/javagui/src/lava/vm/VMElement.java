package lava.vm;

import java.util.HashSet;

/**
 * Represents an element of a fluidic architecture.
 */

public class VMElement {
    /*
     * Name of element.
     */
    private String name;
    
    public VMElement(String name) {
	this.name = name;
    }

    /**
     * Get name of this element.
     */
    public String getName() {
	return name;
    }

}
