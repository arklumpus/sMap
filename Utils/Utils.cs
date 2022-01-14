using SlimTreeNode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;
using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using MathNet.Numerics;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using MathNet.Numerics.Differentiation;

namespace Utils
{
    public class PerformanceDataPoint
    {
        public int NumRuns { get; }
        public int NumChains { get; }
        public double[] TimePerSample { get; }
        public double[] NumberOfSamples { get; }

        public PerformanceDataPoint(int numRuns, int numChains, double[] timePerSample, double[] numberOfSamples)
        {
            NumRuns = numRuns;
            NumChains = numChains;
            TimePerSample = timePerSample;
            NumberOfSamples = numberOfSamples;
        }
    }

    public enum Strategies { RandomWalk, NesterovClimbing, Sampling, IterativeSampling }

    public abstract class MaximisationStrategy
    {
        public virtual Strategies Strategy { get; }
        public virtual bool Plot { get; }

        public static MaximisationStrategy Parse(string sr)
        {
            sr = sr.Replace("\"", "");

            string strategyName = sr.Contains("(") ? sr.Substring(0, sr.IndexOf("(")) : sr;

            string[] args;

            if (sr.Contains("("))
            {
                string argsString = sr.Substring(sr.IndexOf("(") + 1);
                argsString = argsString.Substring(0, argsString.IndexOf(")"));
                args = argsString.Replace(" ", "").Split(',');
            }
            else
            {
                args = new string[0];
            }

            switch (strategyName.ToLower())
            {
                case "randomwalk":
                case "rw":
                    if (args.Length > 0)
                    {
                        bool plot = true;
                        double threshold = 0.001;
                        int steps = 10000;
                        ConvergenceCriteria criterion = ConvergenceCriteria.Value;

                        for (int i = 0; i < args.Length; i++)
                        {
                            if (double.TryParse(args[i], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
                            {
                                if (parsed >= 10)
                                {
                                    steps = (int)Math.Round(parsed);
                                }
                                else
                                {
                                    threshold = parsed;
                                }
                            }
                            else if (args[i].ToLower() == "true" || args[i].ToLower() == "plot")
                            {
                                plot = true;
                            }
                            else if (args[i].ToLower() == "false" || args[i].ToLower() == "noplot")
                            {
                                plot = false;
                            }
                            else if (args[i].ToLower() == "value" || args[i].ToLower() == "val")
                            {
                                criterion = ConvergenceCriteria.Value;
                            }
                            else if (args[i].ToLower() == "variables" || args[i].ToLower() == "var")
                            {
                                criterion = ConvergenceCriteria.Variables;
                            }
                        }

                        return new RandomWalk(plot, criterion, threshold, steps);
                    }
                    else
                    {
                        return new RandomWalk(true, ConvergenceCriteria.Value, 0.001, 10000);
                    }
                case "nesterovclimbing":
                case "nesterov":
                case "nc":
                    if (args.Length > 0)
                    {
                        bool plot = true;
                        double threshold = 0.001;
                        int steps = 100;
                        ConvergenceCriteria criterion = ConvergenceCriteria.Value;

                        for (int i = 0; i < args.Length; i++)
                        {
                            if (double.TryParse(args[i], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
                            {
                                if (parsed >= 10)
                                {
                                    steps = (int)Math.Round(parsed);
                                }
                                else
                                {
                                    threshold = parsed;
                                }
                            }
                            else if (args[i].ToLower() == "true" || args[i].ToLower() == "plot")
                            {
                                plot = true;
                            }
                            else if (args[i].ToLower() == "false" || args[i].ToLower() == "noplot")
                            {
                                plot = false;
                            }
                            else if (args[i].ToLower() == "value" || args[i].ToLower() == "val")
                            {
                                criterion = ConvergenceCriteria.Value;
                            }
                            else if (args[i].ToLower() == "variables" || args[i].ToLower() == "var")
                            {
                                criterion = ConvergenceCriteria.Variables;
                            }
                        }

                        return new NesterovClimbing(plot, criterion, threshold, steps);
                    }
                    else
                    {
                        return new NesterovClimbing(true, ConvergenceCriteria.Value, 0.001, 100);
                    }
                case "sampling":
                case "s":
                    if (args.Length > 0)
                    {
                        bool plot = true;
                        double min = 1;
                        double max = 10;
                        double resolution = 0.1;

                        List<double> suppliedNumbers = new List<double>();

                        for (int i = 0; i < args.Length; i++)
                        {
                            if (double.TryParse(args[i], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
                            {
                                suppliedNumbers.Add(parsed);
                            }
                            else if (args[i].ToLower() == "true" || args[i].ToLower() == "plot")
                            {
                                plot = true;
                            }
                            else if (args[i].ToLower() == "false" || args[i].ToLower() == "noplot")
                            {
                                plot = false;
                            }
                        }

                        if (suppliedNumbers.Count == 1)
                        {
                            if (suppliedNumbers[0] < min)
                            {
                                resolution = suppliedNumbers[0];
                            }
                            else if (suppliedNumbers[0] < max)
                            {
                                min = suppliedNumbers[0];
                            }
                            else
                            {
                                max = suppliedNumbers[0];
                            }
                        }
                        else if (suppliedNumbers.Count == 2)
                        {
                            if (suppliedNumbers[0] > resolution && suppliedNumbers[1] > resolution)
                            {
                                min = suppliedNumbers.Min();
                                max = suppliedNumbers.Max();
                            }
                            else
                            {
                                throw new NotImplementedException("Could not understand parameters for " + sr + "!");
                            }
                        }
                        else if (suppliedNumbers.Count == 3)
                        {
                            resolution = suppliedNumbers[2];
                            min = suppliedNumbers[0];
                            max = suppliedNumbers[1];
                        }

                        return new Sampling(plot, min, max, resolution);
                    }
                    else
                    {
                        return new Sampling(true, 1, 10, 0.1);
                    }
                case "iterativesampling":
                case "is":
                    if (args.Length > 0)
                    {
                        bool plot = true;
                        double min = 1;
                        double max = 10;
                        double resolution = 0.1;
                        double threshold = 0.001;
                        ConvergenceCriteria criterion = ConvergenceCriteria.Value;

                        List<double> suppliedNumbers = new List<double>();

                        for (int i = 0; i < args.Length; i++)
                        {
                            if (double.TryParse(args[i], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
                            {
                                suppliedNumbers.Add(parsed);
                            }
                            else if (args[i].ToLower() == "true" || args[i].ToLower() == "plot")
                            {
                                plot = true;
                            }
                            else if (args[i].ToLower() == "false" || args[i].ToLower() == "noplot")
                            {
                                plot = false;
                            }
                            else if (args[i].ToLower() == "value" || args[i].ToLower() == "val")
                            {
                                criterion = ConvergenceCriteria.Value;
                            }
                            else if (args[i].ToLower() == "variables" || args[i].ToLower() == "var")
                            {
                                criterion = ConvergenceCriteria.Variables;
                            }
                        }

                        if (suppliedNumbers.Count == 1)
                        {
                            if (suppliedNumbers[0] < min)
                            {
                                resolution = suppliedNumbers[0];
                            }
                            else if (suppliedNumbers[0] < max)
                            {
                                min = suppliedNumbers[0];
                            }
                            else
                            {
                                max = suppliedNumbers[0];
                            }
                        }
                        else if (suppliedNumbers.Count == 2)
                        {
                            if (suppliedNumbers[0] > resolution && suppliedNumbers[1] > resolution)
                            {
                                min = suppliedNumbers.Min();
                                max = suppliedNumbers.Max();
                            }
                            else
                            {
                                throw new NotImplementedException("Could not understand parameters for " + sr + "!");
                            }
                        }
                        else if (suppliedNumbers.Count == 3)
                        {
                            resolution = suppliedNumbers[2];
                            min = suppliedNumbers[0];
                            max = suppliedNumbers[1];
                        }
                        else if (suppliedNumbers.Count == 4)
                        {
                            threshold = suppliedNumbers[3];
                            resolution = suppliedNumbers[2];
                            min = suppliedNumbers[0];
                            max = suppliedNumbers[1];
                        }

                        return new IterativeSampling(plot, min, max, resolution, criterion, threshold);
                    }
                    else
                    {
                        return new IterativeSampling(true, 1, 10, 0.1, ConvergenceCriteria.Value, 0.001);
                    }
                default:
                    throw new NotImplementedException(strategyName + " is not a recognised maximisation strategy!");
            }
        }
    }

    public class RandomWalk : MaximisationStrategy
    {
        public override Strategies Strategy => Strategies.RandomWalk;
        public override bool Plot { get; }
        public ConvergenceCriteria ConvergenceCriterion { get; }
        public double ConvergenceThreshold { get; }
        public int StepsPerRun { get; }

        public RandomWalk(bool plot, ConvergenceCriteria convergenceCriterion, double convergenceThreshold, int stepsPerRun)
        {
            Plot = plot;
            ConvergenceThreshold = convergenceThreshold;
            StepsPerRun = stepsPerRun;
            ConvergenceCriterion = convergenceCriterion;
        }

        public override string ToString()
        {
            return "RandomWalk(" + StepsPerRun.ToString() + "," + ConvergenceCriterion.ToString() + "," + ConvergenceThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + (Plot ? "plot" : "noplot") + ")";
        }
    }

    public class NesterovClimbing : MaximisationStrategy
    {
        public override Strategies Strategy => Strategies.NesterovClimbing;
        public override bool Plot { get; }
        public ConvergenceCriteria ConvergenceCriterion { get; }
        public double ConvergenceThreshold { get; }
        public int StepsPerRun { get; }

        public NesterovClimbing(bool plot, ConvergenceCriteria convergenceCriterion, double convergenceThreshold, int stepsPerRun)
        {
            Plot = plot;
            ConvergenceThreshold = convergenceThreshold;
            StepsPerRun = stepsPerRun;
            ConvergenceCriterion = convergenceCriterion;
        }

        public override string ToString()
        {
            return "NesterovClimbing(" + StepsPerRun.ToString() + "," + ConvergenceCriterion.ToString() + "," + ConvergenceThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + (Plot ? "plot" : "noplot") + ")";
        }
    }

    public class Sampling : MaximisationStrategy
    {
        public override Strategies Strategy => Strategies.Sampling;
        public override bool Plot { get; }
        public double Min { get; }
        public double Max { get; }
        public double Resolution { get; }

        public Sampling(bool plot, double min, double max, double resolution)
        {
            Plot = plot;
            Min = min;
            Max = max;
            Resolution = resolution;
        }

        public override string ToString()
        {
            return "Sampling(" + Min.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + Max.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + Resolution.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + (Plot ? "plot" : "noplot") + ")";
        }
    }

    public class IterativeSampling : MaximisationStrategy
    {
        public override Strategies Strategy => Strategies.IterativeSampling;
        public override bool Plot { get; }
        public double Min { get; }
        public double Max { get; }
        public double Resolution { get; }
        public ConvergenceCriteria ConvergenceCriterion { get; }
        public double ConvergenceThreshold { get; }

        public IterativeSampling(bool plot, double min, double max, double resolution, ConvergenceCriteria criterion, double threshold)
        {
            Plot = plot;
            Min = min;
            Max = max;
            Resolution = resolution;
            ConvergenceCriterion = criterion;
            ConvergenceThreshold = threshold;
        }

        public override string ToString()
        {
            return "IterativeSampling(" + Min.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + Max.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + Resolution.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + (ConvergenceCriterion != ConvergenceCriteria.Value ? ConvergenceCriterion.ToString() + "," : "") + ConvergenceThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + (Plot ? "plot" : "noplot") + ")";
        }
    }


    public enum ConvergenceCriteria { Value = 0, Variables = 1 }


    public static class FunctionGenerator
    {
        public static (Func<double, double, double>, double[]) GetFake2VariateLikelihood(double maxVariable)
        {
            (Func<double, double>, Polynomial) funcX = GetFunc(rnd.Next(3, 10), rnd.NextDouble() * maxVariable);
            Func<double, double> functionX = NormalizeFunc(funcX.Item1, maxVariable, 1000);

            (Func<double, double>, Polynomial) funcY = GetFunc(rnd.Next(3, 10), rnd.NextDouble() * maxVariable);
            Func<double, double> functionY = NormalizeFunc(funcY.Item1, maxVariable, 1000);

            double[] rootsX = (from el in funcX.Item2.Differentiate().Roots() where el.IsReal() select el.Real).ToArray();
            double[] rootsY = (from el in funcY.Item2.Differentiate().Roots() where el.IsReal() select el.Real).ToArray();

            int maxXInd = -1;
            int maxYInd = -1;

            double maxVal = double.MinValue;

            for (int x = 0; x < rootsX.Length; x++)
            {
                for (int y = 0; y < rootsY.Length; y++)
                {
                    double z = functionX(rootsX[x]) + functionY(rootsY[y]);
                    if (z > maxVal)
                    {
                        maxVal = z;
                        maxXInd = x;
                        maxYInd = y;
                    }
                }
            }

            return ((x, y) =>
            {
                return Math.Min(0, -1 / (functionX(x) + functionY(y) + 2) + 1 / (maxVal + 2));
            }
            , new double[] { rootsX[maxXInd], rootsY[maxYInd] });
        }


        static Func<double, double> NormalizeFunc(Func<double, double> func, double range, int resolution)
        {
            double min = double.MaxValue;
            double max = double.MinValue;

            for (int i = 0; i < resolution; i++)
            {
                double x = i * range / (resolution - 1);
                double y = func(x);

                min = Math.Min(y, min);
                max = Math.Max(y, max);
            }

            return x =>
            {
                return (func(x) - min) / (max - min) * 2 - 1;
            };
        }

        static Random rnd = new Random();

        static (Func<double, double>, Polynomial) GetFunc(int pointCount, double xRange)
        {
            List<double[]> points = new List<double[]>();

            for (int i = 0; i < pointCount; i++)
            {
                double x = rnd.NextDouble() * xRange;
                double y = -1.0 / rnd.NextDouble() + 1;

                points.Add(new double[] { x, y });
            }

            Polynomial pol = Polynomial.Fit((from el in points select el[0]).ToArray(), (from el in points select el[1]).ToArray(), points.Count - 1);
            Polynomial diff = pol.Differentiate();


            double minX = (from el in points select el[0]).Min();
            double minY = (from el in points orderby el[0] ascending select el[1]).First();

            double maxX = (from el in points select el[0]).Max();
            double maxY = (from el in points orderby el[0] ascending select el[1]).Last();

            if (diff.Evaluate(minX) < 0 && diff.Evaluate(maxX) > 0)
            {
                pol = new Polynomial(from el in pol.Coefficients select -el);
                points = (from el in points select new double[] { el[0], pol.Evaluate(el[0]) }).ToList();
                diff = pol.Differentiate();
            }

            while (diff.Evaluate(minX) < 0 || diff.Evaluate(maxX) > 0)
            {
                points = new List<double[]>();

                for (int i = 0; i < pointCount; i++)
                {
                    double x = rnd.NextDouble() * xRange;
                    double y = -1.0 / rnd.NextDouble() + 1;

                    points.Add(new double[] { x, y });
                }

                pol = Polynomial.Fit((from el in points select el[0]).ToArray(), (from el in points select el[1]).ToArray(), points.Count - 1);
                diff = pol.Differentiate();


                minX = (from el in points select el[0]).Min();
                minY = (from el in points orderby el[0] ascending select el[1]).First();

                maxX = (from el in points select el[0]).Max();
                maxY = (from el in points orderby el[0] ascending select el[1]).Last();

                if (diff.Evaluate(minX) < 0 && diff.Evaluate(maxX) > 0)
                {
                    pol = new Polynomial(from el in pol.Coefficients select -el);
                    points = (from el in points select new double[] { el[0], pol.Evaluate(el[0]) }).ToList();
                    diff = pol.Differentiate();
                }
            }

            minX = (from el in points select el[0]).Min();
            minY = (from el in points orderby el[0] ascending select el[1]).First();

            maxX = (from el in points select el[0]).Max();
            maxY = (from el in points orderby el[0] ascending select el[1]).Last();

            double minJoin = -diff.Evaluate(minX) / Math.Exp(-minX);
            double minShift = minY - minJoin * Math.Exp(-minX);

            double maxJoin = diff.Evaluate(maxX) / Math.Exp(maxX);
            double maxShift = maxY - maxJoin * Math.Exp(maxX);

            return (x =>
            {
                if (x >= minX && x <= maxX)
                {
                    return pol.Evaluate(x);
                }
                else if (x < minX)
                {
                    return minJoin * Math.Exp(-x) + minShift;
                }
                else
                {
                    return maxJoin * Math.Exp(x) + maxShift;
                }
            }
            , pol);

        }

    }

    public class Parameter
    {
        private double realValue;
        public double Value
        {

            get
            {
                if (this.Action != ParameterAction.Equal)
                {
                    return realValue;
                }
                else
                {
                    return this.EqualParameter.Value;
                }
            }

            set
            {
                realValue = value;
            }
        }
        public double DistributionParameter { get; set; }
        public Parameter EqualParameter { get; set; }
        public enum ParameterAction { Fix, ML, Bayes, Equal, Dirichlet, Multinomial }
        public ParameterAction Action { get; set; }
        public IContinuousDistribution PriorDistribution { get; set; }

        private string distributionString;

        public static implicit operator double(Parameter prm)
        {
            return prm.Value;
        }

        public Parameter(double value)
        {
            Value = value;
            Action = ParameterAction.Fix;
        }

        public Parameter()
        {
            Value = 1;
            Action = ParameterAction.ML;
        }

        public Parameter(IContinuousDistribution priorDistribution)
        {
            Value = priorDistribution.Sample();
            PriorDistribution = priorDistribution;
            Action = ParameterAction.Bayes;
        }

        public string ToString(Dictionary<string, Parameter> parameterDictionary)
        {
            switch (this.Action)
            {
                case ParameterAction.Fix:
                    return this.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case ParameterAction.ML:
                    return "ML";
                case ParameterAction.Equal:
                    return "Equal(" + parameterDictionary.GetKey(this.EqualParameter) + ")";
                case ParameterAction.Multinomial:
                    return "Multinomial(" + this.DistributionParameter.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
                case ParameterAction.Dirichlet:
                    return "Dirichlet(" + this.DistributionParameter.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
                case ParameterAction.Bayes:
                    return this.PriorDistribution.ToString();
            }

            return base.ToString();
        }

        public static Parameter DirichletParameter(double parameter)
        {
            return new Parameter() { Action = ParameterAction.Dirichlet, DistributionParameter = parameter };
        }

        public static Parameter MultinomialParameter(double parameter)
        {
            return new Parameter() { Action = ParameterAction.Multinomial, DistributionParameter = parameter };
        }

        public static Parameter Parse(string sr, Random randomSource)
        {
            if (sr.ToLower().StartsWith("equal"))
            {
                throw new Exception("Invalid parameter type!");
            }

            if (sr.ToLower() == "ml")
            {
                return new Parameter();
            }
            else if (double.TryParse(sr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
            {
                return new Parameter(parsed);
            }
            else if (sr.ToLower().StartsWith("dirichlet"))
            {
                string parVal = sr.Substring(sr.IndexOf("(") + 1);
                parVal = parVal.Substring(0, parVal.IndexOf(")"));
                return DirichletParameter(double.Parse(parVal, System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (sr.ToLower().StartsWith("multinomial"))
            {
                string parVal = sr.Substring(sr.IndexOf("(") + 1);
                parVal = parVal.Substring(0, parVal.IndexOf(")"));
                return MultinomialParameter(double.Parse(parVal, System.Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                return new Parameter(Utils.ParseDistribution(sr, randomSource)) { distributionString = sr };
            }
        }

        public static Parameter ParseEqual(string sr, Dictionary<string, Parameter> parameters)
        {
            if (!sr.ToLower().StartsWith("equal"))
            {
                throw new Exception("Invalid parameter type!");
            }

            string originalSr = sr;

            sr = sr.Substring(sr.IndexOf("(") + 1);
            sr = sr.Substring(0, sr.IndexOf(")")).Trim(' ');

            if (!parameters.ContainsKey(sr))
            {
                throw new Exception("Unknown parameter " + sr + "!");
            }

            return new Parameter(parameters[sr].Value) { Action = ParameterAction.Equal, EqualParameter = parameters[sr], distributionString = originalSr };
        }

        public static Dictionary<string, Parameter> CloneParameterDictionary(Dictionary<string, Parameter> subject)
        {
            Dictionary<string, Parameter> tbr = new Dictionary<string, Parameter>();

            foreach (KeyValuePair<string, Parameter> kvp in subject)
            {
                switch (kvp.Value.Action)
                {
                    case ParameterAction.Fix:
                        tbr.Add(kvp.Key, new Parameter(kvp.Value.Value));
                        break;
                    case ParameterAction.ML:
                        tbr.Add(kvp.Key, new Parameter() { Value = kvp.Value.Value });
                        break;
                    case ParameterAction.Dirichlet:
                        tbr.Add(kvp.Key, DirichletParameter(kvp.Value.DistributionParameter));
                        break;
                    case ParameterAction.Bayes:
                        tbr.Add(kvp.Key, Parse(kvp.Value.distributionString, kvp.Value.PriorDistribution.RandomSource));
                        break;
                    case ParameterAction.Multinomial:
                        tbr.Add(kvp.Key, MultinomialParameter(kvp.Value.DistributionParameter));
                        break;
                }
            }

            foreach (KeyValuePair<string, Parameter> kvp in subject)
            {
                switch (kvp.Value.Action)
                {
                    case ParameterAction.Equal:
                        tbr.Add(kvp.Key, ParseEqual(kvp.Value.distributionString, tbr));
                        break;
                }
            }

            return tbr;
        }
    }



    public class CharacterDependency
    {
        public enum Types { Independent, Dependent, Conditioned };
        public Types Type { get; }
        public int Index { get; }
        public int[] Dependencies { get; }
        public Dictionary<string, Parameter> ConditionedProbabilities { get; set; }
        public string InputDependencyName { get; set; }

        public CharacterDependency(int index)
        {
            Index = index;
            Type = Types.Independent;
        }

        public CharacterDependency(int index, Types type, int[] dependencies, Dictionary<string, Parameter> conditionedProbabilities)
        {
            Index = index;
            Type = type;
            Dependencies = dependencies;
            ConditionedProbabilities = conditionedProbabilities;
        }



        public static CharacterDependency[] Parse(string[] dependencyBlock, string[][] states, Random randomSource)
        {
            for (int i = 0; i < dependencyBlock.Length; i++)
            {
                dependencyBlock[i] = dependencyBlock[i].Replace("\t", " ");
                while (dependencyBlock[i].Contains("  "))
                {
                    dependencyBlock[i] = dependencyBlock[i].Replace("  ", " ");
                }
                dependencyBlock[i].Replace(" ;", ";");
                dependencyBlock[i] = dependencyBlock[i].Trim(' ');
                dependencyBlock[i] = Utils.FixString(dependencyBlock[i]);
            }

            dependencyBlock = (from el in dependencyBlock where !string.IsNullOrEmpty(el) select el).ToArray();

            if (!dependencyBlock[0].ToLower().StartsWith("begin dependency;") || !dependencyBlock.Last().ToLower().StartsWith("end;"))
            {
                throw new Exception("Invalid dependency block!");
            }

            List<CharacterDependency> tbr = new List<CharacterDependency>();

            for (int i = 1; i < dependencyBlock.Length - 1; i++)
            {
                if (dependencyBlock[i].ToLower().StartsWith("independent"))
                {
                    string chars = dependencyBlock[i].Substring(dependencyBlock[i].IndexOf(":") + 1);
                    chars = chars.Substring(0, chars.IndexOf(";")).Trim(' ');
                    tbr.AddRange(from el in chars.Split(',') select new CharacterDependency(int.Parse(el.Trim(' '))));
                }
                else if (dependencyBlock[i].ToLower().StartsWith("dependent"))
                {
                    string chars = dependencyBlock[i].Substring(dependencyBlock[i].IndexOf(":") + 1);
                    chars = chars.Substring(0, chars.IndexOf(";")).Trim(' ');
                    int[] dependencyList = (from el in chars.Split(',') select int.Parse(el.Trim(' '))).ToArray();
                    Dictionary<string, Parameter> rates = new Dictionary<string, Parameter>();

                    string[][] involvedStates = (from el in dependencyList select states[el]).ToArray();

                    string[][] combinations = Utils.GetCombinations(involvedStates);

                    for (int j = 0; j < combinations.Length; j++)
                    {
                        for (int k = 0; k < combinations.Length; k++)
                        {
                            if (j != k)
                            {
                                string rateName = Utils.StringifyArray(combinations[j]) + ">" + Utils.StringifyArray(combinations[k]);
                                rates.Add(rateName, new Parameter());
                            }
                        }
                        rates.Add(Utils.StringifyArray(combinations[j]), new Parameter(1.0 / combinations.Length));
                    }

                    if (dependencyBlock[i + 1].ToLower().StartsWith("pi"))
                    {
                        int ind = i + 2;

                        if (dependencyBlock[ind].ToLower().StartsWith("default"))
                        {
                            string defaultRate = dependencyBlock[ind].Substring(dependencyBlock[ind].IndexOf(":") + 1).Trim(' ');
                            for (int j = 0; j < combinations.Length; j++)
                            {
                                string rateName = Utils.StringifyArray(combinations[j]);
                                rates[rateName] = Parameter.Parse(defaultRate, randomSource);
                            }
                            ind++;
                        }

                        if (dependencyBlock[ind].ToLower().StartsWith("fixed:") || dependencyBlock[ind].ToLower().StartsWith("fix:"))
                        {
                            ind++;

                            Dictionary<string, string> equalPis = new Dictionary<string, string>();

                            while (!dependencyBlock[ind].StartsWith(";"))
                            {
                                string rateName = dependencyBlock[ind].Substring(0, dependencyBlock[ind].IndexOf(":")).Replace(" ", "");
                                string rateVal = dependencyBlock[ind].Substring(dependencyBlock[ind].IndexOf(":") + 1).Replace(" ", "");
                                if (!rateVal.ToLower().StartsWith("equal"))
                                {
                                    rates[rateName] = Parameter.Parse(rateVal, randomSource);
                                }
                                else
                                {
                                    equalPis.Add(rateName, rateVal);
                                }
                                ind++;
                            }

                            foreach (KeyValuePair<string, string> kvp in equalPis)
                            {
                                rates[kvp.Key] = Parameter.ParseEqual(kvp.Value, rates);
                            }

                            ind++;
                        }

                        if (dependencyBlock[ind].ToLower().StartsWith("dirichlet:"))
                        {
                            ind++;

                            while (!dependencyBlock[ind].StartsWith(";"))
                            {
                                string rateName = dependencyBlock[ind].Substring(0, dependencyBlock[ind].IndexOf(":")).Replace(" ", "");
                                string rateVal = dependencyBlock[ind].Substring(dependencyBlock[ind].IndexOf(":") + 1).Replace(" ", "");
                                rates[rateName] = Parameter.DirichletParameter(double.Parse(rateVal, System.Globalization.CultureInfo.InvariantCulture));
                                ind++;
                            }

                            ind++;
                        }

                        i = ind;
                    }

                    if (dependencyBlock[i + 1].ToLower().StartsWith("rates"))
                    {
                        int ind = i + 2;

                        if (dependencyBlock[ind].ToLower().StartsWith("default"))
                        {
                            string defaultRate = dependencyBlock[ind].Substring(dependencyBlock[ind].IndexOf(":") + 1).Trim(' ');
                            for (int j = 0; j < combinations.Length; j++)
                            {
                                for (int k = 0; k < combinations.Length; k++)
                                {
                                    if (j != k)
                                    {
                                        string rateName = Utils.StringifyArray(combinations[j]) + ">" + Utils.StringifyArray(combinations[k]);
                                        rates[rateName] = Parameter.Parse(defaultRate, randomSource);
                                    }
                                }
                            }
                            ind++;
                        }

                        Dictionary<string, string> equalRates = new Dictionary<string, string>();

                        while (!dependencyBlock[ind].StartsWith(";"))
                        {
                            string rateName = dependencyBlock[ind].Substring(0, dependencyBlock[ind].IndexOf(":")).Replace(" ", "");
                            string rateVal = dependencyBlock[ind].Substring(dependencyBlock[ind].IndexOf(":") + 1).Replace(" ", "");
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
                    }

                    tbr.Add(new CharacterDependency(-1, Types.Dependent, dependencyList, rates));

                }
                else if (dependencyBlock[i].ToLower().StartsWith("conditioned"))
                {
                    string chars = dependencyBlock[i].Substring(dependencyBlock[i].IndexOf(":") + 1);
                    chars = chars.Substring(0, chars.IndexOf(";")).Trim(' ');

                    int condChar = int.Parse(chars.Substring(0, chars.IndexOf("|")).Trim(' '));
                    int[] dependencyChars = (from el in chars.Substring(chars.IndexOf("|") + 1).Split(',') select int.Parse(el.Trim(' '))).ToArray();

                    Dictionary<string, Parameter> condProbs = new Dictionary<string, Parameter>();
                    string defaultProb = "ML";

                    if (dependencyBlock[i + 1].ToLower().StartsWith("default"))
                    {
                        defaultProb = dependencyBlock[i + 1].Substring(dependencyBlock[i + 1].IndexOf(":") + 1).Trim(' ');
                        i++;
                    }

                    string[][] involvedStates = Utils.GetCombinations((from el in dependencyChars select states[el]).ToArray());

                    for (int j = 0; j < involvedStates.Length; j++)
                    {
                        for (int k = 0; k < states[condChar].Length; k++)
                        {
                            string condProbName = Utils.StringifyArray(involvedStates[j]) + ">" + states[condChar][k];
                            condProbs.Add(condProbName, Parameter.Parse(defaultProb, randomSource));
                        }
                    }


                    if (dependencyBlock[i + 1].ToLower().StartsWith("probs"))
                    {
                        int ind = i + 2;

                        Dictionary<string, string> equalProbs = new Dictionary<string, string>();

                        while (!dependencyBlock[ind].StartsWith(";"))
                        {
                            string rateName = dependencyBlock[ind].Substring(0, dependencyBlock[ind].IndexOf(":")).Replace(" ", "");
                            string rateVal = dependencyBlock[ind].Substring(dependencyBlock[ind].IndexOf(":") + 1).Replace(" ", "");
                            if (!rateVal.ToLower().StartsWith("equal"))
                            {
                                condProbs[rateName] = Parameter.Parse(rateVal, randomSource);
                            }
                            else
                            {
                                equalProbs.Add(rateName, rateVal);
                            }
                            ind++;
                        }

                        foreach (KeyValuePair<string, string> kvp in equalProbs)
                        {
                            condProbs[kvp.Key] = Parameter.ParseEqual(kvp.Value, condProbs);
                        }
                    }

                    Dictionary<string, double> fixedProbs = new Dictionary<string, double>();

                    foreach (KeyValuePair<string, Parameter> el in condProbs)
                    {
                        if (el.Value.Action == Parameter.ParameterAction.Fix)
                        {
                            if (el.Value < 0)
                            {
                                throw new Exception("Negative probability specified for state " + el.Key + "!");
                            }

                            if (!fixedProbs.ContainsKey(el.Key.Substring(0, el.Key.IndexOf(">"))))
                            {
                                fixedProbs.Add(el.Key.Substring(0, el.Key.IndexOf(">")), el.Value.Value);
                            }
                            else
                            {
                                fixedProbs[el.Key.Substring(0, el.Key.IndexOf(">"))] += el.Value.Value;
                            }
                        }
                    }

                    foreach (KeyValuePair<string, double> el in fixedProbs)
                    {
                        if (el.Value > 1)
                        {
                            throw new Exception("The sum of conditioned probabilities for state " + el.Key + " is > 1!");
                        }
                    }

                    tbr.Add(new CharacterDependency(condChar, Types.Conditioned, dependencyChars, condProbs));
                }
            }

            return tbr.ToArray();
        }


        public string ToString(CharacterDependency[] dependencies, bool rates, bool pis, bool condProbs)
        {
            string tbr = "";

            switch (this.Type)
            {
                case Types.Independent:
                    tbr += "\tIndependent: " + this.Index.ToString() + (string.IsNullOrEmpty(this.InputDependencyName) ? "" : " [Original: " + this.InputDependencyName + "]") + ";\n";
                    break;
                case Types.Dependent:
                    tbr += "\tDependent: " + (from el in this.Dependencies select el.ToString()).Aggregate((a, b) => a + ", " + b) + ";\n";
                    if (this.ConditionedProbabilities.Count > 0)
                    {
                        if (pis)
                        {
                            tbr += "\n\tPis:\n";

                            bool hasFixed = false;
                            bool hasDirichlet = false;

                            foreach (KeyValuePair<string, Parameter> kvp in this.ConditionedProbabilities)
                            {
                                if (kvp.Value.Action == Parameter.ParameterAction.Fix || kvp.Value.Action == Parameter.ParameterAction.Equal)
                                {
                                    hasFixed = true;
                                }

                                if (kvp.Value.Action == Parameter.ParameterAction.Dirichlet)
                                {
                                    hasDirichlet = true;
                                }
                            }

                            if (hasFixed)
                            {
                                tbr += "\t\tFixed:\n";
                                foreach (KeyValuePair<string, Parameter> kvp in this.ConditionedProbabilities)
                                {
                                    if (!kvp.Key.Contains(">") && kvp.Value.Action == Parameter.ParameterAction.Fix)
                                    {
                                        tbr += "\t\t\t" + kvp.Key + ": " + kvp.Value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n";
                                    }
                                    else if (!kvp.Key.Contains(">") && kvp.Value.Action == Parameter.ParameterAction.Equal)
                                    {
                                        tbr += "\t\t\t" + kvp.Key + ": " + kvp.Value.ToString(this.ConditionedProbabilities) + "\n";
                                    }
                                }

                                tbr += "\t\t;\n";
                            }

                            if (hasDirichlet)
                            {
                                tbr += "\t\tDirichlet:\n";
                                foreach (KeyValuePair<string, Parameter> kvp in this.ConditionedProbabilities)
                                {
                                    if (!kvp.Key.Contains(">") && kvp.Value.Action == Parameter.ParameterAction.Dirichlet)
                                    {
                                        tbr += "\t\t\t" + kvp.Key + ": " + kvp.Value.DistributionParameter.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n";
                                    }
                                }

                                tbr += "\t\t;\n";
                            }

                            tbr += "\t;\n";
                        }

                        if (rates)
                        {
                            tbr += "\n\tRates:\n";
                            foreach (KeyValuePair<string, Parameter> kvp in this.ConditionedProbabilities)
                            {
                                if (kvp.Key.Contains(">"))
                                {
                                    tbr += "\t\t" + kvp.Key.Replace(">", " > ") + ": " + kvp.Value.ToString(this.ConditionedProbabilities) + "\n";
                                }
                            }
                            tbr += "\t;\n";
                        }
                    }
                    break;
                case Types.Conditioned:
                    string originalDep = "";

                    if (!string.IsNullOrEmpty(this.InputDependencyName))
                    {
                        originalDep = " [Original: " + this.InputDependencyName + "]";
                    }

                    List<string> originals = new List<string>();

                    bool anyOriginal = false;

                    try
                    {
                        for (int i = 0; i < this.Dependencies.Length; i++)
                        {
                            if (!string.IsNullOrEmpty(dependencies[this.Dependencies[i]].InputDependencyName))
                            {
                                originals.Add(dependencies[this.Dependencies[i]].InputDependencyName);
                                anyOriginal = true;
                            }
                            else
                            {
                                originals.Add(dependencies[this.Dependencies[i]].Index.ToString());
                            }
                        }
                    }
                    catch
                    {

                    }

                    string originalCond = "";

                    if (anyOriginal)
                    {
                        originalCond = " [Original: " + originals.Aggregate((a, b) => a + ", " + b) + "]";
                    }

                    tbr += "\tConditioned: " + this.Index.ToString() + originalDep + " | " + (from el in this.Dependencies select el.ToString()).Aggregate((a, b) => a + ", " + b) + originalCond + ";\n";
                    if (this.ConditionedProbabilities.Count > 0 && condProbs)
                    {
                        tbr += "\n\tProbs:\n";
                        foreach (KeyValuePair<string, Parameter> kvp in this.ConditionedProbabilities)
                        {
                            tbr += "\t\t" + kvp.Key + ": " + kvp.Value.ToString(this.ConditionedProbabilities) + "\n";
                        }
                        tbr += "\t;\n";
                    }
                    break;
            }

            return tbr;
        }

    }

    public class FoldedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        internal Dictionary<TKey, TValue[]> internalDictionary;
        internal int internalIndex;

        public TValue this[TKey index]
        {
            get
            {
                return internalDictionary[index][internalIndex];
            }
            set
            {

            }
        }

        public ICollection<TKey> Keys => throw new NotImplementedException();

        public ICollection<TValue> Values => throw new NotImplementedException();

        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(TKey key, TValue value)
        {
            throw new NotImplementedException();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(TKey key)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool Remove(TKey key)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public static class Extensions
    {
        public static double Modulus(this double[] v1)
        {
            double tbr = 0;

            for (int i = 0; i < v1.Length; i++)
            {
                tbr += v1[i] * v1[i];
            }

            return Math.Sqrt(tbr);
        }

        public static double Distance(this double[] v1, double[] v2)
        {
            double tbr = 0;

            for (int i = 0; i < v1.Length; i++)
            {
                tbr += (v1[i] - v2[i]) * (v1[i] - v2[i]);
            }

            return Math.Sqrt(tbr);
        }

        public static double[] Normalize(this double[] v1, double? length = null)
        {
            double modulus = length ?? v1.Modulus();

            return v1.Multiply(1 / modulus);
        }

        public static void WriteLong(this Stream sr, long value)
        {
            sr.WriteByte((byte)((value >> 56) & 255));
            sr.WriteByte((byte)((value >> 48) & 255));
            sr.WriteByte((byte)((value >> 40) & 255));
            sr.WriteByte((byte)((value >> 32) & 255));
            sr.WriteByte((byte)((value >> 24) & 255));
            sr.WriteByte((byte)((value >> 16) & 255));
            sr.WriteByte((byte)((value >> 8) & 255));
            sr.WriteByte((byte)(value & 255));
        }

        public static long ReadLong(this Stream sr)
        {
            long tbr = 0;
            tbr += (long)sr.ReadByte() << 56;
            tbr += (long)sr.ReadByte() << 48;
            tbr += (long)sr.ReadByte() << 40;
            tbr += (long)sr.ReadByte() << 32;
            tbr += (long)sr.ReadByte() << 24;
            tbr += (long)sr.ReadByte() << 16;
            tbr += (long)sr.ReadByte() << 8;
            tbr += (long)sr.ReadByte();
            return tbr;
        }

        public static void WriteInt(this Stream sr, int value)
        {
            sr.WriteByte((byte)((value >> 24) & 255));
            sr.WriteByte((byte)((value >> 16) & 255));
            sr.WriteByte((byte)((value >> 8) & 255));
            sr.WriteByte((byte)(value & 255));
        }

        public static int ReadInt(this Stream sr)
        {
            int tbr = 0;
            tbr += sr.ReadByte() << 24;
            tbr += sr.ReadByte() << 16;
            tbr += sr.ReadByte() << 8;
            tbr += sr.ReadByte();
            return tbr;
        }

        //Adapted from https://referencesource.microsoft.com/#mscorlib/system/io/stream.cs,98ac7cf3acb04bb1
        public static void CopySomeTo(this Stream sr, Stream destination, long howMany)
        {
            byte[] buffer = new byte[81920];
            int read;
            while ((read = sr.Read(buffer, 0, (int)Math.Min(buffer.Length, howMany))) != 0)
            {
                destination.Write(buffer, 0, read);
                howMany -= read;
            }
        }

        public static TreeNode GetRootedTreeConsensus(this IEnumerable<TreeNode> trees, bool clockLike, bool useMedian, double threshold)
        {
            Dictionary<string, List<double>> splits = new Dictionary<string, List<double>>();

            int totalTrees = 0;

            Split.LengthTypes lengthType = clockLike ? Split.LengthTypes.Age : Split.LengthTypes.Length;

            foreach (TreeNode tree in trees)
            {
                List<Split> treeSplits = tree.GetSplits(lengthType);

                for (int i = 0; i < treeSplits.Count; i++)
                {
                    List<double> splitLengths;

                    if (splits.TryGetValue(treeSplits[i].Name, out splitLengths))
                    {
                        splits[treeSplits[i].Name].Add(treeSplits[i].Length);
                    }
                    else
                    {
                        splits.Add(treeSplits[i].Name, new List<double>() { treeSplits[i].Length });
                    }
                }

                totalTrees++;
            }

            List<Split> orderedSplits = new List<Split>(from el in splits orderby el.Value.Count descending where ((double)el.Value.Count / (double)totalTrees) >= threshold select new Split(el.Key, useMedian ? el.Value.Median() : el.Value.Average(), lengthType, ((double)el.Value.Count / (double)totalTrees)));

            List<Split> finalSplits = new List<Split>();

            for (int i = 0; i < orderedSplits.Count; i++)
            {
                if (orderedSplits[i].IsCompatible(finalSplits))
                {
                    finalSplits.Add(orderedSplits[i]);
                }
            }

            return TreeNode.BuildRooted(finalSplits);
        }

        public static T[] Fill<T>(this T[] array, T value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
            return array;
        }

        public static T[,] Fill<T>(this T[,] array, T value)
        {
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    array[i, j] = value;
                }
            }
            return array;
        }

        public static bool ContainsAny<T>(this IEnumerable<T> array1, IEnumerable<T> array2)
        {
            foreach (T t in array2)
            {
                if (array1.Contains(t))
                {
                    return true;
                }
            }

            return false;
        }

        public static double Median(this IEnumerable<double> array)
        {
            List<double> ordered = new List<double>(array);
            ordered.Sort();

            if (ordered.Count % 2 == 0)
            {
                return 0.5 * (ordered[ordered.Count / 2] + ordered[ordered.Count / 2 - 1]);
            }
            else
            {
                return ordered[ordered.Count / 2];
            }
        }

        public static double GetMedian(this IContinuousDistribution distrib)
        {
            try
            {
                return distrib.Median;
            }
            catch
            {
                double leftBound = 0;
                double rightBound = 0;

                Stopwatch sw = new Stopwatch();

                sw.Start();

                while (distrib.CumulativeDistribution(leftBound) > 0.5)
                {
                    leftBound--;

                    if (sw.ElapsedMilliseconds > 1000)
                    {
                        break;
                    }
                }

                sw.Reset();
                sw.Start();

                while (distrib.CumulativeDistribution(rightBound) < 0.5 || double.IsNaN(distrib.CumulativeDistribution(rightBound)))
                {
                    rightBound++;

                    if (sw.ElapsedMilliseconds > 1000)
                    {
                        break;
                    }
                }

                sw.Stop();

                double median = (leftBound + rightBound) * 0.5;

                double val = distrib.CumulativeDistribution(median);

                while (Math.Abs(val - 0.5) * 2 >= 0.001)
                {
                    double prevLB = leftBound;
                    double prevRB = rightBound;

                    if (val < 0.5)
                    {
                        leftBound = median;
                        median = (leftBound + rightBound) * 0.5;
                        val = distrib.CumulativeDistribution(median);
                    }
                    else
                    {
                        rightBound = median;
                        median = (leftBound + rightBound) * 0.5;
                        val = distrib.CumulativeDistribution(median);
                    }

                    if (leftBound == prevLB && rightBound == prevRB)
                    {
                        throw new Exception("Invalid parametrization of the distribution");
                    }
                }

                return median;
            }
        }

        public static double[] GetRange(this IContinuousDistribution distrib, double area)
        {
            double median = distrib.GetMedian();

            double min = median;

            double tails = (1 - area) / 2;

            if (distrib.CumulativeDistribution(0) >= tails)
            {
                min = 0;
            }
            else
            {
                double leftBound = 0;

                double rightBound = median;

                min = (leftBound + rightBound) * 0.5;

                double val = distrib.CumulativeDistribution(min);

                while (Math.Abs((val - tails) / tails) >= 0.1)
                {
                    double prevLB = leftBound;
                    double prevRB = rightBound;

                    if (val < tails)
                    {
                        leftBound = min;
                        min = (min + rightBound) * 0.5;
                        val = distrib.CumulativeDistribution(min);
                    }
                    else
                    {
                        rightBound = min;
                        min = (min + leftBound) * 0.5;
                        val = distrib.CumulativeDistribution(min);
                    }

                    if (leftBound == prevLB && rightBound == prevRB)
                    {
                        throw new Exception("Invalid parametrization of the distribution");
                    }
                }
            }

            double max = median;

            {
                double leftBound = median;

                double rightBound = median;

                double val = 1 - distrib.CumulativeDistribution(rightBound);

                Stopwatch sw = new Stopwatch();

                sw.Start();

                while (val > tails)
                {
                    rightBound++;
                    val = 1 - distrib.CumulativeDistribution(rightBound);

                    if (sw.ElapsedMilliseconds > 1000)
                    {
                        break;
                    }
                }

                sw.Stop();

                max = (leftBound + rightBound) * 0.5;

                val = 1 - distrib.CumulativeDistribution(max);

                while (Math.Abs((val - tails) / tails) >= 0.1)
                {
                    double prevLB = leftBound;
                    double prevRB = rightBound;

                    if (val > tails)
                    {
                        leftBound = max;
                        max = (max + rightBound) * 0.5;
                        val = 1 - distrib.CumulativeDistribution(max);
                    }
                    else
                    {
                        rightBound = max;
                        max = (max + leftBound) * 0.5;
                        val = 1 - distrib.CumulativeDistribution(max);
                    }

                    if (leftBound == prevLB && rightBound == prevRB)
                    {
                        throw new Exception("Invalid parametrization of the distribution");
                    }
                }


            }

            return new double[] { min, max };
        }

        public static int FindDependencyWithIndex(this CharacterDependency[] array, int index)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].Index == index)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int MaxInd(this double[] arr)
        {
            int tbr = 0;

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] > arr[tbr])
                {
                    tbr = i;
                }
            }

            return tbr;
        }

        public static int MaxInd(this double[] arr, double[] multipArr)
        {
            int tbr = 0;

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] > arr[tbr] && multipArr[i] > 0)
                {
                    tbr = i;
                }
            }

            return tbr;
        }

        public static double[] Multiply(this double[] a, double b)
        {
            double[] tbr = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
            {
                tbr[i] = a[i] * b;
            }
            return tbr;
        }

        public static double[] Subtract(this double[] a, double[] b)
        {
            if (a.Length != b.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            double[] tbr = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
            {
                tbr[i] = a[i] - b[i];
            }
            return tbr;
        }

        public static double[] Add(this double[] a, double[] b)
        {
            if (a.Length != b.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            double[] tbr = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
            {
                tbr[i] = a[i] + b[i];
            }
            return tbr;
        }

        public static int IndexOf<T>(this T[] array, T obj)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].Equals(obj))
                {
                    return i;
                }
            }
            return -1;
        }


        public static TKey GetKey<TKey, TValue>(this Dictionary<TKey, TValue> dict, TValue obj)
        {
            foreach (KeyValuePair<TKey, TValue> kvp in dict)
            {
                if (kvp.Value.Equals(obj))
                {
                    return kvp.Key;
                }
            }

            return default(TKey);
        }

        public static int GetKeyIndex<TKey, TValue>(this Dictionary<TKey, TValue> dict, TValue obj)
        {
            int tbr = 0;
            foreach (KeyValuePair<TKey, TValue> kvp in dict)
            {
                if (kvp.Value.Equals(obj))
                {
                    return tbr;
                }
                else
                {
                    tbr++;
                }
            }

            return -1;
        }

        public static string ToString(this double val, int significantDigits, bool decimalDigits = true)
        {
            if (decimalDigits)
            {
                if (val == 0 || double.IsNaN(val) || double.IsInfinity(val))
                {
                    return val.ToString("0." + new string('0', significantDigits), System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (val >= 1)
                {
                    return val.ToString("0." + new string('0', significantDigits), System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    int OoM = -(int)Math.Floor(Math.Log10(Math.Abs(val)));

                    if (OoM + significantDigits <= 8)
                    {
                        return val.ToString("0." + new string('0', OoM + significantDigits - 1), System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        return (val * Math.Pow(10, OoM)).ToString("0." + new string('0', significantDigits), System.Globalization.CultureInfo.InvariantCulture) + "e-" + OoM.ToString();
                    }
                }
            }
            else
            {
                if (val == 0 || double.IsNaN(val) || double.IsInfinity(val))
                {
                    return "0";
                }
                else if (val >= 1)
                {
                    int OoM = (int)Math.Floor(Math.Log10(Math.Abs(val)));

                    val = Math.Round(val / Math.Pow(10, OoM - significantDigits + 1)) * Math.Pow(10, OoM - significantDigits + 1);

                    if (OoM + 1 >= significantDigits)
                    {
                        return val.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        return val.ToString("0." + new string('0', significantDigits - OoM - 1));
                    }
                }
                else
                {
                    int OoM = -(int)Math.Floor(Math.Log10(Math.Abs(val)));

                    if (OoM + significantDigits <= 8)
                    {
                        return val.ToString("0." + new string('0', OoM + significantDigits - 1), System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        for (int i = 0; i < OoM; i++)
                        {
                            val *= 10;
                        }
                        return val.ToString("0." + new string('0', significantDigits), System.Globalization.CultureInfo.InvariantCulture) + "e-" + OoM.ToString();
                    }
                }
            }
        }

        public static (double Mean, double Variance) MeanAndVariance(this IEnumerable<double> values)
        {
            double mean = 0;
            double variance = 0;
            double meanSq = 0;
            int count = 0;

            foreach (double v in values)
            {
                mean += v;
                meanSq += v * v;
                count++;
            }

            mean /= count;
            meanSq /= count;

            variance = meanSq - mean * mean;

            return (mean, variance);
        }

        public static double Median(this double[] values)
        {
            double[] values2 = (from el in values orderby el ascending select el).ToArray();

            return values2.Length % 2 == 1 ? values2[values2.Length / 2] : values2[values2.Length / 2 - 1] * 0.5 + values2[values2.Length / 2] * 0.5;
        }

        public static long Median(this long[] values)
        {
            long[] values2 = (from el in values orderby el ascending select el).ToArray();

            return values2.Length % 2 == 1 ? values2[values2.Length / 2] : values2[values2.Length / 2 - 1] / 2 + values2[values2.Length / 2] / 2;
        }

        public static double IQR(this double[] values)
        {
            double[] values2 = (from el in values orderby el ascending select el).ToArray();

            double q1, q3;

            if (values2.Length % 2 == 0)
            {
                q1 = values2.Length % 4 == 0 ? (values2[values2.Length / 4] + values2[values2.Length / 4 + 1]) * 0.5 : values2[values2.Length / 4];
                q3 = values2.Length % 4 == 0 ? (values2[3 * values2.Length / 4] + values2[3 * values2.Length / 4 + 1]) * 0.5 : values2[3 * values2.Length / 4];
            }
            else if (values2.Length % 4 == 1)
            {
                int n = (values2.Length - 1) / 4;
                q1 = 0.75 * values2[n - 1] + 0.25 * values2[n];
                q3 = 0.25 * values2[3 * n] + 0.75 * values2[3 * n + 1];
            }
            else
            {
                int n = (values2.Length - 3) / 4;
                q1 = 0.75 * values2[n] + 0.25 * values2[n + 1];
                q3 = 0.25 * values2[3 * n + 1] + 0.75 * values2[3 * n + 2];
            }

            return q3 - q1;
        }

        public static double[][] DeepClone(this double[][] obj)
        {
            double[][] tbr = new double[obj.Length][];

            for (int i = 0; i < obj.Length; i++)
            {
                tbr[i] = (double[])obj[i].Clone();
            }

            return tbr;
        }

        public static byte[][] DeepClone(this byte[][] obj)
        {
            byte[][] tbr = new byte[obj.Length][];

            for (int i = 0; i < obj.Length; i++)
            {
                tbr[i] = (byte[])obj[i].Clone();
            }

            return tbr;
        }


        public enum PadType { Left, Right, Center }
        public static string Pad(this string sr, int length, PadType type, char paddingChar = ' ')
        {
            if (sr.Length > length)
            {
                return sr.Substring(0, length);
            }
            else
            {
                switch (type)
                {
                    case PadType.Left:
                        return sr + new string(paddingChar, length - sr.Length);
                    case PadType.Right:
                        return new string(paddingChar, length - sr.Length) + sr;
                    case PadType.Center:
                        return new string(paddingChar, (length - sr.Length) / 2) + sr + new string(paddingChar, length - sr.Length - (length - sr.Length) / 2);
                    default:
                        return sr;
                }

            }
        }


        public static FoldedDictionary<TKey, TValue> Fold<TKey, TValue>(this Dictionary<TKey, TValue[]> originalDictionary, int index)
        {
            FoldedDictionary<TKey, TValue> tbr = new FoldedDictionary<TKey, TValue>
            {
                internalDictionary = originalDictionary,
                internalIndex = index
            };
            return tbr;
        }
    }

    public class Utils
    {
        public static int MaxThreads = 1;

        public static int[] GetRealIndex(int index, List<string> realParameterNames)
        {
            int realInd = 0;
            int realInd2 = 0;

            int currPos = 0;

            while (index > 0)
            {
                if (realParameterNames[realInd][0] == 'U' || realParameterNames[realInd][0] == 'T')
                {
                    index--;
                    currPos++;
                    realInd++;
                    realInd2 = 0;
                }
                else
                {
                    if (realParameterNames[currPos].Substring(0, realParameterNames[currPos].IndexOf("{")) != realParameterNames[currPos + 1].Substring(0, realParameterNames[currPos + 1].IndexOf("{")))
                    {
                        realInd++;
                        realInd2 = 0;
                        index--;
                        currPos++;
                    }
                    else
                    {
                        realInd2++;
                        index--;
                        currPos++;
                    }
                }
            }

            return new int[] { realInd, realInd2 };
        }


        public static double[] GetBranchStateProbs(IEnumerable<TaggedHistory> histories, int[] treeSamples, LikelihoodModel[] likModels, LikelihoodModel meanLikModel, int[][] meanNodeCorresp, List<string> states, int nodeInd, double length, bool alignRight)
        {
            if (!alignRight)
            {
                double age = length;
                int currNode = meanLikModel.Parents[nodeInd];

                while (currNode >= 0 && meanLikModel.Parents[currNode] >= 0)
                {
                    age += meanLikModel.BranchLengths[currNode];
                    currNode = meanLikModel.Parents[currNode];
                }


                int[] tbr = new int[states.Count];

                foreach (TaggedHistory history in histories)
                {
                    if (meanNodeCorresp[nodeInd][treeSamples[history.Tag]] >= 0)
                    {
                        double minAge = 0;
                        currNode = likModels[treeSamples[history.Tag]].Parents[meanNodeCorresp[nodeInd][treeSamples[history.Tag]]];
                        while (currNode >= 0 && likModels[treeSamples[history.Tag]].Parents[currNode] >= 0)
                        {
                            minAge += likModels[treeSamples[history.Tag]].BranchLengths[currNode];
                            currNode = likModels[treeSamples[history.Tag]].Parents[currNode];
                        }
                        double maxAge = minAge + likModels[treeSamples[history.Tag]].BranchLengths[nodeInd];

                        if (age / minAge - 1 >= -0.00001 && 1 - age / maxAge >= -0.00001)
                        {
                            age = Math.Min(Math.Max(age, minAge), maxAge);
                            tbr[states.IndexOf(GetState(history.History[meanNodeCorresp[nodeInd][treeSamples[history.Tag]]], age - minAge))]++;
                        }
                    }
                }

                double sum = tbr.Sum();

                if (sum > 0)
                {
                    return (from el in tbr select (double)el / sum).ToArray();
                }
                else
                {
                    return (from el in tbr select 0.0).ToArray();
                }
            }
            else
            {
                double age = length;
                int currNode = nodeInd;

                while (meanLikModel.Children[currNode].Length > 0)
                {
                    currNode = meanLikModel.Children[currNode][0];
                    age += meanLikModel.BranchLengths[currNode];
                }


                int[] tbr = new int[states.Count];

                foreach (TaggedHistory history in histories)
                {
                    if (meanNodeCorresp[nodeInd][treeSamples[history.Tag]] >= 0)
                    {
                        double minAge = 0;

                        currNode = meanNodeCorresp[nodeInd][treeSamples[history.Tag]];

                        while (likModels[treeSamples[history.Tag]].Children[currNode].Length > 0)
                        {
                            currNode = likModels[treeSamples[history.Tag]].Children[currNode][0];
                            minAge += likModels[treeSamples[history.Tag]].BranchLengths[currNode];
                        }

                        if (likModels[treeSamples[history.Tag]].BranchLengths[meanNodeCorresp[nodeInd][treeSamples[history.Tag]]] > 0)
                        {
                            double maxAge = minAge + likModels[treeSamples[history.Tag]].BranchLengths[meanNodeCorresp[nodeInd][treeSamples[history.Tag]]];

                            if ((minAge == 0 && age >= 0 || age / minAge - 1 >= -0.00001) && 1 - age / maxAge >= -0.00001)
                            {
                                age = Math.Min(Math.Max(age, minAge), maxAge);
                                tbr[states.IndexOf(GetState(history.History[meanNodeCorresp[nodeInd][treeSamples[history.Tag]]], maxAge - age))]++;
                            }
                        }
                        else
                        {
                            tbr[states.IndexOf(GetState(history.History[likModels[treeSamples[history.Tag]].Children[meanNodeCorresp[nodeInd][treeSamples[history.Tag]]][0]], 0))]++;
                        }
                    }
                }

                double sum = tbr.Sum();

                if (sum > 0)
                {
                    return (from el in tbr select (double)el / sum).ToArray();
                }
                else
                {
                    return (from el in tbr select 0.0).ToArray();
                }
            }
        }


        public static int GetBranchSampleSize(IEnumerable<TaggedHistory> histories, int[] treeSamples, LikelihoodModel[] likModels, LikelihoodModel meanLikModel, int[][] meanNodeCorresp, int nodeInd, double length, bool alignRight)
        {
            if (!alignRight)
            {

                double age = length;
                int currNode = meanLikModel.Parents[nodeInd];

                while (meanLikModel.Parents[currNode] >= 0)
                {
                    age += meanLikModel.BranchLengths[currNode];
                    currNode = meanLikModel.Parents[currNode];
                }


                int tbr = 0;

                foreach (TaggedHistory history in histories)
                {
                    if (meanNodeCorresp[nodeInd][treeSamples[history.Tag]] >= 0)
                    {
                        double minAge = 0;
                        currNode = likModels[treeSamples[history.Tag]].Parents[meanNodeCorresp[nodeInd][treeSamples[history.Tag]]];
                        while (likModels[treeSamples[history.Tag]].Parents[currNode] >= 0)
                        {
                            minAge += likModels[treeSamples[history.Tag]].BranchLengths[currNode];
                            currNode = likModels[treeSamples[history.Tag]].Parents[currNode];
                        }
                        double maxAge = minAge + likModels[treeSamples[history.Tag]].BranchLengths[nodeInd];

                        if (age / minAge - 1 >= -0.00001 && 1 - age / maxAge >= -0.00001)
                        {
                            tbr++;
                        }
                    }
                }

                return tbr;
            }
            else
            {
                double age = length;
                int currNode = nodeInd;

                while (meanLikModel.Children[currNode].Length > 0)
                {
                    currNode = meanLikModel.Children[currNode][0];
                    age += meanLikModel.BranchLengths[currNode];
                }


                int tbr = 0;

                foreach (TaggedHistory history in histories)
                {
                    if (meanNodeCorresp[nodeInd][treeSamples[history.Tag]] >= 0)
                    {
                        double minAge = 0;

                        currNode = meanNodeCorresp[nodeInd][treeSamples[history.Tag]];

                        while (likModels[treeSamples[history.Tag]].Children[currNode].Length > 0)
                        {
                            currNode = likModels[treeSamples[history.Tag]].Children[currNode][0];
                            minAge += likModels[treeSamples[history.Tag]].BranchLengths[currNode];
                        }

                        if (likModels[treeSamples[history.Tag]].BranchLengths[meanNodeCorresp[nodeInd][treeSamples[history.Tag]]] > 0)
                        {
                            double maxAge = minAge + likModels[treeSamples[history.Tag]].BranchLengths[meanNodeCorresp[nodeInd][treeSamples[history.Tag]]];

                            if (((minAge == 0 && age >= 0) || age / minAge - 1 >= -0.00001) && 1 - age / maxAge >= -0.00001)
                            {
                                tbr++;
                            }
                        }
                        else
                        {
                            tbr++;
                        }
                    }
                }

                return tbr;
            }
        }

        public static string GetState(BranchState[] history, double length)
        {
            double currLength = 0;
            int currInd = 0;

            while (currLength <= length && currInd < history.Length)
            {
                currLength += history[currInd].Length;
                currInd++;
            }

            return history[currInd - 1].State;
        }

        public static string GetStateLeft(BranchState[] history, double length)
        {
            double currLength = 0;
            int currInd = 0;

            while (currLength < length && currInd < history.Length)
            {
                currLength += history[currInd].Length;
                currInd++;
            }

            return history[currInd - 1].State;
        }

        public static string GetBranchHistoryString(BranchState[] history, double ageScale)
        {
            string tbr = "{";

            for (int i = 0; i < history.Length; i++)
            {
                tbr += history[history.Length - 1 - i].State.Replace(",", "|") + "," + (history[history.Length - 1 - i].Length * ageScale).ToString(System.Globalization.CultureInfo.InvariantCulture) + (i < history.Length - 1 ? ":" : "");
            }

            return tbr + "}";
        }

        public static string GetSmapString(LikelihoodModel lik, BranchState[][] history, double ageScale)
        {
            string[] stringValues = new string[lik.Children.Length];

            foreach (KeyValuePair<string, int> kvp in lik.NamedBranches)
            {
                stringValues[kvp.Value] = kvp.Key + ":" + GetBranchHistoryString(history[kvp.Value], ageScale);
            }

            for (int i = 0; i < lik.Children.Length; i++)
            {
                if (lik.Children[i].Length != 0)
                {
                    stringValues[i] = "(";
                    for (int j = 0; j < lik.Children[i].Length; j++)
                    {
                        stringValues[i] += stringValues[lik.Children[i][j]] + (j < lik.Children[i].Length - 1 ? "," : "");
                    }
                    stringValues[i] += ")" + (lik.Parents[i] >= 0 ? (":" + GetBranchHistoryString(history[i], ageScale)) : ";");
                }
            }

            return stringValues.Last();
        }


        public static string[] GetAllPossibleStatesString(CharacterDependency[] dependencies, string[][] states)
        {
            int numStates = 1;
            List<string[]> statesList = new List<string[]>();        //states[character][i]

            for (int i = 0; i < dependencies.Length; i++)
            {
                switch (dependencies[i].Type)
                {
                    case CharacterDependency.Types.Independent:
                    case CharacterDependency.Types.Conditioned:
                        numStates *= states[dependencies[i].Index].Length;
                        statesList.Add(states[dependencies[i].Index]);
                        break;
                    case CharacterDependency.Types.Dependent:
                        throw new NotImplementedException();
                }

            }

            string[][] statesInts = statesList.ToArray();

            string[][] allPossibleStates = Utils.GetCombinations(statesInts);        //allPossibleStates[combinationIndex][i]

            return (from el in allPossibleStates select Utils.StringifyArray(el)).ToArray();
        }

        public static string[][] GetAllPossibleStatesStrings(CharacterDependency[] dependencies, string[][] states)
        {
            List<string[]> statesList = new List<string[]>();        //states[character][i]

            for (int i = 0; i < dependencies.Length; i++)
            {
                switch (dependencies[i].Type)
                {
                    case CharacterDependency.Types.Independent:
                    case CharacterDependency.Types.Conditioned:
                        statesList.Add(states[dependencies[i].Index]);
                        break;
                    case CharacterDependency.Types.Dependent:
                        throw new NotImplementedException();
                }

            }

            string[][] statesInts = statesList.ToArray();

            return Utils.GetCombinations(statesInts);        //allPossibleStates[combinationIndex][i]
        }

        public static int[][] GetAllPossibleStatesInts(CharacterDependency[] dependencies, string[][] states)
        {
            return Utils.GetCombinations((from el in dependencies where el.Type == CharacterDependency.Types.Independent || el.Type == CharacterDependency.Types.Conditioned select Utils.Range(0, states[el.Index].Length)).ToArray());        //allPossibleStates[combinationIndex][i]
        }

        public static double Max(double d1, double d2)
        {
            if (d1 >= d2)
            {
                return d1;
            }
            else
            {
                return d2;
            }
        }

        private const int MAX_LAG = 2000;

        public static int[] RoundInts(double[] doublesToRound)
        {
            (int rounded, double difference, int originalIndex)[] tbr = new (int, double, int)[doublesToRound.Length];

            int sum = (int)Math.Round(doublesToRound.Sum());

            double floorSum = 0;

            for (int i = 0; i < doublesToRound.Length; i++)
            {
                int rounded = (int)Math.Floor(doublesToRound[i]);
                tbr[i] = (rounded, doublesToRound[i] - rounded, i);

                floorSum += rounded;
            }

            tbr = (from el in tbr orderby el.difference descending select el).ToArray();

            int difference = sum - (int)Math.Round(floorSum);

            for (int i = 0; i < difference; i++)
            {
                tbr[i].rounded++;
            }

            return (from el in tbr orderby el.originalIndex ascending select el.rounded).ToArray();
        }

        static Regex RemoveCommentsRegex = new Regex("\\[[^]]*\\]");

        public static string FixString(string sr)
        {
            sr = RemoveCommentsRegex.Replace(sr, "");

            sr = sr.Replace("\t", " ").Trim(' ');

            while (sr.Contains("  "))
            {
                sr = sr.Replace("  ", " ");
            }

            return sr;
        }

        public static IContinuousDistribution ParseDistribution(string distribution, Random randomSource)
        {
            string distributionName = distribution.Substring(0, distribution.IndexOf("("));
            Type tp = typeof(Gamma).Assembly.GetType("MathNet.Numerics.Distributions." + distributionName, true, true);
            distribution = distribution.Substring(distribution.IndexOf("(") + 1);
            distribution = distribution.Substring(0, distribution.IndexOf(")"));
            double[] distribParameters = (from el in distribution.Split(',') select double.Parse(el.Substring(el.IndexOf("=") + 1), System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            object[] parameters = new object[distribParameters.Length + 1];

            ParameterInfo[] info = (from el in tp.GetConstructors() where el.GetParameters().Length == distribParameters.Length + 1 select el.GetParameters()).First();

            for (int i = 0; i < distribParameters.Length; i++)
            {
                parameters[i] = Convert.ChangeType(distribParameters[i], info[i].ParameterType);
            }

            parameters[parameters.Length - 1] = randomSource;
            return (IContinuousDistribution)Activator.CreateInstance(tp, parameters);
        }

        public static int[] Range(int minInclusive, int maxExclusive)
        {
            int[] tbr = new int[maxExclusive - minInclusive];
            for (int i = minInclusive; i < maxExclusive; i++)
            {
                tbr[i - minInclusive] = i;
            }
            return tbr;
        }

        public static string StringifyArray<T>(IEnumerable<T> array, string separator = ",")
        {
            StringBuilder tbr = new StringBuilder();

            foreach (T i in array)
            {
                tbr.Append(i.ToString());
                tbr.Append(separator);
            }

            return tbr.ToString().Substring(0, tbr.Length - separator.Length);
        }

        public static T[][] GetCombinations<T>(T[][] sets)
        {
            if (sets.Length == 0)
            {
                return new T[0][];
            }
            else if (sets.Length == 1)
            {
                T[][] tbr = new T[sets[0].Length][];
                for (int i = 0; i < sets[0].Length; i++)
                {
                    tbr[i] = new T[] { sets[0][i] };
                }
                return tbr;
            }
            else
            {
                T[][] newSets = new T[sets.Length - 1][];
                for (int i = 0; i < newSets.Length; i++)
                {
                    newSets[i] = sets[i];
                }

                T[][] singleSet = new T[][] { sets[sets.Length - 1] };

                T[][] singleCombination = GetCombinations(singleSet);
                T[][] otherCombinations = GetCombinations(newSets);

                T[][] tbr = new T[otherCombinations.Length * singleCombination.Length][];
                for (int i = 0; i < otherCombinations.Length; i++)
                {
                    for (int j = 0; j < singleCombination.Length; j++)
                    {
                        T[] currComb = new T[otherCombinations[i].Length + 1];
                        for (int k = 0; k < otherCombinations[i].Length; k++)
                        {
                            currComb[k] = otherCombinations[i][k];
                        }
                        currComb[otherCombinations[i].Length] = singleCombination[j][0];
                        tbr[i * singleCombination.Length + j] = currComb;
                    }
                }

                return tbr;
            }
        }

        public enum VariableStepType { NormalSlide, Dirichlet, Multinomial }

        public static Action<List<double>, List<double>> DefaultPlotTriggerMarshal = (List<double> x, List<double> y) =>
        {
            Trigger("Plot", new object[] { x, y, 100 });
        };

        public static Action<List<double>, List<double>> NewParallelTriggerMarshal(double currentMaxValue)
        {
            List<double> allXs = null;
            List<double> allYs = null;

            object lockObject = new object();

            return (List<double> x, List<double> y) =>
            {
                lock (lockObject)
                {
                    if (allXs == null)
                    {
                        allXs = new List<double>(x);
                        allYs = new List<double>(y);
                    }
                    else
                    {
                        double maxX = allXs.Max();
                        double maxY = Math.Max(allYs.Max(), currentMaxValue);

                        for (int i = 0; i < x.Count; i++)
                        {
                            if (i < allXs.Count && allXs[i] == x[i])
                            {
                                allYs[i] = Math.Max(allYs[i], y[i]);
                            }
                            else if (x[i] > maxX)
                            {
                                allXs.Add(x[i]);
                                allYs.Add(Math.Max(y[i], maxY));
                            }
                        }
                    }

                    Trigger("Plot", new object[] { allXs, allYs, 100 });
                }
            };
        }

        public static Action<List<double>, List<double>> PlotTriggerMarshal { get; set; } = DefaultPlotTriggerMarshal;

        public static Func<List<double>, List<double>, EventWaitHandle> DefaultStepFinishedTriggerMarshal = (List<double> x, List<double> y) =>
        {
            if (x != null && y != null)
            {
                Trigger("StepFinished", new object[] { x, y, 100 });
            }

            return null;
        };

        public static Func<List<double>, List<double>, EventWaitHandle> NewParallelStepFinishedTriggerMarshal()
        {
            List<double> allXs = null;
            List<double> allYs = null;

            double maxX = double.MinValue;
            double maxY = double.MinValue;

            int countCalls = 0;
            int expectedCalls = 0;

            EventWaitHandle handle = new EventWaitHandle(false, EventResetMode.ManualReset);

            object lockObject = new object();

            return (List<double> x, List<double> y) =>
            {
                lock (lockObject)
                {
                    if (x == null && y != null)
                    {
                        expectedCalls++;
                        return null;
                    }
                    else if (x != null && y == null)
                    {
                        expectedCalls--;
                        return null;
                    }
                    else
                    {
                        countCalls++;
                        if (allXs == null)
                        {
                            allXs = new List<double>(x);
                            allYs = new List<double>(y);
                            maxX = allXs.Max();
                            maxY = Math.Max(maxY, allYs.Max());
                        }
                        else
                        {
                            maxX = allXs.Max();
                            maxY = Math.Max(maxY, allYs.Max());

                            for (int i = 0; i < x.Count; i++)
                            {
                                if (i < allXs.Count && allXs[i] == x[i])
                                {
                                    allYs[i] = Math.Max(allYs[i], y[i]);
                                }
                                else if (x[i] > maxX)
                                {
                                    allXs.Add(x[i]);
                                    allYs.Add(Math.Max(y[i], maxY));
                                }
                            }
                        }

                        if (countCalls == expectedCalls)
                        {
                            Trigger("StepFinished", new object[] { allXs, allYs, 100 });
                            PlotTriggerMarshal = NewParallelTriggerMarshal(maxY);
                            allXs = null;
                            allYs = null;
                            countCalls = 0;
                            handle.Set();
                        }

                        if (countCalls > 0)
                        {
                            return handle;
                        }
                        else
                        {
                            EventWaitHandle prevHandle = handle;
                            handle = new EventWaitHandle(false, EventResetMode.ManualReset);
                            return prevHandle;
                        }
                    }
                }
            };
        }

        public static Func<List<double>, List<double>, EventWaitHandle> StepFinishedTriggerMarshal { get; set; } = DefaultStepFinishedTriggerMarshal;

        public static double[] MaximiseFunction(Func<double[], double> func, double[] startVal, (VariableStepType stepType, int[] affectedVariables, double sigma)[] variableTypes, long numSteps, Random rnd, int plotTop = -1)
        {
            double[] bestX = new double[startVal.Length];
            double[] currX = new double[startVal.Length];
            for (int i = 0; i < startVal.Length; i++)
            {
                bestX[i] = startVal[i];
                currX[i] = startVal[i];
            }
            double bestVal = func(bestX);
            double currVal = bestVal;

            List<double> x = new List<double>(100);
            List<double> y = new List<double>(100);

            for (long i = 0; i < numSteps; i++)
            {
                double[] newX = new double[currX.Length];
                double logHastingsRatio = 0;
                for (int j = 0; j < currX.Length; j++)
                {
                    newX[j] = currX[j];
                }
                for (int j = 0; j < variableTypes.Length; j++)
                {
                    if (variableTypes[j].stepType == VariableStepType.NormalSlide)
                    {
                        for (int k = 0; k < variableTypes[j].affectedVariables.Length; k++)
                        {
                            newX[variableTypes[j].affectedVariables[k]] = currX[variableTypes[j].affectedVariables[k]] + Normal.Sample(rnd, 0, variableTypes[j].sigma);

                            while (newX[variableTypes[j].affectedVariables[k]] <= 0)
                            {
                                newX[variableTypes[j].affectedVariables[k]] = currX[variableTypes[j].affectedVariables[k]] + Normal.Sample(rnd, 0, variableTypes[j].sigma);
                            }

                            logHastingsRatio += Math.Log(Normal.CDF(0, variableTypes[j].sigma, currX[variableTypes[j].affectedVariables[k]])) - Math.Log(Normal.CDF(0, variableTypes[j].sigma, newX[variableTypes[j].affectedVariables[k]]));
                        }
                    }
                    else if (variableTypes[j].stepType == VariableStepType.Dirichlet)
                    {
                        double[] alpha = (from el in variableTypes[j].affectedVariables select currX[el] / variableTypes[j].sigma).ToArray();
                        if (alpha.Length > 0)
                        {
                            double[] sample = Dirichlet.Sample(rnd, alpha);

                            for (int k = 0; k < variableTypes[j].affectedVariables.Length; k++)
                            {
                                newX[variableTypes[j].affectedVariables[k]] = sample[k];
                            }

                            logHastingsRatio += new Dirichlet((from el in variableTypes[j].affectedVariables select newX[el] / variableTypes[j].sigma).ToArray(), rnd).DensityLn((from el in variableTypes[j].affectedVariables select currX[el]).ToArray()) - new Dirichlet(alpha, rnd).DensityLn((from el in variableTypes[j].affectedVariables select newX[el]).ToArray());
                        }
                    }
                    else if (variableTypes[j].stepType == VariableStepType.Multinomial)
                    {
                        if (variableTypes[j].affectedVariables.Length > 0)
                        {
                            int sample = rnd.Next(0, variableTypes[j].affectedVariables.Length);

                            for (int k = 0; k < variableTypes[j].affectedVariables.Length; k++)
                            {
                                newX[variableTypes[j].affectedVariables[k]] = sample == k ? 1 : 0;
                            }
                        }
                    }
                }



                double newVal = func(newX);

                double acceptanceProbability = newVal / currVal;

                if (newVal < 0 && currVal < 0)
                {
                    acceptanceProbability = currVal / newVal;
                }
                else if (newVal > 0 && currVal < 0)
                {
                    acceptanceProbability = 1;
                }

                if (plotTop == -2)
                {
                    Trigger("ValueSampled", new object[] { newX.Clone() });
                }

                if (rnd.NextDouble() < acceptanceProbability)
                {
                    currVal = newVal;
                    currX = newX;

                    if (currVal > bestVal)
                    {
                        bestVal = currVal;
                        bestX = currX;
                    }
                }

                if ((plotTop >= 0) && i % (numSteps / 100) == 0)
                {
                    x.Add(i);
                    y.Add(bestVal);
                    ConsolePlot(x.ToArray(), y.ToArray(), "Iteration", "LogLikelihood", 10, plotTop, 80, 20, new double[] { 0, numSteps });
                }
                else if (plotTop == -2 && i % (numSteps / 100) == 0)
                {
                    x.Add(i);
                    y.Add(bestVal);

                    PlotTriggerMarshal(x, y);
                }
            }

            if (plotTop >= 0)
            {
                x.Add(numSteps);
                y.Add(bestVal);
                ConsolePlot(x.ToArray(), y.ToArray(), "Iteration", "LogLikelihood", 10, plotTop, 80, 20);
            }
            else if (plotTop == -2)
            {
                x.Add(100);
                y.Add(bestVal);

                StepFinishedTriggerMarshal(x, y)?.WaitOne();
            }
            return bestX;
        }

        public static double[] Gradient(Func<double[], double> func, double[] val, double[] stepSize, out double[] bestVal)
        {
            bestVal = new double[val.Length + 1];

            double[] tbr = new double[val.Length];

            double funcVal = func(val);

            bestVal[0] = funcVal;

            for (int i = 0; i < val.Length; i++)
            {
                bestVal[i + 1] = val[i];
            }

            for (int i = 0; i < val.Length; i++)
            {
                double[] testVal = new double[val.Length];
                for (int j = 0; j < val.Length; j++)
                {
                    if (i != j)
                    {
                        testVal[j] = val[j];
                    }
                    else
                    {
                        testVal[j] = val[j] + stepSize[j];
                    }
                }

                double testFuncVal = func(testVal);
                if (testFuncVal > bestVal[0])
                {
                    bestVal[0] = testFuncVal;
                    for (int j = 0; j < val.Length; j++)
                    {
                        bestVal[j + 1] = testVal[j];
                    }
                }

                tbr[i] = (testFuncVal - funcVal) / stepSize[i];
            }

            return tbr;
        }


        public static double[,] HessianMatrix(Func<double[], double> func, double[] val)
        {
            NumericalHessian hess = new NumericalHessian();

            return hess.Evaluate(func, val);
        }

        public static double[] MaximiseFunctionNesterov(Func<double[], double> func, double[] startVal, long numSteps, double maxRate, double maxMomentum, Random rnd, int plotTop = -1)
        {
            double[][] currVal = new double[3][];

            for (int j = 0; j < 3; j++)
            {
                currVal[j] = new double[startVal.Length];

                for (int i = 0; i < startVal.Length; i++)
                {
                    currVal[j][i] = startVal[i];
                }
            }

            List<double> x = new List<double>(100);
            List<double> y = new List<double>(100);

            int tIndex = 0;

            double[] bestVars = (double[])startVal.Clone();
            double bestVal = func(startVal);

            for (long t = 0; t < numSteps; t++)
            {
                double rate = Math.Max(0.01, maxRate * rnd.NextDouble());
                double momentum = Math.Max(0.01, maxMomentum * rnd.NextDouble());

                double[] gradientStepSize = new double[startVal.Length];

                for (int i = 0; i < startVal.Length; i++)
                {
                    gradientStepSize[i] = Math.Max(0.00001, Math.Abs((currVal[tIndex][i] - currVal[(tIndex + 2) % 3][i]) * 0.01));
                }


                double[] gradient = Gradient(func, currVal[tIndex].Add((currVal[tIndex].Subtract(currVal[(tIndex + 2) % 3])).Multiply(momentum)), gradientStepSize, out double[] gradientBestVal);

                if (gradientBestVal[0] > bestVal)
                {
                    bestVal = gradientBestVal[0];
                    bestVars = gradientBestVal.Skip(1).ToArray();
                }

                currVal[(tIndex + 1) % 3] = currVal[tIndex].Add((currVal[tIndex].Subtract(currVal[(tIndex + 2) % 3])).Multiply(momentum)).Add(gradient.Multiply(rate));
                tIndex = (tIndex + 1) % 3;

                for (int i = 0; i < currVal[tIndex].Length; i++)
                {
                    currVal[tIndex][i] = Math.Max(0.0001, currVal[tIndex][i]);

                    if (double.IsNaN(currVal[tIndex][i]))
                    {
                        currVal[tIndex][i] = currVal[(tIndex + 2) % 3][i];
                    }
                }

                if (plotTop == -2)
                {
                    Trigger("ValueSampled", new object[] { currVal[tIndex].Clone() });
                }

                double funcVal = func(currVal[tIndex]);

                if (funcVal > bestVal)
                {
                    bestVal = funcVal;
                    bestVars = (double[])currVal[tIndex].Clone();


                }

                if ((plotTop >= 0) && t % Math.Max(1, (numSteps / 100)) == 0)
                {
                    x.Add(t);
                    y.Add(bestVal);
                    ConsolePlot(x.ToArray(), y.ToArray(), "Iteration", "LogLikelihood", 10, plotTop, 80, 20, new double[] { 0, numSteps });
                }
                else if (plotTop == -2 && t % Math.Max(1, (numSteps / 100)) == 0)
                {
                    x.Add(t);
                    y.Add(bestVal);

                    PlotTriggerMarshal(x, y);
                }
            }

            if (plotTop >= 0)
            {
                x.Add(numSteps);
                y.Add(bestVal);
                ConsolePlot(x.ToArray(), y.ToArray(), "Iteration", "LogLikelihood", 10, plotTop, 80, 20);
            }
            else if (plotTop == -2)
            {
                x.Add(100);
                y.Add(bestVal);

                StepFinishedTriggerMarshal(x, y)?.WaitOne();
            }

            return bestVars;
        }

        public static double Log1p(double x)
        {
            if (x <= -1)
            {
                return double.NegativeInfinity;
            }
            else if (Math.Abs(x) > 0.0001)
            {
                return Math.Log(1 + x);
            }
            else
            {
                return (1 - 0.5 * x) * x;
            }
        }

        public static double LogSumExp(double[] logs)
        {
            int maxInd = logs.MaxInd();

            double log1pArg = 0;

            for (int i = 0; i < logs.Length; i++)
            {
                if (i != maxInd)
                {
                    log1pArg += Math.Exp(logs[i] - logs[maxInd]);
                }
            }

            if (!double.IsNaN(log1pArg))
            {
                return logs[maxInd] + Log1p(log1pArg);
            }
            else
            {
                double logArg = 0;

                for (int i = 0; i < logs.Length; i++)
                {
                    logArg += Math.Exp(logs[i]);
                }

                return Math.Log(logArg);
            }

        }

        public static double LogSumExp(double log1, double log2)
        {
            double tbr = double.NaN;
            if (log1 > log2)
            {
                double log1pArg = 0;
                log1pArg += Math.Exp(log2 - log1);
                tbr = log1 + Log1p(log1pArg);
            }
            else
            {
                double log1pArg = 0;
                log1pArg += Math.Exp(log1 - log2);
                tbr = log2 + Log1p(log1pArg);
            }

            if (!double.IsNaN(tbr))
            {
                return tbr;
            }
            else
            {
                return Math.Log(Math.Exp(log1) + Math.Exp(log2));
            }

        }

        public static double LogSumExpTimes(double[] logs, double[] multipliers)
        {
            int maxInd = logs.MaxInd(multipliers);

            double log1pArg = 0;

            for (int i = 0; i < logs.Length; i++)
            {
                if (i != maxInd)
                {
                    log1pArg += Math.Exp(logs[i] - logs[maxInd]) * (multipliers[i] / multipliers[maxInd]);
                }
            }

            if (!double.IsNaN(log1pArg))
            {
                return logs[maxInd] + Math.Log(multipliers[maxInd]) + Log1p(log1pArg);
            }
            else
            {
                double logArg = 0;

                for (int i = 0; i < logs.Length; i++)
                {
                    logArg += Math.Exp(logs[i]) * multipliers[i];
                }

                return Math.Log(logArg);
            }
        }

        public static double Logit(double x)
        {
            return Math.Log(x / (1 - x));
        }

        public static bool SkipESS = false;

        //Derived from https://github.com/beast-dev/beast-mcmc/blob/v1.10.3/src/dr/inference/trace/TraceCorrelation.java
        public static double computeESS(List<(int, double[][])>[] values, double mean, int i0, int i2, int i3, int skip)
        {
            if (SkipESS)
            {
                return 1;
            }

            int samples = values[i0].Count - skip;
            int maxLag = Math.Min(samples - 1, MAX_LAG);

            int samplingFrequency = 1;

            double[] gammaStat = new double[maxLag];
            double varStat = 0.0;

            for (int lag = 0; lag < maxLag; lag++)
            {
                for (int j = 0; j < samples - lag; j++)
                {
                    double del1 = values[i0][j * samplingFrequency + skip].Item2[i2][i3] - mean;
                    double del2 = values[i0][j * samplingFrequency + skip + lag].Item2[i2][i3] - mean;
                    gammaStat[lag] += (del1 * del2);
                }

                gammaStat[lag] /= (samples - lag);

                if (lag == 0)
                {
                    varStat = gammaStat[0];
                }
                else if (lag % 2 == 0)
                {
                    // fancy stopping criterion :)
                    if (gammaStat[lag - 1] + gammaStat[lag] > 0)
                    {
                        varStat += 2.0 * (gammaStat[lag - 1] + gammaStat[lag]);
                    }
                    // stop
                    else
                    {
                        //maxLag = lag;
                        break;
                    }
                }
            }

            double ACT = 0;

            // auto correlation time
            if (gammaStat[0] != 0)
            {
                ACT = varStat / gammaStat[0];
            }

            double ESS = 1;

            // effective sample size
            if (ACT != 0)
            {
                ESS = samples / ACT;
            }

            return ESS;
        }

        //Derived from https://github.com/beast-dev/beast-mcmc/blob/v1.10.3/src/dr/inference/trace/TraceCorrelation.java
        public static double computeESS(List<double> values, double mean, int skip)
        {
            int samples = values.Count - skip;
            int maxLag = Math.Min(samples - 1, MAX_LAG);

            int samplingFrequency = 1;

            double[] gammaStat = new double[maxLag];
            double varStat = 0.0;

            for (int lag = 0; lag < maxLag; lag++)
            {
                for (int j = 0; j < samples - lag; j++)
                {
                    double del1 = values[j * samplingFrequency + skip] - mean;
                    double del2 = values[j * samplingFrequency + skip + lag] - mean;
                    gammaStat[lag] += (del1 * del2);
                }

                gammaStat[lag] /= (samples - lag);

                if (lag == 0)
                {
                    varStat = gammaStat[0];
                }
                else if (lag % 2 == 0)
                {
                    // fancy stopping criterion :)
                    if (gammaStat[lag - 1] + gammaStat[lag] > 0)
                    {
                        varStat += 2.0 * (gammaStat[lag - 1] + gammaStat[lag]);
                    }
                    // stop
                    else
                    {
                        //maxLag = lag;
                        break;
                    }
                }
            }

            double ACT = 0;

            // auto correlation time
            if (gammaStat[0] != 0)
            {
                ACT = varStat / gammaStat[0];
            }

            double ESS = 1;

            // effective sample size
            if (ACT != 0)
            {
                ESS = samples / ACT;
            }

            return ESS;
        }

        //Derived from https://github.com/beast-dev/beast-mcmc/blob/v1.10.3/src/dr/inference/trace/TraceCorrelation.java
        public static double computeESS(double[] values, double mean, int skip)
        {
            int samples = values.Length - skip;
            int maxLag = Math.Min(samples - 1, MAX_LAG);

            int samplingFrequency = 1;

            double[] gammaStat = new double[maxLag];
            double varStat = 0.0;

            for (int lag = 0; lag < maxLag; lag++)
            {
                for (int j = 0; j < samples - lag; j++)
                {
                    double del1 = values[j * samplingFrequency + skip] - mean;
                    double del2 = values[j * samplingFrequency + skip + lag] - mean;
                    gammaStat[lag] += (del1 * del2);
                }

                gammaStat[lag] /= (samples - lag);

                if (lag == 0)
                {
                    varStat = gammaStat[0];
                }
                else if (lag % 2 == 0)
                {
                    // fancy stopping criterion :)
                    if (gammaStat[lag - 1] + gammaStat[lag] > 0)
                    {
                        varStat += 2.0 * (gammaStat[lag - 1] + gammaStat[lag]);
                    }
                    // stop
                    else
                    {
                        //maxLag = lag;
                        break;
                    }
                }
            }

            double ACT = 0;

            // auto correlation time
            if (gammaStat[0] != 0)
            {
                ACT = varStat / gammaStat[0];
            }

            double ESS = 1;

            // effective sample size
            if (ACT != 0)
            {
                ESS = samples / ACT;
            }

            return ESS;
        }

        public static double[] AutoMaximiseFunctionRandomWalk(Func<double[], double> func, double[] startVal, (VariableStepType stepType, int[] affectedVariables, double sigma)[] variableTypes, RandomWalk strategy, Random rnd, int plotTop = -1)
        {
            if (strategy.ConvergenceCriterion == ConvergenceCriteria.Value)
            {
                double currVal = func(startVal) - 1;
                double newVal = currVal + 1;
                double[] newVars = startVal;

                //Register thread
                StepFinishedTriggerMarshal(null, new List<double>());

                while (Math.Abs(newVal - currVal) > strategy.ConvergenceThreshold)
                {
                    currVal = newVal;
                    newVars = MaximiseFunction(func, startVal, variableTypes, strategy.StepsPerRun, rnd, strategy.Plot ? plotTop : -1);

                    newVal = func(newVars);
                    startVal = newVars;
                }

                //Unregister thread
                StepFinishedTriggerMarshal(new List<double>(), null);

                return newVars;
            }
            else if (strategy.ConvergenceCriterion == ConvergenceCriteria.Variables)
            {
                double newVal = 1;
                double[] newVars = startVal;

                //Register thread
                StepFinishedTriggerMarshal(null, new List<double>());

                while (newVal > strategy.ConvergenceThreshold)
                {
                    newVars = MaximiseFunction(func, startVal, variableTypes, strategy.StepsPerRun, rnd, strategy.Plot ? plotTop : -1);

                    newVal = 0;

                    for (int i = 0; i < startVal.Length; i++)
                    {
                        newVal = Math.Max(Math.Abs(startVal[i] - newVars[i]), newVal);
                    }

                    startVal = newVars;
                }

                //Unregister thread
                StepFinishedTriggerMarshal(new List<double>(), null);

                double lastVal = func(newVars);

                return newVars;
            }

            return null;
        }

        public static double[] AutoMaximiseFunctionNesterov(Func<double[], double> func, double[] startVal, (VariableStepType stepType, int[] affectedVariables, double sigma)[] variableTypes, NesterovClimbing strategy, Random rnd, int plotTop = -1)
        {
            List<double> realVals = new List<double>();

            for (int i = 0; i < variableTypes.Length; i++)
            {
                if (variableTypes[i].stepType == VariableStepType.NormalSlide)
                {
                    for (int j = 0; j < variableTypes[i].affectedVariables.Length; j++)
                    {
                        realVals.Add(startVal[variableTypes[i].affectedVariables[j]]);
                    }
                }
            }

            double[] startValp = realVals.ToArray();

            Func<double[], double> funcp = (double[] vals) =>
            {
                double[] myVals = new double[startVal.Length];

                for (int i = 0; i < startVal.Length; i++)
                {
                    myVals[i] = startVal[i];
                }

                int ind = 0;

                for (int i = 0; i < variableTypes.Length; i++)
                {
                    if (variableTypes[i].stepType == VariableStepType.NormalSlide)
                    {
                        for (int j = 0; j < variableTypes[i].affectedVariables.Length; j++)
                        {
                            myVals[variableTypes[i].affectedVariables[j]] = vals[ind];
                            ind++;
                        }
                    }
                }

                return func(myVals);
            };


            if (strategy.ConvergenceCriterion == ConvergenceCriteria.Value)
            {
                double currVal = funcp(startValp) - 1;
                double newVal = currVal + 1;
                double[] newVars = startValp;

                //Register thread
                StepFinishedTriggerMarshal(null, new List<double>());

                while (Math.Abs(newVal - currVal) > strategy.ConvergenceThreshold)
                {
                    currVal = newVal;
                    newVars = MaximiseFunctionNesterov(funcp, startValp, strategy.StepsPerRun, 0.2, 0.2, rnd, strategy.Plot ? plotTop : -1);

                    newVal = funcp(newVars);
                    startValp = newVars;
                }

                //Unregister thread
                StepFinishedTriggerMarshal(new List<double>(), null);

                double lastVal = funcp(newVars);


                double[] myVals = new double[startVal.Length];

                for (int i = 0; i < startVal.Length; i++)
                {
                    myVals[i] = startVal[i];
                }

                int ind = 0;

                for (int i = 0; i < variableTypes.Length; i++)
                {
                    if (variableTypes[i].stepType == VariableStepType.NormalSlide)
                    {
                        for (int j = 0; j < variableTypes[i].affectedVariables.Length; j++)
                        {
                            myVals[variableTypes[i].affectedVariables[j]] = newVars[ind];
                            ind++;
                        }
                    }
                }

                return myVals;
            }
            else if (strategy.ConvergenceCriterion == ConvergenceCriteria.Variables)
            {
                double newVal = 1;
                double[] newVars = startValp;

                //Register thread
                StepFinishedTriggerMarshal(null, new List<double>());

                while (newVal > strategy.ConvergenceThreshold)
                {
                    newVars = MaximiseFunctionNesterov(funcp, startValp, strategy.StepsPerRun, 0.2, 0.2, rnd, strategy.Plot ? plotTop : -1);

                    newVal = 0;

                    for (int i = 0; i < startValp.Length; i++)
                    {
                        newVal = Math.Max(Math.Abs(startValp[i] - newVars[i]), newVal);
                    }

                    startValp = newVars;
                }

                //Unregister thread
                StepFinishedTriggerMarshal(new List<double>(), null);

                double lastVal = funcp(newVars);

                double[] myVals = new double[startVal.Length];

                for (int i = 0; i < startVal.Length; i++)
                {
                    myVals[i] = startVal[i];
                }

                int ind = 0;

                for (int i = 0; i < variableTypes.Length; i++)
                {
                    if (variableTypes[i].stepType == VariableStepType.NormalSlide)
                    {
                        for (int j = 0; j < variableTypes[i].affectedVariables.Length; j++)
                        {
                            myVals[variableTypes[i].affectedVariables[j]] = newVars[ind];
                            ind++;
                        }
                    }
                }

                return myVals;
            }

            return null;
        }

        public static double[] AutoMaximiseFunctionSampling(Func<double[], double> func, double[] startVal, (VariableStepType stepType, int[] affectedVariables, double sigma)[] variableTypes, Sampling strategy, bool autoRecenter, int plotTop = -1)
        {
            if (!strategy.Plot)
            {
                plotTop = -1;
            }

            List<int> continuousVariables = new List<int>();
            List<int[]> multinomialVariables = new List<int[]>();
            List<int> dirichletVariables = new List<int>();

            int continuousSteps = (int)Math.Ceiling((strategy.Max - strategy.Min) / strategy.Resolution) + 1;
            double totalSteps = 1;

            for (int i = 0; i < variableTypes.Length; i++)
            {
                switch (variableTypes[i].stepType)
                {
                    case VariableStepType.NormalSlide:
                        continuousVariables.AddRange(variableTypes[i].affectedVariables);
                        totalSteps *= Math.Pow(continuousSteps, variableTypes[i].affectedVariables.Length);
                        break;
                    case VariableStepType.Multinomial:
                        multinomialVariables.Add(variableTypes[i].affectedVariables);
                        totalSteps *= variableTypes[i].affectedVariables.Length;
                        break;
                    case VariableStepType.Dirichlet:
                        dirichletVariables.AddRange(variableTypes[i].affectedVariables);
                        break;
                }
            }

            Sampling[] strategies = new Sampling[continuousVariables.Count];

            if (!autoRecenter)
            {
                for (int i = 0; i < continuousVariables.Count; i++)
                {
                    strategies[i] = strategy;
                }
            }
            else
            {
                for (int i = 0; i < continuousVariables.Count; i++)
                {
                    strategies[i] = new Sampling(strategy.Plot, Math.Max(0.0001, startVal[i] - strategy.Resolution * (continuousSteps / 2)), Math.Max(startVal[i] + strategy.Resolution * (continuousSteps / 2), 0.0001 + strategy.Resolution * continuousSteps), strategy.Resolution);
                }
            }

            int[] steps = new int[continuousVariables.Count + multinomialVariables.Count];

            double[] bestVals = (double[])startVal.Clone();

            double maxVal = func(bestVals);

            double stepsDone = 0;

            int lastPerc = -1;

            List<double> y = new List<double>();
            List<double> x = new List<double>();

            //Register thread
            StepFinishedTriggerMarshal(null, new List<double>());

            do
            {
                double[] currVal = new double[startVal.Length];

                for (int i = 0; i < dirichletVariables.Count; i++)
                {
                    currVal[dirichletVariables[i]] = startVal[dirichletVariables[i]];
                }

                int varInd = 0;

                for (int i = 0; i < continuousVariables.Count; i++)
                {
                    currVal[continuousVariables[i]] = Math.Min(strategies[i].Min + strategies[i].Resolution * steps[varInd], strategies[i].Max);
                    varInd++;
                }

                for (int i = 0; i < multinomialVariables.Count; i++)
                {
                    for (int j = 0; j < multinomialVariables[i].Length; j++)
                    {
                        currVal[multinomialVariables[i][j]] = j == steps[varInd] ? 1 : 0;
                    }
                    varInd++;
                }

                double val = func(currVal);

                if (plotTop == -2)
                {
                    Trigger("ValueSampled", new object[] { currVal.Clone() });
                }

                if (val > maxVal)
                {
                    maxVal = val;
                    bestVals = (double[])currVal.Clone();
                }

                stepsDone++;

                if (plotTop >= 0 && (int)(stepsDone * 100.0 / totalSteps) > lastPerc)
                {
                    lastPerc = (int)(stepsDone * 100.0 / totalSteps);
                    y.Add(maxVal);
                    x.Add(stepsDone);

                    ConsolePlot(x.ToArray(), y.ToArray(), "Step", "LogLikelihood", 10, plotTop, 80, 20, new double[] { 0, totalSteps });
                }
                else if (plotTop == -2 && (int)(stepsDone * 100.0 / totalSteps) > lastPerc)
                {
                    lastPerc = (int)(stepsDone * 100.0 / totalSteps);
                    y.Add(maxVal);
                    x.Add(lastPerc);
                    
                    PlotTriggerMarshal(x, y);
                }

                varInd = 0;
                bool found = false;

                for (int i = 0; i < continuousVariables.Count; i++)
                {
                    if (steps[varInd] + 1 < continuousSteps)
                    {
                        steps[varInd]++;
                        found = true;
                        break;
                    }
                    else
                    {
                        steps[varInd] = 0;
                    }
                    varInd++;
                }

                if (found)
                {
                    continue;
                }

                for (int i = 0; i < multinomialVariables.Count; i++)
                {
                    if (steps[varInd] < multinomialVariables[i].Length - 1)
                    {
                        steps[varInd]++;
                        break;
                    }
                    else
                    {
                        steps[varInd] = 0;
                    }
                    varInd++;
                }
            }
            while (steps.Max() != 0);

            func(bestVals);

            if (plotTop >= 0)
            {
                x.Add(totalSteps);
                y.Add(maxVal);
                ConsolePlot(x.ToArray(), y.ToArray(), "Step", "LogLikelihood", 10, plotTop, 80, 20);
            }
            else if (plotTop == -2)
            {
                y.Add(maxVal);
                x.Add(100);
                
                StepFinishedTriggerMarshal(x, y)?.WaitOne();
            }

            //Unregister thread
            StepFinishedTriggerMarshal(new List<double>(), null);

            return bestVals;
        }

        public static Action<string, object[]> Trigger;
        public static bool RunningGui = false;

        public static double[] AutoMaximiseFunctionIterativeSampling(Func<double[], int, double> func, double[] startVal, (VariableStepType stepType, int[] affectedVariables, double sigma)[] variableTypes, IterativeSampling strategy, bool autoRecenter, int plotTop = -1)
        {
            if (!strategy.Plot)
            {
                plotTop = -1;
            }

            List<int> continuousVariables = new List<int>();
            List<int[]> multinomialVariables = new List<int[]>();
            List<int> dirichletVariables = new List<int>();
            List<int> dirichletVariablesCorresp = new List<int>();


            int continuousSteps = (int)Math.Ceiling((strategy.Max - strategy.Min) / strategy.Resolution) + 1;
            int totalSteps = 0;

            for (int i = 0; i < variableTypes.Length; i++)
            {
                switch (variableTypes[i].stepType)
                {
                    case VariableStepType.NormalSlide:
                        continuousVariables.AddRange(variableTypes[i].affectedVariables);
                        totalSteps += continuousSteps * variableTypes[i].affectedVariables.Length;
                        break;
                    case VariableStepType.Multinomial:
                        multinomialVariables.Add(variableTypes[i].affectedVariables);
                        totalSteps += variableTypes[i].affectedVariables.Length;
                        break;
                    case VariableStepType.Dirichlet:
                        for (int j = 0; j < variableTypes[i].affectedVariables.Length; j++)
                        {
                            dirichletVariables.Add(variableTypes[i].affectedVariables[j]);
                            dirichletVariablesCorresp.Add(i);
                            totalSteps += continuousSteps;
                        }
                        break;
                }
            }

            IterativeSampling[] strategies = new IterativeSampling[continuousVariables.Count];

            if (!autoRecenter)
            {
                for (int i = 0; i < continuousVariables.Count; i++)
                {
                    startVal[continuousVariables[i]] = strategy.Min + strategy.Resolution * (continuousSteps / 2);
                    strategies[i] = strategy;
                }
            }
            else
            {
                for (int i = 0; i < continuousVariables.Count; i++)
                {
                    strategies[i] = new IterativeSampling(strategy.Plot, Math.Max(0.0001, startVal[i] - strategy.Resolution * (continuousSteps / 2)), Math.Max(startVal[i] + strategy.Resolution * (continuousSteps / 2), 0.0001 + strategy.Resolution * continuousSteps), strategy.Resolution, strategy.ConvergenceCriterion, strategy.ConvergenceThreshold);
                }
            }

            double[] bestVals = (double[])startVal.Clone();

            double maxVal = func(bestVals, 0);

            double lastMaxVal = maxVal;
            double[] lastBestVals = (double[])bestVals.Clone();

            bool shouldContinue = true;

            if (continuousVariables.Count == 0 && multinomialVariables.Count == 0 && dirichletVariables.Count == 0)
            {
                shouldContinue = false;
            }



            //Register thread
            StepFinishedTriggerMarshal(null, new List<double>());

            while (shouldContinue)
            {
                lastMaxVal = maxVal;
                lastBestVals = (double[])bestVals.Clone();

                int stepsDone = 0;

                int lastPerc = -1;

                List<double> y = new List<double>();
                List<double> x = new List<double>();

                for (int cVarInd = 0; cVarInd < startVal.Length; cVarInd++)
                {
                    if (cVarInd >= continuousVariables.Count && cVarInd - continuousVariables.Count >= multinomialVariables.Count)
                    {
                        double[] currStartVal = new double[bestVals.Length];
                        bestVals.CopyTo(currStartVal, 0);

                        int dirichletVariable = dirichletVariablesCorresp[cVarInd - continuousVariables.Count - multinomialVariables.Count];

                        object lockObject = new object();

                        List<int> remainingSteps = new List<int>(Range(0, continuousSteps));

                        Thread[] threads = new Thread[Utils.MaxThreads];

                        for (int thrI = 0; thrI < Utils.MaxThreads; thrI++)
                        {
                            threads[thrI] = new Thread((threadInd) =>
                            {
                                bool shouldStart;
                                int step = -1;

                                lock (lockObject)
                                {
                                    shouldStart = remainingSteps.Count > 0;
                                    if (shouldStart)
                                    {
                                        step = remainingSteps[0];
                                        remainingSteps.RemoveAt(0);
                                    }
                                }

                                while (shouldStart)
                                {
                                    double[] currVal = new double[currStartVal.Length];
                                    currStartVal.CopyTo(currVal, 0);

                                    currVal[dirichletVariables[cVarInd - continuousVariables.Count - multinomialVariables.Count]] = step * 1.0 / (continuousSteps - 1);

                                    double otherSum = 0;

                                    for (int i = 0; i < variableTypes[dirichletVariable].affectedVariables.Length; i++)
                                    {
                                        if (variableTypes[dirichletVariable].affectedVariables[i] != dirichletVariables[cVarInd - continuousVariables.Count - multinomialVariables.Count])
                                        {
                                            otherSum += currVal[variableTypes[dirichletVariable].affectedVariables[i]];
                                        }
                                    }

                                    if (otherSum != 0)
                                    {
                                        for (int i = 0; i < variableTypes[dirichletVariable].affectedVariables.Length; i++)
                                        {
                                            if (variableTypes[dirichletVariable].affectedVariables[i] != dirichletVariables[cVarInd - continuousVariables.Count - multinomialVariables.Count])
                                            {
                                                currVal[variableTypes[dirichletVariable].affectedVariables[i]] /= otherSum / (1 - step * 1.0 / (continuousSteps - 1));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        for (int i = 0; i < variableTypes[dirichletVariable].affectedVariables.Length; i++)
                                        {
                                            if (variableTypes[dirichletVariable].affectedVariables[i] != dirichletVariables[cVarInd - continuousVariables.Count - multinomialVariables.Count])
                                            {
                                                currVal[variableTypes[dirichletVariable].affectedVariables[i]] = (1 - step * 1.0 / (continuousSteps - 1)) / (variableTypes[dirichletVariable].affectedVariables.Length - 1);
                                            }
                                        }
                                    }

                                    double val = func(currVal, (int)threadInd);

                                    lock (lockObject)
                                    {
                                        if (val >= maxVal)
                                        {
                                            maxVal = val;
                                            currVal.CopyTo(bestVals, 0);
                                        }

                                        stepsDone++;

                                        if (plotTop == -2)
                                        {
                                            Trigger("ValueSampled", new object[] { currVal.Clone() });
                                        }

                                        if (plotTop >= 0 && (int)(stepsDone * 100.0 / totalSteps) > lastPerc)
                                        {
                                            lastPerc = (int)(stepsDone * 100.0 / totalSteps);
                                            y.Add(maxVal);
                                            x.Add(stepsDone);

                                            ConsolePlot(x.ToArray(), y.ToArray(), "Step", "LogLikelihood", 10, plotTop, 80, 20, new double[] { 0, totalSteps });
                                        }
                                        else if (plotTop == -2 && (int)(stepsDone * 100.0 / totalSteps) > lastPerc)
                                        {
                                            lastPerc = (int)(stepsDone * 100.0 / totalSteps);
                                            y.Add(maxVal);
                                            x.Add(lastPerc);
                                            
                                            PlotTriggerMarshal(x, y);
                                        }

                                        shouldStart = remainingSteps.Count > 0;
                                        if (shouldStart)
                                        {
                                            step = remainingSteps[0];
                                            remainingSteps.RemoveAt(0);
                                        }
                                    }
                                }
                            });

                            threads[thrI].Start(thrI);
                        }

                        for (int thrI = 0; thrI < Utils.MaxThreads; thrI++)
                        {
                            threads[thrI].Join();
                        }
                    }
                    else
                    {
                        double[] currStartVal = new double[bestVals.Length];
                        bestVals.CopyTo(currStartVal, 0);

                        object lockObject = new object();

                        int maxStep = (cVarInd < continuousVariables.Count) ? continuousSteps : multinomialVariables[cVarInd - continuousVariables.Count].Length;

                        List<int> remainingSteps = new List<int>(Range(0, continuousSteps));

                        Thread[] threads = new Thread[Utils.MaxThreads];

                        for (int thrI = 0; thrI < Utils.MaxThreads; thrI++)
                        {
                            threads[thrI] = new Thread((threadInd) =>
                            {
                                bool shouldStart;
                                int step = -1;

                                lock (lockObject)
                                {
                                    shouldStart = remainingSteps.Count > 0;
                                    if (shouldStart)
                                    {
                                        step = remainingSteps[0];
                                        remainingSteps.RemoveAt(0);
                                    }
                                }

                                while (shouldStart)
                                {
                                    double[] currVal = new double[currStartVal.Length];
                                    currStartVal.CopyTo(currVal, 0);

                                    if (cVarInd < continuousVariables.Count)
                                    {
                                        currVal[continuousVariables[cVarInd]] = Math.Min(strategies[cVarInd].Min + strategies[cVarInd].Resolution * step, strategies[cVarInd].Max);
                                    }
                                    else
                                    {
                                        for (int j = 0; j < multinomialVariables[cVarInd - continuousVariables.Count].Length; j++)
                                        {
                                            currVal[multinomialVariables[cVarInd - continuousVariables.Count][j]] = j == step ? 1 : 0;
                                        }
                                    }

                                    double val = func(currVal, (int)threadInd);

                                    lock (lockObject)
                                    {
                                        if (val > maxVal)
                                        {
                                            maxVal = val;
                                            bestVals = (double[])currVal.Clone();
                                        }

                                        stepsDone++;

                                        if (plotTop == -2)
                                        {
                                            Trigger("ValueSampled", new object[] { currVal.Clone() });
                                        }

                                        if (plotTop >= 0 && (int)(stepsDone * 100.0 / totalSteps) > lastPerc)
                                        {
                                            lastPerc = (int)(stepsDone * 100.0 / totalSteps);
                                            y.Add(maxVal);
                                            x.Add(stepsDone);

                                            ConsolePlot(x.ToArray(), y.ToArray(), "Step", "LogLikelihood", 10, plotTop, 80, 20, new double[] { 0, totalSteps });
                                        }
                                        else if (plotTop == -2 && (int)(stepsDone * 100.0 / totalSteps) > lastPerc)
                                        {
                                            lastPerc = (int)(stepsDone * 100.0 / totalSteps);
                                            y.Add(maxVal);
                                            x.Add(lastPerc);
                                            
                                            PlotTriggerMarshal(x, y);
                                        }
                                        shouldStart = remainingSteps.Count > 0;
                                        if (shouldStart)
                                        {
                                            step = remainingSteps[0];
                                            remainingSteps.RemoveAt(0);
                                        }
                                    }
                                }
                            });

                            threads[thrI].Start(thrI);
                        }

                        for (int thrI = 0; thrI < Utils.MaxThreads; thrI++)
                        {
                            threads[thrI].Join();
                        }
                    }
                }

                if (plotTop >= 0)
                {
                    x.Add(totalSteps);
                    y.Add(maxVal);
                    ConsolePlot(x.ToArray(), y.ToArray(), "Step", "LogLikelihood", 10, plotTop, 80, 20);
                }
                else if (plotTop == -2)
                {
                    x.Add(100);
                    y.Add(maxVal);
                    
                    StepFinishedTriggerMarshal(x, y)?.WaitOne();
                }

                if (strategy.ConvergenceCriterion == ConvergenceCriteria.Value)
                {
                    shouldContinue = maxVal - lastMaxVal > strategy.ConvergenceThreshold;
                }
                else if (strategy.ConvergenceCriterion == ConvergenceCriteria.Variables)
                {
                    double newVal = 0;

                    for (int i = 0; i < bestVals.Length; i++)
                    {
                        newVal = Math.Max(Math.Abs(bestVals[i] - lastBestVals[i]), newVal);
                    }

                    shouldContinue = newVal > strategy.ConvergenceThreshold;
                }
            }

            //Unregister thread
            StepFinishedTriggerMarshal(null, new List<double>());

            return bestVals;
        }

        public static void ConsolePlot(double[] X, double[] Y, string xLabel, string yLabel, int leftPadding = 0, int topPadding = 0, int width = 0, int height = 0, double[] xRange = null, double[] yRange = null)
        {
            if (xRange == null)
            {
                xRange = new double[] { X.Min(), X.Max() };
            }

            if (yRange == null)
            {
                yRange = new double[] { Y.Min(), Y.Max() };
            }

            if (width == 0)
            {
                width = Console.WindowWidth - 1;
            }

            if (height == 0)
            {
                height = Console.WindowHeight - 1;
            }

            height--;
            char[,] plot = new char[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    if (x == 0)
                    {
                        plot[0, y] = '|';
                    }
                    else
                    {
                        plot[x, y] = ' ';
                    }
                }
                if (x < width - xLabel.Length)
                {
                    plot[x, height - 2] = '-';
                }
                else
                {
                    plot[x, height - 2] = xLabel[x - (width - xLabel.Length)];
                }
            }

            string yRange0 = yRange[0].ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

            if (yRange0.Length > width)
            {
                yRange0 = yRange[0].ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            for (int i = 0; i < Math.Min(width, yRange0.Length); i++)
            {
                plot[i, height - 2] = yRange0[i];
            }

            for (int i = 0; i < X.Length; i++)
            {
                int x = Math.Min(1 + Math.Max(0, (int)Math.Round((X[i] - xRange[0]) / (xRange[1] - xRange[0]) * (width - 1))), width - 1);
                int y = Math.Min(1 + Math.Max(0, (int)Math.Round((1 - (Y[i] - yRange[0]) / (yRange[1] - yRange[0])) * (height - 4))), height - 2);
                plot[x, y] = '*';
            }

            bool prevCV = ConsoleWrapper.CursorVisible;

            ConsoleWrapper.CursorVisible = false;

            for (int y = 1; y < height - 1; y++)
            {
                ConsoleWrapper.SetCursorPosition(leftPadding, topPadding + y + 1);
                for (int x = 0; x < width; x++)
                {
                    ConsoleWrapper.Write(plot[x, y]);
                }
            }

            ConsoleWrapper.SetCursorPosition(leftPadding, topPadding);
            ConsoleWrapper.Write(yLabel);

            ConsoleWrapper.SetCursorPosition(leftPadding, topPadding + 1);
            string yRange1 = yRange[1].ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

            if (yRange1.Length > width)
            {
                yRange1 = yRange[1].ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            ConsoleWrapper.Write(yRange1);

            ConsoleWrapper.SetCursorPosition(leftPadding, topPadding + height);
            ConsoleWrapper.Write(xRange[0].ToString("0.####", System.Globalization.CultureInfo.InvariantCulture));
            string endLabel = xRange[1].ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
            ConsoleWrapper.Write(new string(' ', leftPadding + width - endLabel.Length - ConsoleWrapper.CursorLeft));
            ConsoleWrapper.SetCursorPosition(leftPadding + width - endLabel.Length, topPadding + height);
            ConsoleWrapper.Write(endLabel);

            ConsoleWrapper.CursorVisible = prevCV;
        }

        public static (List<Parameter> rates, List<(Parameter, int, MultivariateDistribution)> pis) ParametersToEstimateList((int mlParameterCount, int bayesParameterCount, List<List<Parameter>> ratesToEstimate, List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate) parametersToEstimate, Random randomSource)
        {
            List<Parameter> tbrRates = new List<Parameter>();

            for (int i = 0; i < parametersToEstimate.ratesToEstimate.Count; i++)
            {
                for (int j = 0; j < parametersToEstimate.ratesToEstimate[i].Count; j++)
                {
                    if (parametersToEstimate.ratesToEstimate[i][j].Action != Parameter.ParameterAction.Equal && parametersToEstimate.ratesToEstimate[i][j].Action != Parameter.ParameterAction.Fix)
                    {
                        tbrRates.Add(parametersToEstimate.ratesToEstimate[i][j]);
                    }
                }
            }

            List<(Parameter, int, MultivariateDistribution)> tbrPis = new List<(Parameter, int, MultivariateDistribution)>();

            for (int i = 0; i < parametersToEstimate.pisToEstimate.Count; i++)
            {
                List<Parameter> dirichletParams = new List<Parameter>();
                List<Parameter> multinomialParams = new List<Parameter>();

                for (int j = 0; j < parametersToEstimate.pisToEstimate[i].pis.Count; j++)
                {
                    if (parametersToEstimate.pisToEstimate[i].pis[j].Action != Parameter.ParameterAction.Equal && parametersToEstimate.pisToEstimate[i].pis[j].Action != Parameter.ParameterAction.Fix)
                    {
                        if (parametersToEstimate.pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.ML)
                        {
                            tbrPis.Add((parametersToEstimate.pisToEstimate[i].pis[j], 0, null));
                        }
                        else if (parametersToEstimate.pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Dirichlet)
                        {
                            dirichletParams.Add(parametersToEstimate.pisToEstimate[i].pis[j]);
                        }
                        else if (parametersToEstimate.pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Multinomial)
                        {
                            multinomialParams.Add(parametersToEstimate.pisToEstimate[i].pis[j]);
                        }
                    }
                }

                if (dirichletParams.Count > 1)
                {
                    MultivariateDistribution prior = new MultivariateDistribution(new Dirichlet((from el in dirichletParams select el.DistributionParameter).ToArray(), randomSource));

                    for (int j = 0; j < dirichletParams.Count; j++)
                    {
                        tbrPis.Add((dirichletParams[j], j, prior));
                    }
                }

                if (multinomialParams.Count > 1)
                {
                    MultivariateDistribution prior = new MultivariateDistribution(new Multinomial((from el in multinomialParams select el.DistributionParameter).ToArray(), 1, randomSource));

                    for (int j = 0; j < multinomialParams.Count; j++)
                    {
                        tbrPis.Add((multinomialParams[j], j, prior));
                    }
                }
            }

            return (tbrRates, tbrPis);
        }


        public static double[] ParametersPriorSample((int mlParameterCount, int bayesParameterCount, List<List<Parameter>> ratesToEstimate, List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate) parametersToEstimate, Random randomSource)
        {
            List<double> tbr = new List<double>();

            for (int i = 0; i < parametersToEstimate.ratesToEstimate.Count; i++)
            {
                for (int j = 0; j < parametersToEstimate.ratesToEstimate[i].Count; j++)
                {
                    if (parametersToEstimate.ratesToEstimate[i][j].Action == Parameter.ParameterAction.ML)
                    {
                        tbr.Add(parametersToEstimate.ratesToEstimate[i][j].Value);
                    }
                    else if (parametersToEstimate.ratesToEstimate[i][j].Action == Parameter.ParameterAction.Bayes)
                    {
                        tbr.Add(parametersToEstimate.ratesToEstimate[i][j].PriorDistribution.Sample());
                    }
                }
            }

            List<(Parameter, int, MultivariateDistribution)> tbrPis = new List<(Parameter, int, MultivariateDistribution)>();

            for (int i = 0; i < parametersToEstimate.pisToEstimate.Count; i++)
            {
                List<Parameter> dirichletParams = new List<Parameter>();
                List<Parameter> multinomialParams = new List<Parameter>();

                for (int j = 0; j < parametersToEstimate.pisToEstimate[i].pis.Count; j++)
                {
                    if (parametersToEstimate.pisToEstimate[i].pis[j].Action != Parameter.ParameterAction.Equal && parametersToEstimate.pisToEstimate[i].pis[j].Action != Parameter.ParameterAction.Fix)
                    {
                        if (parametersToEstimate.pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.ML)
                        {
                            tbr.Add(parametersToEstimate.pisToEstimate[i].pis[j].Value);
                        }
                        else if (parametersToEstimate.pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Dirichlet)
                        {
                            dirichletParams.Add(parametersToEstimate.pisToEstimate[i].pis[j]);
                        }
                        else if (parametersToEstimate.pisToEstimate[i].pis[j].Action == Parameter.ParameterAction.Multinomial)
                        {
                            multinomialParams.Add(parametersToEstimate.pisToEstimate[i].pis[j]);
                        }
                    }
                }

                if (dirichletParams.Count > 1)
                {
                    MultivariateDistribution prior = new MultivariateDistribution(new Dirichlet((from el in dirichletParams select el.DistributionParameter).ToArray(), randomSource));
                    double[] sample = prior.Sample();
                    tbr.AddRange(sample);
                }

                if (multinomialParams.Count > 1)
                {
                    MultivariateDistribution prior = new MultivariateDistribution(new Multinomial((from el in multinomialParams select el.DistributionParameter).ToArray(), 1, randomSource));
                    double[] sample = prior.Sample();
                    tbr.AddRange(sample);
                }
            }

            return tbr.ToArray();
        }

        public static List<Parameter> RatesToEstimateList((int mlParameterCount, int bayesParameterCount, List<List<Parameter>> ratesToEstimate, List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate) parametersToEstimate)
        {
            List<Parameter> tbrRates = new List<Parameter>();

            for (int i = 0; i < parametersToEstimate.ratesToEstimate.Count; i++)
            {
                for (int j = 0; j < parametersToEstimate.ratesToEstimate[i].Count; j++)
                {
                    if (parametersToEstimate.ratesToEstimate[i][j].Action != Parameter.ParameterAction.Equal && parametersToEstimate.ratesToEstimate[i][j].Action != Parameter.ParameterAction.Fix)
                    {
                        tbrRates.Add(parametersToEstimate.ratesToEstimate[i][j]);
                    }
                }
            }

            return tbrRates;
        }

        public static List<Parameter> PisToEstimateList((int mlParameterCount, int bayesParameterCount, List<List<Parameter>> ratesToEstimate, List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate) parametersToEstimate)
        {
            List<Parameter> tbrPis = new List<Parameter>();

            for (int i = 0; i < parametersToEstimate.pisToEstimate.Count; i++)
            {
                for (int j = 0; j < parametersToEstimate.pisToEstimate[i].pis.Count; j++)
                {
                    if (parametersToEstimate.pisToEstimate[i].pis[j].Action != Parameter.ParameterAction.Equal && parametersToEstimate.pisToEstimate[i].pis[j].Action != Parameter.ParameterAction.Fix)
                    {
                        tbrPis.Add(parametersToEstimate.pisToEstimate[i].pis[j]);
                    }
                }
            }

            return tbrPis;
        }

        public static (int mlParameterCount, int bayesParameterCount, List<List<Parameter>> ratesToEstimate, List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate) GetParametersToEstimate(CharacterDependency[] dependencies, Dictionary<string, Parameter>[] rates, Dictionary<string, Parameter>[] pi)
        {
            List<List<Parameter>> ratesToEstimate = new List<List<Parameter>>();
            List<(double remainingPi, List<Parameter> pis, int[] equalCounts)> pisToEstimate = new List<(double, List<Parameter>, int[])>();
            int mlParameterCount = 0;
            int bayesParameterCount = 0;


            for (int j = 0; j < dependencies.Length; j++)
            {
                if (dependencies[j].Type == CharacterDependency.Types.Independent)
                {
                    int ind = dependencies[j].Index;

                    List<Parameter> currentRates = new List<Parameter>();

                    foreach (KeyValuePair<string, Parameter> kvp in rates[ind])
                    {
                        switch (kvp.Value.Action)
                        {
                            case Parameter.ParameterAction.Equal:
                                currentRates.Add(kvp.Value);
                                break;
                            case Parameter.ParameterAction.ML:
                                mlParameterCount++;
                                currentRates.Add(kvp.Value);
                                break;
                            case Parameter.ParameterAction.Bayes:
                                bayesParameterCount++;
                                currentRates.Add(kvp.Value);
                                break;
                        }
                    }

                    if (currentRates.Count > 0)
                    {
                        ratesToEstimate.Add(currentRates);
                    }

                    bool dirichletFound = false;
                    bool mlFound = false;

                    double remainingPi = 1;
                    List<Parameter> currentPis = new List<Parameter>();

                    foreach (KeyValuePair<string, Parameter> kvp in pi[ind])
                    {
                        switch (kvp.Value.Action)
                        {
                            case Parameter.ParameterAction.Fix:
                                remainingPi -= kvp.Value.Value;
                                break;
                            case Parameter.ParameterAction.Equal:
                                if (kvp.Value.EqualParameter.Action == Parameter.ParameterAction.Fix)
                                {
                                    kvp.Value.Action = Parameter.ParameterAction.Fix;
                                    kvp.Value.Value = kvp.Value.EqualParameter.Value;
                                    remainingPi -= kvp.Value.Value;
                                }
                                else
                                {
                                    currentPis.Add(kvp.Value);
                                }
                                break;
                            case Parameter.ParameterAction.ML:
                                mlFound = true;
                                mlParameterCount++;
                                currentPis.Add(kvp.Value);
                                break;
                            case Parameter.ParameterAction.Dirichlet:
                                dirichletFound = true;
                                bayesParameterCount++;
                                currentPis.Add(kvp.Value);
                                break;
                        }
                    }

                    if (currentPis.Count > 0)
                    {
                        int[] currEqualCounts = new int[currentPis.Count];

                        for (int i = 0; i < currEqualCounts.Length; i++)
                        {
                            currEqualCounts[i] = 0;
                        }

                        for (int i = 0; i < currentPis.Count; i++)
                        {
                            if (currentPis[i].Action == Parameter.ParameterAction.Equal)
                            {
                                currEqualCounts[currentPis.IndexOf(currentPis[i].EqualParameter)]++;
                            }
                        }

                        pisToEstimate.Add((remainingPi, currentPis, currEqualCounts));
                    }

                    if (dirichletFound)
                    {
                        bayesParameterCount--;
                    }

                    if (mlFound && !dirichletFound)
                    {
                        mlParameterCount--;
                    }
                }
                else if (dependencies[j].Type == CharacterDependency.Types.Dependent)
                {
                    throw new NotImplementedException();
                }
                else if (dependencies[j].Type == CharacterDependency.Types.Conditioned)
                {
                    Dictionary<string, List<Parameter>> currentPis = new Dictionary<string, List<Parameter>>();

                    int ind = dependencies[j].Index;

                    Dictionary<string, double> remainingPi = new Dictionary<string, double>();

                    Dictionary<string, bool> dirichletFound = new Dictionary<string, bool>();
                    Dictionary<string, bool> mlFound = new Dictionary<string, bool>();

                    foreach (KeyValuePair<string, Parameter> kvp in dependencies[j].ConditionedProbabilities)
                    {
                        string stateName = kvp.Key.Substring(0, kvp.Key.IndexOf(">"));
                        if (!remainingPi.ContainsKey(stateName))
                        {
                            remainingPi.Add(stateName, 1);
                            currentPis.Add(stateName, new List<Parameter>());
                            dirichletFound.Add(stateName, false);
                            mlFound.Add(stateName, false);
                        }

                        switch (kvp.Value.Action)
                        {
                            case Parameter.ParameterAction.Fix:
                                remainingPi[stateName] -= kvp.Value.Value;
                                break;
                            case Parameter.ParameterAction.Equal:
                                if (kvp.Value.EqualParameter.Action == Parameter.ParameterAction.Fix)
                                {
                                    kvp.Value.Action = Parameter.ParameterAction.Fix;
                                    kvp.Value.Value = kvp.Value.EqualParameter.Value;
                                    remainingPi[stateName] -= kvp.Value.Value;
                                }
                                else
                                {
                                    currentPis[stateName].Add(kvp.Value);
                                }
                                break;
                            case Parameter.ParameterAction.ML:
                                mlFound[stateName] = true;
                                mlParameterCount++;
                                currentPis[stateName].Add(kvp.Value);
                                break;
                            case Parameter.ParameterAction.Dirichlet:
                                dirichletFound[stateName] = true;
                                bayesParameterCount++;
                                currentPis[stateName].Add(kvp.Value);
                                break;
                            case Parameter.ParameterAction.Multinomial:
                                dirichletFound[stateName] = true;
                                bayesParameterCount++;
                                currentPis[stateName].Add(kvp.Value);
                                break;
                        }
                    }

                    foreach (KeyValuePair<string, List<Parameter>> kvp in currentPis)
                    {
                        if (kvp.Value.Count > 0)
                        {
                            int[] currEqualCounts = new int[kvp.Value.Count];

                            for (int i = 0; i < currEqualCounts.Length; i++)
                            {
                                currEqualCounts[i] = 0;
                            }

                            for (int i = 0; i < kvp.Value.Count; i++)
                            {
                                if (kvp.Value[i].Action == Parameter.ParameterAction.Equal)
                                {
                                    currEqualCounts[kvp.Value.IndexOf(kvp.Value[i].EqualParameter)]++;
                                }
                            }

                            pisToEstimate.Add((remainingPi[kvp.Key], kvp.Value, currEqualCounts));
                        }

                        if (dirichletFound[kvp.Key])
                        {
                            bayesParameterCount--;
                        }

                        if (mlFound[kvp.Key] && !dirichletFound[kvp.Key])
                        {
                            mlParameterCount--;
                        }
                    }
                }
            }

            return (mlParameterCount, bayesParameterCount, ratesToEstimate, pisToEstimate);
        }

        public static string ShortFileName(string fullPath, int maxLen = 11)
        {
            string tbr = fullPath;

            tbr = Path.GetFileName(fullPath);

            if (tbr.Length > maxLen)
            {
                if (maxLen % 2 == 0)
                {
                    tbr = tbr.Substring(0, maxLen / 2) + "~" + tbr.Substring(tbr.Length - maxLen / 2 + 1);
                }
                else
                {
                    tbr = tbr.Substring(0, maxLen / 2) + "~" + tbr.Substring(tbr.Length - maxLen / 2);
                }
            }
            else
            {
                tbr += new string(' ', maxLen - tbr.Length);
            }

            return tbr;
        }

        public static string GetDependencyName(CharacterDependency[][] dependencies)
        {

            bool allIndependent = true;
            bool allDependent = true;

            for (int i = 0; i < dependencies.Length; i++)
            {
                for (int j = 0; j < dependencies[i].Length; j++)
                {
                    if (dependencies[i][j].Type != CharacterDependency.Types.Independent)
                    {
                        allIndependent = false;
                    }
                    if (dependencies[i][j].Type != CharacterDependency.Types.Dependent)
                    {
                        allDependent = false;
                    }
                }
            }

            if (allIndependent)
            {
                return "All independent";
            }

            if (allDependent)
            {
                return "All dependent";
            }

            return "Custom";
        }

        public static string GetDependencySource(CharacterDependency[][] dependencies, bool pis, bool rates, bool condProbs)
        {
            string tbr = "";

            for (int i = 0; i < dependencies.Length; i++)
            {
                tbr += "Begin Dependency;\n";

                if (dependencies[i].Length > 1)
                {
                    tbr += "\n";
                }

                List<CharacterDependency> sortedDependencies = new List<CharacterDependency>(dependencies[i]);

                sortedDependencies.Sort((a, b) =>
                {
                    if (a.Type == CharacterDependency.Types.Independent && b.Type == CharacterDependency.Types.Independent)
                    {
                        return a.Index - b.Index;
                    }
                    else if (a.Type == CharacterDependency.Types.Independent)
                    {
                        return -1;
                    }
                    else if (b.Type == CharacterDependency.Types.Independent)
                    {
                        return 1;
                    }
                    else if (a.Type == CharacterDependency.Types.Dependent && b.Type == CharacterDependency.Types.Dependent)
                    {
                        return a.Dependencies.Min() - b.Dependencies.Min();
                    }
                    else if (a.Type == CharacterDependency.Types.Dependent)
                    {
                        return -1;
                    }
                    else if (b.Type == CharacterDependency.Types.Dependent)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                });

                for (int j = 0; j < sortedDependencies.Count; j++)
                {
                    tbr += sortedDependencies[j].ToString(sortedDependencies.ToArray(), rates, pis, condProbs);
                    if (j < sortedDependencies.Count - 1)
                    {
                        tbr += "\n";
                    }
                }

                if (dependencies[i].Length > 1)
                {
                    tbr += "\n";
                }

                tbr += "End;";


                if (i < dependencies.Length - 1)
                {
                    tbr += "\n\n";
                }
            }

            return tbr;
        }


        public static (CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] pi, Dictionary<string, Parameter>[] rates, DataMatrix data) GetRealDependencies(CharacterDependency[][] inputDependencies, DataMatrix inputData, Dictionary<string, Parameter>[] inputPi = null, Dictionary<string, Parameter>[] inputRates = null)
        {
            if (inputPi == null)
            {
                inputPi = Parsing.GetDefaultPi(inputData.States);
            }

            if (inputRates == null)
            {
                inputRates = Parsing.GetDefaultRates(inputData.States);
            }

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
                            allStates[k] = Utils.Range(0, inputData.States[inputDependencies[i][j].Dependencies[k]].Length);
                        }
                        int[][] combinedStates = Utils.GetCombinations(allStates);
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

                        currentDependencies.Add(new CharacterDependency(newIndex) { InputDependencyName = Utils.StringifyArray(inputDependencies[i][j].Dependencies) });

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

            return (dependencies, pi, rates, data);
        }


        public static string GetPisSource(CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] pis)
        {
            string tbr = "";

            for (int i = 0; i < pis.Length; i++)
            {
                CharacterDependency dependency = null;

                for (int j = 0; j < dependencies.Length; j++)
                {
                    for (int k = 0; k < dependencies[j].Length; k++)
                    {
                        if (dependencies[j][k].Index == i)
                        {
                            dependency = dependencies[j][k];
                        }
                    }
                }

                if (dependency.Type != CharacterDependency.Types.Conditioned)
                {

                    tbr += "Begin Pi;\n";

                    string originalName = "";



                    if (!string.IsNullOrEmpty(dependency.InputDependencyName))
                    {
                        originalName = " [Original: " + dependency.InputDependencyName + "]";
                    }

                    tbr += "\n\tCharacter: " + i.ToString() + originalName + ";\n\n";


                    bool hasFixed = false;
                    bool hasDirichlet = false;

                    foreach (KeyValuePair<string, Parameter> kvp in pis[i])
                    {
                        if (kvp.Value.Action == Parameter.ParameterAction.Fix || kvp.Value.Action == Parameter.ParameterAction.Equal)
                        {
                            hasFixed = true;
                        }
                        else if (kvp.Value.Action == Parameter.ParameterAction.Dirichlet)
                        {
                            hasDirichlet = true;
                        }
                    }

                    if (hasFixed)
                    {
                        tbr += "\tFixed:\n";

                        foreach (KeyValuePair<string, Parameter> kvp in pis[i])
                        {
                            if (kvp.Value.Action == Parameter.ParameterAction.Fix || kvp.Value.Action == Parameter.ParameterAction.Equal)
                            {
                                tbr += "\t\t" + kvp.Key + ": " + kvp.Value.ToString(pis[i]) + "\n";
                            }
                        }

                        tbr += "\t;\n";
                    }

                    if (hasDirichlet)
                    {
                        tbr += "\tDirichlet:\n";

                        foreach (KeyValuePair<string, Parameter> kvp in pis[i])
                        {
                            if (kvp.Value.Action == Parameter.ParameterAction.Dirichlet)
                            {
                                tbr += "\t\t" + kvp.Key + ": " + kvp.Value.DistributionParameter + "\n";
                            }
                        }

                        tbr += "\t;\n";
                    }

                    tbr += "\nEnd;";

                    if (i < pis.Length - 1)
                    {
                        tbr += "\n\n\n";
                    }
                }
            }

            return tbr;
        }


        public static string GetOriginalPisSource(CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] pis)
        {
            string tbr = "";

            for (int i = 0; i < pis.Length; i++)
            {
                CharacterDependency dependency = null;

                for (int j = 0; j < dependencies.Length; j++)
                {
                    for (int k = 0; k < dependencies[j].Length; k++)
                    {
                        if (dependencies[j][k].Index == i)
                        {
                            dependency = dependencies[j][k];
                        }
                    }
                }

                if (dependency.Type != CharacterDependency.Types.Conditioned && !dependency.InputDependencyName.Contains(","))
                {

                    tbr += "Begin Pi;\n";

                    tbr += "\n\tCharacter: " + dependency.InputDependencyName + ";\n\n";


                    bool hasFixed = false;
                    bool hasDirichlet = false;

                    foreach (KeyValuePair<string, Parameter> kvp in pis[i])
                    {
                        if (kvp.Value.Action == Parameter.ParameterAction.Fix || kvp.Value.Action == Parameter.ParameterAction.Equal)
                        {
                            hasFixed = true;
                        }
                        else if (kvp.Value.Action == Parameter.ParameterAction.Dirichlet)
                        {
                            hasDirichlet = true;
                        }
                    }

                    if (hasFixed)
                    {
                        tbr += "\tFixed:\n";

                        foreach (KeyValuePair<string, Parameter> kvp in pis[i])
                        {
                            if (kvp.Value.Action == Parameter.ParameterAction.Fix || kvp.Value.Action == Parameter.ParameterAction.Equal)
                            {
                                tbr += "\t\t" + kvp.Key + ": " + kvp.Value.ToString(pis[i]) + "\n";
                            }
                        }

                        tbr += "\t;\n";
                    }

                    if (hasDirichlet)
                    {
                        tbr += "\tDirichlet:\n";

                        foreach (KeyValuePair<string, Parameter> kvp in pis[i])
                        {
                            if (kvp.Value.Action == Parameter.ParameterAction.Dirichlet)
                            {
                                tbr += "\t\t" + kvp.Key + ": " + kvp.Value.DistributionParameter + "\n";
                            }
                        }

                        tbr += "\t;\n";
                    }

                    tbr += "\nEnd;";

                    if (i < pis.Length - 1)
                    {
                        tbr += "\n\n\n";
                    }
                }
            }

            return tbr;
        }


        public static string GetRatesSource(CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] rates)
        {
            string tbr = "";

            for (int i = 0; i < rates.Length; i++)
            {
                CharacterDependency dependency = null;

                for (int j = 0; j < dependencies.Length; j++)
                {
                    for (int k = 0; k < dependencies[j].Length; k++)
                    {
                        if (dependencies[j][k].Index == i)
                        {
                            dependency = dependencies[j][k];
                        }
                    }
                }

                if (dependency.Type != CharacterDependency.Types.Conditioned)
                {

                    tbr += "Begin Rates;\n";

                    string originalName = "";



                    if (!string.IsNullOrEmpty(dependency.InputDependencyName))
                    {
                        originalName = " [Original: " + dependency.InputDependencyName + "]";
                    }

                    tbr += "\n\tCharacter: " + i.ToString() + originalName + ";\n\n";

                    tbr += "\tRates:\n";

                    foreach (KeyValuePair<string, Parameter> kvp in rates[i])
                    {
                        tbr += "\t\t" + kvp.Key + ": " + kvp.Value.ToString(rates[i]) + "\n";
                    }

                    tbr += "\t;\n";

                    tbr += "\nEnd;";

                    if (i < rates.Length - 1)
                    {
                        tbr += "\n\n\n";
                    }
                }
            }

            return tbr;
        }

        public static string GetOriginalRatesSource(CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] rates)
        {
            string tbr = "";

            for (int i = 0; i < rates.Length; i++)
            {
                CharacterDependency dependency = null;

                for (int j = 0; j < dependencies.Length; j++)
                {
                    for (int k = 0; k < dependencies[j].Length; k++)
                    {
                        if (dependencies[j][k].Index == i)
                        {
                            dependency = dependencies[j][k];
                        }
                    }
                }

                if (dependency.Type != CharacterDependency.Types.Conditioned && !dependency.InputDependencyName.Contains(","))
                {

                    tbr += "Begin Rates;\n";

                    tbr += "\n\tCharacter: " + dependency.InputDependencyName + ";\n\n";

                    tbr += "\tRates:\n";

                    foreach (KeyValuePair<string, Parameter> kvp in rates[i])
                    {
                        tbr += "\t\t" + kvp.Key + ": " + kvp.Value.ToString(rates[i]) + "\n";
                    }

                    tbr += "\t;\n";

                    tbr += "\nEnd;";

                    if (i < rates.Length - 1)
                    {
                        tbr += "\n\n\n";
                    }
                }
            }

            return tbr;
        }

        public static string GetPisName(CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] pis)
        {
            bool hasFixed = false;
            bool hasBayesian = false;

            for (int i = 0; i < pis.Length; i++)
            {
                CharacterDependency dependency = null;

                for (int j = 0; j < dependencies.Length; j++)
                {
                    for (int k = 0; k < dependencies[j].Length; k++)
                    {
                        if (dependencies[j][k].Index == i)
                        {
                            dependency = dependencies[j][k];
                        }
                    }
                }

                if (dependency.Type != CharacterDependency.Types.Conditioned)
                {
                    foreach (KeyValuePair<string, Parameter> kvp in pis[i])
                    {
                        if (kvp.Value.Action == Parameter.ParameterAction.Fix || kvp.Value.Action == Parameter.ParameterAction.Equal)
                        {
                            hasFixed = true;
                        }
                        else if (kvp.Value.Action == Parameter.ParameterAction.Dirichlet)
                        {
                            hasBayesian = true;
                        }
                    }
                }
            }

            if (hasFixed && hasBayesian)
            {
                return "Fixed & Bayesian";
            }
            else if (hasFixed)
            {
                return "Fixed";
            }
            else
            {
                return "Bayesian";
            }
        }


        public static string GetRatesName(CharacterDependency[][] dependencies, Dictionary<string, Parameter>[] rates)
        {
            bool hasFixed = false;
            bool hasBayesian = false;
            bool hasML = false;

            for (int i = 0; i < rates.Length; i++)
            {
                CharacterDependency dependency = null;

                for (int j = 0; j < dependencies.Length; j++)
                {
                    for (int k = 0; k < dependencies[j].Length; k++)
                    {
                        if (dependencies[j][k].Index == i)
                        {
                            dependency = dependencies[j][k];
                        }
                    }
                }

                if (dependency.Type != CharacterDependency.Types.Conditioned)
                {
                    foreach (KeyValuePair<string, Parameter> kvp in rates[i])
                    {
                        if (kvp.Value.Action == Parameter.ParameterAction.Fix)
                        {
                            hasFixed = true;
                        }
                        else if (kvp.Value.Action == Parameter.ParameterAction.ML)
                        {
                            hasML = true;
                        }
                        else if (kvp.Value.Action == Parameter.ParameterAction.Bayes)
                        {
                            hasBayesian = true;
                        }
                    }
                }
            }

            if (hasFixed && hasBayesian && hasML)
            {
                return "Fixed & ML & Bayesian";
            }
            else if (hasFixed && hasBayesian)
            {
                return "Fixed & Bayesian";
            }
            else if (hasFixed && hasML)
            {
                return "Fixed & ML";
            }
            else if (hasBayesian && hasML)
            {
                return "ML & Bayesian";
            }
            else if (hasFixed)
            {
                return "Fixed";
            }
            else if (hasML)
            {
                return "Maximum-likelihood";
            }
            else if (hasBayesian)
            {
                return "Bayesian";
            }

            return "Unknown";
        }

        public static string GetCondProbsName(CharacterDependency[][] dependencies)
        {
            bool hasFixed = false;
            bool hasBayesian = false;
            bool hasML = false;


            for (int i = 0; i < dependencies.Length; i++)
            {
                for (int j = 0; j < dependencies[i].Length; j++)
                {
                    if (dependencies[i][j].Type == CharacterDependency.Types.Conditioned)
                    {
                        foreach (KeyValuePair<string, Parameter> kvp in dependencies[i][j].ConditionedProbabilities)
                        {
                            if (kvp.Value.Action == Parameter.ParameterAction.Dirichlet || kvp.Value.Action == Parameter.ParameterAction.Multinomial)
                            {
                                hasBayesian = true;
                            }
                            else if (kvp.Value.Action == Parameter.ParameterAction.ML)
                            {
                                hasML = true;
                            }
                            else if (kvp.Value.Action == Parameter.ParameterAction.Fix)
                            {
                                hasFixed = true;
                            }
                        }
                    }
                }
            }

            if (hasFixed && hasBayesian && hasML)
            {
                return "Fixed & ML & Bayesian";
            }
            else if (hasFixed && hasBayesian)
            {
                return "Fixed & Bayesian";
            }
            else if (hasFixed && hasML)
            {
                return "Fixed & ML";
            }
            else if (hasBayesian && hasML)
            {
                return "ML & Bayesian";
            }
            else if (hasFixed)
            {
                return "Fixed";
            }
            else if (hasML)
            {
                return "Maximum-likelihood";
            }
            else if (hasBayesian)
            {
                return "Bayesian";
            }

            return "Unknown";
        }
    }
}
