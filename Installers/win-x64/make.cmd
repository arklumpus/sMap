@echo off

del sMap.wxs
del sMap.wixobj
del sMap-win-x64.msi

for /f %%i in ('dir /a:d /b SourceDir\*') do rd /s /q SourceDir\%%i
del SourceDir\* /s /f /q 1>nul

xcopy ..\..\Release\win-x64 SourceDir\ /s /y /h

del SourceDir\*.pdb

"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csi.exe" GenerateFileGuids.csx

candle sMap.wxs

light -ext WixUIExtension sMap.wixobj

ren sMap.msi sMap-win-x64.msi
