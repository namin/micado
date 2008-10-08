package lava.vm;

/**
 * Must be implemented by any vm.  The concrete class should include
 * public methods for the native devices of the VM.  These methods
 * should only reference VM elements, not Fluids.
 */
public interface VM {
    /**
     * Return the mixers in the chip.
     */
    VMMixer[] getMixers();

    /**
     * Return the storage banks in the chip.
     */
    VMStore[] getStores();

    /**
     * Perform a mix of the fluids <src> and store the mixture in
     * <dest>.  Use <mixer> with sources in same order as in mixer.
     */
    void mixAndStore(VMMixer mixer, VMInputLocation[] src, VMOutputLocation dest);
}
