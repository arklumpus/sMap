using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Utils
{
    [Serializable]
    public class TaggedHistory
    {
        public int Tag { get; set; }
        public BranchState[][] History { get; set; }

        public TaggedHistory(int tag, BranchState[][] history)
        {
            Tag = tag;
            History = history;
        }

        public long WriteToStream(Stream sr)
        {
            sr.Flush();
            long initLength = sr.Length;
            using (GZipStream gZipStream = new GZipStream(sr, CompressionLevel.Optimal, true))
            {
                using (StreamWriter sw = new StreamWriter(gZipStream))
                using (JsonTextWriter tw = new JsonTextWriter(sw))
                {
                    JsonSerializer ser = new JsonSerializer();
                    ser.Serialize(tw, this);
                    tw.Flush();
                }
            }
            sr.Flush();
            return sr.Length - initLength;
        }

        public static TaggedHistory ReadFromStream(Stream stream)
        {
            using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress, true))
            {
                using (StreamReader sr = new StreamReader(gZipStream))
                using (JsonTextReader tr = new JsonTextReader(sr))
                {
                    JsonSerializer ser = new JsonSerializer();
                    return ser.Deserialize<TaggedHistory>(tr);
                }
            }
        }

        public Dictionary<string, int> GetTransitions(string[] allPossibleStates)
        {
            Dictionary<string, int> tbr = new Dictionary<string, int>();
            for (int i = 0; i < allPossibleStates.Length; i++)
            {
                for (int j = 0; j < allPossibleStates.Length; j++)
                {
                    if (i != j)
                    {
                        tbr.Add(allPossibleStates[i] + ">" + allPossibleStates[j], 0);
                    }
                }
            }

            for (int i = 0; i < History.Length; i++)
            {
                if (History[i] != null)
                {
                    for (int j = 0; j < History[i].Length - 1; j++)
                    {
                        if (History[i][j].State != History[i][j + 1].State)
                        {
                            tbr[History[i][j].State + ">" + History[i][j + 1].State]++;
                        }
                    }
                }
            }

            return tbr;
        }

        public Dictionary<string, double> GetTimes(string[] allPossibleStates)
        {
            Dictionary<string, double> tbr = new Dictionary<string, double>();
            for (int i = 0; i < allPossibleStates.Length; i++)
            {
                tbr.Add(allPossibleStates[i], 0);
            }

            for (int i = 0; i < History.Length; i++)
            {
                if (History[i] != null)
                {
                    for (int j = 0; j < History[i].Length; j++)
                    {
                        tbr[History[i][j].State] += History[i][j].Length;
                    }
                }
            }

            return tbr;
        }

        public DStats ComputeDStat(string[] allPossibleStates, int char0, int char1)
        {
            Dictionary<string, double> times = GetTimes(allPossibleStates);

            string[][] currStates = new string[][] { new HashSet<string>(from el in allPossibleStates select el.Split(',')[char0]).ToArray(), new HashSet<string>(from el in allPossibleStates select el.Split(',')[char1]).ToArray() };

            double[,] tijs = new double[currStates[0].Length, currStates[1].Length];

            double total = 0;

            for (int i = 0; i < currStates[0].Length; i++)
            {
                for (int j = 0; j < currStates[1].Length; j++)
                {
                    foreach (KeyValuePair<string, double> kvp in times)
                    {
                        if (kvp.Key.Split(',')[char0] == currStates[0][i] && kvp.Key.Split(',')[char1] == currStates[1][j])
                        {
                            tijs[i, j] += kvp.Value;
                            total += kvp.Value;
                        }
                    }
                }
            }

            for (int i = 0; i < currStates[0].Length; i++)
            {
                for (int j = 0; j < currStates[1].Length; j++)
                {
                    tijs[i, j] /= total;
                }
            }

            double[] sum0 = new double[currStates[0].Length];
            double[] sum1 = new double[currStates[1].Length];

            for (int i = 0; i < currStates[0].Length; i++)
            {
                for (int j = 0; j < currStates[1].Length; j++)
                {
                    sum0[i] += tijs[i, j];
                }
            }

            for (int j = 0; j < currStates[1].Length; j++)
            {
                for (int i = 0; i < currStates[0].Length; i++)
                {
                    sum1[j] += tijs[i, j];
                }
            }

            double[][] dijs = new double[currStates[0].Length][];

            double D = 0;

            for (int i = 0; i < currStates[0].Length; i++)
            {
                dijs[i] = new double[currStates[1].Length];
                for (int j = 0; j < currStates[1].Length; j++)
                {
                    dijs[i][j] = tijs[i, j] - sum0[i] * sum1[j];
                    D += Math.Abs(dijs[i][j]);
                }
            }

            return new DStats(dijs, D);
        }


    }

    public static class TaggedHistoryExtensions
    {
        public static DStats ComputeAverageDStats(this TaggedHistory[] histories, string[] allPossibleStates, int char1, int char2)
        {
            double[] Ds = new double[histories.Length];
            double[][][] dijs = new double[histories.Length][][];

            for (int i = 0; i < histories.Length; i++)
            {
                DStats stats = histories[i].ComputeDStat(allPossibleStates, char1, char2);

                Ds[i] = stats.D;
                dijs[i] = stats.dij;
            }

            double D = 0;

            if (DStats.DStatMode == DStats.DStatModes.Median)
            {
                D = Ds.Median();
            }
            else if (DStats.DStatMode == DStats.DStatModes.Mean)
            {
                D = Ds.Average();
            }


            string[][] currStates = new string[][] { new HashSet<string>(from el in allPossibleStates select el.Split(',')[char1]).ToArray(), new HashSet<string>(from el in allPossibleStates select el.Split(',')[char2]).ToArray() };

            double[][] dij = new double[currStates[0].Length][];

            for (int i = 0; i < currStates[0].Length; i++)
            {
                dij[i] = new double[currStates[1].Length];
                for (int j = 0; j < currStates[1].Length; j++)
                {
                    if (DStats.DStatMode == DStats.DStatModes.Median)
                    {
                        dij[i][j] = (from el in dijs select el[i][j]).Median();
                    }
                    else if (DStats.DStatMode == DStats.DStatModes.Mean)
                    {
                        for (int k = 0; k < histories.Length; k++)
                        {
                            dij[i][j] += dijs[k][i][j] / histories.Length;
                        }
                    }
                }
            }

            return new DStats(dij, D);
        }
    }

    public class DStats
    {
        public enum DStatModes { Mean, Median };

        public static DStatModes DStatMode = DStatModes.Median;

        public double[][] dij { get; set; }
        public double D { get; set; }

        public DStats(double[][] dijs, double d)
        {
            this.dij = dijs;
            this.D = d;
        }

        public static DStats GetAverageDStats(DStats[] stats)
        {
            int m = stats[0].dij.Length;
            int n = stats[0].dij[0].Length;

            double[][] dijs = new double[m][];

            for (int i = 0; i < m; i++)
            {
                dijs[i] = new double[n];
            }

            double D = 0;

            if (DStatMode == DStatModes.Median)
            {
                D = (from el in stats select el.D).Median();
                for (int k = 0; k < m; k++)
                {
                    for (int l = 0; l < n; l++)
                    {
                        dijs[k][l] = (from el in stats select el.dij[k][l]).Median();
                    }
                }
            }
            else if (DStatMode == DStatModes.Mean)
            {
                for (int i = 0; i < stats.Length; i++)
                {
                    for (int k = 0; k < m; k++)
                    {
                        for (int l = 0; l < n; l++)
                        {
                            dijs[k][l] += stats[i].dij[k][l] / stats.Length;
                        }
                    }

                    D += stats[i].D / stats.Length;
                }
            }

            return new DStats(dijs, D);
        }
    }

    public class DTest
    {
        public DStats DStats { get; }
        public double P { get; }
        public double[][] Pij { get; }
        public DTest(DStats posteriorStats, double p, double[][] pij)
        {
            this.DStats = posteriorStats;
            this.P = p;
            this.Pij = pij;
        }
    }
}
