package lava.sim.basic;

import lava.util.*;
import lava.vm.*;

import java.io.*;
import java.lang.reflect.*;
import java.util.HashSet;

/**
 * A basic simulator for fluidic VM's.
 */
public abstract class BasicSim implements VM {
    /**
     * The vm that we are simulating.  Set by the
     * dynamically-generated subclass.
     */
    protected VM vm;
    /**
     * Method names to forward in the callback instead of wrapping as
     * a generic native method.  This is all the abstract methods of
     * the base VM class.
     */
    private static HashSet forwardMethods = new HashSet();
    static {
	try {
	    Method[] vmMeth = Class.forName("lava.vm.VM").getMethods();
	    for (int i=0; i<vmMeth.length; i++) {
		forwardMethods.add(vmMeth[i].getName());
	    }
	} catch (ClassNotFoundException e) {
	    e.printStackTrace();
	}
    }
    
    /**
     * Generates a simulator for a <vm> and stores it to disk.
     */
    public static void buildSim(String vmClass) {
	// get class for <vmClass>
	Class cVM = null;
	try {
	    cVM = Class.forName(vmClass);
	} catch (Exception e) {
	    System.err.println("Could not find VM " + cVM);
	    e.printStackTrace();
	    System.exit(1);
	}

	// build a version of the sim interface that calls back to
	// this basic sim for all its methods
	buildCallbackSim(cVM);
    }

    /**
     * Given a class of the VM we're simulating, build a parallel
     * version of that VM that calls back to this for all its VM
     * methods.  Write the result to a java file on disk.
     */
    private static void buildCallbackSim(Class cVM) {
	// build up list of callbacks for <cVM>
	StringBuffer decls = new StringBuffer();
	Method[] methods = cVM.getMethods();
	for (int i=0; i<methods.length; i++) {
	    Method meth = methods[i];
	    // Skip methods declared in a superclass (hashCode, etc)
	    if (meth.getDeclaringClass()!=cVM) {
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
	    decls.append("        ");
	    if (!Utils.asTypeDecl(meth.getReturnType()).equals("void")) {
		decls.append("return (" + Utils.asTypeDecl(meth.getReturnType()) + ")");
	    }
	    if (forwardMethods.contains(meth.getName())) {
		// forward some methods if desired
		decls.append("super." + meth.getName() + "(");
		    for (int j=0; j<params.length; j++) {
			decls.append("p" + j);
			if (j!=params.length-1) {
			    decls.append(", ");
			}
		    }
		decls.append(");\n");
	    } else {
		// otherwise use a callNative callback
		decls.append("super.callNative(\"" + meth.getName() + "\", new Object[] {");
		for (int j=0; j<params.length; j++) {
		    decls.append("p" + j);
		    if (j!=params.length-1) {
			decls.append(", ");
		    }
		}
		decls.append("} );\n");
	    }
	    decls.append("    }\n\n");
	}

	// write template file
	String simName = "Sim_" + cVM.getName();
	String fileName = simName + ".java";
	try {
	    String contents = Utils.readFile("lava/sim/basic/BasicSimTemplate.java").toString();
	    // remove package name
	    contents = Utils.replaceAll(contents, "package lava.sim.basic;", "");
	    // for class decl
	    contents = Utils.replaceAll(contents, "abstract class BasicSimTemplate", "class " + simName);
	    // for constructor
	    contents = Utils.replaceAll(contents, "BasicSimTemplate", simName);
	    // for instantiation of what we're wrapping
	    contents = Utils.replaceAll(contents, "// this.vm = new Template()", "this.vm = new " + cVM.getName() + "()");
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
	    int exitVal = jProcess.waitFor();
	    assert exitVal==0 : "Error compiling auto-generated file " + fileName;
	} catch (Exception e) {
	    System.err.println("Error compiling auto-generated file " + fileName);
	    e.printStackTrace();
	    System.exit(1);
	}
    }

    /******************************************************************************
     * CALLBACK METHODS
     ******************************************************************************/

    protected Object callNative(String name, Object[] args) {
	System.out.print("Calling native function " + name + "(");
	for (int i=0; i<args.length; i++) {
	    System.out.print(args[i]);
	    if (i!=args.length-1) {
		System.out.print(", ");
	    }
	}
	System.out.println(")");

	// TODO -- expand simulator's visualization and return value for native function
	return new Double(1.0);
    }

    /******************************************************************************
     * VM INTERFACE
     ******************************************************************************/

    /**
     * Return the mixers in the chip.
     */
    public VMMixer[] getMixers() {
	return vm.getMixers();
    }

    /**
     * Return the storage banks in the chip.
     */
    public VMStore[] getStores() {
	return vm.getStores();
    }

    /**
     * Perform a mix of the fluids <src> and store the mixture in
     * <dest>.  Use <mixer> with sources in same order as in mixer.
     */ 
    public void mixAndStore(VMMixer mixer, VMInputLocation[] src, VMOutputLocation dest) {
	System.out.println("Mixing " + src[0] + " and " + src[1] + " into " + dest);
	// TODO -- expand simulator's visualization of mix
    }
    
    /******************************************************************************/

}
