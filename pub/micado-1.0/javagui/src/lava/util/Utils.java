package lava.util;

import java.io.*;

public class Utils {
    /**
    * Returns the contents of <fileName> as a string buffer.
    */
    public static StringBuffer readFile(String fileName)
    throws IOException
    {
        StringBuffer result = new StringBuffer();
        BufferedReader in = new BufferedReader(new FileReader(fileName));
        while (true) {
            String line = in.readLine();
            if (line == null) {
                break;
            } else {
                result.append(line + "\n");
            }
        }
        in.close();
        return result;
    }

    /**
     * Writes <str> to <filename>, overwriting it if it's already
     * there.
     */
    public static void writeFile(String filename, String str) throws IOException {
	FileWriter out = new FileWriter(filename);
	out.write(str, 0, str.length());
	out.close();
    }

    /**
    /** replaces in all occurances
    *@modifies: nothing.
    *@effects: constructs a new String from orig, replacing all occurances of oldSubStr with newSubStr.
    *@returns: a copy of orig with all occurances of oldSubStr replaced with newSubStr.
    *
    * if any of arguments are null, returns orig.
    */
    public static synchronized String replaceAll( String orig, String oldSubStr, String newSubStr )
    {
	if (orig==null || oldSubStr==null || newSubStr==null) {
	    return orig;
	}
	// create a string buffer to do replacement
	StringBuffer sb = new StringBuffer(orig);
	// keep track of difference in length between orig and new
	int offset = 0;
	// keep track of last index where we saw the substring appearing
	int index = -1;
	
	while (true) {

	    // look for occurrence of old string
	    index = orig.indexOf(oldSubStr, index+1);
	    if (index==-1) {
		// quit when we run out of things to replace
		break;
	    }
	    
	    // otherwise, do replacement
	    sb.replace(index - offset, 
		       index - offset + oldSubStr.length(), 
		       newSubStr);

	    // increment our offset
	    offset += oldSubStr.length() - newSubStr.length();
	}

	// return new string
	return sb.toString();
    }

    /**
     * Get a name for <type> that is a legal java type declaration.
     * Mostly unwraps array dimensions and adds [] as appropriate.
     */
    public static String asTypeDecl(Class type) {
	// count dimensions of array
	int dims = 0;
	while (type.isArray()) {
	    dims++;
	    type = type.getComponentType();
	}
	StringBuffer result = new StringBuffer(type.getName());
	for (int i = 0; i < dims; i++) {
	    result.append("[]");
	}
	return result.toString();
    }
}
