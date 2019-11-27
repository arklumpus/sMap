using Mono.Options;
using SlimTreeNode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace PlotSmap
{
    class Program
    {
        const string version = "1.0.0";

        static ConsoleFrameBuffer mainFrameBuffer;
        static SerializedRun run = null;

        static PlotTargets plotTarget = PlotTargets.StochasticMap;
        static float plotWidth = 500;
        static float plotHeight = 100;
        static float margins = 10;
        static float fontSize = 12;
        static float pieSize = 5;
        static float branchWidth = 3;
        static float lineWidth = 1;
        static string fontFamily = "OpenSans-Regular.ttf";
        static bool nodeNumbers = false;
        static bool scaleAxis = false;
        static float scaleSpacing = 0.2F;
        static bool scaleGrid = false;
        static float gridSpacing = 0.1F;
        static float gridLineWidth = 0.5F;
        static byte[] gridColour = new byte[] { 200, 200, 200 };
        static float treeAgeScale = 1;
        static int significantDigits = 3;
        static float branchTimeResolution = 2;

        static bool[] marginalActiveCharacters;
        static byte[][] stateColours;


        static string[] activeStateNames;


        static int Main(string[] args)
        {
            bool showHelp = false;
            bool showUsage = false;
            string inputFileName = null;

            OptionSet argParser = new OptionSet()
            {
                { "s|smap=", "Input file produced by sMap run", v => { inputFileName = v; } }
            };

            argParser.Parse(args);

            if (!string.IsNullOrEmpty(inputFileName))
            {

                try
                {
                    run = SerializedRun.Deserialize(inputFileName);
                }
                catch (Exception e)
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("Error while parsing input file:");
                    ConsoleWrapper.WriteLine(e.Message);
                    showUsage = true;
                }

                run.States = run.AllPossibleStates;

                outputFileName = (inputFileName.Contains(".bin") ? inputFileName.Substring(0, inputFileName.IndexOf(".bin")) + ".pdf" : inputFileName + ".pdf");

                settingsFileName = (inputFileName.Contains(".bin") ? inputFileName.Substring(0, inputFileName.IndexOf(".bin")) + ".plot" : inputFileName + ".plot");

                marginalActiveCharacters = new bool[run.States[0].Split(',').Length];
                for (int i = 0; i < marginalActiveCharacters.Length; i++)
                {
                    marginalActiveCharacters[i] = true;
                }

                treeAgeScale = (float)run.AgeScale;

                TreeNode summaryTree = run.SummaryTree;

                scaleSpacing = (float)(summaryTree.DownstreamLength() * 0.2);
                gridSpacing = scaleSpacing / 2;

                stateColours = new byte[run.States.Length][];
                activeStateNames = new string[run.States.Length];

                for (int i = 0; i < run.States.Length; i++)
                {
                    (int r, int g, int b, double a) col = Utils.Plotting.GetColor(i, 1, run.States.Length);
                    stateColours[i] = new byte[] { (byte)col.r, (byte)col.g, (byte)col.b };
                    activeStateNames[i] = run.States[i];
                }

                int leafCount = summaryTree.GetLeaves().Count;

                plotHeight = leafCount * 16.8F + 20;
            }

            bool runInteractive = true;
            bool forceInteractive = false;

            argParser = new OptionSet()
            {
                { "h|help", "Print this message and exit.", v => { showHelp = v != null; } },
                { "s|smap=", "Input file produced by sMap run.", v => { } },
                { "b|batch:", "Optional. Draw plot and exit. If an optional path to a plot settings file is provided, settings for the plot will be loaded from that file.", v => {
                    runInteractive = false;
                    if (!string.IsNullOrEmpty(v))
                    {
                        List<string> unrecognisedParsed = argParser.Parse(File.ReadAllLines(v));

                        if (unrecognisedParsed.Count > 0)
                        {
                            ConsoleWrapper.WriteLine();
                            ConsoleWrapper.WriteLine("Unrecognised setting" + (unrecognisedParsed.Count > 1 ? "s" : "") + ": " + Utils.Utils.StringifyArray(unrecognisedParsed, " "));
                            showUsage = true;
                        }
                    }
                } },
                { "i|interactive", "Optional. Force an interactive session even when a plot settings file is specified.", v => { forceInteractive = v != null; } },
                { "o|output=", "Optional. Output file name. Default: determined by input file name.", v => { outputFileName = v; } },
                { "t|target=", "Optional. Plot target. Available targets are: Tree, MeanPriors, MeanPosteriors, MeanCondPosteriors, StochasticMap, SampleSizes. Default: StochasticMap.", v => {
                    switch (v.ToLower())
                    {
                        case "tree":
                            plotTarget = PlotTargets.Tree;
                            break;
                        case "meanpriors":
                            plotTarget = PlotTargets.MeanPriors;
                            break;
                        case "meanposteriors":
                            plotTarget = PlotTargets.MeanPosteriors;
                            break;
                        case "meancondposteriors":
                            plotTarget = PlotTargets.MeanCondPosteriors;
                            break;
                        case "stochasticmap":
                            plotTarget = PlotTargets.StochasticMap;
                            break;
                        case "samplesizes":
                            plotTarget = PlotTargets.SampleSizes;
                            break;
                    }
                } },
                { "w|width=", "Optional. Plot width in points. Default: 500.", v => { plotWidth = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "H|height=", "Optional. Plot height in points. Default: determined by the number of taxa.", v => { plotHeight = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "m|margins=", "Optional. Plot margins in points. Default: 10.", v => { margins = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "f|font-size=", "Optional. Font size in points. Default: 12.", v => { fontSize = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "F|font-family=", "Optional. Font family. This should be a standard font family, or the path to a TrueText font file (this path can be absolute, relative to the location of this executable, or relative to the current working directory. Available standard font families are: Helvetica, Helvetica-Bold, Helvetica-Oblique, Helvetica-BoldOblique, Courier, Courier-Bold, Courier-Oblique, Courier-BoldOblique, Times-Roman, Times-Bold, Times-Italic, Times-BoldItalic, OpenSans-Regular.ttf, OpenSans-Bold.ttf, OpenSans-Italic.ttf, OpenSans-BoldItalic.ttf. Default: OpenSans-Regular.ttf.", v => { fontFamily = v; } },
                { "p|pie-size=", "Optional. Plot pie radius in points. Default: 5.", v => { pieSize = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "B|branch-width=", "Optional. Branch width in points (only for stochastic map plots). Default: 3.", v => { branchWidth = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "l|line-width=", "Optional. Plot line width in points. Default: 1.", v => { lineWidth = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "n|node-ids", "Optional. Show node ids in the plot. Default: off.", v => { nodeNumbers = v != null; } },
                { "x|scale-axis", "Optional. Show scale axis at the bottom of the plot. Default: off.", v => { scaleAxis = v != null; } },
                { "S|scale-spacing=", "Optional. Scale axis spacing. Default: tree_height / 5.", v => { scaleSpacing = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "g|scale-grid", "Optional. Show scale grid behind the plot. Only works when the scale axis is also enabled. Default: off.", v => { scaleGrid = v != null; } },
                { "G|grid-spacing=", "Optional. Scale grid spacing. Default: tree_height / 10.", v => { gridSpacing = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "gw|grid-line-width=", "Optional. Scale grid line width. Default: 0.5.", v => { gridLineWidth = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "gc|grid-colour=", "Optional. Scale grid line colour, in r,g,b format. Default: 200,200,200.", v => { gridColour = (from el in v.Split(',') select byte.Parse(el, System.Globalization.CultureInfo.InvariantCulture)).ToArray(); } },
                { "d|digits=", "Optional. Significant digits on the scale axis. Default: 3.", v => { significantDigits = int.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "a|age-scale=", "Optional. Tree age scale multiplier. Default: same value used to normalise the tree heights in sMap.", v => { treeAgeScale = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "r|resolution=", "Optional. Branch time resolution. Default: plot_width / 250.", v => { branchTimeResolution = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); } },
                { "c|active-characters=", "Optional. Comma-separated list of active characters (e.g. 0,1). Default: all.", v => {
                    if (v.ToLower() == "all")
                    {
                        marginalActiveCharacters = new bool[run.States[0].Split(',').Length];
                        for (int i = 0; i < marginalActiveCharacters.Length; i++)
                        {
                            marginalActiveCharacters[i] = true;
                        }

                        stateColours = new byte[run.States.Length][];
                        activeStateNames = new string[run.States.Length];

                        for (int i = 0; i < run.States.Length; i++)
                        {
                            (int r, int g, int b, double a) col = Utils.Plotting.GetColor(i, 1, run.States.Length);
                            stateColours[i] = new byte[] { (byte)col.r, (byte)col.g, (byte)col.b };
                            activeStateNames[i] = run.States[i];
                        }
                    }
                    else
                    {
                        int[] activeChars = (from el in v.Split(',') select int.Parse(el, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

                        marginalActiveCharacters = new bool[run.States[0].Split(',').Length];

                        for (int i = 0; i < activeChars.Length; i++)
                        {
                            marginalActiveCharacters[activeChars[i]] = true;
                        }

                        List<string[]> activeStates = new List<string[]>();

                        for (int i = 0; i < marginalActiveCharacters.Length; i++)
                        {
                            if (marginalActiveCharacters[i])
                            {
                                activeStates.Add(new HashSet<string>(from el in run.States select el.Split(',')[i]).ToArray());
                            }
                        }

                        activeStateNames = (from el in Utils.Utils.GetCombinations(activeStates.ToArray()) select Utils.Utils.StringifyArray(el)).ToArray();

                        stateColours = new byte[activeStateNames.Length][];

                        for (int i = 0; i < activeStateNames.Length; i++)
                        {
                            (int r, int g, int b, double a) col = Plotting.GetColor(i, 1, activeStateNames.Length);
                            stateColours[i] = new byte[] { (byte)col.r, (byte)col.g, (byte)col.b };
                        }
                    }
                } },
                { "C|state-colours=", "Optional. Colon-separated list of colours in [r,g,b] format (e.g. [255,0,0]:[0,255,255]). Default: auto (determined by the number of states).", v => {
                    if (v.ToLower() == "auto")
                    {
                        stateColours = new byte[activeStateNames.Length][];

                        for (int i = 0; i < activeStateNames.Length; i++)
                        {
                            (int r, int g, int b, double a) col = Plotting.GetColor(i, 1, activeStateNames.Length);
                            stateColours[i] = new byte[] { (byte)col.r, (byte)col.g, (byte)col.b };
                        }
                    }
                    else
                    {
                        string[] colours = v.Split(':');
                        stateColours = new byte[colours.Length][];

                        for (int i = 0; i < colours.Length; i++)
                        {
                            string col = colours[i].Substring(colours[i].IndexOf("[") + 1);
                            col = col.Substring(0, col.IndexOf("]"));
                            stateColours[i] = (from el in col.Split(',') select byte.Parse(el, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                        }
                    }
                } }
            };

            List<string> unrecognised = argParser.Parse(args);

            if (unrecognised.Count > 0)
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Unrecognised argument" + (unrecognised.Count > 1 ? "s" : "") + ": " + Utils.Utils.StringifyArray(unrecognised, " "));
                showUsage = true;
            }

            if (!showHelp)
            {
                if (string.IsNullOrEmpty(inputFileName))
                {
                    ConsoleWrapper.WriteLine();
                    ConsoleWrapper.WriteLine("No input file specified!");
                    showUsage = true;
                }
            }


            if (showUsage || showHelp)
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Plot-sMap version {0}", version);
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("Usage:");
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.WriteLine("  Plot-sMap {-h|--help}");
                ConsoleWrapper.WriteLine("  Plot-sMap -s <sMap_file> [options...]");
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

            if (!runInteractive && !forceInteractive)
            {
                Plot();
                return 0;
            }

            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CursorVisible = false;

            mainFrameBuffer = new ConsoleFrameBuffer();

            mainFrameBuffer.Flush();

            DisplayInterface();

            bool keepRunning = true;

            while (keepRunning)
            {
                ConsoleKeyInfo ki = Console.ReadKey(true);

                switch (ki.Key)
                {
                    case ConsoleKey.DownArrow:
                        if (!activeEditing)
                        {
                            if ((int)editingField < maxFirstCol || ((int)editingField >= startSecondCol && (int)editingField < maxField))
                            {
                                editingField = (EditableFields)((int)editingField + 1);
                            }
                            else if ((int)editingField == maxFirstCol)
                            {
                                editingField = EditableFields.PlotButton;
                            }
                            else if ((int)editingField == maxField)
                            {
                                editingField = EditableFields.ExitButton;
                            }
                        }
                        break;
                    case ConsoleKey.UpArrow:
                        if (!activeEditing)
                        {
                            if (((int)editingField <= maxFirstCol && (int)editingField > 0) || ((int)editingField > startSecondCol && (int)editingField <= maxField))
                            {
                                editingField = (EditableFields)((int)editingField - 1);
                            }
                            else if (editingField == EditableFields.PlotButton || editingField == EditableFields.SaveButton)
                            {
                                editingField = (EditableFields)maxFirstCol;
                            }
                            else if (editingField == EditableFields.ExitButton)
                            {
                                editingField = (EditableFields)maxField;
                            }
                        }
                        break;
                    case ConsoleKey.RightArrow:
                        if (!activeEditing)
                        {
                            if ((int)editingField <= maxFirstCol)
                            {
                                editingField = (EditableFields)(Math.Min(maxField, (int)editingField + startSecondCol));
                            }
                            else if (editingField == EditableFields.PlotButton || editingField == EditableFields.SaveButton)
                            {
                                editingField = (EditableFields)((int)editingField + 1);
                            }
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        if (!activeEditing)
                        {
                            if ((int)editingField >= startSecondCol)
                            {
                                editingField = (EditableFields)((int)editingField - startSecondCol);
                            }
                            if (editingField == EditableFields.ExitButton || editingField == EditableFields.SaveButton)
                            {
                                editingField = (EditableFields)((int)editingField - 1);
                            }
                        }
                        break;
                    case ConsoleKey.Enter:
                        switch (editingField)
                        {
                            case EditableFields.ExitButton:
                                keepRunning = false;
                                break;
                            case EditableFields.PlotButton:
                                PlotDialog();
                                break;
                            case EditableFields.SaveButton:
                                SaveDialog();
                                break;
                            case EditableFields.NodeNumbers:
                                nodeNumbers = !nodeNumbers;
                                break;
                            case EditableFields.ScaleAxis:
                                scaleAxis = !scaleAxis;
                                break;
                            case EditableFields.ScaleGrid:
                                scaleGrid = !scaleGrid;
                                break;
                            case EditableFields.FontFamily:
                                editingFontFamily = fontFamily;
                                FontChoiceDialog();
                                break;
                            case EditableFields.PlotTarget:
                                editingPlotTarget = plotTargets[(int)plotTarget];
                                editingActiveCharacters = (bool[])marginalActiveCharacters.Clone();
                                editingActiveCharIndex = -1;
                                TargetChoiceDialog();

                                {
                                    List<string[]> activeStates = new List<string[]>();

                                    for (int i = 0; i < marginalActiveCharacters.Length; i++)
                                    {
                                        if (marginalActiveCharacters[i])
                                        {
                                            activeStates.Add(new HashSet<string>(from el in run.States select el.Split(',')[i]).ToArray());
                                        }
                                    }

                                    activeStateNames = (from el in Utils.Utils.GetCombinations(activeStates.ToArray()) select Utils.Utils.StringifyArray(el)).ToArray();

                                    stateColours = new byte[activeStateNames.Length][];

                                    for (int i = 0; i < activeStateNames.Length; i++)
                                    {
                                        (int r, int g, int b, double a) col = Plotting.GetColor(i, 1, activeStateNames.Length);
                                        stateColours[i] = new byte[] { (byte)col.r, (byte)col.g, (byte)col.b };
                                    }
                                }

                                break;
                            case EditableFields.GridColour:
                                editingColour = (byte[])gridColour.Clone();
                                if (ColourChoiceDialog())
                                {
                                    gridColour = (byte[])editingColour.Clone();
                                }
                                break;
                            case EditableFields.StateColours:
                                editingMultiColours = stateColours.DeepClone();
                                editingColourIndex = 0;
                                if (MultiColourChoiceDialog())
                                {
                                    stateColours = editingMultiColours.DeepClone();
                                }
                                break;
                        }

                        if (textboxFields.Contains(editingField))
                        {
                            if (!activeEditing)
                            {
                                activeEditing = true;
                                currentEditingString = getValueString(editingField);
                            }
                            else
                            {
                                activeEditing = false;
                                if (!string.IsNullOrEmpty(currentEditingString))
                                {
                                    if (float.TryParse(currentEditingString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float parsed))
                                    {
                                        setValueString(editingField, parsed);
                                    }
                                }
                            }
                        }


                        break;
                    case ConsoleKey.Spacebar:
                        switch (editingField)
                        {
                            case EditableFields.ExitButton:
                                keepRunning = false;
                                break;
                            case EditableFields.PlotButton:
                                PlotDialog();
                                break;
                            case EditableFields.SaveButton:
                                SaveDialog();
                                break;
                            case EditableFields.NodeNumbers:
                                nodeNumbers = !nodeNumbers;
                                break;
                            case EditableFields.ScaleAxis:
                                scaleAxis = !scaleAxis;
                                break;
                            case EditableFields.ScaleGrid:
                                scaleGrid = !scaleGrid;
                                break;
                        }
                        break;

                    case ConsoleKey.Backspace:
                        if (activeEditing && currentEditingString.Length > 0)
                        {
                            currentEditingString = currentEditingString.Substring(0, currentEditingString.Length - 1);
                        }
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
                    case ConsoleKey.Decimal:
                    case ConsoleKey.OemPeriod:
                        if (activeEditing)
                        {
                            currentEditingString += ki.KeyChar;
                        }
                        else
                        {
                            if (textboxFields.Contains(editingField))
                            {
                                activeEditing = true;
                                currentEditingString = ki.KeyChar.ToString();
                            }
                        }
                        break;
                }

                DisplayInterface();
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                }
            }

            Console.Clear();

            Console.CursorVisible = true;

            return 0;
        }

        enum EditableFields { PlotTarget = 0, Width = 1, Height = 2, Margins = 3, FontSize = 4, FontFamily = 5, PieSize = 6, BranchWidth = 7, LineWidth = 8, BranchTimeResolution = 9, PlotButton = 10, SaveButton = 11, ExitButton = 12, NodeNumbers = 13, StateColours = 14, ScaleAxis = 15, ScaleSpacing = 16, ScaleDigits = 17, ScaleGrid = 18, GridSpacing = 19, GridColour = 20, GridWidth = 21, TreeAgeScale = 22 }
        static EditableFields editingField = EditableFields.PlotTarget;
        const int maxFirstCol = 9;
        const int startSecondCol = 13;
        const int maxField = 22;
        static readonly EditableFields[] textboxFields = new EditableFields[] { EditableFields.BranchTimeResolution, EditableFields.BranchWidth, EditableFields.FontSize, EditableFields.GridSpacing, EditableFields.GridWidth, EditableFields.Height, EditableFields.LineWidth, EditableFields.Margins, EditableFields.PieSize, EditableFields.ScaleSpacing, EditableFields.Width, EditableFields.TreeAgeScale, EditableFields.ScaleDigits };

        static string getValueString(EditableFields field)
        {
            switch (field)
            {
                case EditableFields.BranchTimeResolution:
                    return branchTimeResolution.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

                case EditableFields.BranchWidth:
                    return branchWidth.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

                case EditableFields.FontSize:
                    return fontSize.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

                case EditableFields.GridSpacing:
                    return gridSpacing.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

                case EditableFields.GridWidth:
                    return gridLineWidth.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

                case EditableFields.Height:
                    return plotHeight.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

                case EditableFields.LineWidth:
                    return lineWidth.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

                case EditableFields.Margins:
                    return margins.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

                case EditableFields.PieSize:
                    return pieSize.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

                case EditableFields.ScaleSpacing:
                    return scaleSpacing.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

                case EditableFields.Width:
                    return plotWidth.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

                case EditableFields.TreeAgeScale:
                    return treeAgeScale.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

                case EditableFields.ScaleDigits:
                    return significantDigits.ToString();
            }

            return null;
        }

        static void setValueString(EditableFields field, float value)
        {
            switch (field)
            {
                case EditableFields.BranchTimeResolution:
                    branchTimeResolution = value;
                    break;

                case EditableFields.BranchWidth:
                    branchWidth = value;
                    break;

                case EditableFields.FontSize:
                    fontSize = value;
                    break;

                case EditableFields.GridSpacing:
                    gridSpacing = value;
                    break;

                case EditableFields.GridWidth:
                    gridLineWidth = value;
                    break;

                case EditableFields.Height:
                    plotHeight = value;
                    break;

                case EditableFields.LineWidth:
                    lineWidth = value;
                    break;

                case EditableFields.Margins:
                    margins = value;
                    break;

                case EditableFields.PieSize:
                    pieSize = value;
                    break;

                case EditableFields.ScaleSpacing:
                    scaleSpacing = value;
                    break;

                case EditableFields.Width:
                    plotWidth = value;
                    break;

                case EditableFields.TreeAgeScale:
                    treeAgeScale = value;
                    break;

                case EditableFields.ScaleDigits:
                    significantDigits = (int)Math.Round(value);
                    break;
            }

        }

        static readonly string[] plotTargets = new string[] { "Tree", "Mean posteriors", "Mean priors", "Mean cond. posteriors", "Stochastic map", "Sample sizes" };
        static readonly int maxPlotTargetLen = (from el in plotTargets select el.Length).Max();
        enum PlotTargets { Tree = 0, MeanPosteriors = 1, MeanPriors = 2, MeanCondPosteriors = 3, StochasticMap = 4, SampleSizes = 5 }

        static string currentEditingString = "";
        static bool activeEditing = false;

        static void DrawInterface(ConsoleFrameBuffer frameBuffer)
        {
            frameBuffer.CursorLeft = 0;
            frameBuffer.CursorTop = 0;

            frameBuffer.Write("╔" + " sMap plot options ".Pad(frameBuffer.WindowWidth - 2, Extensions.PadType.Center, '═') + "╗");
            for (int y = 1; y < frameBuffer.WindowHeight - 1; y++)
            {
                frameBuffer.CursorLeft = 0;
                frameBuffer.CursorTop = y;
                frameBuffer.Write("║");
                frameBuffer.CursorLeft = frameBuffer.WindowWidth - 1;
                frameBuffer.Write("║");
            }

            frameBuffer.CursorLeft = 0;
            frameBuffer.CursorTop = frameBuffer.WindowHeight - 1;
            frameBuffer.Write("╚" + new string('═', frameBuffer.WindowWidth - 2) + "╝");

            int padding = (frameBuffer.WindowWidth - 4 - (maxPlotTargetLen + 19) * 2) / 3;

            PlotInterfaceItem(frameBuffer, "    Plot target", plotTargets[(int)plotTarget].Pad(maxPlotTargetLen, Extensions.PadType.Center), InterfaceItemType.DropDown, editingField == EditableFields.PlotTarget, 2, 2 + padding);

            PlotInterfaceItem(frameBuffer, "     Plot width", plotWidth.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture).Pad(maxPlotTargetLen + 2, Extensions.PadType.Center), InterfaceItemType.TextBox, editingField == EditableFields.Width, frameBuffer.CursorTop + 2, 2 + padding);

            PlotInterfaceItem(frameBuffer, "    Plot height", plotHeight.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture).Pad(maxPlotTargetLen + 2, Extensions.PadType.Center), InterfaceItemType.TextBox, editingField == EditableFields.Height, frameBuffer.CursorTop + 2, 2 + padding);

            PlotInterfaceItem(frameBuffer, "        Margins", margins.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture).Pad(maxPlotTargetLen + 2, Extensions.PadType.Center), InterfaceItemType.TextBox, editingField == EditableFields.Margins, frameBuffer.CursorTop + 2, 2 + padding);

            PlotInterfaceItem(frameBuffer, "      Font size", fontSize.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture).Pad(maxPlotTargetLen + 2, Extensions.PadType.Center), InterfaceItemType.TextBox, editingField == EditableFields.FontSize, frameBuffer.CursorTop + 2, 2 + padding);

            string fontF = fontFamily.Pad(maxPlotTargetLen, Extensions.PadType.Center);
            if (!fontF.Contains(fontFamily))
            {
                fontF = fontF.Substring(0, fontF.Length - 1) + "…";
            }

            PlotInterfaceItem(frameBuffer, "    Font family", fontF, InterfaceItemType.DropDown, editingField == EditableFields.FontFamily, frameBuffer.CursorTop + 2, 2 + padding);

            PlotInterfaceItem(frameBuffer, "       Pie size", pieSize.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture).Pad(maxPlotTargetLen + 2, Extensions.PadType.Center), InterfaceItemType.TextBox, editingField == EditableFields.PieSize, frameBuffer.CursorTop + 2, 2 + padding);

            PlotInterfaceItem(frameBuffer, "   Branch width", branchWidth.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture).Pad(maxPlotTargetLen + 2, Extensions.PadType.Center), InterfaceItemType.TextBox, editingField == EditableFields.BranchWidth, frameBuffer.CursorTop + 2, 2 + padding);

            PlotInterfaceItem(frameBuffer, "     Line width", lineWidth.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture).Pad(maxPlotTargetLen + 2, Extensions.PadType.Center), InterfaceItemType.TextBox, editingField == EditableFields.LineWidth, frameBuffer.CursorTop + 2, 2 + padding);

            PlotInterfaceItem(frameBuffer, "Time resolution", branchTimeResolution.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture).Pad(maxPlotTargetLen + 2, Extensions.PadType.Center), InterfaceItemType.TextBox, editingField == EditableFields.BranchTimeResolution, frameBuffer.CursorTop + 2, 2 + padding);

            PlotInterfaceItem(frameBuffer, "       Node ids", nodeNumbers ? "true" : null, InterfaceItemType.CheckBox, editingField == EditableFields.NodeNumbers, 2, 2 + padding * 2 + maxPlotTargetLen + 17);

            PlotInterfaceItem(frameBuffer, "  State colours", null, InterfaceItemType.MultiColourBox, editingField == EditableFields.StateColours, frameBuffer.CursorTop + 2, 2 + padding * 2 + maxPlotTargetLen + 17, maxPlotTargetLen + 2, null, stateColours);

            PlotInterfaceItem(frameBuffer, "     Scale axis", scaleAxis ? "true" : null, InterfaceItemType.CheckBox, editingField == EditableFields.ScaleAxis, frameBuffer.CursorTop + 2, 2 + padding * 2 + maxPlotTargetLen + 17);

            PlotInterfaceItem(frameBuffer, "  Scale spacing", scaleSpacing.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture).Pad(maxPlotTargetLen + 2, Extensions.PadType.Center), InterfaceItemType.TextBox, editingField == EditableFields.ScaleSpacing, frameBuffer.CursorTop + 2, 2 + padding * 2 + maxPlotTargetLen + 17);

            PlotInterfaceItem(frameBuffer, "   Scale digits", significantDigits.ToString().Pad(maxPlotTargetLen + 2, Extensions.PadType.Center), InterfaceItemType.TextBox, editingField == EditableFields.ScaleDigits, frameBuffer.CursorTop + 2, 2 + padding * 2 + maxPlotTargetLen + 17);

            PlotInterfaceItem(frameBuffer, "     Scale grid", scaleGrid ? "true" : null, InterfaceItemType.CheckBox, editingField == EditableFields.ScaleGrid, frameBuffer.CursorTop + 2, 2 + padding * 2 + maxPlotTargetLen + 17);

            PlotInterfaceItem(frameBuffer, "   Grid spacing", gridSpacing.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture).Pad(maxPlotTargetLen + 2, Extensions.PadType.Center), InterfaceItemType.TextBox, editingField == EditableFields.GridSpacing, frameBuffer.CursorTop + 2, 2 + padding * 2 + maxPlotTargetLen + 17);

            PlotInterfaceItem(frameBuffer, "    Grid colour", null, InterfaceItemType.SingleColourBox, editingField == EditableFields.GridColour, frameBuffer.CursorTop + 2, 2 + padding * 2 + maxPlotTargetLen + 17, maxPlotTargetLen + 2, gridColour);

            PlotInterfaceItem(frameBuffer, "     Grid width", gridLineWidth.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture).Pad(maxPlotTargetLen + 2, Extensions.PadType.Center), InterfaceItemType.TextBox, editingField == EditableFields.GridWidth, frameBuffer.CursorTop + 2, 2 + padding * 2 + maxPlotTargetLen + 17);

            PlotInterfaceItem(frameBuffer, " Tree age scale", treeAgeScale.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture).Pad(maxPlotTargetLen + 2, Extensions.PadType.Center), InterfaceItemType.TextBox, editingField == EditableFields.TreeAgeScale, frameBuffer.CursorTop + 2, 2 + padding * 2 + maxPlotTargetLen + 17);

            frameBuffer.CursorTop += 2;

            frameBuffer.CursorTop++;

            int buttonTop = frameBuffer.CursorTop + ((frameBuffer.WindowHeight - frameBuffer.CursorTop - 3) / 2);

            int buttonLeft = 2 + (frameBuffer.WindowWidth - 4 - 54) / 4;

            PlotButton(frameBuffer, "     Plot…    ", editingField == EditableFields.PlotButton, buttonTop, buttonLeft);

            PlotButton(frameBuffer, "Save settings…", editingField == EditableFields.SaveButton, buttonTop, 2 * buttonLeft + 18);

            PlotButton(frameBuffer, "     Exit     ", editingField == EditableFields.ExitButton, buttonTop, 3 * buttonLeft + 36);
        }

        static void DisplayInterface()
        {
            ConsoleFrameBuffer frameBuffer = new ConsoleFrameBuffer();

            DrawInterface(frameBuffer);

            mainFrameBuffer.Update(frameBuffer);
        }

        enum InterfaceItemType { DropDown, TextBox, CheckBox, SingleColourBox, MultiColourBox }

        static void PlotInterfaceItem(ConsoleFrameBuffer frameBuffer, string itemName, string itemValue, InterfaceItemType itemType, bool active, int top, int left, int valueLength = -1, byte[] colourValue = null, byte[][] multipleColourValue = null)
        {

            frameBuffer.CursorTop = top;
            frameBuffer.CursorLeft = left;

            frameBuffer.BackgroundColor = ConsoleColor.Black;


            if (active)
            {
                frameBuffer.ForegroundColor = ConsoleColor.Cyan;
            }
            else
            {
                frameBuffer.ForegroundColor = ConsoleColor.Blue;
            }

            frameBuffer.Write(itemName + ": ");

            if (active)
            {
                frameBuffer.ForegroundColor = ConsoleColor.Black;
                frameBuffer.BackgroundColor = ConsoleColor.Gray;
            }
            else
            {
                frameBuffer.ForegroundColor = ConsoleColor.Gray;
            }

            if (itemType == InterfaceItemType.CheckBox)
            {
                if (string.IsNullOrEmpty(itemValue))
                {
                    frameBuffer.Write("□");
                }
                else
                {
                    frameBuffer.Write("√");
                }
            }
            else if (itemType == InterfaceItemType.TextBox)
            {
                if (!active || !activeEditing)
                {
                    frameBuffer.Write(itemValue + " ");
                }
                else
                {
                    frameBuffer.Write((currentEditingString + "■").Pad(itemValue.Length, Extensions.PadType.Center) + " ");
                }
            }
            else if (itemType == InterfaceItemType.SingleColourBox)
            {
                frameBuffer.Write("   ");
                frameBuffer.PlotColour(colourValue, valueLength - 5);
                frameBuffer.Write("   ");
            }
            else if (itemType == InterfaceItemType.MultiColourBox)
            {

                if (multipleColourValue.Length <= valueLength - 5)
                {
                    int colourLength = (valueLength - 5) / multipleColourValue.Length;
                    int paddingLeft = ((valueLength - 5) - colourLength * multipleColourValue.Length) / 2;
                    int paddingRight = (valueLength - 5) - colourLength * multipleColourValue.Length - paddingLeft;

                    frameBuffer.Write(new string(' ', 3 + paddingLeft));

                    for (int i = 0; i < multipleColourValue.Length; i++)
                    {
                        frameBuffer.PlotColour(multipleColourValue[i], colourLength);
                    }

                    frameBuffer.Write(new string(' ', 3 + paddingRight));
                }
                else
                {
                    frameBuffer.Write("   ");

                    for (int i = 0; i < valueLength - 6; i++)
                    {
                        frameBuffer.PlotColour(multipleColourValue[i], 1);
                    }

                    frameBuffer.Write("…   ");
                }
            }
            else
            {
                frameBuffer.Write(itemValue + " ");
            }

            if (itemType == InterfaceItemType.DropDown)
            {
                frameBuffer.ForegroundColor = ConsoleColor.Green;
                frameBuffer.Write("▼ ");
            }

        }

        static void PlotButton(ConsoleFrameBuffer frameBuffer, string text, bool active, int top, int left)
        {
            frameBuffer.CursorLeft = left;
            frameBuffer.CursorTop = top;

            if (!active)
            {
                frameBuffer.ForegroundColor = ConsoleColor.Gray;
                frameBuffer.BackgroundColor = ConsoleColor.Black;

                frameBuffer.Write("┌" + new string('─', text.Length + 2) + "┐");
                frameBuffer.CursorTop = top + 1;
                frameBuffer.CursorLeft = left;
                frameBuffer.Write("│ " + text + " │");
                frameBuffer.CursorTop = top + 2;
                frameBuffer.CursorLeft = left;
                frameBuffer.Write("└" + new string('─', text.Length + 2) + "┘");
            }
            else
            {
                frameBuffer.ForegroundColor = ConsoleColor.Gray;
                frameBuffer.BackgroundColor = ConsoleColor.Black;

                frameBuffer.Write(new string('▄', text.Length + 4));
                frameBuffer.CursorTop = top + 1;
                frameBuffer.CursorLeft = left;

                frameBuffer.ForegroundColor = ConsoleColor.Black;
                frameBuffer.BackgroundColor = ConsoleColor.Gray;
                frameBuffer.Write("  " + text + "  ");

                frameBuffer.ForegroundColor = ConsoleColor.Gray;
                frameBuffer.BackgroundColor = ConsoleColor.Black;

                frameBuffer.CursorTop = top + 2;
                frameBuffer.CursorLeft = left;
                frameBuffer.Write(new string('▀', text.Length + 4));
            }

        }

        static readonly string[] fonts = new string[] { "Helvetica", "Helvetica-Bold", "Helvetica-Oblique", "Helvetica-BoldOblique", "Courier", "Courier-Bold", "Courier-Oblique", "Courier-BoldOblique", "Times-Roman", "Times-Bold", "Times-Italic", "Times-BoldItalic", "OpenSans-Regular.ttf", "OpenSans-Bold.ttf", "OpenSans-Italic.ttf", "OpenSans-BoldItalic.ttf", "Custom (enter name here)" };

        static string editingFontFamily;

        static void FontChoiceDialog()
        {
            ConsoleFrameBuffer frameBuffer = new ConsoleFrameBuffer();

            DrawInterface(frameBuffer);

            int dialogWidth = 50;

            int dialogTop = (frameBuffer.WindowHeight - 24) / 2;
            int dialogLeft = (frameBuffer.WindowWidth - dialogWidth) / 2;

            frameBuffer.CursorTop = dialogTop;
            frameBuffer.CursorLeft = dialogLeft;

            frameBuffer.ForegroundColor = ConsoleColor.Black;
            frameBuffer.BackgroundColor = ConsoleColor.Gray;

            frameBuffer.Write("╔" + " Choose font family ".Pad(dialogWidth - 2, Extensions.PadType.Center, '═') + "╗");

            for (int y = 1; y < 22; y++)
            {
                frameBuffer.CursorLeft = dialogLeft;
                frameBuffer.CursorTop = dialogTop + y;
                frameBuffer.Write("║" + new string(' ', dialogWidth - 2) + "║");

                frameBuffer.ForegroundColor = ConsoleColor.Gray;
                frameBuffer.BackgroundColor = ConsoleColor.Black;
                frameBuffer.Write("▒▒");
                frameBuffer.ForegroundColor = ConsoleColor.Black;
                frameBuffer.BackgroundColor = ConsoleColor.Gray;
            }

            frameBuffer.CursorLeft = dialogLeft;
            frameBuffer.CursorTop = dialogTop + 22;
            frameBuffer.Write("╚" + new string('═', dialogWidth - 2) + "╝");

            frameBuffer.ForegroundColor = ConsoleColor.Gray;
            frameBuffer.BackgroundColor = ConsoleColor.Black;

            frameBuffer.Write("▒▒");

            frameBuffer.CursorLeft = dialogLeft + 2;
            frameBuffer.CursorTop = dialogTop + 23;
            frameBuffer.Write(new string('▒', dialogWidth));

            frameBuffer.ForegroundColor = ConsoleColor.Black;
            frameBuffer.BackgroundColor = ConsoleColor.Gray;

            frameBuffer.CursorLeft = dialogLeft + 2;
            frameBuffer.CursorTop = dialogTop + 2;

            frameBuffer.Write("Font family: " + editingFontFamily);

            frameBuffer.CursorTop++;

            int editingFontIndex = fonts.IndexOf(editingFontFamily);
            if (editingFontIndex == -1)
            {
                editingFontIndex = fonts.Length - 1;
            }

            for (int i = 0; i < fonts.Length; i++)
            {
                if (editingFontFamily == fonts[i])
                {
                    frameBuffer.CursorLeft = dialogLeft + 13;
                    frameBuffer.CursorTop++;
                    frameBuffer.ForegroundColor = ConsoleColor.Blue;
                    frameBuffer.Write("■ ");
                    frameBuffer.ForegroundColor = ConsoleColor.Black;
                    frameBuffer.Write(fonts[i]);
                }
                else if (i == fonts.Length - 1 && !fonts.Contains(editingFontFamily))
                {
                    frameBuffer.CursorLeft = dialogLeft + 13;
                    frameBuffer.CursorTop++;
                    frameBuffer.ForegroundColor = ConsoleColor.Blue;
                    frameBuffer.Write("■ ");
                    frameBuffer.ForegroundColor = ConsoleColor.Black;
                    frameBuffer.Write(editingFontFamily + "■");
                }
                else
                {
                    frameBuffer.CursorLeft = dialogLeft + 15;
                    frameBuffer.CursorTop++;
                    frameBuffer.Write(fonts[i]);
                }

            }

            mainFrameBuffer.Update(frameBuffer);

            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }

            ConsoleKeyInfo ki = Console.ReadKey(true);

            switch (ki.Key)
            {
                case ConsoleKey.Enter:
                    fontFamily = editingFontFamily;
                    return;
                case ConsoleKey.Escape:
                    return;
                case ConsoleKey.UpArrow:
                    if (editingFontIndex > 0)
                    {
                        editingFontFamily = fonts[editingFontIndex - 1];
                    }
                    FontChoiceDialog();
                    break;
                case ConsoleKey.DownArrow:
                    if (editingFontIndex < fonts.Length - 1)
                    {
                        editingFontFamily = fonts[editingFontIndex + 1];
                        if (editingFontIndex == fonts.Length - 2)
                        {
                            editingFontFamily = "";
                        }
                    }
                    FontChoiceDialog();
                    break;
                case ConsoleKey.Backspace:
                    if (editingFontIndex == fonts.Length - 1 && editingFontFamily.Length > 0)
                    {
                        editingFontFamily = editingFontFamily.Substring(0, editingFontFamily.Length - 1);
                    }
                    FontChoiceDialog();
                    break;
                default:
                    if (editingFontIndex == fonts.Length - 1 && !char.IsControl(ki.KeyChar))
                    {
                        editingFontFamily += ki.KeyChar;
                    }
                    FontChoiceDialog();
                    break;
            }
        }


        static string editingPlotTarget;
        static bool[] editingActiveCharacters;
        static int editingActiveCharIndex = -1;

        static void TargetChoiceDialog()
        {
            ConsoleFrameBuffer frameBuffer = new ConsoleFrameBuffer();

            DrawInterface(frameBuffer);

            int charRows = (int)Math.Sqrt(editingActiveCharacters.Length);
            int charsPerRow = (int)(Math.Ceiling((double)editingActiveCharacters.Length / charRows));

            int dialogWidth = Math.Max(40, charsPerRow * 6 + 8);
            int dialogHeight = 8 + plotTargets.Length + charRows;

            int dialogTop = (frameBuffer.WindowHeight - dialogHeight - 1) / 2;
            int dialogLeft = (frameBuffer.WindowWidth - dialogWidth) / 2;

            frameBuffer.CursorTop = dialogTop;
            frameBuffer.CursorLeft = dialogLeft;

            frameBuffer.ForegroundColor = ConsoleColor.Black;
            frameBuffer.BackgroundColor = ConsoleColor.Gray;

            frameBuffer.Write("╔" + " Choose plot target ".Pad(dialogWidth - 2, Extensions.PadType.Center, '═') + "╗");

            for (int y = 1; y < dialogHeight - 1; y++)
            {
                frameBuffer.CursorLeft = dialogLeft;
                frameBuffer.CursorTop = dialogTop + y;
                frameBuffer.Write("║" + new string(' ', dialogWidth - 2) + "║");

                frameBuffer.ForegroundColor = ConsoleColor.Gray;
                frameBuffer.BackgroundColor = ConsoleColor.Black;
                frameBuffer.Write("▒▒");
                frameBuffer.ForegroundColor = ConsoleColor.Black;
                frameBuffer.BackgroundColor = ConsoleColor.Gray;
            }

            frameBuffer.CursorLeft = dialogLeft;
            frameBuffer.CursorTop = dialogTop + dialogHeight - 1;
            frameBuffer.Write("╚" + new string('═', dialogWidth - 2) + "╝");

            frameBuffer.ForegroundColor = ConsoleColor.Gray;
            frameBuffer.BackgroundColor = ConsoleColor.Black;

            frameBuffer.Write("▒▒");

            frameBuffer.CursorLeft = dialogLeft + 2;
            frameBuffer.CursorTop = dialogTop + dialogHeight;
            frameBuffer.Write(new string('▒', dialogWidth));

            frameBuffer.ForegroundColor = ConsoleColor.Black;
            frameBuffer.BackgroundColor = ConsoleColor.Gray;

            frameBuffer.CursorLeft = dialogLeft + 2;
            frameBuffer.CursorTop = dialogTop + 2;

            frameBuffer.Write("Plot target: " + editingPlotTarget);

            frameBuffer.CursorTop++;

            int editingTargetIndex = plotTargets.IndexOf(editingPlotTarget);

            for (int i = 0; i < plotTargets.Length; i++)
            {
                if (editingPlotTarget == plotTargets[i] && editingActiveCharIndex == -1)
                {
                    frameBuffer.CursorLeft = dialogLeft + 13;
                    frameBuffer.CursorTop++;
                    frameBuffer.ForegroundColor = ConsoleColor.Blue;
                    frameBuffer.Write("■ ");
                    frameBuffer.ForegroundColor = ConsoleColor.Black;
                    frameBuffer.Write(plotTargets[i]);
                }
                else
                {
                    frameBuffer.CursorLeft = dialogLeft + 15;
                    frameBuffer.CursorTop++;
                    frameBuffer.Write(plotTargets[i]);
                }

            }

            frameBuffer.CursorTop += 2;
            frameBuffer.CursorLeft = dialogLeft + 2;
            frameBuffer.Write("Active characters:");

            int padding = 1 + (dialogWidth - charsPerRow * 6) / 2;

            frameBuffer.CursorLeft = dialogLeft + padding;
            frameBuffer.CursorTop++;

            for (int i = 0; i < editingActiveCharacters.Length; i++)
            {
                if (editingActiveCharIndex == i)
                {
                    frameBuffer.ForegroundColor = ConsoleColor.Blue;
                }
                else
                {
                    frameBuffer.ForegroundColor = ConsoleColor.Black;
                }

                if (!editingActiveCharacters[i])
                {
                    frameBuffer.Write("□");
                }
                else
                {
                    frameBuffer.Write("√");
                }

                frameBuffer.Write(" " + i.ToString().Pad(3, Extensions.PadType.Left));

                if ((i + 1) % charsPerRow == 0)
                {
                    frameBuffer.CursorLeft = dialogLeft + padding;
                    frameBuffer.CursorTop++;
                }
                else
                {
                    frameBuffer.CursorLeft += 1;
                }
            }

            mainFrameBuffer.Update(frameBuffer);

            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }

            ConsoleKeyInfo ki = Console.ReadKey(true);

            switch (ki.Key)
            {
                case ConsoleKey.Enter:
                    if (editingActiveCharIndex == -1)
                    {
                        plotTarget = (PlotTargets)(plotTargets.IndexOf(editingPlotTarget));
                        marginalActiveCharacters = (bool[])editingActiveCharacters.Clone();
                        return;
                    }
                    else
                    {
                        if (!editingActiveCharacters[editingActiveCharIndex] || (from el in editingActiveCharacters where el == true select el).Count() > 1)
                        {
                            editingActiveCharacters[editingActiveCharIndex] = !editingActiveCharacters[editingActiveCharIndex];
                        }
                        TargetChoiceDialog();
                    }
                    break;
                case ConsoleKey.Spacebar:
                    if (editingActiveCharIndex >= 0)
                    {
                        if (!editingActiveCharacters[editingActiveCharIndex] || (from el in editingActiveCharacters where el == true select el).Count() > 1)
                        {
                            editingActiveCharacters[editingActiveCharIndex] = !editingActiveCharacters[editingActiveCharIndex];
                        }
                    }
                    TargetChoiceDialog();
                    break;
                case ConsoleKey.Escape:
                    return;
                case ConsoleKey.UpArrow:
                    if (editingActiveCharIndex == -1)
                    {
                        if (plotTargets.IndexOf(editingPlotTarget) > 0)
                        {
                            editingPlotTarget = plotTargets[plotTargets.IndexOf(editingPlotTarget) - 1];
                        }
                    }
                    else
                    {
                        if (editingActiveCharIndex >= charsPerRow)
                        {
                            editingActiveCharIndex -= charsPerRow;
                        }
                        else
                        {
                            editingActiveCharIndex = -1;
                            editingPlotTarget = plotTargets[plotTargets.Length - 1];
                        }
                    }
                    TargetChoiceDialog();
                    break;
                case ConsoleKey.DownArrow:
                    if (editingActiveCharIndex == -1)
                    {
                        if (plotTargets.IndexOf(editingPlotTarget) < plotTargets.Length - 1)
                        {
                            editingPlotTarget = plotTargets[plotTargets.IndexOf(editingPlotTarget) + 1];
                        }
                        else
                        {

                            editingActiveCharIndex = 0;
                        }
                    }
                    else if (editingActiveCharIndex + charsPerRow < editingActiveCharacters.Length)
                    {
                        editingActiveCharIndex += charsPerRow;
                    }
                    TargetChoiceDialog();
                    break;
                case ConsoleKey.RightArrow:
                    if (editingActiveCharIndex >= 0 && editingActiveCharIndex < editingActiveCharacters.Length - 1)
                    {
                        editingActiveCharIndex++;
                    }
                    TargetChoiceDialog();
                    break;
                case ConsoleKey.LeftArrow:
                    if (editingActiveCharIndex > 0)
                    {
                        editingActiveCharIndex--;
                    }
                    TargetChoiceDialog();
                    break;
                default:
                    TargetChoiceDialog();
                    break;
            }
        }

        static byte[] editingColour;
        static int editingColourComponent;

        static bool ColourChoiceDialog()
        {
            ConsoleFrameBuffer frameBuffer = new ConsoleFrameBuffer();

            DrawInterface(frameBuffer);

            int dialogWidth = 44;
            int dialogHeight = 11;

            int dialogTop = (frameBuffer.WindowHeight - dialogHeight - 1) / 2;
            int dialogLeft = (frameBuffer.WindowWidth - dialogWidth) / 2;

            frameBuffer.CursorTop = dialogTop;
            frameBuffer.CursorLeft = dialogLeft;

            frameBuffer.ForegroundColor = ConsoleColor.Black;
            frameBuffer.BackgroundColor = ConsoleColor.Gray;

            frameBuffer.Write("╔" + " Choose colour ".Pad(dialogWidth - 2, Extensions.PadType.Center, '═') + "╗");

            for (int y = 1; y < dialogHeight - 1; y++)
            {
                frameBuffer.CursorLeft = dialogLeft;
                frameBuffer.CursorTop = dialogTop + y;
                frameBuffer.Write("║" + new string(' ', dialogWidth - 2) + "║");

                frameBuffer.ForegroundColor = ConsoleColor.Gray;
                frameBuffer.BackgroundColor = ConsoleColor.Black;
                frameBuffer.Write("▒▒");
                frameBuffer.ForegroundColor = ConsoleColor.Black;
                frameBuffer.BackgroundColor = ConsoleColor.Gray;
            }

            frameBuffer.CursorLeft = dialogLeft;
            frameBuffer.CursorTop = dialogTop + dialogHeight - 1;
            frameBuffer.Write("╚" + new string('═', dialogWidth - 2) + "╝");

            frameBuffer.ForegroundColor = ConsoleColor.Gray;
            frameBuffer.BackgroundColor = ConsoleColor.Black;

            frameBuffer.Write("▒▒");

            frameBuffer.CursorLeft = dialogLeft + 2;
            frameBuffer.CursorTop = dialogTop + dialogHeight;
            frameBuffer.Write(new string('▒', dialogWidth));

            frameBuffer.ForegroundColor = ConsoleColor.Black;
            frameBuffer.BackgroundColor = ConsoleColor.Gray;

            frameBuffer.CursorLeft = dialogLeft + 3;
            frameBuffer.CursorTop = dialogTop + 2;

            frameBuffer.Write("Current colour: ");
            frameBuffer.PlotColour(editingColour, 10);


            for (int i = 0; i < 3; i++)
            {
                frameBuffer.CursorTop += 2;
                frameBuffer.CursorLeft = dialogLeft + 3;

                frameBuffer.ForegroundColor = ConsoleColor.Black;
                frameBuffer.BackgroundColor = ConsoleColor.Gray;

                if (editingColourComponent != i)
                {
                    frameBuffer.Write(new string[] { "R", "G", "B" }[i] + " " + new string('─', 32) + " " + editingColour[i].ToString());
                    frameBuffer.CursorLeft = dialogLeft + 5 + (int)((double)editingColour[i] / 8.0);
                    frameBuffer.Write("■");
                }
                else
                {
                    frameBuffer.Write(new string[] { "R", "G", "B" }[i] + " " + new string('─', 32) + " ");

                    frameBuffer.ForegroundColor = ConsoleColor.Gray;
                    frameBuffer.BackgroundColor = i == 0 ? ConsoleColor.Red : i == 1 ? ConsoleColor.Green : ConsoleColor.Blue;
                    frameBuffer.Write(editingColour[i].ToString() + "■");

                    frameBuffer.ForegroundColor = i == 0 ? ConsoleColor.Red : i == 1 ? ConsoleColor.Green : ConsoleColor.Blue;
                    frameBuffer.BackgroundColor = ConsoleColor.Gray;
                    frameBuffer.CursorLeft = dialogLeft + 5 + (int)((double)editingColour[i] / 8.0);
                    frameBuffer.Write("■");
                }
            }

            mainFrameBuffer.Update(frameBuffer);

            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }

            ConsoleKeyInfo ki = Console.ReadKey(true);

            switch (ki.Key)
            {
                case ConsoleKey.Enter:
                    return true;

                case ConsoleKey.Escape:
                    return false;
                case ConsoleKey.UpArrow:
                    if (editingColourComponent > 0)
                    {
                        editingColourComponent -= 1;
                    }
                    return ColourChoiceDialog();
                case ConsoleKey.DownArrow:
                    if (editingColourComponent < 2)
                    {
                        editingColourComponent++;
                    }
                    return ColourChoiceDialog();
                case ConsoleKey.RightArrow:
                    if (editingColour[editingColourComponent] < 255)
                    {
                        editingColour[editingColourComponent]++;
                    }
                    return ColourChoiceDialog();
                case ConsoleKey.LeftArrow:
                    if (editingColour[editingColourComponent] > 0)
                    {
                        editingColour[editingColourComponent]--;
                    }
                    return ColourChoiceDialog();
                case ConsoleKey.Backspace:
                    {
                        string colString = editingColour[editingColourComponent].ToString();
                        if (colString.Length > 1)
                        {
                            editingColour[editingColourComponent] = byte.Parse(colString.Substring(0, colString.Length - 1));
                        }
                        else
                        {
                            editingColour[editingColourComponent] = 0;
                        }
                    }
                    return ColourChoiceDialog();
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
                    {
                        string colString = editingColour[editingColourComponent].ToString();
                        editingColour[editingColourComponent] = (byte)Math.Min(255, int.Parse(colString + ki.KeyChar));
                    }
                    return ColourChoiceDialog();
                default:
                    return ColourChoiceDialog();
            }
        }


        static byte[][] editingMultiColours;
        static int editingColourIndex;

        static bool MultiColourChoiceDialog()
        {
            ConsoleFrameBuffer frameBuffer = new ConsoleFrameBuffer();

            DrawInterface(frameBuffer);

            int stateRows = (int)Math.Sqrt(editingMultiColours.Length);
            int statesPerRow = (int)(Math.Ceiling((double)editingMultiColours.Length / stateRows));

            int maxStateLen = (from el in activeStateNames select el.Length).Max();

            int dialogWidth = Math.Max(40, statesPerRow * (maxStateLen + 3) + 8);
            int dialogHeight = stateRows + 4;

            int dialogTop = (frameBuffer.WindowHeight - dialogHeight - 1) / 2;
            int dialogLeft = (frameBuffer.WindowWidth - dialogWidth) / 2;

            frameBuffer.CursorTop = dialogTop;
            frameBuffer.CursorLeft = dialogLeft;

            frameBuffer.ForegroundColor = ConsoleColor.Black;
            frameBuffer.BackgroundColor = ConsoleColor.Gray;

            frameBuffer.Write("╔" + " Choose state colours ".Pad(dialogWidth - 2, Extensions.PadType.Center, '═') + "╗");

            for (int y = 1; y < dialogHeight - 1; y++)
            {
                frameBuffer.CursorLeft = dialogLeft;
                frameBuffer.CursorTop = dialogTop + y;
                frameBuffer.Write("║" + new string(' ', dialogWidth - 2) + "║");

                frameBuffer.ForegroundColor = ConsoleColor.Gray;
                frameBuffer.BackgroundColor = ConsoleColor.Black;
                frameBuffer.Write("▒▒");
                frameBuffer.ForegroundColor = ConsoleColor.Black;
                frameBuffer.BackgroundColor = ConsoleColor.Gray;
            }

            frameBuffer.CursorLeft = dialogLeft;
            frameBuffer.CursorTop = dialogTop + dialogHeight - 1;
            frameBuffer.Write("╚" + new string('═', dialogWidth - 2) + "╝");

            frameBuffer.ForegroundColor = ConsoleColor.Gray;
            frameBuffer.BackgroundColor = ConsoleColor.Black;

            frameBuffer.Write("▒▒");

            frameBuffer.CursorLeft = dialogLeft + 2;
            frameBuffer.CursorTop = dialogTop + dialogHeight;
            frameBuffer.Write(new string('▒', dialogWidth));

            frameBuffer.ForegroundColor = ConsoleColor.Black;
            frameBuffer.BackgroundColor = ConsoleColor.Gray;

            frameBuffer.CursorLeft = dialogLeft + 2;
            frameBuffer.CursorTop = dialogTop + 2;

            int padding = (dialogWidth - statesPerRow * (maxStateLen + 3)) / 2;

            frameBuffer.CursorLeft = dialogLeft + padding;

            for (int i = 0; i < editingMultiColours.Length; i++)
            {
                if (editingColourIndex == i)
                {
                    frameBuffer.ForegroundColor = ConsoleColor.Blue;
                }
                else
                {
                    frameBuffer.ForegroundColor = ConsoleColor.Black;
                }

                frameBuffer.Write(activeStateNames[i].Pad(maxStateLen, Extensions.PadType.Left));
                frameBuffer.PlotColour(editingMultiColours[i], 1);

                if ((i + 1) % statesPerRow == 0)
                {
                    frameBuffer.CursorLeft = dialogLeft + padding;
                    frameBuffer.CursorTop++;
                }
                else
                {
                    frameBuffer.CursorLeft += 2;
                }
            }

            mainFrameBuffer.Update(frameBuffer);

            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }

            ConsoleKeyInfo ki = Console.ReadKey(true);

            switch (ki.Key)
            {
                case ConsoleKey.Enter:
                    return true;

                case ConsoleKey.Escape:
                    return false;

                case ConsoleKey.UpArrow:

                    if (editingColourIndex >= statesPerRow)
                    {
                        editingColourIndex -= statesPerRow;
                    }

                    return MultiColourChoiceDialog();

                case ConsoleKey.DownArrow:
                    if (editingColourIndex + statesPerRow < editingMultiColours.Length)
                    {
                        editingColourIndex += statesPerRow;
                    }
                    return MultiColourChoiceDialog();

                case ConsoleKey.RightArrow:
                    if (editingColourIndex >= 0 && editingColourIndex < editingMultiColours.Length - 1)
                    {
                        editingColourIndex++;
                    }
                    return MultiColourChoiceDialog();

                case ConsoleKey.LeftArrow:
                    if (editingColourIndex > 0)
                    {
                        editingColourIndex--;
                    }
                    return MultiColourChoiceDialog();
                case ConsoleKey.Spacebar:
                    editingColour = (byte[])editingMultiColours[editingColourIndex].Clone();
                    if (ColourChoiceDialog())
                    {
                        editingMultiColours[editingColourIndex] = (byte[])editingColour.Clone();
                    }
                    return MultiColourChoiceDialog();
                default:
                    return MultiColourChoiceDialog();
            }
        }

        static string outputFileName = "";
        static string settingsFileName = "";

        static void PlotDialog()
        {
            ConsoleFrameBuffer frameBuffer = new ConsoleFrameBuffer();

            DrawInterface(frameBuffer);

            int dialogWidth = Math.Min(80, Math.Max(50, outputFileName.Length + 5));

            int dialogTop = (frameBuffer.WindowHeight - 8) / 2;
            int dialogLeft = (frameBuffer.WindowWidth - dialogWidth) / 2;

            frameBuffer.CursorTop = dialogTop;
            frameBuffer.CursorLeft = dialogLeft;

            frameBuffer.ForegroundColor = ConsoleColor.Black;
            frameBuffer.BackgroundColor = ConsoleColor.Gray;

            frameBuffer.Write("╔" + " Enter output file name ".Pad(dialogWidth - 2, Extensions.PadType.Center, '═') + "╗");

            for (int y = 1; y < 6; y++)
            {
                frameBuffer.CursorLeft = dialogLeft;
                frameBuffer.CursorTop = dialogTop + y;
                frameBuffer.Write("║" + new string(' ', dialogWidth - 2) + "║");

                frameBuffer.ForegroundColor = ConsoleColor.Gray;
                frameBuffer.BackgroundColor = ConsoleColor.Black;
                frameBuffer.Write("▒▒");
                frameBuffer.ForegroundColor = ConsoleColor.Black;
                frameBuffer.BackgroundColor = ConsoleColor.Gray;
            }

            frameBuffer.CursorLeft = dialogLeft;
            frameBuffer.CursorTop = dialogTop + 6;
            frameBuffer.Write("╚" + new string('═', dialogWidth - 2) + "╝");

            frameBuffer.ForegroundColor = ConsoleColor.Gray;
            frameBuffer.BackgroundColor = ConsoleColor.Black;

            frameBuffer.Write("▒▒");

            frameBuffer.CursorLeft = dialogLeft + 2;
            frameBuffer.CursorTop = dialogTop + 7;
            frameBuffer.Write(new string('▒', dialogWidth));

            frameBuffer.ForegroundColor = ConsoleColor.Black;
            frameBuffer.BackgroundColor = ConsoleColor.Gray;

            frameBuffer.CursorLeft = dialogLeft + 2;
            frameBuffer.CursorTop = dialogTop + 2;

            frameBuffer.Write("Output file: ");

            frameBuffer.CursorLeft = dialogLeft + 2;
            frameBuffer.CursorTop += 2;
            frameBuffer.ForegroundColor = ConsoleColor.Black;
            frameBuffer.BackgroundColor = ConsoleColor.Blue;

            if (outputFileName.Length <= dialogWidth - 5)
            {
                frameBuffer.Write(outputFileName + "■" + new string(' ', Math.Max(0, dialogWidth - 5 - outputFileName.Length)));
            }
            else
            {
                frameBuffer.Write("…" + outputFileName.Substring(outputFileName.Length - dialogWidth + 6) + "■" + new string(' ', Math.Max(0, dialogWidth - 5 - outputFileName.Length)));
            }

            mainFrameBuffer.Update(frameBuffer);

            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }

            ConsoleKeyInfo ki = Console.ReadKey(true);

            switch (ki.Key)
            {
                case ConsoleKey.Enter:
                    if (outputFileName.Length > 0)
                    {
                        Plot();
                        return;
                    }
                    PlotDialog();
                    break;
                case ConsoleKey.Escape:
                    return;
                case ConsoleKey.Backspace:
                    if (outputFileName.Length > 0)
                    {
                        outputFileName = outputFileName.Substring(0, outputFileName.Length - 1);
                    }
                    PlotDialog();
                    break;
                default:
                    if (!char.IsControl(ki.KeyChar))
                    {
                        outputFileName += ki.KeyChar;
                    }
                    PlotDialog();
                    break;
            }
        }


        static void Plot()
        {
            switch (plotTarget)
            {
                case PlotTargets.Tree:
                    PlotTree();
                    break;
                case PlotTargets.MeanPriors:
                    PlotPriors();
                    break;
                case PlotTargets.MeanPosteriors:
                    PlotPosteriors();
                    break;
                case PlotTargets.MeanCondPosteriors:
                    PlotConditionedProbs();
                    break;
                case PlotTargets.StochasticMap:
                    PlotSMap();
                    break;
                case PlotTargets.SampleSizes:
                    PlotSampleSizes();
                    break;
            }
        }

        static void PlotTree()
        {

            string realFontFamily = "";

            if (fonts.Contains(fontFamily) && !fontFamily.Contains(".ttf"))
            {
                realFontFamily = fontFamily;
            }
            else
            {
                if (File.Exists(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily)))
                {
                    realFontFamily = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily);
                }
                else if (File.Exists(fontFamily))
                {
                    realFontFamily = fontFamily;
                }
                else
                {
                    realFontFamily = Path.Combine(Directory.GetCurrentDirectory(), fontFamily);
                }
            }

            Plotting.Options opt = new Plotting.Options()
            {
                FontFamily = realFontFamily,
                FontSize = fontSize,
                NodeNumbers = nodeNumbers,
                LineWidth = lineWidth,
                PieSize = 0,
                ScaleAxis = scaleAxis,
                ScaleSpacing = scaleSpacing,
                ScaleGrid = scaleGrid,
                GridSpacing = gridSpacing,
                GridColour = gridColour,
                GridWidth = gridLineWidth,
                TreeScale = treeAgeScale,
                SignificantDigits = significantDigits
            };


            run.SummaryTree.PlotSimpleTree(plotWidth, plotHeight, margins, outputFileName, opt);
        }

        static void PlotPriors()
        {
            string realFontFamily = "";

            if (fonts.Contains(fontFamily) && !fontFamily.Contains(".ttf"))
            {
                realFontFamily = fontFamily;
            }
            else
            {
                if (File.Exists(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily)))
                {
                    realFontFamily = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily);
                }
                else if (File.Exists(fontFamily))
                {
                    realFontFamily = fontFamily;
                }
                else
                {
                    realFontFamily = Path.Combine(Directory.GetCurrentDirectory(), fontFamily);
                }
            }

            Plotting.Options opt = new Plotting.Options()
            {
                FontFamily = realFontFamily,
                FontSize = fontSize,
                NodeNumbers = nodeNumbers,
                LineWidth = lineWidth,
                PieSize = pieSize,
                ScaleAxis = scaleAxis,
                ScaleSpacing = scaleSpacing,
                ScaleGrid = scaleGrid,
                GridSpacing = gridSpacing,
                GridColour = gridColour,
                GridWidth = gridLineWidth,
                TreeScale = treeAgeScale,
                SignificantDigits = significantDigits,
                StateColours = stateColours
            };

            run.SummaryTree.PlotTreeWithPies(plotWidth, plotHeight, margins, outputFileName, opt, GetMarginalProbabilities(run.MeanPrior), activeStateNames);
        }


        static void PlotPosteriors()
        {
            string realFontFamily = "";

            if (fonts.Contains(fontFamily) && !fontFamily.Contains(".ttf"))
            {
                realFontFamily = fontFamily;
            }
            else
            {
                if (File.Exists(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily)))
                {
                    realFontFamily = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily);
                }
                else if (File.Exists(fontFamily))
                {
                    realFontFamily = fontFamily;
                }
                else
                {
                    realFontFamily = Path.Combine(Directory.GetCurrentDirectory(), fontFamily);
                }
            }

            Plotting.Options opt = new Plotting.Options()
            {
                FontFamily = realFontFamily,
                FontSize = fontSize,
                NodeNumbers = nodeNumbers,
                LineWidth = lineWidth,
                PieSize = pieSize,
                ScaleAxis = scaleAxis,
                ScaleSpacing = scaleSpacing,
                ScaleGrid = scaleGrid,
                GridSpacing = gridSpacing,
                GridColour = gridColour,
                GridWidth = gridLineWidth,
                TreeScale = treeAgeScale,
                SignificantDigits = significantDigits,
                StateColours = stateColours
            };



            run.SummaryTree.PlotTreeWithPies(plotWidth, plotHeight, margins, outputFileName, opt, GetMarginalProbabilities(run.MeanPosterior), activeStateNames);
        }

        static void PlotConditionedProbs()
        {
            string realFontFamily = "";

            if (fonts.Contains(fontFamily) && !fontFamily.Contains(".ttf"))
            {
                realFontFamily = fontFamily;
            }
            else
            {
                if (File.Exists(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily)))
                {
                    realFontFamily = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily);
                }
                else if (File.Exists(fontFamily))
                {
                    realFontFamily = fontFamily;
                }
                else
                {
                    realFontFamily = Path.Combine(Directory.GetCurrentDirectory(), fontFamily);
                }
            }

            Plotting.Options opt = new Plotting.Options()
            {
                FontFamily = realFontFamily,
                FontSize = fontSize,
                NodeNumbers = nodeNumbers,
                LineWidth = lineWidth,
                PieSize = pieSize,
                ScaleAxis = scaleAxis,
                ScaleSpacing = scaleSpacing,
                ScaleGrid = scaleGrid,
                GridSpacing = gridSpacing,
                GridColour = gridColour,
                GridWidth = gridLineWidth,
                TreeScale = treeAgeScale,
                SignificantDigits = significantDigits,
                StateColours = stateColours
            };



            run.SummaryTree.PlotTreeWithPies(plotWidth, plotHeight, margins, outputFileName, opt, GetMarginalProbabilities(GetConditionedProbabilities()), activeStateNames);
        }


        static void PlotSMap()
        {
            string realFontFamily = "";

            if (fonts.Contains(fontFamily) && !fontFamily.Contains(".ttf"))
            {
                realFontFamily = fontFamily;
            }
            else
            {
                if (File.Exists(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily)))
                {
                    realFontFamily = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily);
                }
                else if (File.Exists(fontFamily))
                {
                    realFontFamily = fontFamily;
                }
                else
                {
                    realFontFamily = Path.Combine(Directory.GetCurrentDirectory(), fontFamily);
                }
            }

            Plotting.Options opt = new Plotting.Options()
            {
                FontFamily = realFontFamily,
                FontSize = fontSize,
                NodeNumbers = nodeNumbers,
                LineWidth = lineWidth,
                PieSize = pieSize,
                ScaleAxis = scaleAxis,
                ScaleSpacing = scaleSpacing,
                ScaleGrid = scaleGrid,
                GridSpacing = gridSpacing,
                GridColour = gridColour,
                GridWidth = gridLineWidth,
                TreeScale = treeAgeScale,
                SignificantDigits = significantDigits,
                StateColours = stateColours,
                BranchSize = branchWidth
            };

            run.SummaryTree.PlotTreeWithPiesAndBranchStates(plotWidth, plotHeight, margins, outputFileName, opt, GetMarginalProbabilities(GetConditionedProbabilities()), GetMarginalHistories(), run.TreeSamples, run.LikelihoodModels, new LikelihoodModel(run.SummaryTree), run.SummaryNodeCorresp, branchTimeResolution, new List<string>(activeStateNames));

        }

        static void PlotSampleSizes()
        {
            string realFontFamily = "";

            if (fonts.Contains(fontFamily) && !fontFamily.Contains(".ttf"))
            {
                realFontFamily = fontFamily;
            }
            else
            {
                if (File.Exists(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily)))
                {
                    realFontFamily = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), fontFamily);
                }
                else if (File.Exists(fontFamily))
                {
                    realFontFamily = fontFamily;
                }
                else
                {
                    realFontFamily = Path.Combine(Directory.GetCurrentDirectory(), fontFamily);
                }
            }

            Plotting.Options opt = new Plotting.Options()
            {
                FontFamily = realFontFamily,
                FontSize = fontSize,
                NodeNumbers = nodeNumbers,
                LineWidth = lineWidth,
                PieSize = pieSize,
                ScaleAxis = scaleAxis,
                ScaleSpacing = scaleSpacing,
                ScaleGrid = scaleGrid,
                GridSpacing = gridSpacing,
                GridColour = gridColour,
                GridWidth = gridLineWidth,
                TreeScale = treeAgeScale,
                SignificantDigits = significantDigits,
                StateColours = stateColours,
                BranchSize = branchWidth
            };

            run.SummaryTree.PlotTreeWithBranchSampleSizes(plotWidth, plotHeight, margins, outputFileName, opt, run.Histories, run.TreeSamples, run.LikelihoodModels, new LikelihoodModel(run.SummaryTree), run.SummaryNodeCorresp, branchTimeResolution, new List<string>(activeStateNames));
        }

        static TaggedHistory[] GetMarginalHistories()
        {
            List<string[]> activeStates = new List<string[]>();
            List<int> activeChars = new List<int>();

            for (int i = 0; i < marginalActiveCharacters.Length; i++)
            {
                if (marginalActiveCharacters[i])
                {
                    List<string> st = new List<string>();

                    for (int j = 0; j < run.States.Length; j++)
                    {
                        if (!st.Contains(run.States[j].Split(',')[i]))
                        {
                            st.Add(run.States[j].Split(',')[i]);
                        }
                    }

                    activeStates.Add(st.ToArray());

                    activeChars.Add(i);
                }
            }

            string[][] stateCombinations = Utils.Utils.GetCombinations(activeStates.ToArray());

            int[] invCorrespStates = new int[run.States.Length];

            Dictionary<string, int> statesInds = new Dictionary<string, int>();

            for (int i = 0; i < run.States.Length; i++)
            {
                statesInds.Add(run.States[i], i);

                string[] splitState = run.States[i].Split(',');

                for (int j = 0; j < stateCombinations.Length; j++)
                {
                    bool foundDiff = false;
                    for (int k = 0; k < stateCombinations[j].Length; k++)
                    {
                        if (stateCombinations[j][k] != splitState[activeChars[k]])
                        {
                            foundDiff = true;
                            break;
                        }
                    }
                    if (!foundDiff)
                    {
                        invCorrespStates[i] = j;
                        break;
                    }
                }
            }

            TaggedHistory[] marginalHistories = new TaggedHistory[run.Histories.Length];

            for (int l = 0; l < run.Histories.Length; l++)
            {
                marginalHistories[l] = new TaggedHistory(run.Histories[l].Tag, Utils.Simulation.GetMarginalHistory(run.Histories[l].History, invCorrespStates, (from el in stateCombinations select Utils.Utils.StringifyArray(el)).ToArray(), statesInds));
            }

            return marginalHistories;
        }

        static double[][] GetMarginalProbabilities(double[][] meanProb)
        {
            List<string[]> activeStates = new List<string[]>();
            List<int> activeChars = new List<int>();

            for (int i = 0; i < marginalActiveCharacters.Length; i++)
            {
                if (marginalActiveCharacters[i])
                {
                    List<string> st = new List<string>();

                    for (int j = 0; j < run.States.Length; j++)
                    {
                        if (!st.Contains(run.States[j].Split(',')[i]))
                        {
                            st.Add(run.States[j].Split(',')[i]);
                        }
                    }

                    activeStates.Add(st.ToArray());

                    activeChars.Add(i);
                }
            }

            string[][] stateCombinations = Utils.Utils.GetCombinations(activeStates.ToArray());

            double[][] tbr = new double[meanProb.Length][];

            for (int j = 0; j < meanProb.Length; j++)
            {
                tbr[j] = new double[stateCombinations.Length];
            }


            for (int i = 0; i < stateCombinations.Length; i++)
            {
                bool[] correspStates = new bool[run.States.Length];

                for (int j = 0; j < run.States.Length; j++)
                {
                    correspStates[j] = true;
                }

                for (int j = 0; j < run.States.Length; j++)
                {
                    for (int k = 0; k < stateCombinations[i].Length; k++)
                    {
                        if (run.States[j].Split(',')[activeChars[k]] != stateCombinations[i][k])
                        {
                            correspStates[j] = false;
                        }
                    }
                }

                for (int j = 0; j < meanProb.Length; j++)
                {
                    tbr[j][i] = 0;

                    for (int k = 0; k < correspStates.Length; k++)
                    {
                        if (correspStates[k])
                        {
                            tbr[j][i] += meanProb[j][k];
                        }
                    }
                }
            }

            return tbr;
        }

        static double[][] GetConditionedProbabilities()
        {
            bool isClockLike = run.SummaryTree.IsClocklike();

            double[][] tbr = new double[run.MeanPosterior.Length][];

            LikelihoodModel summaryLikelihoodModel = new LikelihoodModel(run.SummaryTree);

            for (int i = 0; i < tbr.Length; i++)
            {

                if (isClockLike)
                {
                    tbr[i] = Utils.Utils.GetBranchStateProbs(run.Histories, run.TreeSamples, run.LikelihoodModels, summaryLikelihoodModel, run.SummaryNodeCorresp, new List<string>(run.AllPossibleStates), i, 0, true);
                }
                else
                {
                    tbr[i] = Utils.Utils.GetBranchStateProbs(run.Histories, run.TreeSamples, run.LikelihoodModels, summaryLikelihoodModel, run.SummaryNodeCorresp, new List<string>(run.AllPossibleStates), i, summaryLikelihoodModel.BranchLengths[i], false);
                }
            }

            if (!isClockLike)
            {
                tbr[tbr.Length - 1] = run.MeanPosterior.Last();
            }

            return tbr;
        }

        static void SaveDialog()
        {
            ConsoleFrameBuffer frameBuffer = new ConsoleFrameBuffer();

            DrawInterface(frameBuffer);

            int dialogWidth = Math.Min(80, Math.Max(50, settingsFileName.Length + 5));

            int dialogTop = (frameBuffer.WindowHeight - 8) / 2;
            int dialogLeft = (frameBuffer.WindowWidth - dialogWidth) / 2;

            frameBuffer.CursorTop = dialogTop;
            frameBuffer.CursorLeft = dialogLeft;

            frameBuffer.ForegroundColor = ConsoleColor.Black;
            frameBuffer.BackgroundColor = ConsoleColor.Gray;

            frameBuffer.Write("╔" + " Enter settings file name ".Pad(dialogWidth - 2, Extensions.PadType.Center, '═') + "╗");

            for (int y = 1; y < 6; y++)
            {
                frameBuffer.CursorLeft = dialogLeft;
                frameBuffer.CursorTop = dialogTop + y;
                frameBuffer.Write("║" + new string(' ', dialogWidth - 2) + "║");

                frameBuffer.ForegroundColor = ConsoleColor.Gray;
                frameBuffer.BackgroundColor = ConsoleColor.Black;
                frameBuffer.Write("▒▒");
                frameBuffer.ForegroundColor = ConsoleColor.Black;
                frameBuffer.BackgroundColor = ConsoleColor.Gray;
            }

            frameBuffer.CursorLeft = dialogLeft;
            frameBuffer.CursorTop = dialogTop + 6;
            frameBuffer.Write("╚" + new string('═', dialogWidth - 2) + "╝");

            frameBuffer.ForegroundColor = ConsoleColor.Gray;
            frameBuffer.BackgroundColor = ConsoleColor.Black;

            frameBuffer.Write("▒▒");

            frameBuffer.CursorLeft = dialogLeft + 2;
            frameBuffer.CursorTop = dialogTop + 7;
            frameBuffer.Write(new string('▒', dialogWidth));

            frameBuffer.ForegroundColor = ConsoleColor.Black;
            frameBuffer.BackgroundColor = ConsoleColor.Gray;

            frameBuffer.CursorLeft = dialogLeft + 2;
            frameBuffer.CursorTop = dialogTop + 2;

            frameBuffer.Write("Output file: ");

            frameBuffer.CursorLeft = dialogLeft + 2;
            frameBuffer.CursorTop += 2;
            frameBuffer.ForegroundColor = ConsoleColor.Black;
            frameBuffer.BackgroundColor = ConsoleColor.Blue;

            if (settingsFileName.Length <= dialogWidth - 5)
            {
                frameBuffer.Write(settingsFileName + "■" + new string(' ', Math.Max(0, dialogWidth - 5 - settingsFileName.Length)));
            }
            else
            {
                frameBuffer.Write("…" + settingsFileName.Substring(settingsFileName.Length - dialogWidth + 6) + "■" + new string(' ', Math.Max(0, dialogWidth - 5 - settingsFileName.Length)));
            }

            mainFrameBuffer.Update(frameBuffer);

            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }

            ConsoleKeyInfo ki = Console.ReadKey(true);

            switch (ki.Key)
            {
                case ConsoleKey.Enter:
                    if (settingsFileName.Length > 0)
                    {
                        Save();
                        return;
                    }
                    SaveDialog();
                    break;
                case ConsoleKey.Escape:
                    return;
                case ConsoleKey.Backspace:
                    if (settingsFileName.Length > 0)
                    {
                        settingsFileName = settingsFileName.Substring(0, settingsFileName.Length - 1);
                    }
                    SaveDialog();
                    break;
                default:
                    if (!char.IsControl(ki.KeyChar))
                    {
                        settingsFileName += ki.KeyChar;
                    }
                    SaveDialog();
                    break;
            }
        }

        static void Save()
        {
            using (StreamWriter sw = new StreamWriter(settingsFileName))
            {
                sw.WriteLine("--target");
                switch (plotTarget)
                {
                    case PlotTargets.Tree:
                        sw.WriteLine("Tree");
                        break;
                    case PlotTargets.MeanPriors:
                        sw.WriteLine("MeanPriors");
                        break;
                    case PlotTargets.MeanPosteriors:
                        sw.WriteLine("MeanPosteriors");
                        break;
                    case PlotTargets.MeanCondPosteriors:
                        sw.WriteLine("MeanCondPosteriors");
                        break;
                    case PlotTargets.StochasticMap:
                        sw.WriteLine("StochasticMap");
                        break;
                    case PlotTargets.SampleSizes:
                        sw.WriteLine("SampleSizes");
                        break;
                }

                sw.WriteLine("--width");
                sw.WriteLine(plotWidth.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--height");
                sw.WriteLine(plotHeight.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--margins");
                sw.WriteLine(margins.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--font-size");
                sw.WriteLine(fontSize.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--font-family");
                sw.WriteLine(fontFamily);

                sw.WriteLine("--pie-size");
                sw.WriteLine(pieSize.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--branch-width");
                sw.WriteLine(branchWidth.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--line-width");
                sw.WriteLine(lineWidth.ToString(System.Globalization.CultureInfo.InvariantCulture));

                if (nodeNumbers)
                {
                    sw.WriteLine("--node-ids");
                }

                if (scaleAxis)
                {
                    sw.WriteLine("--scale-axis");
                }

                sw.WriteLine("--scale-spacing");
                sw.WriteLine(scaleSpacing.ToString(System.Globalization.CultureInfo.InvariantCulture));

                if (scaleGrid)
                {
                    sw.WriteLine("--scale-grid");
                }

                sw.WriteLine("--grid-spacing");
                sw.WriteLine(gridSpacing.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--grid-line-width");
                sw.WriteLine(gridLineWidth.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--grid-colour");
                sw.WriteLine(Utils.Utils.StringifyArray(gridColour, ","));

                sw.WriteLine("--digits");
                sw.WriteLine(significantDigits.ToString());

                sw.WriteLine("--age-scale");
                sw.WriteLine(treeAgeScale.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--resolution");
                sw.WriteLine(branchTimeResolution.ToString(System.Globalization.CultureInfo.InvariantCulture));

                sw.WriteLine("--active-characters");
                string activeChars = "";
                for (int i = 0; i < marginalActiveCharacters.Length; i++)
                {
                    if (marginalActiveCharacters[i])
                    {
                        activeChars += i.ToString() + ",";
                    }
                }
                sw.WriteLine(activeChars.Substring(0, activeChars.Length - 1));

                sw.WriteLine("--state-colours");
                string colourStr = "";

                for (int i = 0; i < stateColours.Length; i++)
                {
                    colourStr += "[" + Utils.Utils.StringifyArray(stateColours[i], ",") + "]:";
                }

                sw.WriteLine(colourStr.Substring(0, colourStr.Length - 1));
            }
        }
    }
}

