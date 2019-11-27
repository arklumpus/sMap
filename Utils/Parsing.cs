using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class DataMatrix
    {
        public string[][] States { get; private set; }
        public Dictionary<string, double[][]> Data { get; private set; }

        public DataMatrix(DataMatrix original)
        {
            Data = new Dictionary<string, double[][]>();
            foreach (KeyValuePair<string, double[][]> kvp in original.Data)
            {
                Data.Add(kvp.Key, new double[0][]);
            }
            States = new string[0][];
        }

        public int Add(DataMatrix original, int character)
        {
            foreach (KeyValuePair<string, double[][]> kvp in original.Data)
            {
                List<double[]> currentStates = new List<double[]>(Data[kvp.Key]);
                currentStates.Add((double[])kvp.Value[character].Clone());
                Data[kvp.Key] = currentStates.ToArray();
            }
            List<string[]> allCurrentStates = new List<string[]>(States);
            allCurrentStates.Add((string[])original.States[character].Clone());
            States = allCurrentStates.ToArray();
            return States.Length - 1;
        }

        public int Add(Dictionary<string, double[]> data, string[] states)
        {
            foreach (KeyValuePair<string, double[]> kvp in data)
            {
                List<double[]> currentStates = new List<double[]>(Data[kvp.Key]);
                currentStates.Add((double[])kvp.Value.Clone());
                Data[kvp.Key] = currentStates.ToArray();
            }
            List<string[]> allCurrentStates = new List<string[]>(States);
            allCurrentStates.Add(states);
            States = allCurrentStates.ToArray();
            return States.Length - 1;
        }

        public DataMatrix(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                string line = sr.ReadLine();
                line = line.Replace("\t", " ");
                while (line.Contains("  "))
                {
                    line = line.Replace("  ", " ");
                }

                line = line.Trim(' ');

                int numTaxa = int.Parse(line.Substring(0, line.IndexOf(" ")));
                int charCount = int.Parse(line.Substring(line.IndexOf(" ") + 1));

                List<string>[] states = new List<string>[charCount];

                for (int i = 0; i < charCount; i++)
                {
                    states[i] = new List<string>();
                }

                Dictionary<string, List<double>[]> tempData = new Dictionary<string, List<double>[]>();

                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    line = line.Replace("\t", " ");
                    line = line.Trim(' ');
                    string seqName = line.Substring(0, line.IndexOf(" "));
                    List<double>[] parsedData = new List<double>[charCount];
                    for (int i = 0; i < charCount; i++)
                    {
                        parsedData[i] = new List<double>();
                    }

                    string data = line.Substring(line.IndexOf(" ") + 1).Replace(" ", "");

                    for (int i = 0; i < charCount; i++)
                    {
                        if (data[0] != '{')
                        {
                            string state = data.Substring(0, 1);
                            if (!states[i].Contains(state))
                            {
                                states[i].Add(state);
                            }
                            int ind = states[i].IndexOf(state);
                            for (int j = 0; j < ind; j++)
                            {
                                parsedData[i].Add(0);
                            }
                            parsedData[i].Add(1);
                            data = data.Substring(1);
                        }
                        else
                        {
                            string state = data.Substring(1, data.IndexOf("}") - 1);
                            string[] splitState = state.Split(',');
                            for (int j = 0; j < splitState.Length; j++)
                            {
                                string cState = splitState[j].Substring(0, splitState[j].IndexOf(":"));
                                double val = double.Parse(splitState[j].Substring(splitState[j].IndexOf(":") + 1), System.Globalization.CultureInfo.InvariantCulture);
                                if (!states[i].Contains(cState))
                                {
                                    states[i].Add(cState);
                                }
                                int ind = states[i].IndexOf(cState);
                                for (int k = parsedData[i].Count; k <= ind; k++)
                                {
                                    parsedData[i].Add(0);
                                }
                                parsedData[i][ind] = val;
                            }
                            data = data.Substring(data.IndexOf("}") + 1);
                        }
                    }
                    tempData.Add(seqName, parsedData);
                }

                States = (from el in states select el.ToArray()).ToArray();

                Data = new Dictionary<string, double[][]>();

                foreach (KeyValuePair<string, List<double>[]> dp in tempData)
                {
                    double[][] parsedData = new double[dp.Value.Length][];
                    for (int i = 0; i < dp.Value.Length; i++)
                    {
                        if (dp.Value[i].Count < States[i].Length)
                        {
                            for (int j = dp.Value[i].Count; j < States[i].Length; j++)
                            {
                                dp.Value[i].Add(0);
                            }
                        }
                        parsedData[i] = dp.Value[i].ToArray();
                    }
                    Data.Add(dp.Key, parsedData);
                }

                if (Data.Count != numTaxa)
                {
                    throw new Exception("Wrong number of taxa! Expecting " + numTaxa.ToString() + ", found " + Data.Count.ToString() + "!");
                }
            }
        }

        public override string ToString()
        {
            string tbr = this.Data.Count.ToString() + "\t" + this.States.Length + "\n";

            foreach (KeyValuePair<string, double[][]> kvp in this.Data)
            {
                tbr += kvp.Key + "\t";

                for (int i = 0; i < kvp.Value.Length; i++)
                {
                    tbr += "{";

                    for (int j = 0; j < kvp.Value[i].Length; j++)
                    {
                        tbr += this.States[i][j] + ":" + kvp.Value[i][j].ToString(System.Globalization.CultureInfo.InvariantCulture) + (j < kvp.Value[i].Length - 1 ? "," : "");
                    }

                    tbr += "}" + (i < kvp.Value.Length - 1 ? "\t" : "\n");
                }
            }

            return tbr;
        }
    }

    public class Parsing
    {
        public static CharacterDependency[][] ReadDependencyFile(StreamReader sr, string[][] states, Random randomSource)
        {
            List<CharacterDependency[]> tbr = new List<CharacterDependency[]>();

            string line = sr.ReadLine();
            if (!line.StartsWith("#NEXUS"))
            {
                throw new Exception("Not a valid NEXUS file!");
            }

            while (!sr.EndOfStream)
            {
                while (!Utils.FixString(line).ToLower().StartsWith("begin dependency;") && !sr.EndOfStream)
                {
                    line = sr.ReadLine();
                }

                List<string> block = new List<string>();

                while (!Utils.FixString(line).ToLower().StartsWith("end;") && !sr.EndOfStream)
                {
                    block.Add(line);
                    line = sr.ReadLine();
                }

                if (block.Count > 0 && !Utils.FixString(line).ToLower().StartsWith("end;"))
                {
                    throw new Exception("Incomplete dependency block!");
                }

                if (block.Count > 0)
                {
                    block.Add(line);
                    tbr.Add(CharacterDependency.Parse(block.ToArray(), states, randomSource));
                }
            }

            return tbr.ToArray();
        }

        public static CharacterDependency[][] ParseDependencies(string dependencyFile, string[][] states, Random randomSource)
        {
            using (StreamReader sr = new StreamReader(dependencyFile))
            {
                return ParseDependencies(sr, states, randomSource);
            }
        }


        public static CharacterDependency[][] ParseDependencies(StreamReader dependencyStream, string[][] states, Random randomSource)
        {
            List<int> unaccountedForChars = new List<int>(Utils.Range(0, states.Length));
            CharacterDependency[][] tempDependencies;
            tempDependencies = ReadDependencyFile(dependencyStream, states, randomSource);

            List<CharacterDependency[]> realDependencies = new List<CharacterDependency[]>();

            for (int i = 0; i < tempDependencies.Length; i++)
            {
                realDependencies.Add(tempDependencies[i]);
            }

            for (int i = 0; i < realDependencies.Count; i++)
            {
                for (int j = 0; j < realDependencies[i].Length; j++)
                {
                    switch (realDependencies[i][j].Type)
                    {
                        case CharacterDependency.Types.Independent:
                        case CharacterDependency.Types.Conditioned:
                            if (unaccountedForChars.Contains(realDependencies[i][j].Index))
                            {
                                unaccountedForChars.Remove(realDependencies[i][j].Index);
                            }
                            else
                            {
                                throw new Exception("Character " + realDependencies[i][j].Index.ToString() + " appears more than once in the dependency file!");
                            }
                            break;
                        case CharacterDependency.Types.Dependent:
                            for (int k = 0; k < realDependencies[i][j].Dependencies.Length; k++)
                            {
                                if (unaccountedForChars.Contains(realDependencies[i][j].Dependencies[k]))
                                {
                                    unaccountedForChars.Remove(realDependencies[i][j].Dependencies[k]);
                                }
                                else
                                {
                                    throw new Exception("Character " + realDependencies[i][j].Dependencies[k].ToString() + " appears more than once in the dependency file!");
                                }
                            }
                            break;
                    }
                }
            }

            for (int i = 0; i < unaccountedForChars.Count; i++)
            {
                realDependencies.Add(new CharacterDependency[] { new CharacterDependency(unaccountedForChars[i]) });
            }

            CharacterDependency[][] dependencies = realDependencies.ToArray();

            List<int> independentChars = new List<int>();
            List<HashSet<int>> dependencyGroups = new List<HashSet<int>>();

            for (int i = 0; i < dependencies.Length; i++)
            {
                if (dependencies[i].Length == 1 && dependencies[i][0].Type == CharacterDependency.Types.Independent)
                {
                    independentChars.Add(dependencies[i][0].Index);
                }
                else
                {
                    HashSet<int> currGroup = new HashSet<int>();
                    List<int> currIndependent = new List<int>();
                    for (int j = 0; j < dependencies[i].Length; j++)
                    {
                        switch (dependencies[i][j].Type)
                        {
                            case CharacterDependency.Types.Independent:
                                currIndependent.Add(dependencies[i][j].Index);
                                break;
                            case CharacterDependency.Types.Conditioned:
                                ConsoleWrapper.Write("Assuming character {0} is conditioned by character(s) ", dependencies[i][j].Index);
                                currGroup.Add(dependencies[i][j].Index);
                                for (int k = 0; k < dependencies[i][j].Dependencies.Length; k++)
                                {
                                    ConsoleWrapper.Write(dependencies[i][j].Dependencies[k].ToString() + (k < dependencies[i][j].Dependencies.Length - 1 ? ", " : ""));
                                    currGroup.Add(dependencies[i][j].Dependencies[k]);
                                }
                                ConsoleWrapper.WriteLine();
                                break;
                            case CharacterDependency.Types.Dependent:
                                ConsoleWrapper.Write("Assuming characters ");
                                for (int k = 0; k < dependencies[i][j].Dependencies.Length; k++)
                                {
                                    ConsoleWrapper.Write(dependencies[i][j].Dependencies[k].ToString() + (k < dependencies[i][j].Dependencies.Length - 1 ? ", " : ""));
                                    currGroup.Add(dependencies[i][j].Dependencies[k]);
                                }
                                ConsoleWrapper.WriteLine(" depend on each other");
                                break;
                        }
                    }

                    if (currIndependent.Count > 0)
                    {
                        ConsoleWrapper.Write("Assuming character(s) ");
                        for (int k = 0; k < currIndependent.Count; k++)
                        {
                            ConsoleWrapper.Write(currIndependent[k].ToString() + (k < currIndependent.Count - 1 ? ", " : ""));
                            currGroup.Add(currIndependent[k]);
                        }
                        ConsoleWrapper.WriteLine(" are independent");
                    }

                    dependencyGroups.Add(currGroup);
                }
            }

            ConsoleWrapper.Write("Assuming character(s) ");

            if (independentChars.Count > 0)
            {
                for (int k = 0; k < independentChars.Count; k++)
                {
                    ConsoleWrapper.Write(independentChars[k].ToString() + ((k < independentChars.Count - 1 || dependencyGroups.Count > 0) ? ", " : ""));
                }
            }

            if (dependencyGroups.Count > 0)
            {
                for (int i = 0; i < dependencyGroups.Count; i++)
                {
                    ConsoleWrapper.Write("(");
                    for (int k = 0; k < dependencyGroups[i].Count; k++)
                    {
                        ConsoleWrapper.Write(dependencyGroups[i].ElementAt(k).ToString() + (k < dependencyGroups[i].Count - 1 ? ", " : ""));
                    }
                    ConsoleWrapper.Write(")");

                    if (i < dependencyGroups.Count - 1)
                    {
                        ConsoleWrapper.Write(", ");
                    }
                }
            }

            ConsoleWrapper.WriteLine(" are independent");
            ConsoleWrapper.WriteLine();

            return dependencies;
        }

        public static CharacterDependency[][] GetDefaultDependencies(string[][] states)
        {
            List<int> unaccountedForChars = new List<int>(Utils.Range(0, states.Length));

            List<CharacterDependency[]> realDependencies = new List<CharacterDependency[]>();

            for (int i = 0; i < unaccountedForChars.Count; i++)
            {
                realDependencies.Add(new CharacterDependency[] { new CharacterDependency(unaccountedForChars[i]) });
            }

            CharacterDependency[][] dependencies = realDependencies.ToArray();

            List<int> independentChars = new List<int>();

            for (int i = 0; i < dependencies.Length; i++)
            {
                independentChars.Add(dependencies[i][0].Index);
            }

            ConsoleWrapper.Write("Assuming character(s) ");

            if (independentChars.Count > 0)
            {
                for (int k = 0; k < independentChars.Count; k++)
                {
                    ConsoleWrapper.Write(independentChars[k].ToString() + ((k < independentChars.Count - 1) ? ", " : ""));
                }
            }

            ConsoleWrapper.WriteLine(" are independent");
            ConsoleWrapper.WriteLine();

            return dependencies;
        }

        public static Dictionary<string, Parameter>[] ParseRateFile(string path, string[][] states, Random randomSource)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                return ParseRateFile(sr, states, randomSource);
            }
        }

        public static Dictionary<string, Parameter>[] ParseRateFile(StreamReader sr, string[][] states, Random randomSource)
        {
            Dictionary<string, Parameter>[] tbr = new Dictionary<string, Parameter>[states.Length];

            Dictionary<int, Dictionary<string, Parameter>> parsedBlocks = new Dictionary<int, Dictionary<string, Parameter>>();

            string line = sr.ReadLine();
            if (!line.StartsWith("#NEXUS"))
            {
                throw new Exception("Not a valid NEXUS file!");
            }

            while (!sr.EndOfStream)
            {
                while (!Utils.FixString(line).ToLower().StartsWith("begin rates;") && !sr.EndOfStream)
                {
                    line = sr.ReadLine();
                }

                List<string> block = new List<string>();

                while (!Utils.FixString(line).ToLower().StartsWith("end;") && !sr.EndOfStream)
                {
                    block.Add(line);
                    line = sr.ReadLine();
                }

                if (block.Count > 0 && !Utils.FixString(line).ToLower().StartsWith("end;"))
                {
                    throw new Exception("Incomplete rates block!");
                }

                if (block.Count > 0)
                {
                    block.Add(line);
                    ((IDictionary<int, Dictionary<string, Parameter>>)parsedBlocks).Add(ParseRatesBlock(block.ToArray(), states, randomSource));
                }
            }

            for (int i = 0; i < states.Length; i++)
            {
                if (parsedBlocks.ContainsKey(i))
                {
                    tbr[i] = parsedBlocks[i];
                }
                else
                {
                    Dictionary<string, Parameter> defaultDict = new Dictionary<string, Parameter>();

                    for (int j = 0; j < states[i].Length; j++)
                    {
                        for (int k = 0; k < states[i].Length; k++)
                        {
                            if (j != k)
                            {
                                defaultDict.Add(states[i][j] + ">" + states[i][k], new Parameter());
                            }
                        }
                    }

                    tbr[i] = defaultDict;
                }
            }

            return tbr.ToArray();
        }

        public static Dictionary<string, Parameter>[] GetDefaultRates(string[][] states)
        {
            Dictionary<string, Parameter>[] tbr = new Dictionary<string, Parameter>[states.Length];

            for (int i = 0; i < states.Length; i++)
            {
                Dictionary<string, Parameter> defaultDict = new Dictionary<string, Parameter>();

                for (int j = 0; j < states[i].Length; j++)
                {
                    for (int k = 0; k < states[i].Length; k++)
                    {
                        if (j != k)
                        {
                            defaultDict.Add(states[i][j] + ">" + states[i][k], new Parameter());
                        }
                    }
                }

                tbr[i] = defaultDict;
            }

            return tbr.ToArray();
        }

        public static KeyValuePair<int, Dictionary<string, Parameter>> ParseRatesBlock(string[] ratesBlock, string[][] states, Random randomSource)
        {
            for (int i = 0; i < ratesBlock.Length; i++)
            {
                ratesBlock[i] = ratesBlock[i].Replace("\t", " ");
                while (ratesBlock[i].Contains("  "))
                {
                    ratesBlock[i] = ratesBlock[i].Replace("  ", " ");
                }
                ratesBlock[i].Replace(" ;", ";");
                ratesBlock[i] = ratesBlock[i].Trim(' ');
                ratesBlock[i] = Utils.FixString(ratesBlock[i]);
            }

            if (!ratesBlock[0].ToLower().StartsWith("begin rates;") || !ratesBlock.Last().ToLower().StartsWith("end;"))
            {
                throw new Exception("Invalid rates block!");
            }

            int charNum = -1;

            for (int i = 1; i < ratesBlock.Length - 1; i++)
            {
                if (ratesBlock[i].ToLower().StartsWith("character"))
                {
                    string charString = ratesBlock[i].Substring(ratesBlock[i].IndexOf(":") + 1);
                    charString = charString.Substring(0, charString.IndexOf(";")).Trim(' ');
                    charNum = int.Parse(charString);
                    break;
                }
            }

            if (charNum == -1)
            {
                if (states.Length == 1)
                {
                    charNum = 0;
                }
                else
                {
                    throw new Exception("Rates block does not specify character!");
                }
            }

            string defaultRate = "ML";

            for (int i = 1; i < ratesBlock.Length - 1; i++)
            {
                if (ratesBlock[i].ToLower().StartsWith("default"))
                {
                    defaultRate = ratesBlock[i].Substring(ratesBlock[i].IndexOf(":") + 1);
                    defaultRate = defaultRate.Substring(0, defaultRate.IndexOf(";")).Replace(" ", "");
                    break;
                }
            }

            Dictionary<string, Parameter> rates = new Dictionary<string, Parameter>();

            for (int i = 0; i < states[charNum].Length; i++)
            {
                for (int j = 0; j < states[charNum].Length; j++)
                {
                    if (i != j)
                    {
                        rates.Add(states[charNum][i] + ">" + states[charNum][j], Parameter.Parse(defaultRate, randomSource));
                    }
                }
            }


            for (int i = 1; i < ratesBlock.Length - 1; i++)
            {
                if (ratesBlock[i].ToLower().StartsWith("rates:"))
                {
                    int ind = i + 1;

                    Dictionary<string, string> equalRates = new Dictionary<string, string>();

                    while (!ratesBlock[ind].StartsWith(";"))
                    {
                        string rateName = ratesBlock[ind].Substring(0, ratesBlock[ind].IndexOf(":")).Replace(" ", "");
                        string rateVal = ratesBlock[ind].Substring(ratesBlock[ind].IndexOf(":") + 1).Replace(" ", "");
                        if (!rateVal.ToLower().StartsWith("equal"))
                        {
                            rates[rateName] = Parameter.Parse(rateVal, randomSource);
                        }
                        else
                        {
                            equalRates.Add(rateName, rateVal);
                        }
                        ind++;
                    }

                    foreach (KeyValuePair<string, string> kvp in equalRates)
                    {
                        rates[kvp.Key] = Parameter.ParseEqual(kvp.Value, rates);
                    }
                    break;
                }
            }

            return new KeyValuePair<int, Dictionary<string, Parameter>>(charNum, rates);
        }

        public static Dictionary<string, Parameter>[] ParsePiFile(string path, string[][] states, Random randomSource)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                return ParsePiFile(sr, states, randomSource);
            }
        }

        public static Dictionary<string, Parameter>[] ParsePiFile(StreamReader sr, string[][] states, Random randomSource)
        {
            Dictionary<string, Parameter>[] tbr = new Dictionary<string, Parameter>[states.Length];

            Dictionary<int, Dictionary<string, Parameter>> parsedBlocks = new Dictionary<int, Dictionary<string, Parameter>>();


            string line = sr.ReadLine();
            if (!line.StartsWith("#NEXUS"))
            {
                throw new Exception("Not a valid NEXUS file!");
            }

            while (!sr.EndOfStream)
            {
                while (!Utils.FixString(line).ToLower().StartsWith("begin pi;") && !sr.EndOfStream)
                {
                    line = sr.ReadLine();
                }

                List<string> block = new List<string>();

                while (!Utils.FixString(line).ToLower().StartsWith("end;") && !sr.EndOfStream)
                {
                    block.Add(line);
                    line = sr.ReadLine();
                }

                if (block.Count > 0 && !Utils.FixString(line).ToLower().StartsWith("end;"))
                {
                    throw new Exception("Incomplete pi block!");
                }

                if (block.Count > 0)
                {
                    block.Add(line);
                    ((IDictionary<int, Dictionary<string, Parameter>>)parsedBlocks).Add(ParsePiBlock(block.ToArray(), states, randomSource));
                }
            }


            for (int i = 0; i < states.Length; i++)
            {
                if (parsedBlocks.ContainsKey(i))
                {
                    tbr[i] = parsedBlocks[i];
                }
                else
                {
                    Dictionary<string, Parameter> defaultDict = new Dictionary<string, Parameter>();

                    for (int j = 0; j < states[i].Length; j++)
                    {
                        defaultDict.Add(states[i][j], new Parameter(1.0 / states[i].Length));
                    }

                    tbr[i] = defaultDict;
                }
            }

            return tbr.ToArray();
        }

        public static Dictionary<string, Parameter>[] GetDefaultPi(string[][] states)
        {
            Dictionary<string, Parameter>[] tbr = new Dictionary<string, Parameter>[states.Length];

            for (int i = 0; i < states.Length; i++)
            {
                Dictionary<string, Parameter> defaultDict = new Dictionary<string, Parameter>();

                for (int j = 0; j < states[i].Length; j++)
                {
                    defaultDict.Add(states[i][j], new Parameter(1.0 / states[i].Length));
                }

                tbr[i] = defaultDict;
            }

            return tbr.ToArray();
        }

        public static KeyValuePair<int, Dictionary<string, Parameter>> ParsePiBlock(string[] piBlock, string[][] states, Random randomSource)
        {
            for (int i = 0; i < piBlock.Length; i++)
            {
                piBlock[i] = piBlock[i].Replace("\t", " ");
                while (piBlock[i].Contains("  "))
                {
                    piBlock[i] = piBlock[i].Replace("  ", " ");
                }
                piBlock[i].Replace(" ;", ";");
                piBlock[i] = piBlock[i].Trim(' ');
                piBlock[i] = Utils.FixString(piBlock[i]);
            }

            if (!piBlock[0].ToLower().StartsWith("begin pi;") || !piBlock.Last().ToLower().StartsWith("end;"))
            {
                throw new Exception("Invalid pi block!");
            }

            int charNum = -1;

            for (int i = 1; i < piBlock.Length - 1; i++)
            {
                if (piBlock[i].ToLower().StartsWith("character"))
                {
                    string charString = piBlock[i].Substring(piBlock[i].IndexOf(":") + 1);
                    charString = charString.Substring(0, charString.IndexOf(";")).Trim(' ');
                    charNum = int.Parse(charString);
                    break;
                }
            }

            if (charNum == -1)
            {
                if (states.Length == 1)
                {
                    charNum = 0;
                }
                else
                {
                    throw new Exception("Pi block does not specify character!");
                }
            }

            string defaultPi = (1.0 / states[charNum].Length).ToString(System.Globalization.CultureInfo.InvariantCulture);

            for (int i = 1; i < piBlock.Length - 1; i++)
            {
                if (piBlock[i].ToLower().StartsWith("default"))
                {
                    defaultPi = piBlock[i].Substring(piBlock[i].IndexOf(":") + 1);
                    defaultPi = defaultPi.Substring(0, defaultPi.IndexOf(";")).Replace(" ", "");
                    break;
                }
            }

            Dictionary<string, Parameter> pis = new Dictionary<string, Parameter>();

            for (int i = 0; i < states[charNum].Length; i++)
            {
                pis.Add(states[charNum][i], Parameter.Parse(defaultPi, randomSource));
            }


            for (int i = 1; i < piBlock.Length - 1; i++)
            {
                if (piBlock[i].ToLower().StartsWith("fixed:") || piBlock[i].ToLower().StartsWith("fix:"))
                {
                    int ind = i + 1;

                    Dictionary<string, string> equalPis = new Dictionary<string, string>();

                    while (!piBlock[ind].StartsWith(";"))
                    {
                        string rateName = piBlock[ind].Substring(0, piBlock[ind].IndexOf(":")).Replace(" ", "");
                        string rateVal = piBlock[ind].Substring(piBlock[ind].IndexOf(":") + 1).Replace(" ", "");
                        if (!rateVal.ToLower().StartsWith("equal"))
                        {
                            pis[rateName] = Parameter.Parse(rateVal, randomSource);
                        }
                        else
                        {
                            equalPis.Add(rateName, rateVal);
                        }
                        ind++;
                    }

                    foreach (KeyValuePair<string, string> kvp in equalPis)
                    {
                        pis[kvp.Key] = Parameter.ParseEqual(kvp.Value, pis);
                    }

                    i = ind;
                }
                else if (piBlock[i].ToLower().StartsWith("dirichlet:"))
                {
                    int ind = i + 1;

                    while (!piBlock[ind].StartsWith(";"))
                    {
                        string rateName = piBlock[ind].Substring(0, piBlock[ind].IndexOf(":")).Replace(" ", "");
                        string rateVal = piBlock[ind].Substring(piBlock[ind].IndexOf(":") + 1).Replace(" ", "");
                        pis[rateName].Action = Parameter.ParameterAction.Dirichlet;
                        pis[rateName].DistributionParameter = double.Parse(rateVal, System.Globalization.CultureInfo.InvariantCulture);
                        ind++;
                    }

                    i = ind;
                }
            }

            double totalPi = 0;

            foreach (KeyValuePair<string, Parameter> kvp in pis)
            {
                if (kvp.Value.Action == Parameter.ParameterAction.Fix)
                {
                    totalPi += kvp.Value.Value;
                }
                if (kvp.Value.Action == Parameter.ParameterAction.Equal && kvp.Value.EqualParameter.Action == Parameter.ParameterAction.Fix)
                {
                    totalPi += kvp.Value.EqualParameter.Value;
                }
            }

            if (totalPi > 1)
            {
                throw new Exception("The sum of pis for character " + charNum.ToString() + " is > 1!");
            }

            return new KeyValuePair<int, Dictionary<string, Parameter>>(charNum, pis);
        }
    }
}
