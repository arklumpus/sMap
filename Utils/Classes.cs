using MathNet.Numerics.Distributions;
using Mono.Options;
using Newtonsoft.Json;
using SlimTreeNode;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class Action3ParamOption<T1, T2, T3> : Option
    {
        Action<T1, T2, T3> action;
        public Action3ParamOption(string prototype, string description,
                 Action<T1, T2, T3> action)
            : base(prototype, description, 3)
        {
            this.action = action;
        }

        protected override void OnParseComplete(OptionContext c)
        {
            action(Parse<T1>(c.OptionValues[0], c), Parse<T2>(c.OptionValues[1], c), Parse<T3>(c.OptionValues[2], c));
        }
    }


    public static class ConsoleWrapper
    {
        public static bool ConsoleEnabled = true;

        public static void Write(string value)
        {
            if (ConsoleEnabled)
            {
                Console.Write(value);
            }
        }

        public static void Write(char value)
        {
            if (ConsoleEnabled)
            {
                Console.Write(value);
            }
        }

        public static void Write(string format, params object[] args)
        {
            if (ConsoleEnabled)
            {
                Console.Write(format, args);
            }
        }

        public static void WriteLine()
        {
            if (ConsoleEnabled)
            {
                Console.WriteLine();
            }
        }

        public static void WriteLine(string value)
        {
            if (ConsoleEnabled)
            {
                Console.WriteLine(value);
            }
        }

        public static void WriteLine(string format, params object[] args)
        {
            if (ConsoleEnabled)
            {
                Console.WriteLine(format, args);
            }
        }

        public static void SetCursorPosition(int left, int top)
        {
            if (ConsoleEnabled && !Console.IsOutputRedirected)
            {
                Console.SetCursorPosition(left, top);
            }
        }

        public static int CursorTop { get { if (ConsoleEnabled && !Console.IsOutputRedirected) { return Console.CursorTop; } else { return 0; } } }

        public static int CursorLeft { get { if (ConsoleEnabled && !Console.IsOutputRedirected) { return Console.CursorLeft; } else { return 0; } } set { if (ConsoleEnabled && !Console.IsOutputRedirected) { Console.CursorLeft = value; } } }

        public static bool CursorVisible { get { if (ConsoleEnabled && !Console.IsOutputRedirected) { try { return Console.CursorVisible; } catch { return false; } } else { return false; } } set { if (ConsoleEnabled && !Console.IsOutputRedirected) { try { Console.CursorVisible = value; } catch { } } } }
    }

    [Serializable]
    public struct BranchState
    {
        public string State;
        public double Length;

        public BranchState(string state, double length)
        {
            State = state;
            Length = length;
        }
    }

    [Serializable]
    public class SerializedRun
    {
        public TaggedHistory[] Histories { get; set; }

        [JsonIgnore, NonSerialized]
        private Stream priorHistoriesStream;

        [JsonIgnore, NonSerialized]
        private long[] priorOffsets;

        [JsonIgnore, NonSerialized]
        private long priorStartOffset;

        public int PriorMultiplicity { get; set; }

        public DStats[][][][] AllDStats { get; set; }

        [JsonIgnore]
        public TreeNode SummaryTree { get; private set; }

        [JsonIgnore]
        public bool HasPriorHistories
        {
            get
            {
                return this.priorHistoriesStream != null;
            }
        }

        public string SummaryTreeString { get; set; }

        public string[] States { get; set; }
        public double[][] MeanPosterior { get; set; }
        public double[][] MeanPrior { get; set; }
        public int[][] SummaryNodeCorresp { get; set; }
        public int[] TreeSamples { get; set; }
        public LikelihoodModel[] LikelihoodModels { get; set; }
        public string[] AllPossibleStates { get; set; }
        public double AgeScale { get; set; }
        public double[][][] Parameters { get; set; }
        public string[][] ParameterNames { get; set; }

        const byte minRev = 0;
        const byte maxRev = 3;

        public byte revision = 255;

        public SerializedRun()
        {

        }

        public void CloseStream()
        {
            if (priorHistoriesStream != null)
            {
                priorHistoriesStream.Dispose();
            }
        }

        public SerializedRun(TreeNode summaryTree, TaggedHistory[] histories, string[] states, double[][] meanPosterior, double[][] meanPrior, int[][] summaryNodeCorresp, int[] treeSamples, LikelihoodModel[] likelihoodModels, string[] allPossibleStates, double ageScale)
        {
            revision = 0;
            SummaryTree = summaryTree;
            SummaryTreeString = summaryTree.ToString();
            Histories = histories;
            States = states;
            MeanPosterior = meanPosterior;
            MeanPrior = meanPrior;
            SummaryNodeCorresp = summaryNodeCorresp;
            TreeSamples = treeSamples;
            LikelihoodModels = likelihoodModels;
            AllPossibleStates = allPossibleStates;
            AgeScale = ageScale;
            Parameters = null;
            ParameterNames = null;
        }

        public SerializedRun(TreeNode summaryTree, TaggedHistory[] histories, DStats[][][][] dStats, string[] states, double[][] meanPosterior, double[][] meanPrior, int[][] summaryNodeCorresp, int[] treeSamples, LikelihoodModel[] likelihoodModels, string[] allPossibleStates, double ageScale, double[][][] parameters, string[][] parameterNames)
        {
            revision = 2;
            SummaryTree = summaryTree;
            SummaryTreeString = summaryTree.ToString();
            Histories = histories;
            States = states;
            MeanPosterior = meanPosterior;
            MeanPrior = meanPrior;
            SummaryNodeCorresp = summaryNodeCorresp;
            TreeSamples = treeSamples;
            LikelihoodModels = likelihoodModels;
            AllPossibleStates = allPossibleStates;
            AgeScale = ageScale;
            Parameters = parameters;
            ParameterNames = parameterNames;
            AllDStats = dStats;
        }

        public SerializedRun(TreeNode summaryTree, TaggedHistory[] histories, Stream priorHistories, string[] states, double[][] meanPosterior, double[][] meanPrior, int[][] summaryNodeCorresp, int[] treeSamples, LikelihoodModel[] likelihoodModels, string[] allPossibleStates, double ageScale, double[][][] parameters, string[][] parameterNames)
        {
            revision = 0;
            SummaryTree = summaryTree;
            SummaryTreeString = summaryTree.ToString();
            Histories = histories;
            States = states;
            MeanPosterior = meanPosterior;
            MeanPrior = meanPrior;
            SummaryNodeCorresp = summaryNodeCorresp;
            TreeSamples = treeSamples;
            LikelihoodModels = likelihoodModels;
            AllPossibleStates = allPossibleStates;
            AgeScale = ageScale;
            Parameters = parameters;
            ParameterNames = parameterNames;

            if (priorHistories != null)
            {
                revision = 3;
                priorHistoriesStream = priorHistories;
                this.priorStartOffset = 0;
                priorHistoriesStream.Seek(-4, SeekOrigin.End);
                this.PriorMultiplicity = priorHistoriesStream.ReadInt();
                priorHistoriesStream.Seek(-12, SeekOrigin.End);
                long offsetsOffset = priorHistoriesStream.ReadLong();

                priorHistoriesStream.Seek(offsetsOffset + this.priorStartOffset, SeekOrigin.Begin);
                this.priorOffsets = new long[this.Histories.Length];

                for (int i = 0; i < this.Histories.Length; i++)
                {
                    this.priorOffsets[i] = priorHistoriesStream.ReadLong();
                }
            }
        }

        public void Serialize(string filename)
        {

            string leavesNames = Utils.StringifyArray(SummaryTree.GetLeafNames(), " ");

            using (FileStream fs = new FileStream(filename, FileMode.Create))
            {
                fs.WriteByte((byte)'s');
                fs.WriteByte((byte)'M');
                fs.WriteByte((byte)'a');
                fs.WriteByte((byte)'p');
                fs.WriteByte(revision);

                if (priorHistoriesStream != null)
                {
                    fs.WriteLong(priorHistoriesStream.Length);
                }
                else if (revision >= 2)
                {
                    fs.WriteLong(0);
                }

                fs.WriteInt(leavesNames.Length);

                for (int i = 0; i < leavesNames.Length; i++)
                {
                    fs.WriteByte((byte)leavesNames[i]);
                }

                using (GZipStream gZipStream = new GZipStream(fs, CompressionLevel.Optimal, true))
                {
                    using (StreamWriter sw = new StreamWriter(gZipStream))
                    using (JsonTextWriter tw = new JsonTextWriter(sw))
                    {
                        JsonSerializer ser = new JsonSerializer();
                        ser.Serialize(tw, this);
                        tw.Flush();
                    }
                }

                if (priorHistoriesStream != null)
                {
                    priorHistoriesStream.Seek(0, SeekOrigin.Begin);
                    priorHistoriesStream.CopyTo(fs);
                }
            }
        }

        public static SerializedRun Deserialize(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);

            byte[] header = new byte[4];
            fs.Read(header, 0, 4);

            if (BitConverter.ToString(header) != "73-4D-61-70")
            {
                throw new FormatException(filename + " is not a supported sMap run file!");
            }

            byte rev = (byte)fs.ReadByte();

            if (rev < minRev || rev > maxRev)
            {
                throw new FormatException("Revision " + rev.ToString() + " is not supported! Supported revisions: " + minRev.ToString() + "-" + maxRev.ToString());
            }

            int length;
            long priorLength = 0;

            if (rev < 2)
            {
                length = fs.ReadInt();
            }
            else
            {
                if (rev >= 2)
                {
                    priorLength = fs.ReadLong();
                }

                length = fs.ReadInt();
            }

            fs.Seek(length, SeekOrigin.Current);

            SerializedRun tbr;

            using (GZipStream gZipStream = new GZipStream(fs, CompressionMode.Decompress, true))
            {
                using (StreamReader sr = new StreamReader(gZipStream))
                using (JsonTextReader tr = new JsonTextReader(sr))
                {
                    JsonSerializer ser = new JsonSerializer();
                    tbr = ser.Deserialize<SerializedRun>(tr);
                    tbr.SummaryTree = TreeNode.Parse(tbr.SummaryTreeString, null);
                    tbr.States = tbr.AllPossibleStates;
                }
            }

            if (priorLength > 0 && rev >= 3)
            {
                tbr.priorStartOffset = fs.Length - priorLength;

                fs.Seek(-4, SeekOrigin.End);
                tbr.PriorMultiplicity = fs.ReadInt();
                fs.Seek(-12, SeekOrigin.End);
                long offsetsOffset = fs.ReadLong();

                fs.Seek(offsetsOffset + tbr.priorStartOffset, SeekOrigin.Begin);
                tbr.priorOffsets = new long[tbr.Histories.Length];

                for (int i = 0; i < tbr.Histories.Length; i++)
                {
                    tbr.priorOffsets[i] = fs.ReadLong();
                }

                tbr.priorHistoriesStream = fs;
            }

            return tbr;
        }

        public TaggedHistory[] GetPriorHistories(int simulationInd)
        {
            long offset = this.priorOffsets[simulationInd];
            this.priorHistoriesStream.Seek(offset + this.priorStartOffset, SeekOrigin.Begin);

            long startOffset = this.priorHistoriesStream.ReadLong();
            long[] lengths = new long[this.PriorMultiplicity];

            for (int i = 0; i < lengths.Length; i++)
            {
                lengths[i] = this.priorHistoriesStream.ReadLong();
            }

            long prevLength = 0;

            TaggedHistory[] tbr = new TaggedHistory[this.PriorMultiplicity];

            for (int i = 0; i < this.PriorMultiplicity; i++)
            {
                this.priorHistoriesStream.Seek(startOffset + this.priorStartOffset + prevLength, SeekOrigin.Begin);
                tbr[i] = TaggedHistory.ReadFromStream(this.priorHistoriesStream);
                prevLength += lengths[i];
            }

            return tbr;
        }


        public DTest ComputeDTest(int char1, int char2, Action<double> progressAction = null)
        {
            if (revision == 2)
            {
                DStats posteriorStats = this.Histories.ComputeAverageDStats(this.AllPossibleStates, char1, char2);

                int greaterThan = 0;
                int[][] greaterThan_ij = new int[posteriorStats.dij.Length][];

                for (int i = 0; i < greaterThan_ij.Length; i++)
                {
                    greaterThan_ij[i] = new int[posteriorStats.dij[i].Length];
                }

                double lastProgress = -1;

                for (int i = 0; i < this.AllDStats.Length; i++)
                {
                    DStats averageDStats = DStats.GetAverageDStats(this.AllDStats[i][Math.Min(char1, char2)][Math.Max(char1, char2)]);

                    if (averageDStats.D >= posteriorStats.D)
                    {
                        greaterThan++;
                    }

                    for (int j = 0; j < averageDStats.dij.Length; j++)
                    {
                        for (int k = 0; k < averageDStats.dij[j].Length; k++)
                        {
                            if (Math.Abs(averageDStats.dij[j][k]) >= Math.Abs(posteriorStats.dij[j][k]))
                            {
                                greaterThan_ij[j][k]++;
                            }
                        }
                    }

                    double progress = (double)(i + 1) / this.AllDStats.Length;

                    if (Math.Round(progress * 100) > Math.Round(lastProgress * 100))
                    {
                        lastProgress = progress;
                        progressAction?.Invoke(progress);
                    }


                }

                double P = (double)greaterThan / this.AllDStats.Length;

                double[][] Pij = (from el in greaterThan_ij select (from el2 in el select (double)el2 / this.AllDStats.Length).ToArray()).ToArray();

                return new DTest(posteriorStats, P, Pij);
            }
            else if (revision == 3)
            {
                DStats posteriorStats = this.Histories.ComputeAverageDStats(this.AllPossibleStates, char1, char2);

                int greaterThan = 0;
                int[][] greaterThan_ij = new int[posteriorStats.dij.Length][];

                for (int i = 0; i < greaterThan_ij.Length; i++)
                {
                    greaterThan_ij[i] = new int[posteriorStats.dij[i].Length];
                }

                double lastProgress = -1;

                for (int i = 0; i < this.Histories.Length; i++)
                {
                    TaggedHistory[] hist = this.GetPriorHistories(i);
                    DStats stats = hist.ComputeAverageDStats(this.AllPossibleStates, char1, char2);

                    if (stats.D >= posteriorStats.D)
                    {
                        greaterThan++;
                    }

                    for (int j = 0; j < stats.dij.Length; j++)
                    {
                        for (int k = 0; k < stats.dij[j].Length; k++)
                        {
                            if (Math.Abs(stats.dij[j][k]) >= Math.Abs(posteriorStats.dij[j][k]))
                            {
                                greaterThan_ij[j][k]++;
                            }
                        }
                    }

                    double progress = (double)(i + 1) / this.Histories.Length;

                    if (Math.Round(progress * 100) > Math.Round(lastProgress * 100))
                    {
                        lastProgress = progress;
                        progressAction?.Invoke(progress);
                    }
                }

                double P = (double)greaterThan / this.Histories.Length;

                double[][] Pij = (from el in greaterThan_ij select (from el2 in el select (double)el2 / this.Histories.Length).ToArray()).ToArray();

                return new DTest(posteriorStats, P, Pij);
            }

            return null;
        }
    }


    public class MultivariateDistribution
    {
        private Dirichlet internalDirichlet;
        private Multinomial internalMultinomial;

        public enum DistributionType
        {
            Dirichlet, Multinomial
        }

        public DistributionType Type;

        public int Dimension
        {
            get
            {
                switch (Type)
                {
                    case DistributionType.Dirichlet:
                        return internalDirichlet.Dimension;
                    case DistributionType.Multinomial:
                        return internalMultinomial.P.Length;
                    default:
                        return -1;
                }
            }
        }

        public double[] Alpha
        {
            get
            {
                switch (Type)
                {
                    case DistributionType.Dirichlet:
                        return internalDirichlet.Alpha;
                    case DistributionType.Multinomial:
                        return internalMultinomial.P;
                    default:
                        return new double[0];
                }
            }
        }

        public double[] Mean
        {
            get
            {
                switch (Type)
                {
                    case DistributionType.Dirichlet:
                        return internalDirichlet.Mean;
                    case DistributionType.Multinomial:
                        return (from el in internalMultinomial.Sample() select (double)el).ToArray();
                    default:
                        return new double[0];
                }
            }
        }

        public double[] Sample()
        {
            switch (Type)
            {
                case DistributionType.Dirichlet:
                    return internalDirichlet.Sample();
                case DistributionType.Multinomial:
                    return (from el in internalMultinomial.Sample() select (double)el).ToArray();
                default:
                    return new double[0];
            }
        }

        public double DensityLn(double[] x)
        {
            switch (Type)
            {
                case DistributionType.Dirichlet:
                    return internalDirichlet.DensityLn(x);
                case DistributionType.Multinomial:
                    return internalMultinomial.ProbabilityLn((from el in x select (int)el).ToArray());
                default:
                    return double.NegativeInfinity;
            }
        }

        public double MarginalDensity(double x, int i)
        {
            switch (Type)
            {
                case DistributionType.Dirichlet:
                    Beta beta = new Beta(internalDirichlet.Alpha[i], internalDirichlet.Alpha.Sum() - internalDirichlet.Alpha[i], internalDirichlet.RandomSource);
                    return beta.Density(x);
                case DistributionType.Multinomial:
                    return internalMultinomial.P[i];
                default:
                    return 0;
            }
        }

        public double MarginalCumulativeDistribution(double x, int i)
        {
            switch (Type)
            {
                case DistributionType.Dirichlet:
                    Beta beta = new Beta(internalDirichlet.Alpha[i], internalDirichlet.Alpha.Sum() - internalDirichlet.Alpha[i], internalDirichlet.RandomSource);
                    return beta.CumulativeDistribution(x);
                case DistributionType.Multinomial:
                    if (x == 0)
                    {
                        return 0;
                    }
                    else if (x < 1)
                    {
                        return 1 - internalMultinomial.P[i];
                    }
                    else
                    {
                        return 1.0;
                    }
                default:
                    return 0;
            }
        }

        public MultivariateDistribution(Dirichlet dirichlet)
        {
            this.Type = DistributionType.Dirichlet;
            internalDirichlet = dirichlet;
        }

        public MultivariateDistribution(Multinomial multinomial)
        {
            this.Type = DistributionType.Multinomial;
            internalMultinomial = multinomial;
        }
    }

    [Serializable]
    public class LikelihoodModel
    {
        public double[] BranchLengths { get; set; }
        public int[][] Children { get; set; }
        public int[] Parents { get; set; }
        public Dictionary<string, int> NamedBranches { get; set; }

        public override string ToString()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public LikelihoodModel()
        {

        }

        public LikelihoodModel(TreeNode tree)
        {
            List<TreeNode> nodes = tree.GetChildrenRecursive();

            BranchLengths = new double[nodes.Count];
            Children = new int[nodes.Count][];
            Parents = new int[nodes.Count];
            NamedBranches = new Dictionary<string, int>();

            List<string> guids = new List<string>(nodes.Count);
            int j = 0;

            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                guids.Add(nodes[i].Guid);
                BranchLengths[j] = nodes[i].Length;
                Children[j] = new int[nodes[i].Children.Count];
                if (nodes[i].Children.Count > 0)
                {
                    for (int k = 0; k < nodes[i].Children.Count; k++)
                    {
                        Children[j][k] = guids.IndexOf(nodes[i].Children[k].Guid);
                    }
                }
                if (!string.IsNullOrEmpty(nodes[i].Name))
                {
                    NamedBranches.Add(nodes[i].Name, j);
                }
                j++;
            }

            j = 0;

            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                if (nodes[i].Parent != null)
                {
                    Parents[j] = guids.IndexOf(nodes[i].Parent.Guid);
                }
                else
                {
                    Parents[j] = -1;
                }
                j++;
            }
        }

        public List<int> GetAllChildLeavesSorted(int node)
        {
            if (Children[node].Length == 0)
            {
                return new List<int>() { node };
            }
            else
            {
                List<int> tbr = new List<int>();

                foreach (int i in Children[node])
                {
                    tbr.AddRange(GetAllChildLeavesSorted(i));
                }
                tbr.Sort();
                return tbr;
            }
        }
    }
    public class ThreadSafeRandom : Random
    {
        private static RNGCryptoServiceProvider _global = new RNGCryptoServiceProvider();
        private static Random _globalRandom;
        private static object _globalLock = new object();
        [ThreadStatic] private static Random _local;

        private bool _useGlobalRandom;

        public ThreadSafeRandom(int seed)
        {
            lock (_globalLock)
            {
                _globalRandom = new Random(seed);
                _useGlobalRandom = true;
            }
        }

        public ThreadSafeRandom()
        {
            _useGlobalRandom = false;
        }

        private void InitialiseLocal()
        {
            if (_local == null)
            {
                if (!_useGlobalRandom)
                {
                    byte[] buffer = new byte[4];
                    _global.GetBytes(buffer);
                    _local = new Random(BitConverter.ToInt32(buffer, 0));
                }
                else
                {
                    lock (_globalLock)
                    {
                        _local = new Random(_globalRandom.Next());
                    }
                }
            }
        }

        public override int Next()
        {
            InitialiseLocal();
            return _local.Next();
        }

        public override int Next(int maxValue)
        {
            InitialiseLocal();
            return _local.Next(maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            InitialiseLocal();
            return _local.Next(minValue, maxValue);
        }

        public override double NextDouble()
        {
            InitialiseLocal();
            return _local.NextDouble();
        }

        public override void NextBytes(byte[] buffer)
        {
            InitialiseLocal();
            _local.NextBytes(buffer);
        }
    }
}
