@echo off

echo.

set platform=%1

set found=0

if "%platform%" == "Linux-x64" set found=1
if "%platform%" == "Win-x64" set found=1
if "%platform%" == "Mac-x64" set found=1

if %found% == 0 (
	echo [91mInvalid platform specified![0m Valid options are: [94mLinux-64[0m, [94mWin-x64[0m or [94mMac-x64[0m
	exit /B 64
)

echo Building with target [94m%1[0m


echo.
echo [104;97mDeleting previous build...[0m

for /f %%i in ('dir /a:d /b Release\%1\*') do rd /s /q Release\%1\%%i
del Release\%1\* /s /f /q 1>nul

echo.
echo [104;97mCopying common resources...[0m

xcopy Resources\%1 Release\%1\ /s /y /h

echo.
echo [104;97mBuilding sMap...[0m

cd sMap
dotnet publish -c Release /p:PublishProfile=Properties\PublishProfiles\%1.pubxml
cd ..

echo.
echo [104;97mBuilding Plot-sMap...[0m

cd Plot-sMap
dotnet publish -c Release /p:PublishProfile=Properties\PublishProfiles\%1.pubxml
cd ..

echo.
echo [104;97mBuilding NodeInfo...[0m

cd NodeInfo
dotnet publish -c Release /p:PublishProfile=Properties\PublishProfiles\%1.pubxml
cd ..

echo.
echo [104;97mBuilding ChainMonitor...[0m

cd ChainMonitor
dotnet publish -c Release /p:PublishProfile=Properties\PublishProfiles\%1.pubxml
cd ..

echo.
echo [104;97mBuilding Blend-sMap...[0m

cd Blend-sMap
dotnet publish -c Release /p:PublishProfile=Properties\PublishProfiles\%1.pubxml
cd ..

echo.
echo [104;97mBuilding Stat-sMap...[0m

cd Stat-sMap
dotnet publish -c Release /p:PublishProfile=Properties\PublishProfiles\%1.pubxml
cd ..

echo.
echo [104;97mBuilding Merge-sMap...[0m

cd Merge-sMap
dotnet publish -c Release /p:PublishProfile=Properties\PublishProfiles\%1.pubxml
cd ..

echo.
echo [104;97mBuilding Script-sMap...[0m

cd Script-sMap
dotnet publish -c Release /p:PublishProfile=Properties\PublishProfiles\%1.pubxml -f net6.0
cd ..

echo.
echo [104;97mBuilding sMap-GUI...[0m

cd sMap-GUI
dotnet publish -c Release /p:PublishProfile=Properties\PublishProfiles\%1.pubxml -f net6.0
cd ..

echo.
echo [94mAll done![0m
