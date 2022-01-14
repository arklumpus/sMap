using Mono.Options;
using SlimTreeNode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace NodeInfo
{
    class Program
    {
        static ConsoleColor[] stateColors = new ConsoleColor[] { ConsoleColor.Red, ConsoleColor.Blue, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.Cyan, ConsoleColor.Yellow, ConsoleColor.Gray };

        static ConsoleFrameBuffer mainFrameBuffer;

        static double position;
        static int nodeInd;
        static List<TreeNode> allNodes;

        const string version = "1.0.0";

        static int Main(string[] args)
        {
            string sMapFilename = "";

            List<string[]> monophyleticConstraint = new List<string[]>();
            List<int> nodeInds = new List<int>();
            List<double> positions = new List<double>();
            position = 0;
            bool simple = false;
            bool batch = false;
            string format = "";
            bool showHelp = false;

            OptionSet argParser = new OptionSet()
            {
                { "h|help", "Print this message and exit.", v => { showHelp = v != null; } },
                { "s|smap=", "Input file produced by sMap run", v => { sMapFilename = v; } },
                { "n|node-id=", "Specify which {0:node(s)} to analyse using node ids. Multiple node ids can be separated by a comma    (,). This option may be specified multiple times. If neither the --simple or --batch options are specified, only the first node will be analysed.", v => { nodeInds.AddRange(from el in v.Split(',') select int.Parse(el)); } },
                { "m|mono-constraint={ }", "Specify which node to analyse as the least inclusive monophyletic node including {0:taxon_1} and {1:taxon_2}. This option may be specified multiple times. If neither the --simple or       --batch options are specified, only the first node will be analysed.", (a, b) => { monophyleticConstraint.Add(new string[] { a, b }); } },
                { "p|position=", "Optional. Specify the positions(s) to analyse (starting from the leaf-end of each branch). Multiple positions can be separated by a comma    (,). This option may be specified multiple times (once per each node specified). If neither the --simple or       --batch options are specified, only the first node will be analysed. Default: 0.", v => { positions.AddRange(from el in v.Split(',') select double.Parse(el, System.Globalization.CultureInfo.InvariantCulture)); } },
                { "simple", "Optional. Display node information without plots and exit. Default: off.", v => { simple = v != null; } },
                { "batch", "Optional. Only display node id and state probabilities and exit. Default: off", v => { batch = v != null; } },
                { "f|format=", "Optional. When combined with the --batch option, it specifies the format of the output string. It can contain escape characters (e.g. \\t, \\n). Available output parameters are:\n%I%\tNode id\n%P%\tPosition\n%L%\tBranch length\n%C%\tNo. of internal children\n%l%\tNo. of internal leaves\n%D1%\tDefining child 1\n%D2%\tDefining child 2\n%##%\tTotal no. of states\n%#n%\tWhere n is a 0-based integer. Name of\n    \tstate n. E.g.: %#1%.\n%p(#n)%\tWhere n is a 0-based integer. Posterior\n    \tprobability of state n. E.g. %p(#0)%.\n%p(s)%\tWhere s is a state name. Posterior\n    \tprobability of state s. E.g. %p(A)%.", v => { format = v; } }
            };

            List<string> unrecognised = argParser.Parse(args);


            SerializedRun run = null;

            try
            {
                bool showUsage = false;
                
                if (!showHelp && monophyleticConstraint.Count > 0)
                {

                    run = SerializedRun.Deserialize(sMapFilename);
                    allNodes = run.SummaryTree.GetChildrenRecursive();

                    for (int k = 0; k < monophyleticConstraint.Count; k++)
                    {
                        TreeNode node = run.SummaryTree.GetMonophyleticGroup(monophyleticConstraint[k]);

                        if (node == null)
                        {
                            ConsoleWrapper.WriteLine("Could not find the specified node!");
                            return 1;
                        }

                        nodeInds.Add(allNodes.IndexOf(node));
                    }
                }

                if (nodeInds.Count == 0 && !showHelp)
                {
                    ConsoleWrapper.WriteLine("You need to specify at least one node!");
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
                    if (string.IsNullOrEmpty(sMapFilename))
                    {
                        ConsoleWrapper.WriteLine();
                        ConsoleWrapper.WriteLine("No sMap run file specified!");
                        showUsage = true;
                    }
                }


                if (showUsage || showHelp)
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("NodeInfo version {0}", version);
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("Usage:");
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("  NodeInfo {-h|--help}");
                    ConsoleWrapper.WriteLine("  NodeInfo -s <sMap_file> -n <node_id>[,<node_id>[,...]] [options...]");
                    ConsoleWrapper.WriteLine("  NodeInfo -s <sMap_file> -m <taxon_1> <taxon_2> [options...]");
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

                if (run == null)
                {
                    run = SerializedRun.Deserialize(sMapFilename);
                    allNodes = run.SummaryTree.GetChildrenRecursive();
                }

                bool isClockLike = run.TreesClockLike;
                LikelihoodModel summaryLikelihoodModel = new LikelihoodModel(run.SummaryTree);


                while (positions.Count < nodeInds.Count)
                {
                    positions.Add(0);
                }

                if (batch)
                {
                    for (int k = 0; k < nodeInds.Count; k++)
                    {
                        if (string.IsNullOrEmpty(format))
                        {
                            TreeNode node = allNodes[nodeInds[k]];
                            nodeInd = nodeInds[k];
                            position = positions[k];

                            position = Math.Min(Math.Max(0, position), node.Length);

                            double[] conditionedMeanPosteriorAtPos;

                            if (isClockLike)
                            {
                                conditionedMeanPosteriorAtPos = Utils.Utils.GetBranchStateProbs(run.Histories, run.TreeSamples, run.LikelihoodModels, summaryLikelihoodModel, run.SummaryNodeCorresp, new List<string>(run.AllPossibleStates), summaryLikelihoodModel.BranchLengths.Length - nodeInd - 1, position, true);
                            }
                            else
                            {
                                conditionedMeanPosteriorAtPos = Utils.Utils.GetBranchStateProbs(run.Histories, run.TreeSamples, run.LikelihoodModels, summaryLikelihoodModel, run.SummaryNodeCorresp, new List<string>(run.AllPossibleStates), summaryLikelihoodModel.BranchLengths.Length - nodeInd - 1, node.Length - position, false);
                            }

                            if (!isClockLike && nodeInd == 0 && position == 0)
                            {
                                conditionedMeanPosteriorAtPos = run.MeanPosterior.Last();
                            }

                            ConsoleWrapper.WriteLine(nodeInd.ToString());

                            for (int i = 0; i < run.States.Length; i++)
                            {
                                ConsoleWrapper.WriteLine(run.States[i] + "\t" + conditionedMeanPosteriorAtPos[i].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                            }
                        }
                        else
                        {
                            TreeNode node = allNodes[nodeInds[k]];
                            nodeInd = nodeInds[k];
                            position = positions[k];

                            position = Math.Min(Math.Max(0, position), node.Length);

                            double[] conditionedMeanPosteriorAtPos;

                            if (isClockLike)
                            {
                                conditionedMeanPosteriorAtPos = Utils.Utils.GetBranchStateProbs(run.Histories, run.TreeSamples, run.LikelihoodModels, summaryLikelihoodModel, run.SummaryNodeCorresp, new List<string>(run.AllPossibleStates), summaryLikelihoodModel.BranchLengths.Length - nodeInd - 1, position, true);
                            }
                            else
                            {
                                conditionedMeanPosteriorAtPos = Utils.Utils.GetBranchStateProbs(run.Histories, run.TreeSamples, run.LikelihoodModels, summaryLikelihoodModel, run.SummaryNodeCorresp, new List<string>(run.AllPossibleStates), summaryLikelihoodModel.BranchLengths.Length - nodeInd - 1, node.Length - position, false);
                            }

                            if (!isClockLike && nodeInd == 0 && position == 0)
                            {
                                conditionedMeanPosteriorAtPos = run.MeanPosterior.Last();
                            }

                            List<string> leaves = node.GetLeafNames();

                            int nodeLeafCount = leaves.Count;
                            int nodeChildCount = node.GetChildrenRecursive().Count - nodeLeafCount - 1;

                            

                            string nodeIndStr = nodeInd.ToString();
                            string positionString = position.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            string branchLength = node.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            string nodeChildString = nodeChildCount.ToString();
                            string nodeLeafString = nodeLeafCount.ToString();
                            string definingChild1 = leaves[0];
                            string definingChild2 = leaves.Last();
                            string totalStates = run.States.Length.ToString();

                            string formatted = Regex.Unescape(format).Replace("%I%", nodeIndStr).Replace("%P%", positionString).Replace("%L%", branchLength).Replace("%C%", nodeChildString).Replace("%l%", nodeLeafString).Replace("%D1%", definingChild1).Replace("%D2%", definingChild2).Replace("%##%", totalStates);

                            for (int i = 0; i < run.States.Length; i++)
                            {
                                formatted = formatted.Replace("%#" + i.ToString() + "%", run.States[i]);
                                formatted = formatted.Replace("%p(#" + i.ToString() + ")%", conditionedMeanPosteriorAtPos[i].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                                formatted = formatted.Replace("%p(" + run.States[i] + ")%", conditionedMeanPosteriorAtPos[i].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                            }

                            ConsoleWrapper.WriteLine(formatted);
                        }
                    }
                }
                else if (simple)
                {
                    for (int k = 0; k < nodeInds.Count; k++)
                    {
                        TreeNode node = allNodes[nodeInds[k]];
                        nodeInd = nodeInds[k];
                        position = positions[k];

                        position = Math.Min(Math.Max(0, position), node.Length);

                        double[] conditionedMeanPosteriorAtPos;

                        if (isClockLike)
                        {
                            conditionedMeanPosteriorAtPos = Utils.Utils.GetBranchStateProbs(run.Histories, run.TreeSamples, run.LikelihoodModels, summaryLikelihoodModel, run.SummaryNodeCorresp, new List<string>(run.AllPossibleStates), summaryLikelihoodModel.BranchLengths.Length - nodeInd - 1, position, true);
                        }
                        else
                        {
                            conditionedMeanPosteriorAtPos = Utils.Utils.GetBranchStateProbs(run.Histories, run.TreeSamples, run.LikelihoodModels, summaryLikelihoodModel, run.SummaryNodeCorresp, new List<string>(run.AllPossibleStates), summaryLikelihoodModel.BranchLengths.Length - nodeInd - 1, node.Length - position, false);
                        }

                        if (!isClockLike && nodeInd == 0 && position == 0)
                        {
                            conditionedMeanPosteriorAtPos = run.MeanPosterior.Last();
                        }

                        List<string> leaves = node.GetLeafNames();

                        int nodeLeafCount = leaves.Count;
                        int nodeChildCount = node.GetChildrenRecursive().Count - nodeLeafCount - 1;

                        ConsoleWrapper.WriteLine("Node id: {0}", nodeInd.ToString());

                        ConsoleWrapper.WriteLine();



                        ConsoleWrapper.WriteLine(node.Parent != null ? "Branch length: " + node.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) : "Root node");
                        ConsoleWrapper.WriteLine(node.Children.Count > 0 ? "Internal nodes: " + nodeChildCount.ToString() : "Leaf node");
                        ConsoleWrapper.WriteLine(node.Children.Count > 0 ? "Tip children: " + nodeLeafCount.ToString() : "");
                        ConsoleWrapper.WriteLine();

                        ConsoleWrapper.WriteLine(node.Parent != null ? "Position along the branch: " + position.ToString(System.Globalization.CultureInfo.InvariantCulture) : "");

                        if (node.Children.Count > 0)
                        {
                            ConsoleWrapper.WriteLine("Defining children: " + leaves[0]);
                            ConsoleWrapper.WriteLine("                   " + leaves.Last());
                        }
                        else
                        {
                            ConsoleWrapper.WriteLine("Leaf name: " + node.Name);
                            ConsoleWrapper.WriteLine();
                        }

                        ConsoleWrapper.WriteLine();

                        ConsoleWrapper.WriteLine("Mean conditioned state posteriors:");

                        for (int i = 0; i < run.States.Length; i++)
                        {
                            ConsoleWrapper.WriteLine("    " + run.States[i] + ": " + conditionedMeanPosteriorAtPos[i].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
                        }

                        ConsoleWrapper.WriteLine();
                    }
                }
                else
                {
                    TreeNode node = allNodes[nodeInds[0]];
                    nodeInd = nodeInds[0];
                    position = positions[0];

                    Console.OutputEncoding = System.Text.Encoding.UTF8;

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

                    object lockObject = new object();

                    double newPosition = position;
                    int newNodeInd = nodeInd;

                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                    {

                        Thread drawThread = new Thread(() =>
                        {
                            while (!drawCloseHandle.WaitOne(0))
                            {
                                drawHandle.WaitOne();

                                lock (lockObject)
                                {
                                    position = newPosition;
                                    nodeInd = newNodeInd;
                                }

                                drawHandle.Reset();

                                PlotEverything(run);
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
                                position = newPosition;
                                nodeInd = newNodeInd;
                            }

                            PlotEverything(run);
                        }

                        ConsoleKeyInfo ki = Console.ReadKey(true);
                        lock (lockObject)
                        {
                            switch (ki.Key)
                            {
                                case ConsoleKey.LeftArrow:
                                    if (newPosition < allNodes[nodeInd].Length)
                                    {
                                        newPosition = Math.Min(allNodes[nodeInd].Length, newPosition + allNodes[nodeInd].Length / (mainFrameBuffer.WindowWidth - 1));
                                    }
                                    break;
                                case ConsoleKey.RightArrow:
                                    if (newPosition > 0)
                                    {
                                        newPosition = Math.Max(0, newPosition - allNodes[nodeInd].Length / (mainFrameBuffer.WindowWidth - 1));
                                    }
                                    break;

                                case ConsoleKey.UpArrow:
                                    if (newNodeInd < allNodes.Count - 1)
                                    {
                                        newNodeInd++;
                                        newPosition = 0;
                                    }
                                    break;
                                case ConsoleKey.DownArrow:
                                    if (newNodeInd > 0)
                                    {
                                        newNodeInd--;
                                        newPosition = 0;
                                    }
                                    break;
                                case ConsoleKey.Q:
                                    closeHandle.Set();
                                    drawCloseHandle.Set();
                                    drawHandle.Set();
                                    break;
                            }
                        }

                    }

                    prepareExit();
                }

                return 0;
            }
            catch (Exception e)
            {
                ConsoleWrapper.WriteLine("NodeInfo: Error! " + e.Message);
                return 1;
            }
        }

        static void prepareExit()
        {
            Console.Clear();
            Console.CursorVisible = true;
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        static void PlotEverything(SerializedRun run)
        {
            TreeNode node = allNodes[nodeInd];

            ConsoleFrameBuffer frameBuffer = new ConsoleFrameBuffer();

            double[] conditionedMeanPosterior;
            bool isClockLike = run.TreesClockLike;

            LikelihoodModel summaryLikelihoodModel = new LikelihoodModel(run.SummaryTree);

            if (isClockLike)
            {
                conditionedMeanPosterior = Utils.Utils.GetBranchStateProbs(run.Histories, run.TreeSamples, run.LikelihoodModels, summaryLikelihoodModel, run.SummaryNodeCorresp, new List<string>(run.AllPossibleStates), summaryLikelihoodModel.BranchLengths.Length - nodeInd - 1, 0, true);
            }
            else
            {
                conditionedMeanPosterior = Utils.Utils.GetBranchStateProbs(run.Histories, run.TreeSamples, run.LikelihoodModels, summaryLikelihoodModel, run.SummaryNodeCorresp, new List<string>(run.AllPossibleStates), summaryLikelihoodModel.BranchLengths.Length - nodeInd - 1, node.Length, false);
            }

            if (!isClockLike && nodeInd == 0)
            {
                conditionedMeanPosterior = run.MeanPosterior.Last();
            }


            double[] conditionedMeanPosteriorAtPos;

            if (isClockLike)
            {
                conditionedMeanPosteriorAtPos = Utils.Utils.GetBranchStateProbs(run.Histories, run.TreeSamples, run.LikelihoodModels, summaryLikelihoodModel, run.SummaryNodeCorresp, new List<string>(run.AllPossibleStates), summaryLikelihoodModel.BranchLengths.Length - nodeInd - 1, position, true);
            }
            else
            {
                conditionedMeanPosteriorAtPos = Utils.Utils.GetBranchStateProbs(run.Histories, run.TreeSamples, run.LikelihoodModels, summaryLikelihoodModel, run.SummaryNodeCorresp, new List<string>(run.AllPossibleStates), summaryLikelihoodModel.BranchLengths.Length - nodeInd - 1, node.Length - position, false);
            }

            if (!isClockLike && nodeInd == 0 && position == 0)
            {
                conditionedMeanPosteriorAtPos = run.MeanPosterior.Last();
            }

            List<string> leaves = node.GetLeafNames();

            int nodeLeafCount = leaves.Count;
            int nodeChildCount = node.GetChildrenRecursive().Count - nodeLeafCount - 1;

            position = Math.Min(Math.Max(0, position), node.Length);

            frameBuffer.Write("Node id: ");

            if (nodeInd > 0)
            {
                frameBuffer.ForegroundColor = ConsoleColor.Green;
            }

            frameBuffer.Write("▼");

            frameBuffer.ForegroundColor = ConsoleColor.Gray;
            frameBuffer.Write(" " + nodeInd.ToString() + " ");

            if (nodeInd < allNodes.Count - 1)
            {
                frameBuffer.ForegroundColor = ConsoleColor.Green;
            }

            frameBuffer.Write("▲");

            frameBuffer.ForegroundColor = ConsoleColor.Gray;

            frameBuffer.WriteLine();
            frameBuffer.WriteLine();



            frameBuffer.WriteLine(node.Parent != null ? "Branch length: " + node.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) : "Root node");
            frameBuffer.WriteLine(node.Children.Count > 0 ? "Internal nodes: " + nodeChildCount.ToString() : "Leaf node");
            frameBuffer.WriteLine(node.Children.Count > 0 ? "Tip children: " + nodeLeafCount.ToString() : "");
            frameBuffer.WriteLine();

            frameBuffer.WriteLine(node.Parent != null ? "Position along the branch: " + position.ToString(System.Globalization.CultureInfo.InvariantCulture) : "");

            if (node.Children.Count > 0)
            {
                frameBuffer.WriteLine("Defining children: " + leaves[0]);
                frameBuffer.CursorLeft = 19;
                frameBuffer.WriteLine(leaves.Last());
            }
            else
            {
                frameBuffer.WriteLine("Leaf name: " + node.Name);
                frameBuffer.WriteLine();
            }

            frameBuffer.WriteLine();

            frameBuffer.WriteLine("Mean conditioned state posteriors:");

            for (int i = 0; i < run.States.Length; i++)
            {
                frameBuffer.ForegroundColor = stateColors[i % stateColors.Length];
                frameBuffer.Write("  █");
                frameBuffer.ForegroundColor = ConsoleColor.Gray;
                frameBuffer.WriteLine(" " + run.States[i] + ": " + conditionedMeanPosteriorAtPos[i].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            }

            int writtenLines = frameBuffer.CursorTop;

            int circleDiameter = Math.Min((frameBuffer.WindowWidth - 40) / 2, Math.Max(writtenLines, frameBuffer.WindowHeight / 2) - 3 - (6 * run.States.Length) / (frameBuffer.WindowWidth - 40));

            int howManyLegendsPerLine = Math.Min((frameBuffer.WindowWidth - 40) / (6 * run.States.Length), run.States.Length);

            int padLeft = ((frameBuffer.WindowWidth - 40) - (howManyLegendsPerLine - 1) * 6 + 3) / 2;

            frameBuffer.CursorTop = 0;
            frameBuffer.CursorLeft = 40 + padLeft;

            frameBuffer.CursorLeft = 40;
            

            List<double> angles = new List<double>();
            double currAng = 0;

            for (int i = 0; i < run.States.Length; i++)
            {
                currAng += 2 * Math.PI * conditionedMeanPosterior[i];
                angles.Add(currAng);
            }

            int circleTop = frameBuffer.CursorTop;

            double circleCenterX = (frameBuffer.WindowWidth - 40) * 0.5;
            double circleCenterY = circleTop + circleDiameter * 0.5 + 2;

            for (int y = 0; y <= circleDiameter + 2; y++)
            {
                for (int x = 0; x < frameBuffer.WindowWidth - 40; x++)
                {
                    if (Math.Sqrt((circleCenterX - x) * (circleCenterX - x) * 0.25 + (circleCenterY - y - 0.5) * (circleCenterY - y - 0.5)) <= circleDiameter * 0.5)
                    {
                        double angle = Math.Atan2((circleCenterY - y - 0.5), (circleCenterX - x) * 0.5);

                        while (angle < 0)
                        {
                            angle += Math.PI * 2;
                        }

                        angle = angle % (2 * Math.PI);

                        int currInd = 0;

                        while (currInd < angles.Count && angles[currInd] <= angle)
                        {
                            currInd++;
                        }

                        frameBuffer.ForegroundColor = stateColors[currInd % stateColors.Length];

                        frameBuffer.Write("█");
                    }
                    else
                    {
                        frameBuffer.CursorLeft++;
                    }
                }
                frameBuffer.CursorTop = circleTop + y;
                frameBuffer.CursorLeft = 40;
            }

            if (node.Parent != null)
            {

                frameBuffer.ForegroundColor = ConsoleColor.Gray;
                frameBuffer.CursorLeft = 0;
                frameBuffer.WriteLine("Branch plot:");

                frameBuffer.CursorTop++;

                frameBuffer.CursorLeft = 0;
                if (position < node.Length)
                {
                    frameBuffer.ForegroundColor = ConsoleColor.Green;
                }

                frameBuffer.Write("◄");

                frameBuffer.ForegroundColor = ConsoleColor.Gray;
                frameBuffer.Write(" To root");

                frameBuffer.CursorLeft = frameBuffer.WindowWidth - 9;
                frameBuffer.Write("To tips ");
                if (position > 0)
                {
                    frameBuffer.ForegroundColor = ConsoleColor.Green;
                }
                frameBuffer.Write("►");

                int startBranchPlot = frameBuffer.CursorTop;

                List<double>[] branchPlotProps = new List<double>[frameBuffer.WindowWidth];

                for (int x = 0; x < frameBuffer.WindowWidth; x++)
                {
                    branchPlotProps[x] = new List<double>();

                    double currProp = 0;

                    double[] probs;

                    if (isClockLike)
                    {
                        probs = Utils.Utils.GetBranchStateProbs(run.Histories, run.TreeSamples, run.LikelihoodModels, summaryLikelihoodModel, run.SummaryNodeCorresp, new List<string>(run.AllPossibleStates), summaryLikelihoodModel.BranchLengths.Length - nodeInd - 1, (1 - (double)x / (frameBuffer.WindowWidth - 1)) * node.Length, true);
                    }
                    else
                    {
                        probs = Utils.Utils.GetBranchStateProbs(run.Histories, run.TreeSamples, run.LikelihoodModels, summaryLikelihoodModel, run.SummaryNodeCorresp, new List<string>(run.AllPossibleStates), summaryLikelihoodModel.BranchLengths.Length - nodeInd - 1, node.Length - (1 - (double)x / (frameBuffer.WindowWidth - 1)) * node.Length, false);
                    }

                    for (int i = 0; i < run.States.Length; i++)
                    {
                        currProp += probs[i];
                        branchPlotProps[x].Add(currProp);
                    }
                }

                for (int y = 0; y < frameBuffer.WindowHeight - startBranchPlot - 2; y++)
                {
                    frameBuffer.CursorTop = startBranchPlot + y;
                    frameBuffer.CursorLeft = 0;

                    for (int x = 0; x < frameBuffer.WindowWidth; x++)
                    {
                        double prop = (double)y / (frameBuffer.WindowHeight - startBranchPlot - 2);
                        int currInd = 0;

                        while (currInd < branchPlotProps[x].Count && branchPlotProps[x][currInd] <= prop)
                        {
                            currInd++;
                        }
                        frameBuffer.ForegroundColor = stateColors[currInd % stateColors.Length];
                        frameBuffer.Write("█");
                    }
                }

                int currPos = (int)Math.Round((1 - position / node.Length) * (frameBuffer.WindowWidth - 1));

                frameBuffer.CursorTop = frameBuffer.WindowHeight - 2;

                frameBuffer.CursorLeft = currPos;
                frameBuffer.ForegroundColor = ConsoleColor.Gray;
                frameBuffer.Write("↑");

                frameBuffer.CursorTop = frameBuffer.WindowHeight - 2;

                string posStr = position.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture);

                if (frameBuffer.WindowWidth - currPos - 2 > posStr.Length)
                {
                    frameBuffer.Write(" " + posStr);
                }
                else
                {
                    frameBuffer.CursorLeft = currPos - posStr.Length - 1;
                    frameBuffer.Write(posStr);
                }
            }

            mainFrameBuffer.Update(frameBuffer);
        }
    }
}
