using Mono.Options;
using SlimTreeNode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utils;

namespace Blend_sMap
{
    class Program
    {
        const string version = "1.0.1";
        static int Main(string[] args)
        {
            List<(double, string)> filesToBlend = new List<(double, string)>();
            int finalCount = -1;
            string outputFile = "";
            string priorTempFolder = null;

            bool showHelp = false;

            OptionSet argParser = new OptionSet()
            {
                { "h|help", "Print this message and exit.", v => { showHelp = v != null; } },
                { "s|smap=", "Input file produced by sMap run, with the respective weight in the blended output, separated by a comma. E.g. \"smap.bin,0.5\". The weights for all files will be normalised, thus they do not need to sum to 1. This argument can be specified multiple times.", v => { filesToBlend.Add((double.Parse(v.Split(',')[1], System.Globalization.CultureInfo.InvariantCulture), v.Split(',')[0])); } },
                { "n|num-sim=", "Number of simulated histories in the blended output file.", v => { finalCount = int.Parse(v); } },
                { "o|output=", "Output file name.", v => { outputFile = v; } }
            };

            List<string> unrecognised = argParser.Parse(args);

            bool showUsage = false;

            if (filesToBlend.Count == 0 && !showHelp)
            {
                ConsoleWrapper.WriteLine("You need to specify at least one input file!");
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
                if (string.IsNullOrEmpty(outputFile))
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("No output file name specified!");
                    showUsage = true;
                }
            }

            if (!showHelp)
            {
                if (finalCount < 0)
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("You need to specify the number of simulated histories in the output file!");
                    showUsage = true;
                }
            }

            if (showUsage || showHelp)
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Blend-sMap version {0}", version);
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Usage:");
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("  Blend-sMap {-h|--help}");
                ConsoleWrapper.WriteLine("  Blend-sMap -o <output_file> -n <num_sim> -s <sMap_file>,<weight> [-s <sMap_file>,<weight> [...]]");
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


            string summaryTreeString = null;
            double ageScale = -1;
            string[] allPossibleStates = null;
            string[] states = null;
            int[][] summaryNodeCorresp = null;
            LikelihoodModel[] likModels = null;

            bool treesClockLike = false;

            Console.WriteLine("Reading input files...");

            List<SerializedRun> runs = new List<SerializedRun>();

            for (int i = 0; i < filesToBlend.Count; i++)
            {
                SerializedRun run;

                try
                {
                    run = SerializedRun.Deserialize(filesToBlend[i].Item2);
                }
                catch (Exception e)
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("Error while parsing input file:");
                    ConsoleWrapper.WriteLine(e.Message);
                    return 1;
                }

                if (i == 0)
                {
                    summaryTreeString = run.SummaryTree.ToString();
                    treesClockLike = run.TreesClockLike;
                    ageScale = run.AgeScale;
                    allPossibleStates = run.AllPossibleStates;
                    states = run.States;
                    summaryNodeCorresp = run.SummaryNodeCorresp;
                    likModels = run.LikelihoodModels;
                }
                else
                {
                    if (summaryTreeString != run.SummaryTree.ToString())
                    {
                        ConsoleWrapper.WriteLine();
                        ConsoleWrapper.WriteLine("The sMap runs use different summary trees!");
                        return 1;
                    }

                    if (ageScale != run.AgeScale)
                    {
                        ConsoleWrapper.WriteLine();
                        ConsoleWrapper.WriteLine("The sMap runs use different tree age normalisation constants!");
                        return 1;
                    }

                    if (!allPossibleStates.SequenceEqual(run.AllPossibleStates) || !states.SequenceEqual(run.States))
                    {
                        ConsoleWrapper.WriteLine();
                        ConsoleWrapper.WriteLine("The sMap runs use different data!");
                        return 1;
                    }
                }

                runs.Add(run);

