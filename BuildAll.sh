#!/bin/sh

found=0

if [ "$1" = "Win-x64" ] || [ "$1" = "Linux-x64" ] || [ "$1" = "Mac-x64" ]; then
	found=1
fi

if [ $found -eq 0 ]; then
	printf "\n[91mInvalid platform specified![0m Valid options are: [94mLinux-64[0m, [94mWin-x64[0m or [94mMac-x64[0m\n\n"
	exit 64
fi

platform=$(echo "$1" | tr '[:upper:]' '[:lower:]')

printf "\nBuilding with target [94m$1[0m\n"

printf "\n"
printf "[104;97mDeleting previous build...[0m\n"

rm -rf Release/$platform/*

printf "\n"
printf "[104;97mCopying common resources...[0m\n"

cp -r Resources/$platform/* Release/$platform/

printf "\n"
printf "[104;97mBuilding sMap...[0m\n"

cd sMap
dotnet publish -c Release /p:PublishProfile=Properties/PublishProfiles/$platform.pubxml
cd ..

printf "\n"
printf "[104;97mBuilding Plot-sMap...[0m\n"

cd Plot-sMap
dotnet publish -c Release /p:PublishProfile=Properties/PublishProfiles/$platform.pubxml
cd ..

printf "\n"
printf "[104;97mBuilding NodeInfo...[0m\n"

cd NodeInfo
dotnet publish -c Release /p:PublishProfile=Properties/PublishProfiles/$platform.pubxml
cd ..

printf "\n"
printf "[104;97mBuilding ChainMonitor...[0m\n"

cd ChainMonitor
dotnet publish -c Release /p:PublishProfile=Properties/PublishProfiles/$platform.pubxml
cd ..

printf "\n"
printf "[104;97mBuilding Blend-sMap...[0m\n"

cd Blend-sMap
dotnet publish -c Release /p:PublishProfile=Properties/PublishProfiles/$platform.pubxml
cd ..

printf "\n"
printf "[104;97mBuilding Merge-sMap...[0m\n"

cd Merge-sMap
dotnet publish -c Release /p:PublishProfile=Properties/PublishProfiles/$platform.pubxml
cd ..

printf "\n"
printf "[104;97mBuilding Stat-sMap...[0m\n"

cd Stat-sMap
dotnet publish -c Release /p:PublishProfile=Properties/PublishProfiles/$platform.pubxml
cd ..

printf "\n"
printf "[104;97mBuilding Script-sMap...[0m\n"

cd Script-sMap
dotnet publish -c Release /p:PublishProfile=Properties/PublishProfiles/$platform.pubxml -f netcoreapp3.0
cd ..

printf "\n"
printf "[104;97mBuilding sMap-GUI...[0m\n"

cd sMap-GUI
dotnet publish -c Release /p:PublishProfile=Properties/PublishProfiles/$platform.pubxml -f netcoreapp3.0
cd ..

printf "\n"
printf "[94mAll done![0m\n\n"
