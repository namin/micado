usage = """
python MakeApp.py [--template TemplateName] AppName [appname.png appname.dat]
TemplateName -- the prefix of the template
AppName -- the prefix of the application
appname.png -- the png image file exported from micado
appname.dat -- the java data file exported from micado

if appname.png and appname.dat are not provided
then the png image is AppName.png and
the port locations file is AppName.dat
"""

import re

reImageSize = re.compile(r'{Width=(?P<width>\d+), Height=(?P<height>\d+)}')
reNumberOfControlLines = re.compile(r'number of control lines: (?P<n>\d+)')
reNumberOfInstructions = re.compile(r'number of instructions: (?P<n>\d+)')
reSubDriverControlLines = re.compile(r'(?P<d>int CONTROL_LINES = )(?P<n>\d+)')
reSubDriverInstructions = re.compile(r'(?P<d>int INSTRUCTIONS = )(?P<n>\d+)')
reSubGUIImageName = re.compile(r'(?P<b>IMAGE_FILENAME = ")(?P<imagepng>\w+.png)(?P<a>")')
reSubGUIImageWidth = re.compile(r'(?P<d>int IMAGE_WIDTH = )(?P<n>\d+)')
reSubGUIImageHeight = re.compile(r'(?P<d>int IMAGE_HEIGHT = )(?P<n>\d+)')
reLocations = re.compile(r'(?P<all>// BEGIN port locations((.|[\n\r])*)// END port locations)')
reInstructions = re.compile(r'(?P<all>// BEGIN instructions(?P<instructions>(.|[\n\r])*)// END instructions)')
reInstructionPumps = re.compile(r'(?P<all>// BEGIN instruction pumps(?P<pumps>(.|[\n\r])*)// END instruction pumps)')
reInstructionShortcuts = re.compile(r'(?P<all>// BEGIN instruction shortcuts((.|[\n\r])*)// END instruction shortcuts)')

aliasTemplate = """
    public static void %s() {
        RunInstruction(%d);    
    }
    """

aliasTemplate2 = """
    public static void %s(int runTime) {
        RunInstruction(%d, runTime);    
    }
    """
aliasParamTemplate = """
    public static void %s(%s) {
        %s
        RunInstruction(%s_instruction_map[i]);    
    }
    """

aliasParamTemplate2 = """
    public static void %s(%s, int runTime) {
        %s
        RunInstruction(%s_instruction_map[i], runTime);    
    }
    """

import sys
import os

def main():
    templatePrefix = None
    appPrefix = None
    apppng = None
    appdat = None
    args = sys.argv[0:]
    if len(sys.argv) > 2 and sys.argv[1] == "--template":
        templatePrefix = sys.argv[2]
        print "using template", templatePrefix
        args = sys.argv[2:]
        args[0] = sys.argv[0]
    if len(args) == 2:
        appPrefix = args[1]
        apppng = appPrefix + ".png"
        appdat = appPrefix + ".dat"
    elif len(args) == 4:
        appPrefix = args[1]
        apppng = args[2]
        appdat = args[3]
    else:
        print usage
        return

    print 'makeApp('+appPrefix+', '+apppng+', '+appdat+', '+str(templatePrefix)+')'
    pngok = os.path.exists(apppng)
    datok = os.path.exists(appdat)
    if (not pngok) or (not datok):
        if not pngok:
            print apppng, "does not exists!"
        if not datok:
            print appdat, "does not exists!"
        print "Script aborted!"
        return
    makeApp(appPrefix, apppng, appdat, templatePrefix)
        
        
def getContent(filename):
    f = open(filename, 'r')
    txt = f.read();
    f.close();
    return txt

def allInstructionNames(justInstructions):
    lst = justInstructions.split('\n')[1:-1]
    assert len(lst) % 4 == 0
    nInstructions = len(lst)/4
    names = range(0, nInstructions)
    for i in range(0, nInstructions):
        nameLine = lst[4*i+1]
        names[i] = nameLine[len('// '):]
    return names

def instructionAliasCode(names):
    name2dis = {}
    name2dim = {}
    code = "// BEGIN instruction shortcuts\n"
    for i in range(0, len(names)):
        code += aliasTemplate % (names[i], i)
        code += aliasTemplate2 % (names[i], i)
        addInstruction(names[i], i, name2dis, name2dim)
    normalizeDims(name2dim)
    if len(name2dim.keys()) > 0:
        code += instructionsWithParamsCode(name2dis, name2dim)
    code += "// END instruction shortcuts\n"
    return code

def instructionsWithParamsCode(name2dis, name2dim):
    codel = [instructionWithParamsCode(name, name2dis[name], name2dim[name]) for name in name2dis.keys()]
    return "\n".join(codel)

