using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using SlimTreeNode;
using System;
using System.Collections.Generic;
using Utils;
using VectSharp;

namespace sMap_GUI
{

    public static class PlotUtils
    {
        static readonly Colour BlackColour = Colour.FromRgb(0, 0, 0);
        static readonly Colour TransparentWhite = Colour.FromRgba(255, 255, 255, 0);

        public static readonly Dictionary<string, Delegate> actions = new Dictionary<string, Delegate>();

        public static Action<List<TreeNode>, int, Graphics, double, double, double, double> BranchSimpleInteractive(Dictionary<string, Delegate> taggedActions, Plotting.Options options, Action<Path> enterAction, Action<Path> exitAction, Action<int> clickAction, Action<int, double, double, bool, Path> controlAction)
        {
            return (nodes, i, context, x, y, pX, pY) =>
                {
                    string guid1 = Guid.NewGuid().ToString();
                    string guid2 = Guid.NewGuid().ToString();

                    context.StrokePath(new GraphicsPath().MoveTo(x, y).LineTo(pX, y).LineTo(pX, pY), TransparentWhite, options.LineWidth * 4, tag: guid1);
                    context.StrokePath(new GraphicsPath().MoveTo(x, y).LineTo(pX, y).LineTo(pX, pY), BlackColour, options.LineWidth, tag: guid2);

                    PathFigure highlightFigure = new PathFigure() { StartPoint = new Avalonia.Point(pX, pY) };
                    highlightFigure.Segments.Add(new Avalonia.Media.LineSegment() { Point = new Avalonia.Point(pX, y) });

                    highlightFigure.Segments.Add(new Avalonia.Media.LineSegment() { Point = new Avalonia.Point(x, y) });

                    highlightFigure.IsClosed = false;

                    PathFigure circleFigure = new PathFigure() { StartPoint = new Avalonia.Point(x + options.PieSize, y) };
                    circleFigure.Segments.Add(new Avalonia.Media.ArcSegment() { Point = new Avalonia.Point(x - options.PieSize, y), RotationAngle = Math.PI, Size = new Avalonia.Size(options.PieSize, options.PieSize) });
                    circleFigure.Segments.Add(new Avalonia.Media.ArcSegment() { Point = new Avalonia.Point(x + options.PieSize, y), RotationAngle = Math.PI, Size = new Avalonia.Size(options.PieSize, options.PieSize) });
                    circleFigure.IsClosed = true;

                    PathGeometry geo = new PathGeometry();
                    geo.Figures.Add(highlightFigure);
                    geo.Figures.Add(circleFigure);

                    Path pth = new Path() { Data = geo, Stroke = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255)), StrokeThickness = 4, StrokeJoin = PenLineJoin.Round, ZIndex = -1, RenderTransform = new TranslateTransform(18, 10) };

                    pth.PointerEnter += (s, e) => { enterAction(pth); };
                    pth.PointerLeave += (s, e) => { exitAction(pth); };
                    pth.PointerPressed += (s, e) =>
                    {
                        if (e.GetCurrentPoint(pth).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
                        {
                            clickAction(i);
                        }
                    };

                    pth.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);

                    taggedActions.Add(guid1, new Action<Path>(ph =>
                    {
                        ph.ZIndex = -2;
                        ph.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);

                        ph.PointerEnter += (s, e) => { enterAction(pth); };
                        ph.PointerLeave += (s, e) => { exitAction(pth); };
                        ph.PointerPressed += (s, e) => { clickAction(i); };
                    }));

