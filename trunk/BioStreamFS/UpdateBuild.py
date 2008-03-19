usage = """Really dumb updater of build.bat -- just call python UpdateBuild.py"""

fileBat = "build.bat"
fileFsharpp = "BioStreamFS.fsharpp"

reLstRegexp = '"([\w\s\-\.\r\n])*GOTO DONE'
def reLstSub(lst):
	return '" '+' '.join(lst+['biostreamfs.fs'])+'\nGOTO CLEAN'
	
import shutil
import sys
import re

def main(fileBat=fileBat, fileFsharpp=fileFsharpp):
	shutil.copyfile(fileBat, fileBat+".bak")
	f = open(fileFsharpp)
	line = None
	while line != '"Files"':
		line = f.readline().strip()
		if line == '':
			print 'Error: file %s ended before encountering "Files" cue' % fileFsharpp
			return 1
	f.readline() # { 
	lst = []
	line = f.readline().strip()
	while line != '}':
		filename = line[1:-1] # get rid of surrounding quotes
		print 'Adding "%s"' % filename
		lst += [filename]
		f.readline() # {
		f.readline() # "ProjRelPath" = "T"
		f.readline() #}
		line = f.readline().strip()
		if line == '':
			print 'Error: file %s ended before encountering "}" cue' % fileFsharpp
			return 1
	f.close()
	
	f = open(fileBat)
	bat = f.read()
	f.close()
	
	reLst = re.compile(reLstRegexp)
	if not reLst.search(bat):
		print 'Error: cannot find anchors in %s to update new list of filenames' % fileBat
		return 1
	bat = reLst.sub(reLstSub(lst), bat)
	
	print bat
	
	fw = open(fileBat, 'w')
	fw.write(bat)
	fw.close()
	
if __name__ == '__main__':
	sys.exit(main())