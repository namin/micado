SET appname=%1
if (%appname%) == (clean) GOTO CLEAN

SET main=%appname%Main

IF NOT EXIST %main%.class GOTO GENERATE
:RUN
java -classpath ".;..\lib\ij.jar" %main%
GOTO DONE

:GENERATE
if (%appname%) == (Template) GOTO BUILD
python MakeApp.py %appname%

:BUILD
javac -classpath ".;..\lib\ij.jar" %appname%*.java
GOTO RUN

:CLEAN
del *.class

:DONE