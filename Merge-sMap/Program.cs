using Mono.Options;
using SlimTreeNode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utils;

namespace Merge_sMap
{
    class Program
    {
        const string version = "1.0.0";
        static int Main(string[] args)
        {
            List<string> files = new List<string>();

            List<int[]> activeCharacters = new List<int[]>();

            int samples = 0;

            string outputsMap = null;
            string outputPhytools = null;
            string priorTempFolder = null;

            bool showHelp = false;

            OptionSet argParser = new OptionSet()
            {
                { "h|help", "Print this message and exit.", v => { showHelp = v != null; } },
                { "s|smap=", "Input file produced by sMap run, followed by a semicolon and a comma-separated list of characters to consider. E.g. \"sMap.bin;0,2\". This argument can be specified multiple times.", v =>
                    {
                        files.Add(v.Substring(0, v.IndexOf(';')));
                        activeCharacters.Add((from el in v.Substring(v.IndexOf(";") + 1).Split(',') select int.Parse(el)).ToArray());
                    }
                },
                { "n|num-sim=", "Number of simulated histories in the merged output file.", v => { samples = int.Parse(v); } },
                { "o|output-smap=", "Output file name for the sMap run file.", v => { outputsMap = v; } },
                { "p|output-phytools=", "Output file name for the phytools-compatible file.", v => { outputPhytools = v; } }
            };

            List<string> unrecognised = argParser.Parse(args);

            bool showUsage = false;

            if (files.Count == 0 && !showHelp)
            {
                ConsoleWrapper.WriteLine("You need to specify at least one input file!");
                showUsage = true;
            }

            if ((from el in activeCharacters select el.Count()).Sum() <= 0 && !showHelp)
            {
                ConsoleWrapper.WriteLine("You need to specify at least one active character!");
                showUsage = true;
            }

            if (unrecognised.Count > 0)
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Unrecognised argument" + (unrecognised.Count > 1 ? "s" : "") + ": " + Utils.Utils.StringifyArray(unrecognised, " "));
                showUsage = true;
            }

            if (!showHelp)
            {
                if (string.IsNullOrEmpty(outputsMap) && string.IsNullOrEmpty(outputsMap))
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("No output file name specified!");
                    showUsage = true;
                }
            }

