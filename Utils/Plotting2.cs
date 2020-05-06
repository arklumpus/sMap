using MathNet.Numerics.Distributions;
using SlimTreeNode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VectSharp;
using VectSharp.PDF;

namespace Utils
{
    public static partial class Plotting
    {
        public static void PlotDTest(DTest test, DStats[] priorDstats, string[][] states, double[][] timeSpent, string outputFile)
        {
            Document doc = new Document();
            Font titleFont = new Font(new FontFamily(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "OpenSans-Bold.ttf")), 60);
            Font subtitleFont = new Font(new FontFamily(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "OpenSans-Bold.ttf")), 50);
            Font boldFont = new Font(new FontFamily(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "OpenSans-Bold.ttf")), 36);
            Font normalFont = new Font(new FontFamily(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "OpenSans-Regular.ttf")), 36);


            //Time spent
            {
                double cellWidth = (from el in timeSpent select (from el2 in el select normalFont.MeasureText(el2.ToString("0.###%", System.Globalization.CultureInfo.InvariantCulture)).Width).Max()).Max() + 50;

                double width = Math.Max(924, cellWidth * (states[1].Length + 2));
                double height = 175 + 60 * (states[0].Length + 2) * 2 + 140;

                doc.Pages.Add(new Page(width + 100, height + 100));

                Graphics gpr = doc.Pages.Last().Graphics;

                gpr.Translate(50, 50);

                gpr.FillText(width / 2 - titleFont.MeasureText("Time spent").Width / 2, 0, "Time spent", titleFont, BlackColour);

                gpr.FillText(0, 75, "Actual:", subtitleFont, BlackColour);

                gpr.Translate((width - cellWidth * (states[1].Length + 2)) * 0.5, 175);

                for (int i = 0; i < timeSpent.Length + 1; i++)
                {
                    gpr.StrokePath(new GraphicsPath().MoveTo(0, 30 + i * 60).LineTo(cellWidth * (timeSpent[0].Length + 2), 30 + i * 60), Colour.FromRgb(180, 180, 180), lineWidth: 3);
                    if (i > 0 && i < timeSpent.Length + 1)
                    {
                        gpr.FillText(cellWidth * 0.5 - boldFont.MeasureText(states[0][i - 1]).Width / 2, 60 * i, states[0][i - 1], boldFont, BlackColour, TextBaselines.Middle);
                    }
                }

                for (int j = 0; j < timeSpent[0].Length + 1; j++)
                {
                    gpr.StrokePath(new GraphicsPath().MoveTo(cellWidth * (j + 1), -30).LineTo(cellWidth * (j + 1), 60 * (timeSpent.Length + 1.5)), Colour.FromRgb(180, 180, 180), lineWidth: 3);

                    if (j < timeSpent[0].Length)
                    {
                        gpr.FillText(j * cellWidth + cellWidth * 1.5 - boldFont.MeasureText(states[1][j]).Width / 2, 0, states[1][j], boldFont, BlackColour, TextBaselines.Middle);
                    }
                }

                gpr.Translate(0, 60);

                for (int i = 0; i < timeSpent.Length; i++)
                {
                    for (int j = 0; j < timeSpent[i].Length; j++)
                    {
                        gpr.FillText(j * cellWidth + cellWidth * 1.5 - normalFont.MeasureText(timeSpent[i][j].ToString("0.###%", System.Globalization.CultureInfo.InvariantCulture)).Width / 2, 0, timeSpent[i][j].ToString("0.###%", System.Globalization.CultureInfo.InvariantCulture), normalFont, BlackColour, TextBaselines.Middle);
                    }
                    gpr.FillText(timeSpent[i].Length * cellWidth + cellWidth * 1.5 - normalFont.MeasureText(timeSpent[i].Sum().ToString("0.###%", System.Globalization.CultureInfo.InvariantCulture)).Width / 2, 0, timeSpent[i].Sum().ToString("0.###%", System.Globalization.CultureInfo.InvariantCulture), normalFont, Colour.FromRgb(128, 128, 128), TextBaselines.Middle);
                    gpr.Translate(0, 60);
                }

                for (int j = 0; j < timeSpent[0].Length; j++)
                {
                    gpr.FillText(j * cellWidth + cellWidth * 1.5 - normalFont.MeasureText((from el in timeSpent select el[j]).Sum().ToString("0.###%", System.Globalization.CultureInfo.InvariantCulture)).Width / 2, 0, (from el in timeSpent select el[j]).Sum().ToString("0.###%", System.Globalization.CultureInfo.InvariantCulture), normalFont, Colour.FromRgb(128, 128, 128), TextBaselines.Middle);
                }

                gpr.FillText(0, 100, "Expected:", subtitleFont, BlackColour);

                gpr.Translate(-(width - cellWidth * 4) * 0.5, 100);

                double[][] expectedTime = new double[timeSpent.Length][];

                for (int i = 0; i < timeSpent.Length; i++)
                {
                    expectedTime[i] = new double[timeSpent[i].Length];
                    for (int j = 0; j < timeSpent[i].Length; j++)
                    {
                        expectedTime[i][j] = timeSpent[i].Sum() * (from el in timeSpent select el[j]).Sum();
                    }
                }

                gpr.Translate((width - cellWidth * 4) * 0.5, 100);

                for (int i = 0; i < expectedTime.Length + 1; i++)
                {
                    gpr.StrokePath(new GraphicsPath().MoveTo(0, 30 + i * 60).LineTo(cellWidth * (expectedTime[0].Length + 2), 30 + i * 60), Colour.FromRgb(180, 180, 180), lineWidth: 3);
                    if (i > 0 && i < expectedTime.Length + 1)
                    {
                        gpr.FillText(cellWidth * 0.5 - boldFont.MeasureText(states[0][i - 1]).Width / 2, 60 * i, states[0][i - 1], boldFont, BlackColour, TextBaselines.Middle);
                    }
                }

                for (int j = 0; j < expectedTime[0].Length + 1; j++)
                {
                    gpr.StrokePath(new GraphicsPath().MoveTo(cellWidth * (j + 1), -30).LineTo(cellWidth * (j + 1), 60 * (expectedTime.Length + 1.5)), Colour.FromRgb(180, 180, 180), lineWidth: 3);

                    if (j < expectedTime[0].Length)
                    {
                        gpr.FillText(j * cellWidth + cellWidth * 1.5 - boldFont.MeasureText(states[1][j]).Width / 2, 0, states[1][j], boldFont, BlackColour, TextBaselines.Middle);
                    }
                }

                gpr.Translate(0, 60);

                for (int i = 0; i < expectedTime.Length; i++)
                {
                    for (int j = 0; j < expectedTime[i].Length; j++)
                    {
                        gpr.FillText(j * cellWidth + cellWidth * 1.5 - normalFont.MeasureText(expectedTime[i][j].ToString("0.###%", System.Globalization.CultureInfo.InvariantCulture)).Width / 2, 0, expectedTime[i][j].ToString("0.###%", System.Globalization.CultureInfo.InvariantCulture), normalFont, BlackColour, TextBaselines.Middle);
                    }
                    gpr.FillText(expectedTime[i].Length * cellWidth + cellWidth * 1.5 - normalFont.MeasureText(expectedTime[i].Sum().ToString("0.###%", System.Globalization.CultureInfo.InvariantCulture)).Width / 2, 0, expectedTime[i].Sum().ToString("0.###%", System.Globalization.CultureInfo.InvariantCulture), normalFont, Colour.FromRgb(128, 128, 128), TextBaselines.Middle);
                    gpr.Translate(0, 60);
                }

                for (int j = 0; j < expectedTime[0].Length; j++)
                {
                    gpr.FillText(j * cellWidth + cellWidth * 1.5 - normalFont.MeasureText((from el in expectedTime select el[j]).Sum().ToString("0.###%", System.Globalization.CultureInfo.InvariantCulture)).Width / 2, 0, (from el in expectedTime select el[j]).Sum().ToString("0.###%", System.Globalization.CultureInfo.InvariantCulture), normalFont, Colour.FromRgb(128, 128, 128), TextBaselines.Middle);
                }
            }


            //Overall stats
            {

                double cellWidth = Math.Max(normalFont.MeasureText("0.0000001").Width, (from el in test.DStats.dij select (from el2 in el select normalFont.MeasureText(el2.ToString("0.###%", System.Globalization.CultureInfo.InvariantCulture)).Width).Max()).Max()) + 50;

                double width = Math.Max(924, cellWidth * (states[1].Length + 1));
                double height = 375 + 60 * (states[0].Length + 1) + 175 + 60 * (states[0].Length + 1);

                doc.Pages.Add(new Page(width + 100, height + 100));

                Graphics gpr = doc.Pages.Last().Graphics;

                gpr.Translate(50, 50);

                gpr.FillText(width / 2 - titleFont.MeasureText("D-test").Width / 2, 0, "D-test", titleFont, BlackColour);

                gpr.FillText(0, 150, "D", boldFont, BlackColour, TextBaselines.Baseline);
                gpr.FillText(boldFont.MeasureText("D").Width + boldFont.MeasureTextAdvanced("D").RightSideBearing, 150, " = " + test.DStats.D.ToString(System.Globalization.CultureInfo.InvariantCulture), normalFont, BlackColour, TextBaselines.Baseline);

                gpr.FillText(0, 200, "Posterior-predictive", normalFont, BlackColour, TextBaselines.Baseline);
                gpr.FillText(normalFont.MeasureText("Posterior-predictive").Width + normalFont.MeasureTextAdvanced("Posterior-predictive").RightSideBearing, 200, " P", boldFont, BlackColour, TextBaselines.Baseline);
                gpr.Save();


                gpr.FillText(0, 300, "d", subtitleFont, BlackColour, TextBaselines.Baseline);
                gpr.FillText(subtitleFont.MeasureText("d").Width + subtitleFont.MeasureTextAdvanced("d").RightSideBearing + boldFont.MeasureTextAdvanced("i,j").LeftSideBearing, 310, "i,j", boldFont, BlackColour, TextBaselines.Baseline);
                gpr.FillText(subtitleFont.MeasureText("d").Width + subtitleFont.MeasureTextAdvanced("d").RightSideBearing + boldFont.MeasureTextAdvanced("i,j").LeftSideBearing + boldFont.MeasureText("i,j").Width + boldFont.MeasureTextAdvanced("i,j").RightSideBearing, 300, " =", subtitleFont, BlackColour, TextBaselines.Baseline);

                gpr.Translate((width - cellWidth * (states[1].Length + 1)) / 2, 375);

                for (int i = 0; i < test.DStats.dij.Length + 1; i++)
                {
                    gpr.StrokePath(new GraphicsPath().MoveTo(0, 30 + i * 60).LineTo(cellWidth * (test.DStats.dij[0].Length + 1), 30 + i * 60), Colour.FromRgb(180, 180, 180), lineWidth: 3);
                    if (i > 0)
                    {
                        gpr.FillText(cellWidth * 0.5 - boldFont.MeasureText(states[0][i - 1]).Width / 2, 60 * i, states[0][i - 1], boldFont, BlackColour, TextBaselines.Middle);
                    }
                }

                for (int j = 0; j < test.DStats.dij[0].Length; j++)
                {
                    gpr.StrokePath(new GraphicsPath().MoveTo(cellWidth * (j + 1), -30).LineTo(cellWidth * (j + 1), 60 * (test.DStats.dij.Length + 1)), Colour.FromRgb(180, 180, 180), lineWidth: 3);
                    gpr.FillText(j * cellWidth + cellWidth * 1.5 - boldFont.MeasureText(states[1][j]).Width / 2, 0, states[1][j], boldFont, BlackColour, TextBaselines.Middle);
                }

                gpr.Translate(0, 60);

                for (int i = 0; i < test.DStats.dij.Length; i++)
                {
                    for (int j = 0; j < test.DStats.dij[i].Length; j++)
                    {
                        gpr.FillText(j * cellWidth + cellWidth * 1.5 - normalFont.MeasureText(test.DStats.dij[i][j].ToString("0.###%", System.Globalization.CultureInfo.InvariantCulture)).Width / 2, 0, test.DStats.dij[i][j].ToString("0.###%", System.Globalization.CultureInfo.InvariantCulture), normalFont, BlackColour, TextBaselines.Middle);
                    }
                    gpr.Translate(0, 60);
                }

                gpr.Translate(-(width - cellWidth * (states[1].Length + 1)) / 2, 100);

                gpr.FillText(0, 0, "P", subtitleFont, BlackColour, TextBaselines.Baseline);
                gpr.FillText(subtitleFont.MeasureText("P").Width + subtitleFont.MeasureTextAdvanced("d").RightSideBearing + boldFont.MeasureTextAdvanced("i,j").LeftSideBearing, 10, "i,j", boldFont, BlackColour, TextBaselines.Baseline);
                gpr.FillText(subtitleFont.MeasureText("P").Width + subtitleFont.MeasureTextAdvanced("d").RightSideBearing + boldFont.MeasureTextAdvanced("i,j").LeftSideBearing + boldFont.MeasureText("i,j").Width + boldFont.MeasureTextAdvanced("i,j").RightSideBearing, 0, " =", subtitleFont, BlackColour, TextBaselines.Baseline);

                gpr.Translate((width - cellWidth * (states[1].Length + 1)) / 2, 75);

                for (int i = 0; i < test.DStats.dij.Length + 1; i++)
                {
                    gpr.StrokePath(new GraphicsPath().MoveTo(0, 30 + i * 60).LineTo(cellWidth * (test.DStats.dij[0].Length + 1), 30 + i * 60), Colour.FromRgb(180, 180, 180), lineWidth: 3);
                    if (i > 0)
                    {
                        gpr.FillText(cellWidth * 0.5 - boldFont.MeasureText(states[0][i - 1]).Width / 2, 60 * i, states[0][i - 1], boldFont, BlackColour, TextBaselines.Middle);
                    }
                }

                for (int j = 0; j < test.DStats.dij[0].Length; j++)
                {
                    gpr.StrokePath(new GraphicsPath().MoveTo(cellWidth * (j + 1), -30).LineTo(cellWidth * (j + 1), 60 * (test.DStats.dij.Length + 1)), Colour.FromRgb(180, 180, 180), lineWidth: 3);
                    gpr.FillText(j * cellWidth + cellWidth * 1.5 - boldFont.MeasureText(states[1][j]).Width / 2, 0, states[1][j], boldFont, BlackColour, TextBaselines.Middle);
                }

                gpr.Translate(0, 60);

                for (int i = 0; i < test.DStats.dij.Length; i++)
                {
                    for (int j = 0; j < test.DStats.dij[i].Length; j++)
                    {
                        gpr.FillText(j * cellWidth + cellWidth * 1.5 - normalFont.MeasureText(test.Pij[i][j].ToString(2, false)).Width / 2, 0, test.Pij[i][j].ToString(2, false), normalFont, BlackColour, TextBaselines.Middle);
                    }
                    gpr.Translate(0, 60);
                }



                gpr.Restore();
            }

            double bandwidth = -1;
            DrawDTestDistributions(doc, test, priorDstats, timeSpent, states, ref bandwidth);

            doc.SaveAsPDF(outputFile);
        }

        public static double DrawDTestDistributions(Document doc, DTest test, DStats[] priorDstats, double[][] timeSpent, string[][] states, ref double kdeBandwidth)
        {
            Font titleFont = new Font(new FontFamily(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "OpenSans-Bold.ttf")), 60);
            Font boldFont = new Font(new FontFamily(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "OpenSans-Bold.ttf")), 36);
            Font normalFont = new Font(new FontFamily(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "OpenSans-Regular.ttf")), 36);

            double deltaX = 0;

            //Distribution
            {
                double width = 1248;
                double height = 924;

                doc.Pages.Add(new Page(width + 100, height + 100));

                Graphics gpr = doc.Pages.Last().Graphics;

                gpr.Translate(50, 50);

                gpr.FillText(width / 2 - titleFont.MeasureText("D-statistic distribution").Width / 2, 0, "D-statistic distribution", titleFont, BlackColour);

                gpr.Translate(0, 75);

                double[] dValues = (from el in priorDstats select el.D).ToArray();

                double priorD = 0;
                
                if (DStats.DStatMode == DStats.DStatModes.Median)
                {
                    priorD = dValues.Median();
                }
                else
                {
                    priorD = dValues.Average();
                }

                (double, string, bool)[] interestingValues = new (double, string, bool)[] { (priorD, "E(D|X*) = " + priorD.ToString(4), test.DStats.D < priorD), (test.DStats.D, "E(D|X) = " + test.DStats.D.ToString(4), test.DStats.D >= priorD) };

                deltaX = PlotHistogram(dValues, BinRules.FreedmanDiaconis, width, height - 75, height - 75, new Options() { FontFamily = normalFont.FontFamily.FileName, FontSize = (float)normalFont.FontSize, LineWidth = 3 }, gpr, Colour.FromRgb(34, 177, 76), null, 0, true, Math.Min(dValues.Min(), test.DStats.D), Math.Max(dValues.Max(), test.DStats.D), false, interestingValues);
            }


            //KDE
            {

                double timeSpent1 = 0;
                double timeSpent2 = 0;

                for (int i = 0; i < timeSpent.Length - 1; i++)
                {
                    timeSpent1 += timeSpent[i].Sum();
                }

                for (int j = 0; j < timeSpent[0].Length - 1; j++)
                {
                    timeSpent2 += (from el in timeSpent select el[j]).Sum();
                }

                double maxStat = 1;

                double width = 1248;
                double height = 924;

                doc.Pages.Add(new Page(width + 100, height + 100));

                Graphics gpr = doc.Pages.Last().Graphics;

                gpr.Translate(50, 50);

                gpr.FillText(width / 2 - titleFont.MeasureText("D-statistic distribution").Width / 2, 0, "D-statistic distribution", titleFont, BlackColour);

                width -= deltaX;
                height -= 75;
                gpr.Translate(deltaX, 75);

                height = (height - normalFont.FontSize) * 128 / 132;

                double[] dValues = (from el in priorDstats select el.D / maxStat).ToArray();
                
                double priorD = 0;

                if (DStats.DStatMode == DStats.DStatModes.Median)
                {
                    priorD = dValues.Median();
                }
                else
                {
                    priorD = dValues.Average();
                }

                double minValue = Math.Min(dValues.Min(), test.DStats.D / maxStat);
                double maxValue = Math.Max(dValues.Max(), test.DStats.D / maxStat);

                double range = maxValue - minValue;
                double minX = Math.Max(range * 0.001, minValue - range * 0.05);
                double maxX = Math.Min(1, maxValue + range * 0.05);

                double xResolution = 1000;

                List<double> ys = new List<double>();
                List<double> postYs = new List<double>();

                if (kdeBandwidth < 0)
                {
                    kdeBandwidth = GetBandwidth(minX, maxX, range, dValues, xResolution);
                }

                double bandwidth = kdeBandwidth;

                for (double x = minX; x <= maxX - range * 0.025; x += (maxX - minX) / xResolution)
                {
                    double y = 0;
                    for (int j = 0; j < dValues.Length; j++)
                    {
                        y += BetaKernelKDE(x, dValues[j], bandwidth);
                    }

                    if (x < test.DStats.D / maxStat)
                    {
                        ys.Add(y / dValues.Length);
                    }
                    else
                    {
                        postYs.Add(y / dValues.Length);
                    }
                }

                double maxY = Math.Max(ys.Max(), postYs.Max()) * 1.05;

                GraphicsPath path = new GraphicsPath();
                GraphicsPath fillPath = new GraphicsPath();

                fillPath.MoveTo(0, height);

                for (int i = 0; i < ys.Count; i++)
                {
                    if (i == 0)
                    {
                        path.MoveTo(i * width / xResolution, height * (1 - ys[i] / maxY));
                    }
                    else
                    {
                        path.LineTo(i * width / xResolution, height * (1 - ys[i] / maxY));
                    }

                    fillPath.LineTo(i * width / xResolution, height * (1 - ys[i] / maxY));
                }

                fillPath.LineTo((ys.Count - 1) * width / xResolution, height);
                fillPath.Close();

                GraphicsPath path2 = new GraphicsPath();
                GraphicsPath fillPath2 = new GraphicsPath();

                path2.MoveTo((ys.Count - 2) * width / xResolution, height * (1 - ys[ys.Count - 2] / maxY));
                path2.LineTo((ys.Count - 1) * width / xResolution, height * (1 - ys[ys.Count - 1] / maxY));
                fillPath2.MoveTo((ys.Count - 1) * width / xResolution, height);
                fillPath2.LineTo((ys.Count - 1) * width / xResolution, height * (1 - ys[ys.Count - 1] / maxY));

                for (int i = ys.Count; i < ys.Count + postYs.Count; i++)
                {
                    path2.LineTo(i * width / xResolution, height * (1 - postYs[i - ys.Count] / maxY));

                    fillPath2.LineTo(i * width / xResolution, height * (1 - postYs[i - ys.Count] / maxY));
                }

                fillPath2.LineTo((ys.Count + postYs.Count - 1) * width / xResolution, height);
                fillPath2.Close();

                gpr.FillPath(fillPath, Colour.FromRgb(196, 237, 255));
                gpr.FillPath(fillPath2, Colour.FromRgb(255, 211, 183));

                gpr.StrokePath(new GraphicsPath().
                   MoveTo(width / 128, width / 128).
                   LineTo(0, 0).
                   MoveTo(-width / 128, width / 128).
                   LineTo(0, 0).
                   LineTo(0, height).
                   LineTo(width, height).
                   LineTo(width * 127 / 128, height - width / 128).
                   MoveTo(width, height).
                   LineTo(width * 127 / 128, height + width / 128), BlackColour, 3);


                gpr.StrokePath(new GraphicsPath().
                    MoveTo((priorD - minX) / (maxX - minX) * width, height * 132 / 128).
                    LineTo((priorD - minX) / (maxX - minX) * width, 0), BlackColour, 3 * 0.75, lineDash: new LineDash((float)(3 * 0.75 * 3), (float)(3 * 0.75 * 5), 0));

                gpr.StrokePath(path2, Colour.FromRgb(255, 127, 39), lineWidth: 4, lineJoin: LineJoins.Round);
                gpr.StrokePath(path, Colour.FromRgb(0, 162, 232), lineWidth: 4, lineJoin: LineJoins.Round);

                (double, string, bool)[] interestingValues = new (double, string, bool)[] { (priorD, "E(D|X*) = " + (priorD * maxStat).ToString(4), test.DStats.D < (priorD * maxStat)), (test.DStats.D / maxStat, "E(D|X) = " + test.DStats.D.ToString(4), test.DStats.D >= (priorD * maxStat)) };

                if (interestingValues != null)
                {
                    Font smallerFont = new Font(normalFont.FontFamily, normalFont.FontSize * 0.75);
                    for (int i = 0; i < interestingValues.Length; i++)
                    {
                        gpr.StrokePath(new GraphicsPath().
                        MoveTo((interestingValues[i].Item1 - minX) / (maxX - minX) * width, height * 132 / 128).
                        LineTo((interestingValues[i].Item1 - minX) / (maxX - minX) * width, 0), BlackColour, 3 * 0.75, lineDash: new LineDash((float)(3 * 0.75 * 3), (float)(3 * 0.75 * 5), 0));

                        bool alignLeft = interestingValues[i].Item3;

                        if (alignLeft)
                        {
                            if ((interestingValues[i].Item1 - minX) / (maxX - minX) * width + gpr.MeasureText("  " + interestingValues[i].Item2, smallerFont).Width > width - deltaX)
                            {
                                alignLeft = false;
                            }
                        }
                        else
                        {
                            if ((interestingValues[i].Item1 - minX) / (maxX - minX) * width - gpr.MeasureText(interestingValues[i].Item2 + "  ", smallerFont).Width < -deltaX)
                            {
                                alignLeft = true;
                            }
                        }

                        if (alignLeft)
                        {
                            gpr.FillText((interestingValues[i].Item1 - minX) / (maxX - minX) * width + gpr.MeasureText("  ", smallerFont).Width, height * 130 / 128, interestingValues[i].Item2, smallerFont, BlackColour);
                        }
                        else
                        {
                            gpr.FillText((interestingValues[i].Item1 - minX) / (maxX - minX) * width - gpr.MeasureText(interestingValues[i].Item2 + "  ", smallerFont).Width, height * 130 / 128, interestingValues[i].Item2, smallerFont, BlackColour);
                        }
                    }
                }

                double integral = 0;
                double integralRange = maxStat - test.DStats.D;

                for (int step = 0; step < 1000; step++)
                {
                    double val = (test.DStats.D + step * integralRange / 1000) / maxStat;
                    double val1 = (test.DStats.D + (step + 1) * integralRange / 1000) / maxStat;

                    double y = 0;
                    for (int j = 0; j < dValues.Length; j++)
                    {
                        y += BetaKernelKDE(val, dValues[j], bandwidth);
                    }
                    y /= dValues.Length;

                    double y1 = 0;
                    for (int j = 0; j < dValues.Length; j++)
                    {
                        y1 += BetaKernelKDE(val1, dValues[j], bandwidth);
                    }
                    y1 /= dValues.Length;

                    integral += (y + y1) * (val1 - val) * 0.5;
                }

                if (doc.Pages.Count == 4)
                {
                    double left = normalFont.MeasureText("Posterior-predictive").Width + normalFont.MeasureTextAdvanced("Posterior-predictive").RightSideBearing + boldFont.MeasureText(" P").Width + boldFont.MeasureTextAdvanced(" P").RightSideBearing;

                    doc.Pages[1].Graphics.FillText(left, 200, " = " + test.P.ToString(System.Globalization.CultureInfo.InvariantCulture), normalFont, BlackColour, TextBaselines.Baseline);
                    left += normalFont.MeasureText(" = " + test.P.ToString(System.Globalization.CultureInfo.InvariantCulture) + " (").Width + normalFont.MeasureTextAdvanced(" = " + test.P.ToString(System.Globalization.CultureInfo.InvariantCulture)).RightSideBearing;
                    string integralString = integral.ToString(2, false);

                    if (integralString.Contains("e"))
                    {
                        doc.Pages[1].Graphics.Save();
                        doc.Pages[1].Graphics.Translate(left, 200 - normalFont.MeasureTextAdvanced("(").Bottom);
                        doc.Pages[1].Graphics.Scale(1, 1.25);
                        doc.Pages[1].Graphics.FillText(0, 0, "(", normalFont, BlackColour, TextBaselines.Bottom);
                        doc.Pages[1].Graphics.Restore();


                        left += normalFont.MeasureText("(").Width + normalFont.MeasureTextAdvanced("(").RightSideBearing + 5;

                        doc.Pages[1].Graphics.FillText(left, 200, integralString.Substring(0, integralString.IndexOf("e")) + " \u00b7 10", normalFont, BlackColour, TextBaselines.Baseline);
                        left += normalFont.MeasureText(integralString.Substring(0, integralString.IndexOf("e")) + " \u00b7 10").Width + normalFont.MeasureTextAdvanced(integralString.Substring(0, integralString.IndexOf("e")) + "*10").RightSideBearing;
                        Font smallerFont = new Font(normalFont.FontFamily, 0.6 * normalFont.FontSize);
                        doc.Pages[1].Graphics.FillText(left, 185, integralString.Substring(integralString.IndexOf("e") + 1), smallerFont, BlackColour, TextBaselines.Baseline);
                        left += smallerFont.MeasureText(integralString.Substring(integralString.IndexOf("e") + 1)).Width + smallerFont.MeasureTextAdvanced(integralString.Substring(integralString.IndexOf("e") + 1)).RightSideBearing + 5;

                        doc.Pages[1].Graphics.Save();
                        doc.Pages[1].Graphics.Translate(left, 200 - normalFont.MeasureTextAdvanced(")").Bottom);
                        doc.Pages[1].Graphics.Scale(1, 1.25);
                        doc.Pages[1].Graphics.FillText(0, 0, ")", normalFont, BlackColour, TextBaselines.Bottom);
                        doc.Pages[1].Graphics.Restore();
                    }
                    else
                    {
                        doc.Pages[1].Graphics.FillText(left, 200, "(" + integralString + ")", normalFont, BlackColour, TextBaselines.Baseline);
                    }
                }

                return integral;
            }

        }


        //Beta kernel from Chen 1999
        public static double BetaKernelKDE(double x, double xi, double bandwidth)
        {
            if (x < 2 * bandwidth)
            {
                double rho = 2 * bandwidth * bandwidth + 2.5 - Math.Sqrt(4 * bandwidth * bandwidth * bandwidth * bandwidth + 6 * bandwidth * bandwidth + 2.25 - x * x - x / bandwidth);

                return Beta.PDF(rho, (1 - x) / bandwidth, xi);
            }
            else if (x <= 1 - 2 * bandwidth)
            {
                return Beta.PDF(x / bandwidth, (1 - x) / bandwidth, xi);
            }
            else
            {
                double rho = 2 * bandwidth * bandwidth + 2.5 - Math.Sqrt(4 * bandwidth * bandwidth * bandwidth * bandwidth + 6 * bandwidth * bandwidth + 2.25 - (1 - x) * (1 - x) - (1 - x) / bandwidth);

                return Beta.PDF(x / bandwidth, rho, xi);
            }
        }


        public static double GetBandwidth(double minX, double maxX, double range, double[] dValues, double xResolution)
        {
            double bandwidth = 0.000001;
            double lastBandwidth = 0;

            double lowerBound = double.NaN;
            double upperBound = double.NaN;



            while (Math.Abs((bandwidth - lastBandwidth) / bandwidth) >= 0.001)
            {
                List<double> currYs = new List<double>();

                for (double x = minX; x <= maxX - range * 0.025; x += (maxX - minX) / xResolution)
                {
                    double y = 0;
                    for (int j = 0; j < dValues.Length; j++)
                    {
                        y += BetaKernelKDE(x, dValues[j], bandwidth);
                    }

                    currYs.Add(y / dValues.Length);
                }

                int changesCounts = 0;
                double lastDerivSign = 0;

                for (int i = 1; i < currYs.Count - 1; i++)
                {

                    double deriv = (currYs[i] - currYs[i - 1]) / ((maxX - minX) / xResolution);

                    if (Math.Sign(deriv) != lastDerivSign && lastDerivSign != 0)
                    {
                        changesCounts++;
                    }
                    lastDerivSign = Math.Sign(deriv);
                }


                lastBandwidth = bandwidth;

                if (changesCounts <= 1)
                {
                    upperBound = double.IsNaN(upperBound) ? bandwidth : Math.Min(upperBound, bandwidth);
                }
                else
                {
                    lowerBound = double.IsNaN(lowerBound) ? bandwidth : Math.Max(lowerBound, bandwidth);
                }

                if (!double.IsNaN(lowerBound) && !double.IsNaN(upperBound))
                {
                    bandwidth = (lowerBound + upperBound) * 0.5;
                }
                else if (!double.IsNaN(lowerBound))
                {
                    bandwidth = lowerBound * 10;
                }
                else
                {
                    bandwidth = upperBound * 0.1;
                }
            }


            return bandwidth;
        }

        public static void PlotBranchProbs(double[] branchProbs, double[] minBranchProbs, double[] maxBranchProbs, Plotting.Options options, string outputFile)
        {

            Document doc = new Document();
            Font pointFont = new Font(new FontFamily(options.FontFamily), 8);
            Font axisFont = new Font(new FontFamily(options.FontFamily), 16);
            Font titleFont = new Font(new FontFamily(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "OpenSans-Bold.ttf")), 60);

            double minY = Math.Floor(Math.Log10(minBranchProbs.Min()));

            double maxWidth = (from el in Utils.Range((int)minY, 0) select axisFont.MeasureText(Math.Pow(10, el).ToString(1, false)).Width).Max() + 10;

            double width = Math.Max(pointFont.MeasureText("100").Height * branchProbs.Length * 1.4, titleFont.MeasureText("Average branch probabilities").Width);
            double height = 1000;

            doc.Pages.Add(new Page(width + 200 + maxWidth, height + 200 + pointFont.MeasureText(new string('8', branchProbs.Length.ToString().Length)).Width + 5));
            Graphics gpr = doc.Pages.Last().Graphics;
            gpr.Translate(100 + maxWidth, 150);

            gpr.FillText(width / 2 - titleFont.MeasureText("Average branch probabilities").Width / 2, -50, "Average branch probabilities", titleFont, BlackColour, TextBaselines.Bottom);

            for (int i = (int)minY; i <= 0; i++)
            {
                for (int i2 = 0; i2 < 9; i2++)
                {
                    double val2 = Math.Pow(10, i) * (1 + i2);
                    double val = Math.Log10(val2);
                    double y = height * (1 + (val - minY) / minY);

                    if (i2 == 0)
                    {
                        gpr.FillText(-70 - axisFont.MeasureText(val2.ToString(1, false)).Width, y, val2.ToString(1, false), axisFont, BlackColour, TextBaselines.Middle);
                        gpr.StrokePath(new GraphicsPath().MoveTo(-50, y).LineTo(50 + width, y), Colour.FromRgb(220, 220, 220), lineWidth: 3);
                    }
                    else if (i != 0)
                    {
                        gpr.StrokePath(new GraphicsPath().MoveTo(-50, y).LineTo(50 + width, y), Colour.FromRgb(240, 240, 240), lineWidth: 3);
                    }
                }
            }

            GraphicsPath pth = new GraphicsPath();

            for (int i = 0; i < branchProbs.Length; i++)
            {
                double x = width * (branchProbs.Length - i - 1 + 0.5) / branchProbs.Length;
                double y = height * (1 + (Math.Log10(branchProbs[i]) - minY) / minY);

                pth.LineTo(x, y);
            }

            gpr.StrokePath(pth, Colour.FromRgb(200, 200, 200), lineWidth: 1);


            for (int i = 0; i < branchProbs.Length; i++)
            {
                double x = width * (branchProbs.Length - i - 1 + 0.5) / branchProbs.Length;
                double y = height * (1 + (Math.Log10(branchProbs[i]) - minY) / minY);

                double yMin = height * (1 + (Math.Log10(minBranchProbs[i]) - minY) / minY);
                double yMax = height * (1 + (Math.Log10(maxBranchProbs[i]) - minY) / minY);

                gpr.StrokePath(new GraphicsPath().MoveTo(x - 2.5, yMin).LineTo(x + 2.5, yMin).MoveTo(x, yMin).LineTo(x, yMax).MoveTo(x - 2.5, yMax).LineTo(x + 2.5, yMax), Colour.FromRgba(GetColor(i, 0.5, branchProbs.Length)), lineWidth: 1);
                gpr.FillPath(new GraphicsPath().Arc(x, y, 2.5, 0, 2 * Math.PI), Colour.FromRgba(GetColor(i, 1, branchProbs.Length)));

                gpr.Save();
                gpr.Translate(x, y);
                gpr.Rotate(Math.PI / 2);
                gpr.FillText(7.5, 0, (branchProbs.Length - i - 1).ToString(), pointFont, BlackColour, TextBaselines.Middle);
                gpr.Restore();
            }

            doc.SaveAsPDF(outputFile);
        }


        public static void PlotBranchProbsTree(double width, double height, TreeNode summaryTree, double[] branchProbs, double[] minBranchProbs, Plotting.Options options, string outputFile)
        {
            Document doc = new Document();
            doc.Pages.Add(new Page(width, height + 150));
            Font titleFont = new Font(new FontFamily(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "OpenSans-Bold.ttf")), 60);
            Graphics gpr = doc.Pages.Last().Graphics;
            gpr.FillText(width / 2 - titleFont.MeasureText("Average branch probabilities").Width / 2, 100, "Average branch probabilities", titleFont, BlackColour, TextBaselines.Baseline);

            gpr.Translate(0, 150);
            double minY = Math.Floor(Math.Log10(branchProbs.Min()));

            Action<List<TreeNode>, int, Graphics, double, double, double, double> BranchProb =
            (nodes, i, context, x, y, pX, pY) =>
            {
                int col = (int)Math.Min(1023, ((-(Math.Log10(branchProbs[branchProbs.Length - 1 - i]) - minY) / minY) * 1024));
                context.StrokePath(new GraphicsPath().MoveTo(x, y).LineTo(pX, y).LineTo(pX, pY), Colour.FromRgb(ViridisColorScale[col][0], ViridisColorScale[col][1], ViridisColorScale[col][2]), options.LineWidth * 3);
            };

            summaryTree.PlotTree(gpr, (float)width, (float)height, options, (float)width / 50F, Plotting.NodeNoAction, BranchProb, Plotting.ViridisLegend(summaryTree, (float)width / 50F, (float)width, (float)height, options, minY));

            doc.Pages.Add(new Page(width, height + 150));
            gpr = doc.Pages.Last().Graphics;

            gpr.FillText(width / 2 - titleFont.MeasureText("Minimum branch probabilities").Width / 2, 100, "Minimum branch probabilities", titleFont, BlackColour, TextBaselines.Baseline);

            gpr.Translate(0, 150);

            minY = Math.Floor(Math.Log10(minBranchProbs.Min()));

            BranchProb =
            (nodes, i, context, x, y, pX, pY) =>
            {
                int col = (int)Math.Min(1023, ((-(Math.Log10(minBranchProbs[minBranchProbs.Length - 1 - i]) - minY) / minY) * 1024));
                context.StrokePath(new GraphicsPath().MoveTo(x, y).LineTo(pX, y).LineTo(pX, pY), Colour.FromRgb(ViridisColorScale[col][0], ViridisColorScale[col][1], ViridisColorScale[col][2]), options.LineWidth * 3);
            };

            summaryTree.PlotTree(gpr, (float)width, (float)height, options, (float)width / 50F, Plotting.NodeNoAction, BranchProb, Plotting.ViridisLegend(summaryTree, (float)width / 50F, (float)width, (float)height, options, minY));

            doc.SaveAsPDF(outputFile);
        }


       
    }
}
