#!/usr/bin/env zsh

if [ "$#" -ne 5 ]; then
    echo "This script requires the following parameters:"
    echo "    Developer ID Application signing identity"
    echo "    Developer ID Installer signing identity"
    echo "    Apple ID (email address)"
    echo "    App-specific password for the Apple ID"
    echo "    Developer team ID"
    exit 64
fi

rm -rf Binary/mac-x64

mkdir -p Binary/mac-x64

cd Installers/mac-x64/

./CreateDMG.sh $1 $3 $4 $5

cd ../..

mv sMap.dmg Binary/mac-x64/sMap-mac-x64.dmg

cd Installers/mac-x64

./CreatePKG.sh $2 $3 $4 $5

mv sMap.pkg ../../Binary/mac-x64/sMap-mac-x64.pkg

cd ../..