            if (!showHelp)
            {
                if (samples <= 0)
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("You need to specify the number of simulated histories in the output file!");
                    showUsage = true;
                }
            }

            if (showUsage || showHelp)
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Merge-sMap version {0}", version);
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Usage:");
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("  Merge-sMap {-h|--help}");
                ConsoleWrapper.WriteLine("  Merge-sMap -o <output_sMap> -n <num_sim> -s <sMap_file>;<char1>[,<char2>[...]] \\\n                                          [-s <sMap_file>;<char1>[,<char2>[...]]]");
                ConsoleWrapper.WriteLine("  Merge-sMap -p <output_phytools> -n <num_sim> -s <sMap_file>;<char1>[,<char2>[...]] \\\n                                              [-s <sMap_file>;<char1>[,<char2>[...]]]");
                ConsoleWrapper.WriteLine("  Merge-sMap -o <output_sMap> -p <output_phytools> -n <num_sim> -s <sMap_file>;<char1>[,<char2>[...]] \\\n                                                               [-s <sMap_file>;<char1>[,<char2>[...]]]");
            }

            if (showHelp)
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Options:");
                ConsoleWrapper.WriteLine();
                argParser.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            if (showUsage)
            {
                return 64;
            }


            SerializedRun[] runs = new SerializedRun[files.Count];

            ConsoleWrapper.WriteLine("Reading sMap run files...");
            ConsoleWrapper.WriteLine();

            for (int i = 0; i < files.Count; i++)
            {
                ConsoleWrapper.WriteLine("Reading file {0} / {1}", i + 1, files.Count);
                runs[i] = SerializedRun.Deserialize(files[i]);
            }

            ConsoleWrapper.WriteLine();

            double ageScale = -1;
            string summaryTreeString = "";
            int[][] meanNodeCorresp = null;
            LikelihoodModel[] LikelihoodModels = null;

            for (int i = 0; i < runs.Length; i++)
            {
                if (i != 0 && ageScale != runs[i].AgeScale)
                {
                    ConsoleWrapper.WriteLine("Cannot proceed: not all runs use the same age scale!");
                    ConsoleWrapper.WriteLine();
                    return 1;
                }

                if (i == 0)
                {
                    summaryTreeString = runs[i].SummaryTree.ToString();
                    ageScale = runs[i].AgeScale;
                    meanNodeCorresp = runs[i].SummaryNodeCorresp;
                    LikelihoodModels = runs[i].LikelihoodModels;
                }
                else
                {
                    if (summaryTreeString != runs[i].SummaryTree.ToString())
                    {
                        ConsoleWrapper.WriteLine();
                        ConsoleWrapper.WriteLine("The sMap runs use different summary trees!");
                        return 1;
                    }

                    if (ageScale != runs[i].AgeScale)
                    {
                        ConsoleWrapper.WriteLine();
                        ConsoleWrapper.WriteLine("The sMap runs use different tree age normalisation constants!");
                        return 1;
                    }
                }
            }

            List<string[]> allStates = new List<string[]>();

            for (int i = 0; i < runs.Length; i++)
            {
                for (int j = 0; j < activeCharacters[i].Length; j++)
                {
                    HashSet<string> charStates = new HashSet<string>();

                    for (int k = 0; k < runs[i].AllPossibleStates.Length; k++)
                    {
                        charStates.Add(runs[i].AllPossibleStates[k].Split(',')[activeCharacters[i][j]]);
                    }

                    allStates.Add(charStates.ToArray());
                }
            }

            string[] allPossibleStates = (from el in Utils.Utils.GetCombinations(allStates.ToArray()) select Utils.Utils.StringifyArray(el)).ToArray();

            ConsoleWrapper.WriteLine("Assuming that all runs use the same set of trees.");
            ConsoleWrapper.WriteLine();
            ConsoleWrapper.WriteLine("Computing priors and posteriors...");
            ConsoleWrapper.WriteLine();

            double[][] meanPosterior = new double[runs[0].MeanPosterior.Length][];
            double[][] meanPrior = new double[runs[0].MeanPrior.Length][];

            for (int i = 0; i < meanPosterior.Length; i++)
            {
                double[][] prior = new double[allStates.Count][];
                double[][] posterior = new double[allStates.Count][];

                int charInd = 0;

                for (int k = 0; k < runs.Length; k++)
                {
                    for (int l = 0; l < activeCharacters[k].Length; l++)
                    {
                        prior[charInd] = new double[allStates[charInd].Length];
                        posterior[charInd] = new double[allStates[charInd].Length];

                        for (int j = 0; j < runs[k].AllPossibleStates.Length; j++)
                        {
                            int ind = allStates[charInd].IndexOf(runs[k].AllPossibleStates[j].Split(',')[activeCharacters[k][l]]);
                            posterior[charInd][ind] += runs[k].MeanPosterior[i][j];
                            prior[charInd][ind] += runs[k].MeanPrior[i][j];
                        }

                        charInd++;
                    }
                }

                meanPosterior[i] = new double[allPossibleStates.Length];
                meanPrior[i] = new double[allPossibleStates.Length];

                for (int k = 0; k < allPossibleStates.Length; k++)
                {
                    string[] states = allPossibleStates[k].Split(',');
                    double statePost = 1;
                    double statePrior = 1;

                    for (int j = 0; j < states.Length; j++)
                    {
                        int ind = allStates[j].IndexOf(states[j]);
                        statePost *= posterior[j][ind];
                        statePrior *= prior[j][ind];
                    }

                    meanPosterior[i][k] = statePost;
                    meanPrior[i][k] = statePrior;
                }
            }

            string[] States = allPossibleStates;

            bool includePriorHistories = !(from el in runs select el.revision == 3).Contains(false);

            TreeNode summaryTree = runs[0].SummaryTree;

            List<TaggedHistory> histories = new List<TaggedHistory>();
            Stream priorHistories = null;
            int priorMultiplicity = 0;
            List<long> priorOffsets = new List<long>(samples);
            string prefix = Guid.NewGuid().ToString().Replace("-", "");

            if (includePriorHistories)
            {
                priorMultiplicity = (from el in runs select el.PriorMultiplicity).Min();

                if (priorTempFolder == "memory")
                {
                    priorHistories = new MemoryStream();
                }
                else
                {
                    if (priorTempFolder == null)
                    {
                        priorTempFolder = Path.GetDirectoryName(string.IsNullOrEmpty(outputsMap) ? outputPhytools : outputsMap);
                    }

                    priorHistories = new FileStream(Path.Combine(priorTempFolder, prefix + ".prior"), FileMode.Create);
                }
            }


            TaggedHistory[][][] runsHistories = new TaggedHistory[runs.Length][][];
            Func<int, TaggedHistory[]>[][] runsPriorHistories = new Func<int, TaggedHistory[]>[runs.Length][];

            List<int> allTreeSamples = new List<int>();

            ConsoleWrapper.WriteLine("Extracting marginal histories...");
            ConsoleWrapper.WriteLine();

            for (int i = 0; i < runs.Length; i++)
            {
                runsHistories[i] = new TaggedHistory[activeCharacters[i].Length][];

                if (includePriorHistories)
                {
                    runsPriorHistories[i] = new Func<int, TaggedHistory[]>[activeCharacters[i].Length];
                }

                for (int j = 0; j < activeCharacters[i].Length; j++)
                {
                    runsHistories[i][j] = Utils.Simulation.GetMarginalHistory(runs[i], activeCharacters[i][j]);
                    if (includePriorHistories)
                    {
                        int localI = i;
                        int localJ = j;
                        runsPriorHistories[i][j] = new Func<int, TaggedHistory[]>(k => Utils.Simulation.GetMarginalPriorHistory(runs[localI], activeCharacters[localI][localJ], k));
                    }
                }

                allTreeSamples.AddRange(runs[i].TreeSamples);
            }

            Random rnd = new Random();

            int[] treeSamples = new int[samples];

            for (int i = 0; i < treeSamples.Length; i++)
            {
                treeSamples[i] = allTreeSamples[rnd.Next(allTreeSamples.Count)];
            }

            ConsoleWrapper.WriteLine("Merging histories...");
            ConsoleWrapper.WriteLine();

            for (int i = 0; i < treeSamples.Length; i++)
            {
                TaggedHistory[] currHistories = new TaggedHistory[allStates.Count];
                TaggedHistory[][] currPriorHistories = new TaggedHistory[allStates.Count][];

                bool resampleTree = true;

                while (resampleTree)
                {
                    resampleTree = false;

                    int charInd = 0;

                    for (int k = 0; k < runs.Length; k++)
                    {
                        int[] correspHistories = (from el in Utils.Utils.Range(0, runs[k].TreeSamples.Length) where runs[k].TreeSamples[el] == treeSamples[i] select el).ToArray();

                        if (correspHistories.Length > 0)
                        {
                            int histInd = correspHistories[rnd.Next(correspHistories.Length)];
                            for (int j = 0; j < activeCharacters[k].Length; j++)
                            {
                                currHistories[charInd] = runsHistories[k][j][histInd];
                                if (includePriorHistories)
                                {
                                    currPriorHistories[charInd] = runsPriorHistories[k][j](histInd);
                                }
                                charInd++;
                            }
                        }
                        else
                        {
                            resampleTree = true;
                            break;
                        }
                    }

                    if (resampleTree)
                    {
                        treeSamples[i] = allTreeSamples[rnd.Next(allTreeSamples.Count)];
                    }
                }

                TaggedHistory mergedHistory = Simulation.MergeHistories(currHistories, LikelihoodModels[treeSamples[i]]);
                mergedHistory.Tag = histories.Count;
                histories.Add(mergedHistory);

                if (includePriorHistories)
                {
                    long position = priorHistories.Position;
                    long[] lengths = new long[priorMultiplicity];

                    for (int j = 0; j < priorMultiplicity; j++)
                    {
                        TaggedHistory mergedPriorHistory = Simulation.MergeHistories((from el in currPriorHistories select el[j]).ToArray(), LikelihoodModels[treeSamples[i]]);
                        mergedPriorHistory.Tag = histories.Count - 1;
                        lengths[j] = mergedPriorHistory.WriteToStream(priorHistories);
                    }

                    priorHistories.Flush();

                    priorOffsets.Add(priorHistories.Position);
                    priorHistories.WriteLong(position);
                    for (int k = 0; k < priorMultiplicity; k++)
                    {
                        priorHistories.WriteLong(lengths[k]);
                    }

                    priorHistories.Flush();
                }
            }

            if (includePriorHistories)
            {
                long pos = priorHistories.Position;

                for (int i = 0; i < treeSamples.Length; i++)
                {
                    priorHistories.WriteLong(priorOffsets[i]);
                }

                priorHistories.WriteLong(pos);
                priorHistories.WriteInt(priorMultiplicity);
                priorHistories.Flush();
                priorHistories.Seek(0, SeekOrigin.Begin);
            }

            SerializedRun mergedRun;

            if (!includePriorHistories)
            {
                mergedRun = new SerializedRun(summaryTree, histories.ToArray(), States, meanPosterior, meanPrior, meanNodeCorresp, treeSamples, LikelihoodModels, allPossibleStates, ageScale);
            }
            else
            {
                mergedRun = new SerializedRun(summaryTree, histories.ToArray(), priorHistories, States, meanPosterior, meanPrior, meanNodeCorresp, treeSamples, LikelihoodModels, allPossibleStates, ageScale, null, null);
            }

            ConsoleWrapper.WriteLine("Saving output...");
            ConsoleWrapper.WriteLine();

            if (!string.IsNullOrEmpty(outputsMap))
            {
                mergedRun.Serialize(outputsMap);
            }

            if (!string.IsNullOrEmpty(outputPhytools))
            {
                using (StreamWriter sw = new StreamWriter(outputPhytools))
                {
                    foreach (TaggedHistory history in histories)
                    {
                        sw.WriteLine(Utils.Utils.GetSmapString(LikelihoodModels[treeSamples[history.Tag]], history.History));
                    }
                }
            }

            ConsoleWrapper.WriteLine("Done.");
            ConsoleWrapper.WriteLine();

            for (int i = 0; i < runs.Length; i++)
            {
                runs[i].CloseStream();
            }

            if (includePriorHistories)
            {
                if (priorTempFolder != "memory")
                {
                    priorHistories.Close();
                    File.Delete(Path.Combine(priorTempFolder, prefix + ".prior"));
                }
            }

            return 0;
        }
    }
}
