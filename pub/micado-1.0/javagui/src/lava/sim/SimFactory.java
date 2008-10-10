package lava.sim;

import lava.vm.VM;

import java.lang.reflect.*;

/**
 * Provides generic simulator for fluidic VM's.
 */
public class SimFactory {
    /**
     * First argument should be name of VM class that you want to
     * simulate.
     */
    public static void main(String[] args) {
	if (args.length==0) {
	    System.err.println("usage:  java lava.sim.SimFactory \"vmName\"");
	    System.exit(1);
	}
	buildSim(args[0]);
    }

    /**
     * Generates a simulator for a <vm> and stores it to disk.
     */
    public static void buildSim(String vmClass) {
	lava.sim.basic.BasicSim.buildSim(vmClass);
    }
}
