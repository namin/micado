cd ..\..\doc\images\icons\

del *.png
copy ..\..\..\trunk\micado-pub\Icons\*.BMP .

for %%f in (*.BMP) do convert -transparent "rgb ( 236,233,216 ) " %%f %%f.png

del *.BMP

for /f "tokens=1-3 delims=." %%i in ('dir *.png /b') do ren "%%i.%%j.%%k" "%%i.%%k"

cd ..\..\..\trunk\bin