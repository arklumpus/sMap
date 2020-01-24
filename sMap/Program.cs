using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
using MatrixExponential;
using SlimTreeNode;
using Utils;
using Mono.Options;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.IO.Compression;

namespace sMap
{
    public class Program
    {
        static ThreadSafeRandom mainRandom;

        public static bool ExitAfterMCMC = false;

        public static double mcmcSampleCount = -1;

        private static bool _runningGui = false;

        public static string Version = "1.0.5";

        public static bool RunningGUI
        {
            get
            {
                return _runningGui;
            }

            set
            {
                _runningGui = value;
                Utils.Utils.RunningGui = value;
            }
        }

        public static int Main(string[] args)
        {
            string treeFile = "";
            string meanTreeFile = "";
            string dataFile = "";
            string dependencyFile = "";
            string rateFile = "";
            string piFile = "";
            string outputPrefix = "";
            string parameterFiles = "";

            MaximisationStrategy[] MLStrategies = new MaximisationStrategy[] { MaximisationStrategy.Parse("IterativeSampling(0.0001, 10.0001, 0.1, plot, Value, 0.001)"), MaximisationStrategy.Parse("RandomWalk(Value, 0.001, 10000, plot)"), MaximisationStrategy.Parse("NesterovClimbing(Value, 0.001, 100, plot)") };

            int numSim = 0;
            int ppMultiplicity = 0;
            bool computeDTest = false;
            string priorTempFolder = null;

            int numRuns = 2;
            int numChains = 4;
            MCMC.temperatureIncrement = 0.5;
            MCMC.sampleFrequency = 10;
            MCMC.swapFrequency = 10;
            MCMC.diagnosticFrequency = 1000;

            MCMC.minSamples = -1;
            MCMC.convergenceCoVThreshold = -1;
            MCMC.convergenceESSThreshold = 200;

            MCMC.initialBurnin = 1000;

            MCMC.estimateStepSize = true;
            MCMC.tuningAttempts = 10;
            MCMC.tuningSteps = 100;
            MCMC.magicAcceptanceRate = 0.37;
            double[] inputStepSizeMultipliers = new double[] { 1 };
            MCMC.globalStepSizeMultiplier = 1;

            bool steppingStone = false;

            int steppingStoneSteps = 8;
            double steppingStoneShape = 0.3;
            int steppingStoneSamples = -1;
            bool steppingStoneEstimateStepSize = false;

            float plotWidth = -1;
            float? plotHeight = null;

            Plotting.BinRules binRule = Plotting.BinRules.FreedmanDiaconis;

            bool showHelp = false;

            int seed = -1;

            bool runUnderPrior = false;

            bool normaliseLength = false;
            double? normalisationFactor = null;

            bool coerceLengths = false;
            double? coercionThreshold = null;

            bool pollInterrupt = false;

            int numThreads = 1;


            string archiveFileName = null;


            OptionSet preParser = new OptionSet()
            {
                { "o|output=", "Output file prefix. May point to a different directory.", v => { outputPrefix = v; } },
                { "a|archive=", "Analysis archive. A ZIP file containing all the data and command-line parameters necessary to perform the analysis.", v => { archiveFileName = v; } },
            };

            List<string> preUnrecognised = preParser.Parse(args);

            bool showUsage = false;

            string tempDir = null;
            string prevWD = null;

            if (!string.IsNullOrEmpty(archiveFileName))
            {
                if (preUnrecognised.Count > 0)
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("Unrecognised argument" + (preUnrecognised.Count > 1 ? "s" : "") + ": " + Utils.Utils.StringifyArray(preUnrecognised, " "));
                    showUsage = true;
                }

                if (string.IsNullOrEmpty(outputPrefix))
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("No output prefix specified!");
                    showUsage = true;
                }

                if (!string.IsNullOrEmpty(archiveFileName) && !showUsage)
                {
                    prevWD = Directory.GetCurrentDirectory();

                    tempDir = Path.GetTempFileName();
                    File.Delete(tempDir);

                    Directory.CreateDirectory(tempDir);

                    ZipFile.ExtractToDirectory(archiveFileName, tempDir);

                    args = File.ReadAllLines(Path.Combine(tempDir, ".args"));

                    outputPrefix = Path.GetFullPath(outputPrefix);

                    Directory.SetCurrentDirectory(tempDir);
                }
            }

