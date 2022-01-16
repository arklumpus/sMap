#!/usr/bin/env bash

echo
echo -e "\033[104m\033[97m Adding executable flags \033[0m"
echo

cd ../../Release/mac-x64/sMap.app/Contents/sMap/

chmod +x "sMap" "sMap-GUI" "Blend-sMap" "Merge-sMap" "Plot-sMap" "Stat-sMap" "ChainMonitor" "NodeInfo" "Script-sMap" "gs-mac" "createdump"

version=$(strings sMap.dll | grep -A2 "Release" | head -n3 | tail -n1)

echo -e "\033[104m\033[97m Setting version number $version \033[0m"
echo

sed -i '' "s/@@VersionHere@@/$version/g" ../Info.plist

cd ../../../

echo -e "\033[104m\033[97m Signing app \033[0m"
echo

codesign --deep --force --timestamp --options=runtime --entitlements="sMap.entitlements" --sign "$1" "sMap.app/Contents/MacOS/sMap"

codesign --deep --force --timestamp --options=runtime --entitlements="sMap.entitlements" --sign "$1" "sMap.app/Contents/sMap/sMap"

codesign --deep --force --timestamp --options=runtime --entitlements="sMap.entitlements" --sign "$1" "sMap.app/Contents/sMap/sMap-GUI"

codesign --deep --force --timestamp --options=runtime --entitlements="sMap.entitlements" --sign "$1" "sMap.app/Contents/sMap/Blend-sMap"

codesign --deep --force --timestamp --options=runtime --entitlements="sMap.entitlements" --sign "$1" "sMap.app/Contents/sMap/Merge-sMap"

codesign --deep --force --timestamp --options=runtime --entitlements="sMap.entitlements" --sign "$1" "sMap.app/Contents/sMap/Plot-sMap"

codesign --deep --force --timestamp --options=runtime --entitlements="sMap.entitlements" --sign "$1" "sMap.app/Contents/sMap/Stat-sMap"

codesign --deep --force --timestamp --options=runtime --entitlements="sMap.entitlements" --sign "$1" "sMap.app/Contents/sMap/ChainMonitor"

codesign --deep --force --timestamp --options=runtime --entitlements="sMap.entitlements" --sign "$1" "sMap.app/Contents/sMap/NodeInfo"

codesign --deep --force --timestamp --options=runtime --entitlements="sMap.entitlements" --sign "$1" "sMap.app/Contents/sMap/Script-sMap"

codesign --deep --force --timestamp --options=runtime --entitlements="sMap.entitlements" --sign "$1" "sMap.app/Contents/sMap/gs-mac"

codesign --deep --force --timestamp --options=runtime --entitlements="sMap.entitlements" --sign "$1" "sMap.app/Contents/sMap/createdump"

find sMap.app/ -name "*.dylib" -type f -exec codesign --deep --force --timestamp --options=runtime --entitlements="sMap.entitlements" --sign "$1" {} \;

codesign --deep --preserve-metadata="identifier,entitlements,requirements,flags,runtime" --force --timestamp --options=runtime --entitlements="sMap.entitlements" --sign "$1" "sMap.app"

codesign --verify -vvv --strict --deep "sMap.app"

echo
echo -e "\033[104m\033[97m Notarizing app \033[0m"
echo

rm -f "sMap.zip"

ditto -ck --rsrc --sequesterRsrc --keepParent "sMap.app" "sMap.zip"

requestID=$(xcrun altool --notarize-app -f "sMap.zip" --primary-bundle-id "io.github.arklumpus.sMap" -u "$2" -p "$3" | grep "RequestUUID" | cut -d" " -f 3)

echo "Request UUID: $requestID"

breakloop="0"

while [ $breakloop -lt 1 ]; do

    echo "Waiting for 1 minute..."
    sleep 60

    currStatus=$(xcrun altool --notarization-info $requestID -u $2 -p $3 | grep "Status:" | cut -d":" -f 2)

    echo "Status: $currStatus"

    if [ "$currStatus" != " in progress" ]; then
    	if [ "$currStatus" = " success" ]; then
    	    breakloop="2"
    	else
    	    breakloop="1"
    	fi
    fi

done

if [ $breakloop -eq 2 ]; then

    echo
    echo -e "\033[104m\033[97m Stapling app \033[0m"
    echo

	xcrun stapler staple sMap.app
	xcrun stapler validate sMap.app

else

    echo
    echo -e "\033[101m\033[97m App notarization failed! \033[0m"
    echo

fi

rm -f "sMap.zip"

cd ../..

echo
echo -e "\033[104m\033[97m Creating DMG \033[0m"
echo

hdiutil create -srcfolder Release/mac-x64 -volname "sMap" -fs HFS+ -format UDRW -size 350m "sMap.rw.dmg"

device=$(hdiutil attach -readwrite -noverify -noautoopen "sMap.rw.dmg" | grep -e "^/dev/" | head -n1 | cut -f 1)

rm /Volumes/sMap/sMap.entitlements

mkdir /Volumes/sMap/.background/

cp Icons/DMGBackground.png /Volumes/sMap/.background/background.png

echo '
tell application "Finder"
 tell disk "sMap"
  open
  set current view of container window to icon view
  set toolbar visible of container window to false
  set statusbar visible of container window to false
  set the bounds of container window to {100, 100, 795, 660}
  set iconViewOptions to the icon view options of container window
  set arrangement of iconViewOptions to not arranged
  set background picture of iconViewOptions to file ".background:background.png"
  set icon size of iconViewOptions to 128
  set position of item "sMap.app" of container window to {140, 120}
  make new alias file at container window to POSIX file "/Applications" with properties {name:"Applications"}
  set position of item "Applications" of container window to {555, 120}
  close
  open
  update without registering applications
  close
 end tell
end tell' | osascript

cp Icons/sMap-dmg.icns /Volumes/sMap/.VolumeIcon.icns
SetFile -a C /Volumes/sMap

sync
sync

hdiutil detach ${device}

hdiutil convert "sMap.rw.dmg" -format UDZO -o "sMap.dmg"

rm "sMap.rw.dmg"

echo
echo -e "\033[104m\033[97m Signing DMG \033[0m"
echo

codesign --deep --force --timestamp --sign "$1" "sMap.dmg"

codesign --verify --verbose "sMap.dmg"

echo
echo -e "\033[104m\033[97m Notarizing DMG \033[0m"
echo

requestID=$(xcrun altool --notarize-app -f "sMap.dmg" --primary-bundle-id "io.github.arklumpus.sMap" -u "$2" -p "$3" | grep "RequestUUID" | cut -d" " -f 3)

echo "Request UUID: $requestID"

breakloop="0"

while [ $breakloop -lt 1 ]; do

    echo "Waiting for 1 minute..."
    sleep 60

    currStatus=$(xcrun altool --notarization-info $requestID -u $2 -p $3 | grep "Status:" | cut -d":" -f 2)

    echo "Status: $currStatus"

    if [ "$currStatus" != " in progress" ]; then
    	if [ "$currStatus" = " success" ]; then
    	    breakloop="2"
    	else
    	    breakloop="1"
    	fi
    fi

done

if [ $breakloop -eq 2 ]; then

    echo
    echo -e "\033[104m\033[97m Stapling DMG \033[0m"
    echo

	xcrun stapler staple sMap.dmg
	xcrun stapler validate sMap.dmg

else

    echo
    echo -e "\033[101m\033[97m DMG notarization failed! \033[0m"
    echo

fi

echo
echo -e "\033[94mAll done!\033[0m"
echo



