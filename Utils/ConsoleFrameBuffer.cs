using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Utils
{
    public class ConsoleColourLibrary
    {
        static bool initialised = false;

        static Dictionary<ComplexColour, double[]> colourLibrary = new Dictionary<ComplexColour, double[]>();

        static ConsoleColor[] availableColours = new ConsoleColor[] { ConsoleColor.Black, ConsoleColor.Gray, ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Blue, ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.Magenta };
        static byte[][] availableColoursRGB = new byte[][] { new byte[] { 0, 0, 0 }, new byte[] { 0xC0, 0xC0, 0xC0 }, new byte[] { 255, 0, 0 }, new byte[] { 0, 255, 0 }, new byte[] { 0, 0, 255 }, new byte[] { 255, 255, 0 }, new byte[] { 0, 255, 255 }, new byte[] { 255, 0, 255 } };

        public static void Initialise()
        {
            for (int i = 0; i < availableColours.Length; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    if (i == j)
                    {
                        Color col = Color.FromArgb(availableColoursRGB[i][0], availableColoursRGB[i][1], availableColoursRGB[i][2]);
                        colourLibrary.Add(new ComplexColour(availableColours[i], availableColours[j], '█'), new double[] { col.GetHue() / 360.0, col.GetSaturation(), col.GetBrightness() });
                    }
                    else
                    {
                        Color col1 = Color.FromArgb((int)(availableColoursRGB[i][0] * 0.25 + availableColoursRGB[j][0] * 0.75), (int)(availableColoursRGB[i][1] * 0.25 + availableColoursRGB[j][1] * 0.75), (int)(availableColoursRGB[i][2] * 0.25 + availableColoursRGB[j][2] * 0.75));
                        Color col2 = Color.FromArgb((int)(availableColoursRGB[i][0] * 0.5 + availableColoursRGB[j][0] * 0.5), (int)(availableColoursRGB[i][1] * 0.5 + availableColoursRGB[j][1] * 0.5), (int)(availableColoursRGB[i][2] * 0.5 + availableColoursRGB[j][2] * 0.5));
                        Color col3 = Color.FromArgb((int)(availableColoursRGB[i][0] * 0.75 + availableColoursRGB[j][0] * 0.25), (int)(availableColoursRGB[i][1] * 0.75 + availableColoursRGB[j][1] * 0.25), (int)(availableColoursRGB[i][2] * 0.75 + availableColoursRGB[j][2] * 0.25));

                        colourLibrary.Add(new ComplexColour(availableColours[i], availableColours[j], '░'), new double[] { col1.GetHue() / 360.0, col1.GetSaturation(), col1.GetBrightness() });

                        if ((availableColours[i] != ConsoleColor.Red && availableColours[i] != ConsoleColor.Green && availableColours[i] != ConsoleColor.Blue) || (availableColours[j] != ConsoleColor.Red && availableColours[j] != ConsoleColor.Green && availableColours[j] != ConsoleColor.Blue))
                        {
                            colourLibrary.Add(new ComplexColour(availableColours[i], availableColours[j], '▒'), new double[] { col2.GetHue() / 360.0, col2.GetSaturation(), col2.GetBrightness() });
                        }

                        colourLibrary.Add(new ComplexColour(availableColours[i], availableColours[j], '▓'), new double[] { col3.GetHue() / 360.0, col3.GetSaturation(), col3.GetBrightness() });
                    }
                }
            }

            initialised = true;
        }

        public static ComplexColour GetClosestColour(byte[] colour)
        {
            if (!initialised)
            {
                Initialise();
            }

            Color col = Color.FromArgb(colour[0], colour[1], colour[2]);

            double hue = col.GetHue() / 360.0;
            double sat = col.GetSaturation();
            double bri = col.GetBrightness();

            double minDist = double.MaxValue;
            ComplexColour tbr = new ComplexColour();

            foreach (KeyValuePair<ComplexColour, double[]> cl in colourLibrary)
            {
                double dist = (cl.Value[0] - hue) * (cl.Value[0] - hue) * 2 + (cl.Value[1] - sat) * (cl.Value[1] - sat) + (cl.Value[2] - bri) * (cl.Value[2] - bri);
                if (dist < minDist)
                {
                    tbr = cl.Key;
                    minDist = dist;
                }
            }

            return tbr;
        }

        public struct ComplexColour
        {
            public ConsoleColor Colour1 { get; }
            public ConsoleColor Colour2 { get; }
            public char Blend { get; }

            public ComplexColour(ConsoleColor colour1, ConsoleColor colour2, char blend)
            {
                Colour1 = colour1;
                Colour2 = colour2;
                Blend = blend;
            }
        }
    }

    public class ConsoleFrameBuffer
    {
        private char[,] frameBuffer;
        private ConsoleColor[,] bgColorBuffer;
        private ConsoleColor[,] fgColorBuffer;
        private int height;
        private int width;

        public ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;
        public ConsoleColor ForegroundColor { get; set; } = ConsoleColor.Gray;

        public int CursorLeft { get; set; }
        public int CursorTop { get; set; }

        public int WindowWidth { get { return width; } }
        public int WindowHeight { get { return height; } }

        public ConsoleFrameBuffer()
        {
            height = Console.WindowHeight;
            width = Console.WindowWidth;

            frameBuffer = new char[width, height];
            bgColorBuffer = new ConsoleColor[width, height];
            fgColorBuffer = new ConsoleColor[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    frameBuffer[x, y] = ' ';
                    bgColorBuffer[x, y] = ConsoleColor.Black;
                    fgColorBuffer[x, y] = ConsoleColor.Gray;
                }
            }
        }

        public void Write(string sr)
        {
            for (int i = 0; i < sr.Length; i++)
            {
                if (CursorLeft >= 0 && CursorLeft < width && CursorTop >= 0 && CursorTop < height)
                {
                    frameBuffer[CursorLeft, CursorTop] = sr[i];
                    bgColorBuffer[CursorLeft, CursorTop] = BackgroundColor;
                    fgColorBuffer[CursorLeft, CursorTop] = ForegroundColor;
                }

                CursorLeft++;

                if (CursorLeft >= width)
                {
                    CursorLeft = 0;
                    CursorTop++;
                }
            }
        }

        public void WriteLine(string sr = "")
        {
            for (int i = 0; i < sr.Length; i++)
            {
                if (CursorLeft >= 0 && CursorLeft < width && CursorTop >= 0 && CursorTop < height)
                {
                    frameBuffer[CursorLeft, CursorTop] = sr[i];
                    bgColorBuffer[CursorLeft, CursorTop] = BackgroundColor;
                    fgColorBuffer[CursorLeft, CursorTop] = ForegroundColor;
                }

                CursorLeft++;

                if (CursorLeft >= width)
                {
                    CursorLeft = 0;
                    CursorTop++;
                }
            }
            CursorLeft = 0;
            CursorTop++;
        }

        public void Flush()
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Clear();

            bool cursorXOk = false;

            for (int y = 0; y < height; y++)
            {
                Console.CursorTop = y;
                Console.CursorLeft = 0;
                for (int x = 0; x < width; x++)
                {


                    if (frameBuffer[x, y] != ' ')
                    {
                        if (!cursorXOk)
                        {
                            Console.CursorLeft = x;
                            cursorXOk = true;
                        }

                        Console.ForegroundColor = fgColorBuffer[x, y];
                        Console.BackgroundColor = bgColorBuffer[x, y];
                        string writableString = frameBuffer[x, y].ToString();

                        while (x < width - 1 && fgColorBuffer[x, y] == fgColorBuffer[x + 1, y] && bgColorBuffer[x, y] == bgColorBuffer[x + 1, y])
                        {
                            x++;
                            writableString += frameBuffer[x, y];
                        }

                        ConsoleWrapper.Write(writableString);
                    }
                    else
                    {
                        cursorXOk = false;
                    }
                }
            }

            Console.CursorLeft = 0;
            Console.CursorTop = 0;
        }

        public void Update(ConsoleFrameBuffer newScreen)
        {
            bool cursorXOk = false;

            for (int y = 0; y < height; y++)
            {
                Console.CursorTop = y;
                Console.CursorLeft = 0;

                for (int x = 0; x < width; x++)
                {
                    if (!cursorXOk)
                    {
                        Console.CursorLeft = x;
                        cursorXOk = true;
                    }

                    if (newScreen.frameBuffer[x, y] != frameBuffer[x, y] || newScreen.bgColorBuffer[x, y] != bgColorBuffer[x, y] || newScreen.fgColorBuffer[x, y] != fgColorBuffer[x, y])
                    {
                        Console.ForegroundColor = newScreen.fgColorBuffer[x, y];
                        Console.BackgroundColor = newScreen.bgColorBuffer[x, y];
                        string writableString = newScreen.frameBuffer[x, y].ToString();

                        while (x < width - 1 && newScreen.fgColorBuffer[x, y] == newScreen.fgColorBuffer[x + 1, y] && newScreen.bgColorBuffer[x, y] == newScreen.bgColorBuffer[x + 1, y])
                        {
                            x++;
                            writableString += newScreen.frameBuffer[x, y];
                        }

                        ConsoleWrapper.Write(writableString);
                    }
                    else
                    {
                        cursorXOk = false;
                    }
                }
            }

            this.frameBuffer = newScreen.frameBuffer;
            this.fgColorBuffer = newScreen.fgColorBuffer;
            this.bgColorBuffer = newScreen.bgColorBuffer;

            Console.CursorLeft = 0;
            Console.CursorTop = 0;
        }


        public void PlotColour(byte[] colour, int length)
        {
            ConsoleColourLibrary.ComplexColour col = ConsoleColourLibrary.GetClosestColour(colour);

            ConsoleColor prevFg = this.ForegroundColor;
            ConsoleColor prevBg = this.BackgroundColor;

            this.ForegroundColor = col.Colour1;
            this.BackgroundColor = col.Colour2;

            this.Write(new string(col.Blend, length));

            this.ForegroundColor = prevFg;
            this.BackgroundColor = prevBg;
        }
    }
}