def instructionWithParamsCode(name, dis, dim):
    map = [-1 for x in range(0, dim2len(dim))]
    for di in dis:
        d = di[0:-1]
        i = di[-1]
        map[d2i(d, dim)] = i
    code = ""
    code += "\n"
    code += "    public static final int[]" + name + "_instruction_map = {"+",".join([str(x) for x in map])+"};"
    code += aliasParamTemplate % (name, paramsCode(dim), d2iCode(dim), name)
    code += aliasParamTemplate2 % (name, paramsCode(dim), d2iCode(dim), name)
    return code

def paramsCode(dim):
    return ", ".join(['int p'+str(i) for i in range(0, len(dim))])

def d2iCode(dim):
    b = dim2b(dim)
    return "int i = " + "+".join(['p'+str(i)+'*'+str(b[i]) for i in range(0, len(b))])+";"

def dim2len(dim):
    p = 1
    for x in dim:
        p *= x
    return p

def dim2b(dim):
    b = [0 for x in dim]
    v = 1
    for i in range(len(dim)-1, -1, -1):
        b[i] = v
        v *= dim[i]
    return b

def d2i(d, dim):
    b = dim2b(dim)
    return sum([b[i]*d[i] for i in range(0, len(d))])
    
def normalizeDims(name2dim):
    for name in name2dim.keys():
        name2dim[name] = [x+1 for x in name2dim[name]]
        
def addInstruction(name_full, i, name2dis, name2dim):
    namel = name_full.split('_')
    if len(namel) > 1:
        name = namel[0]
        d = [int(x) for x in namel[1:]]
        if not name2dis.has_key(name):
            name2dis[name] = [d+[i]]
            name2dim[name] = [x for x in d]
        else:
            dim = name2dim[name]
            if len(dim)!=len(d):
                print "ignoring", name_full, "for parameterized instruction method"
            else:
                name2dis[name].append(d+[i])
                name2dim[name] = [max(dim[x],d[x]) for x in range(0, len(dim))]
                
def makeApp(appPrefix, apppng, appdat, templatePrefix=None):
    if not templatePrefix:
        templatePrefix = "Template"

    files = ['Main', 'Mapping', 'Imaging', 'Driver', 'GUI']
    templatefiles = map(lambda x : templatePrefix + x + '.java', files)
    appfiles = map(lambda x : appPrefix + x + '.java', files)
    contents = map(getContent, templatefiles)

    for i in range(0, len(contents)):
        contents[i] = contents[i].replace(templatePrefix, appPrefix)

    locations = getContent(appdat)
    
    guiIndex = files.index('GUI')
    driverIndex = files.index('Driver')

    m = reImageSize.search(locations)
    imageWidth = m.group('width')
    imageHeight = m.group('height')

    m = reNumberOfControlLines.search(locations)
    nControlLines = m.group('n')

    m = reNumberOfInstructions.search(locations)
    nInstructions = m.group('n')

    m = reLocations.search(locations)
    actualLocations = m.group('all')
    
    m = reInstructions.search(locations)
    actualInstructions = m.group('all')
    justInstructions = m.group('instructions')

    m = reInstructionPumps.search(locations)
    if m:
        actualInstructionPumps = m.group('all')
    else:
        actualInstructionPumps = ",".join(['null' for i in range(0,int(nInstructions))])
    
    instructionCode = instructionAliasCode(allInstructionNames(justInstructions))

    contents[driverIndex] = reSubDriverControlLines.sub(r'\g<d>' + nControlLines, contents[driverIndex], 1)
    contents[driverIndex] = reSubDriverInstructions.sub(r'\g<d>' + nInstructions, contents[driverIndex], 1)
    contents[driverIndex] = reInstructions.sub(actualInstructions, contents[driverIndex], 1)
    contents[driverIndex] = reInstructionShortcuts.sub(instructionCode, contents[driverIndex], 1)
    contents[driverIndex] = reInstructionPumps.sub(actualInstructionPumps, contents[driverIndex], 1)
    contents[guiIndex] = reSubGUIImageWidth.sub(r'\g<d>' + imageWidth, contents[guiIndex], 1)
    contents[guiIndex] = reSubGUIImageHeight.sub(r'\g<d>' + imageHeight, contents[guiIndex], 1)
    contents[guiIndex] = reLocations.sub(actualLocations, contents[guiIndex], 1)
    contents[guiIndex] = reSubGUIImageName.sub(r'\g<b>' + apppng + r'\g<a>', contents[guiIndex], 1)
    
    for i in range(0, len(files)):
        f = open(appfiles[i], 'w')
        f.write(contents[i])
        f.close()

if __name__ == '__main__':
    main()

        
        