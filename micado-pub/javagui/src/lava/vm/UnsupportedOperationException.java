package lava.vm;

/**
 * Represents that a fluidic chip does not support an operation
 * required by a given interface.
 */
public class UnsupportedOperationException extends RuntimeException {

    public UnsupportedOperationException() { super(); }
    public UnsupportedOperationException(String str) { super(str); }
}
