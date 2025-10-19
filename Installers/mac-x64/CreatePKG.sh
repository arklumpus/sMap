#!/usr/bin/env zsh

version=$(strings ../../Release/mac-x64/sMap.app/Contents/sMap/sMap.dll | grep -A1 "Release" | head -n2 | tail -n1)

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

xcrun notarytool submit sMap.pkg --apple-id "$2" --password "$3" --team-id "$4" --wait
xcrun stapler staple sMap.pkg
xcrun stapler validate sMap.pkg

echo
echo -e "\033[94mAll done!\033[0m"
echo
