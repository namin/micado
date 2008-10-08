package lava.engine.basic;

import lava.engine.*;
import lava.vm.*;

import java.io.*;
import java.util.*;

class BasicFluid implements Fluid {
    /**
     * Registry of all fluids.
     */
    private static HashSet registry = new HashSet();
    /**
     * Max ID across fluids.
     */
    private static int MAX_ID = 0;
    /**
     * A unique ID for this fluid.
     */
    private int id;
    /**
     * The engine that's orchestrating operations with this.
     */
    private BasicEngine engine;
    /**
     * The operation that generated this fluid.
     */
    private Operation generator;
    /**
     * Current location of this fluid.  If the fluid is not currently
     * stored in the VM, then this is VMLocation.INVALID.
     */
    private VMLocation location;
    
    /**
     * Construct a fluid with the given location and generator.  The
     * generator may be null if the engine is sure that this fluid
     * will not need to be regenerated (i.e., it is a temporary fluid
     * with a single, one-time use).
     */
    BasicFluid(BasicEngine engine, VMLocation location, Operation generator) {
	registry.add(this);
	this.id = MAX_ID++;
	this.engine = engine;
	this.location = location;
	this.generator = generator;
    }

    /**
     * Construct a fluid without a location, but with a generator.  It
     * will be generated when it is needed (or explicitly evaluated).
     */
    BasicFluid(BasicEngine engine, Operation generator) {
	this(engine, VMLocation.INVALID, generator);
    }

    /**
     * Returns whether or not a unit of this is available on chip,
     * i.e., whether it is currently stored in a location.
     */
    boolean avail() {
	return location!=VMLocation.INVALID;
    }

    /**
     * Marks this as being consumed, i.e., it is not currently
     * available for use.  Also frees the storage location allocated
     * for this.
     */
    void consume() {
	if (location!=VMLocation.INVALID) {
	    engine.setFree(location, true);
	}
	location = VMLocation.INVALID;
    }

    /**
     * Ensures that this is available, by regenerating the fluid if
     * necessary.
     */
    void ensureAvail() {
	if (!avail()) {
	    regenerate();
	}
    }

    /**
     * Regenerates this fluid from its inputs.
     */
    private void regenerate() {
	// ensure that inputs are available before allocating space so
	// that inputs can use this space to compute themselves
	generator.ensureAvailInputs();
	location = engine.allocateLocation();
	generator.executeWithLocation(this);
    }

    /**
     * Returns location of this.
     */
    VMLocation getLocation() {
	return location;
    }

    /**
     * Dump dot graph of all fluids in registry.
     */
    static void dumpGraph(String filename) {
	try {
	    PrintWriter out = new PrintWriter(new FileWriter(filename));
	    out.println("digraph G {");
	    out.println("size=\"6.5,9\";");
	    for (Iterator it = registry.iterator(); it.hasNext(); ) {
		BasicFluid fluid = (BasicFluid)it.next();
		Operation oper = fluid.generator;
		// label node
		out.println("fluid" + fluid.hashCode() + " [label=\"fluid\"];");
		// label the generator
		out.print("oper" + oper.hashCode() + " ");
		if (oper instanceof MixOperation) {
		    out.println("[shape=\"box\",label=\"mix\"];");
		} else if (oper instanceof NativeOperation) {
		    out.println("[shape=\"diamond\",label=\"" + ((NativeOperation)oper).getName() + "\"];"); 
		} else {
		    assert false : "Don't know how to print dot graph for " + oper.getClass();
		}
		// put arrow from generator to this
		out.println("oper" + oper.hashCode() + " -> " + "fluid" + fluid.hashCode() + ";");
		// put arrows from generator inputs to generator
		for (int i=0; i<oper.numInputs(); i++) {
		    if (oper.getInput(i) instanceof BasicFluid) {
			out.println("fluid" + oper.getInput(i).hashCode() + " -> " + "oper" + oper.hashCode() + ";");
		    }
		}
	    }
	    out.println("}");
	    out.close();
	} catch (IOException e) {
	    e.printStackTrace();
	}
    }

    public int hashCode() {
	return id;
    }
}
