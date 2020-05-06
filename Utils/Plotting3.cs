using Accord.Statistics.Analysis;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VectSharp;
using VectSharp.PDF;

namespace Utils
{
    public static partial class Plotting
    {
        public static void PlotLikelihoodSamples(string fileName, IReadOnlyList<double[]> data, IReadOnlyList<double> values, Random randomSource)
        {
            Document doc = new Document();

            DrawLikelihoodValues(doc, data, values, randomSource, 5, "All data");

            double max = values.Max();

            int[] indices = (from el in Utils.Range(0, values.Count) where max - values[el] <= 5 select el).ToArray();

            values = (from el in indices select values[el]).ToList();
            data = (from el in indices select data[el]).ToList();

            DrawLikelihoodValues(doc, data, values, randomSource, 1, "Samples within 5 log-units of the MLE");

            indices = (from el in Utils.Range(0, values.Count) where max - values[el] <= 1 select el).ToArray();

            values = (from el in indices select values[el]).ToList();
            data = (from el in indices select data[el]).ToList();

            DrawLikelihoodValues(doc, data, values, randomSource, -1, "Samples within 1 log-unit of the MLE");

            doc.SaveAsPDF(fileName);
        }

        static void DrawLikelihoodValues(Document doc, IReadOnlyList<double[]> data, IReadOnlyList<double> values, Random randomSource, double highlightedRange, string subtitle)
        {
            int resolution = (int)Math.Round(Math.Sqrt(values.Count));

            PCA pca = null;

            if (data[0].Length > 2)
            {
                pca = PerformPCA(data, values, randomSource);
            }
            else if (data[0].Length == 2)
            {
                pca = new PCA()
                {
                    ExplainedVariance = new double[] { 0.5, 0.5 },
                    TransformedAxes = new double[][] { new double[] { 1, 0 }, new double[] { 0, 1 } },
                    TransformedData = data.ToArray()
                };
            }
            else
            {
                pca = new PCA()
                {
                    ExplainedVariance = new double[] { 1 },
                    TransformedAxes = new double[][] { new double[] { 1 } },
                    TransformedData = data.ToArray()
                };
            }

            double[][] transformedData = pca.TransformedData;

            double pageWidth = resolution * 1.3;

            Font titleFont = new Font(new FontFamily(FontFamily.StandardFontFamilies.HelveticaBold), resolution * 0.05);
            Font normalFont = new Font(new FontFamily(FontFamily.StandardFontFamilies.Helvetica), resolution * 0.025);

            if (data[0].Length == 1)
            {
                pageWidth = resolution * 1.05 + normalFont.MeasureText("-0000.000").Width;
            }

            Page pag = new Page(pageWidth + resolution * 0.1, resolution * 1.27);

            doc.Pages.Add(pag);

            Graphics gpr = pag.Graphics;

            gpr.Translate(resolution * 0.05, resolution * 0.05);

            gpr.FillText(pageWidth * 0.5 - titleFont.MeasureText("Likelihood landscape").Width * 0.5, resolution * 0.025, "Likelihood landscape", titleFont, Colour.FromRgb(0, 0, 0), TextBaselines.Middle);

            gpr.FillText(pageWidth * 0.5 - normalFont.MeasureText(subtitle).Width * 0.5, resolution * 0.07, subtitle, normalFont, Colour.FromRgb(0, 0, 0), TextBaselines.Middle);

            if (data[0].Length >= 2)
            {
                gpr.Translate(0, resolution * 0.1);
                Draw2DDistribution(gpr, transformedData, values, pca.TransformedAxes, resolution, highlightedRange);
            }
            else
            {
                gpr.Translate(normalFont.MeasureText("-0000.000").Width, resolution * 0.1);
                Draw1DDistribution(gpr, transformedData, values, pca.TransformedAxes, resolution, highlightedRange);
            }

            double currRowY = 0;

            if (data[0].Length > 2)
            {
                gpr.FillText(resolution * 1.05, currRowY, "Explained variance", normalFont, Colour.FromRgb(0, 0, 0));
                currRowY += resolution * 0.025 * 1.4;

                gpr.FillText(resolution * 1.05, currRowY, "of the variation:", normalFont, Colour.FromRgb(0, 0, 0));

                currRowY += resolution * 0.025 * 1.8;

                gpr.FillPath(new GraphicsPath().Arc(resolution * 1.06, currRowY + normalFont.MeasureText("PC1: " + pca.ExplainedVariance[0].ToString("0%")).Height * 0.5, resolution * 0.005, 0, 2 * Math.PI), Colour.FromRgb(0, 0, 0));

                gpr.FillText(resolution * 1.08, currRowY, "PC1: " + pca.ExplainedVariance[0].ToString("0%"), normalFont, Colour.FromRgb(0, 0, 0));

                Point arrow1EndPoint = new Point(resolution * 1.11 + normalFont.MeasureText("PC2: " + pca.ExplainedVariance[1].ToString("0%")).Width, currRowY + normalFont.MeasureText("PC2: " + pca.ExplainedVariance[1].ToString("0%")).Height * 0.5);
                Point arrow1StartPoint = new Point(resolution * 1.09 + normalFont.MeasureText("PC2: " + pca.ExplainedVariance[1].ToString("0%")).Width, currRowY + normalFont.MeasureText("PC2: " + pca.ExplainedVariance[1].ToString("0%")).Height * 0.5);

                DrawArrow(gpr, arrow1StartPoint, arrow1EndPoint, Colour.FromRgb(0, 0, 0), resolution * 0.0025, resolution * 0.005);

                currRowY += resolution * 0.025 * 1.4;

                gpr.FillPath(new GraphicsPath().Arc(resolution * 1.06, currRowY + normalFont.MeasureText("PC2: " + pca.ExplainedVariance[1].ToString("0%")).Height * 0.5, resolution * 0.005, 0, 2 * Math.PI), Colour.FromRgb(0, 0, 0));

                gpr.FillText(resolution * 1.08, currRowY, "PC2: " + pca.ExplainedVariance[1].ToString("0%"), normalFont, Colour.FromRgb(0, 0, 0));

                Point arrow2EndPoint = new Point(resolution * 1.1 + normalFont.MeasureText("PC2: " + pca.ExplainedVariance[1].ToString("0%")).Width, currRowY + normalFont.MeasureText("PC2: " + pca.ExplainedVariance[1].ToString("0%")).Height);
                Point arrow2StartPoint = new Point(resolution * 1.1 + normalFont.MeasureText("PC2: " + pca.ExplainedVariance[1].ToString("0%")).Width, currRowY);

                DrawArrow(gpr, arrow2StartPoint, arrow2EndPoint, Colour.FromRgb(0, 0, 0), resolution * 0.0025, resolution * 0.005);

                currRowY += resolution * 0.025 * 3;
            }

            if (data[0].Length >= 2)
            {
                gpr.FillText(resolution * 1.05, currRowY, "Parameters:", normalFont, Colour.FromRgb(0, 0, 0));

                currRowY += resolution * 0.025 * 1.4;

                gpr.Save();
                gpr.Translate(resolution * 1.05, currRowY);

                DrawAxes(gpr, pca.TransformedAxes, resolution);

                gpr.Restore();
            }
        }