                Console.WriteLine("Read file {0}/{1}.", i + 1, filesToBlend.Count);
            }

            Console.WriteLine();
            Console.WriteLine("Assuming that all runs use the same set of trees.");
            Console.WriteLine();

            Console.WriteLine("Blending character histories...");

            double sum = (from el in filesToBlend select el.Item1).Sum();

            int[] sampleCount = Utils.Utils.RoundInts((from el in filesToBlend select el.Item1 * finalCount / sum).ToArray());

            double[][] blendedMeanPosterior = new double[runs[0].MeanPosterior.Length][];
            double[][] blendedMeanPrior = new double[runs[0].MeanPrior.Length][];

            for (int i = 0; i < blendedMeanPosterior.Length; i++)
            {
                blendedMeanPosterior[i] = new double[runs[0].MeanPosterior[i].Length];
                blendedMeanPrior[i] = new double[runs[0].MeanPrior[i].Length];

                for (int j = 0; j < blendedMeanPosterior[i].Length; j++)
                {
                    for (int k = 0; k < runs.Count; k++)
                    {
                        blendedMeanPosterior[i][j] += runs[k].MeanPosterior[i][j] * filesToBlend[k].Item1;
                        blendedMeanPrior[i][j] += runs[k].MeanPrior[i][j] * filesToBlend[k].Item1;
                    }
                    blendedMeanPosterior[i][j] /= sum;
                    blendedMeanPrior[i][j] /= sum;
                }
            }

            bool includeDStats = !(from el in runs select el.revision == 2 && el.AllDStats != null).Contains(false);
            bool includePriorHistories = !(from el in runs select el.revision == 3 && el.HasPriorHistories).Contains(false);

            List<TaggedHistory> blendedHistories = new List<TaggedHistory>(finalCount);
            List<DStats[][][]> allDStats = new List<DStats[][][]>(finalCount);

            Stream blendedPriorHistories = null;
            int priorMultiplicity = 0;
            List<long> priorOffsets = new List<long>(finalCount);
            string prefix = Guid.NewGuid().ToString().Replace("-", "");

            if (includePriorHistories)
            {
                priorMultiplicity = (from el in runs select el.PriorMultiplicity).Min();

                if (priorTempFolder == "memory")
                {
                    blendedPriorHistories = new MemoryStream();
                }
                else
                {
                    if (priorTempFolder == null)
                    {
                        priorTempFolder = Path.GetDirectoryName(outputFile);
                    }

                    blendedPriorHistories = new FileStream(Path.Combine(priorTempFolder, prefix + ".prior"), FileMode.Create);
                }
            }


            List<int> blendedTreeSamples = new List<int>(finalCount);

            Random rnd = new Random();

            for (int i = 0; i < sampleCount.Length; i++)
            {
                int[] samples = getSamples(runs[i].Histories.Length, sampleCount[i], rnd);

                for (int j = 0; j < sampleCount[i]; j++)
                {
                    runs[i].Histories[samples[j]].Tag = blendedHistories.Count;
                    blendedHistories.Add(runs[i].Histories[samples[j]]);

                    if (includeDStats)
                    {
                        allDStats.Add(runs[i].AllDStats[samples[j]]);
                    }

                    if (includePriorHistories)
                    {
                        TaggedHistory[] sampledPriorHistories = runs[i].GetPriorHistories(samples[j]);

                        long position = blendedPriorHistories.Position;
                        long[] lengths = new long[priorMultiplicity];

                        for (int k = 0; k < priorMultiplicity; k++)
                        {
                            sampledPriorHistories[k].Tag = blendedHistories.Count - 1;
                            lengths[k] = sampledPriorHistories[k].WriteToStream(blendedPriorHistories);
                        }

                        blendedPriorHistories.Flush();

                        priorOffsets.Add(blendedPriorHistories.Position);
                        blendedPriorHistories.WriteLong(position);
                        for (int k = 0; k < priorMultiplicity; k++)
                        {
                            blendedPriorHistories.WriteLong(lengths[k]);
                        }

                        blendedPriorHistories.Flush();
                    }

                    blendedTreeSamples.Add(runs[i].TreeSamples[samples[j]]);
                }
            }

            if (includePriorHistories)
            {
                long pos = blendedPriorHistories.Position;

                for (int i = 0; i < finalCount; i++)
                {
                    blendedPriorHistories.WriteLong(priorOffsets[i]);
                }

                blendedPriorHistories.WriteLong(pos);
                blendedPriorHistories.WriteInt(priorMultiplicity);
                blendedPriorHistories.Flush();
                blendedPriorHistories.Seek(0, SeekOrigin.Begin);
            }

            Console.WriteLine("Saving output...");

            SerializedRun tbr;

            if (includeDStats)
            {
                tbr = new SerializedRun(TreeNode.Parse(summaryTreeString, null), blendedHistories.ToArray(), allDStats.ToArray(), states, blendedMeanPosterior, blendedMeanPrior, summaryNodeCorresp, blendedTreeSamples.ToArray(), likModels, allPossibleStates, ageScale, null, null, treesClockLike);
                
            }
            else if (includePriorHistories)
            {
                tbr = new SerializedRun(TreeNode.Parse(summaryTreeString, null), blendedHistories.ToArray(), blendedPriorHistories, states, blendedMeanPosterior, blendedMeanPrior, summaryNodeCorresp, blendedTreeSamples.ToArray(), likModels, allPossibleStates, ageScale, null, null, treesClockLike);
            }
            else 
            {
                tbr = new SerializedRun(TreeNode.Parse(summaryTreeString, null), blendedHistories.ToArray(), states, blendedMeanPosterior, blendedMeanPrior, summaryNodeCorresp, blendedTreeSamples.ToArray(), likModels, allPossibleStates, ageScale, treesClockLike);
            }

            tbr.Serialize(outputFile);

            Console.WriteLine("Done.");
            Console.WriteLine("");

            for (int i = 0; i < runs.Count; i++)
            {
                runs[i].CloseStream();
            }

            if (includePriorHistories)
            {
                if (priorTempFolder != "memory")
                {
                    blendedPriorHistories.Close();
                    File.Delete(Path.Combine(priorTempFolder, prefix + ".prior"));
                }
            }

            return 0;
        }

        static int[] getSamples(int max, int count, Random rnd)
        {
            if (count == max)
            {
                return Utils.Utils.Range(0, max);
            }
            else if (count > max)
            {
                List<int> tbr = new List<int>(count);

                while (count - tbr.Count > max)
                {
                    tbr.AddRange(Utils.Utils.Range(0, max));
                }

                List<int> pool = new List<int>(Utils.Utils.Range(0, max));

                int remaining = count - tbr.Count;

                for (int i = 0; i < remaining; i++)
                {
                    int ind = rnd.Next(0, pool.Count);
                    tbr.Add(pool[ind]);
                    pool.RemoveAt(ind);
                }

                return tbr.ToArray();
            }
            else
            {
                List<int> tbr = new List<int>(count);

                List<int> pool = new List<int>(Utils.Utils.Range(0, max));

                int remaining = count - tbr.Count;

                for (int i = 0; i < remaining; i++)
                {
                    int ind = rnd.Next(0, pool.Count);
                    tbr.Add(pool[ind]);
                    pool.RemoveAt(ind);
                }

                return tbr.ToArray();
            }
        }
    }
}