            OptionSet argParser = new OptionSet()
            {
                { "h|help", "Print this message and exit.", v => { showHelp = v != null; } },
                { "t|tree=", "A tree file. It should contain one or more trees (one per line) in newick format.", v => { treeFile = v; } },
                { "d|x|data=", "A data matrix file in (relaxed) PHYLIP format.", v => { dataFile = v; } },
                { "o|output=", "Output file prefix. May point to a different directory.", v => { outputPrefix = v; } },
                { "n|num-sim=", "Number of simulations/MCMC samples. Has to be greater than 0.", v => { numSim = int.Parse(v); } },
                { "T|mean-tree=", "A mean tree file. It should contain exactly one tree in newick format. If the tree file contains only one tree, this parameter is optional and its default value equals the tree file; if the tree file contains more than one tree, this parameter is required.", v => { meanTreeFile = v; } },
                { "a|archive=", "Analysis archive. A ZIP file containing all the data and command-line parameters necessary to perform the analysis.", v => { } },
                { "D|dependency=", "Optional. A NEXUS format file containing one or more \"Dependency\" blocks that specify dependency relationships between characters. Characters whose relationships have not been specified are assumed to be independent.", v => { dependencyFile = v; } },
                { "r|rates=", "Optional. A NEXUS format file containing one or more \"Rates\" blocks that specify transition rates between character states. Rates that are not specified will be estimated using Maximum-Likelihood.", v => { rateFile = v; } },
                { "p|pi=", "Optional. A NEXUS format file containing one or more \"Pi\" blocks that specify the equilibrium frequencies of character states. Equilibrium frequencies that are not specified will be assumed to be equal to 1/(number of states).", v => { piFile = v; } },
                { "i|input=", "Optional. A NEXUS format file containing one or more \"Dependency\", \"Rates\" and/or \"Pi\" blocks. This is equivalent to providing the same file to the -D, -r and -p options.", v => { dependencyFile = v; rateFile = v; piFile = v; } },
                { "s|seed=", "Optional. Random number seed. Should only be used for debug purposes, as it will likely cause degradation of random number quality. Default: none.", v => { seed = int.Parse(v); } },
                { "N|norm:", "Optional. If enabled, the branch lengths of the trees are normalised. If an optional {VALUE} is supplied, the branch lengths are all divided by that VALUE, otherwise they are divided by the mean tree height. Default: disabled.", v => { normaliseLength = true; if (!string.IsNullOrEmpty(v)) { normalisationFactor = double.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } } },
                { "c|coerce=", "Optional. If enabled, any branch lengths that are smaller than the specified {VALUE} are coerced to VALUE. Default: disabled.", v=> { coerceLengths = true; coercionThreshold = double.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "m=", "Optional. Maximum likelihood optimisation strategy. Default: IterativeSampling(0.0001,10.0001,0.1,plot,Value,0.001)|RandomWalk(Value,0.001,10000,plot)|NesterovClimbing(Value,0.001,100,plot).", v => { MLStrategies = (from el in v.Split('|') select MaximisationStrategy.Parse(el)).ToArray(); } },
                { "nt|num-threads=", "Optional. Number of threads for the prior and posterior computation step, and maximum number of threads for the simulation step. Default: 1.", v => { numThreads = int.Parse(v); Utils.Utils.MaxThreads = numThreads; } },
                { "dt|d-test=", "Optional. Perform a D-test for correlation between different characters, sampling {VALUE} histories from the posterior predictive distribution of each parameter sample. Note that will cause {VALUE} * num-sim unconstrained histories to be sampled, which may take some time. Default: 0 (disabled).", v => { ppMultiplicity = int.Parse(v); computeDTest = true; } },
                { "pp|posterior-predictive=", "Optional. Sample {VALUE} histories from the posterior predictive distribution of each parameter sample. These may be later used with Stat-sMap to perform D-tests. Note that will cause {VALUE} * num-sim unconstrained histories to be sampled and saved, which may take some time and will increase the output file size. Default: 0 (disabled).", v => { ppMultiplicity = int.Parse(v); computeDTest = false; } },
                { "poll", "Optional. If enabled, during MCMC analysis we will look for a file called \"<output>.interrupt\". If such a file exists, the MCMC chain will stop as if CTRL+C had been detected (and the file will be deleted).", v => { pollInterrupt = v != null; } },
                { "num-runs=", "Optional. Number of independent parallel MCMC runs. Default: 2.", v => { numRuns = int.Parse(v); } },
                { "num-chains=", "Optional. Number of parallel Metropolis-coupled MCMC chains (per run). Default: 4.", v => { numChains = int.Parse(v); } },
                { "temp=", "Optional. Coefficient used to determine the 'temperature' of the MCMCMC chains. Default: 0.5.", v => { MCMC.temperatureIncrement = double.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "sf|sampling-frequency=", "Optional. MCMC sampling frequency. Default: 10.", v => { MCMC.sampleFrequency = int.Parse(v); } },
                { "wf|swap-frequency=", "Optional. Chain swapping frequency. Must be a multiple of sampling-frequency. Default: 10.", v => { MCMC.swapFrequency = int.Parse(v); } },
                { "df|diagnostic-frequency=", "Optional. MCMC diagnostic frequency. Default: 1000.", v => { MCMC.diagnosticFrequency = int.Parse(v); } },
                { "min-samples=", "Optional. Minimum number of MCMC samples that a MCMC run needs to pass the convergence test. Default: 2 * num-sim.", v => { MCMC.minSamples = int.Parse(v); } },
                { "max-cov=", "Optional. The coefficient of variation of the mean and of the standard deviation of each sampled parameter must have a lower values than this for the run to pass the convergence test. Default: -ln(1 - (7/3 - 2 / (2 * num-runs - 1)) / 3) / 16.", v => { MCMC.convergenceCoVThreshold = double.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "min-ess=", "Optional. Minimum Effective Sample Size (ESS) that each sampled parameter in each run must have for the run to pass the convergence test. Default: 200.", v => { MCMC.convergenceESSThreshold = double.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "burn-in=", "Optional. Number of MCMC steps that will be discarded for the initial burn-in. Default: 1000.", v => { MCMC.initialBurnin = int.Parse(v); } },
                { "estimate-steps", "Optional. If enabled, an optimal MCMC proposal step sizes will be estimated. If disabled, the default step sizes will be used. Default: on.", v => { MCMC.estimateStepSize = v != null; } },
                { "tuning-attempts=", "Optional. Number of tuning attempts to estimate the optimal MCMC proposal step sizes. Default: 10.", v => { MCMC.tuningAttempts = int.Parse(v); } },
                { "tuning-steps=", "Optional. Number of MCMC steps in each tuning attempt to estimate the optimal MCMC proposal step sizes. Default: 100.", v => { MCMC.tuningSteps = int.Parse(v); } },
                { "acceptance-rate=", "Optional. Target \"magic\" acceptance rate for a single chain to be used when estimating MCMC proposal step sizes. If multiple chains are used, the final acceptance rate will be higher. Default: 0.37.", v => { MCMC.magicAcceptanceRate = double.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "sm|step-multipliers=", "Optional. MCMC proposal step size multipliers (one per variable). If only one number is specified, it will be applied to all variables. Note: when estimate-step is specified, it only has the effect of shifting the estimation starting point. Default: 1.", v => { inputStepSizeMultipliers = (from el in v.Split(',') select double.Parse(el, System.Globalization.CultureInfo.InvariantCulture)).ToArray(); } },
                { "prior", "Optional. If enabled, all likelihood computations during the MCMC analysis will be disabled. Default: off.", v => { runUnderPrior = v != null; } },
                { "ss|stepping-stone", "Optional. If enabled, a stepping-stone analysis will be performed to estimate the marginal likelihood of the model. Default: off.", v => { steppingStone = v != null; } },
                { "ss-steps=", "Optional. Number of stepping-stone steps. Default: 8.", v => { steppingStoneSteps = int.Parse(v); } },
                { "ss-shape=", "Optional. Shape parameter of the beta distribution determining the likelihood exponent for each stepping-stone step. Default: 0.3.", v => { steppingStoneShape = double.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "ss-samples=", "Optional. Number of MCMC samples for each stepping-stone step. Default: num-sim.", v => { steppingStoneSamples = int.Parse(v); } },
                { "ss-estimate-steps", "Optional. If enabled, optimal MCMC proposal step sizes will be estimated for each stepping-stone step. If disabled, the step size determined for the first (posterior) MCMC run will be used instead. Default: off.", v => { steppingStoneEstimateStepSize = v != null; } },
                { "parameters=", "Optional. A list of comma-separated files containing samples for every parameter in the model, one file for each set of character. Each file is allowed to have header rows, and must contain exactly num-sim data rows, each with a sample value for every parameter in the model.", v => { parameterFiles = v; } },
                { "pw|plot-width=", "Optional. Page width in points for the PDF plots. Default: 500.", v => { plotWidth = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "ph|plot-height=", "Optional. Page height in points for the PDF plots. If not specified, it will be determined automatically depending on the plot. Default: auto.", v => { if (float.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float parsed)) { plotHeight = parsed; } else { plotHeight= null; } } },
                { "br|bin-rule=", @"Optional. Rule to determine the number of bins in histogram plots. Default: FreedmanDiaconis. Possible values:
 Sqrt: square-root of the number of samples (n)
 Sturges: log2(n) + 1
 Rice: 2 * n^(1/3)
 Doane: 1 + log2(n) + log2(1 + |g1|/σ(g1))
 Scott: bin_width = 3.5 * σ / n^(1/3)
 FreedmanDiaconis: bin_width = 2 * IQR / n^(1/3)
", v => { binRule = Plotting.ParseBinRule(v); } }
            };

            List<string> unrecognised = argParser.Parse(args);

            treeFile = treeFile.Replace("\"", "");
            meanTreeFile = meanTreeFile.Replace("\"", "");
            dataFile = dataFile.Replace("\"", "");
            dependencyFile = dependencyFile.Replace("\"", "");
            rateFile = rateFile.Replace("\"", "");
            piFile = piFile.Replace("\"", "");
            outputPrefix = outputPrefix.Replace("\"", "");
            parameterFiles = parameterFiles.Replace("\"", "");

            if (unrecognised.Count > 0)
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Unrecognised argument" + (unrecognised.Count > 1 ? "s" : "") + ": " + Utils.Utils.StringifyArray(unrecognised, " "));
                showUsage = true;
            }

            if (!showHelp)
            {
                if (string.IsNullOrEmpty(treeFile))
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("No tree file specified!");
                    showUsage = true;
                }

                if (string.IsNullOrEmpty(dataFile))
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("No data file specified!");
                    showUsage = true;
                }

                if (string.IsNullOrEmpty(outputPrefix))
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("No output prefix specified!");
                    showUsage = true;
                }

                if (numSim <= 0)
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("Invalid number of simulations specified!");
                    showUsage = true;
                }
            }

            if (showUsage || showHelp)
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("sMap version {0}", Version);
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Usage:");
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("  sMap {-h|--help}");
                ConsoleWrapper.WriteLine("  sMap -a <archive_file> -o <output_prefix>");
                ConsoleWrapper.WriteLine("  sMap -t <tree_file> -d <data_file> -o <output_prefix> -n <num_sim> [options...]");
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

            ConsoleWrapper.WriteLine();
            ConsoleWrapper.WriteLine("sMap version {0} was called with the following arguments:", Version);
            ConsoleWrapper.WriteLine(Utils.Utils.StringifyArray(args, " "));
            ConsoleWrapper.WriteLine();

            if (MCMC.minSamples < 0)
            {
                MCMC.minSamples = 2 * numSim;
            }

            if (MCMC.convergenceCoVThreshold < 0)
            {
                MCMC.convergenceCoVThreshold = -Math.Log(1 - (7.0 / 3 - 2.0 / (2 * numRuns - 1)) / 3) / 16;
            }

            if (steppingStoneSamples < 0)
            {
                steppingStoneSamples = numSim;
            }

            Console.OutputEncoding = Encoding.UTF8;

            List<TreeNode> trees = new List<TreeNode>();
            LikelihoodModel[] likModels;
            List<string[]> treeNodeStrings = new List<string[]>();

            ConsoleWrapper.WriteLine("Reading data from {0}", dataFile);
            DataMatrix inputData;
            try
            {
                inputData = new DataMatrix(dataFile);
            }
            catch (Exception e)
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Data parsing error!");
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine(e.Message);
                ConsoleWrapper.WriteLine();
                return 1;
            }

            if (RunningGUI)
            {
                Utils.Utils.Trigger("DataRead", new object[] { inputData });
            }

            ConsoleWrapper.WriteLine("Read data for {0} taxa, {1} characters", inputData.Data.Count, inputData.States.Length);
            ConsoleWrapper.WriteLine("Number of character states:");
            for (int i = 0; i < inputData.States.Length; i++)
            {
                ConsoleWrapper.Write("{0} ", inputData.States[i].Length);
            }

            ConsoleWrapper.WriteLine();
            ConsoleWrapper.WriteLine();

            ConsoleWrapper.WriteLine("Reading tree(s) from {0}", treeFile);

            if (RunningGUI)
            {
                Utils.Utils.Trigger("StartReadingTrees", new object[] { treeFile });
            }

            try
            {
                using (StreamReader sr = new StreamReader(treeFile))
                {

                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            TreeNode tree = TreeNode.Parse(line, null);
                            tree.SortNodes(false);
                            trees.Add(tree);

                            tree.Length = -1;

                            LikelihoodModel tempLikMod = new LikelihoodModel(tree);
                            string[] treeNodeString = new string[tempLikMod.BranchLengths.Length];

                            List<TreeNode> treeNodeList = tree.GetChildrenRecursive();

                            for (int i = 0; i < tempLikMod.BranchLengths.Length; i++)
                            {
                                List<string> nodeLeaves = treeNodeList[i].GetLeafNames();
                                nodeLeaves.Sort();
                                treeNodeString[i] = Utils.Utils.StringifyArray(nodeLeaves);
                            }

                            treeNodeStrings.Add(treeNodeString);
                        }
                        ConsoleWrapper.Write("Read {0} tree(s)", trees.Count);
                        ConsoleWrapper.SetCursorPosition(0, ConsoleWrapper.CursorTop);

                        if (RunningGUI)
                        {
                            Utils.Utils.Trigger("ReadTree", new object[] { trees.Count });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Tree parsing error at line {0}:", trees.Count);
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine(e.Message);
                ConsoleWrapper.WriteLine();
                return 1;
            }

            ConsoleWrapper.WriteLine("Read {0} tree(s)", trees.Count);

            if (RunningGUI)
            {
                Utils.Utils.Trigger("ReadAllTrees", new object[] { trees.Count });
            }

            if (string.IsNullOrEmpty(meanTreeFile) && trees.Count == 1)
            {
                meanTreeFile = treeFile;
            }
            else if (string.IsNullOrEmpty(meanTreeFile))
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("No mean tree file specified, and tree file contains multiple trees!");
                ConsoleWrapper.WriteLine();
                return 1;
            }

            TreeNode meanTree;
            LikelihoodModel meanLikModel;

            try
            {
                string[] meanTreeFileContent = File.ReadAllLines(meanTreeFile);

                if (meanTreeFileContent.Length > 1)
                {
                    for (int i = 1; i < meanTreeFileContent.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(meanTreeFileContent[i].Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace(" ", "")))
                        {
                            ConsoleWrapper.WriteLine();
                            ConsoleWrapper.WriteLine("The mean tree file seems to contain multiple trees!");
                            ConsoleWrapper.WriteLine();
                            return 1;
                        }
                    }
                }

                TreeNode tree = TreeNode.Parse(meanTreeFileContent[0], null);
                tree.SortNodes(false);
                tree.Length = -1;

                meanTree = tree;

                if (plotWidth < 0)
                {
                    double totalLength = tree.DownstreamLength();

                    double minBranch = (from el in tree.GetChildrenRecursive() where el.Length > 0 select el.Length).Min();

                    List<string> leaves = tree.GetLeafNames();

                    double maxLabelWidth = 0;

                    VectSharp.Font fnt = new VectSharp.Font(new VectSharp.FontFamily(new Plotting.Options().FontFamily), 12);

                    for (int i = 0; i < leaves.Count; i++)
                    {
                        maxLabelWidth = Math.Max(maxLabelWidth, fnt.MeasureText(leaves[i]).Width);
                    }

                    maxLabelWidth += 28;

                    plotWidth = Math.Max(500, Math.Min(2000, (float)(10 * totalLength / minBranch))) + (float)maxLabelWidth;
                }

                ConsoleWrapper.WriteLine("Read mean tree");
            }
            catch (Exception e)
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Tree parsing error in mean tree file:");
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine(e.Message);
                ConsoleWrapper.WriteLine();
                return 1;
            }

            bool isMeanTreeClockLike = meanTree.IsClocklike();

            if (isMeanTreeClockLike)
            {
                ConsoleWrapper.WriteLine("Mean tree is clock-like");
            }
            else
            {
                ConsoleWrapper.WriteLine("Mean tree is not clock-like");
            }

            if (normaliseLength && (normalisationFactor == null))
            {
                double meanHeight = meanTree.DownstreamLength();
                normalisationFactor = meanHeight;

                for (int i = 0; i < trees.Count; i++)
                {
                    List<TreeNode> nodes = trees[i].GetChildrenRecursive();

                    for (int j = 0; j < nodes.Count; j++)
                    {
                        nodes[j].Length /= meanHeight;
                    }
                }

                {
                    List<TreeNode> nodes = meanTree.GetChildrenRecursive();

                    for (int j = 0; j < nodes.Count; j++)
                    {
                        nodes[j].Length /= meanHeight;
                    }
                }

                ConsoleWrapper.WriteLine("Mean tree height: {0}", meanHeight.ToString(System.Globalization.CultureInfo.InvariantCulture));
                ConsoleWrapper.WriteLine("Branch lengths have been normalised to obtain a mean tree height of 1");

                using (StreamWriter sw = new StreamWriter(outputPrefix + ".normalised.tre", false, Encoding.UTF8))
                {
                    for (int i = 0; i < trees.Count; i++)
                    {
                        sw.WriteLine(trees[i].ToString());
                    }
                }
            }
            else if (normaliseLength)
            {
                for (int i = 0; i < trees.Count; i++)
                {
                    List<TreeNode> nodes = trees[i].GetChildrenRecursive();

                    for (int j = 0; j < nodes.Count; j++)
                    {
                        nodes[j].Length /= (double)normalisationFactor;
                    }
                }

                {
                    List<TreeNode> nodes = meanTree.GetChildrenRecursive();

                    for (int j = 0; j < nodes.Count; j++)
                    {
                        nodes[j].Length /= (double)normalisationFactor;
                    }
                }

                ConsoleWrapper.WriteLine("Branch lengths have been divided by {0}", ((double)normalisationFactor).ToString(System.Globalization.CultureInfo.InvariantCulture));

                using (StreamWriter sw = new StreamWriter(outputPrefix + ".normalised.tre", false, Encoding.UTF8))
                {
                    for (int i = 0; i < trees.Count; i++)
                    {
                        sw.WriteLine(trees[i].ToString());
                    }
                }
            }

            int coercionCount = 0;

            if (coerceLengths)
            {
                double thres = (double)coercionThreshold;
                for (int i = 0; i < trees.Count; i++)
                {
                    List<TreeNode> nodes = trees[i].GetChildrenRecursive();

                    for (int j = 0; j < nodes.Count; j++)
                    {
                        if (nodes[j].Parent != null && nodes[j].Length < thres)
                        {
                            coercionCount++;
                            double delta = nodes[j].Length - thres;
                            nodes[j].Length = thres;
                            for (int k = 0; k < nodes[j].Children.Count; k++)
                            {
                                nodes[j].Children[k].Length += delta;
                            }
                        }
                    }
                }

                {
                    List<TreeNode> nodes = meanTree.GetChildrenRecursive();

                    for (int j = 0; j < nodes.Count; j++)
                    {
                        if (nodes[j].Parent != null && nodes[j].Length < thres)
                        {
                            coercionCount++;
                            double delta = nodes[j].Length - thres;
                            nodes[j].Length = thres;
                            for (int k = 0; k < nodes[j].Children.Count; k++)
                            {
                                nodes[j].Children[k].Length += delta;
                            }
                        }
                    }
                }
            }

            if (coercionCount > 0)
            {
                ConsoleWrapper.WriteLine("{0} branches have been coerced to have a minimum length of {1}", coercionCount, ((double)coercionThreshold).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            ConsoleWrapper.WriteLine();

            if (seed == -1)
            {
                ConsoleWrapper.WriteLine("Initializing RNG");
                ConsoleWrapper.WriteLine();
                mainRandom = new ThreadSafeRandom();
            }
            else
            {
                ConsoleWrapper.WriteLine("Initializing RNG (seed: {0})", seed);
                ConsoleWrapper.WriteLine();
                mainRandom = new ThreadSafeRandom(seed);
            }



            ConsoleWrapper.WriteLine("Generating likelihood models...");
            ConsoleWrapper.WriteLine();

            likModels = new LikelihoodModel[trees.Count];

            for (int i = 0; i < trees.Count; i++)
            {
                likModels[i] = new LikelihoodModel(trees[i]);
            }

            meanLikModel = new LikelihoodModel(meanTree);

            int[][] meanNodeCorresp = new int[meanLikModel.BranchLengths.Length][];

            List<TreeNode> meanTreeNodeList = meanTree.GetChildrenRecursive();

            for (int i = 0; i < meanLikModel.BranchLengths.Length; i++)
            {
                List<string> nodeLeafList = meanTreeNodeList[i].GetLeafNames();
                nodeLeafList.Sort();
                string nodeLeaves = Utils.Utils.StringifyArray(nodeLeafList);
                meanNodeCorresp[meanLikModel.BranchLengths.Length - i - 1] = new int[trees.Count];

                for (int j = 0; j < trees.Count; j++)
                {
                    int ind = treeNodeStrings[j].IndexOf(nodeLeaves);
                    if (ind >= 0)
                    {
                        meanNodeCorresp[meanLikModel.BranchLengths.Length - i - 1][j] = likModels[j].BranchLengths.Length - ind - 1;
                    }
                    else
                    {
                        meanNodeCorresp[meanLikModel.BranchLengths.Length - i - 1][j] = -1;
                    }
                }
            }

            treeNodeStrings.Clear();
            meanTreeNodeList.Clear();

            if (RunningGUI)
            {
                Utils.Utils.Trigger("ReadMeanTree", new object[] { meanTree, meanLikModel, meanNodeCorresp, likModels });
            }

            CharacterDependency[][] inputDependencies;

            if (!string.IsNullOrEmpty(dependencyFile))
            {
                ConsoleWrapper.WriteLine("Reading character dependencies from {0}", dependencyFile);

                try
                {
                    inputDependencies = Utils.Parsing.ParseDependencies(dependencyFile, inputData.States, mainRandom);
                }
                catch (Exception e)
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("Error parsing dependency file:");
                    ConsoleWrapper.WriteLine(e.Message);
                    ConsoleWrapper.WriteLine();
                    return 1;
                }
            }
            else
            {
                inputDependencies = Utils.Parsing.GetDefaultDependencies(inputData.States);
            }

            Dictionary<string, Parameter>[] inputRates;

            if (!string.IsNullOrEmpty(rateFile))
            {
                ConsoleWrapper.WriteLine("Reading rates from {0}", rateFile);

                try
                {
                    inputRates = Parsing.ParseRateFile(rateFile, inputData.States, mainRandom);
                }
                catch (Exception e)
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("Error parsing rates file:");
                    ConsoleWrapper.WriteLine(e.Message);
                    ConsoleWrapper.WriteLine();
                    return 1;
                }
            }
            else
            {
                inputRates = Parsing.GetDefaultRates(inputData.States);
            }

            Dictionary<string, Parameter>[] inputPi;

            if (!string.IsNullOrEmpty(piFile))
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Reading pis from {0}", piFile);

                try
                {
                    inputPi = Parsing.ParsePiFile(piFile, inputData.States, mainRandom);
                }
                catch (Exception e)
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("Error parsing pi file:");
                    ConsoleWrapper.WriteLine(e.Message);
                    ConsoleWrapper.WriteLine();
                    return 1;
                }
            }
            else
            {
                inputPi = Parsing.GetDefaultPi(inputData.States);
            }

            ConsoleWrapper.WriteLine();

            Thread interruptThread = null;

            EventWaitHandle interruptThreadWaitHandle = null;

            if (pollInterrupt)
            {
                ConsoleWrapper.WriteLine("Starting interrupt poll...");
                ConsoleWrapper.WriteLine();

                interruptThreadWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

                interruptThread = new Thread(() =>
                {
                    while (!interruptThreadWaitHandle.WaitOne(1000))
                    {
                        if (interruptThreadWaitHandle.WaitOne(0))
                        {
                            break;
                        }
                        else
                        {
                            if (File.Exists(outputPrefix + ".interrupt"))
                            {
                                ConsoleWrapper.WriteLine();
                                ConsoleWrapper.WriteLine("Interrupt file found!");
                                ConsoleWrapper.WriteLine();

                                foreach (KeyValuePair<string, ConsoleCancelEventHandler> h in MCMC.cancelEventHandlers)
                                {
                                    h.Value.Invoke(h.Value.Target, null);
                                }

                                File.Delete(outputPrefix + ".interrupt");
                            }
                        }
                    }
                });

                interruptThread.Start();

                AppDomain.CurrentDomain.ProcessExit += (s, e) =>
                {
                    interruptThreadWaitHandle.Set();
                };
            }

            /****************/
            //Convert dependent characters to independent

            DataMatrix data = new DataMatrix(inputData);
            List<CharacterDependency[]> realDependencies = new List<CharacterDependency[]>();
            List<Dictionary<string, Parameter>> realPi = new List<Dictionary<string, Parameter>>();
            List<Dictionary<string, Parameter>> realRates = new List<Dictionary<string, Parameter>>();

            for (int i = 0; i < inputDependencies.Length; i++)
            {
                List<CharacterDependency> currentDependencies = new List<CharacterDependency>();

                for (int j = 0; j < inputDependencies[i].Length; j++)
                {
                    if (inputDependencies[i][j].Type == CharacterDependency.Types.Independent)
                    {
                        int newIndex = data.Add(inputData, inputDependencies[i][j].Index);
                        currentDependencies.Add(new CharacterDependency(newIndex, inputDependencies[i][j].Type, inputDependencies[i][j].Dependencies, inputDependencies[i][j].ConditionedProbabilities) { InputDependencyName = inputDependencies[i][j].Index.ToString() });
                        realPi.Add(inputPi[inputDependencies[i][j].Index]);
                        realRates.Add(inputRates[inputDependencies[i][j].Index]);
                    }
                    else if (inputDependencies[i][j].Type == CharacterDependency.Types.Dependent)
                    {
                        int[][] allStates = new int[inputDependencies[i][j].Dependencies.Length][];
                        for (int k = 0; k < inputDependencies[i][j].Dependencies.Length; k++)
                        {
                            allStates[k] = Utils.Utils.Range(0, inputData.States[inputDependencies[i][j].Dependencies[k]].Length);
                        }
                        int[][] combinedStates = Utils.Utils.GetCombinations(allStates);
                        string[] combinedStatesName = new string[combinedStates.Length];
                        for (int k = 0; k < combinedStates.Length; k++)
                        {
                            combinedStatesName[k] = "";
                            for (int l = 0; l < combinedStates[k].Length; l++)
                            {
                                combinedStatesName[k] += inputData.States[inputDependencies[i][j].Dependencies[l]][combinedStates[k][l]] + (l < combinedStates[k].Length - 1 ? "," : "");
                            }
                        }

                        Dictionary<string, double[]> newData = new Dictionary<string, double[]>();

                        foreach (KeyValuePair<string, double[][]> kvp in inputData.Data)
                        {
                            double[] stateProbs = new double[combinedStates.Length];
                            for (int k = 0; k < combinedStates.Length; k++)
                            {
                                stateProbs[k] = 1;

                                for (int l = 0; l < combinedStates[k].Length; l++)
                                {
                                    stateProbs[k] *= kvp.Value[inputDependencies[i][j].Dependencies[l]][combinedStates[k][l]];
                                }
                            }
                            newData.Add(kvp.Key, stateProbs);
                        }

                        int newIndex = data.Add(newData, combinedStatesName);

                        currentDependencies.Add(new CharacterDependency(newIndex) { InputDependencyName = Utils.Utils.StringifyArray(inputDependencies[i][j].Dependencies) });

                        Dictionary<string, Parameter> newPi = new Dictionary<string, Parameter>();
                        Dictionary<string, Parameter> newRates = new Dictionary<string, Parameter>();
                        foreach (KeyValuePair<string, Parameter> kvp in inputDependencies[i][j].ConditionedProbabilities)
                        {
                            if (!kvp.Key.Contains(">"))
                            {
                                newPi.Add(kvp.Key, kvp.Value);
                            }
                            else
                            {
                                newRates.Add(kvp.Key, kvp.Value);
                            }
                        }

                        realPi.Add(newPi);
                        realRates.Add(newRates);
                    }
                    else if (inputDependencies[i][j].Type == CharacterDependency.Types.Conditioned)
                    {
                        int newIndex = data.Add(inputData, inputDependencies[i][j].Index);
                        List<int> newDependencies = new List<int>();

                        for (int k = 0; k < inputDependencies[i][j].Dependencies.Length; k++)
                        {
                            for (int l = 0; l < currentDependencies.Count; l++)
                            {
                                if (currentDependencies[l].InputDependencyName.Split(',').Contains(inputDependencies[i][j].Dependencies[k].ToString()) && !newDependencies.Contains(l))
                                {
                                    newDependencies.Add(l);
                                }
                            }
                        }

                        currentDependencies.Add(new CharacterDependency(newIndex, inputDependencies[i][j].Type, newDependencies.ToArray(), inputDependencies[i][j].ConditionedProbabilities) { InputDependencyName = inputDependencies[i][j].Index.ToString() });
                        realPi.Add(inputPi[inputDependencies[i][j].Index]);
                        realRates.Add(inputRates[inputDependencies[i][j].Index]);
                    }
                }

                realDependencies.Add(currentDependencies.ToArray());
            }

            CharacterDependency[][] dependencies = realDependencies.ToArray();
            Dictionary<string, Parameter>[] pi = realPi.ToArray();
            Dictionary<string, Parameter>[] rates = realRates.ToArray();

            /****************/

            if (RunningGUI)
            {
                Utils.Utils.Trigger("ReadModel", new object[] { inputDependencies, data, dependencies, pi, rates });
            }

            double[][][] parameters = new double[numSim][][];
            int[] treeSamples = new int[numSim];

            List<string>[] paramNames = new List<string>[dependencies.Length];

            using (StreamWriter sw = new StreamWriter(outputPrefix + ".paramNames.txt", false, Encoding.UTF8))
            {
                for (int i = 0; i < dependencies.Length; i++)
                {
                    paramNames[i] = new List<string>();

                    sw.WriteLine("Set {0}:", i);

                    (List<Parameter> rates, List<(Parameter, int, MultivariateDistribution)> pis) paramsToEstimate = Utils.Utils.ParametersToEstimateList(Utils.Utils.GetParametersToEstimate(dependencies[i], rates, pi), mainRandom);

                    string header0 = "T\t";
                    paramNames[i].Add("T");

                    for (int j = 0; j < paramsToEstimate.rates.Count; j++)
                    {
                        string key = null;

                        string charName = null;

                        for (int k = 0; k < dependencies[i].Length; k++)
                        {
                            if (dependencies[i][k].Type == CharacterDependency.Types.Independent)
                            {
                                key = rates[dependencies[i][k].Index].GetKey(paramsToEstimate.rates[j]);
                                if (!string.IsNullOrEmpty(key))
                                {
                                    charName = dependencies[i][k].InputDependencyName;
                                    break;
                                }
                            }
                        }

                        header0 += "r(" + key.Replace(">", " > ") + ")\t";
                        paramNames[i].Add(charName + ": r(" + key.Replace(">", " > ") + ")");
                    }

                    for (int j = 0; j < paramsToEstimate.pis.Count; j++)
                    {
                        string key = null;

                        string charName = null;

                        for (int k = 0; k < dependencies[i].Length; k++)
                        {
                            if (dependencies[i][k].Type == CharacterDependency.Types.Independent)
                            {
                                key = pi[dependencies[i][k].Index].GetKey(paramsToEstimate.pis[j].Item1);
                                if (!string.IsNullOrEmpty(key))
                                {
                                    charName = dependencies[i][k].InputDependencyName;
                                    break;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(key))
                        {
                            header0 += "π(" + key + ")\t";
                            paramNames[i].Add(charName + ": π(" + key + ")");
                        }
                        else
                        {
                            for (int k = 0; k < dependencies[i].Length; k++)
                            {
                                if (dependencies[i][k].Type == CharacterDependency.Types.Conditioned)
                                {
                                    key = dependencies[i][k].ConditionedProbabilities.GetKey(paramsToEstimate.pis[j].Item1);

                                    if (!string.IsNullOrEmpty(key))
                                    {
                                        string newKey = key.Substring(key.IndexOf(">") + 1) + " | " + key.Substring(0, key.IndexOf(">"));
                                        charName = dependencies[i][k].InputDependencyName;
                                        header0 += "p(" + newKey + ")\t";
                                        paramNames[i].Add(charName + ": p(" + newKey + ")");
                                        break;
                                    }
                                }

                            }
                        }
                    }

                    header0 = header0.Substring(0, header0.Length - 1);
                    sw.WriteLine(header0);
                }
            }

            if (RunningGUI)
            {
                Utils.Utils.Trigger("DataParsingFinished", new object[] { paramNames });
            }

            int totalBayesParamCount = 0;

            if (string.IsNullOrEmpty(parameterFiles))
            {
                ConsoleWrapper.WriteLine("Sampling parameters...");

                double[][] MLEstimates = new double[dependencies.Length][];
                bool needMCMC = false;
                bool mlPerformed = false;

                for (int i = 0; i < dependencies.Length; i++)
                {
                    if (dependencies[i].Length > 1 || (dependencies[i].Length == 1 && dependencies[i][0].Type != CharacterDependency.Types.Independent))
                    {
                        ConsoleWrapper.Write("Jointly estimating parameters for characters ");
                        for (int j = 0; j < dependencies[i].Length; j++)
                        {
                            switch (dependencies[i][j].Type)
                            {
                                case CharacterDependency.Types.Independent:
                                case CharacterDependency.Types.Conditioned:
                                    ConsoleWrapper.Write(dependencies[i][j].InputDependencyName);
                                    if (j < dependencies[i].Length - 1)
                                    {
                                        ConsoleWrapper.Write(", ");
                                    }
                                    break;
                            }
                        }
                        ConsoleWrapper.WriteLine();
                    }
                    else
                    {
                        ConsoleWrapper.WriteLine("Estimating parameters for character " + dependencies[i][0].InputDependencyName);
                    }

                    MLEstimates[i] = Utils.Likelihoods.EstimateParameters(MLStrategies, data, dependencies[i], rates, pi, meanLikModel, mainRandom, out bool mcmcRequired, out bool thisMlPerformed);
                    needMCMC |= mcmcRequired;
                    mlPerformed |= thisMlPerformed;
                }

                int totalMlParamCount = 0;

                for (int i = 0; i < dependencies.Length; i++)
                {
                    (int mlParameterCount, int bayesParameterCount, List<List<Parameter>> ratesToEstimate, List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate) parametersToEstimate = Utils.Utils.GetParametersToEstimate(dependencies[i], rates, pi);
                    totalMlParamCount += parametersToEstimate.mlParameterCount;
                    totalBayesParamCount += parametersToEstimate.bayesParameterCount;
                }


                if (totalMlParamCount == 0 && totalBayesParamCount == 0)
                {
                    double currLogL = Utils.Likelihoods.ComputeAllLikelihoods(meanLikModel, data, dependencies, pi, rates);
                    ConsoleWrapper.WriteLine("Current ln-likelihood: {0}", currLogL.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    ConsoleWrapper.WriteLine();
                    using (StreamWriter sw = new StreamWriter(outputPrefix + ".modelstat.txt"))
                    {
                        sw.WriteLine("Current ln-likelihood: {0}", currLogL.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
                else if (totalMlParamCount > 0)
                {
                    double ml = Utils.Likelihoods.ComputeAllLikelihoods(meanLikModel, data, dependencies, pi, rates);
                    double aic = 2 * (totalMlParamCount + totalBayesParamCount) - 2 * ml;
                    double aicc = aic + (double)(2 * (totalMlParamCount + totalBayesParamCount) * (totalMlParamCount + totalBayesParamCount) + 2 * (totalMlParamCount + totalBayesParamCount)) / (meanLikModel.NamedBranches.Count * inputData.States.Length - (totalMlParamCount + totalBayesParamCount) - 1);
                    double bic = Math.Log(meanLikModel.NamedBranches.Count * inputData.States.Length) * (totalMlParamCount + totalBayesParamCount) - 2 * ml;

                    ConsoleWrapper.WriteLine("ln-Maximum likelihood: {0}", ml.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    ConsoleWrapper.WriteLine("AIC: {0}", aic.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    ConsoleWrapper.WriteLine("AICc: {0}", aicc.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    ConsoleWrapper.WriteLine("BIC: {0}", bic.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    ConsoleWrapper.WriteLine();

                    using (StreamWriter sw = new StreamWriter(outputPrefix + ".modelstat.txt"))
                    {
                        sw.WriteLine("ln-Maximum likelihood: {0}", ml.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        sw.WriteLine("AIC: {0}", aic.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        sw.WriteLine("AICc: {0}", aicc.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        sw.WriteLine("BIC: {0}", bic.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }

                    if (RunningGUI)
                    {
                        Utils.Utils.Trigger("AllMLFinished", new object[] { ml, aic, aicc, bic });
                    }
                }


                if (mlPerformed)
                {

                    ConsoleWrapper.WriteLine("Maximum-likelihood estimates:");

                    for (int i = 0; i < data.States.Length; i++)
                    {
                        string oldInputName = "-1";

                        for (int j = 0; j < dependencies.Length; j++)
                        {
                            for (int k = 0; k < dependencies[j].Length; k++)
                            {
                                if (dependencies[j][k].Index == i)
                                {
                                    oldInputName = dependencies[j][k].InputDependencyName;
                                }
                            }
                        }

                        ConsoleWrapper.WriteLine();
                        ConsoleWrapper.WriteLine("\tCharacter {0}:", oldInputName);
                        ConsoleWrapper.WriteLine();
                        ConsoleWrapper.WriteLine("\t\tPi:");
                        foreach (KeyValuePair<string, Parameter> kvp in pi[i])
                        {
                            ConsoleWrapper.WriteLine("\t\t\t" + kvp.Key + ": " + kvp.Value.Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                        }
                        ConsoleWrapper.WriteLine();
                        ConsoleWrapper.WriteLine("\t\tRates:");
                        ConsoleWrapper.Write("\t\t\t         ");
                        for (int j = 0; j < data.States[i].Length; j++)
                        {
                            ConsoleWrapper.Write(data.States[i][j].Pad(7, Extensions.PadType.Center));
                        }
                        ConsoleWrapper.WriteLine();
                        ConsoleWrapper.WriteLine("\t\t\t" + new string('-', (data.States[i].Length + 1) * 7 + 2));

                        for (int j = 0; j < data.States[i].Length; j++)
                        {
                            ConsoleWrapper.Write("\t\t\t");
                            ConsoleWrapper.Write(data.States[i][j].Pad(7, Extensions.PadType.Right) + " |");

                            for (int k = 0; k < data.States[i].Length; k++)
                            {
                                if (j == k)
                                {
                                    ConsoleWrapper.Write("-".Pad(7, Extensions.PadType.Center));
                                }
                                else
                                {
                                    ConsoleWrapper.Write(rates[i][data.States[i][j] + ">" + data.States[i][k]].Value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture).Pad(7, Extensions.PadType.Center));
                                }
                            }


                            ConsoleWrapper.WriteLine();
                        }
                    }
                }

                for (int i = 0; i < parameters.Length; i++)
                {
                    parameters[i] = MLEstimates.DeepClone();
                    treeSamples[i] = mainRandom.Next(trees.Count);
                }

                if (needMCMC)
                {
                    if (RunningGUI)
                    {
                        Utils.Utils.Trigger("BayesianStarted", new object[] { });
                    }

                    double[][] stepSizeMultipliers = new double[dependencies.Length][];

                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("Performing MCMC sampling...");
                    ConsoleWrapper.WriteLine();

                    for (int i = 0; i < dependencies.Length; i++)
                    {
                        if (dependencies[i].Length > 1 || (dependencies[i].Length == 1 && dependencies[i][0].Type != CharacterDependency.Types.Independent))
                        {
                            ConsoleWrapper.Write("Jointly sampling parameters for characters ");
                            for (int j = 0; j < dependencies[i].Length; j++)
                            {
                                switch (dependencies[i][j].Type)
                                {
                                    case CharacterDependency.Types.Independent:
                                    case CharacterDependency.Types.Conditioned:
                                        ConsoleWrapper.Write(dependencies[i][j].InputDependencyName);
                                        if (j < dependencies[i].Length - 1)
                                        {
                                            ConsoleWrapper.Write(", ");
                                        }
                                        break;
                                }
                            }
                            ConsoleWrapper.WriteLine();
                        }
                        else
                        {
                            ConsoleWrapper.WriteLine("Sampling parameters for character " + dependencies[i][0].InputDependencyName);
                        }

                        ((int, double[])[], List<List<Parameter>>[], List<(double remainingPi, List<Parameter> pis)>[], (int, double[][])[], CharacterDependency[][][], Dictionary<string, Parameter>[][], Dictionary<string, Parameter>[][], double[], double) mcmc;

                        if (RunningGUI)
                        {
                            Utils.Utils.Trigger("BayesianSetStarted", new object[] { i, numRuns });
                        }

                        mcmc = MCMC.SamplePosterior(dependencies, likModels, meanLikModel, rates, pi, i, (vars, chainId, ratesToEstimateByChain, pisToEstimateByChain, ratesByChain, pisByChain, dependenciesByChain, tree) =>
                        {
                            int varInd = 0;

                            for (int j = 0; j < ratesToEstimateByChain[chainId].Count; j++)
                            {
                                for (int k = 0; k < ratesToEstimateByChain[chainId][j].Count; k++)
                                {
                                    ratesToEstimateByChain[chainId][j][k].Value = vars[varInd][0];
                                    varInd++;
                                }
                            }

                            for (int j = 0; j < pisToEstimateByChain[chainId].Count; j++)
                            {
                                for (int k = 0; k < pisToEstimateByChain[chainId][j].pis.Count; k++)
                                {
                                    pisToEstimateByChain[chainId][j].pis[k].Value = vars[varInd][k];
                                }
                                varInd++;
                            }

                            return Likelihoods.ComputeAllLikelihoods(tree, data, dependenciesByChain[chainId], pisByChain[chainId], ratesByChain[chainId]);
                        }, mainRandom, outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".paramNames.txt", outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : ""), numRuns, numChains, numSim, runUnderPrior, inputStepSizeMultipliers);

                        (int, double[])[] mcmcSamples = mcmc.Item1;

                        stepSizeMultipliers[i] = mcmc.Item8;

                        for (int j = 0; j < parameters.Length; j++)
                        {
                            treeSamples[j] = mcmcSamples[j].Item1;
                            for (int k = 0; k < parameters[j][i].Length; k++)
                            {
                                if (!double.IsNaN(mcmcSamples[j].Item2[k]))
                                {
                                    parameters[j][i][k] = mcmcSamples[j].Item2[k];
                                }
                            }
                        }

                        if (ExitAfterMCMC)
                        {
                            mcmcSampleCount = mcmc.Item9;
                            return 0;
                        }

                        if (RunningGUI)
                        {
                            Utils.Utils.Trigger("BayesianSetFinished", new object[] { i });
                        }
                    }


                    if (RunningGUI)
                    {
                        Utils.Utils.Trigger("BayesianFinished", new object[] { parameters });
                    }


                    if (steppingStone)
                    {
                        if (RunningGUI)
                        {
                            Utils.Utils.Trigger("SteppingStoneStarted", new object[] { steppingStoneEstimateStepSize });
                        }

                        ConsoleWrapper.WriteLine();
                        ConsoleWrapper.WriteLine("Performing stepping-stone analysis...");
                        ConsoleWrapper.WriteLine();

                        double finalLogMarginalLikelihood = 0;

                        using (StreamWriter marginalLikelihoodSW = new StreamWriter(outputPrefix + ".marginal.likelihood.txt"))
                        {
                            for (int i = 0; i < dependencies.Length; i++)
                            {
                                if (dependencies.Length > 1)
                                {
                                    marginalLikelihoodSW.WriteLine("Set " + i.ToString());
                                }

                                if (RunningGUI)
                                {
                                    Utils.Utils.Trigger("SteppingStoneSetStarted", new object[] { i, steppingStoneSteps });
                                }

                                if (dependencies[i].Length > 1 || (dependencies[i].Length == 1 && dependencies[i][0].Type != CharacterDependency.Types.Independent))
                                {
                                    ConsoleWrapper.Write("Computing marginal likelihood for characters ");
                                    if (dependencies.Length > 1)
                                    {
                                        marginalLikelihoodSW.Write("\t");
                                    }
                                    marginalLikelihoodSW.Write("Characters ");
                                    for (int j = 0; j < dependencies[i].Length; j++)
                                    {
                                        switch (dependencies[i][j].Type)
                                        {
                                            case CharacterDependency.Types.Independent:
                                            case CharacterDependency.Types.Conditioned:
                                                ConsoleWrapper.Write(dependencies[i][j].InputDependencyName);
                                                marginalLikelihoodSW.Write(dependencies[i][j].InputDependencyName);
                                                if (j < dependencies[i].Length - 1)
                                                {
                                                    ConsoleWrapper.Write(", ");
                                                    marginalLikelihoodSW.Write(", ");
                                                }
                                                break;
                                        }
                                    }
                                    ConsoleWrapper.WriteLine();
                                    marginalLikelihoodSW.WriteLine();
                                }
                                else
                                {
                                    ConsoleWrapper.WriteLine("Computing marginal likelihood for character " + dependencies[i][0].InputDependencyName);

                                    if (dependencies.Length > 1)
                                    {
                                        marginalLikelihoodSW.Write("\t");
                                    }
                                    marginalLikelihoodSW.WriteLine("Character " + dependencies[i][0].InputDependencyName);
                                }

                                if (!steppingStoneEstimateStepSize)
                                {
                                    MCMC.estimateStepSize = false;
                                }
                                else
                                {
                                    MCMC.estimateStepSize = true;
                                }

                                double[] beta = new double[steppingStoneSteps + 1];
                                double[] rSS = new double[steppingStoneSteps];

                                for (int k = 0; k <= steppingStoneSteps; k++)
                                {
                                    beta[k] = Math.Pow((double)k / (double)steppingStoneSteps, 1.0 / steppingStoneShape);
                                }

                                for (int step = 1; step <= steppingStoneSteps; step++)
                                {
                                    if (RunningGUI)
                                    {
                                        Utils.Utils.Trigger("SteppingStoneStepStarted", new object[] { i, numRuns, step });
                                    }

                                    ConsoleWrapper.WriteLine("Step {0}/{1}...", step, steppingStoneSteps);
                                    ConsoleWrapper.WriteLine();

                                    ((int, double[])[], List<List<Parameter>>[], List<(double remainingPi, List<Parameter> pis)>[], (int, double[][])[], CharacterDependency[][][], Dictionary<string, Parameter>[][], Dictionary<string, Parameter>[][], double[], double) mcmcSamples = MCMC.SamplePosterior(dependencies, likModels, meanLikModel, rates, pi, i, (vars, chainId, ratesToEstimateByChain, pisToEstimateByChain, ratesByChain, pisByChain, dependenciesByChain, likModel) =>
                                    {
                                        int varInd = 0;

                                        for (int j = 0; j < ratesToEstimateByChain[chainId].Count; j++)
                                        {
                                            for (int k = 0; k < ratesToEstimateByChain[chainId][j].Count; k++)
                                            {
                                                ratesToEstimateByChain[chainId][j][k].Value = vars[varInd][0];
                                                varInd++;
                                            }
                                        }

                                        for (int j = 0; j < pisToEstimateByChain[chainId].Count; j++)
                                        {
                                            for (int k = 0; k < pisToEstimateByChain[chainId][j].pis.Count; k++)
                                            {
                                                pisToEstimateByChain[chainId][j].pis[k].Value = vars[varInd][k];
                                            }
                                            varInd++;
                                        }

                                        return Likelihoods.ComputeAllLikelihoods(likModel, data, dependenciesByChain[chainId], pisByChain[chainId], ratesByChain[chainId]) * beta[step - 1];
                                    }, mainRandom, null, outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".ss" + step.ToString(), numRuns, numChains, steppingStoneSamples, beta[step - 1] == 0, stepSizeMultipliers[i]);

                                    double[] likelihoodSamples = new double[steppingStoneSamples];

                                    for (int l = 0; l < steppingStoneSamples; l++)
                                    {
                                        int varInd = 0;

                                        for (int j = 0; j < mcmcSamples.Item2[0].Count; j++)
                                        {
                                            for (int k = 0; k < mcmcSamples.Item2[0][j].Count; k++)
                                            {
                                                mcmcSamples.Item2[0][j][k].Value = mcmcSamples.Item4[l].Item2[varInd][0];
                                                varInd++;
                                            }
                                        }

                                        for (int j = 0; j < mcmcSamples.Item3[0].Count; j++)
                                        {
                                            for (int k = 0; k < mcmcSamples.Item3[0][j].pis.Count; k++)
                                            {
                                                mcmcSamples.Item3[0][j].pis[k].Value = mcmcSamples.Item4[l].Item2[varInd][k];
                                            }
                                            varInd++;
                                        }

                                        likelihoodSamples[l] = Likelihoods.ComputeAllLikelihoods(likModels[mcmcSamples.Item4[l].Item1], data, mcmcSamples.Item5[0], mcmcSamples.Item6[0], mcmcSamples.Item7[0]);
                                    }

                                    File.WriteAllLines(outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".ss" + step.ToString() + ".samples.log", from el in likelihoodSamples select el.ToString(System.Globalization.CultureInfo.InvariantCulture));

                                    double maxLikelihood = likelihoodSamples.Max();

                                    rSS[step - 1] = maxLikelihood * (beta[step] - beta[step - 1]) + Math.Log((from el in likelihoodSamples select Math.Exp((el - maxLikelihood) * (beta[step] - beta[step - 1]))).Sum() / steppingStoneSamples);

                                    ConsoleWrapper.WriteLine();
                                    ConsoleWrapper.WriteLine("Contribution of step {0}: {1}", step, rSS[step - 1]);
                                    ConsoleWrapper.WriteLine();

                                    if (dependencies.Length > 1)
                                    {
                                        marginalLikelihoodSW.Write("\t");
                                    }

                                    marginalLikelihoodSW.WriteLine("\tContribution of step {0}: {1}", step, rSS[step - 1]);

                                    if (RunningGUI)
                                    {
                                        Utils.Utils.Trigger("SteppingStoneStepFinished", new object[] { i, step - 1, rSS[step - 1] });
                                    }
                                }


                                double logMarginalLikelihood = rSS.Sum();

                                if (RunningGUI)
                                {
                                    Utils.Utils.Trigger("SteppingStoneSetFinished", new object[] { i, logMarginalLikelihood });
                                }

                                finalLogMarginalLikelihood += logMarginalLikelihood;

                                if (dependencies[i].Length > 1 || (dependencies[i].Length == 1 && dependencies[i][0].Type != CharacterDependency.Types.Independent))
                                {
                                    ConsoleWrapper.Write("Ln-marginal likelihood for characters ");
                                    marginalLikelihoodSW.Write("Ln-marginal likelihood for characters ");
                                    for (int j = 0; j < dependencies[i].Length; j++)
                                    {
                                        switch (dependencies[i][j].Type)
                                        {
                                            case CharacterDependency.Types.Independent:
                                            case CharacterDependency.Types.Conditioned:
                                                ConsoleWrapper.Write(dependencies[i][j].InputDependencyName);
                                                marginalLikelihoodSW.Write(dependencies[i][j].InputDependencyName);
                                                if (j < dependencies[i].Length - 1)
                                                {
                                                    ConsoleWrapper.Write(", ");
                                                    marginalLikelihoodSW.Write(", ");
                                                }
                                                break;
                                        }
                                    }
                                    ConsoleWrapper.WriteLine(": " + logMarginalLikelihood.ToString(System.Globalization.CultureInfo.InvariantCulture));
                                    ConsoleWrapper.WriteLine();

                                    marginalLikelihoodSW.WriteLine(": " + logMarginalLikelihood.ToString(System.Globalization.CultureInfo.InvariantCulture));
                                    marginalLikelihoodSW.WriteLine();
                                }
                                else
                                {
                                    ConsoleWrapper.WriteLine("Ln-marginal likelihood for character " + dependencies[i][0].InputDependencyName + ": " + logMarginalLikelihood.ToString(System.Globalization.CultureInfo.InvariantCulture));
                                    ConsoleWrapper.WriteLine();
                                    marginalLikelihoodSW.WriteLine("Ln-marginal likelihood for character " + dependencies[i][0].InputDependencyName + ": " + logMarginalLikelihood.ToString(System.Globalization.CultureInfo.InvariantCulture));
                                    marginalLikelihoodSW.WriteLine();
                                }
                            }

                            ConsoleWrapper.WriteLine();
                            ConsoleWrapper.WriteLine("Overall ln-marginal likelihood: " + finalLogMarginalLikelihood.ToString(System.Globalization.CultureInfo.InvariantCulture));
                            ConsoleWrapper.WriteLine();

                            marginalLikelihoodSW.WriteLine();
                            marginalLikelihoodSW.WriteLine("Overall ln-marginal likelihood: " + finalLogMarginalLikelihood.ToString(System.Globalization.CultureInfo.InvariantCulture));

                            if (RunningGUI)
                            {
                                Utils.Utils.Trigger("SteppingStoneFinished", new object[] { finalLogMarginalLikelihood });
                            }
                        }
                    }
                }
            }
            else
            {
                for (int n = 0; n < numSim; n++)
                {
                    parameters[n] = new double[dependencies.Length][];
                }

                for (int i = 0; i < dependencies.Length; i++)
                {
                    string parameterFile = parameterFiles.Split(',')[i];

                    Console.WriteLine("Reading parameters for set {0} from file {1}...", i, parameterFile);

                    using (StreamReader sr = new StreamReader(parameterFile))
                    {
                        string[] line = sr.ReadLine().Split('\t');
                        int skip = 0;

                        while (!int.TryParse(line[0], out int ignored) && !sr.EndOfStream)
                        {
                            line = sr.ReadLine().Split('\t');
                            skip++;
                        }

                        ConsoleWrapper.WriteLine("Skipping {0} lines.", skip);

                        int ind = 0;

                        while (!sr.EndOfStream && int.TryParse(line[0], out int ignored))
                        {
                            if (ind > 0)
                            {
                                line = sr.ReadLine().Split('\t');
                            }
                            if (ind >= numSim)
                            {
                                ConsoleWrapper.WriteLine();
                                ConsoleWrapper.WriteLine("Attention! The file {0} appears to contain more than {1} parameter samples!", parameterFile, numSim);
                                ConsoleWrapper.WriteLine();
                                return 1;
                            }

                            treeSamples[ind] = int.Parse(line[0]);
                            parameters[ind][i] = (from el in line.Skip(1) select double.Parse(el, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                            ind++;
                        }

                        if (ind < numSim)
                        {
                            ConsoleWrapper.WriteLine();
                            ConsoleWrapper.WriteLine("Attention! The file {0} appears to contain only {1} parameter samples!", parameterFile, ind);
                            ConsoleWrapper.WriteLine();
                            return 1;
                        }

                    }

                    Console.WriteLine("Done.");
                }
            }


            if (ExitAfterMCMC)
            {
                return 0;
            }

            double[][][] parameterPriors = new double[numSim][][];

            for (int k = 0; k < numSim; k++)
            {
                parameterPriors[k] = new double[dependencies.Length][];
            }

            for (int i = 0; i < dependencies.Length; i++)
            {
                var parametersToEstimate = Utils.Utils.GetParametersToEstimate(dependencies[i], rates, pi);

                for (int k = 0; k < numSim; k++)
                {
                    parameterPriors[k][i] = Utils.Utils.ParametersPriorSample(parametersToEstimate, mainRandom);
                }
            }

            for (int i = 0; i < dependencies.Length; i++)
            {
                using (StreamWriter sw = new StreamWriter(outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".params.txt", false, Encoding.UTF8))
                {
                    using (StreamWriter swPrior = new StreamWriter(outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".prior.params.txt", false, Encoding.UTF8))
                    {
                        (List<Parameter> rates, List<(Parameter, int, MultivariateDistribution)> pis) paramsToEstimate = Utils.Utils.ParametersToEstimateList(Utils.Utils.GetParametersToEstimate(dependencies[i], rates, pi), mainRandom);

                        string header0 = "T\t";

                        for (int j = 0; j < paramsToEstimate.rates.Count; j++)
                        {
                            string key = null;

                            for (int k = 0; k < dependencies[i].Length; k++)
                            {
                                if (dependencies[i][k].Type == CharacterDependency.Types.Independent)
                                {
                                    key = rates[dependencies[i][k].Index].GetKey(paramsToEstimate.rates[j]);
                                    if (!string.IsNullOrEmpty(key))
                                    {
                                        break;
                                    }
                                }
                            }

                            header0 += "r(" + key.Replace(">", " > ") + ")\t";
                        }

                        for (int j = 0; j < paramsToEstimate.pis.Count; j++)
                        {
                            string key = null;

                            for (int k = 0; k < dependencies[i].Length; k++)
                            {
                                if (dependencies[i][k].Type == CharacterDependency.Types.Independent)
                                {
                                    key = pi[dependencies[i][k].Index].GetKey(paramsToEstimate.pis[j].Item1);
                                    if (!string.IsNullOrEmpty(key))
                                    {
                                        break;
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(key))
                            {
                                header0 += "π(" + key + ")\t";
                            }
                            else
                            {
                                for (int k = 0; k < dependencies[i].Length; k++)
                                {
                                    if (dependencies[i][k].Type == CharacterDependency.Types.Conditioned)
                                    {
                                        key = dependencies[i][k].ConditionedProbabilities.GetKey(paramsToEstimate.pis[j].Item1);

                                        if (!string.IsNullOrEmpty(key))
                                        {
                                            string newKey = key.Substring(key.IndexOf(">") + 1) + " | " + key.Substring(0, key.IndexOf(">"));
                                            header0 += "p(" + newKey + ")\t";
                                            break;
                                        }
                                    }

                                }
                            }
                        }

                        header0 = header0.Substring(0, header0.Length - 1);
                        sw.WriteLine(header0);
                        swPrior.WriteLine(header0);

                        for (int k = 0; k < numSim; k++)
                        {
                            sw.Write(treeSamples[k].ToString() + "\t");
                            swPrior.Write(treeSamples[k].ToString() + "\t");

                            for (int j = 0; j < parameters[k][i].Length; j++)
                            {
                                sw.Write(parameters[k][i][j].ToString(System.Globalization.CultureInfo.InvariantCulture) + (j < parameters[k][i].Length - 1 ? "\t" : ""));
                                swPrior.Write(parameterPriors[k][i][j].ToString(System.Globalization.CultureInfo.InvariantCulture) + (j < parameterPriors[k][i].Length - 1 ? "\t" : ""));
                            }

                            sw.WriteLine();
                            swPrior.WriteLine();
                        }
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter(outputPrefix + ".mean.params.txt", false, Encoding.UTF8))
            {
                for (int i = 0; i < dependencies.Length; i++)
                {
                    (double[], string, object, int)[] samples = new (double[], string, object, int)[parameters[0][i].Length];

                    (List<Parameter> rates, List<(Parameter, int, MultivariateDistribution)> pis) paramsToEstimate = Utils.Utils.ParametersToEstimateList(Utils.Utils.GetParametersToEstimate(dependencies[i], rates, pi), mainRandom);

                    for (int j = 0; j < paramsToEstimate.rates.Count; j++)
                    {
                        string key = null;

                        for (int k = 0; k < dependencies[i].Length; k++)
                        {
                            if (dependencies[i][k].Type == CharacterDependency.Types.Independent)
                            {
                                key = rates[dependencies[i][k].Index].GetKey(paramsToEstimate.rates[j]);
                                if (!string.IsNullOrEmpty(key))
                                {
                                    break;
                                }
                            }
                        }

                        samples[j] = (new double[numSim], "r(" + key.Replace(">", " > ") + ")", paramsToEstimate.rates[j].PriorDistribution, 0);

                        for (int k = 0; k < numSim; k++)
                        {
                            samples[j].Item1[k] = parameters[k][i][j];
                        }
                    }

                    for (int j = 0; j < paramsToEstimate.pis.Count; j++)
                    {
                        string key = null;

                        for (int k = 0; k < dependencies[i].Length; k++)
                        {
                            if (dependencies[i][k].Type == CharacterDependency.Types.Independent)
                            {
                                key = pi[dependencies[i][k].Index].GetKey(paramsToEstimate.pis[j].Item1);
                                if (!string.IsNullOrEmpty(key))
                                {
                                    break;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(key))
                        {
                            samples[j + paramsToEstimate.rates.Count] = (new double[numSim], "π(" + key + ")", paramsToEstimate.pis[j].Item3, paramsToEstimate.pis[j].Item2);
                        }
                        else
                        {
                            for (int k = 0; k < dependencies[i].Length; k++)
                            {
                                if (dependencies[i][k].Type == CharacterDependency.Types.Conditioned)
                                {
                                    key = dependencies[i][k].ConditionedProbabilities.GetKey(paramsToEstimate.pis[j].Item1);

                                    if (!string.IsNullOrEmpty(key))
                                    {
                                        string newKey = key.Substring(key.IndexOf(">") + 1) + "|" + key.Substring(0, key.IndexOf(">"));
                                        samples[j + paramsToEstimate.rates.Count] = (new double[numSim], "p(" + newKey + ")", paramsToEstimate.pis[j].Item3, paramsToEstimate.pis[j].Item2);
                                    }
                                }

                            }
                        }

                        for (int k = 0; k < numSim; k++)
                        {
                            samples[j + paramsToEstimate.rates.Count].Item1[k] = parameters[k][i][j + paramsToEstimate.rates.Count];
                        }
                    }

                    if (totalBayesParamCount > 0 || !string.IsNullOrEmpty(parameterFiles))
                    {
                        Plotting.PlotHistograms(samples, binRule, plotWidth, plotHeight ?? (plotWidth * 0.7F), plotWidth / 25F, outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".params.pdf", new Plotting.Options() { FontFamily = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "OpenSans-Regular.ttf"), FontSize = (plotHeight ?? (plotWidth * 0.7F)) / 28F });
                    }

                    List<Parameter> onlyPisToEstimate = new List<Parameter>(from el in paramsToEstimate.pis select el.Item1);

                    sw.WriteLine("Set " + i.ToString() + ":");

                    for (int j = 0; j < dependencies[i].Length; j++)
                    {
                        sw.WriteLine("\tCharacter " + dependencies[i][j].Index.ToString() + ":");
                        if (dependencies[i][j].Type == CharacterDependency.Types.Independent)
                        {
                            sw.WriteLine("\t\tPi:");

                            for (int k = 0; k < data.States[dependencies[i][j].Index].Length; k++)
                            {
                                sw.Write("\t\t\tπ(" + data.States[dependencies[i][j].Index][k] + "):\t");
                                int ind = onlyPisToEstimate.IndexOf(pi[dependencies[i][j].Index][data.States[dependencies[i][j].Index][k]]);
                                if (ind >= 0)
                                {
                                    sw.WriteLine(samples[ind + paramsToEstimate.rates.Count].Item1.Average().ToString(System.Globalization.CultureInfo.InvariantCulture));
                                }
                                else
                                {
                                    sw.WriteLine(pi[dependencies[i][j].Index][data.States[dependencies[i][j].Index][k]].Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                                }
                            }
                            sw.WriteLine();
                            sw.WriteLine("\t\tRates:");

                            for (int k = 0; k < data.States[dependencies[i][j].Index].Length; k++)
                            {
                                for (int l = 0; l < data.States[dependencies[i][j].Index].Length; l++)
                                {
                                    if (k != l)
                                    {
                                        string state = data.States[dependencies[i][j].Index][k] + ">" + data.States[dependencies[i][j].Index][l];
                                        sw.Write("\t\t\tr(" + state.Replace(">", " > ") + "):\t");

                                        int ind = paramsToEstimate.rates.IndexOf(rates[dependencies[i][j].Index][state]);

                                        if (ind >= 0)
                                        {
                                            sw.WriteLine(samples[ind].Item1.Average().ToString(System.Globalization.CultureInfo.InvariantCulture));
                                        }
                                        else
                                        {
                                            if (rates[dependencies[i][j].Index][state].Action == Parameter.ParameterAction.Equal)
                                            {
                                                sw.WriteLine("Equal(" + rates[dependencies[i][j].Index].GetKey(rates[dependencies[i][j].Index][state].EqualParameter).Replace(">", " > ") + ")");
                                            }
                                            else
                                            {
                                                sw.WriteLine(rates[dependencies[i][j].Index][state].Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                                            }
                                        }
                                    }
                                }
                            }
                            sw.WriteLine();
                        }
                        else if (dependencies[i][j].Type == CharacterDependency.Types.Conditioned)
                        {
                            sw.WriteLine("\t\tProbs:");

                            foreach (KeyValuePair<string, Parameter> kvp in dependencies[i][j].ConditionedProbabilities)
                            {
                                string probName = kvp.Key.Substring(kvp.Key.IndexOf(">") + 1) + " | " + kvp.Key.Substring(0, kvp.Key.IndexOf(">"));
                                sw.Write("\t\t\tp(" + probName + "):\t");
                                int ind = onlyPisToEstimate.IndexOf(kvp.Value);
                                if (ind >= 0)
                                {
                                    sw.WriteLine(samples[ind + paramsToEstimate.rates.Count].Item1.Average().ToString(System.Globalization.CultureInfo.InvariantCulture));
                                }
                                else
                                {
                                    sw.WriteLine(kvp.Value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                                }
                            }
                            sw.WriteLine();
                        }
                    }
                }
            }

            ConsoleWrapper.WriteLine();

            double[][][][] priors = new double[dependencies.Length][][][];
            double[][][][] posteriors = new double[dependencies.Length][][][];      //[dependency][replicate][node][state]
            double[][][][] likelihoods = new double[dependencies.Length][][][];

            double[][][] meanPrior = new double[dependencies.Length][][];
            double[][][] meanPosterior = new double[dependencies.Length][][];   //[dependency][node][state]
            double[][][] meanLikelihood = new double[dependencies.Length][][];

            if (RunningGUI)
            {
                Utils.Utils.Trigger("StartComputingNodeProbs", new object[] { });
            }

            for (int i = 0; i < dependencies.Length; i++)
            {
                priors[i] = new double[numSim][][];
                posteriors[i] = new double[numSim][][];
                likelihoods[i] = new double[numSim][][];
                double[][] branchProbs = new double[numSim][];

                if (dependencies[i].Length > 1 || (dependencies[i].Length == 1 && dependencies[i][0].Type != CharacterDependency.Types.Independent))
                {
                    ConsoleWrapper.Write("Jointly computing priors and posteriors for characters ");
                    for (int j = 0; j < dependencies[i].Length; j++)
                    {
                        switch (dependencies[i][j].Type)
                        {
                            case CharacterDependency.Types.Independent:
                            case CharacterDependency.Types.Conditioned:
                                ConsoleWrapper.Write(dependencies[i][j].InputDependencyName);
                                if (j < dependencies[i].Length - 1)
                                {
                                    ConsoleWrapper.Write(", ");
                                }
                                break;
                        }
                    }
                    ConsoleWrapper.Write(": ");
                    int cursorPos = ConsoleWrapper.CursorLeft;

                    using (StreamWriter sw = new StreamWriter(outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".priors.log"))
                    {
                        using (StreamWriter sw2 = new StreamWriter(outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".posteriors.log"))
                        {
                            using (StreamWriter sw3 = new StreamWriter(outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".loglikelihoods.log"))
                            {
                                using (StreamWriter sw4 = new StreamWriter(outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".branchprobs.txt"))
                                {
                                    string[] allPossibleStates = Utils.Utils.GetAllPossibleStatesString(dependencies[i], data.States);

                                    StringBuilder header0 = new StringBuilder((2 * meanLikModel.NamedBranches.Count - 1) * (int)(Math.Ceiling(Math.Log10(2 * meanLikModel.NamedBranches.Count - 1)) + 1));
                                    StringBuilder header1 = new StringBuilder((2 * meanLikModel.NamedBranches.Count - 1) * allPossibleStates.Length * (allPossibleStates[0].Length + 1));

                                    for (int j = 0; j < 2 * meanLikModel.NamedBranches.Count - 1; j++)
                                    {
                                        for (int k = 0; k < allPossibleStates.Length; k++)
                                        {
                                            if (k == 0)
                                            {
                                                header0.Append(j.ToString());
                                            }

                                            header1.Append(allPossibleStates[k]);

                                            if (k < allPossibleStates.Length - 1 || j < 2 * meanLikModel.NamedBranches.Count - 2)
                                            {
                                                header0.Append("\t");
                                                header1.Append("\t");
                                            }
                                        }
                                    }



                                    sw.WriteLine(header0.ToString());
                                    sw.WriteLine(header1.ToString());
                                    sw2.WriteLine(header0.ToString());
                                    sw2.WriteLine(header1.ToString());
                                    sw3.WriteLine(header0.ToString());
                                    sw3.WriteLine(header1.ToString());

                                    Dictionary<string, Parameter>[][] pisByThread = new Dictionary<string, Parameter>[numThreads + 1][];
                                    Dictionary<string, Parameter>[][] ratesByThread = new Dictionary<string, Parameter>[numThreads + 1][];
                                    CharacterDependency[][] dependenciesIByThread = new CharacterDependency[numThreads + 1][];

                                    for (int j = 0; j < numThreads + 1; j++)
                                    {
                                        dependenciesIByThread[j] = new CharacterDependency[dependencies[i].Length];

                                        for (int k = 0; k < dependencies[i].Length; k++)
                                        {
                                            switch (dependencies[i][k].Type)
                                            {
                                                case CharacterDependency.Types.Independent:
                                                    dependenciesIByThread[j][k] = new CharacterDependency(dependencies[i][k].Index);
                                                    break;
                                                case CharacterDependency.Types.Dependent:
                                                case CharacterDependency.Types.Conditioned:
                                                    dependenciesIByThread[j][k] = new CharacterDependency(dependencies[i][k].Index, dependencies[i][k].Type, (int[])dependencies[i][k].Dependencies.Clone(), Parameter.CloneParameterDictionary(dependencies[i][k].ConditionedProbabilities));
                                                    break;
                                            }
                                        }

                                        pisByThread[j] = new Dictionary<string, Parameter>[pi.Length];

                                        for (int k = 0; k < pi.Length; k++)
                                        {
                                            pisByThread[j][k] = Parameter.CloneParameterDictionary(pi[k]);
                                        }

                                        ratesByThread[j] = new Dictionary<string, Parameter>[rates.Length];

                                        for (int k = 0; k < rates.Length; k++)
                                        {
                                            ratesByThread[j][k] = Parameter.CloneParameterDictionary(rates[k]);
                                        }
                                    }


                                    for (int j = 0; j < numSim; j++)
                                    {
                                        priors[i][j] = new double[likModels[treeSamples[j]].Parents.Length][];
                                        posteriors[i][j] = new double[likModels[treeSamples[j]].Parents.Length][];
                                        likelihoods[i][j] = new double[likModels[treeSamples[j]].Parents.Length][];
                                        for (int k = 0; k < likModels[treeSamples[j]].Parents.Length; k++)
                                        {
                                            priors[i][j][k] = new double[allPossibleStates.Length];
                                            posteriors[i][j][k] = new double[allPossibleStates.Length];
                                            likelihoods[i][j][k] = new double[allPossibleStates.Length];
                                        }
                                    }

                                    int[] indicesByThreadCount = Utils.Utils.RoundInts((from el in Utils.Utils.Range(0, numThreads) select (double)numSim / numThreads).ToArray());
                                    int[][] indicesByThread = new int[numThreads][];

                                    int simInd = 0;

                                    for (int j = 0; j < numThreads; j++)
                                    {
                                        indicesByThread[j] = new int[indicesByThreadCount[j]];

                                        for (int k = 0; k < indicesByThreadCount[j]; k++)
                                        {
                                            indicesByThread[j][k] = simInd;
                                            simInd++;
                                        }
                                    }

                                    EventWaitHandle notifyProgressThread = new EventWaitHandle(false, EventResetMode.ManualReset);
                                    EventWaitHandle abortProgressThread = new EventWaitHandle(false, EventResetMode.ManualReset);

                                    object progressLock = new object();

                                    int progress = 0;

                                    Thread progressThread = new Thread(() =>
                                    {
                                        while (!abortProgressThread.WaitOne(0))
                                        {
                                            int ind = EventWaitHandle.WaitAny(new WaitHandle[] { notifyProgressThread, abortProgressThread });
                                            if (ind == 0)
                                            {
                                                lock (progressLock)
                                                {
                                                    notifyProgressThread.Reset();
                                                    progress++;
                                                }

                                                if (progress % Math.Max(1, (numSim / 100)) == 0)
                                                {
                                                    ConsoleWrapper.CursorLeft = cursorPos;
                                                    ConsoleWrapper.Write("{0}   ", ((double)progress / numSim).ToString("0%", System.Globalization.CultureInfo.InvariantCulture));
                                                    ConsoleWrapper.CursorLeft = ConsoleWrapper.CursorLeft - 3;

                                                    if (RunningGUI)
                                                    {
                                                        Utils.Utils.Trigger("NodeStateProbProgress", new object[] { (double)progress / numSim });
                                                    }
                                                }
                                            }
                                        }

                                        if (RunningGUI)
                                        {
                                            Utils.Utils.Trigger("NodeStateProbSetFinished", new object[] { });
                                        }
                                    });

                                    progressThread.Start();

                                    Thread[] threads = new Thread[numThreads];

                                    for (int threadInd = 0; threadInd < numThreads; threadInd++)
                                    {
                                        threads[threadInd] = new Thread((threadIndexObj) =>
                                        {
                                            int threadIndex = (int)threadIndexObj;

                                            for (int simIndex = 0; simIndex < indicesByThread[threadIndex].Length; simIndex++)
                                            {
                                                int j = indicesByThread[threadIndex][simIndex];

                                                Likelihoods.ComputeAndSampleJointPriors(likModels[treeSamples[j]], data.States, pisByThread[threadIndex], ratesByThread[threadIndex], parameterPriors[j][i], dependenciesIByThread[threadIndex], priors[i][j], false, mainRandom);

                                                Likelihoods.ComputeJointLikelihoods(likModels[treeSamples[j]], data, dependenciesIByThread[threadIndex], pisByThread[threadIndex], ratesByThread[threadIndex], parameters[j][i], likelihoods[i][j], false);

                                                Likelihoods.ComputeAndSampleJointPosteriors(likModels[treeSamples[j]], data.States, pisByThread[threadIndex], ratesByThread[threadIndex], parameters[j][i], dependenciesIByThread[threadIndex], likelihoods[i][j], posteriors[i][j], false, out branchProbs[j], mainRandom);

                                                lock (progressLock)
                                                {
                                                    notifyProgressThread.Set();
                                                }
                                            }
                                        });

                                        threads[threadInd].Start(threadInd);

                                    }

                                    for (int j = 0; j < threads.Length; j++)
                                    {
                                        threads[j].Join();
                                    }

                                    abortProgressThread.Set();
                                    progressThread.Join();

                                    for (int j = 0; j < numSim; j++)
                                    {
                                        for (int k = 0; k < priors[i][j].Length; k++)
                                        {
                                            for (int l = 0; l < priors[i][j][k].Length; l++)
                                            {
                                                sw.Write(priors[i][j][k][l].ToString(System.Globalization.CultureInfo.InvariantCulture));

                                                if (k < priors[i][j].Length - 1 || l < priors[i][j][k].Length - 1)
                                                {
                                                    sw.Write("\t");
                                                }
                                            }
                                        }
                                        sw.WriteLine();

                                        for (int k = 0; k < posteriors[i][j].Length; k++)
                                        {
                                            for (int l = 0; l < posteriors[i][j][k].Length; l++)
                                            {
                                                sw2.Write(posteriors[i][j][k][l].ToString(System.Globalization.CultureInfo.InvariantCulture));

                                                if (k < posteriors[i][j].Length - 1 || l < posteriors[i][j][k].Length - 1)
                                                {
                                                    sw2.Write("\t");
                                                }
                                            }
                                        }
                                        sw2.WriteLine();


                                        for (int k = 0; k < likelihoods[i][j].Length; k++)
                                        {
                                            for (int l = 0; l < likelihoods[i][j][k].Length; l++)
                                            {
                                                sw3.Write(likelihoods[i][j][k][l].ToString(System.Globalization.CultureInfo.InvariantCulture));

                                                if (k < likelihoods[i][j].Length - 1 || l < likelihoods[i][j][k].Length - 1)
                                                {
                                                    sw3.Write("\t");
                                                }
                                            }
                                        }
                                        sw3.WriteLine();

                                        for (int k = 0; k < branchProbs[j].Length; k++)
                                        {
                                            sw4.Write(branchProbs[j][k].ToString(System.Globalization.CultureInfo.InvariantCulture));

                                            if (k < branchProbs[j].Length - 1)
                                            {
                                                sw4.Write("\t");
                                            }
                                        }
                                        sw4.WriteLine();
                                    }

                                    ConsoleWrapper.CursorLeft = cursorPos;
                                    ConsoleWrapper.WriteLine("Done.   ");
                                }
                            }
                        }
                    }

                }
                else
                {
                    ConsoleWrapper.Write("Computing priors and posteriors for character " + dependencies[i][0].InputDependencyName + ": ");
                    int cursorPos = ConsoleWrapper.CursorLeft;

                    using (StreamWriter sw = new StreamWriter(outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".priors.log"))
                    {
                        using (StreamWriter sw2 = new StreamWriter(outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".posteriors.log"))
                        {
                            using (StreamWriter sw3 = new StreamWriter(outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".loglikelihoods.log"))
                            {
                                using (StreamWriter sw4 = new StreamWriter(outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".branchprobs.txt"))
                                {
                                    string[] allPossibleStates = Utils.Utils.GetAllPossibleStatesString(dependencies[i], data.States);
                                    StringBuilder header0 = new StringBuilder((2 * meanLikModel.NamedBranches.Count - 1) * (int)(Math.Ceiling(Math.Log10(2 * meanLikModel.NamedBranches.Count - 1)) + 1));
                                    StringBuilder header1 = new StringBuilder((2 * meanLikModel.NamedBranches.Count - 1) * allPossibleStates.Length * (allPossibleStates[0].Length + 1));

                                    for (int j = 0; j < 2 * meanLikModel.NamedBranches.Count - 1; j++)
                                    {
                                        for (int k = 0; k < allPossibleStates.Length; k++)
                                        {
                                            if (k == 0)
                                            {
                                                header0.Append(j.ToString());
                                            }

                                            header1.Append(allPossibleStates[k]);

                                            if (k < allPossibleStates.Length - 1 || j < 2 * meanLikModel.NamedBranches.Count - 2)
                                            {
                                                header0.Append("\t");
                                                header1.Append("\t");
                                            }
                                        }
                                    }



                                    sw.WriteLine(header0.ToString());
                                    sw.WriteLine(header1.ToString());
                                    sw2.WriteLine(header0.ToString());
                                    sw2.WriteLine(header1.ToString());
                                    sw3.WriteLine(header0.ToString());
                                    sw3.WriteLine(header1.ToString());

                                    for (int j = 0; j < numSim; j++)
                                    {
                                        priors[i][j] = Likelihoods.ComputeAndSamplePriors(likModels[treeSamples[j]], data.States[dependencies[i][0].Index], pi, rates, parameterPriors[j][i], dependencies[i], mainRandom);

                                        for (int k = 0; k < priors[i][j].Length; k++)
                                        {
                                            for (int l = 0; l < priors[i][j][k].Length; l++)
                                            {
                                                sw.Write(priors[i][j][k][l].ToString(System.Globalization.CultureInfo.InvariantCulture));

                                                if (k < priors[i][j].Length - 1 || l < priors[i][j][k].Length - 1)
                                                {
                                                    sw.Write("\t");
                                                }
                                            }
                                        }
                                        sw.WriteLine();

                                        Likelihoods.ComputeLikelihoods(likModels[treeSamples[j]], (data.States[dependencies[i][0].Index], data.Data.Fold(dependencies[i][0].Index)), pi[dependencies[i][0].Index], rates[dependencies[i][0].Index], out likelihoods[i][j]);

                                        posteriors[i][j] = Likelihoods.ComputeAndSamplePosteriors(likModels[treeSamples[j]], data.States[dependencies[i][0].Index], pi, rates, parameters[j][i], dependencies[i], likelihoods[i][j], out branchProbs[j], mainRandom);

                                        for (int k = 0; k < posteriors[i][j].Length; k++)
                                        {
                                            for (int l = 0; l < posteriors[i][j][k].Length; l++)
                                            {
                                                sw2.Write(posteriors[i][j][k][l].ToString(System.Globalization.CultureInfo.InvariantCulture));

                                                if (k < posteriors[i][j].Length - 1 || l < posteriors[i][j][k].Length - 1)
                                                {
                                                    sw2.Write("\t");
                                                }
                                            }
                                        }
                                        sw2.WriteLine();


                                        for (int k = 0; k < likelihoods[i][j].Length; k++)
                                        {
                                            for (int l = 0; l < likelihoods[i][j][k].Length; l++)
                                            {
                                                sw3.Write(likelihoods[i][j][k][l].ToString(System.Globalization.CultureInfo.InvariantCulture));

                                                if (k < likelihoods[i][j].Length - 1 || l < likelihoods[i][j][k].Length - 1)
                                                {
                                                    sw3.Write("\t");
                                                }
                                            }
                                        }
                                        sw3.WriteLine();

                                        for (int k = 0; k < branchProbs[j].Length; k++)
                                        {
                                            sw4.Write(branchProbs[j][k].ToString(System.Globalization.CultureInfo.InvariantCulture));

                                            if (k < branchProbs[j].Length - 1)
                                            {
                                                sw4.Write("\t");
                                            }
                                        }
                                        sw4.WriteLine();

                                        if (j % Math.Max(1, (numSim / 100)) == 0)
                                        {
                                            ConsoleWrapper.CursorLeft = cursorPos;
                                            ConsoleWrapper.Write("{0}   ", ((double)(j + 1) / numSim).ToString("0%", System.Globalization.CultureInfo.InvariantCulture));
                                            ConsoleWrapper.CursorLeft = ConsoleWrapper.CursorLeft - 3;

                                            if (RunningGUI)
                                            {
                                                Utils.Utils.Trigger("NodeStateProbProgress", new object[] { (double)(j + 1) / numSim });
                                            }
                                        }

                                        if (j == numSim - 1)
                                        {
                                            if (RunningGUI)
                                            {
                                                Utils.Utils.Trigger("NodeStateProbSetFinished", new object[] { });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    ConsoleWrapper.CursorLeft = cursorPos;
                    ConsoleWrapper.WriteLine("Done.   ");
                }

                double[] meanBranchProbs = (from el in Utils.Utils.Range(0, meanLikModel.Parents.Length) select (from el2 in Utils.Utils.Range(0, branchProbs.Length) where meanNodeCorresp[el][treeSamples[el2]] >= 0 select branchProbs[el2][meanNodeCorresp[el][treeSamples[el2]]]).Average()).ToArray();
                double[] minBranchProbs = (from el in Utils.Utils.Range(0, meanLikModel.Parents.Length) select (from el2 in Utils.Utils.Range(0, branchProbs.Length) where meanNodeCorresp[el][treeSamples[el2]] >= 0 select branchProbs[el2][meanNodeCorresp[el][treeSamples[el2]]]).Min()).ToArray();
                double[] maxBranchProbs = (from el in Utils.Utils.Range(0, meanLikModel.Parents.Length) select (from el2 in Utils.Utils.Range(0, branchProbs.Length) where meanNodeCorresp[el][treeSamples[el2]] >= 0 select branchProbs[el2][meanNodeCorresp[el][treeSamples[el2]]]).Max()).ToArray();

                float realPlotHeight = plotHeight ?? (12 * meanLikModel.NamedBranches.Count * 1.4F + plotWidth / 25F);

                Utils.Plotting.PlotBranchProbs(meanBranchProbs, minBranchProbs, maxBranchProbs, new Plotting.Options(), outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".branchprobs.pdf");
                Utils.Plotting.PlotBranchProbsTree(plotWidth, realPlotHeight, meanTree, meanBranchProbs, minBranchProbs, new Plotting.Options() { FontSize = realPlotHeight / (meanLikModel.NamedBranches.Count * 1.4F) }, outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".branchprobs.tree.pdf");

                double minBranchProb = (from el in branchProbs select el.Min()).Min();

                if (minBranchProb < 1e-6)
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("Attention! The lowest branch probability is {0} (< 1e-6): the simulation step may take a long time!", minBranchProb.ToString(2, false));
                    ConsoleWrapper.WriteLine("You may want to double-check your model.");
                    ConsoleWrapper.WriteLine();
                }

                meanPrior[i] = new double[meanLikModel.BranchLengths.Length][];
                meanPosterior[i] = new double[meanLikModel.BranchLengths.Length][];
                meanLikelihood[i] = new double[meanLikModel.BranchLengths.Length][];

                int stateCount = Utils.Utils.GetAllPossibleStatesString(dependencies[i], data.States).Length;

                for (int k = 0; k < meanPrior[i].Length; k++)
                {
                    meanPrior[i][k] = new double[stateCount];
                    meanPosterior[i][k] = new double[stateCount];
                    meanLikelihood[i][k] = new double[stateCount];
                }


                for (int k = 0; k < meanLikModel.BranchLengths.Length; k++)
                {
                    for (int j = 0; j < numSim; j++)
                    {
                        if (meanNodeCorresp[k][treeSamples[j]] >= 0)
                        {
                            for (int l = 0; l < stateCount; l++)
                            {
                                meanPrior[i][k][l] += priors[i][j][meanNodeCorresp[k][treeSamples[j]]][l];
                                meanPosterior[i][k][l] += posteriors[i][j][meanNodeCorresp[k][treeSamples[j]]][l];
                                meanLikelihood[i][k][l] += likelihoods[i][j][meanNodeCorresp[k][treeSamples[j]]][l];
                            }
                        }
                    }
                }

                for (int k = 0; k < meanLikModel.BranchLengths.Length; k++)
                {
                    int count = (from el in treeSamples where meanNodeCorresp[k][el] >= 0 select el).Count();
                    for (int l = 0; l < stateCount; l++)
                    {
                        meanPrior[i][k][l] /= count;
                        meanPosterior[i][k][l] /= count;
                        meanLikelihood[i][k][l] /= count;
                    }
                }


                double pieSize = realPlotHeight / (meanLikModel.NamedBranches.Count * 0.035) * 0.01;

                meanTree.PlotTreeWithPies(plotWidth, realPlotHeight, plotWidth / 50F, outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".prior.mean.pdf", new Plotting.Options() { FontSize = realPlotHeight / (meanLikModel.NamedBranches.Count * 1.4F), PieSize = pieSize, BranchSize = pieSize * 0.6 }, meanPrior[i], Utils.Utils.GetAllPossibleStatesString(dependencies[i], data.States));
                meanTree.PlotTreeWithPies(plotWidth, realPlotHeight, plotWidth / 50F, outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".posterior.mean.pdf", new Plotting.Options() { FontSize = realPlotHeight / (meanLikModel.NamedBranches.Count * 1.4F), PieSize = pieSize, BranchSize = pieSize * 0.6 }, meanPosterior[i], Utils.Utils.GetAllPossibleStatesString(dependencies[i], data.States));
                meanTree.PlotTreeWithPieTarget(plotWidth, realPlotHeight, plotWidth / 50F, outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".likelihood.mean.pdf", new Plotting.Options() { FontSize = realPlotHeight / (meanLikModel.NamedBranches.Count * 1.4F), PieSize = pieSize, BranchSize = pieSize * 0.6 }, meanLikelihood[i], Utils.Utils.GetAllPossibleStatesString(dependencies[i], data.States));
                meanTree.PlotTreeWithSquares(plotWidth, realPlotHeight, plotWidth / 50F, outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".all.mean.pdf", new Plotting.Options() { FontSize = realPlotHeight / (meanLikModel.NamedBranches.Count * 1.4F), PieSize = pieSize, BranchSize = pieSize * 0.6 }, meanPrior[i], meanLikelihood[i], Utils.Utils.GetAllPossibleStatesString(dependencies[i], data.States));
            }

            double[][][] meanMarginalPosterior = new double[inputData.States.Length][][];       //[character][node][state]

            for (int i = 0; i < dependencies.Length; i++)
            {
                string[] states = Utils.Utils.GetAllPossibleStatesString(dependencies[i], data.States);

                int currInd = 0;

                for (int j = 0; j < dependencies[i].Length; j++)
                {
                    int[] charInds = (from el in dependencies[i][j].InputDependencyName.Split(',') select int.Parse(el)).ToArray();

                    for (int k = 0; k < charInds.Length; k++)
                    {
                        int[][] correspStates = new int[inputData.States[charInds[k]].Length][];
                        int[] invCorrespStates = new int[states.Length];
                        for (int l = 0; l < inputData.States[charInds[k]].Length; l++)
                        {
                            List<int> currStates = new List<int>();
                            for (int m = 0; m < states.Length; m++)
                            {
                                if (states[m].Split(',')[k + currInd] == inputData.States[charInds[k]][l])
                                {
                                    currStates.Add(m);
                                    invCorrespStates[m] = l;
                                }
                            }

                            correspStates[l] = currStates.ToArray();
                        }


                        meanMarginalPosterior[charInds[k]] = new double[meanPosterior[i].Length][];

                        for (int n = 0; n < meanPosterior[i].Length; n++)
                        {
                            meanMarginalPosterior[charInds[k]][n] = new double[inputData.States[charInds[k]].Length];

                            for (int l = 0; l < inputData.States[charInds[k]].Length; l++)
                            {
                                for (int m = 0; m < correspStates[l].Length; m++)
                                {
                                    meanMarginalPosterior[charInds[k]][n][l] += meanPosterior[i][n][correspStates[l][m]];
                                }
                            }
                        }

                        float realPlotHeight = plotHeight ?? (12 * meanLikModel.NamedBranches.Count * 1.4F + plotWidth / 25F);
                        double pieSize = realPlotHeight / (meanLikModel.NamedBranches.Count * 0.035) * 0.01;

                        if (inputData.States.Length > 1)
                        {
                            meanTree.PlotTreeWithPies(plotWidth, realPlotHeight, plotWidth / 50F, outputPrefix + ".character" + charInds[k] + ".marginal.posterior.mean.pdf", new Plotting.Options() { FontSize = realPlotHeight / (meanLikModel.NamedBranches.Count * 1.4F), PieSize = pieSize, BranchSize = pieSize * 0.6 }, meanMarginalPosterior[charInds[k]], inputData.States[charInds[k]]);
                        }
                    }

                    currInd += charInds.Length;
                }
            }

            ConsoleWrapper.WriteLine();

            if (RunningGUI)
            {
                Utils.Utils.Trigger("FinishedComputingNodeProbs", new object[] { meanPrior, meanLikelihood, meanPosterior, treeSamples });
            }

            ConsoleWrapper.WriteLine();

            if (RunningGUI)
            {
                Utils.Utils.Trigger("StartedSimulatingHistories", new object[] { });
            }

            for (int i = 0; i < dependencies.Length; i++)
            {
                List<Parameter> ratesToEstimate = Utils.Utils.RatesToEstimateList(Utils.Utils.GetParametersToEstimate(dependencies[i], rates, pi));
                List<Parameter> pisToEstimate = Utils.Utils.PisToEstimateList(Utils.Utils.GetParametersToEstimate(dependencies[i], rates, pi));

                Dictionary<string, int>[] parameterKeys = new Dictionary<string, int>[dependencies[i].Length];
                int[][][] correspStates = new int[dependencies[i].Length][][];

                for (int k = 0; k < dependencies[i].Length; k++)
                {
                    if (dependencies[i][k].Type == CharacterDependency.Types.Independent)
                    {
                        parameterKeys[k] = new Dictionary<string, int>();

                        for (int j = 0; j < ratesToEstimate.Count; j++)
                        {
                            string key = rates[dependencies[i][k].Index].GetKey(ratesToEstimate[j]);
                            if (!string.IsNullOrEmpty(key))
                            {
                                parameterKeys[k].Add(key, j);
                            }
                        }
                    }
                    else
                    {
                        parameterKeys[k] = new Dictionary<string, int>();

                        for (int j = 0; j < pisToEstimate.Count; j++)
                        {
                            string key = dependencies[i][k].ConditionedProbabilities.GetKey(pisToEstimate[j]);
                            if (!string.IsNullOrEmpty(key))
                            {
                                parameterKeys[k].Add(key, ratesToEstimate.Count + j);
                            }
                        }
                    }
                }

                if (dependencies[i].Length > 1 || (dependencies[i].Length == 1 && dependencies[i][0].Type != CharacterDependency.Types.Independent))
                {
                    ConsoleWrapper.Write("Simulating {0} histories for characters ", numSim);
                    for (int j = 0; j < dependencies[i].Length; j++)
                    {
                        switch (dependencies[i][j].Type)
                        {
                            case CharacterDependency.Types.Independent:
                            case CharacterDependency.Types.Conditioned:
                                ConsoleWrapper.Write(dependencies[i][j].InputDependencyName);
                                if (j < dependencies[i].Length - 1)
                                {
                                    ConsoleWrapper.Write(", ");
                                }
                                break;
                        }
                    }
                    ConsoleWrapper.Write(": ");
                }
                else
                {
                    ConsoleWrapper.Write("Simulating {0} histories for character " + dependencies[i][0].InputDependencyName + ": ", numSim);
                }

                TaggedHistory[] histories = Utils.Simulation.SimulateJointHistories(likModels, treeSamples, dependencies[i], i, data.States, posteriors[i], parameters, rates, pi, mainRandom, numSim, numThreads);

                Stream finalPriorStream = null;
                string priorPrefix = Guid.NewGuid().ToString().Replace("-", "");

                string[] allPossibleStates = Utils.Utils.GetAllPossibleStatesString(dependencies[i], data.States);

                DStats[][][][] allPPDStats = null; //allDStats[numSim][charCount][charCount][priorMultiplicity]

                if (ppMultiplicity > 0 && (!computeDTest || (dependencies[i].Length > 1 || (dependencies[i].Length == 1 && dependencies[i][0].Type != CharacterDependency.Types.Independent))))
                {
                    ConsoleWrapper.Write("Simulating {0} posterior-predictive histories " + (computeDTest ? "and computing D-stats " : "")+ "for characters ", numSim * ppMultiplicity);
                    for (int j = 0; j < dependencies[i].Length; j++)
                    {
                        switch (dependencies[i][j].Type)
                        {
                            case CharacterDependency.Types.Independent:
                            case CharacterDependency.Types.Conditioned:
                                ConsoleWrapper.Write(dependencies[i][j].InputDependencyName);
                                if (j < dependencies[i].Length - 1)
                                {
                                    ConsoleWrapper.Write(", ");
                                }
                                break;
                        }
                    }
                    ConsoleWrapper.Write(": ");

                    int charCount = allPossibleStates[0].Split(',').Length;

                    Stream[] priorStreams = null;

                    if (computeDTest)
                    {
                        allPPDStats = new DStats[numSim][][][];

                        for (int l = 0; l < numSim; l++)
                        {
                            allPPDStats[l] = new DStats[charCount][][];
                            for (int j = 0; j < charCount; j++)
                            {
                                allPPDStats[l][j] = new DStats[charCount][];
                                for (int k = 0; k < charCount; k++)
                                {
                                    if (k > j)
                                    {
                                        allPPDStats[l][j][k] = new DStats[ppMultiplicity];
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        priorStreams = new Stream[numSim];

                        if (string.IsNullOrEmpty(priorTempFolder) || priorTempFolder != "memory")
                        {
                            if (priorTempFolder == null)
                            {
                                priorTempFolder = Path.GetDirectoryName(outputPrefix);
                            }

                            for (int j = 0; j < numSim; j++)
                            {
                                priorStreams[j] = new FileStream(Path.Combine(priorTempFolder, priorPrefix + "." + j.ToString()), FileMode.Create);
                            }
                        }
                        else
                        {
                            for (int j = 0; j < numSim; j++)
                            {
                                priorStreams[j] = new MemoryStream();
                            }
                        }

                        //Structure of each stream: |history[0]|length[0]|history[1]|length[1]|...
                    }


                    for (int j = 0; j < ppMultiplicity; j++)
                    {
                        TaggedHistory[] ppHistories = Utils.Simulation.SimulateJointHistories(likModels, treeSamples, dependencies[i], i, data.States, priors[i], parameters, rates, pi, mainRandom, numSim, numThreads, 1.0 / ppMultiplicity, (double)j / ppMultiplicity);

                        if (computeDTest)
                        {
                            for (int k = 0; k < charCount; k++)
                            {
                                for (int l = 0; l < charCount; l++)
                                {
                                    if (l > k)
                                    {
                                        for (int m = 0; m < numSim; m++)
                                        {
                                            allPPDStats[m][k][l][j] = ppHistories[m].ComputeDStat(allPossibleStates, k, l);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (int k = 0; k < numSim; k++)
                            {
                                long bytesWritten = ppHistories[k].WriteToStream(priorStreams[k]);
                                ppHistories[k] = null;
                                priorStreams[k].WriteLong(bytesWritten);
                            }
                        }

                        ppHistories = null;
                        GC.Collect();
                    }

                    ConsoleWrapper.WriteLine("Done.  ");

                    if (!computeDTest)
                    {
                        for (int k = 0; k < numSim; k++)
                        {
                            priorStreams[k].Flush();
                        }

                        ConsoleWrapper.WriteLine("Rearranging histories...");

                        long[] positions = new long[numSim];

                        if (priorTempFolder == "memory")
                        {
                            finalPriorStream = new MemoryStream();
                        }
                        else
                        {
                            finalPriorStream = new FileStream(Path.Combine(priorTempFolder, priorPrefix + ".prior"), FileMode.Create);
                        }

                        //Stream structure: | history[0][0] | history[0][1] | ... | history[0][priorMultiplicity - 1] | x[0] | length[0][0] | length[0][1] | ... | length[0][priorMultiplicity - 1] | history[1][0] | history[1][1] | ... | history[1][priorMultiplicity - 1] | x[1] | length[0][0] | length[0][1] | ... | length[0][priorMultiplicity - 1] | ... | s[0] | s[1] | ... | s[numSim - 1] | y | priorMultiplicity |
                        //                  ^x[0]                                                                     ^s[0]                                                                         ^x[1]                                                                     ^s[1]                                                                               ^y

                        long[] s = new long[numSim];

                        for (int j = 0; j < numSim; j++)
                        {
                            long x = finalPriorStream.Position;
                            long[] lengths = new long[ppMultiplicity];

                            long currOffset = priorStreams[j].Position;
                            int count = 0;

                            while (currOffset != 0)
                            {
                                priorStreams[j].Seek(-8, SeekOrigin.Current);
                                lengths[count] = priorStreams[j].ReadLong();
                                currOffset = priorStreams[j].Position - lengths[count] - 8;
                                priorStreams[j].Seek(-lengths[count] - 8, SeekOrigin.Current);
                                priorStreams[j].CopySomeTo(finalPriorStream, lengths[count]);
                                priorStreams[j].Seek(-lengths[count], SeekOrigin.Current);
                                count++;
                            }

                            finalPriorStream.Flush();

                            s[j] = finalPriorStream.Position;

                            finalPriorStream.WriteLong(x);

                            for (int k = 0; k < ppMultiplicity; k++)
                            {
                                finalPriorStream.WriteLong(lengths[k]);
                            }

                            finalPriorStream.Flush();
                        }

                        long y = finalPriorStream.Position;

                        for (int j = 0; j < numSim; j++)
                        {
                            finalPriorStream.WriteLong(s[j]);
                        }

                        finalPriorStream.WriteLong(y);
                        finalPriorStream.WriteInt(ppMultiplicity);

                        finalPriorStream.Flush();

                        finalPriorStream.Seek(0, SeekOrigin.Begin);

                        for (int k = 0; k < numSim; k++)
                        {
                            priorStreams[k].Dispose();
                        }

                        if (priorTempFolder != "memory")
                        {
                            for (int j = 0; j < numSim; j++)
                            {
                                File.Delete(Path.Combine(priorTempFolder, priorPrefix + "." + j.ToString()));
                            }
                        }
                    }
                }

                using (StreamWriter sw = new StreamWriter(outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".smap.tre"))
                {
                    foreach (TaggedHistory history in histories)
                    {
                        sw.WriteLine(Utils.Utils.GetSmapString(likModels[treeSamples[history.Tag]], history.History));
                    }
                }

                SerializedRun run;

                if (computeDTest)
                {
                    run = new SerializedRun(meanTree, histories, allPPDStats, allPossibleStates, meanPosterior[i], meanPrior[i], meanNodeCorresp, treeSamples, likModels, allPossibleStates, normaliseLength ? (double)normalisationFactor : 1.0, parameters, (from el in paramNames select el.ToArray()).ToArray());
                }
                else
                {
                    run = new SerializedRun(meanTree, histories, finalPriorStream, allPossibleStates, meanPosterior[i], meanPrior[i], meanNodeCorresp, treeSamples, likModels, allPossibleStates, normaliseLength ? (double)normalisationFactor : 1.0, parameters, (from el in paramNames select el.ToArray()).ToArray());
                }

                run.Serialize(outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".smap.bin");

                if (ppMultiplicity > 0 && priorTempFolder != "memory" && !computeDTest)
                {
                    finalPriorStream.Dispose();
                    File.Delete(Path.Combine(priorTempFolder, priorPrefix + ".prior"));
                }

                if (RunningGUI)
                {
                    Utils.Utils.Trigger("SerializedRun", new object[] { i, run });
                }

                float realPlotHeight = plotHeight ?? (12 * meanLikModel.NamedBranches.Count * 1.4F + plotWidth / 25F);
                double pieSize = realPlotHeight / (meanLikModel.NamedBranches.Count * 0.035) * 0.01;

                double[][] conditionedMeanPosterior = new double[meanPosterior[i].Length][];

                for (int j = 0; j < conditionedMeanPosterior.Length; j++)
                {
                    if (isMeanTreeClockLike)
                    {
                        conditionedMeanPosterior[j] = Utils.Utils.GetBranchStateProbs(histories, treeSamples, likModels, meanLikModel, meanNodeCorresp, new List<string>(allPossibleStates), j, 0, true);
                    }
                    else
                    {
                        conditionedMeanPosterior[j] = Utils.Utils.GetBranchStateProbs(histories, treeSamples, likModels, meanLikModel, meanNodeCorresp, new List<string>(allPossibleStates), j, meanLikModel.BranchLengths[j], false);
                    }
                }


                if (!isMeanTreeClockLike)
                {
                    conditionedMeanPosterior[conditionedMeanPosterior.Length - 1] = meanPosterior[i][conditionedMeanPosterior.Length - 1];
                }

                if (RunningGUI)
                {
                    Utils.Utils.Trigger("SimulationStepFinished", new object[] { histories, conditionedMeanPosterior });
                }

                meanTree.PlotTreeWithPiesAndBranchStates(plotWidth, realPlotHeight, plotWidth / 50F, outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".smap.pdf", new Plotting.Options() { FontSize = realPlotHeight / (meanLikModel.NamedBranches.Count * 1.4F), PieSize = pieSize, BranchSize = pieSize * 0.6 }, conditionedMeanPosterior, histories, treeSamples, likModels, meanLikModel, meanNodeCorresp, plotWidth / 250F, new List<string>(allPossibleStates));

                if (trees.Count > 1)
                {
                    meanTree.PlotTreeWithBranchSampleSizes(plotWidth, realPlotHeight, plotWidth / 50F, outputPrefix + (dependencies.Length > 1 ? ".set" + i.ToString() : "") + ".ssize.pdf", new Plotting.Options() { FontSize = realPlotHeight / (meanLikModel.NamedBranches.Count * 1.4F), PieSize = pieSize, BranchSize = pieSize * 0.6 }, histories, treeSamples, likModels, meanLikModel, meanNodeCorresp, plotWidth / 250F, new List<string>(allPossibleStates));
                }

                if (inputData.States.Length > 1)
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.Write("Computing marginal maps: ");
                    int cursorPos = ConsoleWrapper.CursorLeft;

                    ConsoleWrapper.Write("0%");

                    Dictionary<string, int> statesInds = new Dictionary<string, int>();

                    for (int k = 0; k < allPossibleStates.Length; k++)
                    {
                        statesInds.Add(allPossibleStates[k], k);
                    }

                    int currInd = 0;

                    int doneChars = 0;

                    int totalChars = 0;

                    for (int j = 0; j < dependencies[i].Length; j++)
                    {
                        totalChars += (from el in dependencies[i][j].InputDependencyName.Split(',') select int.Parse(el)).Count();
                    }

                    for (int j = 0; j < dependencies[i].Length; j++)
                    {
                        int[] charInds = (from el in dependencies[i][j].InputDependencyName.Split(',') select int.Parse(el)).ToArray();

                        for (int k = 0; k < charInds.Length; k++)
                        {
                            int[] invCorrespStates = new int[allPossibleStates.Length];
                            for (int l = 0; l < inputData.States[charInds[k]].Length; l++)
                            {
                                for (int m = 0; m < allPossibleStates.Length; m++)
                                {
                                    if (allPossibleStates[m].Split(',')[k + currInd] == inputData.States[charInds[k]][l])
                                    {
                                        invCorrespStates[m] = l;
                                    }
                                }
                            }

                            TaggedHistory[] marginalHistories = new TaggedHistory[histories.Length];

                            double progress = ((double)(doneChars + k) / totalChars);

                            double lastProgress = -1;

                            for (int l = 0; l < histories.Length; l++)
                            {
                                marginalHistories[l] = new TaggedHistory(histories[l].Tag, Utils.Simulation.GetMarginalHistory(histories[l].History, invCorrespStates, inputData.States[charInds[k]], statesInds));
                                progress = ((double)(doneChars + k + (double)l / histories.Length) / totalChars);
                                if (Math.Round(progress, 2) > lastProgress)
                                {
                                    lastProgress = Math.Round(progress, 2);
                                    ConsoleWrapper.CursorLeft = cursorPos;
                                    ConsoleWrapper.Write("{0}   ", (progress).ToString("0%", System.Globalization.CultureInfo.InvariantCulture));
                                    ConsoleWrapper.CursorLeft = ConsoleWrapper.CursorLeft - 3;
                                }
                            }

                            meanTree.PlotTreeWithPiesAndBranchStates(plotWidth, realPlotHeight, plotWidth / 50F, outputPrefix + ".character" + charInds[k] + ".marginal.smap.pdf", new Plotting.Options() { FontSize = realPlotHeight / (meanLikModel.NamedBranches.Count * 1.4F), PieSize = pieSize, BranchSize = pieSize * 0.6 }, meanMarginalPosterior[charInds[k]], marginalHistories, treeSamples, likModels, meanLikModel, meanNodeCorresp, plotWidth / 250F, new List<string>(inputData.States[charInds[k]]));

                            ConsoleWrapper.CursorLeft = cursorPos;
                            ConsoleWrapper.Write("{0}   ", ((double)(doneChars + k + 1) / totalChars).ToString("0%", System.Globalization.CultureInfo.InvariantCulture));
                            ConsoleWrapper.CursorLeft = ConsoleWrapper.CursorLeft - 3;
                        }
                        doneChars += charInds.Length;
                        currInd += charInds.Length;
                    }

                    ConsoleWrapper.CursorLeft = cursorPos;
                    ConsoleWrapper.WriteLine("Done.  ");
                }
            }

            if (RunningGUI)
            {
                Utils.Utils.Trigger("FinishedSimulatingHistories", new object[] { });
            }

            if (pollInterrupt)
            {
                interruptThreadWaitHandle.Set();
            }

            if (!string.IsNullOrEmpty(tempDir))
            {
                try
                {
                    Directory.SetCurrentDirectory(prevWD);
                    Directory.Delete(tempDir, true);
                }
                catch (Exception e)
                {
                    ConsoleWrapper.WriteLine("The analysis finished correctly, but an error occurred during cleanup: " + e.Message);
                }
            }

            if (RunningGUI)
            {
                Utils.Utils.Trigger("AllFinished", new object[] { });
            }

            return 0;
        }
    }
}