        static void DrawAxes(Graphics gpr, double[][] axes, double resolution)
        {
            double maxLength = (from el in axes select new double[] { el[0], el[1] }.Modulus()).Max();

            for (int i = 0; i < axes.Length; i++)
            {
                Colour arrowColour = Colour.FromRgba(Plotting.GetColor(i, 1, axes.Length));

                Point endPoint = new Point(resolution * 0.125 + axes[i][0] / maxLength * resolution * 0.125, resolution * 0.125 + axes[i][1] / maxLength * resolution * 0.125);
                Point startPoint = new Point(resolution * 0.125, resolution * 0.125);

                DrawArrow(gpr, startPoint, endPoint, arrowColour, resolution * 0.0025, resolution * 0.005);
            }

            if (axes.Length <= 224)
            {
                gpr.Translate(0, resolution * 0.2675);

                Font axisFont = new Font(new FontFamily(FontFamily.StandardFontFamilies.Courier), resolution * (axes.Length <= 70 ? 0.025 : 0.0125));
                double axisBoxWidth = axisFont.MeasureText((axes.Length - 1).ToString()).Width + axisFont.FontSize * 0.8;
                double axisBoxHeight = axisFont.MeasureText((axes.Length - 1).ToString()).Height + axisFont.FontSize * 0.8;

                int countBoxesLine = (int)Math.Floor(resolution * 0.25 / axisBoxWidth);

                axisBoxWidth = resolution * 0.25 / countBoxesLine;
                axisBoxWidth = Math.Max(axisBoxWidth, resolution * 0.25 / axes.Length);

                for (int i = 0; i < axes.Length; i++)
                {
                    double y = (i / countBoxesLine) * axisBoxHeight;
                    double x = (i % countBoxesLine) * axisBoxWidth;
                    Colour colour = Colour.FromRgba(Plotting.GetColor(i, 1, axes.Length));

                    gpr.FillRectangle(x, y, axisBoxWidth, axisBoxHeight, colour);
                    gpr.FillText(x + axisBoxWidth * 0.5 - axisFont.MeasureText(i.ToString()).Width * 0.5, y + axisBoxHeight * 0.5, i.ToString(), axisFont, Colour.FromRgb(0, 0, 0), TextBaselines.Middle);
                }
            }

        }

