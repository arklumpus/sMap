using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utils;

namespace Stat_sMap
{
    class Program
    {
        const string version = "1.0.0";
        static int Main(string[] args)
        {
            string filename = "";
            string outputPrefix = "";

            bool marginal = false;

            int[] corrTestChars = new int[] { };

            bool showHelp = false;

            OptionSet argParser = new OptionSet()
            {
                { "h|help", "Print this message and exit.", v => { showHelp = v != null; } },
                { "s|smap=", "Input file produced by sMap run.", v => { filename = v; } },
                { "m|marginal", "Display marginal transitions and time spent in each state.", v => { marginal = !string.IsNullOrEmpty(v); } },
            };

            argParser.Add(new Action3ParamOption<string, string, string>("t|test=", "Perform a D-test (Huelsenbeck et al, 2003) between two characters {VALUE1} and {VALUE2}. Save the results as {VALUE3}.pdf", (v1, v2, v3) => { corrTestChars = new int[] { int.Parse(v1), int.Parse(v2) }; outputPrefix = v3; }));

            List<string> unrecognised = argParser.Parse(args);

            bool showUsage = false;

            if (string.IsNullOrEmpty(filename) && !showHelp)
            {
                ConsoleWrapper.WriteLine("You need to specify an input file!");
                showUsage = true;
            }

            if (corrTestChars.Length > 0 && corrTestChars[0] == corrTestChars[1])
            {
                ConsoleWrapper.WriteLine("You need to specify two different characters to perform the D-test output files!");
                showUsage = true;
            }

            if (corrTestChars.Length > 0 && string.IsNullOrEmpty(outputPrefix))
            {
                ConsoleWrapper.WriteLine("You need to specify an output file for the D-test!");
                showUsage = true;
            }

            if (unrecognised.Count > 0)
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Unrecognised argument" + (unrecognised.Count > 1 ? "s" : "") + ": " + Utils.Utils.StringifyArray(unrecognised, " "));
                showUsage = true;
            }

            if (showUsage || showHelp)
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Stat-sMap version {0}", version);
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Usage:");
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("  Stat-sMap {-h|--help}");
                ConsoleWrapper.WriteLine("  Stat-sMap -s <sMap_file>");
                ConsoleWrapper.WriteLine("  Stat-sMap -s <sMap_file> -m");
                ConsoleWrapper.WriteLine("  Stat-sMap -s <sMap_file> -t <char0> <char1> <output_prefix>");
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


            ConsoleWrapper.WriteLine("Reading input file...");
            ConsoleWrapper.WriteLine();

            SerializedRun run = SerializedRun.Deserialize(filename);

