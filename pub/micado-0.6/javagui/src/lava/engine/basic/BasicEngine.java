package lava.engine.basic;

import lava.util.*;
import lava.engine.*;
import lava.vm.*;

import java.io.*;
import java.lang.reflect.*;
import java.util.Stack;

/**
 * Provides basic high-level engine for fluidic operations.
 */
public class BasicEngine implements FluidEngine {
    /**
     * The default precision;
     */
    public static final double DEFAULT_PRECISION = 0.001;
    /**
     * VM that this is using as a backend.
     */
    private VM vm;
    /**
     * Current absolute precision (on scale from 0-1) that we're using
     * for mixing.
     */
    private double precision;
    /**
     * Pointer to one-to-one mixer.
     */
    private VMMixer mixer;
    /**
     * Pointer to storage we're using
     */
    private VMStore store;
    /**
     * Whether or not a storage location is currently free.
     */
    private boolean[] free;
    
    /**
     * Given the name of an interface <engineInterface> that a user
     * wants to use for an engine, as well as the name of a class
     * <vmClass> that represents the vm, this does the following:
     *
     *  1. Checks that the given vm supports the interface.
     *  2. Builds an engine and returns it.
     *
     */
    public static FluidEngine buildEngine(String engineInterface, String vmClass) {
	// get class for <engineInterface>
	Class cEngine = null;
	try {
	    cEngine = Class.forName(engineInterface);
	} catch (Exception e) {
	    System.err.println("Could not find engine interface " + cEngine);
	    e.printStackTrace();
	    System.exit(1);
	}

	// get class for <vmClass>
	Class cVM = null;
	try {
	    cVM = Class.forName(vmClass);
	} catch (Exception e) {
	    System.err.println("Could not find VM " + cEngine);
	    e.printStackTrace();
	    System.exit(1);
	}

	// make a VM
	VM vm = null;
	try {
	    vm = (VM)cVM.newInstance();
	} catch (Exception e) {
	    System.err.println("Could not instantiate VM " + cEngine);
	    e.printStackTrace();
	    System.exit(1);
	}

	// check that the VM implements the interface
	VMUtils.checkInterface(cVM, cEngine);

	// build a version of the engine interface that calls back to
	// this basic engine for all its native methods
	BasicEngine result = buildCallbackEngine(engineInterface, cEngine);

	// do the setup for the resulting engine
	result.setup(vm);
	
	return result;
    }

    /**
     * Given class of the interface for a fluid engine, build a
     * concrete instance of that interface that calls back into a
     * Basic Engine for all methods in the interface.
     */
    private static BasicEngine buildCallbackEngine(String engineInterface, Class cEngine) {
	// build up list of callbacks for <engineInterface>
	StringBuffer decls = new StringBuffer();
	Method[] engineMethods = cEngine.getMethods();
	for (int i=0; i<engineMethods.length; i++) {
	    Method meth = engineMethods[i];
	    // skip methods that aren't declared in the interface
	    if (meth.getDeclaringClass()!=cEngine) {
		continue;
	    }
	    decls.append("    public " + Utils.asTypeDecl(meth.getReturnType()) + " " + meth.getName() + "(");
	    Class[] params = meth.getParameterTypes();
	    for (int j=0; j<params.length; j++) {
		decls.append(Utils.asTypeDecl(params[j]) + " p" + j);
		if (j!=params.length-1) {
		    decls.append(", ");
		}
	    }
	    decls.append(") {\n");
	    decls.append("        return (" + Utils.asTypeDecl(meth.getReturnType()) + ")super.callNative(\"" + meth.getName() + "\", new Object[] {");
	    for (int j=0; j<params.length; j++) {
		decls.append("p" + j);
		if (j!=params.length-1) {
		    decls.append(", ");
		}
	    }
	    decls.append("} );\n");
	    decls.append("    }\n\n");
	}

	// write template file
	String engineName = "BasicEngine_" + engineInterface;
	String fileName = engineName + ".java";
	try {
	    String contents = Utils.readFile("lava/engine/basic/BasicEngineTemplate.java").toString();
	    // remove package name
	    contents = Utils.replaceAll(contents, "package lava.engine.basic;", "");
	    // for class decl
	    contents = Utils.replaceAll(contents, "extends BasicEngine", "extends BasicEngine implements " + engineInterface);
	    // for constructor
	    contents = Utils.replaceAll(contents, "BasicEngineTemplate", engineName);
	    // for methods
	    contents = Utils.replaceAll(contents, "// INSERT METHODS HERE", decls.toString());
	    Utils.writeFile(fileName, contents);
	} catch (IOException e) {
	    e.printStackTrace();
	    System.exit(1);
	}

	// compile the file
	try {
	    Process jProcess = Runtime.getRuntime().exec("jikes " + fileName, null, null);
	    jProcess.waitFor();
	} catch (Exception e) {
	    System.err.println("Error compiling auto-generated file " + fileName);
	    e.printStackTrace();
	    System.exit(1);
	}

	// instantiate the class
	BasicEngine result = null;
	try {
	    result = (BasicEngine)Class.forName(engineName).newInstance();
	    // cleanup the files we made
	    new File(engineName + ".java").delete();
	    new File(engineName + ".class").delete();
	} catch (Exception e) {
	    e.printStackTrace();
	    System.exit(1);
	}

	return result;
    }

