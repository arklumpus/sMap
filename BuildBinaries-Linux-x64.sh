rm -rf Binary/linux-x64

mkdir -p Binary/linux-x64

cd Installers/linux-x64

./make.sh

mv sMap-linux-x64.run ../../Binary/linux-x64/
mv sMap-linux-x64.tar.gz ../../Binary/linux-x64/

cd ../..
