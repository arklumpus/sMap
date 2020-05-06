#!/bin/bash

echo "# Colour palettes" > Readme.md
echo "" >> Readme.md
echo "sMap allows the use of custom palettes to replace the default colour palette for all plots produced by the program. If no custom palette is chosen, the default palette is used." >> Readme.md
echo "" >> Readme.md
echo "Each palette specifies a certain number of colours (which are used, e.g., to represent different character states). If more colours are needed than are specified in the current palette, the colours are instead obtained from a continuous colour scale. If no custom palette is specified, in this case equi-spaced colours from the HSL spectrum are used. If any custom colour palette (including the \"Basic\" palette) is used, equi-spaced colours from the [Viridis colour scale](https://cran.r-project.org/web/packages/viridis/vignettes/intro-to-viridis.html) are used (regardless of which custom colour palette is in use)." >> Readme.md
echo "" >> Readme.md
echo "It is possible to choose a custom palette during the installation of sMap. If you wish to change colour palette after installing sMap, you can delete the old palette file (if any) from the folder containing the sMap executable and replace it with one of the files in this folder. If multiple palette files are placed in the sMap executable folder, the first one in alphabetical order will be used." >> Readme.md
echo "" >> Readme.md
echo "You can also create a custom palette file: this file should contain one colour per line in \`R, G, B\` format. \`#\` signals that the rest of the line is a comment. Open one of the files in this folder with a text editor for examples. If you would like to have your palette be included in sMap and appear in the list in this page, please open an \"Issue\" in this repository." >> Readme.md
echo "" >> Readme.md
echo "## Available palettes" >> Readme.md

rm colours/*

for i in *.palette; do
	name=$(grep "#" $i | head -n1)
	name=${name:1}
	echo "" >> Readme.md
	echo "### $name [\`$i\`](https://raw.githubusercontent.com/arklumpus/sMap/master/Resources/Palettes/$i)" >> Readme.md
	echo "" >> Readme.md
	desc=$(grep "#" $i | head -n2 | tail -n1)
	desc=${desc:1}
	echo "$desc" >> Readme.md
	echo "" >> Readme.md
	
	comm="#"
	
	num=0
	
	while IFS= read -r line; do
		colour=${line%%$comm*}
		colour=${colour// /}
		if [ ! -z "$colour" ]; then
			convert -size 64x64 xc:rgb\($colour\) colours/$i.$num.png
			echo "![col$num](colours/$i.$num.png)" >> Readme.md
			num=$((num+1))
		fi
	done < $i
	
done