        static void DrawArrow(Graphics gpr, Point startPoint, Point endPoint, Colour arrowColour, double lineWidth, double arrowSize)
        {
            double[] deltaVector = new double[] { endPoint.X - startPoint.X, endPoint.Y - startPoint.Y }.Normalize();

            double[] perpVector = new double[] { -deltaVector[1], deltaVector[0] };

            Point trianglePoint1 = new Point(endPoint.X + deltaVector[0] * arrowSize, endPoint.Y + deltaVector[1] * arrowSize);

            Point trianglePoint2 = new Point(endPoint.X - deltaVector[0] * arrowSize + perpVector[0] * arrowSize, endPoint.Y - deltaVector[1] * arrowSize + perpVector[1] * arrowSize);
            Point trianglePoint3 = new Point(endPoint.X - deltaVector[0] * arrowSize - perpVector[0] * arrowSize, endPoint.Y - deltaVector[1] * arrowSize - perpVector[1] * arrowSize);

            gpr.StrokePath(new GraphicsPath().MoveTo(startPoint).LineTo(endPoint), arrowColour, lineWidth);

            gpr.FillPath(new GraphicsPath().MoveTo(trianglePoint1).LineTo(trianglePoint2).LineTo(trianglePoint3), arrowColour);
        }

        class PCA
        {
            public double[][] TransformedData;
            public double[][] TransformedAxes;
            public double[] ExplainedVariance;
        }


        static PCA PerformPCA(IReadOnlyList<IReadOnlyList<double>> data, IReadOnlyList<double> values, Random randomSource, int gradientSampleCount = -1, double tol = 0.0001)
        {
            if (gradientSampleCount < 0)
            {
                gradientSampleCount = Math.Min(data.Count, 1000);
            }

            int n = data[0].Count;

            double[][] gradients = new double[gradientSampleCount][];

            double[] mins = new double[n];
            double[] maxs = new double[n];

            for (int j = 0; j < n; j++)
            {
                mins[j] = double.MaxValue;
                maxs[j] = double.MinValue;
            }

            for (int i = 0; i < data.Count; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    mins[j] = Math.Min(mins[j], data[i][j]);
                    maxs[j] = Math.Max(maxs[j], data[i][j]);
                }
            }

            double maxLength = maxs.Distance(mins);

            if (maxLength == 0)
            {
                double[][] trAxes = new double[n][];
                for (int i = 0; i < n; i++)
                {
                    trAxes[i] = new double[n];
                    trAxes[i][i] = 1;
                }

                return new PCA()
                {
                    ExplainedVariance = new double[n],
                    TransformedAxes = trAxes,
                    TransformedData = (from el in data select el.ToArray()).ToArray()
                };
            }

            int count = 0;

            while (count < gradientSampleCount)
            {
                int sample1 = randomSource.Next(0, data.Count);
                int sample2 = randomSource.Next(0, data.Count);

                double[] direction = (from el in Utils.Range(0, n) select data[sample2][el] - data[sample1][el]).ToArray();
                double length = direction.Modulus();

                while (sample2 == sample1 || length < tol || randomSource.NextDouble() < length / maxLength)
                {
                    sample1 = randomSource.Next(0, data.Count);
                    sample2 = randomSource.Next(0, data.Count);

                    direction = (from el in Utils.Range(0, n) select data[sample2][el] - data[sample1][el]).ToArray();
                    length = direction.Modulus();
                }

                direction = direction.Normalize(length);

                double diff = (values[sample2] - values[sample1]) / length;

                if (!double.IsNaN(diff))
                {
                    gradients[count] = direction.Multiply(diff);
                    count++;
                }
            }

            PrincipalComponentAnalysis pca = new PrincipalComponentAnalysis(PrincipalComponentMethod.Center, numberOfOutputs: 2);
            pca.Learn(gradients);

            Matrix<double> transformMatrix = Matrix<double>.Build.DenseOfRowArrays(pca.ComponentVectors);

            double[][] transformedData = new double[data.Count][];

            for (int i = 0; i < data.Count; i++)
            {
                transformedData[i] = transformMatrix.Multiply(Vector<double>.Build.DenseOfEnumerable(data[i])).ToArray();
            }

            double[][] axes = new double[n][];

            for (int i = 0; i < axes.Length; i++)
            {
                axes[i] = new double[n];
                axes[i][i] = 1;

                axes[i] = transformMatrix.Multiply(Vector<double>.Build.DenseOfArray(axes[i])).ToArray();
            }

            return new PCA()
            {
                TransformedData = transformedData,
                TransformedAxes = axes,
                ExplainedVariance = pca.ComponentProportions
            };
        }



