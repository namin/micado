package lava.engine;

import lava.vm.VM;
import lava.vm.UnsupportedOperationException;

import java.lang.reflect.*;

/**
 * Provides basic high-level engine for fluidic operations.
 */
public class EngineFactory {
    /**
     * Given the name of an interface <engineInterface> that a user
     * wants to use for an engine, as well as the name of a class
     * <vmClass> that represents the vm, this does the following:
     *
     *  1. Checks that the given vm supports the interface.
     *  2. Builds an engine and returns it.
     */
    public static FluidEngine buildEngine(String engineInterface, String vmClass) {
	return lava.engine.basic.BasicEngine.buildEngine(engineInterface, vmClass);
    }
}