    private void setup(VM vm) {
	this.vm = vm;
	
	// find a 1-to-1 mixer
	VMMixer[] mixers = vm.getMixers();
	for (int i=0; i<mixers.length; i++) {
	    if (mixers[i].numInputs()==2 && mixers[i].getRatio(0)==mixers[i].getRatio(1)) {
		this.mixer = mixers[i];
	    }
	}
	assert mixer!=null : "Could not find 1-to-1 mixer in VM";

	// find store
	int numStores = vm.getStores().length;
	assert numStores>0 : "No storage cells in VM";
	if (numStores>1) {
	    System.err.println("WARNING:  BasicEngine only deals with 1 store, will ignore stores 2-" + numStores);
	}
	this.store = vm.getStores()[0];
	
	// initialize list of free cells
	this.free = new boolean[store.getSize()];
	for (int i=0; i<free.length; i++) {
	    free[i] = true;
	}

	// set default precision
	this.precision = DEFAULT_PRECISION;
    }

    private Fluid internalMix (Fluid[] f, double[] r) {
	return new MixEngine(this, precision, mixer).mix(f, r);
    }

    /******************************************************************************
     * CALLBACK METHODS
     ******************************************************************************/

    protected Object callNative(String name, Object[] args) {
	return new NativeOperation(this, name, args).execute();
    }

    /******************************************************************************
     * UTILS FOR HELPER CLASSES
     ******************************************************************************/

    /**
     * Returns a free location, and marks the location as occupied.
     */
    VMLocation allocateLocation() {
	for (int i=0; i<free.length; i++) {
	    if (free[i]) {
		free[i] = false;
		return new VMLocation(store, i);
	    }
	}
	// stupid compiler
	assert (false || false) : "The store is full.";
	return null;
    }

    /**
     * Sets a location to be free or occupied.
     */
    void setFree(VMLocation loc, boolean free) {
	assert this.store==loc.store;
	this.free[loc.index] = free;
    }

    /**
     * Returns VM that this engine is currently using.
     */
    VM getVM() {
	return vm;
    }

    /**
     * Returns mixer that this engine is currently using.
     */
    VMMixer getMixer() {
	return mixer;
    }

    /******************************************************************************
     * FLUID ENGINE LIBRARY INTERFACE
     ******************************************************************************/

    /**
     * Set precision to a given value;
     */
    public void setPrecision(double precision) {
	assert (precision >= 0 && precision <= 1) : "Precision out of range; should be 0-1 but is " + precision;
	this.precision = precision;
    }
    /**
     * Mix some set of fluids.
     */
    public Fluid mix (Fluid f1, double r1, Fluid f2, double r2) { 
	return internalMix(new Fluid[] {f1, f2}, new double[] {r1, r2});
    }
    public Fluid mix (Fluid f1, double r1, Fluid f2, double r2, Fluid f3, double r3) {
	return internalMix(new Fluid[] {f1, f2, f3}, new double[] {r1, r2, r3});
    }
    public Fluid mix (Fluid f1, double r1, Fluid f2, double r2, Fluid f3, double r3, Fluid f4, double r4) {
	return internalMix(new Fluid[] {f1, f2, f3, f4}, new double[] {r1, r2, r3, r4});
    }
    public Fluid mix (Fluid[] f, double[] r) {
	return internalMix(f, r);
    }

    /**
     * Dump dependency dot-graph to <filename>.
     */
    public void dumpGraph(String filename) {
	BasicFluid.dumpGraph(filename);
    }
    /******************************************************************************/

}

/**
 * Actually carries out the mixing.
 */
class MixEngine {
    private BasicEngine engine;
    private double precision;
    private VMMixer mixer;

    MixEngine(BasicEngine engine, double precision, VMMixer mixer) {
	this.engine = engine;
	this.precision = precision;
	this.mixer = mixer;
	// designed for 1:1 mixer
	assert mixer.numInputs()==2 && mixer.getRatio(0)==mixer.getRatio(1);
    }

