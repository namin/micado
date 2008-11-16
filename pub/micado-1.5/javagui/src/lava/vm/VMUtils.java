package lava.vm;

/**
 * Provides utilities for VM's.
 */
public class VMUtils {
    /**
     * Checks that this chip supports the interface of <cEngine>.  If
     * not, throws an UnsupportedOperationException.
     */
    public static void checkInterface(Class cVM, Class cEngine) throws UnsupportedOperationException {
	// note that this should also check that <cVM> only uses
	// VMInputLocation/VMOutputLocation, not plain VMLocation, and
	// with no ambiguity (two methods that differ only by the
	// Input/Output keyword).  Actually, current interface further
	// requires that a widget only have one fluid output.
	//
	// Should also check that return types match, as this isn't
	// checked later.
	
	// TODO -- implement interface checking
    }
}
