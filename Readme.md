# sMap: Evolution of Independent, Dependent and Conditioned Discrete Characters in a Bayesian Framework

[![DOI](https://zenodo.org/badge/DOI/10.5281/zenodo.4293924.svg)](https://doi.org/10.5281/zenodo.4293924)

## Introduction
**sMap** is a program to perform stochastic mapping (Nielsen, 2002; Huelsenbeck, Nielsen and Bollback, 2003) analyses on discrete characters. This kind of analysis involves estimating substitution parameters, reconstructing ancestral states and simulating histories, in order to study the evolution of multiple types of discrete characters (e.g. morphological features, presence of genes, habitats...), without necessarily relying on a single phylogenetic tree.

sMap is written using .NET Core, and is available for Windows, macOS and Linux operating systems; the main program is a command-line utility, but a graphical front-end (sMap-GUI) is also provided. sMap is licensed under a GPLv3 license.

The [sMap manual](https://github.com/arklumpus/sMap/raw/master/sMap.pdf) holds a detailed description of the programs and multiple tutorials. These are written mostly with the command-line version of sMap in mind, but it should be reasonably easy to follow them using the GUI version too.

## Installing sMap
The easiest way to start using sMap is to install the program using the installer for your operating system.
### Windows
First, please uninstall any previous versions of sMap.
Download [`sMap-win-x64.msi`](https://github.com/arklumpus/sMap/releases/latest/download/sMap-win-x64.msi) and double click it. The installer will guide you to the process, and will do three main things:
1. Copy the program files (by default in `C:\Program Files`).
2. Add the installation path to the `PATH` environment variable (so that you can recall sMap from the command line, wherever you are located).
3. Add a shortcut to sMap-GUI to the Start Menu.

Of course, 2 and 3 are optional, and you can decide to skip these steps during the installation.

You can now run the GUI version of sMap by using the shortcut and the command-line version by typing `sMap` in the command line (which you can open by pressing `Win+R` on your keyboard, typing `cmd` and pressing Enter).

### macOS
Download [`sMap-mac-x64.pkg`](https://github.com/arklumpus/sMap/releases/latest/download/sMap-mac-x64.pkg) and double click it. The installer will guide you to the process, and will do two main things:
1. Copy the sMap app to the /Applications folder.
2. Create symlinks to the sMap executables (`Blend-sMap`, `ChainMonitor`, `Merge-sMap`, `NodeInfo`, `Plot-sMap`, `Script-sMap`, `sMap`, `sMap-GUI`, `Stat-sMap`) in the `/usr/bin` folder.

You can now run the GUI version of sMap by opening the app in your Applications folder and the command-line version by typing `sMap` in a terminal window.

### Linux
sMap is officially supported on Debian stretch+, Ubuntu 16.04+, Fedora 29+, and CentOS 7+. An automatic installer is available, which may also work on other distributions.
Open a terminal window. Download the installer using `wget` or `curl` (whichever you prefer/have available):

	wget https://github.com/arklumpus/sMap/releases/latest/download/sMap-linux-x64.run

(To use `curl`, replace `wget` with `curl -LO`). Make the downloaded file executable and execute it as root:

	chmod +x sMap-linux-x64.run
	su -c "./sMap-linux-x64.run"

You should be prompted for the super-user password. The installer will:

1. Copy the sMap files to `/usr/lib/sMap` (this can be changed).
2. Create symlinks to the executables (`Blend-sMap`, `ChainMonitor`, `Merge-sMap`, `NodeInfo`, `Plot-sMap`, `Script-sMap`, `sMap`, `sMap-GUI`, `Stat-sMap`) in `/usr/bin` (this step can be skipped).

You can now run the command-line version of sMap by typing `sMap` in the command line and (if you are using a desktop environment) the GUI version by typing `sMap-GUI`. You may wish to create a desktop shortcut to `sMap-GUI` using your distribution's tools.

## Checking that everything works
After having installed sMap, you can run a simple analysis to confirm that everything works as it should. Here are separate instructions for the command-line and GUI version of sMap; ideally you should test both.

### Command-line version (any OS)

Download [`Test-analysis.zip`](https://github.com/arklumpus/sMap/raw/master/TestAnalysis/TestAnalysis.zip) and save it in a location where you have write permissions (e.g. your Documents or Desktop folder).

Open a command-line window and navigate to that folder. Assuming that you have created symlinks/added sMap to the `PATH` environment variable, you can then type:

	mkdir test-output
	sMap -a Test-analysis.zip -o test-output/analysis

This will create a folder called `test-output` and run a quick Maximum-Likelihood analysis on the Cerataphidini dataset (Stern, 1998) that is also used for the tutorials in the sMap manual. The program should exit without error messages, and multiple files should appear in the `test-output` folder.

### GUI version (any OS)

Download [`Cerataphidini.txt`](https://raw.githubusercontent.com/arklumpus/sMap/master/TestAnalysis/Cerataphidini.txt) and [`Cerataphidini.tre`](https://raw.githubusercontent.com/arklumpus/sMap/master/TestAnalysis/Cerataphidini.tre). Open sMap-GUI and start the sMap Wizard. Choose `Cerataphidini.txt` as data file and `Cerataphidini.tre` as tree file; leave all other settings to default and click `Confirm` twice, then `Run analysis`.

Once the analysis finishes, close the dialog and the analysis window, then click on the `Plot set 0...` button and on `Plot preview...`. You should now see a plot of the analysis. If you do, congratulations: you have correctly installed sMap!

## Alternative palettes and colour blindness
The plots produced by sMap convey information primarily through colour. The default palette used by sMap is not colour-blind safe; however, it is possible to enable alternative colour palettes that are colour-blind safe either during the sMap installation process, or afterwards, following the instructions in [Resources/Palettes](Resources/Palettes).

## Troubleshooting and known issues
- On macOS, if when you try to start the GUI version of sMap by clicking on the app you get a message complaining that the application may be damaged or incomplete, there might be some permission issues on the starter script. Open a terminal and enter:

		chmod +x /Applications/sMap.app/Contents/MacOs/sMap

	Then try again. If it still does not work, open a terminal and type:
	
		/Applications/sMap.app/Contents/MacOs/sMap

	This should hopefully lead at least to a more informative error message.

- Known issue on Linux and macOS: sMap-GUI may become randomly unresponsive and need to be forcefully closed. This is because sMap uses the Avalonia framework for its UI ([http://avaloniaui.net/](http://avaloniaui.net/)), which is stil in beta. No workaround for now, unfortunately :-( Just make sure that you save your progress (e.g. the output of the analysis) whenever you can. Also note that if this happens while an analysis is running, the analysis will still run in the background (if you started sMap-GUI from a command-line, you should see the analysis output in the console). In this case you may want to wait until the analysis finishes before killing the program, so that you can scavenge the results!

## Manual installation
If you wish to have more control over the installation process, you can manually install sMap following these instructions.

### Windows
Download the [`sMap-win-x64.zip`](https://github.com/arklumpus/sMap/releases/latest/download/sMap-win-x64.zip) archive, which contains the binaries and libraries for sMap on Windows. Extract the compressed folder somewhere. You can now start the GUI version of sMap by double clicking the `sMap-GUI.exe` executable, or the command-line version by opening a command line window in the extracted folder and typing `sMap`.

If you wish, you can also add the folder where the sMap executables are located to the `PATH` environment variable:
  - Press `Win+R` on the keyboard to bring up the "Run" window, and enter `SystemPropertiesAdvanced`, then press `Enter`.
  - Click on the `Environment Variables...` button in the bottom-right corner.
  - Double click on the `Path` entry in the `User variables` section.
  - Double click on the first empty line and enter the path of the folder where you have extracted the sMap executables.
  - Click `OK` three times.

### macOS
Download the [`sMap-mac-x64.dmg`](https://github.com/arklumpus/sMap/releases/latest/download/sMap-mac-x64.dmg) disk image file and double click to mount it. Open the `sMap` disk that should have appeared on your desktop and drag the `sMap` app to the `Applications` folder. You can now start the GUI version of sMap using the icon in your applications and the command-line version by opening a terminal and typing:

	/Applications/sMap.app/Contents/sMap/sMap

You can also create symlinks to the sMap executables in a folder that is included in your `PATH` (such as `/usr/bin`): open a terminal and type:

	ln -s /Applications/sMap.app/Contents/sMap/Blend-sMap /Applications/sMap.app/Contents/sMap/ChainMonitor /Applications/sMap.app/Contents/sMap/Merge-sMap /Applications/sMap.app/Contents/sMap/NodeInfo /Applications/sMap.app/Contents/sMap/Plot-sMap /Applications/sMap.app/Contents/sMap/Script-sMap /Applications/sMap.app/Contents/sMap/sMap /Applications/sMap.app/Contents/sMap/sMap-GUI /Applications/sMap.app/Contents/sMap/Stat-sMap /usr/bin/

This will allow you to run sMap from the command line in any folder.

### Linux

#### All distributions

Download the [`sMap-linux-x64.tar.gz`](https://github.com/arklumpus/sMap/releases/latest/download/sMap-linux-x64.tar.gz) archive and extract it:

	wget https://github.com/arklumpus/sMap/releases/latest/download/sMap-linux-x64.tar.gz
	tar -xzf sMap-linux-x64.tar.gz
	rm sMap-linux-x64.tar.gz

Depending on  your system, you may want to replace `wget` with `curl -LO`. This will create a folder called `sMap-linux-x64`, which contains the sMap executables. You can now run sMap by typing `sMap-linux-x64/sMap` and the GUI version by typing `sMap-linux-x64/sMap-GUI`.

You can also create symlinks to the sMap executables in a folder that is included in your `PATH` (such as `/usr/bin`): open a terminal and type:

	ln -s "$(pwd)"/sMap-linux-x64/Blend-sMap "$(pwd)"/sMap-linux-x64/ChainMonitor "$(pwd)"/sMap-linux-x64/Merge-sMap "$(pwd)"/sMap-linux-x64/NodeInfo sMap-linux-x64/Plot-sMap sMap-linux-x64/Script-sMap "$(pwd)"/sMap-linux-x64/sMap "$(pwd)"/sMap-linux-x64/sMap-GUI "$(pwd)"/sMap-linux-x64/Stat-sMap /usr/bin/

If you wish, you can create a desktop shortcut to `sMap-GUI` using your distribution's tools.

## Compiling sMap from source

To be able to compile sMap from source, you will need to install the [.NET Core 3 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.0) or higher for your operating system.

You can use [Microsoft Visual Studio](https://visualstudio.microsoft.com/it/vs/) to compile the program. The following instructions will cover compiling sMap from the command line, instead.

First of all, you will need to download the sMap source code: [sMap-1.0.6.tar.gz](https://github.com/arklumpus/sMap/archive/v1.0.6.tar.gz) and extract it somewhere.

### Windows
Open a command-line window in the folder where you have extracted the source code, and type:

	BuildAll <Target>

Where `<Target>` can be one of `Win-x64`, `Linux-x64` or `Mac-x64` depending on which platform you wish to generate executables for.

In the Release folder and in the appropriate subfolder for the target platform you selected, you will find the compiled program.

### macOS and Linux
Open a terminal in the folder where you have extracted the source code, and type:

	./BuildAll.sh <Target>

Where `<Target>` can be one of `Win-x64`, `Linux-x64` or `Mac-x64` depending on which platform you wish to generate executables for.

In the Release folder and in the appropriate subfolder for the target platform you selected, you will find the compiled program.

If you receive an error about permissions being denied, try typing `chmod +x BuildAll.sh` first.

## Licence
See the [sMap manual](https://github.com/arklumpus/sMap/raw/master/sMap.pdf) for licensing information.