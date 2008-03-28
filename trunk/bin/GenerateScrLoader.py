usageDoc = """python GenerateScrLoader.py [--release] [generateDirectory] [pluginDirectory]

GenerateScrLoader.py generates the file micado-debug.scr or micado.scr.orig,
which, if loaded with the /b option when starting AutoCAD, will load
all the micado plugin libraries.

See ..\\ReadMe.txt for more information on running micado.

-r or --release: generate micado.scr.orig, instead of micado-debug.scr

generateDirectory: the directory in which the .scr file is generated
for micado-debug.scr, defaults to C:\\Program Files\\Autodesk\\Acade 2008\\UserDataCache\\
for micado.scr.orig, defaults to $MICADO_DIR\\trunk\\micado-pub\\

pluginDirectory: the directory where the micado libraries are located
for micado-debug.scr, defaults to $MICADO_DIR\\trunk\\debug\\
for micado.scr.orig, defaults to C:\\micado\\
"""

generateDirectoryDebug = "C:\\Program Files\\Autodesk\\Acade 2008\\UserDataCache\\"
generateDirectoryRelease = "$MICADO_DIR\\trunk\\micado-pub\\"

pluginDirectoryDebug = "$MICADO_DIR\\trunk\\debug\\"
pluginDirectoryRelease = "C:\\micado\\"

scrFilenameDebug = "micado-debug.scr"
scrFilenameRelease = "micado.scr.orig"

arxloadFilenames = ["BioStreamDB.dbx"]
netloadFilenames = ["BioStreamMg.dll", "BioStreamCS.dll", "biostreamfs.dll"]
debugExtraNetloadFilenames = ["plugintest.dll"]

arxloadCommand = "(arxload \"%s%s\")"
netloadCommand = "(command \"Netload\" \"%s%s\")"

micadoDirSymbol = "$MICADO_DIR"

import os
import os.path
import sys
import getopt

def scrLines(pluginDirectory):
    """returns a list of the lines of the .scr file"""
    lines = []
    lines.extend([arxloadCommand % (pluginDirectory, filename) for filename in arxloadFilenames])
    lines.extend([netloadCommand % (pluginDirectory, filename) for filename in netloadFilenames])
    return [(line.replace("\\", "/"))+"\n" for line in lines]

def getMicadoDir():
    """returns the micado top-level directory, inferred from the location of this script"""
    micadoTrunk, _ = os.path.split(os.getcwd())
    micadoDir, _ = os.path.split(micadoTrunk)
    return micadoDir

def generateScrLoader(scrFilename, generateDirectory, pluginDirectory):
    """generates the .scr file as scrFilename in generateDirectory
    using pluginDirectory as the directory where the micado libraries are located"""
    scrFilepath = os.path.join(generateDirectory, scrFilename)
    f = open(scrFilepath, 'w')
    f.writelines(scrLines(pluginDirectory))
    f.close()
    print "generated", scrFilepath

def usage():
    print usageDoc
    
def main():
    try:
        opts, args = getopt.getopt(sys.argv[1:], "rh", ["release", "help"])
    except getopt.GetoptError, err:
        print str(err)
        usage()
        sys.exit(2)
    if len(args) > 2:
        print "too many arguments"
        usage()
        sys.exit(2)

    release = False
    for o, a in opts:
        if o in ("-r", "--release"):
            release = True
        elif o in ("-h", "--help"):
            usage()
            sys.exit()
        else:
            assert False, "unhandled option"

    if release:
        scrFilename = scrFilenameRelease
        generateDirectory = generateDirectoryRelease
        pluginDirectory = pluginDirectoryRelease
    else:
        scrFilename = scrFilenameDebug
        generateDirectory = generateDirectoryDebug
        pluginDirectory = pluginDirectoryDebug
        netloadFilenames.extend(debugExtraNetloadFilenames)
        
    if len(args) >= 2:
        pluginDirectory = args[1]
    if len(args) >= 1:
        generateDirectory = args[0]

    micadoDir = getMicadoDir()
    generateDirectory = generateDirectory.replace(micadoDirSymbol, micadoDir)
    pluginDirectory = pluginDirectory.replace(micadoDirSymbol, micadoDir)

    print "generateScrLoader(%s, %s, %s)" % (scrFilename, generateDirectory, pluginDirectory)    
    generateScrLoader(scrFilename, generateDirectory, pluginDirectory)
    
if __name__ == '__main__':
    main()