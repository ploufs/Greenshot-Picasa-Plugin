REM copy file to GreenShot folder
@echo off
cls

set GreenShotPath=%1
set PlugInPath="\Plugins\GreenshotPicasaPlugin"

MD %GreenShotPath%%PlugInPath%
copy "bin\Release\GreenshotPicasaPlugin.gsp" %GreenShotPath%%PlugInPath%\GreenshotPicasaPlugin.gsp

MD %GreenShotPath%\Languages\%PlugInPath%
copy "Languages\*.*" %GreenShotPath%\Languages\%PlugInPath%