using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

Dictionary<string, string> Guids = new Dictionary<string, string>();
string[] savedGuids = File.ReadAllLines("savedGuids.txt");
for (int i = 0; i < savedGuids.Length; i++)
{
	Guids.Add(savedGuids[i].Substring(0, savedGuids[i].IndexOf("\t")), savedGuids[i].Substring(savedGuids[i].IndexOf("\t") + 1));
}

string[] files = (from el in Directory.GetFiles("SourceDir") select Path.GetFileName(el)).ToArray();

Dictionary<string, string> newGuids = new Dictionary<string, string>();

string tbr = "";

for (int i = 0; i < files.Length; i++)
{
	string guid;
	if (Guids.ContainsKey(files[i]))
	{
		guid = Guids[files[i]];
	}
	else
	{
		guid = Guid.NewGuid().ToString();
		newGuids.Add(files[i], guid);
	}
	
	tbr += "\t\t<Component Directory=\"INSTALLFOLDER\" Id=\"" + files[i].Replace("-", ".") + "\" Guid=\"" + guid + "\">\n\t\t\t<File Id=\"" + files[i].Replace("-", ".") + "\" KeyPath=\"yes\" Source=\"SourceDir\\" + files[i] + "\" />\n\t\t</Component>\n";
}


string tbrPalettes = "";

string[] palettes = (from el in Directory.GetFiles("..\\..\\Resources\\Palettes", "*.palette") select Path.GetFileName(el)).ToArray();

for (int i = 0; i < palettes.Length; i++)
{
	string guid;
	if (Guids.ContainsKey(palettes[i]))
	{
		guid = Guids[palettes[i]];
	}
	else
	{
		guid = Guid.NewGuid().ToString();
		newGuids.Add(palettes[i], guid);
	}

	string featureId = palettes[i].Replace(".palette", "");

	string[] lines = File.ReadAllLines("..\\..\\Resources\\Palettes\\" + palettes[i]);
	string title = lines[0].Substring(1).Replace("\"", "&quot;").Replace("'", "&apos;");
	string desc = lines[1].Substring(1).Replace("\"", "&quot;").Replace("'", "&apos;");

	tbrPalettes += "\t\t\t\t<Feature Id=\"" + featureId + "\" Title=\"" + title + "\" Description=\"" + desc + "\" Level=\"3\" AllowAdvertise='no' InstallDefault='local'>\n";
	tbrPalettes += "\t\t\t\t\t<Component Directory=\"INSTALLFOLDER\" Id=\"" + palettes[i] + "\" Guid=\"" + guid + "\">\n\t\t\t\t\t\t<File Id=\"" + palettes[i] + "\" KeyPath=\"yes\" Source=\"..\\..\\Resources\\Palettes\\" + palettes[i] + "\" />\n";
	tbrPalettes += "\t\t\t\t\t</Component>\n\t\t\t\t</Feature>\n";
}

string version = System.Reflection.AssemblyName.GetAssemblyName(@"..\..\Release\win-x64\sMap.dll").Version.ToString(3);

string file = File.ReadAllText("sMap.wxs.original");
file = file.Replace("@@VersionHere@@", version);
file = file.Replace("<!-- Files here -->", tbr);
file = file.Replace("<!-- Palettes here -->", tbrPalettes);
File.WriteAllText("sMap.wxs", file);

string newGuidsString = "";

foreach (KeyValuePair<string, string> kvp in newGuids)
{
	newGuidsString += kvp.Key + "\t" + kvp.Value + "\n";
}

File.AppendAllText("savedGuids.txt", newGuidsString);