                    taggedActions.Add(guid2, new Action<Path>(ph =>
                    {
                        ph.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);

                        ((Canvas)ph.Parent).Children.Add(pth);
                        ph.PointerEnter += (s, e) => { enterAction(pth); };
                        ph.PointerLeave += (s, e) => { exitAction(pth); };
                        ph.PointerPressed += (s, e) => { clickAction(i); };

                        controlAction(i, (x + pX) * 0.5, y, pY > y, pth);
                    }));

                };
        }




        public static Action<List<TreeNode>, int, Graphics, double, double, double, double> BranchSMapInteractive(Dictionary<string, Delegate> taggedActions, double resolution, IEnumerable<TaggedHistory> histories, bool isClockLike, int[] treeSamples, LikelihoodModel[] likModels, LikelihoodModel meanLikModel, int[][] meanNodeCorresp, List<string> states, Plotting.Options options, List<(int r, int g, int b, double a)> stateColours, Action<Path> enterAction, Action<Path> exitAction, Action<int> clickAction, Action<int, double, double, bool, Path> controlAction)
        {
            return (nodes, i, context, x, y, pX, pY) =>
            {
                double[] sampleXs = new double[Math.Max(0, (int)Math.Ceiling((x - pX) / resolution) + 1)];
                double[][] stateXs = new double[sampleXs.Length][];

                for (int j = 0; j < sampleXs.Length; j++)
                {
                    sampleXs[j] = Math.Min(pX + j * resolution, x);
                    double sampleLength = (sampleXs[j] - pX) / (x - pX) * nodes[i].Length;

                    if (isClockLike)
                    {
                        stateXs[j] = Utils.Utils.GetBranchStateProbs(histories, treeSamples, likModels, meanLikModel, meanNodeCorresp, states, nodes.Count - 1 - i, nodes[i].Length - sampleLength, true);
                    }
                    else
                    {
                        stateXs[j] = Utils.Utils.GetBranchStateProbs(histories, treeSamples, likModels, meanLikModel, meanNodeCorresp, states, nodes.Count - 1 - i, sampleLength, false);
                    }

                }

                double[] usedProbs = new double[sampleXs.Length];


                PathFigure highlightFigure = new PathFigure() { StartPoint = new Avalonia.Point(pX, pY) };
                highlightFigure.Segments.Add(new Avalonia.Media.LineSegment() { Point = new Avalonia.Point(pX, y) });

                if (sampleXs.Length > 0)
                {
                    highlightFigure.Segments.Add(new Avalonia.Media.LineSegment() { Point = new Avalonia.Point(sampleXs[1], y - options.BranchSize) });
                    highlightFigure.Segments.Add(new Avalonia.Media.LineSegment() { Point = new Avalonia.Point(sampleXs[sampleXs.Length - 1], y - options.BranchSize) });
                    highlightFigure.Segments.Add(new Avalonia.Media.LineSegment() { Point = new Avalonia.Point(sampleXs[sampleXs.Length - 1], y + options.BranchSize) });
                    highlightFigure.Segments.Add(new Avalonia.Media.LineSegment() { Point = new Avalonia.Point(sampleXs[1], y + options.BranchSize) });
                    highlightFigure.Segments.Add(new Avalonia.Media.LineSegment() { Point = new Avalonia.Point(pX, y) });
                }
                else
                {
                    highlightFigure.Segments.Add(new Avalonia.Media.LineSegment() { Point = new Avalonia.Point(x, y) });
                }

                highlightFigure.IsClosed = false;

                PathFigure circleFigure = new PathFigure() { StartPoint = new Avalonia.Point(x + options.PieSize, y) };
                circleFigure.Segments.Add(new Avalonia.Media.ArcSegment() { Point = new Avalonia.Point(x - options.PieSize, y), RotationAngle = Math.PI, Size = new Avalonia.Size(options.PieSize, options.PieSize) });
                circleFigure.Segments.Add(new Avalonia.Media.ArcSegment() { Point = new Avalonia.Point(x + options.PieSize, y), RotationAngle = Math.PI, Size = new Avalonia.Size(options.PieSize, options.PieSize) });
                circleFigure.IsClosed = true;

                PathGeometry geo = new PathGeometry();
                geo.Figures.Add(highlightFigure);
                geo.Figures.Add(circleFigure);

                Path pth = new Path() { Data = geo, Stroke = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255)), StrokeThickness = 4, StrokeJoin = PenLineJoin.Round, ZIndex = -1, RenderTransform = new TranslateTransform(18, 10) };

                pth.PointerEnter += (s, e) => { enterAction(pth); };
                pth.PointerLeave += (s, e) => { exitAction(pth); };
                pth.PointerPressed += (s, e) =>
                {
                    if (e.GetCurrentPoint(pth).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
                    {
                        clickAction(i);
                    }
                };

                pth.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);

                if (sampleXs.Length > 0)
                {
                    string guid0 = Guid.NewGuid().ToString();
                    context.StrokePath(new GraphicsPath().MoveTo(pX, y).LineTo(pX, pY), BlackColour, options.LineWidth, tag: guid0);

                    taggedActions.Add(guid0, new Action<Path>(ph =>
                    {
                        ph.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);

                        ph.PointerEnter += (s, e) => { enterAction(pth); };
                        ph.PointerLeave += (s, e) => { exitAction(pth); };
                        ph.PointerPressed += (s, e) =>
                        {
                            if (e.GetCurrentPoint(ph).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
                            {
                                clickAction(i);
                            }
                        };
                    }));


                    for (int j = 0; j < states.Count; j++)
                    {
                        GraphicsPath pth2 = new GraphicsPath();

                        pth2.MoveTo(pX, y - options.LineWidth * 0.5 + usedProbs[0] * options.LineWidth);

                        for (int k = 1; k < sampleXs.Length; k++)
                        {
                            pth2.LineTo(sampleXs[k], y - options.BranchSize + usedProbs[k] * options.BranchSize * 2);
                        }

                        for (int k = sampleXs.Length - 1; k > 0; k--)
                        {
                            pth2.LineTo(sampleXs[k], y - options.BranchSize + (usedProbs[k] + stateXs[k][j]) * options.BranchSize * 2);
                            usedProbs[k] += stateXs[k][j];
                        }

                        pth2.LineTo(pX, y - options.LineWidth * 0.5 + (usedProbs[0] + stateXs[0][j]) * options.LineWidth);
                        usedProbs[0] += stateXs[0][j];

                        string guid = Guid.NewGuid().ToString();

                        context.FillPath(pth2, Colour.FromRgba(stateColours[j % stateColours.Count]), tag: guid);

                        if (j == 0)
                        {
                            taggedActions.Add(guid, new Action<Path>(ph =>
                            {
                                ((Canvas)ph.Parent).Children.Add(pth);
                                ph.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                                ph.PointerEnter += (s, e) => { enterAction(pth); };
                                ph.PointerLeave += (s, e) => { exitAction(pth); };
                                ph.PointerPressed += (s, e) =>
                                {
                                    if (e.GetCurrentPoint(ph).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
                                    {
                                        clickAction(i);
                                    }
                                };
                            }));
                        }
                        else
                        {
                            taggedActions.Add(guid, new Action<Path>(ph =>
                            {
                                ph.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                                ph.PointerEnter += (s, e) => { enterAction(pth); };
                                ph.PointerLeave += (s, e) => { exitAction(pth); };
                                ph.PointerPressed += (s, e) =>
                                {
                                    if (e.GetCurrentPoint(ph).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
                                    {
                                        clickAction(i);
                                    }
                                };
                            }));
                        }
                    }
                }
                else
                {
                    string guid = Guid.NewGuid().ToString();
                    context.StrokePath(new GraphicsPath().MoveTo(x, y).LineTo(pX, y).LineTo(pX, pY), BlackColour, options.LineWidth, tag: guid);

                    taggedActions.Add(guid, new Action<Path>(ph =>
                    {
                        ((Canvas)ph.Parent).Children.Add(pth);
                        ph.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                    }));
                }

                controlAction(i, (x + pX) * 0.5, y, pY > y, pth);
            };
        }

        public static Action<List<TreeNode>, int, Graphics, double, double> NodePie(Dictionary<string, Delegate> taggedActions, Plotting.Options options, double[][] stateProbs, List<(int, int, int, double)> stateColours, Action<Path> enterAction, Action<Path> exitAction, Action<int> clickAction, Path[] branchHighlights, Action<int, double, double, bool, Path> controlAction)
        {
            return (nodes, i, context, x, y) =>
            {



                double prevAngle = 0;

                for (int j = 0; j < stateProbs[nodes.Count - 1 - i].Length; j++)
                {
                    double finalAngle = prevAngle + stateProbs[nodes.Count - 1 - i][j] * 2 * Math.PI;

                    if (Math.Abs(prevAngle - finalAngle) > 0.0001)
                    {
                        string guid = Guid.NewGuid().ToString();
                        context.FillPath(new GraphicsPath().MoveTo(x, y).Arc(x, y, options.PieSize, prevAngle, finalAngle).LineTo(x, y), Colour.FromRgba(stateColours[j % stateColours.Count]), tag: guid);

                        taggedActions.Add(guid, new Action<Path>(ph =>
                        {
                            ph.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                            ph.PointerEnter += (s, e) => { enterAction(branchHighlights[i]); };
                            ph.PointerLeave += (s, e) => { exitAction(branchHighlights[i]); };
                            ph.PointerPressed += (s, e) => { clickAction(i); };
                        }));
                    }

                    prevAngle = finalAngle;
                }

                if (i == 0)
                {
                    PathFigure circleFigure = new PathFigure() { StartPoint = new Avalonia.Point(x + options.PieSize, y) };
                    circleFigure.Segments.Add(new Avalonia.Media.ArcSegment() { Point = new Avalonia.Point(x - options.PieSize, y), RotationAngle = Math.PI, Size = new Avalonia.Size(options.PieSize, options.PieSize) });
                    circleFigure.Segments.Add(new Avalonia.Media.ArcSegment() { Point = new Avalonia.Point(x + options.PieSize, y), RotationAngle = Math.PI, Size = new Avalonia.Size(options.PieSize, options.PieSize) });
                    circleFigure.IsClosed = true;

                    PathGeometry geo = new PathGeometry();
                    geo.Figures.Add(circleFigure);

                    Path pth = new Path() { Data = geo, Stroke = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255)), StrokeThickness = 4, StrokeJoin = PenLineJoin.Round, ZIndex = -1 };

                    pth.PointerEnter += (s, e) => { enterAction(pth); };
                    pth.PointerLeave += (s, e) => { exitAction(pth); };
                    pth.PointerPressed += (s, e) =>
                    {
                        if (e.GetCurrentPoint(pth).Properties.PointerUpdateKind == Avalonia.Input.PointerUpdateKind.LeftButtonPressed)
                        {
                            clickAction(i);
                        }
                    };

                    pth.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);

                    branchHighlights[0] = pth;

                    string guid = Guid.NewGuid().ToString();
                    context.StrokePath(new GraphicsPath().Arc(x, y, options.PieSize, 0, 2 * Math.PI), BlackColour, options.LineWidth, tag: guid);

                    taggedActions.Add(guid, new Action<Path>((ph) =>
                    {
                        controlAction(0, x, y - options.PieSize, true, pth);
                        ph.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                        ((Canvas)ph.Parent).Children.Add(pth);

                    }));
                }
                else
                {
                    string guid = Guid.NewGuid().ToString();
                    context.StrokePath(new GraphicsPath().Arc(x, y, options.PieSize, 0, 2 * Math.PI), BlackColour, options.LineWidth, tag: guid);

                    taggedActions.Add(guid, new Action<Path>((ph) =>
                    {
                        ph.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                    }));
                }
            };
        }
    }
}
