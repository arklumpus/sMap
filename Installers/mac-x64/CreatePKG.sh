#!/usr/bin/env zsh

version=$(strings ../../Release/mac-x64/sMap.app/Contents/sMap/sMap.dll | grep -A2 "Release" | head -n3 | tail -n1)

echo
echo -e "\033[104m\033[97m Setting version $version \033[0m"
echo

rm -f sMap.pkgproj

cp sMap.pkgproj.original sMap.pkgproj

sed -i '' "s/@@VersionHere@@/$version/g" sMap.pkgproj

echo
echo -e "\033[104m\033[97m Creating PKG \033[0m"
echo

packagesbuild sMap.pkgproj

rm sMap.pkgproj

cd ../../Release/mac-x64

pkgbuild --install-location /Applications --component "sMap.app" "sMap_signed.pkg"

mv "sMap_signed.pkg" ../../Installers/mac-x64/

cd ../../Installers/mac-x64/

pkgutil --expand "sMap_signed.pkg" "sMap_signedPKG"

pkgutil --expand "sMap.pkg" "sMapPKG"

rm "sMapPKG/sMap.pkg/Payload"

mv "sMap_signedPKG/Payload" "sMapPKG/sMap.pkg/"

rm "sMap.pkg" "sMap_signed.pkg"
rm -r "sMap_signedPKG"

pkgutil --flatten "sMapPKG" "sMap.pkg"

rm -r "sMapPKG"

echo
echo -e "\033[104m\033[97m Signing PKG \033[0m"
echo

productsign --sign "$1" "sMap.pkg" "sMap_signed.pkg"

if [ -f "sMap_signed.pkg" ]; then
	mv "sMap_signed.pkg" "sMap.pkg"
fi

pkgutil --check-signature "sMap.pkg"

echo
echo -e "\033[104m\033[97m Notarizing PKG \033[0m"
echo

requestID=$(xcrun altool --notarize-app -f "sMap.pkg" --primary-bundle-id "io.github.arklumpus.sMap" -u "$2" -p "$3" | grep "RequestUUID" | cut -d" " -f 3)

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
    echo -e "\033[104m\033[97m Stapling PKG \033[0m"
    echo

	xcrun stapler staple sMap.pkg
	xcrun stapler validate sMap.pkg

else

    echo
    echo -e "\033[101m\033[97m PKG notarization failed! \033[0m"
    echo

fi

echo
echo -e "\033[94mAll done!\033[0m"
echo
