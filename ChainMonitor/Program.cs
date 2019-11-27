using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace ChainMonitor
{
    class Program
    {
        static StreamReader[] streamReaders;

        static string[] headings;
        static int[] sampleSizes;
        static List<string> chainFiles;
        static List<double>[][] samples;
        static double percBurnin = 0.1;
        static int burnin = -1;

        static double[][] means;
        static double[][] stdDevs;
        static double[][] ess;

        static double[] overallMeans;
        static double[] overallStdDevs;

        static int activeStat = 1;
        static bool[] activeFiles;

        static int xTransl = 0;
        static int yTransl = 0;

        static ConsoleFrameBuffer mainFrameBuffer;

        static ConsoleColor[] colors = new ConsoleColor[] { ConsoleColor.Blue, ConsoleColor.Green, ConsoleColor.Red, ConsoleColor.DarkYellow, ConsoleColor.White, ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.Gray, ConsoleColor.Yellow, ConsoleColor.DarkGray };

        static string version = "1.0.0";

        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            bool showHelp = false;

            OptionSet argParser = new OptionSet()
            {
                { "h|help", "Print this message and exit.", v => { showHelp = v != null; } },
                { "b|burn-in=", "Optional. Specify number of samples to discard as burn-in. Can be specified either as an integer   (e.g. 1000) or as a percentage (e.g. 10%). Default: 10%.", v => { if (v.Contains("%")) { percBurnin = double.Parse(v.Replace("%", ""), System.Globalization.CultureInfo.InvariantCulture) * 0.01; burnin = -1; } else { burnin = int.Parse(v); } } }
            };

            chainFiles = argParser.Parse(args);

            bool showUsage = false;

            if (chainFiles.Count < 1 && !showHelp)
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("No trace file specified!");
                showUsage = true;
            }

            if (showUsage || showHelp)
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("ChainMonitor version {0}", version);
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Usage:");
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("  ChainMonitor {-h|--help}");
                ConsoleWrapper.WriteLine("  ChainMonitor [options...] <file1> [<file2> [...]]");
                ConsoleWrapper.WriteLine();
            }

            if (showHelp)
            {
                ConsoleWrapper.WriteLine("Options:");
                ConsoleWrapper.WriteLine();
                argParser.WriteOptionDescriptions(Console.Out);
                ConsoleWrapper.WriteLine();
                return 0;
            }

            if (chainFiles.Count < 1)
            {
                return 1;
            }

            streamReaders = new StreamReader[chainFiles.Count];

            for (int i = 0; i < chainFiles.Count; i++)
            {
                streamReaders[i] = new StreamReader(chainFiles[i]);

                string line = Regex.Replace(streamReaders[i].ReadLine(), "\\[[^]]*\\]", "").Replace("\t", " ").Trim(' ');

                while (string.IsNullOrEmpty(line))
                {
                    line = Regex.Replace(streamReaders[i].ReadLine(), "\\[[^]]*\\]", "").Replace("\t", " ").Trim(' ');
                }

                string header = Regex.Replace(line, " +", " ").Trim(' ');

                if (i == 0)
                {
                    headings = header.Split(' ');
                }
                else
                {
                    if (Utils.Utils.StringifyArray(headings, "%%@%%") != Utils.Utils.StringifyArray(header.Split(' '), "%%@%%"))
                    {
                        ConsoleWrapper.WriteLine("The headers from file {0} are not consistent with the others!");
                        prepareExit();
                        return 1;
                    }
                }
            }

            samples = new List<double>[headings.Length][];

            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = new List<double>[streamReaders.Length];
                for (int j = 0; j < streamReaders.Length; j++)
                {
                    samples[i][j] = new List<double>();
                }
            }

            sampleSizes = new int[streamReaders.Length];

            ConsoleWrapper.WriteLine();

            ConsoleWrapper.WriteLine("Reading samples...");

            for (int i = 0; i < streamReaders.Length; i++)
            {

                string line = streamReaders[i].ReadLine();
                sampleSizes[i] = 0;

                while (!string.IsNullOrEmpty(line))
                {
                    line = Regex.Replace(line.Replace("\t", " "), " +", " ").Trim(' ');

                    double[] parsedValues = null;

                    try
                    {
                        parsedValues = (from el in line.Split(' ') select double.Parse(el, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                    }
                    catch
                    {
                        break;
                    }

                    if (parsedValues.Length != headings.Length)
                    {
                        break;
                    }

                    for (int j = 0; j < parsedValues.Length; j++)
                    {
                        samples[j][i].Add(parsedValues[j]);
                    }

                    sampleSizes[i] = int.Parse(line.Substring(0, line.IndexOf(" ")));

                    line = streamReaders[i].ReadLine();
                }


                streamReaders[i].Dispose();
            }

            ConsoleWrapper.WriteLine();
            ConsoleWrapper.WriteLine("Analysing samples...");

            means = new double[headings.Length][];
            stdDevs = new double[headings.Length][];
            ess = new double[headings.Length][];

            overallMeans = new double[headings.Length];
            overallStdDevs = new double[headings.Length];

            for (int i = 0; i < headings.Length; i++)
            {
                List<double> overallWorkingSamples = new List<double>();
                means[i] = new double[streamReaders.Length];
                stdDevs[i] = new double[streamReaders.Length];
                ess[i] = new double[streamReaders.Length];

                for (int j = 0; j < streamReaders.Length; j++)
                {
                    int skip = (burnin >= 0 ? burnin : (int)Math.Floor(percBurnin * samples[i][j].Count));

                    List<double> workingSamples = new List<double>(samples[i][j].Skip(skip));

                    means[i][j] = workingSamples.Average();
                    stdDevs[i][j] = Math.Sqrt(workingSamples.Aggregate(0.0, (acc, val) => acc + (val - means[i][j]) * (val - means[i][j])) / workingSamples.Count);
                    ess[i][j] = Utils.Utils.computeESS(workingSamples, means[i][j], 0);

                    overallWorkingSamples.AddRange(workingSamples);
                }

                overallMeans[i] = overallWorkingSamples.Average();
                overallStdDevs[i] = Math.Sqrt(overallWorkingSamples.Aggregate(0.0, (acc, val) => acc + (val - overallMeans[i]) * (val - overallMeans[i])) / overallWorkingSamples.Count);
            }

            activeFiles = new bool[streamReaders.Length];

            for (int i = 0; i < activeFiles.Length; i++)
            {
                activeFiles[i] = true;
            }

            Console.CursorVisible = false;

            EventWaitHandle closeHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            Console.CancelKeyPress += (s, e) =>
            {
                prepareExit();
                closeHandle.Set();
                e.Cancel = true;
            };

            mainFrameBuffer = new ConsoleFrameBuffer();
            mainFrameBuffer.Flush();

            EventWaitHandle drawHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            EventWaitHandle drawCloseHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            int newXTransl = xTransl;
            int newYTransl = yTransl;
            int newActiveStat = activeStat;
            bool[] newActiveFiles = (bool[])activeFiles.Clone();

            object lockObject = new object();

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                Thread drawThread = new Thread(() =>
                {
                    while (!drawCloseHandle.WaitOne(0))
                    {
                        drawHandle.WaitOne();

                        lock (lockObject)
                        {
                            xTransl = newXTransl;
                            yTransl = newYTransl;
                            activeStat = newActiveStat;
                            activeFiles = (bool[])newActiveFiles.Clone();
                        }

                        drawHandle.Reset();

                        drawConsolePlot();
                    }
                });

                drawThread.Start();
            }

            while (!closeHandle.WaitOne(0))
            {
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    drawHandle.Set();
                }
                else
                {
                    lock (lockObject)
                    {
                        xTransl = newXTransl;
                        yTransl = newYTransl;
                        activeStat = newActiveStat;
                        activeFiles = (bool[])newActiveFiles.Clone();
                    }

                    drawConsolePlot();
                }

                ConsoleKeyInfo ki = Console.ReadKey(true);
                lock (lockObject)
                {
                    switch (ki.Key)
                    {
                        case ConsoleKey.LeftArrow:
                            newXTransl = Math.Max(0, xTransl - 1);
                            break;
                        case ConsoleKey.RightArrow:
                            newXTransl = xTransl + 1;
                            break;

                        case ConsoleKey.UpArrow:
                            newActiveStat = Math.Max(1, activeStat - 1);
                            if (activeStat - 1 - yTransl <= 0 && activeStat > 1)
                            {
                                newYTransl = newYTransl - 1;
                            }
                            break;
                        case ConsoleKey.DownArrow:
                            newActiveStat = Math.Min(activeStat + 1, headings.Length - 1);
                            if (Math.Max(mainFrameBuffer.WindowHeight - headings.Length - 1, chainFiles.Count + 2) + activeStat - yTransl > mainFrameBuffer.WindowHeight - 3 && activeStat < headings.Length - 1)
                            {
                                newYTransl = newYTransl + 1;
                            }
                            break;
                        case ConsoleKey.Q:
                            closeHandle.Set();
                            drawCloseHandle.Set();
                            drawHandle.Set();
                            break;
                        case ConsoleKey.D0:
                        case ConsoleKey.D1:
                        case ConsoleKey.D2:
                        case ConsoleKey.D3:
                        case ConsoleKey.D4:
                        case ConsoleKey.D5:
                        case ConsoleKey.D6:
                        case ConsoleKey.D7:
                        case ConsoleKey.D8:
                        case ConsoleKey.D9:
                        case ConsoleKey.NumPad0:
                        case ConsoleKey.NumPad1:
                        case ConsoleKey.NumPad2:
                        case ConsoleKey.NumPad3:
                        case ConsoleKey.NumPad4:
                        case ConsoleKey.NumPad5:
                        case ConsoleKey.NumPad6:
                        case ConsoleKey.NumPad7:
                        case ConsoleKey.NumPad8:
                        case ConsoleKey.NumPad9:
                            int ind = (int.Parse(ki.KeyChar.ToString()) + 9) % 10;
                            if (ind < activeFiles.Length && (!activeFiles[ind] || activeFiles.Aggregate(0, (acc, val) => val ? acc + 1 : acc) > 1))
                            {
                                newActiveFiles[ind] = !activeFiles[ind];
                            }
                            break;
                    }
                }

            }

            prepareExit();

            return 0;
        }


        static void drawConsolePlot()
        {
            ConsoleFrameBuffer FrameBuffer = new ConsoleFrameBuffer();

            FrameBuffer.CursorLeft = 0;
            FrameBuffer.CursorTop = 0;

            FrameBuffer.ForegroundColor = ConsoleColor.DarkGray;
            FrameBuffer.BackgroundColor = ConsoleColor.Gray;
            FrameBuffer.Write("│");
            FrameBuffer.ForegroundColor = ConsoleColor.Black;
            FrameBuffer.Write(" " + "Input files ".Pad(Math.Max(11, (from el in headings.Skip(1) select el.Length).Max()), Extensions.PadType.Center) + " ");
            FrameBuffer.ForegroundColor = ConsoleColor.DarkGray;
            FrameBuffer.Write("│");
            FrameBuffer.ForegroundColor = ConsoleColor.Black;
            FrameBuffer.Write(" " + "Samples".Pad(Math.Max(7, sampleSizes.Max().ToString().Length), Extensions.PadType.Center) + " ");
            FrameBuffer.ForegroundColor = ConsoleColor.DarkGray;
            FrameBuffer.Write("│");

            FrameBuffer.ForegroundColor = ConsoleColor.Gray;
            FrameBuffer.BackgroundColor = ConsoleColor.Black;

            for (int i = 0; i < chainFiles.Count; i++)
            {
                FrameBuffer.CursorLeft = 0;
                FrameBuffer.CursorTop = i + 1;

                FrameBuffer.ForegroundColor = ConsoleColor.DarkGray;
                FrameBuffer.Write(((i + 1) % 10).ToString());

                if (activeFiles[i])
                {
                    FrameBuffer.ForegroundColor = colors[i % 10];
                    FrameBuffer.Write("■");
                }
                else
                {
                    FrameBuffer.Write(" ");
                }

                FrameBuffer.ForegroundColor = ConsoleColor.Gray;

                string shortName = Utils.Utils.ShortFileName(chainFiles[i], Math.Max(11, (from el in headings.Skip(1) select el.Length).Max()));

                if (!shortName.Contains("~"))
                {
                    FrameBuffer.Write(shortName);
                }
                else
                {
                    FrameBuffer.Write(shortName.Substring(0, shortName.IndexOf("~")));
                    FrameBuffer.ForegroundColor = ConsoleColor.Cyan;
                    FrameBuffer.ForegroundColor = ConsoleColor.DarkCyan;
                    FrameBuffer.Write("~");
                    FrameBuffer.ForegroundColor = ConsoleColor.Gray;
                    FrameBuffer.Write(shortName.Substring(shortName.IndexOf("~") + 1));
                }

                FrameBuffer.ForegroundColor = ConsoleColor.DarkGray;
                FrameBuffer.Write(" │ ");

                FrameBuffer.ForegroundColor = ConsoleColor.Gray;
                FrameBuffer.Write(sampleSizes[i].ToString().Pad(Math.Max(7, sampleSizes.Max().ToString().Length), Extensions.PadType.Right) + " ");

                FrameBuffer.ForegroundColor = ConsoleColor.DarkGray;
                FrameBuffer.Write("│");
            }

            FrameBuffer.CursorTop++;
            FrameBuffer.CursorLeft = 0;
            FrameBuffer.Write("└" + new string('─', Math.Max(11, (from el in headings.Skip(1) select el.Length).Max()) + 2) + "┴" + new string('─', Math.Max(7, sampleSizes.Max().ToString().Length) + 2) + "┘");

            FrameBuffer.CursorTop = Math.Max(FrameBuffer.WindowHeight - headings.Length - 1, chainFiles.Count + 2);
            FrameBuffer.CursorLeft = 0;

            FrameBuffer.BackgroundColor = ConsoleColor.Gray;
            FrameBuffer.Write("│ ");

            FrameBuffer.ForegroundColor = ConsoleColor.Black;
            FrameBuffer.Write("Statistic".Pad(Math.Max(11, (from el in headings.Skip(1) select el.Length).Max()), Extensions.PadType.Center) + " ");

            FrameBuffer.ForegroundColor = ConsoleColor.DarkGray;
            FrameBuffer.Write("│ ");

            FrameBuffer.ForegroundColor = ConsoleColor.Black;
            FrameBuffer.Write(" Mean   ");

            FrameBuffer.ForegroundColor = ConsoleColor.DarkGray;
            FrameBuffer.Write("│");

            FrameBuffer.BackgroundColor = ConsoleColor.Black;

            int activeCount = activeFiles.Aggregate(0, (acc, nval) => nval ? acc++ : acc);

            if (yTransl > 0)
            {
                FrameBuffer.CursorLeft = 0;
                FrameBuffer.CursorTop++;

                FrameBuffer.Write("│");

                FrameBuffer.ForegroundColor = ConsoleColor.Green;
                FrameBuffer.Write("▲");


                FrameBuffer.ForegroundColor = ConsoleColor.Gray;
                FrameBuffer.Write("...".Pad(Math.Max(11, (from el in headings.Skip(1) select el.Length).Max()), Extensions.PadType.Center) + " ");

                FrameBuffer.ForegroundColor = ConsoleColor.DarkGray;
                FrameBuffer.Write("│ ");

                FrameBuffer.ForegroundColor = ConsoleColor.Gray;

                FrameBuffer.Write("...".Pad(Math.Max(7, sampleSizes.Max().ToString().Length), Extensions.PadType.Right) + " ");

                FrameBuffer.ForegroundColor = ConsoleColor.DarkGray;
                FrameBuffer.Write("│");

                yTransl++;
            }



            for (int i = 1 + yTransl; i < headings.Length; i++)
            {
                if (FrameBuffer.CursorTop < FrameBuffer.WindowHeight - 3 || (FrameBuffer.CursorTop < FrameBuffer.WindowHeight - 2 && i == headings.Length - 1))
                {
                    FrameBuffer.CursorLeft = 0;
                    FrameBuffer.CursorTop++;

                    FrameBuffer.Write("│");

                    if (i != activeStat)
                    {
                        if (i > 1 + yTransl && i < headings.Length - 1 || (i == 1 + yTransl && yTransl > 0))
                        {
                            FrameBuffer.Write(" ");
                        }
                        else if (i == 1 && yTransl == 0)
                        {
                            FrameBuffer.ForegroundColor = ConsoleColor.Green;
                            FrameBuffer.Write("▲");
                        }
                        else
                        {
                            FrameBuffer.ForegroundColor = ConsoleColor.Green;
                            FrameBuffer.Write("▼");
                        }
                    }
                    else
                    {
                        FrameBuffer.ForegroundColor = ConsoleColor.Blue;
                        FrameBuffer.Write("■");
                    }

                    FrameBuffer.ForegroundColor = ConsoleColor.Gray;
                    FrameBuffer.Write(headings[i].Pad(Math.Max(11, (from el in headings.Skip(1) select el.Length).Max()), Extensions.PadType.Center) + " ");

                    FrameBuffer.ForegroundColor = ConsoleColor.DarkGray;
                    FrameBuffer.Write("│ ");

                    double mean = 0;
                    double totalSamples = 0;

                    for (int j = 0; j < samples[i].Length; j++)
                    {
                        if (activeFiles[j])
                        {
                            int skip = (burnin >= 0 ? burnin : (int)Math.Floor(percBurnin * samples[i][j].Count));
                            mean += means[i][j] * (samples[i][j].Count - skip);
                            totalSamples += (samples[i][j].Count - skip);
                        }
                    }

                    mean /= totalSamples;

                    FrameBuffer.ForegroundColor = ConsoleColor.Gray;

                    FrameBuffer.Write(mean.ToString(System.Globalization.CultureInfo.InvariantCulture).Pad(Math.Max(7, sampleSizes.Max().ToString().Length), Extensions.PadType.Right) + " ");
                    FrameBuffer.ForegroundColor = ConsoleColor.DarkGray;
                    FrameBuffer.Write("│");
                }
                else if (FrameBuffer.CursorTop < FrameBuffer.WindowHeight - 2)
                {
                    FrameBuffer.CursorLeft = 0;
                    FrameBuffer.CursorTop++;

                    FrameBuffer.Write("│");

                    FrameBuffer.ForegroundColor = ConsoleColor.Green;
                    FrameBuffer.Write("▼");


                    FrameBuffer.ForegroundColor = ConsoleColor.Gray;
                    FrameBuffer.Write("...".Pad(Math.Max(11, (from el in headings.Skip(1) select el.Length).Max()), Extensions.PadType.Center) + " ");

                    FrameBuffer.ForegroundColor = ConsoleColor.DarkGray;
                    FrameBuffer.Write("│ ");

                    FrameBuffer.ForegroundColor = ConsoleColor.Gray;

                    FrameBuffer.Write("...".Pad(Math.Max(7, sampleSizes.Max().ToString().Length), Extensions.PadType.Right) + " ");

                    FrameBuffer.ForegroundColor = ConsoleColor.DarkGray;
                    FrameBuffer.Write("│");

                }
                else
                {
                    break;
                }
            }

            if (yTransl > 0)
            {
                yTransl--;
            }

            FrameBuffer.CursorTop++;
            FrameBuffer.CursorLeft = 0;
            FrameBuffer.Write("└" + new string('─', Math.Max(11, (from el in headings.Skip(1) select el.Length).Max()) + 2) + "┴" + new string('─', Math.Max(7, sampleSizes.Max().ToString().Length) + 2) + "┘");

            List<double> activeSamples = new List<double>();

            for (int j = 0; j < samples[activeStat].Length; j++)
            {
                if (activeFiles[j])
                {
                    int skip = (burnin >= 0 ? burnin : (int)Math.Floor(percBurnin * samples[activeStat][j].Count));
                    activeSamples.AddRange(samples[activeStat][j].Skip(skip));
                }
            }

            double activeMean = activeSamples.Average();
            double activeSD = Math.Sqrt(activeSamples.Aggregate(0.0, (acc, val) => acc + (val - activeMean) * (val - activeMean)) / activeSamples.Count);

            (string, ConsoleColor)[] stats = new (string, ConsoleColor)[6];

            stats[0] = ("ESS(s): ", ConsoleColor.Gray);

            string esss = "(";
            for (int i = 0; i < activeFiles.Length; i++)
            {
                if (activeFiles[i])
                {
                    esss += ess[activeStat][i].ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "/";
                }
            }
            esss = esss.Substring(0, esss.Length - 1) + ")";

            stats[1] = (esss, ConsoleColor.DarkGray);

            stats[2] = (" Mean(s): ", ConsoleColor.Gray);


            string meanString = activeMean.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + "(";

            for (int i = 0; i < activeFiles.Length; i++)
            {
                if (activeFiles[i])
                {
                    meanString += means[activeStat][i].ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + "/";
                }
            }

            meanString = meanString.Substring(0, meanString.Length - 1) + ")";

            stats[3] = (meanString, ConsoleColor.DarkGray);

            stats[4] = (" SD(s): ", ConsoleColor.Gray);

            string sds = activeSD.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + "(";

            for (int i = 0; i < activeFiles.Length; i++)
            {
                if (activeFiles[i])
                {
                    sds += stdDevs[activeStat][i].ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + "/";
                }
            }

            sds = sds.Substring(0, sds.Length - 1) + ")";

            stats[5] = (sds, ConsoleColor.DarkGray);

            int maxXTransl = Math.Max(0, stats.Aggregate(0, (acc, val) => acc + val.Item1.Length) - (FrameBuffer.WindowWidth - 6 - FrameBuffer.CursorLeft));

            xTransl = Math.Min(Math.Max(0, xTransl), maxXTransl);

            if (maxXTransl > 0)
            {
                if (xTransl > 0)
                {
                    FrameBuffer.ForegroundColor = ConsoleColor.Green;
                }
                else
                {
                    FrameBuffer.ForegroundColor = ConsoleColor.DarkGray;
                }
                FrameBuffer.Write(" ◄ ");
            }
            else
            {
                FrameBuffer.Write("   ");
            }

            int remainingTransl = xTransl;

            for (int i = 0; i < stats.Length; i++)
            {
                if (stats[i].Item1.Length > remainingTransl)
                {
                    if (FrameBuffer.CursorLeft < FrameBuffer.WindowWidth - 2)
                    {
                        FrameBuffer.ForegroundColor = stats[i].Item2;
                        string subStr = stats[i].Item1.Substring(remainingTransl);
                        remainingTransl = 0;
                        FrameBuffer.Write(subStr.Substring(0, Math.Min(subStr.Length, FrameBuffer.WindowWidth - FrameBuffer.CursorLeft - 3)));
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    remainingTransl -= stats[i].Item1.Length;
                }
            }

            if (maxXTransl > 0)
            {

                if (xTransl < maxXTransl)
                {
                    FrameBuffer.ForegroundColor = ConsoleColor.Green;
                }
                else
                {
                    FrameBuffer.ForegroundColor = ConsoleColor.Gray;
                }

                FrameBuffer.Write(" ►");

            }

            int plotX0 = Math.Max(7, sampleSizes.Max().ToString().Length) + Math.Max(11, (from el in headings.Skip(1) select el.Length).Max()) + 7;

            int plotResolutionX = FrameBuffer.WindowWidth - plotX0 - 1;
            int plotResolutionY = FrameBuffer.WindowHeight - 3;

            FrameBuffer.ForegroundColor = ConsoleColor.White;

            FrameBuffer.CursorTop = FrameBuffer.WindowHeight - 2;
            FrameBuffer.CursorLeft = plotX0;
            FrameBuffer.Write(new string('─', plotResolutionX));

            List<int[]> bins = new List<int[]>();
            List<int> binInds = new List<int>();

            double minX = (from el in samples[activeStat] select el.Min()).Min();
            double maxX = (from el in samples[activeStat] select el.Max()).Max();

            double binSize = (maxX - minX) / plotResolutionX;

            if (binSize == 0)
            {
                binSize = 0.05;
            }

            for (int i = 0; i < streamReaders.Length; i++)
            {
                if (activeFiles[i])
                {
                    bins.Add(new int[plotResolutionX]);
                    binInds.Add(i);

                    int skip = (burnin >= 0 ? burnin : (int)Math.Floor(percBurnin * samples[activeStat][i].Count));

                    for (int j = 0; j + skip < samples[activeStat][i].Count; j++)
                    {
                        bins[bins.Count - 1][Math.Min((int)Math.Floor((samples[activeStat][i][skip + j] - minX) / binSize), plotResolutionX - 1)]++;
                    }
                }
            }

            int[] sumYs = (from el in bins select el.Sum()).ToArray();

            double[][] doubleBins = new double[bins.Count][];
            for (int i = 0; i < bins.Count; i++)
            {
                doubleBins[i] = new double[bins[i].Length];
                for (int x = 0; x < bins[i].Length; x++)
                {
                    doubleBins[i][x] = (double)bins[i][x] / sumYs[i];
                }
            }

            double maxY = (from el in doubleBins select el.Max()).Max();

            for (int x = 0; x < plotResolutionX; x++)
            {
                List<(double, int)> sortedBins = new List<(double, int)>(doubleBins.Length);

                for (int i = 0; i < doubleBins.Length; i++)
                {
                    sortedBins.Add((doubleBins[i][x] * plotResolutionY / maxY, binInds[i]));
                }

                sortedBins.Sort((a, b) => Math.Sign(a.Item1 - b.Item1));

                for (int y = (int)Math.Round(sortedBins[0].Item1); y >= 0; y--)
                {
                    FrameBuffer.CursorLeft = x + plotX0;
                    FrameBuffer.CursorTop = plotResolutionY - y;
                    FrameBuffer.ForegroundColor = colors[sortedBins[0].Item2 % 10];
                    FrameBuffer.Write("█");
                }

                for (int i = 1; i < sortedBins.Count; i++)
                {
                    for (int y = (int)Math.Round(sortedBins[i].Item1); y > (int)Math.Round(sortedBins[i - 1].Item1); y--)
                    {
                        FrameBuffer.CursorLeft = x + plotX0;
                        FrameBuffer.CursorTop = plotResolutionY - y;
                        FrameBuffer.ForegroundColor = colors[sortedBins[i].Item2 % 10];
                        FrameBuffer.Write("█");
                    }
                }
            }

            mainFrameBuffer.Update(FrameBuffer);
        }

        static void prepareExit()
        {
            Console.Clear();
            Console.CursorVisible = true;
            Console.ForegroundColor = ConsoleColor.Gray;
            for (int i = 0; i < streamReaders.Length; i++)
            {
                streamReaders[i].Dispose();
            }
        }
    }
}