        static void Draw1DDistribution(Graphics gpr, IReadOnlyList<IReadOnlyList<double>> data, IReadOnlyList<double> values, double[][] axes, int resolution, double highlightedRange)
        {
            double minX = double.MaxValue;
            double maxX = double.MinValue;

            for (int i = 0; i < data.Count; i++)
            {
                if (!double.IsNaN(data[i][0]) && !double.IsInfinity(data[i][0]) && !double.IsNaN(values[i]) && !double.IsInfinity(values[i]))
                {
                    minX = Math.Min(minX, data[i][0]);
                    maxX = Math.Max(maxX, data[i][0]);
                }
            }

            double maxValue = (from el in values where !double.IsNaN(el) && !double.IsInfinity(el) select el).Max();
            
            double minValue = (from el in values where !double.IsNaN(el) && !double.IsInfinity(el) select el).Min();

            double[] plotData = new double[resolution + 1];

            for (int x = 0; x <= resolution; x++)
            {
                plotData[x] = double.NaN;
            }

            double max = double.MinValue;
            double min = double.MaxValue;
            double maxValueX = double.MinValue;

            for (int i = 0; i < data.Count; i++)
            {
                if (!double.IsNaN(data[i][0]) && !double.IsInfinity(data[i][0]) && !double.IsNaN(values[i]) && !double.IsInfinity(values[i]))
                {
                    int index = (int)Math.Floor((data[i][0] - minX) / (maxX - minX) * resolution);

                    index = Math.Min(index, resolution);

                    if (values[i] == maxValue)
                    {
                        maxValueX = index;
                    }

                    if (double.IsNaN(plotData[index]))
                    {
                        plotData[index] = values[i];
                        max = Math.Max(max, plotData[index]);
                        min = Math.Min(min, plotData[index]);
                    }
                    else
                    {
                        plotData[index] = Math.Max(plotData[index], values[i]);
                        max = Math.Max(max, plotData[index]);
                        min = Math.Min(min, plotData[index]);
                    }
                }
            }


            double minHighlight = double.MaxValue;
            double maxHighlight = double.MinValue;

            List<List<double[]>> paths = new List<List<double[]>>();

            List<double[]> pth = new List<double[]>();
            paths.Add(pth);

            (int, double) lastValidPoint = (-1, resolution);

            List<List<double[]>> questionMarks = new List<List<double[]>>();

            for (int x = 0; x <= resolution; x++)
            {
                if (!double.IsNaN(plotData[x]))
                {
                    double y = resolution - (plotData[x] - min) / (max - min) * resolution;

                    if (lastValidPoint.Item1 + 1 != x)
                    {
                        questionMarks.Add(new List<double[]>()
                        {
                             new double[] { lastValidPoint.Item1, lastValidPoint.Item2 },
                             new double[] { (double)(lastValidPoint.Item1 + x) * 0.5, (lastValidPoint.Item2 + y) * 0.5 },
                             new double[] { x, y }
                        });
                        pth = new List<double[]>();
                        paths.Add(pth);
                    }

                    lastValidPoint = (x, y);
                    pth.Add(new double[] { x, y });

                    if (max - plotData[x] <= highlightedRange)
                    {
                        minHighlight = Math.Min(minHighlight, x);
                        maxHighlight = Math.Max(maxHighlight, x);
                    }
                }
            }

            Font questionMarkFont = new Font(new FontFamily(FontFamily.StandardFontFamilies.Helvetica), resolution * 0.05);
            Font normalFont = new Font(new FontFamily(FontFamily.StandardFontFamilies.Helvetica), resolution * 0.03);

            gpr.FillText(-normalFont.MeasureText(maxValue.ToString("0.###")).Width - resolution * 0.02, 0, maxValue.ToString("0.###"), normalFont, Colour.FromRgb(0, 0, 0), TextBaselines.Middle);
            gpr.FillText(-normalFont.MeasureText((maxValue - 5).ToString("0.###")).Width - resolution * 0.02, highlightedRange / (maxValue - minValue) * resolution, (maxValue - 5).ToString("0.###"), normalFont, Colour.FromRgb(0, 0, 0), TextBaselines.Middle);
            gpr.FillText(-normalFont.MeasureText(minValue.ToString("0.###")).Width - resolution * 0.02, resolution, minValue.ToString("0.###"), normalFont, Colour.FromRgb(0, 0, 0), TextBaselines.Middle);

            gpr.FillText(0, resolution * 1.02, minX.ToString("0.###"), normalFont, Colour.FromRgb(0, 0, 0));
            gpr.FillText(resolution - normalFont.MeasureText(maxX.ToString("0.###")).Width, resolution * 1.02, maxX.ToString("0.###"), normalFont, Colour.FromRgb(0, 0, 0));

            DrawArrow(gpr, new Point(-resolution * 0.0055, resolution * 1.008), new Point(-resolution * 0.0055, -resolution * 0.02), Colour.FromRgb(0, 0, 0), resolution * 0.005, resolution * 0.008);
            DrawArrow(gpr, new Point(-resolution * 0.0055, resolution * 1.0055), new Point(resolution + resolution * 0.02, resolution * 1.0055), Colour.FromRgb(0, 0, 0), resolution * 0.005, resolution * 0.008);

            if (highlightedRange > 0 && maxValue - minValue > 0)
            {
                double height = highlightedRange / (maxValue - minValue) * resolution;

                gpr.FillRectangle(minHighlight - resolution * 0.0055, -resolution * 0.0055, maxHighlight - minHighlight + resolution * 0.011, height + resolution * 0.011, Colour.FromRgba(0, 0, 0, 10));
                gpr.StrokeRectangle(minHighlight - resolution * 0.0055, -resolution * 0.0055, maxHighlight - minHighlight + resolution * 0.011, height + resolution * 0.011, Colour.FromRgb(237, 28, 36), resolution * 0.003);
            }

            for (int i = 0; i < questionMarks.Count; i++)
            {
                gpr.StrokePath(new GraphicsPath().MoveTo(questionMarks[i][0][0], questionMarks[i][0][1]).LineTo(questionMarks[i][2][0], questionMarks[i][2][1]), Colour.FromRgb(200, 200, 200), resolution * 0.005, LineCaps.Round, LineJoins.Round);
            }

            for (int i = 0; i < paths.Count; i++)
            {
                GraphicsPath path = new GraphicsPath();
                for (int j = 0; j < paths[i].Count; j++)
                {
                    if (j == 0)
                    {
                        path.MoveTo(paths[i][j][0], paths[i][j][1]);
                    }
                    path.LineTo(paths[i][j][0], paths[i][j][1]);
                }
                gpr.StrokePath(path, Colour.FromRgb(Plotting.ViridisColorScale[512][0], Plotting.ViridisColorScale[512][1], Plotting.ViridisColorScale[512][2]), resolution * 0.005, LineCaps.Round, LineJoins.Round);
            }

            Size questionMarkSize = questionMarkFont.MeasureText("?");

            for (int i = 0; i < questionMarks.Count; i++)
            {
                double rappX = (questionMarks[i][2][0] - questionMarks[i][0][0] - resolution * 0.005) / questionMarkSize.Width;
                double rappY = (questionMarks[i][2][1] - questionMarks[i][0][1] - resolution * 0.005) / questionMarkSize.Height;
                double rapp = Math.Max(0, Math.Min(1, Math.Max(rappX, rappY)));

                gpr.Save();
                gpr.Translate(questionMarks[i][1][0], questionMarks[i][1][1]);
                gpr.Scale(rapp, rapp);

                gpr.StrokePath(new GraphicsPath().AddText(-questionMarkSize.Width * 0.5, 0, "?", questionMarkFont, TextBaselines.Middle), Colour.FromRgb(1.0, 1.0, 1.0), resolution * 0.005, LineCaps.Round, LineJoins.Round);
                gpr.FillText(-questionMarkSize.Width * 0.5, 0, "?", questionMarkFont, Colour.FromRgb(0, 0, 0), TextBaselines.Middle);
                gpr.Restore();
            }

            gpr.StrokePath(new GraphicsPath().Arc(maxValueX, 0, resolution * 0.01, 0, 2 * Math.PI).Close(), Colour.FromRgb(1.0, 1.0, 1.0), resolution * 0.005);
            gpr.StrokePath(new GraphicsPath().Arc(maxValueX, 0, resolution * 0.01, 0, 2 * Math.PI).Close(), Colour.FromRgb(237, 28, 36), resolution * 0.003);

            gpr.Save();

            gpr.Translate(resolution * 0.01, resolution * 1.07);

            gpr.StrokePath(new GraphicsPath().Arc(0, 0, resolution * 0.01, 0, 2 * Math.PI).Close(), Colour.FromRgb(1.0, 1.0, 1.0), resolution * 0.005);
            gpr.StrokePath(new GraphicsPath().Arc(0, 0, resolution * 0.01, 0, 2 * Math.PI).Close(), Colour.FromRgb(237, 28, 36), resolution * 0.003);

            gpr.FillText(resolution * 0.02, resolution * 0.01, "MLE", normalFont, Colour.FromRgb(0, 0, 0), TextBaselines.Baseline);

            gpr.Translate(resolution * 0.05 + normalFont.MeasureText("MLE").Width, 0);

            if (highlightedRange > 0)
            {
                gpr.StrokeRectangle(0, -resolution * 0.01, resolution * 0.02, resolution * 0.02, Colour.FromRgb(237, 28, 36), resolution * 0.003);
                gpr.FillRectangle(0, -resolution * 0.01, resolution * 0.02, resolution * 0.02, Colour.FromRgba(0, 0, 0, 10));
                
                if (highlightedRange > 1)
                {
                    gpr.FillText(resolution * 0.03, resolution * 0.01, "Samples within " + highlightedRange.ToString() + " log-units of the MLE", normalFont, Colour.FromRgb(0, 0, 0), TextBaselines.Baseline);
                }
                else
                {
                    gpr.FillText(resolution * 0.03, resolution * 0.01, "Samples within " + highlightedRange.ToString() + " log-unit of the MLE", normalFont, Colour.FromRgb(0, 0, 0), TextBaselines.Baseline);
                }
            }

            gpr.Restore();
        }