            if (corrTestChars.Length == 0)
            {

                Dictionary<string, int> totalTransitions = new Dictionary<string, int>();

                for (int i = 0; i < run.AllPossibleStates.Length; i++)
                {
                    for (int j = 0; j < run.AllPossibleStates.Length; j++)
                    {
                        if (i != j)
                        {
                            totalTransitions.Add(run.AllPossibleStates[i] + ">" + run.AllPossibleStates[j], 0);
                        }
                    }
                }

                Dictionary<string, double> totalTimes = new Dictionary<string, double>();

                for (int i = 0; i < run.AllPossibleStates.Length; i++)
                {
                    totalTimes.Add(run.AllPossibleStates[i], 0);
                }

                for (int i = 0; i < run.Histories.Length; i++)
                {
                    Dictionary<string, double> historyTimes = run.Histories[i].GetTimes(run.AllPossibleStates);
                    foreach (KeyValuePair<string, double> kvp in historyTimes)
                    {
                        totalTimes[kvp.Key] += kvp.Value;
                    }
                }

                for (int i = 0; i < run.Histories.Length; i++)
                {
                    Dictionary<string, int> historyTransitions = run.Histories[i].GetTransitions(run.AllPossibleStates);
                    foreach (KeyValuePair<string, int> kvp in historyTransitions)
                    {
                        totalTransitions[kvp.Key] += kvp.Value;
                    }
                }

                if (!marginal)
                {
                    ConsoleWrapper.WriteLine("Average number of transitions:");

                    foreach (KeyValuePair<string, int> kvp in totalTransitions)
                    {
                        ConsoleWrapper.WriteLine("\t{0}: {1}", kvp.Key, ((double)kvp.Value / run.Histories.Length).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }

                    ConsoleWrapper.WriteLine("");
                    ConsoleWrapper.WriteLine("Average time spent in each state:");

                    double totalTime = (from el in totalTimes select el.Value).Sum();

                    foreach (KeyValuePair<string, double> kvp in totalTimes)
                    {
                        ConsoleWrapper.WriteLine("\t{0}: {1}\t({2})", kvp.Key, ((double)kvp.Value / run.Histories.Length * run.AgeScale).ToString(System.Globalization.CultureInfo.InvariantCulture), ((double)kvp.Value / totalTime).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
                else
                {
                    string[][] allStates = new string[run.AllPossibleStates[0].Split(',').Length][];

                    for (int i = 0; i < allStates.Length; i++)
                    {
                        allStates[i] = new HashSet<string>(from el in run.AllPossibleStates select el.Split(',')[i]).ToArray();
                    }

                    Dictionary<string, int>[] marginalTransitions = new Dictionary<string, int>[allStates.Length];

                    for (int i = 0; i < allStates.Length; i++)
                    {
                        marginalTransitions[i] = new Dictionary<string, int>();

                        for (int j = 0; j < allStates[i].Length; j++)
                        {
                            for (int k = 0; k < allStates[i].Length; k++)
                            {
                                if (j != k)
                                {
                                    marginalTransitions[i].Add(allStates[i][j] + ">" + allStates[i][k], 0);
                                }
                            }
                        }

                        foreach (KeyValuePair<string, int> kvp in totalTransitions)
                        {
                            string leftState = kvp.Key.Substring(0, kvp.Key.IndexOf(">")).Split(',')[i];
                            string rightState = kvp.Key.Substring(kvp.Key.IndexOf(">") + 1).Split(',')[i];

                            if (leftState != rightState)
                            {
                                marginalTransitions[i][leftState + ">" + rightState] += kvp.Value;
                            }
                        }
                    }


                    Dictionary<string, double>[] marginalTimes = new Dictionary<string, double>[allStates.Length];

                    for (int i = 0; i < allStates.Length; i++)
                    {
                        marginalTimes[i] = new Dictionary<string, double>();

                        for (int j = 0; j < allStates[i].Length; j++)
                        {
                            marginalTimes[i].Add(allStates[i][j], 0);
                        }

                        foreach (KeyValuePair<string, double> kvp in totalTimes)
                        {
                            string state = kvp.Key.Split(',')[i];
                            marginalTimes[i][state] += kvp.Value;
                        }
                    }

                    ConsoleWrapper.WriteLine("Average (marginal) number of transitions:");

                    for (int i = 0; i < marginalTransitions.Length; i++)
                    {
                        ConsoleWrapper.WriteLine("\tCharacter {0}:", i);
                        foreach (KeyValuePair<string, int> kvp in marginalTransitions[i])
                        {
                            ConsoleWrapper.WriteLine("\t\t{0}: {1}", kvp.Key, ((double)kvp.Value / run.Histories.Length).ToString());
                        }
                    }

                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("Average (marginal) time spent in each state:");

                    double totalTime = (from el in totalTimes select el.Value).Sum();

                    for (int i = 0; i < marginalTimes.Length; i++)
                    {
                        ConsoleWrapper.WriteLine("\tCharacter {0}:", i);
                        foreach (KeyValuePair<string, double> kvp in marginalTimes[i])
                        {
                            ConsoleWrapper.WriteLine("\t\t{0}: {1}\t({2})", kvp.Key, ((double)kvp.Value / run.Histories.Length * run.AgeScale).ToString(System.Globalization.CultureInfo.InvariantCulture), ((double)kvp.Value / totalTime).ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture));
                        }
                    }
                }
            }
            else
            {
                Dictionary<string, double> totalTimes = new Dictionary<string, double>();

                for (int i = 0; i < run.AllPossibleStates.Length; i++)
                {
                    totalTimes.Add(run.AllPossibleStates[i], 0);
                }

                for (int i = 0; i < run.Histories.Length; i++)
                {
                    Dictionary<string, double> historyTimes = run.Histories[i].GetTimes(run.AllPossibleStates);
                    foreach (KeyValuePair<string, double> kvp in historyTimes)
                    {
                        totalTimes[kvp.Key] += kvp.Value;
                    }
                }

                string[][] allStates = new string[run.AllPossibleStates[0].Split(',').Length][];

                for (int i = 0; i < allStates.Length; i++)
                {
                    allStates[i] = new HashSet<string>(from el in run.AllPossibleStates select el.Split(',')[i]).ToArray();
                }

                Dictionary<string, double> myTimes = new Dictionary<string, double>();

                string[] myStates = new HashSet<string>(from el in run.AllPossibleStates select el.Split(',')[corrTestChars[0]] + "," + el.Split(',')[corrTestChars[1]]).ToArray();

                for (int i = 0; i < myStates.Length; i++)
                {
                    myTimes.Add(myStates[i], 0);
                }

                foreach (KeyValuePair<string, double> kvp in totalTimes)
                {
                    string state = kvp.Key.Split(',')[corrTestChars[0]] + "," + kvp.Key.Split(',')[corrTestChars[1]];
                    myTimes[state] += kvp.Value;
                }

                double totalTime = (from el in totalTimes select el.Value).Sum();

                double[][] times = new double[allStates[corrTestChars[0]].Length][];

                for (int i = 0; i < allStates[corrTestChars[0]].Length; i++)
                {
                    times[i] = new double[allStates[corrTestChars[1]].Length];
                    for (int j = 0; j < allStates[corrTestChars[1]].Length; j++)
                    {
                        times[i][j] = myTimes[allStates[corrTestChars[0]][i] + "," + allStates[corrTestChars[1]][j]] / totalTime;
                    }
                }

                Dictionary<string, double>[] marginalTimes = new Dictionary<string, double>[allStates.Length];

                for (int i = 0; i < allStates.Length; i++)
                {
                    marginalTimes[i] = new Dictionary<string, double>();

                    for (int j = 0; j < allStates[i].Length; j++)
                    {
                        marginalTimes[i].Add(allStates[i][j], 0);
                    }

                    foreach (KeyValuePair<string, double> kvp in totalTimes)
                    {
                        string state = kvp.Key.Split(',')[i];
                        marginalTimes[i][state] += kvp.Value;
                    }
                }


                ConsoleWrapper.Write("Performing D-test: ");
                int curPos = ConsoleWrapper.CursorLeft;
                ConsoleWrapper.Write("0%    ");
                ConsoleWrapper.CursorVisible = false;

                DTest test = run.ComputeDTest(corrTestChars[0], corrTestChars[1], v =>
                    {
                        ConsoleWrapper.CursorLeft = curPos;
                        ConsoleWrapper.Write(v.ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + "    ");
                    }
                );

                ConsoleWrapper.CursorLeft = curPos;
                ConsoleWrapper.WriteLine("Done.");

                ConsoleWrapper.CursorVisible = true;

                string[][] currStates = new string[][] { new HashSet<string>(from el in run.AllPossibleStates select el.Split(',')[corrTestChars[0]]).ToArray(), new HashSet<string>(from el in run.AllPossibleStates select el.Split(',')[corrTestChars[1]]).ToArray() };

                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("D = {0}", test.DStats.D.ToString(System.Globalization.CultureInfo.InvariantCulture));
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Posterior predictive P = {0}", test.P.ToString(System.Globalization.CultureInfo.InvariantCulture));
                ConsoleWrapper.WriteLine();

                ConsoleWrapper.WriteLine("dij = ");

                for (int j = 0; j < currStates[1].Length; j++)
                {
                    ConsoleWrapper.Write("\t" + currStates[1][j]);
                }
                ConsoleWrapper.WriteLine();

                for (int i = 0; i < currStates[0].Length; i++)
                {
                    ConsoleWrapper.Write(currStates[0][i] + "\t");

                    for (int j = 0; j < currStates[1].Length; j++)
                    {
                        ConsoleWrapper.Write(test.DStats.dij[i][j].ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + (j < currStates[1].Length - 1 ? "\t" : ""));
                    }
                    ConsoleWrapper.WriteLine();
                }

                ConsoleWrapper.WriteLine();

                ConsoleWrapper.WriteLine("Pij = ");

                for (int j = 0; j < currStates[1].Length; j++)
                {
                    ConsoleWrapper.Write("\t" + currStates[1][j]);
                }
                ConsoleWrapper.WriteLine();

                for (int i = 0; i < currStates[0].Length; i++)
                {
                    ConsoleWrapper.Write(currStates[0][i] + "\t");

                    for (int j = 0; j < currStates[1].Length; j++)
                    {
                        ConsoleWrapper.Write(test.Pij[i][j].ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + (j < currStates[1].Length - 1 ? "\t" : ""));
                    }
                    ConsoleWrapper.WriteLine();
                }

                ConsoleWrapper.WriteLine();
                ConsoleWrapper.Write("Computing D-stat distribution: ");
                curPos = ConsoleWrapper.CursorLeft;
                ConsoleWrapper.Write("0%    ");

                ConsoleWrapper.CursorVisible = false;

                if (run.AllDStats != null)
                {
                    DStats[] allDStats = new DStats[run.AllDStats.Length];

                    double lastProgress = -1;

                    for (int i = 0; i < run.AllDStats.Length; i++)
                    {
                        allDStats[i] = DStats.GetAverageDStats(run.AllDStats[i][Math.Min(corrTestChars[0], corrTestChars[1])][Math.Max(corrTestChars[0], corrTestChars[1])]);
                        
                        double progress = (double)(i + 1) / run.AllDStats.Length;
                        if (Math.Round(progress * 100) > Math.Round(lastProgress * 100))
                        {
                            ConsoleWrapper.CursorLeft = curPos;
                            ConsoleWrapper.Write(progress.ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + "    ");
                        }
                    }

                    Plotting.PlotDTest(test, allDStats, currStates, times, outputPrefix + ".pdf");
                }
                else
                {
                    DStats[] allDStats = new DStats[run.Histories.Length];

                    double lastProgress = -1;

                    for (int i = 0; i < run.Histories.Length; i++)
                    {
                        allDStats[i] = run.GetPriorHistories(i).ComputeAverageDStats(run.AllPossibleStates, Math.Min(corrTestChars[0], corrTestChars[1]), Math.Max(corrTestChars[0], corrTestChars[1]));

                        double progress = (double)(i + 1) / run.Histories.Length;
                        if (Math.Round(progress * 100) > Math.Round(lastProgress * 100))
                        {
                            ConsoleWrapper.CursorLeft = curPos;
                            ConsoleWrapper.Write(progress.ToString("0%", System.Globalization.CultureInfo.InvariantCulture) + "    ");
                        }
                    }

                    Plotting.PlotDTest(test, allDStats, currStates, times, outputPrefix + ".pdf");
                }

                curPos = ConsoleWrapper.CursorLeft;
                ConsoleWrapper.WriteLine("Done.");
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.CursorVisible = true;
            }

            return 0;
        }
    }
}