    /**
     * Performs symbolic mix of fluids in given target ratios.
     */
    Fluid mix(Fluid[] fluid, double[] target) {
	assert fluid.length==target.length;

	// prune out zero-weights
	int numZeros = 0;
	for (int i=0; i<fluid.length; i++) {
	    if (target[i]==0) {
		numZeros++;
	    }
	}
	if (numZeros>0) {
	    Fluid[] _fluid = new Fluid[fluid.length-numZeros];
	    double[] _target = new double[fluid.length-numZeros];
	    int pos = 0;
	    for (int i=0; i<fluid.length; i++) {
		if (target[i]!=0) {
		    _fluid[pos] = fluid[i];
		    _target[pos] = target[i];
		    pos++;
		}
	    }
	    fluid = _fluid;
	    target = _target;
	}

	// if we're down to one fluid, return it
	if (fluid.length==1) {
	    return fluid[0];
	}

	// normalize the targets
	double sum = 0;
	for (int i=0; i<target.length; i++) {
	    sum += target[i];
	}
	for (int i=0; i<target.length; i++) {
	    target[i] /= sum;
	}

	// find depth of mixing tree that is needed to obtain
	// precision
	int depth = 0;
	int[] ratios = null;
	do {
	    depth++;
	    ratios = getRatios(depth, target);
	} while (ratios==null);
	assert depth<32 : "Depth is " + depth + ", bigger than 32:  bit-width issues.";

	//System.out.println("ratio (" + ratios.length + "): " + ratios[0] + ":" + ratios[1] + "; depth=" + depth);

	// break up ratios into bins
	Stack[] bins = new Stack[depth+1];
	for (int i=0; i<bins.length; i++) {
	    bins[i] = new Stack();
	}
	for (int i=0; i<target.length; i++) {
	    int mask = 1;
	    for (int j=0; j<depth; j++){ 
		if ((mask & ratios[i]) != 0) {
		    bins[j].push(fluid[i]);
		}
		mask = mask << 1;
	    }
	}
	
	BasicFluid result = buildMixingTree(bins, depth);

	// this toggles a greedy evaluation instead of demand-driven
	result.ensureAvail();

	return result;
    }

    /**
     * Follows algorithm write-up to build minimal mixing tree using
     * base-two breakdown of reagants.
     */
    BasicFluid buildMixingTree(Stack[] bins, int depth) {
	if (bins[depth].empty()) {
	    BasicFluid f1 = buildMixingTree(bins, depth-1);
	    BasicFluid f2 = buildMixingTree(bins, depth-1);
	    Operation mix = new MixOperation(engine, 
					     new BasicFluid[] {f1, f2}, 
					     new int[] {1, 1});
	    return new BasicFluid(engine, mix);
	} else {
	    return (BasicFluid)bins[depth].pop();
	}
    }

    /**
     * Returns a set of numerators k_i so that the following
     * constraints are met:
     *
     * 1. k_i / 2^d is within range of target[i] +/- this.precision
     * 2. sum_i k_i = 2^d
     *
     * If no such selection of k's is possible, then returns null.
     */
    private int[] getRatios(int depth, double[] target) {
	// construct upper and lower bounds (inclusive) for each k
	int lowerSum = 0;
	int upperSum = 0;
	int[] lower = new int[target.length];
	int[] upper = new int[target.length];
	double unit = 1.0 / Math.pow(2, depth);
	for (int i=0; i<target.length; i++) {
	    lower[i] = (int)Math.ceil(Math.max(0.0,(target[i]-precision))/unit);
	    upper[i] = (int)Math.floor(Math.min(1.0,(target[i]+precision))/unit);
	    lowerSum += lower[i];
	    upperSum += upper[i];
	    // if there is not a single satisfying point in range,
	    // then fail
	    if (lower[i]>upper[i]) {
		return null;
	    }
	}

	// if we can never get to sum to 2^d, then fail
	int targetSum = (int)Math.pow(2, depth);
	if (lowerSum > targetSum || upperSum < targetSum) {
	    return null;
	}

	// now the problem is to select a number from each range so
	// that the total sums to 2^d.  Do this in a simple greedy way
	// -- start with lower bounds, and increase each numerator by
	// one (towards their upper bounds) until finding a combo that
	// is in range.
	while (lowerSum<targetSum) {
	    for (int i=0; i<target.length; i++) {
		if (lowerSum<targetSum && lower[i]<upper[i]) {
		    lower[i]++;
		    lowerSum++;
		}
	    }
	}

	return lower;
    }
}
