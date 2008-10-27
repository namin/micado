package lava.engine;

/**
 * This interface provides the main functionality for fluidic
 * operations.
 */
public interface FluidEngine {

    /**
     * Set precision to a given value;
     */
    void setPrecision(double precision);

    /**
     * Mix some set of fluids.
     */
    Fluid mix (Fluid f1, double r1, Fluid f2, double r2);
    Fluid mix (Fluid f1, double r1, Fluid f2, double r2, Fluid f3, double r3);
    Fluid mix (Fluid f1, double r1, Fluid f2, double r2, Fluid f3, double r3, Fluid f4, double r4);
    Fluid mix (Fluid[] f1, double[] r1);

    /**
     * Dump dependency dot-graph to <filename>.
     */
    void dumpGraph(String filename);
}