        static void Draw2DDistribution(Graphics gpr, IReadOnlyList<IReadOnlyList<double>> data, IReadOnlyList<double> values, double[][] axes, int resolution, double highlightedRange)
        {
            double min = double.MaxValue;
            double max = double.MinValue;

            double[,] plotData = new double[resolution, resolution];

            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    plotData[x, y] = double.NaN;
                }
            }

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            for (int i = 0; i < data.Count; i++)
            {
                minX = Math.Min(minX, data[i][0]);
                minY = Math.Min(minY, data[i][1]);

                maxX = Math.Max(maxX, data[i][0]);
                maxY = Math.Max(maxY, data[i][1]);
            }


            for (int i = 0; i < data.Count; i++)
            {
                int x = (int)Math.Floor((data[i][0] - minX) / (maxX - minX) * resolution);
                x = Math.Max(0, Math.Min(x, resolution - 1));

                int y = (int)Math.Floor((data[i][1] - minY) / (maxY - minY) * resolution);
                y = Math.Max(0, Math.Min(y, resolution - 1));

                if (!double.IsNaN(values[i]) && !double.IsInfinity(values[i]))
                {
                    if (double.IsNaN(plotData[x, y]))
                    {
                        plotData[x, y] = values[i];
                    }
                    else
                    {
                        plotData[x, y] = Math.Max(plotData[x, y], values[i]);
                    }
                }
            }

            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    if (!double.IsNaN(plotData[x, y]))
                    {
                        min = Math.Min(min, plotData[x, y]);
                        max = Math.Max(max, plotData[x, y]);
                    }
                }
            }

            gpr.FillRectangle(0, 0, resolution, resolution, Colour.FromRgb(230, 230, 230));

            int[] maxPoint = null;

            double[][] normalizedAxes = new double[axes.Length][];

            for (int i = 0; i < axes.Length; i++)
            {
                normalizedAxes[i] = new double[] { axes[i][0], axes[i][1] }.Normalize();
            }

            List<double[]> pointsWithin5Units = new List<double[]>();

            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    if (!double.IsNaN(plotData[x, y]) && !double.IsInfinity(plotData[x, y]))
                    {
                        int colInd = Math.Max(0, 1023 - (int)Math.Min(1023, Math.Floor((plotData[x, y] - min) / (max - min) * 1024)));

                        if (max - plotData[x, y] <= highlightedRange || highlightedRange < 0)
                        {
                            gpr.FillRectangle(x, y, 1, 1, Colour.FromRgb(Plotting.ViridisColorScale[colInd][0], Plotting.ViridisColorScale[colInd][1], Plotting.ViridisColorScale[colInd][2]));
                        }
                        else
                        {
                            gpr.FillRectangle(x, y, 1, 1, Colour.FromRgba(Plotting.ViridisColorScale[colInd][0], Plotting.ViridisColorScale[colInd][1], Plotting.ViridisColorScale[colInd][2], 180));
                        }

                        if (plotData[x, y] == max)
                        {
                            maxPoint = new int[] { x, y };
                        }

                        if (max - plotData[x, y] <= highlightedRange)
                        {
                            pointsWithin5Units.Add(new double[] { x + 0.5, y + 0.5 });
                        }
                    }
                }
            }

            if (pointsWithin5Units.Count > 1)
            {
                PrincipalComponentAnalysis pca = new PrincipalComponentAnalysis(PrincipalComponentMethod.Center);
                pca.Learn(pointsWithin5Units.ToArray());

                Matrix<double> transformMatrix = Matrix<double>.Build.DenseOfRowArrays(pca.ComponentVectors);

                double transfMinX = double.MaxValue;
                double transfMinY = double.MaxValue;
                double transfMaxX = double.MinValue;
                double transfMaxY = double.MinValue;

                for (int i = 0; i < pointsWithin5Units.Count; i++)
                {
                    double[] point = transformMatrix.Multiply(Vector<double>.Build.DenseOfEnumerable(pointsWithin5Units[i])).ToArray();

                    transfMinX = Math.Min(transfMinX, point[0]);
                    transfMaxX = Math.Max(transfMaxX, point[0]);
                    transfMinY = Math.Min(transfMinY, point[1]);
                    transfMaxY = Math.Max(transfMaxY, point[1]);
                }

                
                    double[] rect0Point = new double[] { transfMinX, transfMinY };
                    double[] rect1Point = new double[] { transfMaxX, transfMinY };
                    double[] rect2Point = new double[] { transfMaxX, transfMaxY };
                    double[] rect3Point = new double[] { transfMinX, transfMaxY };

                    rect0Point = transformMatrix.Inverse().Multiply(Vector<double>.Build.DenseOfArray(rect0Point)).ToArray();
                    rect1Point = transformMatrix.Inverse().Multiply(Vector<double>.Build.DenseOfArray(rect1Point)).ToArray();
                    rect2Point = transformMatrix.Inverse().Multiply(Vector<double>.Build.DenseOfArray(rect2Point)).ToArray();
                    rect3Point = transformMatrix.Inverse().Multiply(Vector<double>.Build.DenseOfArray(rect3Point)).ToArray();

                    double[] centroid = new double[]
                    {
                0.25 * (rect0Point[0] + rect1Point[0] + rect2Point[0] + rect3Point[0]),
                0.25 * (rect0Point[1] + rect1Point[1] + rect2Point[1] + rect3Point[1]),
                    };

                    double[] diff0 = new double[] { rect0Point[0] - centroid[0], rect0Point[1] - centroid[1] };
                    double[] diff1 = new double[] { rect1Point[0] - centroid[0], rect1Point[1] - centroid[1] };
                    double[] diff2 = new double[] { rect2Point[0] - centroid[0], rect2Point[1] - centroid[1] };
                    double[] diff3 = new double[] { rect3Point[0] - centroid[0], rect3Point[1] - centroid[1] };

                    double[] widthAxis = new double[] { rect0Point[0] - rect1Point[0], rect0Point[1] - rect1Point[1] }.Normalize();
                    double[] heightAxis = new double[] { rect1Point[0] - rect2Point[0], rect1Point[1] - rect2Point[1] }.Normalize();

                

                    rect0Point = new double[]
                    {
                centroid[0] + diff0[0] + (widthAxis[1] - heightAxis[1]) / (widthAxis[1] * heightAxis[0] - widthAxis[0] * heightAxis[1]),
                centroid[1] + diff0[1] + (heightAxis[0] - widthAxis[0]) / (widthAxis[1] * heightAxis[0] - widthAxis[0] * heightAxis[1])
                    };

                    widthAxis = widthAxis.Multiply(-1);

                    rect1Point = new double[]
                    {
                centroid[0] + diff1[0] + (widthAxis[1] - heightAxis[1]) / (widthAxis[1] * heightAxis[0] - widthAxis[0] * heightAxis[1]),
                centroid[1] + diff1[1] + (heightAxis[0] - widthAxis[0]) / (widthAxis[1] * heightAxis[0] - widthAxis[0] * heightAxis[1])
                    };

                    heightAxis = heightAxis.Multiply(-1);

                    rect2Point = new double[]
                    {
                centroid[0] + diff2[0] + (widthAxis[1] - heightAxis[1]) / (widthAxis[1] * heightAxis[0] - widthAxis[0] * heightAxis[1]),
                centroid[1] + diff2[1] + (heightAxis[0] - widthAxis[0]) / (widthAxis[1] * heightAxis[0] - widthAxis[0] * heightAxis[1])
                    };

                    widthAxis = widthAxis.Multiply(-1);

                    rect3Point = new double[]
                    {
                centroid[0] + diff3[0] + (widthAxis[1] - heightAxis[1]) / (widthAxis[1] * heightAxis[0] - widthAxis[0] * heightAxis[1]),
                centroid[1] + diff3[1] + (heightAxis[0] - widthAxis[0]) / (widthAxis[1] * heightAxis[0] - widthAxis[0] * heightAxis[1])
                    };

                if (!double.IsNaN(rect0Point[0]) && !double.IsNaN(rect0Point[1]) && !double.IsInfinity(rect0Point[0]) && !double.IsInfinity(rect0Point[1]) &&
                    !double.IsNaN(rect1Point[0]) && !double.IsNaN(rect1Point[1]) && !double.IsInfinity(rect1Point[0]) && !double.IsInfinity(rect1Point[1]) &&
                    !double.IsNaN(rect2Point[0]) && !double.IsNaN(rect2Point[1]) && !double.IsInfinity(rect2Point[0]) && !double.IsInfinity(rect2Point[1]) &&
                    !double.IsNaN(rect3Point[0]) && !double.IsNaN(rect3Point[1]) && !double.IsInfinity(rect3Point[0]) && !double.IsInfinity(rect3Point[1]))
                {
                    GraphicsPath pth = new GraphicsPath();
                    pth.MoveTo(rect0Point[0], rect0Point[1]);
                    pth.LineTo(rect1Point[0], rect1Point[1]);
                    pth.LineTo(rect2Point[0], rect2Point[1]);
                    pth.LineTo(rect3Point[0], rect3Point[1]);
                    pth.Close();

                    gpr.StrokePath(pth, Colour.FromRgb(237, 28, 36), resolution * 0.003, LineCaps.Round, LineJoins.Round);
                }
            }

            gpr.StrokePath(new GraphicsPath().Arc(maxPoint[0] + 0.5, maxPoint[1] + 0.5, resolution * 0.01, 0, 2 * Math.PI).Close(), Colour.FromRgb(1.0, 1.0, 1.0), resolution * 0.005);
            gpr.StrokePath(new GraphicsPath().Arc(maxPoint[0] + 0.5, maxPoint[1] + 0.5, resolution * 0.01, 0, 2 * Math.PI).Close(), Colour.FromRgb(237, 28, 36), resolution * 0.003);

            gpr.Save();

            gpr.Translate(resolution * 0.01, resolution * 1.04);

            Font normalFont = new Font(new FontFamily(FontFamily.StandardFontFamilies.Helvetica), resolution * 0.025);
            Font smallerFont = new Font(new FontFamily(FontFamily.StandardFontFamilies.Helvetica), resolution * 0.0125);

            gpr.StrokePath(new GraphicsPath().Arc(0, 0, resolution * 0.01, 0, 2 * Math.PI).Close(), Colour.FromRgb(1.0, 1.0, 1.0), resolution * 0.005);
            gpr.StrokePath(new GraphicsPath().Arc(0, 0, resolution * 0.01, 0, 2 * Math.PI).Close(), Colour.FromRgb(237, 28, 36), resolution * 0.003);

            gpr.FillText(resolution * 0.02, resolution * 0.01, "MLE", normalFont, Colour.FromRgb(0, 0, 0), TextBaselines.Baseline);

            gpr.Translate(resolution * 0.05 + normalFont.MeasureText("MLE").Width, 0);

            if (highlightedRange > 0)
            {
                gpr.StrokeRectangle(0, -resolution * 0.01, resolution * 0.02, resolution * 0.02, Colour.FromRgb(237, 28, 36), resolution * 0.003);
                gpr.FillRectangle(resolution * 0.0075, -resolution * 0.0025, resolution * 0.005, resolution * 0.005, Colour.FromRgb(Plotting.ViridisColorScale[0][0], Plotting.ViridisColorScale[0][1], Plotting.ViridisColorScale[0][2]));
                if (highlightedRange > 1)
                {
                    gpr.FillText(resolution * 0.03, resolution * 0.01, "Samples within " + highlightedRange.ToString() + " log-units of the MLE", normalFont, Colour.FromRgb(0, 0, 0), TextBaselines.Baseline);
                }
                else
                {
                    gpr.FillText(resolution * 0.03, resolution * 0.01, "Samples within " + highlightedRange.ToString() + " log-unit of the MLE", normalFont, Colour.FromRgb(0, 0, 0), TextBaselines.Baseline);
                }
            }

            gpr.Translate(resolution * 0.05 + normalFont.MeasureText("Samples within " + (highlightedRange > 0 ? highlightedRange : 5).ToString() + " log-units of the MLE").Width, 0);

            double remainingWidth = resolution * 0.89 - normalFont.MeasureText("Samples within " + highlightedRange.ToString() + " log-units of the MLE").Width - normalFont.MeasureText("MLE").Width;

            for (int i = 0; i < 100; i++)
            {
                int index = (int)Math.Min(1023, Math.Floor((i / 99.0) * 1024));

                double value = max + (min - max) * i / 99;

                if (max - value <= highlightedRange || highlightedRange < 0)
                {
                    gpr.FillRectangle(i * remainingWidth / 100, -resolution * 0.01, remainingWidth / 100, resolution * 0.02, Colour.FromRgb(Plotting.ViridisColorScale[index][0], Plotting.ViridisColorScale[index][1], Plotting.ViridisColorScale[index][2]));
                }
                else
                {
                    gpr.FillRectangle(i * remainingWidth / 100, -resolution * 0.01, remainingWidth / 100, resolution * 0.02, Colour.FromRgba(Plotting.ViridisColorScale[index][0], Plotting.ViridisColorScale[index][1], Plotting.ViridisColorScale[index][2], 180));
                }
            }

            gpr.FillText(0, resolution * 0.015, max.ToString("0.###"), smallerFont, Colour.FromRgb(0, 0, 0));

            gpr.FillText(remainingWidth - smallerFont.MeasureText(min.ToString("0.###")).Width, resolution * 0.015, min.ToString("0.###"), smallerFont, Colour.FromRgb(0, 0, 0));

            if (highlightedRange > 0)
            {
                gpr.FillText(remainingWidth * (highlightedRange) / (max - min), -resolution * 0.015, (max - highlightedRange).ToString("0.###"), smallerFont, Colour.FromRgb(0, 0, 0), TextBaselines.Bottom);
            }

            gpr.Restore();
        }
    }
}
