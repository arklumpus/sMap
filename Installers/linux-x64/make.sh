#!/bin/bash

rm -rf sMap_setup/*
mkdir -p sMap_setup/sMap-linux-x64
mkdir -p sMap_setup/Palettes

"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/Roslyn/csi.exe" UpdateSetupWithPalettes.csx

cp -r ../../Release/linux-x64/* sMap_setup/sMap-linux-x64/
cp -r ../../Resources/Palettes/*.palette sMap_setup/Palettes/

rm sMap_setup/sMap-linux-x64/*.pdb

cd sMap_setup

tar -czf sMap-linux-x64.tar.gz sMap-linux-x64/

mv sMap-linux-x64.tar.gz ../

cd ..

makeself-2.4.0/makeself.sh sMap_setup sMap-linux-x64.run "sMap" ./sMap_setup.sh

rm -rf sMap_setup/*