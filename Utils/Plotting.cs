using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SlimTreeNode;
using MathNet.Numerics.Distributions;
using System.Drawing.Drawing2D;
using System.Threading;
using VectSharp;
using VectSharp.PDF;

namespace Utils
{
    public static partial class Plotting
    {
        static bool CustomPalette = false;

        static readonly Colour BlackColour = Colour.FromRgb(0, 0, 0);

        static readonly int[][] colors = { new int[] { 237, 28, 36 }, new int[] { 34, 177, 76 }, new int[] { 255, 127, 39 }, new int[] { 0, 162, 232 }, new int[] { 255, 242, 0 }, new int[] { 63, 72, 204 }, new int[] { 255, 201, 14 }, new int[] { 163, 73, 164 }, new int[] { 181, 230, 29 } };

        public static readonly int[][] ViridisColorScale = { new int[] { 72, 0, 84 }, new int[] { 72, 0, 85 }, new int[] { 72, 0, 85 }, new int[] { 72, 0, 86 }, new int[] { 73, 0, 86 }, new int[] { 73, 0, 86 }, new int[] { 73, 0, 87 }, new int[] { 73, 0, 87 }, new int[] { 73, 0, 87 }, new int[] { 73, 0, 88 }, new int[] { 73, 0, 88 }, new int[] { 73, 0, 89 }, new int[] { 74, 0, 89 }, new int[] { 74, 0, 89 }, new int[] { 74, 0, 90 }, new int[] { 74, 0, 90 }, new int[] { 74, 1, 90 }, new int[] { 74, 1, 91 }, new int[] { 74, 1, 91 }, new int[] { 74, 2, 92 }, new int[] { 74, 2, 92 }, new int[] { 75, 2, 92 }, new int[] { 75, 3, 93 }, new int[] { 75, 3, 93 }, new int[] { 75, 3, 93 }, new int[] { 75, 4, 94 }, new int[] { 75, 4, 94 }, new int[] { 75, 5, 94 }, new int[] { 75, 5, 95 }, new int[] { 75, 5, 95 }, new int[] { 75, 6, 95 }, new int[] { 76, 6, 96 }, new int[] { 76, 7, 96 }, new int[] { 76, 7, 97 }, new int[] { 76, 7, 97 }, new int[] { 76, 8, 97 }, new int[] { 76, 8, 98 }, new int[] { 76, 9, 98 }, new int[] { 76, 9, 98 }, new int[] { 76, 10, 99 }, new int[] { 76, 10, 99 }, new int[] { 77, 10, 99 }, new int[] { 77, 11, 100 }, new int[] { 77, 11, 100 }, new int[] { 77, 12, 100 }, new int[] { 77, 12, 101 }, new int[] { 77, 13, 101 }, new int[] { 77, 13, 101 }, new int[] { 77, 13, 102 }, new int[] { 77, 14, 102 }, new int[] { 77, 14, 102 }, new int[] { 77, 15, 103 }, new int[] { 77, 15, 103 }, new int[] { 77, 15, 103 }, new int[] { 78, 16, 104 }, new int[] { 78, 16, 104 }, new int[] { 78, 17, 104 }, new int[] { 78, 17, 105 }, new int[] { 78, 17, 105 }, new int[] { 78, 18, 105 }, new int[] { 78, 18, 106 }, new int[] { 78, 19, 106 }, new int[] { 78, 19, 106 }, new int[] { 78, 19, 106 }, new int[] { 78, 20, 107 }, new int[] { 78, 20, 107 }, new int[] { 78, 20, 107 }, new int[] { 78, 21, 108 }, new int[] { 78, 21, 108 }, new int[] { 79, 22, 108 }, new int[] { 79, 22, 109 }, new int[] { 79, 22, 109 }, new int[] { 79, 23, 109 }, new int[] { 79, 23, 110 }, new int[] { 79, 23, 110 }, new int[] { 79, 24, 110 }, new int[] { 79, 24, 110 }, new int[] { 79, 24, 111 }, new int[] { 79, 25, 111 }, new int[] { 79, 25, 111 }, new int[] { 79, 26, 112 }, new int[] { 79, 26, 112 }, new int[] { 79, 26, 112 }, new int[] { 79, 27, 112 }, new int[] { 79, 27, 113 }, new int[] { 79, 27, 113 }, new int[] { 79, 28, 113 }, new int[] { 79, 28, 114 }, new int[] { 79, 28, 114 }, new int[] { 79, 29, 114 }, new int[] { 79, 29, 114 }, new int[] { 79, 29, 115 }, new int[] { 79, 30, 115 }, new int[] { 80, 30, 115 }, new int[] { 80, 30, 115 }, new int[] { 80, 31, 116 }, new int[] { 80, 31, 116 }, new int[] { 80, 31, 116 }, new int[] { 80, 32, 117 }, new int[] { 80, 32, 117 }, new int[] { 80, 32, 117 }, new int[] { 80, 33, 117 }, new int[] { 80, 33, 118 }, new int[] { 80, 33, 118 }, new int[] { 80, 34, 118 }, new int[] { 80, 34, 118 }, new int[] { 80, 34, 119 }, new int[] { 80, 35, 119 }, new int[] { 80, 35, 119 }, new int[] { 80, 35, 119 }, new int[] { 80, 36, 120 }, new int[] { 80, 36, 120 }, new int[] { 80, 36, 120 }, new int[] { 80, 37, 120 }, new int[] { 80, 37, 120 }, new int[] { 80, 37, 121 }, new int[] { 80, 38, 121 }, new int[] { 80, 38, 121 }, new int[] { 80, 38, 121 }, new int[] { 80, 39, 122 }, new int[] { 80, 39, 122 }, new int[] { 80, 39, 122 }, new int[] { 80, 40, 122 }, new int[] { 80, 40, 123 }, new int[] { 80, 40, 123 }, new int[] { 80, 41, 123 }, new int[] { 80, 41, 123 }, new int[] { 80, 41, 123 }, new int[] { 80, 42, 124 }, new int[] { 80, 42, 124 }, new int[] { 80, 42, 124 }, new int[] { 80, 43, 124 }, new int[] { 80, 43, 124 }, new int[] { 80, 43, 125 }, new int[] { 80, 44, 125 }, new int[] { 80, 44, 125 }, new int[] { 80, 44, 125 }, new int[] { 80, 45, 125 }, new int[] { 80, 45, 126 }, new int[] { 80, 45, 126 }, new int[] { 79, 46, 126 }, new int[] { 79, 46, 126 }, new int[] { 79, 46, 126 }, new int[] { 79, 47, 127 }, new int[] { 79, 47, 127 }, new int[] { 79, 47, 127 }, new int[] { 79, 48, 127 }, new int[] { 79, 48, 127 }, new int[] { 79, 48, 128 }, new int[] { 79, 49, 128 }, new int[] { 79, 49, 128 }, new int[] { 79, 49, 128 }, new int[] { 79, 49, 128 }, new int[] { 79, 50, 128 }, new int[] { 79, 50, 129 }, new int[] { 79, 50, 129 }, new int[] { 79, 51, 129 }, new int[] { 79, 51, 129 }, new int[] { 79, 51, 129 }, new int[] { 79, 52, 129 }, new int[] { 79, 52, 130 }, new int[] { 79, 52, 130 }, new int[] { 79, 53, 130 }, new int[] { 79, 53, 130 }, new int[] { 79, 53, 130 }, new int[] { 78, 54, 130 }, new int[] { 78, 54, 131 }, new int[] { 78, 54, 131 }, new int[] { 78, 55, 131 }, new int[] { 78, 55, 131 }, new int[] { 78, 55, 131 }, new int[] { 78, 55, 131 }, new int[] { 78, 56, 132 }, new int[] { 78, 56, 132 }, new int[] { 78, 56, 132 }, new int[] { 78, 57, 132 }, new int[] { 78, 57, 132 }, new int[] { 78, 57, 132 }, new int[] { 78, 58, 132 }, new int[] { 78, 58, 133 }, new int[] { 78, 58, 133 }, new int[] { 78, 59, 133 }, new int[] { 77, 59, 133 }, new int[] { 77, 59, 133 }, new int[] { 77, 60, 133 }, new int[] { 77, 60, 133 }, new int[] { 77, 60, 133 }, new int[] { 77, 60, 134 }, new int[] { 77, 61, 134 }, new int[] { 77, 61, 134 }, new int[] { 77, 61, 134 }, new int[] { 77, 62, 134 }, new int[] { 77, 62, 134 }, new int[] { 77, 62, 134 }, new int[] { 77, 63, 134 }, new int[] { 77, 63, 135 }, new int[] { 76, 63, 135 }, new int[] { 76, 63, 135 }, new int[] { 76, 64, 135 }, new int[] { 76, 64, 135 }, new int[] { 76, 64, 135 }, new int[] { 76, 65, 135 }, new int[] { 76, 65, 135 }, new int[] { 76, 65, 135 }, new int[] { 76, 66, 135 }, new int[] { 76, 66, 136 }, new int[] { 76, 66, 136 }, new int[] { 76, 66, 136 }, new int[] { 76, 67, 136 }, new int[] { 75, 67, 136 }, new int[] { 75, 67, 136 }, new int[] { 75, 68, 136 }, new int[] { 75, 68, 136 }, new int[] { 75, 68, 136 }, new int[] { 75, 69, 136 }, new int[] { 75, 69, 137 }, new int[] { 75, 69, 137 }, new int[] { 75, 69, 137 }, new int[] { 75, 70, 137 }, new int[] { 75, 70, 137 }, new int[] { 74, 70, 137 }, new int[] { 74, 71, 137 }, new int[] { 74, 71, 137 }, new int[] { 74, 71, 137 }, new int[] { 74, 71, 137 }, new int[] { 74, 72, 137 }, new int[] { 74, 72, 137 }, new int[] { 74, 72, 138 }, new int[] { 74, 73, 138 }, new int[] { 74, 73, 138 }, new int[] { 74, 73, 138 }, new int[] { 73, 74, 138 }, new int[] { 73, 74, 138 }, new int[] { 73, 74, 138 }, new int[] { 73, 74, 138 }, new int[] { 73, 75, 138 }, new int[] { 73, 75, 138 }, new int[] { 73, 75, 138 }, new int[] { 73, 76, 138 }, new int[] { 73, 76, 138 }, new int[] { 73, 76, 139 }, new int[] { 72, 76, 139 }, new int[] { 72, 77, 139 }, new int[] { 72, 77, 139 }, new int[] { 72, 77, 139 }, new int[] { 72, 78, 139 }, new int[] { 72, 78, 139 }, new int[] { 72, 78, 139 }, new int[] { 72, 78, 139 }, new int[] { 72, 79, 139 }, new int[] { 72, 79, 139 }, new int[] { 71, 79, 139 }, new int[] { 71, 80, 139 }, new int[] { 71, 80, 139 }, new int[] { 71, 80, 139 }, new int[] { 71, 80, 139 }, new int[] { 71, 81, 139 }, new int[] { 71, 81, 140 }, new int[] { 71, 81, 140 }, new int[] { 71, 81, 140 }, new int[] { 71, 82, 140 }, new int[] { 70, 82, 140 }, new int[] { 70, 82, 140 }, new int[] { 70, 83, 140 }, new int[] { 70, 83, 140 }, new int[] { 70, 83, 140 }, new int[] { 70, 83, 140 }, new int[] { 70, 84, 140 }, new int[] { 70, 84, 140 }, new int[] { 70, 84, 140 }, new int[] { 70, 84, 140 }, new int[] { 69, 85, 140 }, new int[] { 69, 85, 140 }, new int[] { 69, 85, 140 }, new int[] { 69, 86, 140 }, new int[] { 69, 86, 140 }, new int[] { 69, 86, 140 }, new int[] { 69, 86, 140 }, new int[] { 69, 87, 140 }, new int[] { 69, 87, 141 }, new int[] { 68, 87, 141 }, new int[] { 68, 87, 141 }, new int[] { 68, 88, 141 }, new int[] { 68, 88, 141 }, new int[] { 68, 88, 141 }, new int[] { 68, 89, 141 }, new int[] { 68, 89, 141 }, new int[] { 68, 89, 141 }, new int[] { 68, 89, 141 }, new int[] { 68, 90, 141 }, new int[] { 67, 90, 141 }, new int[] { 67, 90, 141 }, new int[] { 67, 90, 141 }, new int[] { 67, 91, 141 }, new int[] { 67, 91, 141 }, new int[] { 67, 91, 141 }, new int[] { 67, 91, 141 }, new int[] { 67, 92, 141 }, new int[] { 67, 92, 141 }, new int[] { 66, 92, 141 }, new int[] { 66, 93, 141 }, new int[] { 66, 93, 141 }, new int[] { 66, 93, 141 }, new int[] { 66, 93, 141 }, new int[] { 66, 94, 141 }, new int[] { 66, 94, 141 }, new int[] { 66, 94, 141 }, new int[] { 66, 94, 141 }, new int[] { 65, 95, 141 }, new int[] { 65, 95, 141 }, new int[] { 65, 95, 141 }, new int[] { 65, 95, 141 }, new int[] { 65, 96, 142 }, new int[] { 65, 96, 142 }, new int[] { 65, 96, 142 }, new int[] { 65, 96, 142 }, new int[] { 65, 97, 142 }, new int[] { 65, 97, 142 }, new int[] { 64, 97, 142 }, new int[] { 64, 97, 142 }, new int[] { 64, 98, 142 }, new int[] { 64, 98, 142 }, new int[] { 64, 98, 142 }, new int[] { 64, 99, 142 }, new int[] { 64, 99, 142 }, new int[] { 64, 99, 142 }, new int[] { 64, 99, 142 }, new int[] { 63, 100, 142 }, new int[] { 63, 100, 142 }, new int[] { 63, 100, 142 }, new int[] { 63, 100, 142 }, new int[] { 63, 101, 142 }, new int[] { 63, 101, 142 }, new int[] { 63, 101, 142 }, new int[] { 63, 101, 142 }, new int[] { 63, 102, 142 }, new int[] { 63, 102, 142 }, new int[] { 62, 102, 142 }, new int[] { 62, 102, 142 }, new int[] { 62, 103, 142 }, new int[] { 62, 103, 142 }, new int[] { 62, 103, 142 }, new int[] { 62, 103, 142 }, new int[] { 62, 104, 142 }, new int[] { 62, 104, 142 }, new int[] { 62, 104, 142 }, new int[] { 61, 104, 142 }, new int[] { 61, 105, 142 }, new int[] { 61, 105, 142 }, new int[] { 61, 105, 142 }, new int[] { 61, 105, 142 }, new int[] { 61, 106, 142 }, new int[] { 61, 106, 142 }, new int[] { 61, 106, 142 }, new int[] { 61, 106, 142 }, new int[] { 61, 107, 142 }, new int[] { 60, 107, 142 }, new int[] { 60, 107, 142 }, new int[] { 60, 107, 142 }, new int[] { 60, 108, 142 }, new int[] { 60, 108, 142 }, new int[] { 60, 108, 142 }, new int[] { 60, 108, 142 }, new int[] { 60, 109, 142 }, new int[] { 60, 109, 142 }, new int[] { 60, 109, 142 }, new int[] { 59, 109, 142 }, new int[] { 59, 110, 142 }, new int[] { 59, 110, 142 }, new int[] { 59, 110, 142 }, new int[] { 59, 110, 142 }, new int[] { 59, 111, 142 }, new int[] { 59, 111, 142 }, new int[] { 59, 111, 142 }, new int[] { 59, 111, 142 }, new int[] { 58, 111, 142 }, new int[] { 58, 112, 142 }, new int[] { 58, 112, 142 }, new int[] { 58, 112, 142 }, new int[] { 58, 112, 142 }, new int[] { 58, 113, 142 }, new int[] { 58, 113, 142 }, new int[] { 58, 113, 142 }, new int[] { 58, 113, 142 }, new int[] { 58, 114, 142 }, new int[] { 57, 114, 142 }, new int[] { 57, 114, 142 }, new int[] { 57, 114, 142 }, new int[] { 57, 115, 142 }, new int[] { 57, 115, 142 }, new int[] { 57, 115, 142 }, new int[] { 57, 115, 142 }, new int[] { 57, 116, 142 }, new int[] { 57, 116, 142 }, new int[] { 57, 116, 142 }, new int[] { 56, 116, 142 }, new int[] { 56, 117, 142 }, new int[] { 56, 117, 142 }, new int[] { 56, 117, 142 }, new int[] { 56, 117, 142 }, new int[] { 56, 118, 142 }, new int[] { 56, 118, 142 }, new int[] { 56, 118, 142 }, new int[] { 56, 118, 142 }, new int[] { 55, 119, 142 }, new int[] { 55, 119, 142 }, new int[] { 55, 119, 142 }, new int[] { 55, 119, 142 }, new int[] { 55, 120, 142 }, new int[] { 55, 120, 142 }, new int[] { 55, 120, 142 }, new int[] { 55, 120, 142 }, new int[] { 55, 120, 142 }, new int[] { 55, 121, 142 }, new int[] { 54, 121, 142 }, new int[] { 54, 121, 142 }, new int[] { 54, 121, 142 }, new int[] { 54, 122, 142 }, new int[] { 54, 122, 142 }, new int[] { 54, 122, 142 }, new int[] { 54, 122, 142 }, new int[] { 54, 123, 142 }, new int[] { 54, 123, 142 }, new int[] { 53, 123, 142 }, new int[] { 53, 123, 142 }, new int[] { 53, 124, 142 }, new int[] { 53, 124, 142 }, new int[] { 53, 124, 142 }, new int[] { 53, 124, 142 }, new int[] { 53, 125, 142 }, new int[] { 53, 125, 142 }, new int[] { 53, 125, 142 }, new int[] { 52, 125, 142 }, new int[] { 52, 126, 142 }, new int[] { 52, 126, 142 }, new int[] { 52, 126, 142 }, new int[] { 52, 126, 142 }, new int[] { 52, 126, 142 }, new int[] { 52, 127, 142 }, new int[] { 52, 127, 142 }, new int[] { 52, 127, 142 }, new int[] { 51, 127, 142 }, new int[] { 51, 128, 142 }, new int[] { 51, 128, 142 }, new int[] { 51, 128, 142 }, new int[] { 51, 128, 142 }, new int[] { 51, 129, 142 }, new int[] { 51, 129, 142 }, new int[] { 51, 129, 142 }, new int[] { 51, 129, 142 }, new int[] { 50, 130, 142 }, new int[] { 50, 130, 142 }, new int[] { 50, 130, 142 }, new int[] { 50, 130, 142 }, new int[] { 50, 131, 142 }, new int[] { 50, 131, 142 }, new int[] { 50, 131, 142 }, new int[] { 50, 131, 142 }, new int[] { 50, 131, 142 }, new int[] { 49, 132, 142 }, new int[] { 49, 132, 142 }, new int[] { 49, 132, 142 }, new int[] { 49, 132, 142 }, new int[] { 49, 133, 142 }, new int[] { 49, 133, 142 }, new int[] { 49, 133, 142 }, new int[] { 49, 133, 142 }, new int[] { 48, 134, 142 }, new int[] { 48, 134, 142 }, new int[] { 48, 134, 142 }, new int[] { 48, 134, 142 }, new int[] { 48, 135, 142 }, new int[] { 48, 135, 142 }, new int[] { 48, 135, 142 }, new int[] { 48, 135, 142 }, new int[] { 47, 136, 142 }, new int[] { 47, 136, 142 }, new int[] { 47, 136, 142 }, new int[] { 47, 136, 142 }, new int[] { 47, 136, 141 }, new int[] { 47, 137, 141 }, new int[] { 47, 137, 141 }, new int[] { 47, 137, 141 }, new int[] { 46, 137, 141 }, new int[] { 46, 138, 141 }, new int[] { 46, 138, 141 }, new int[] { 46, 138, 141 }, new int[] { 46, 138, 141 }, new int[] { 46, 139, 141 }, new int[] { 46, 139, 141 }, new int[] { 46, 139, 141 }, new int[] { 45, 139, 141 }, new int[] { 45, 140, 141 }, new int[] { 45, 140, 141 }, new int[] { 45, 140, 141 }, new int[] { 45, 140, 141 }, new int[] { 45, 141, 141 }, new int[] { 45, 141, 141 }, new int[] { 45, 141, 141 }, new int[] { 44, 141, 141 }, new int[] { 44, 142, 141 }, new int[] { 44, 142, 141 }, new int[] { 44, 142, 141 }, new int[] { 44, 142, 141 }, new int[] { 44, 142, 141 }, new int[] { 44, 143, 141 }, new int[] { 43, 143, 141 }, new int[] { 43, 143, 141 }, new int[] { 43, 143, 140 }, new int[] { 43, 144, 140 }, new int[] { 43, 144, 140 }, new int[] { 43, 144, 140 }, new int[] { 43, 144, 140 }, new int[] { 43, 145, 140 }, new int[] { 42, 145, 140 }, new int[] { 42, 145, 140 }, new int[] { 42, 145, 140 }, new int[] { 42, 146, 140 }, new int[] { 42, 146, 140 }, new int[] { 42, 146, 140 }, new int[] { 42, 146, 140 }, new int[] { 41, 147, 140 }, new int[] { 41, 147, 140 }, new int[] { 41, 147, 140 }, new int[] { 41, 147, 140 }, new int[] { 41, 147, 140 }, new int[] { 41, 148, 140 }, new int[] { 41, 148, 140 }, new int[] { 41, 148, 139 }, new int[] { 40, 148, 139 }, new int[] { 40, 149, 139 }, new int[] { 40, 149, 139 }, new int[] { 40, 149, 139 }, new int[] { 40, 149, 139 }, new int[] { 40, 150, 139 }, new int[] { 40, 150, 139 }, new int[] { 39, 150, 139 }, new int[] { 39, 150, 139 }, new int[] { 39, 151, 139 }, new int[] { 39, 151, 139 }, new int[] { 39, 151, 139 }, new int[] { 39, 151, 139 }, new int[] { 39, 152, 139 }, new int[] { 38, 152, 139 }, new int[] { 38, 152, 138 }, new int[] { 38, 152, 138 }, new int[] { 38, 152, 138 }, new int[] { 38, 153, 138 }, new int[] { 38, 153, 138 }, new int[] { 38, 153, 138 }, new int[] { 38, 153, 138 }, new int[] { 37, 154, 138 }, new int[] { 37, 154, 138 }, new int[] { 37, 154, 138 }, new int[] { 37, 154, 138 }, new int[] { 37, 155, 138 }, new int[] { 37, 155, 138 }, new int[] { 37, 155, 137 }, new int[] { 37, 155, 137 }, new int[] { 36, 156, 137 }, new int[] { 36, 156, 137 }, new int[] { 36, 156, 137 }, new int[] { 36, 156, 137 }, new int[] { 36, 157, 137 }, new int[] { 36, 157, 137 }, new int[] { 36, 157, 137 }, new int[] { 36, 157, 137 }, new int[] { 35, 158, 137 }, new int[] { 35, 158, 137 }, new int[] { 35, 158, 136 }, new int[] { 35, 158, 136 }, new int[] { 35, 158, 136 }, new int[] { 35, 159, 136 }, new int[] { 35, 159, 136 }, new int[] { 35, 159, 136 }, new int[] { 35, 159, 136 }, new int[] { 34, 160, 136 }, new int[] { 34, 160, 136 }, new int[] { 34, 160, 136 }, new int[] { 34, 160, 136 }, new int[] { 34, 161, 135 }, new int[] { 34, 161, 135 }, new int[] { 34, 161, 135 }, new int[] { 34, 161, 135 }, new int[] { 34, 162, 135 }, new int[] { 34, 162, 135 }, new int[] { 33, 162, 135 }, new int[] { 33, 162, 135 }, new int[] { 33, 162, 135 }, new int[] { 33, 163, 134 }, new int[] { 33, 163, 134 }, new int[] { 33, 163, 134 }, new int[] { 33, 163, 134 }, new int[] { 33, 164, 134 }, new int[] { 33, 164, 134 }, new int[] { 33, 164, 134 }, new int[] { 33, 164, 134 }, new int[] { 33, 165, 134 }, new int[] { 33, 165, 133 }, new int[] { 33, 165, 133 }, new int[] { 33, 165, 133 }, new int[] { 32, 166, 133 }, new int[] { 32, 166, 133 }, new int[] { 32, 166, 133 }, new int[] { 32, 166, 133 }, new int[] { 32, 167, 133 }, new int[] { 32, 167, 132 }, new int[] { 32, 167, 132 }, new int[] { 32, 167, 132 }, new int[] { 32, 167, 132 }, new int[] { 32, 168, 132 }, new int[] { 32, 168, 132 }, new int[] { 32, 168, 132 }, new int[] { 32, 168, 132 }, new int[] { 32, 169, 131 }, new int[] { 32, 169, 131 }, new int[] { 32, 169, 131 }, new int[] { 32, 169, 131 }, new int[] { 32, 170, 131 }, new int[] { 32, 170, 131 }, new int[] { 32, 170, 131 }, new int[] { 32, 170, 130 }, new int[] { 32, 171, 130 }, new int[] { 32, 171, 130 }, new int[] { 32, 171, 130 }, new int[] { 32, 171, 130 }, new int[] { 32, 171, 130 }, new int[] { 32, 172, 130 }, new int[] { 33, 172, 129 }, new int[] { 33, 172, 129 }, new int[] { 33, 172, 129 }, new int[] { 33, 173, 129 }, new int[] { 33, 173, 129 }, new int[] { 33, 173, 129 }, new int[] { 33, 173, 128 }, new int[] { 33, 174, 128 }, new int[] { 33, 174, 128 }, new int[] { 33, 174, 128 }, new int[] { 33, 174, 128 }, new int[] { 33, 174, 128 }, new int[] { 34, 175, 128 }, new int[] { 34, 175, 127 }, new int[] { 34, 175, 127 }, new int[] { 34, 175, 127 }, new int[] { 34, 176, 127 }, new int[] { 34, 176, 127 }, new int[] { 34, 176, 127 }, new int[] { 34, 176, 126 }, new int[] { 35, 177, 126 }, new int[] { 35, 177, 126 }, new int[] { 35, 177, 126 }, new int[] { 35, 177, 126 }, new int[] { 35, 177, 125 }, new int[] { 35, 178, 125 }, new int[] { 36, 178, 125 }, new int[] { 36, 178, 125 }, new int[] { 36, 178, 125 }, new int[] { 36, 179, 125 }, new int[] { 36, 179, 124 }, new int[] { 37, 179, 124 }, new int[] { 37, 179, 124 }, new int[] { 37, 179, 124 }, new int[] { 37, 180, 124 }, new int[] { 37, 180, 123 }, new int[] { 38, 180, 123 }, new int[] { 38, 180, 123 }, new int[] { 38, 181, 123 }, new int[] { 38, 181, 123 }, new int[] { 39, 181, 122 }, new int[] { 39, 181, 122 }, new int[] { 39, 182, 122 }, new int[] { 39, 182, 122 }, new int[] { 40, 182, 122 }, new int[] { 40, 182, 121 }, new int[] { 40, 182, 121 }, new int[] { 40, 183, 121 }, new int[] { 41, 183, 121 }, new int[] { 41, 183, 121 }, new int[] { 41, 183, 120 }, new int[] { 42, 184, 120 }, new int[] { 42, 184, 120 }, new int[] { 42, 184, 120 }, new int[] { 42, 184, 120 }, new int[] { 43, 184, 119 }, new int[] { 43, 185, 119 }, new int[] { 43, 185, 119 }, new int[] { 44, 185, 119 }, new int[] { 44, 185, 118 }, new int[] { 44, 186, 118 }, new int[] { 45, 186, 118 }, new int[] { 45, 186, 118 }, new int[] { 45, 186, 118 }, new int[] { 46, 186, 117 }, new int[] { 46, 187, 117 }, new int[] { 46, 187, 117 }, new int[] { 47, 187, 117 }, new int[] { 47, 187, 116 }, new int[] { 47, 188, 116 }, new int[] { 48, 188, 116 }, new int[] { 48, 188, 116 }, new int[] { 49, 188, 116 }, new int[] { 49, 188, 115 }, new int[] { 49, 189, 115 }, new int[] { 50, 189, 115 }, new int[] { 50, 189, 115 }, new int[] { 50, 189, 114 }, new int[] { 51, 189, 114 }, new int[] { 51, 190, 114 }, new int[] { 52, 190, 114 }, new int[] { 52, 190, 113 }, new int[] { 52, 190, 113 }, new int[] { 53, 191, 113 }, new int[] { 53, 191, 113 }, new int[] { 54, 191, 112 }, new int[] { 54, 191, 112 }, new int[] { 55, 191, 112 }, new int[] { 55, 192, 112 }, new int[] { 55, 192, 111 }, new int[] { 56, 192, 111 }, new int[] { 56, 192, 111 }, new int[] { 57, 192, 111 }, new int[] { 57, 193, 110 }, new int[] { 58, 193, 110 }, new int[] { 58, 193, 110 }, new int[] { 58, 193, 109 }, new int[] { 59, 193, 109 }, new int[] { 59, 194, 109 }, new int[] { 60, 194, 109 }, new int[] { 60, 194, 108 }, new int[] { 61, 194, 108 }, new int[] { 61, 195, 108 }, new int[] { 62, 195, 108 }, new int[] { 62, 195, 107 }, new int[] { 62, 195, 107 }, new int[] { 63, 195, 107 }, new int[] { 63, 196, 106 }, new int[] { 64, 196, 106 }, new int[] { 64, 196, 106 }, new int[] { 65, 196, 106 }, new int[] { 65, 196, 105 }, new int[] { 66, 197, 105 }, new int[] { 66, 197, 105 }, new int[] { 67, 197, 104 }, new int[] { 67, 197, 104 }, new int[] { 68, 197, 104 }, new int[] { 68, 198, 104 }, new int[] { 69, 198, 103 }, new int[] { 69, 198, 103 }, new int[] { 70, 198, 103 }, new int[] { 70, 198, 102 }, new int[] { 71, 199, 102 }, new int[] { 71, 199, 102 }, new int[] { 72, 199, 101 }, new int[] { 72, 199, 101 }, new int[] { 73, 199, 101 }, new int[] { 73, 200, 101 }, new int[] { 74, 200, 100 }, new int[] { 74, 200, 100 }, new int[] { 75, 200, 100 }, new int[] { 75, 200, 99 }, new int[] { 76, 201, 99 }, new int[] { 76, 201, 99 }, new int[] { 77, 201, 98 }, new int[] { 77, 201, 98 }, new int[] { 78, 201, 98 }, new int[] { 79, 201, 97 }, new int[] { 79, 202, 97 }, new int[] { 80, 202, 97 }, new int[] { 80, 202, 96 }, new int[] { 81, 202, 96 }, new int[] { 81, 202, 96 }, new int[] { 82, 203, 95 }, new int[] { 82, 203, 95 }, new int[] { 83, 203, 95 }, new int[] { 83, 203, 94 }, new int[] { 84, 203, 94 }, new int[] { 85, 204, 94 }, new int[] { 85, 204, 93 }, new int[] { 86, 204, 93 }, new int[] { 86, 204, 93 }, new int[] { 87, 204, 92 }, new int[] { 87, 204, 92 }, new int[] { 88, 205, 92 }, new int[] { 88, 205, 91 }, new int[] { 89, 205, 91 }, new int[] { 90, 205, 91 }, new int[] { 90, 205, 90 }, new int[] { 91, 206, 90 }, new int[] { 91, 206, 90 }, new int[] { 92, 206, 89 }, new int[] { 92, 206, 89 }, new int[] { 93, 206, 89 }, new int[] { 94, 206, 88 }, new int[] { 94, 207, 88 }, new int[] { 95, 207, 88 }, new int[] { 95, 207, 87 }, new int[] { 96, 207, 87 }, new int[] { 96, 207, 86 }, new int[] { 97, 207, 86 }, new int[] { 98, 208, 86 }, new int[] { 98, 208, 85 }, new int[] { 99, 208, 85 }, new int[] { 99, 208, 85 }, new int[] { 100, 208, 84 }, new int[] { 101, 209, 84 }, new int[] { 101, 209, 83 }, new int[] { 102, 209, 83 }, new int[] { 102, 209, 83 }, new int[] { 103, 209, 82 }, new int[] { 104, 209, 82 }, new int[] { 104, 210, 82 }, new int[] { 105, 210, 81 }, new int[] { 105, 210, 81 }, new int[] { 106, 210, 80 }, new int[] { 107, 210, 80 }, new int[] { 107, 210, 80 }, new int[] { 108, 211, 79 }, new int[] { 108, 211, 79 }, new int[] { 109, 211, 78 }, new int[] { 110, 211, 78 }, new int[] { 110, 211, 78 }, new int[] { 111, 211, 77 }, new int[] { 111, 212, 77 }, new int[] { 112, 212, 76 }, new int[] { 113, 212, 76 }, new int[] { 113, 212, 76 }, new int[] { 114, 212, 75 }, new int[] { 115, 212, 75 }, new int[] { 115, 212, 74 }, new int[] { 116, 213, 74 }, new int[] { 116, 213, 74 }, new int[] { 117, 213, 73 }, new int[] { 118, 213, 73 }, new int[] { 118, 213, 72 }, new int[] { 119, 213, 72 }, new int[] { 120, 214, 72 }, new int[] { 120, 214, 71 }, new int[] { 121, 214, 71 }, new int[] { 122, 214, 70 }, new int[] { 122, 214, 70 }, new int[] { 123, 214, 69 }, new int[] { 123, 214, 69 }, new int[] { 124, 215, 69 }, new int[] { 125, 215, 68 }, new int[] { 125, 215, 68 }, new int[] { 126, 215, 67 }, new int[] { 127, 215, 67 }, new int[] { 127, 215, 66 }, new int[] { 128, 215, 66 }, new int[] { 129, 216, 66 }, new int[] { 129, 216, 65 }, new int[] { 130, 216, 65 }, new int[] { 130, 216, 64 }, new int[] { 131, 216, 64 }, new int[] { 132, 216, 63 }, new int[] { 132, 216, 63 }, new int[] { 133, 217, 62 }, new int[] { 134, 217, 62 }, new int[] { 134, 217, 62 }, new int[] { 135, 217, 61 }, new int[] { 136, 217, 61 }, new int[] { 136, 217, 60 }, new int[] { 137, 217, 60 }, new int[] { 138, 218, 59 }, new int[] { 138, 218, 59 }, new int[] { 139, 218, 58 }, new int[] { 140, 218, 58 }, new int[] { 140, 218, 57 }, new int[] { 141, 218, 57 }, new int[] { 142, 218, 57 }, new int[] { 142, 218, 56 }, new int[] { 143, 219, 56 }, new int[] { 144, 219, 55 }, new int[] { 144, 219, 55 }, new int[] { 145, 219, 54 }, new int[] { 146, 219, 54 }, new int[] { 146, 219, 53 }, new int[] { 147, 219, 53 }, new int[] { 148, 219, 52 }, new int[] { 148, 220, 52 }, new int[] { 149, 220, 51 }, new int[] { 150, 220, 51 }, new int[] { 150, 220, 50 }, new int[] { 151, 220, 50 }, new int[] { 152, 220, 49 }, new int[] { 152, 220, 49 }, new int[] { 153, 220, 48 }, new int[] { 154, 221, 48 }, new int[] { 154, 221, 47 }, new int[] { 155, 221, 47 }, new int[] { 156, 221, 46 }, new int[] { 156, 221, 46 }, new int[] { 157, 221, 46 }, new int[] { 158, 221, 45 }, new int[] { 158, 221, 45 }, new int[] { 159, 221, 44 }, new int[] { 160, 222, 44 }, new int[] { 160, 222, 43 }, new int[] { 161, 222, 43 }, new int[] { 162, 222, 42 }, new int[] { 162, 222, 41 }, new int[] { 163, 222, 41 }, new int[] { 164, 222, 40 }, new int[] { 164, 222, 40 }, new int[] { 165, 222, 39 }, new int[] { 166, 223, 39 }, new int[] { 166, 223, 38 }, new int[] { 167, 223, 38 }, new int[] { 168, 223, 37 }, new int[] { 168, 223, 37 }, new int[] { 169, 223, 36 }, new int[] { 170, 223, 36 }, new int[] { 171, 223, 35 }, new int[] { 171, 223, 35 }, new int[] { 172, 224, 34 }, new int[] { 173, 224, 34 }, new int[] { 173, 224, 33 }, new int[] { 174, 224, 33 }, new int[] { 175, 224, 32 }, new int[] { 175, 224, 32 }, new int[] { 176, 224, 31 }, new int[] { 177, 224, 31 }, new int[] { 177, 224, 30 }, new int[] { 178, 224, 30 }, new int[] { 179, 225, 29 }, new int[] { 179, 225, 29 }, new int[] { 180, 225, 28 }, new int[] { 181, 225, 27 }, new int[] { 181, 225, 27 }, new int[] { 182, 225, 26 }, new int[] { 183, 225, 26 }, new int[] { 183, 225, 25 }, new int[] { 184, 225, 25 }, new int[] { 185, 225, 24 }, new int[] { 185, 225, 24 }, new int[] { 186, 226, 23 }, new int[] { 187, 226, 23 }, new int[] { 187, 226, 22 }, new int[] { 188, 226, 22 }, new int[] { 189, 226, 21 }, new int[] { 190, 226, 21 }, new int[] { 190, 226, 20 }, new int[] { 191, 226, 20 }, new int[] { 192, 226, 19 }, new int[] { 192, 226, 19 }, new int[] { 193, 226, 18 }, new int[] { 194, 227, 18 }, new int[] { 194, 227, 17 }, new int[] { 195, 227, 17 }, new int[] { 196, 227, 16 }, new int[] { 196, 227, 16 }, new int[] { 197, 227, 15 }, new int[] { 198, 227, 15 }, new int[] { 198, 227, 15 }, new int[] { 199, 227, 14 }, new int[] { 200, 227, 14 }, new int[] { 200, 227, 13 }, new int[] { 201, 227, 13 }, new int[] { 202, 228, 12 }, new int[] { 202, 228, 12 }, new int[] { 203, 228, 12 }, new int[] { 204, 228, 11 }, new int[] { 204, 228, 11 }, new int[] { 205, 228, 11 }, new int[] { 206, 228, 10 }, new int[] { 206, 228, 10 }, new int[] { 207, 228, 10 }, new int[] { 208, 228, 10 }, new int[] { 208, 228, 9 }, new int[] { 209, 228, 9 }, new int[] { 210, 229, 9 }, new int[] { 210, 229, 9 }, new int[] { 211, 229, 9 }, new int[] { 212, 229, 9 }, new int[] { 212, 229, 9 }, new int[] { 213, 229, 9 }, new int[] { 213, 229, 9 }, new int[] { 214, 229, 9 }, new int[] { 215, 229, 9 }, new int[] { 215, 229, 9 }, new int[] { 216, 229, 9 }, new int[] { 217, 229, 9 }, new int[] { 217, 229, 9 }, new int[] { 218, 230, 9 }, new int[] { 219, 230, 9 }, new int[] { 219, 230, 10 }, new int[] { 220, 230, 10 }, new int[] { 221, 230, 10 }, new int[] { 221, 230, 11 }, new int[] { 222, 230, 11 }, new int[] { 223, 230, 11 }, new int[] { 223, 230, 12 }, new int[] { 224, 230, 12 }, new int[] { 224, 230, 12 }, new int[] { 225, 230, 13 }, new int[] { 226, 230, 13 }, new int[] { 226, 231, 14 }, new int[] { 227, 231, 14 }, new int[] { 228, 231, 14 }, new int[] { 228, 231, 15 }, new int[] { 229, 231, 15 }, new int[] { 229, 231, 16 }, new int[] { 230, 231, 16 }, new int[] { 231, 231, 17 }, new int[] { 231, 231, 17 }, new int[] { 232, 231, 18 }, new int[] { 233, 231, 19 }, new int[] { 233, 231, 19 }, new int[] { 234, 231, 20 }, new int[] { 234, 232, 20 }, new int[] { 235, 232, 21 }, new int[] { 236, 232, 21 }, new int[] { 236, 232, 22 }, new int[] { 237, 232, 22 }, new int[] { 238, 232, 23 }, new int[] { 238, 232, 24 }, new int[] { 239, 232, 24 }, new int[] { 239, 232, 25 }, new int[] { 240, 232, 25 }, new int[] { 241, 232, 26 }, new int[] { 241, 232, 26 }, new int[] { 242, 233, 27 }, new int[] { 242, 233, 28 }, new int[] { 243, 233, 28 } };
        public static (int r, int g, int b, double a) GetColor(int index, double alpha, int totalColors)
        {
            if (totalColors <= colors.Length)
            {
                return (colors[index][0], colors[index][1], colors[index][2], alpha);
            }
            else
            {
                int orderedIndex = index % 2 == 0 ? index / 2 : (int)(Math.Ceiling(totalColors * 0.5) + (index / 2));

                if (!CustomPalette)
                {
                    return new HSLColor(240.0 * orderedIndex / totalColors, 240, 120, alpha);
                }
                else
                {
                    if (totalColors > 1)
                    {
                        int totalIndex = orderedIndex * 1023 / (totalColors - 1);
                        return (ViridisColorScale[totalIndex][0], ViridisColorScale[totalIndex][1], ViridisColorScale[totalIndex][2], alpha);
                    }
                    else
                    {
                        return (ViridisColorScale[0][0], ViridisColorScale[0][1], ViridisColorScale[0][2], alpha);
                    }
                }
            }
        }

        static Plotting()
        {
            string[] paletteFiles = Directory.GetFiles(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "*.palette");

            if (paletteFiles.Length > 0)
            {
                string paletteFile = paletteFiles[0];

                ConsoleWrapper.WriteLine();

                if (paletteFiles.Length > 1)
                {
                    ConsoleWrapper.WriteLine("Multiple palette files detected!");
                }

                ConsoleWrapper.WriteLine("Loading custom colour palette from " + Path.GetFileName(paletteFile));

                ConsoleWrapper.WriteLine();

                CustomPalette = true;
                System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("#.*");

                try
                {
                    colors = (from el in File.ReadLines(paletteFile) let line = reg.Replace(el, "") where !string.IsNullOrWhiteSpace(line) select (from el2 in line.Split(",") select int.Parse(el2.Trim())).ToArray()).ToArray();
                }
                catch (Exception ex)
                {
                    ConsoleWrapper.WriteLine("Error while loading custom color palette: " + ex.Message);
                }
            }
        }


        public static Action<List<TreeNode>, int, Graphics, double, double> NodeNoAction = (a, b, c, d, e) => { };

        public static Action<List<TreeNode>, int, Graphics, double, double> NodePie(Options options, double[][] stateProbs, List<(int, int, int, double)> stateColours)
        {
            return (nodes, i, context, x, y) =>
            {
                double prevAngle = 0;

                for (int j = 0; j < stateProbs[nodes.Count - 1 - i].Length; j++)
                {
                    double finalAngle = prevAngle + stateProbs[nodes.Count - 1 - i][j] * 2 * Math.PI;

                    if (Math.Abs(prevAngle - finalAngle) > 0.0001)
                    {
                        context.FillPath(new GraphicsPath().MoveTo(x, y).Arc(x, y, options.PieSize, prevAngle, finalAngle).LineTo(x, y), Colour.FromRgba(stateColours[j % stateColours.Count]));
                    }

                    prevAngle = finalAngle;
                }

                context.StrokePath(new GraphicsPath().Arc(x, y, options.PieSize, 0, 2 * Math.PI), BlackColour, options.LineWidth);
            };
        }

        public static Action<List<TreeNode>, int, Graphics, double, double> NodeTarget(Options options, double[][] stateLiks, List<(int, int, int, double)> stateColours)
        {
            return (nodes, i, context, x, y) =>
            {
                double[] normalisedLiks = new double[stateLiks[nodes.Count - 1 - i].Length];

                double sumLik = Utils.LogSumExp(stateLiks[nodes.Count - 1 - i]);

                for (int j = 0; j < normalisedLiks.Length; j++)
                {
                    normalisedLiks[j] = Math.Exp(stateLiks[nodes.Count - 1 - i][j] - sumLik);
                }

                double prevRadius = 1;

                context.StrokePath(new GraphicsPath().Arc(x, y, options.PieSize, 0, 2 * Math.PI), BlackColour, options.LineWidth * 2);

                for (int j = 0; j < normalisedLiks.Length; j++)
                {
                    if (normalisedLiks[j] > 0.001)
                    {
                        context.FillPath(new GraphicsPath().MoveTo(x, y).Arc(x, y, options.PieSize * prevRadius, 0, 2 * Math.PI).LineTo(x, y), Colour.FromRgba(stateColours[j % stateColours.Count]));
                    }
                    prevRadius -= normalisedLiks[j];
                }
            };
        }

        public static Action<List<TreeNode>, int, Graphics, double, double, double, double> BranchSimple(double lineWidth) =>
            (nodes, i, context, x, y, pX, pY) =>
        {
            context.StrokePath(new GraphicsPath().MoveTo(x, y).LineTo(pX, y).LineTo(pX, pY), BlackColour, lineWidth);
        };


        public static Action<List<TreeNode>, int, Graphics, double, double, double, double> BranchSMap(double resolution, IEnumerable<TaggedHistory> histories, bool isClockLike, int[] treeSamples, LikelihoodModel[] likModels, LikelihoodModel meanLikModel, int[][] meanNodeCorresp, List<string> states, Options options, List<(int r, int g, int b, double a)> stateColours)
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
                        stateXs[j] = Utils.GetBranchStateProbs(histories, treeSamples, likModels, meanLikModel, meanNodeCorresp, states, nodes.Count - 1 - i, nodes[i].Length - sampleLength, true);
                    }
                    else
                    {
                        stateXs[j] = Utils.GetBranchStateProbs(histories, treeSamples, likModels, meanLikModel, meanNodeCorresp, states, nodes.Count - 1 - i, sampleLength, false);
                    }

                }

                double[] usedProbs = new double[sampleXs.Length];

                if (sampleXs.Length > 0)
                {
                    context.StrokePath(new GraphicsPath().MoveTo(pX, y).LineTo(pX, pY), BlackColour, options.LineWidth);
                    for (int j = 0; j < states.Count; j++)
                    {
                        GraphicsPath pth = new GraphicsPath();

                        pth.MoveTo(pX, y - options.LineWidth * 0.5 + usedProbs[0] * options.LineWidth);

                        for (int k = 1; k < sampleXs.Length; k++)
                        {
                            pth.LineTo(sampleXs[k], y - options.BranchSize + usedProbs[k] * options.BranchSize * 2);
                        }

                        for (int k = sampleXs.Length - 1; k > 0; k--)
                        {
                            pth.LineTo(sampleXs[k], y - options.BranchSize + (usedProbs[k] + stateXs[k][j]) * options.BranchSize * 2);
                            usedProbs[k] += stateXs[k][j];
                        }

                        pth.LineTo(pX, y - options.LineWidth * 0.5 + (usedProbs[0] + stateXs[0][j]) * options.LineWidth);
                        usedProbs[0] += stateXs[0][j];

                        context.FillPath(pth, Colour.FromRgba(stateColours[j % stateColours.Count]));
                    }
                }
                else
                {
                    context.StrokePath(new GraphicsPath().MoveTo(x, y).LineTo(pX, y).LineTo(pX, pY), BlackColour, options.LineWidth);
                }
            };
        }

        public static Action<List<TreeNode>, int, Graphics, double, double, double, double> BranchSampleSizes(double resolution, IEnumerable<TaggedHistory> histories, bool isClockLike, int[] treeSamples, LikelihoodModel[] likModels, LikelihoodModel meanLikModel, int[][] meanNodeCorresp, Options options)
        {
            List<double> sampleSizes = new List<double>();

            return (nodes, i, context, x, y, pX, pY) =>
            {
                double[] sampleXs = new double[Math.Max(0, (int)Math.Ceiling((x - pX) / resolution) + 1)];
                double[] sampleSizeXs = new double[sampleXs.Length];

                for (int j = 0; j < sampleXs.Length; j++)
                {
                    sampleXs[j] = Math.Min(pX + j * resolution, x);
                    double sampleLength = (sampleXs[j] - pX) / (x - pX) * nodes[i].Length;

                    if (isClockLike)
                    {
                        sampleSizeXs[j] = (double)Utils.GetBranchSampleSize(histories, treeSamples, likModels, meanLikModel, meanNodeCorresp, nodes.Count - 1 - i, nodes[i].Length - sampleLength, isClockLike) / (double)treeSamples.Length;
                    }
                    else
                    {
                        sampleSizeXs[j] = (double)Utils.GetBranchSampleSize(histories, treeSamples, likModels, meanLikModel, meanNodeCorresp, nodes.Count - 1 - i, sampleLength, isClockLike) / (double)treeSamples.Length;
                    }
                }

                sampleSizes.AddRange(sampleSizeXs);

                double[] usedProbs = new double[sampleXs.Length];

                if (sampleXs.Length > 0)
                {
                    context.StrokePath(new GraphicsPath().MoveTo(pX, y).LineTo(pX, pY), BlackColour, options.LineWidth);
                    GraphicsPath pth = new GraphicsPath();

                    pth.MoveTo(pX, y - options.LineWidth * 0.5 + usedProbs[0] * options.LineWidth);

                    for (int k = 1; k < sampleXs.Length; k++)
                    {
                        pth.LineTo(sampleXs[k], y - options.BranchSize + usedProbs[k] * options.BranchSize * 2);
                    }

                    for (int k = sampleXs.Length - 1; k > 0; k--)
                    {
                        pth.LineTo(sampleXs[k], y - options.BranchSize + (usedProbs[k] + sampleSizeXs[k]) * options.BranchSize * 2);
                        usedProbs[k] += sampleSizeXs[k];
                    }

                    pth.LineTo(pX, y - options.LineWidth * 0.5 + (usedProbs[0] + sampleSizeXs[0]) * options.LineWidth);
                    usedProbs[0] += sampleSizeXs[0];

                    context.FillPath(pth, Colour.FromRgba(GetColor(0, 1, 1)));
                }
                else
                {
                    context.StrokePath(new GraphicsPath().MoveTo(x, y).LineTo(pX, y).LineTo(pX, pY), BlackColour, options.LineWidth);
                }


            };
        }

        public static Func<Graphics, double[]> NoLegend = a => new double[] { 0, 0 };

        public static Func<Graphics, double[]> StandardLegend(TreeNode tree, float margin, float pageWidth, float pageHeight, Options options, string[] states, List<(int, int, int, double)> stateColours)
        {
            return (context) =>
            {
                Font baseFont = new Font(new FontFamily(options.FontFamily), options.FontSize);
                Font legendFont = new Font(new FontFamily(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "OpenSans-Bold.ttf")), options.FontSize);

                double width = pageWidth / 64 + context.MeasureText("Legend", legendFont).Width;

                for (int i = 0; i < states.Length; i++)
                {
                    width = Math.Max(width, pageWidth / 128 * 3 + options.PieSize * 3 + context.MeasureText(states[i], baseFont).Width);
                }

                StandardAgeAxis(margin, pageWidth, pageHeight, width + pageWidth / 32, options, tree)(context);

                double legendHeight = pageWidth / 64 + options.FontSize * 1.4 + Math.Max(2.8 * options.PieSize, options.FontSize * 1.4) * (states.Length - 0.4 / 1.4);
                double legendY = pageWidth / 128;

                context.FillRectangle(pageWidth / 64, pageWidth / 64, width, legendHeight, Colour.FromRgb(180, 180, 180));

                context.FillRectangle(pageWidth / 128, legendY, width, legendHeight, Colour.FromRgb(255, 255, 255));

                context.StrokeRectangle(pageWidth / 128, legendY, width, legendHeight, BlackColour, options.LineWidth * 0.75);

                context.FillText(pageWidth / 64, legendY + pageWidth / 128, "Legend", legendFont, BlackColour);

                double legendStartY = legendY + pageWidth / 128 + options.FontSize * 1.4F;

                for (int i = 0; i < states.Length; i++)
                {
                    context.FillPath(new GraphicsPath().Arc(pageWidth / 128 * 3 + options.PieSize, legendStartY + options.PieSize + Math.Max(2.8 * options.PieSize, options.FontSize * 1.4) * i, options.PieSize, 0, 2 * Math.PI), Colour.FromRgba(stateColours[i % stateColours.Count]));
                    context.StrokePath(new GraphicsPath().Arc(pageWidth / 128 * 3 + options.PieSize, legendStartY + options.PieSize + Math.Max(2.8 * options.PieSize, options.FontSize * 1.4) * i, options.PieSize, 0, 2 * Math.PI), BlackColour, options.LineWidth * 0.75);

                    context.FillText(pageWidth / 128 * 3 + options.PieSize * 3, legendStartY + options.PieSize + Math.Max(2.8 * options.PieSize, options.FontSize * 1.4) * i, states[i], baseFont, BlackColour, TextBaselines.Middle);
                }

                return new double[] { width + pageWidth / 32, 0 };
            };
        }

        public static Func<Graphics, double[]> StandardAgeAxis(double margin, double pageWidth, double pageHeight, double legendMargin, Options options, TreeNode tree)
        {
            return (context) =>
            {
                Font baseFont = new Font(new FontFamily(options.FontFamily), options.FontSize);

                if (options.ScaleAxis)
                {
                    List<string> leaves = tree.GetLeafNames();

                    double maxLabelWidth = 0;

                    for (int i = 0; i < leaves.Count; i++)
                    {
                        double labelW = context.MeasureText(leaves[i], baseFont).Width;
                        maxLabelWidth = Math.Max(maxLabelWidth, labelW);
                    }

                    maxLabelWidth += margin + options.PieSize + 10;

                    double len = tree.DownstreamLength();

                    context.Save();
                    context.Translate(legendMargin + options.PieSize, 0);
                    pageWidth -= legendMargin + options.PieSize;

                    if (options.ScaleGrid)
                    {
                        for (double age = 0; age <= len; age += options.GridSpacing)
                        {
                            context.StrokePath(new GraphicsPath().
                            MoveTo((pageWidth - margin * 2 - maxLabelWidth) * (1 - age / len), pageHeight - margin * 2 - options.LineWidth - options.FontSize * 2).
                            LineTo((pageWidth - margin * 2 - maxLabelWidth) * (1 - age / len), 0), Colour.FromRgba((options.GridColour[0], options.GridColour[1], options.GridColour[2], 1)), options.GridWidth);
                        }
                    }


                    context.StrokePath(new GraphicsPath().
                    MoveTo(0, pageHeight - margin * 2 - options.LineWidth - options.FontSize * 2).
                    LineTo(pageWidth - margin * 2 - maxLabelWidth, pageHeight - margin * 2 - options.LineWidth - options.FontSize * 2), BlackColour, options.LineWidth);

                    for (double age = 0; age <= len; age += options.ScaleSpacing)
                    {
                        context.StrokePath(new GraphicsPath().
                        MoveTo((pageWidth - margin * 2 - maxLabelWidth) * (1 - age / len), pageHeight - margin * 2 - options.LineWidth - options.FontSize * 2).
                        LineTo((pageWidth - margin * 2 - maxLabelWidth) * (1 - age / len), pageHeight - margin * 2 - options.LineWidth - options.FontSize * 1.5F), BlackColour, options.LineWidth);


                        if ((len - age) / age < 0.0001)
                        {
                            age = len;
                        }

                        context.FillText(Math.Min(Math.Max(0, (pageWidth - margin * 2 - maxLabelWidth) * (1 - age / len) - context.MeasureText((age * options.TreeScale).ToString(options.SignificantDigits, false), baseFont).Width * 0.5F), pageWidth - margin * 2 - context.MeasureText((age * options.TreeScale).ToString(options.SignificantDigits, false), baseFont).Width), pageHeight - margin * 2 - options.LineWidth - options.FontSize, (age * options.TreeScale).ToString(options.SignificantDigits, false), baseFont, BlackColour);

                    }

                    context.Restore();
                }

                return new double[] { 0, 0 };
            };
        }

        public static Func<Graphics, double[]> StandardLegendSize(TreeNode tree, float margin, float pageWidth, float pageHeight, Options options, string[] states)
        {
            return (context) =>
            {
                Font legendFont = new Font(new FontFamily(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "OpenSans-Bold.ttf")), options.FontSize);
                Font baseFont = new Font(new FontFamily(options.FontFamily), options.FontSize);

                double width = pageWidth / 64 + context.MeasureText("Legend", legendFont).Width;

                for (int i = 0; i < states.Length; i++)
                {
                    width = Math.Max(width, pageWidth / 128 * 3 + options.PieSize * 3 + context.MeasureText(states[i], baseFont).Width);
                }

                StandardAgeAxis(margin, pageWidth, pageHeight, width + pageWidth / 32, options, tree)(context);

                return new double[] { width + pageWidth / 32, 0 };
            };
        }

        public static Func<Graphics, double[]> ViridisLegend(TreeNode tree, float margin, float pageWidth, float pageHeight, Options options, double min)
        {
            return (context) =>
            {
                Font legendFont = new Font(new FontFamily(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "OpenSans-Bold.ttf")), options.FontSize);
                Font baseFont = new Font(new FontFamily(options.FontFamily), options.FontSize);

                double width = pageWidth / 64 + context.MeasureText("Legend", legendFont).Width;

                width = Math.Max(width, pageWidth / 32 + 36 + baseFont.MeasureText(Math.Pow(10, min).ToString(1, false)).Width);

                double legendHeight = pageWidth / 64 + options.FontSize * 1.4 + 128;
                double legendY = pageWidth / 128;


                context.FillRectangle(pageWidth / 64, pageWidth / 64, width, legendHeight, Colour.FromRgb(180, 180, 180));

                context.FillRectangle(pageWidth / 128, legendY, width, legendHeight, Colour.FromRgb(255, 255, 255));

                context.StrokeRectangle(pageWidth / 128, legendY, width, legendHeight, BlackColour, options.LineWidth * 0.75);

                context.FillText(pageWidth / 64, legendY + pageWidth / 128, "Legend", legendFont, BlackColour);

                for (int i = 0; i < 128; i++)
                {
                    context.FillRectangle(pageWidth / 64, legendY + pageWidth / 128 + options.FontSize * 1.4 + i, 20, i < 127 ? 1.5 : 1, Colour.FromRgb(ViridisColorScale[1023 - i * 8][0], ViridisColorScale[1023 - i * 8][1], ViridisColorScale[1023 - i * 8][2]));
                }

                for (int i = (int)min; i <= 0; i++)
                {
                    for (int i2 = 0; i2 < 9; i2++)
                    {
                        double val2 = Math.Pow(10, i) * (1 + i2);
                        double val = Math.Log10(val2);
                        double y = legendY + pageWidth / 128 + options.FontSize * 1.4 + 128 * (1 + (val - min) / min);

                        if (i2 == 0)
                        {
                            context.StrokePath(new GraphicsPath().MoveTo(pageWidth / 64 + 24, y).LineTo(pageWidth / 64 + 32, y), Colour.FromRgb(0, 0, 0), lineWidth: 0.5);
                        }
                        else if (i != 0)
                        {
                            context.StrokePath(new GraphicsPath().MoveTo(pageWidth / 64 + 24, y).LineTo(pageWidth / 64 + 32, y), Colour.FromRgb(128, 128, 128), lineWidth: 0.5);
                        }
                    }
                }

                context.FillText(pageWidth / 64 + 36, legendY + pageWidth / 128 + options.FontSize * 1.4, "1", baseFont, BlackColour);
                context.FillText(pageWidth / 64 + 36, legendY + pageWidth / 128 + options.FontSize * 1.4 + 128, Math.Pow(10, min).ToString(1, false), baseFont, BlackColour, textBaseline: TextBaselines.Bottom);

                return new double[] { width + pageWidth / 32, 0 };
            };
        }



        public class Options
        {
            public double PieSize { get; set; } = 5;
            public double BranchSize { get; set; } = 3;
            public float FontSize { get; set; } = 12;
            public string FontFamily { get; set; } = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "OpenSans-Regular.ttf");
            public bool NodeNumbers { get; set; } = false;
            public float LineWidth { get; set; } = 1;
            public bool ScaleAxis { get; set; } = false;
            public float ScaleSpacing { get; set; } = 1;
            public bool ScaleGrid { get; set; } = false;
            public float GridSpacing { get; set; } = 0.25F;
            public byte[] GridColour { get; set; } = new byte[] { 200, 200, 200 };
            public float GridWidth { get; set; } = 0.5F;
            public float TreeScale { get; set; } = 1;
            public int SignificantDigits { get; set; } = 3;
            public byte[][] StateColours { get; set; } = new byte[0][];
        }

        public static void PlotTree(this TreeNode tree, float pageWidth, float pageHeight, float margin, string path, Options options, Action<List<TreeNode>, int, Graphics, double, double> nodePlotAction, Action<List<TreeNode>, int, Graphics, double, double, double, double> branchPlotAction, Func<Graphics, double[]> legendFunc, float treeRealHeight = -1, bool logProgress = false)
        {
            Document treeDoc = new Document();
            treeDoc.Pages.Add(new Page(pageWidth, pageHeight));
            Graphics context = treeDoc.Pages.Last().Graphics;
            tree.PlotTree(context, pageWidth, pageHeight, options, margin, nodePlotAction, branchPlotAction, legendFunc, treeRealHeight, null, logProgress);

            treeDoc.SaveAsPDF(path);

            if (Utils.RunningGui)
            {
                Utils.Trigger("PlottingFinished", new object[] { });
            }
        }

        public static void PlotTree(this TreeNode tree, Graphics context, double pageWidth, double pageHeight, Options options, float margin, Action<List<TreeNode>, int, Graphics, double, double> nodePlotAction, Action<List<TreeNode>, int, Graphics, double, double, double, double> branchPlotAction, Func<Graphics, double[]> legendFunc, float treeRealHeight = -1, EventWaitHandle abortHandle = null, bool logProgress = false)
        {
            if (treeRealHeight >= 0)
            {
                pageHeight = treeRealHeight;
            }

            Font baseFont = new Font(new FontFamily(options.FontFamily), options.FontSize);

            context.Save();

            context.Translate(margin, margin);

            context.Save();

            double[] legendTransl = legendFunc(context);

            context.Restore();

            context.Translate(options.PieSize + legendTransl[0], legendTransl[1]);

            List<string> leaves = tree.GetLeafNames();

            double width = pageWidth - 2 * margin - options.PieSize - legendTransl[0];
            double height = pageHeight - 2 * margin - legendTransl[1];

            double maxLabelWidth = 0;


            for (int i = 0; i < leaves.Count; i++)
            {
                double labelW = context.MeasureText(leaves[i], baseFont).Width;
                maxLabelWidth = Math.Max(maxLabelWidth, labelW);
            }

            maxLabelWidth += margin + options.PieSize + 10;

            for (int i = 0; i < leaves.Count; i++)
            {
                context.Save();

                context.Translate(options.PieSize, 0);

                TreeNode node = tree.GetBranchFromName(leaves[i]);

                double x = (node.GetXMultiplier() * (width - options.PieSize - maxLabelWidth)) + options.PieSize * 2 + 10;

                context.FillText(x, (i + 0.5) * height / leaves.Count, leaves[i], baseFont, BlackColour, TextBaselines.Middle);

                context.Restore();
            }

            List<TreeNode> nodes = tree.GetChildrenRecursive();

            Dictionary<string, double?[]> storedPos = new Dictionary<string, double?[]>();

            int leftPos = -1;

            int lastPerc = 0;

            if (logProgress)
            {
                ConsoleWrapper.WriteLine();
                ConsoleWrapper.Write("Plotting branches: ");
                leftPos = ConsoleWrapper.CursorLeft;
                ConsoleWrapper.Write("0%");
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                context.Save();

                double x = nodes[i].GetXMultiplier() * (width - maxLabelWidth);
                double y = nodes[i].GetYMultiplier() * height;


                if (nodes[i].Parent == null)
                {
                    storedPos[nodes[i].Guid] = new double?[] { x, y, null, null };
                }

                if (nodes[i].Parent != null)
                {
                    double pX = nodes[i].Parent.GetXMultiplier() * (width - maxLabelWidth);
                    double pY = nodes[i].Parent.GetYMultiplier() * height;

                    storedPos[nodes[i].Guid] = new double?[] { x, y, pX, pY };

                    context.Save();

                    branchPlotAction(nodes, i, context, x, y, pX, pY);

                    if (abortHandle != null && abortHandle.WaitOne(0))
                    {
                        return;
                    }

                    context.Restore();
                }
                context.Restore();

                if (logProgress)
                {
                    int perc = (int)Math.Round(((double)(i + 1) / nodes.Count) * 100);

                    if (perc != lastPerc)
                    {
                        lastPerc = perc;
                        ConsoleWrapper.CursorLeft = leftPos;
                        ConsoleWrapper.Write(((double)(i + 1) / nodes.Count).ToString("0%"));

                        if (Utils.RunningGui)
                        {
                            Utils.Trigger("BranchProgress", new object[] { (double)(i + 1) / nodes.Count });
                        }
                    }
                }
            }

            if (logProgress)
            {
                ConsoleWrapper.CursorLeft = leftPos;
                ConsoleWrapper.Write("Done.");

                lastPerc = 0;

                ConsoleWrapper.WriteLine();
                ConsoleWrapper.Write("Plotting nodes: ");
                leftPos = ConsoleWrapper.CursorLeft;
                ConsoleWrapper.Write("0%");
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                context.Save();

                double x = (double)storedPos[nodes[i].Guid][0];
                double y = (double)storedPos[nodes[i].Guid][1];

                if (nodes[i].Children.Count > 0 && options.NodeNumbers)
                {
                    context.StrokeText(x - context.MeasureText(i.ToString(), baseFont).Width / 2, y - 5 * options.LineWidth - options.PieSize, i.ToString(), baseFont, Colour.FromRgb(255, 255, 255), TextBaselines.Bottom, options.LineWidth * 5, lineJoin: LineJoins.Round);
                    context.FillText(x - context.MeasureText(i.ToString(), baseFont).Width / 2, y - 5 * options.LineWidth - options.PieSize, i.ToString(), baseFont, BlackColour, TextBaselines.Bottom);
                }

                nodePlotAction(nodes, i, context, x, y);

                if (abortHandle != null && abortHandle.WaitOne(0))
                {
                    return;
                }

                context.Restore();

                if (logProgress)
                {
                    int perc = (int)Math.Round(((double)(i + 1) / nodes.Count) * 100);

                    if (perc != lastPerc)
                    {
                        lastPerc = perc;
                        ConsoleWrapper.CursorLeft = leftPos;
                        ConsoleWrapper.Write(((double)(i + 1) / nodes.Count).ToString("0%"));

                        if (Utils.RunningGui)
                        {
                            Utils.Trigger("NodeProgress", new object[] { (double)(i + 1) / nodes.Count });
                        }
                    }
                }
            }
            context.Restore();

            if (logProgress)
            {
                ConsoleWrapper.CursorLeft = leftPos;
                ConsoleWrapper.Write("Done.");
            }
        }

        public static void PlotSimpleTree(this TreeNode tree, float pageWidth, float pageHeight, float margin, string path, Options options, bool logProgress = false)
        {
            float realHeight = pageHeight;

            if (options.ScaleAxis)
            {
                realHeight = pageHeight - options.LineWidth - options.FontSize * 3;
            }

            tree.PlotTree(pageWidth, pageHeight, margin, path, options, NodeNoAction, BranchSimple(options.LineWidth), StandardAgeAxis(margin, pageWidth, pageHeight, 0, options, tree), realHeight, logProgress);
        }

        public static void PlotTreeWithPies(this TreeNode tree, float pageWidth, float pageHeight, float margin, string path, Options options, double[][] stateProbs, string[] states, bool logProgress = false)
        {
            List<(int r, int g, int b, double a)> stateColours = new List<(int r, int g, int b, double a)>();

            if (options.StateColours.Length != states.Length)
            {
                for (int j = 0; j < states.Length; j++)
                {
                    stateColours.Add(GetColor(j, 1, states.Length));
                }
            }
            else
            {
                for (int j = 0; j < states.Length; j++)
                {
                    stateColours.Add((options.StateColours[j][0], options.StateColours[j][1], options.StateColours[j][2], 1));
                }
            }

            float realHeight = pageHeight;

            if (options.ScaleAxis)
            {
                realHeight = pageHeight - options.LineWidth - options.FontSize * 3;
            }

            PlotTree(tree, pageWidth, pageHeight, margin, path, options, NodePie(options, stateProbs, stateColours), BranchSimple(options.LineWidth), StandardLegend(tree, margin, pageWidth, pageHeight, options, states, stateColours), realHeight, logProgress);
        }

        public static void PlotTreeWithPiesAndBranchStates(this TreeNode tree, float pageWidth, float pageHeight, float margin, string path, Options options, double[][] stateProbs, IEnumerable<TaggedHistory> histories, int[] treeSamples, LikelihoodModel[] likModels, LikelihoodModel meanLikModel, int[][] meanNodeCorresp, double resolution, List<string> states,bool isClockLike, bool logProgress = false)
        {
            List<(int r, int g, int b, double a)> stateColours = new List<(int r, int g, int b, double a)>();

            if (options.StateColours.Length != states.Count)
            {
                for (int j = 0; j < states.Count; j++)
                {
                    stateColours.Add(GetColor(j, 1, states.Count));
                }
            }
            else
            {
                for (int j = 0; j < states.Count; j++)
                {
                    stateColours.Add((options.StateColours[j][0], options.StateColours[j][1], options.StateColours[j][2], 1));
                }
            }

            float realHeight = pageHeight;

            if (options.ScaleAxis)
            {
                realHeight = pageHeight - options.LineWidth - options.FontSize * 3;
            }

            PlotTree(tree, pageWidth, pageHeight, margin, path, options, NodePie(options, stateProbs, stateColours), BranchSMap(resolution, histories, isClockLike, treeSamples, likModels, meanLikModel, meanNodeCorresp, states, options, stateColours), StandardLegend(tree, margin, pageWidth, pageHeight, options, states.ToArray(), stateColours), realHeight, logProgress);
        }

        public static void PlotTreeWithBranchSampleSizes(this TreeNode tree, float pageWidth, float pageHeight, float margin, string path, Options options, IEnumerable<TaggedHistory> histories, int[] treeSamples, LikelihoodModel[] likModels, LikelihoodModel meanLikModel, int[][] meanNodeCorresp, double resolution, List<string> states, bool isClockLike, bool logProgress = false)
        {
            float realHeight = pageHeight;

            if (options.ScaleAxis)
            {
                realHeight = pageHeight - options.LineWidth - options.FontSize * 3;
            }

            List<double> sampleSizes = new List<double>();

            PlotTree(tree, pageWidth, pageHeight, margin, path, options, NodeNoAction, (nodes, i, context, x, y, pX, pY) =>
            {
                double[] sampleXs = new double[Math.Max(0, (int)Math.Ceiling((x - pX) / resolution) + 1)];
                double[] sampleSizeXs = new double[sampleXs.Length];

                for (int j = 0; j < sampleXs.Length; j++)
                {
                    sampleXs[j] = Math.Min(pX + j * resolution, x);
                    double sampleLength = (sampleXs[j] - pX) / (x - pX) * nodes[i].Length;

                    if (isClockLike)
                    {
                        sampleSizeXs[j] = (double)Utils.GetBranchSampleSize(histories, treeSamples, likModels, meanLikModel, meanNodeCorresp, nodes.Count - 1 - i, nodes[i].Length - sampleLength, isClockLike) / (double)treeSamples.Length;
                    }
                    else
                    {
                        sampleSizeXs[j] = (double)Utils.GetBranchSampleSize(histories, treeSamples, likModels, meanLikModel, meanNodeCorresp, nodes.Count - 1 - i, sampleLength, isClockLike) / (double)treeSamples.Length;
                    }
                }

                sampleSizes.AddRange(sampleSizeXs);

                double[] usedProbs = new double[sampleXs.Length];

                if (sampleXs.Length > 0)
                {
                    context.StrokePath(new GraphicsPath().MoveTo(pX, y).LineTo(pX, pY), BlackColour, options.LineWidth);

                    GraphicsPath pth = new GraphicsPath();

                    pth.MoveTo(pX, y - options.LineWidth * 0.5 + usedProbs[0] * options.LineWidth);

                    for (int k = 1; k < sampleXs.Length; k++)
                    {
                        pth.LineTo(sampleXs[k], y - options.BranchSize + usedProbs[k] * options.BranchSize * 2);
                    }

                    for (int k = sampleXs.Length - 1; k > 0; k--)
                    {
                        pth.LineTo(sampleXs[k], y - options.BranchSize + (usedProbs[k] + sampleSizeXs[k]) * options.BranchSize * 2);
                        usedProbs[k] += sampleSizeXs[k];
                    }

                    pth.LineTo(pX, y - options.LineWidth * 0.5 + (usedProbs[0] + sampleSizeXs[0]) * options.LineWidth);
                    usedProbs[0] += sampleSizeXs[0];

                    context.FillPath(pth, Colour.FromRgba(GetColor(0, 1, 1)));
                }
                else
                {
                    context.StrokePath(new GraphicsPath().MoveTo(x, y).LineTo(pX, y).LineTo(pX, pY), BlackColour, options.LineWidth);
                }


            }, StandardLegendSize(tree, margin, pageWidth, pageHeight, options, states.ToArray()), realHeight, logProgress);
        }



        public static void PlotTreeWithPieTarget(this TreeNode tree, float pageWidth, float pageHeight, float margin, string path, Options options, double[][] stateLiks, string[] states, bool logProgress = false)
        {
            List<(int r, int g, int b, double a)> stateColours = new List<(int r, int g, int b, double a)>();

            if (options.StateColours.Length != states.Length)
            {
                for (int j = 0; j < states.Length; j++)
                {
                    stateColours.Add(GetColor(j, 1, states.Length));
                }
            }
            else
            {
                for (int j = 0; j < states.Length; j++)
                {
                    stateColours.Add((options.StateColours[j][0], options.StateColours[j][1], options.StateColours[j][2], 1));
                }
            }

            PlotTree(tree, pageWidth, pageHeight, margin, path, options, NodeTarget(options, stateLiks, stateColours), (nodes, i, context, x, y, pX, pY) =>
            {
                context.StrokePath(new GraphicsPath().MoveTo(x, y).LineTo(pX, y).LineTo(pX, pY), BlackColour, options.LineWidth);

            }, StandardLegend(tree, margin, pageWidth, pageHeight, options, states.ToArray(), stateColours), logProgress: logProgress);
        }

        public static void PlotTreeWithSquares(this TreeNode tree, float pageWidth, float pageHeight, float margin, string path, Options options, double[][] statePrior, double[][] stateLiks, string[] states, bool logProgress = false)
        {
            List<(int r, int g, int b, double a)> stateColours = new List<(int r, int g, int b, double a)>();

            if (options.StateColours.Length != states.Length)
            {
                for (int j = 0; j < states.Length; j++)
                {
                    stateColours.Add(GetColor(j, 1, states.Length));
                }
            }
            else
            {
                for (int j = 0; j < states.Length; j++)
                {
                    stateColours.Add((options.StateColours[j][0], options.StateColours[j][1], options.StateColours[j][2], 1));
                }
            }

            PlotTree(tree, pageWidth, pageHeight, margin, path, options, (nodes, i, context, x, y) =>
            {
                context.Translate(options.LineWidth, -options.LineWidth);

                context.StrokeRectangle(x - options.PieSize - options.LineWidth, y - options.PieSize, options.PieSize * 2 + options.LineWidth, options.PieSize * 2 + options.LineWidth, Colour.FromRgb(255, 255, 255), 2 * options.LineWidth);
                context.FillRectangle(x - options.PieSize - options.LineWidth, y - options.PieSize, options.PieSize * 2 + options.LineWidth, options.PieSize * 2 + options.LineWidth, Colour.FromRgb(255, 255, 255));

                double[] normalisedLiks = new double[stateLiks[nodes.Count - 1 - i].Length];

                double sumLik = Utils.LogSumExp(stateLiks[nodes.Count - 1 - i]);

                for (int j = 0; j < normalisedLiks.Length; j++)
                {
                    normalisedLiks[j] = Math.Exp(stateLiks[nodes.Count - 1 - i][j] - sumLik);
                }

                double prevPrior = 0;
                double prevLik = 0;

                for (int j = 0; j < normalisedLiks.Length; j++)
                {
                    double x0 = x + options.PieSize * (2 * prevLik - 1);
                    double y0 = y + options.PieSize * (1 - 2 * prevPrior);

                    prevLik += normalisedLiks[j];
                    prevPrior += statePrior[nodes.Count - 1 - i][j];

                    double x1 = x + options.PieSize * (2 * prevLik - 1);
                    double y1 = y + options.PieSize * (1 - 2 * prevPrior);

                    Colour stroke = Colour.FromRgba((stateColours[j % stateColours.Count].Item1 / 2 + 128, stateColours[j % stateColours.Count].Item2 / 2 + 128, stateColours[j % stateColours.Count].Item3 / 2 + 128, stateColours[j % stateColours.Count].Item4));

                    if (statePrior[nodes.Count - 1 - i][j] > 0.001)
                    {
                        context.StrokePath(new GraphicsPath().MoveTo(x - options.PieSize - 2 * options.LineWidth, y0).LineTo(x - options.PieSize - 2 * options.LineWidth, y1), stroke, options.LineWidth);
                    }

                    if (normalisedLiks[j] > 0.001)
                    {
                        context.StrokePath(new GraphicsPath().MoveTo(x0, y + options.PieSize + 2 * options.LineWidth).LineTo(x1, y + options.PieSize + 2 * options.LineWidth), stroke, options.LineWidth);
                    }

                    if (normalisedLiks[j] > 0.001 && statePrior[nodes.Count - 1 - i][j] > 0.001)
                    {
                        context.FillRectangle(x0, y0, x1 - x0, y1 - y0, Colour.FromRgba(stateColours[j % stateColours.Count]));
                    }
                }

            }, (nodes, i, context, x, y, pX, pY) =>
            {
                context.StrokePath(new GraphicsPath().MoveTo(x, y).LineTo(pX, y).LineTo(pX, pY), BlackColour, options.LineWidth);

            }, (context) =>
            {
                Font legendFont = new Font(new FontFamily(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "OpenSans-Bold.ttf")), options.FontSize);
                Font baseFont = new Font(new FontFamily(options.FontFamily), options.FontSize);

                double width = pageWidth / 64 + context.MeasureText("Legend", legendFont).Width;

                for (int i = 0; i < states.Length; i++)
                {
                    width = Math.Max(width, pageWidth / 128 * 3 + options.PieSize * 3 + context.MeasureText(states[i], baseFont).Width);
                }

                context.FillRectangle(pageWidth / 64, pageWidth / 64, width, pageWidth / 128 * 3 + options.FontSize * 2.1 + options.PieSize + (2 * options.PieSize + options.FontSize * 0.4) * (states.Length - 1) + options.PieSize * 3 + options.FontSize * 1.4, Colour.FromRgb(180, 180, 180));

                context.FillRectangle(pageWidth / 128, pageWidth / 128, width, pageWidth / 128 * 3 + options.FontSize * 2.1 + options.PieSize + (2 * options.PieSize + options.FontSize * 0.4) * (states.Length - 1) + options.PieSize * 3 + options.FontSize * 1.4, Colour.FromRgb(255, 255, 255));

                context.StrokeRectangle(pageWidth / 128, pageWidth / 128, width, pageWidth / 128 * 3 + options.FontSize * 2.1 + options.PieSize + (2 * options.PieSize + options.FontSize * 0.4) * (states.Length - 1) + options.PieSize * 3 + options.FontSize * 1.4, BlackColour, options.LineWidth * 0.75);

                context.FillText(pageWidth / 64, pageWidth / 64 + pageWidth / 128, "Legend", legendFont, BlackColour);

                double[] normalisedLiks = new double[states.Length];

                for (int j = 0; j < normalisedLiks.Length; j++)
                {
                    normalisedLiks[j] = 1.0 / states.Length;
                }

                double prevPrior = 0;
                double prevLik = 0;

                double x = width / 2 + pageWidth / 128;
                double y = pageWidth / 128 * 3 + options.FontSize * 1.4 + options.PieSize;

                for (int j = 0; j < normalisedLiks.Length; j++)
                {
                    double x0 = x + options.PieSize * (2 * prevLik - 1);
                    double y0 = y + options.PieSize * (1 - 2 * prevPrior);

                    prevLik += normalisedLiks[j];
                    prevPrior += normalisedLiks[j];

                    double x1 = x + options.PieSize * (2 * prevLik - 1);
                    double y1 = y + options.PieSize * (1 - 2 * prevPrior);

                    Colour stroke = Colour.FromRgba((stateColours[j % stateColours.Count].Item1 / 2 + 128, stateColours[j % stateColours.Count].Item2 / 2 + 128, stateColours[j % stateColours.Count].Item3 / 2 + 128, stateColours[j % stateColours.Count].Item4));

                    context.StrokePath(new GraphicsPath().MoveTo(x - options.PieSize - 2 * options.LineWidth, y0).LineTo(x - options.PieSize - 2 * options.LineWidth, y1), stroke, options.LineWidth);

                    context.StrokePath(new GraphicsPath().MoveTo(x0, y + options.PieSize + 2 * options.LineWidth).LineTo(x1, y + options.PieSize + 2 * options.LineWidth), stroke, options.LineWidth);

                    context.FillRectangle(x0, y0, x1 - x0, y1 - y0, Colour.FromRgba(stateColours[j % stateColours.Count]));
                }

                Font smallFont = new Font(new FontFamily(options.FontFamily), options.FontSize * 0.5);

                context.FillText(pageWidth / 64, pageWidth / 64 + pageWidth / 128 + options.FontSize * 0.5 * 2.1, "Prior", smallFont, BlackColour);

                context.FillText(width - context.MeasureText("Posterior", smallFont).Width, y + options.PieSize * 2, "Posterior", smallFont, BlackColour);

                context.FillText(pageWidth / 64, y + options.PieSize * 2 + options.FontSize * 0.5, "Likelihood", smallFont, BlackColour);

                GraphicsPath pth = new GraphicsPath();

                pth.MoveTo(x - options.PieSize - 2.5 * options.LineWidth / 1.25, y);
                pth.LineTo(pageWidth / 64 + context.MeasureText("Prior", smallFont).Width * 0.5, pageWidth / 128 * 3 + options.FontSize * 0.5 * 3.1);

                pth.MoveTo(x - options.PieSize * 0.75, y + options.PieSize + 2.5 * options.LineWidth / 1.25);
                pth.LineTo(pageWidth / 64 + context.MeasureText("Likelih", smallFont).Width * 0.5, y + options.PieSize * 2 + options.FontSize * 0.5);

                pth.MoveTo(x, y);
                pth.LineTo(width - context.MeasureText("Posterior", smallFont).Width * 0.5, y + options.PieSize * 2);

                context.StrokePath(pth, BlackColour, options.LineWidth / 1.25, LineCaps.Round);


                for (int i = 0; i < states.Length; i++)
                {
                    context.FillRectangle(pageWidth / 128 * 3, y + options.PieSize * 2 + options.FontSize * 1.4 + (2 * options.PieSize + options.FontSize * 0.4) * i, 2 * options.PieSize, 2 * options.PieSize, Colour.FromRgba(stateColours[i % stateColours.Count]));

                    context.StrokeRectangle(pageWidth / 128 * 3, y + options.PieSize * 2 + options.FontSize * 1.4 + (2 * options.PieSize + options.FontSize * 0.4) * i, 2 * options.PieSize, 2 * options.PieSize, BlackColour, options.LineWidth);

                    context.FillText(pageWidth / 128 * 3 + options.PieSize * 3, y + options.PieSize * 3 + options.FontSize * 1.4 + (2 * options.PieSize + options.FontSize * 0.4) * i, states[i], baseFont, BlackColour);
                }

                return new double[] { width + pageWidth / 32, 0 };
            }, logProgress: logProgress);
        }

        public enum BinRules { Sqrt, Sturges, Rice, Doane, Scott, FreedmanDiaconis }

        public static BinRules ParseBinRule(string sr)
        {
            switch (sr.ToLower().Replace("-", ""))
            {
                case "sqrt":
                    return BinRules.Sqrt;
                case "sturges":
                    return BinRules.Sturges;
                case "rice":
                    return BinRules.Rice;
                case "doane":
                    return BinRules.Doane;
                case "scott":
                    return BinRules.Scott;
                case "freedmandiaconis":
                    return BinRules.FreedmanDiaconis;
                default:
                    throw new NotImplementedException("Unknown bin rule: " + sr + "!");
            }
        }

        public static double PlotHistogram(double[] values, BinRules binRule, double width, double height, double pageHeight, Options options, Graphics context, Colour binColor, object priorDistribution, int multiVariateIndex, bool useFontSize = false, double customMin = -1, double customMax = -1, bool showMeanMedianEtc = true, (double, string, bool)[] interestingValues = null)
        {
            double minValue = values.Min();
            double maxValue = values.Max();

            (double Mean, double Variance) meanAndVariance = values.MeanAndVariance();

            double median = values.Median();

            int binCount = -1;

            switch (binRule)
            {
                case BinRules.Sqrt:
                    binCount = (int)Math.Ceiling(Math.Sqrt(values.Length));
                    break;
                case BinRules.Sturges:
                    binCount = (int)(Math.Ceiling(Math.Log(values.Length, 2)) + 1);
                    break;
                case BinRules.Rice:
                    binCount = (int)Math.Ceiling(2 * Math.Pow(values.Length, 1.0 / 3.0));
                    break;
                case BinRules.Doane:
                    double g1 = ((from el in values select el * el * el).Average() - 3 * meanAndVariance.Mean * meanAndVariance.Variance - meanAndVariance.Mean * meanAndVariance.Mean * meanAndVariance.Mean) / Math.Pow(meanAndVariance.Variance, 3.0 / 2.0);
                    double sg1 = Math.Sqrt(6 * (values.Length - 2.0) / ((values.Length + 1) * (values.Length + 3)));
                    binCount = (int)Math.Ceiling(1 + Math.Log(values.Length, 2) + Math.Log(1 + Math.Abs(g1) / sg1, 2));
                    break;
                case BinRules.Scott:
                    double h = 3.5 * Math.Sqrt(meanAndVariance.Variance) / Math.Pow(values.Length, 1.0 / 3.0);
                    binCount = (int)Math.Ceiling((maxValue - minValue) / h);
                    break;
                case BinRules.FreedmanDiaconis:
                    double iqr = values.IQR();
                    double h2 = 2 * iqr / Math.Pow(values.Length, 1.0 / 3.0);
                    binCount = (int)Math.Ceiling((maxValue - minValue) / h2);
                    break;
            }

            double binWidth = (maxValue - minValue) / binCount;

            if (binCount < 0)
            {
                binCount = 1;
                binWidth = 1;
                minValue -= 0.5;
                maxValue += 0.5;
            }

            int[] bins = new int[binCount];

            for (int i = 0; i < values.Length; i++)
            {
                bins[Math.Max(0, Math.Min(binCount - 1, (int)((values[i] - minValue) / binWidth)))]++;
            }

            float fontSize = useFontSize ? options.FontSize : (float)(height / 48);

            Font baseFont = new Font(new FontFamily(options.FontFamily), fontSize);

            double deltaX = context.MeasureText(new string('0', (int)Math.Ceiling(Math.Log10(bins.Max()))) + "  ", baseFont).Width;
            width -= deltaX;
            context.Translate(deltaX, 0);
            height = (height - fontSize) * 128 / 132;

            double range = maxValue - minValue;
            double minX = Math.Max(0, minValue - range * 0.05);
            double maxX = maxValue + range * 0.05;

            if (customMax >= 0 && customMin >= 0)
            {
                range = customMax - customMin;
                minX = Math.Max(0, customMin - range * 0.05);
                maxX = customMax + range * 0.05;
            }

            int maxBin = bins.Max();

            int maxY = maxBin + Math.Max(1, (int)(maxBin * 0.05));

            int yIncrement = Math.Max(1, maxY / 10);
           
            LineDash dash = new LineDash((float)(options.LineWidth * 3), (float)(options.LineWidth * 5), 0);

            for (int y = yIncrement; height - (double)y / maxY * height > width / 64; y += yIncrement)
            {
                context.StrokePath(new GraphicsPath().MoveTo(0, height - (double)y / maxY * height).LineTo(width, height - (double)y / maxY * height), Colour.FromRgb(200, 200, 200), options.LineWidth * 0.75, lineDash: dash);

                context.FillText(-context.MeasureText(y.ToString() + "  ", baseFont).Width, height - (double)y / maxY * height, y.ToString(), baseFont, BlackColour, TextBaselines.Middle);
            }

            for (int i = 0; i < bins.Length; i++)
            {
                context.FillRectangle((minValue + binWidth * i - minX) / (maxX - minX) * width, height, binWidth / (maxX - minX) * width, -height * bins[i] / maxY, binColor);

                context.StrokeRectangle((minValue + binWidth * i - minX) / (maxX - minX) * width, height, binWidth / (maxX - minX) * width, -height * bins[i] / maxY, BlackColour, options.LineWidth * 0.5);
            }

            if (priorDistribution is IContinuousDistribution distrib)
            {
                double[] densities = new double[1000];
                double sumDens = 1 - distrib.CumulativeDistribution(0);

                for (int i = 0; i < densities.Length; i++)
                {
                    if (minX + (maxX - minX) / (densities.Length - 1) * i < 0)
                    {
                        densities[i] = 0;
                    }
                    else
                    {
                        if (distrib.Density(minX + (maxX - minX) / (densities.Length - 1) * i + binWidth * 0.5) > 0 && distrib.Density(minX + (maxX - minX) / (densities.Length - 1) * i - binWidth * 0.5) > 0)
                        {
                            double top = Math.Max(0, minX + (maxX - minX) / (densities.Length - 1) * i + binWidth * 0.5);
                            double bottom = Math.Max(0, minX + (maxX - minX) / (densities.Length - 1) * i - binWidth * 0.5);
                            densities[i] = distrib.CumulativeDistribution(top) - distrib.CumulativeDistribution(bottom);
                        }
                        else
                        {
                            densities[i] = 0;
                        }
                    }
                }

                int i0 = 1;

                while (densities[i0] == 0 || densities[i0] > maxY * sumDens / values.Length)
                {
                    i0++;
                }

                int rightI = (int)((width * i0 / (densities.Length - 1) + context.MeasureText("prior", baseFont).Width) / width * (densities.Length - 1));

                context.Save();
                
                context.Translate(width * i0 / (densities.Length - 1), height - (densities[i0] / sumDens * values.Length) / maxY * height);
                double angle = -Math.Atan2(((densities[rightI] - densities[i0]) / sumDens * values.Length) / maxY * height, (rightI - i0) * width / (densities.Length - 1));
                context.Rotate(angle);

                context.FillText(0, -3, "prior", baseFont, Colour.FromRgb(128, 128, 128), TextBaselines.Bottom);

                context.Restore();

                double prevX0 = ((width * i0 / (densities.Length - 1)) / width * (densities.Length - 1));

                i0 = (int)Math.Ceiling(prevX0);

                double frac = i0 - prevX0;

                GraphicsPath pth = new GraphicsPath();

                pth.MoveTo(width * (i0 - frac) / (densities.Length - 1), height - (densities[i0] * (1 - frac) + densities[i0 - 1] * frac) / sumDens * values.Length / maxY * height);

                int lastDensity = densities.Length - 1;
                while (lastDensity > 0 && (densities[lastDensity] == 0 || densities[lastDensity] > maxY * sumDens / values.Length))
                {
                    lastDensity--;
                }

                for (int i = i0; i <= lastDensity; i++)
                {
                    pth.LineTo(width * i / (densities.Length - 1), height - densities[i] / sumDens * values.Length / maxY * height);
                }

                context.StrokePath(pth, Colour.FromRgb(128, 128, 128), options.LineWidth);
            }
            else if (priorDistribution is MultivariateDistribution distribM)
            {
                double[] densities = new double[1000];
                double sumDens = 1 - distribM.MarginalCumulativeDistribution(0, multiVariateIndex);

                for (int i = 0; i < densities.Length; i++)
                {
                    if (minX + (maxX - minX) / (densities.Length - 1) * i < 0)
                    {
                        densities[i] = 0;
                    }
                    else
                    {
                        if (distribM.MarginalDensity(minX + (maxX - minX) / (densities.Length - 1) * i + binWidth * 0.5, multiVariateIndex) > 0 && distribM.MarginalDensity(minX + (maxX - minX) / (densities.Length - 1) * i - binWidth * 0.5, multiVariateIndex) > 0)
                        {
                            densities[i] = distribM.MarginalCumulativeDistribution(minX + (maxX - minX) / (densities.Length - 1) * i + binWidth * 0.5, multiVariateIndex) - distribM.MarginalCumulativeDistribution(minX + (maxX - minX) / (densities.Length - 1) * i - binWidth * 0.5, multiVariateIndex);
                        }
                        else
                        {
                            densities[i] = 0;
                        }
                    }
                }

                int i0 = 1;

                while (densities[i0] == 0 || densities[i0] > maxY * sumDens / values.Length)
                {
                    i0++;
                }


                int rightI = (int)((width * i0 / (densities.Length - 1) + context.MeasureText("prior", baseFont).Width) / width * (densities.Length - 1));

                context.Save();
                
                context.Translate(width * i0 / (densities.Length - 1), height - densities[i0] / sumDens * values.Length / maxY * height);
                double angle = -Math.Atan2((densities[rightI] - densities[i0]) / sumDens * values.Length / maxY * height, (rightI - i0) * width / (densities.Length - 1));
                context.Rotate(angle);


                context.FillText(0, -3, "prior", baseFont, Colour.FromRgb(128, 128, 128), TextBaselines.Bottom);

                context.Restore();

                double prevX0 = ((width * i0 / (densities.Length - 1)) / width * (densities.Length - 1));

                i0 = (int)Math.Ceiling(prevX0);

                double frac = i0 - prevX0;

                GraphicsPath pth = new GraphicsPath();

                pth.MoveTo(width * (i0 - frac) / (densities.Length - 1), height - (densities[i0] * (1 - frac) + densities[i0 - 1] * frac) / sumDens * values.Length / maxY * height);

                int lastDensity = densities.Length - 1;
                while (lastDensity > 0 && (densities[lastDensity] == 0 || densities[lastDensity] > maxY * sumDens / values.Length))
                {
                    lastDensity--;
                }

                for (int i = i0; i <= lastDensity; i++)
                {
                    pth.LineTo(width * i / (densities.Length - 1), height - densities[i] / sumDens * values.Length / maxY * height);
                }

                context.StrokePath(pth, Colour.FromRgb(128, 128, 128), options.LineWidth);
            }

            context.StrokePath(new GraphicsPath().
            MoveTo(width / 128, width / 128).
            LineTo(0, 0).
            MoveTo(-width / 128, width / 128).
            LineTo(0, 0).
            LineTo(0, height).
            LineTo(width, height).
            LineTo(width * 127 / 128, height - width / 128).
            MoveTo(width, height).
            LineTo(width * 127 / 128, height + width / 128), BlackColour, options.LineWidth);

            if (showMeanMedianEtc)
            {

                context.StrokePath(new GraphicsPath().
                MoveTo((minValue - minX) / (maxX - minX) * width, height * 129 / 128).
                LineTo((minValue - minX) / (maxX - minX) * width, height * 131 / 128).
                MoveTo((minValue - minX) / (maxX - minX) * width, height * 130 / 128).
                LineTo((minValue + binWidth - minX) / (maxX - minX) * width - options.LineWidth * 0.75 * 0.5, height * 130 / 128), BlackColour, options.LineWidth * 0.75);


                context.StrokePath(new GraphicsPath().Arc((minValue + binWidth - minX) / (maxX - minX) * width - height / 64, height * 130 / 128, height / 64, -0.523598775598299, 0.523598775598299), BlackColour, options.LineWidth * 0.75);

                context.StrokePath(new GraphicsPath().
                MoveTo((maxValue - binWidth - minX) / (maxX - minX) * width, height * 129 / 128).
                LineTo((maxValue - binWidth - minX) / (maxX - minX) * width, height * 131 / 128).
                MoveTo((maxValue - binWidth - minX) / (maxX - minX) * width, height * 130 / 128).
                LineTo((maxValue - minX) / (maxX - minX) * width, height * 130 / 128).
                MoveTo((maxValue - minX) / (maxX - minX) * width, height * 129 / 128).
                LineTo((maxValue - minX) / (maxX - minX) * width, height * 131 / 128), BlackColour, options.LineWidth * 0.75);


                Font smallerFont = new Font(new FontFamily(options.FontFamily), fontSize * 0.75);

                context.FillText((minValue + binWidth / 2 - minX) / (maxX - minX) * width - context.MeasureText("[" + minValue.ToString(3) + ", " + (minValue + binWidth).ToString(3) + ")", smallerFont).Width / 2, height * 132 / 128, "[" + minValue.ToString(3) + ", " + (minValue + binWidth).ToString(3) + ")", smallerFont, BlackColour);

                context.FillText((maxValue - binWidth / 2 - minX) / (maxX - minX) * width - context.MeasureText("[" + (maxValue - binWidth).ToString(3) + ", " + maxValue.ToString(3) + "]", smallerFont).Width / 2, height * 132 / 128, "[" + (maxValue - binWidth).ToString(3) + ", " + maxValue.ToString(3) + "]", smallerFont, BlackColour);


                context.StrokePath(new GraphicsPath().
                MoveTo((median - minX) / (maxX - minX) * width, height * 132 / 128).
                LineTo((median - minX) / (maxX - minX) * width, 0).
                MoveTo((meanAndVariance.Mean - minX) / (maxX - minX) * width, height * 132 / 128).
                LineTo((meanAndVariance.Mean - minX) / (maxX - minX) * width, 0), BlackColour, options.LineWidth * 0.75, lineDash: new LineDash((float)(options.LineWidth * 0.75 * 3), (float)(options.LineWidth * 0.75 * 5), 0));

                if (meanAndVariance.Mean < median)
                {

                    context.FillText((meanAndVariance.Mean - minX) / (maxX - minX) * width - context.MeasureText("x = " + meanAndVariance.Mean.ToString(3) + "  ", smallerFont).Width, height * 130 / 128, "x = " + meanAndVariance.Mean.ToString(3), smallerFont, BlackColour);
                    context.FillText((meanAndVariance.Mean - minX) / (maxX - minX) * width - context.MeasureText("x = " + meanAndVariance.Mean.ToString(3) + "  ", smallerFont).Width + (context.MeasureText("x", smallerFont).Width - context.MeasureText("_", smallerFont).Width) * 0.5, height * 129.7 / 128, "_", smallerFont, BlackColour);
                    context.FillText((median - minX) / (maxX - minX) * width + context.MeasureText("  ", smallerFont).Width, height * 130 / 128, "x̃ = " + median.ToString(3), smallerFont, BlackColour);
                }
                else
                {
                    context.FillText((meanAndVariance.Mean - minX) / (maxX - minX) * width + context.MeasureText("  ", smallerFont).Width, height * 130 / 128, "x = " + meanAndVariance.Mean.ToString(3), smallerFont, BlackColour);
                    context.FillText((meanAndVariance.Mean - minX) / (maxX - minX) * width + context.MeasureText("  ", smallerFont).Width + (context.MeasureText("x", smallerFont).Width - context.MeasureText("_", smallerFont).Width) * 0.5, height * 129.7 / 128, "_", smallerFont, BlackColour);
                    context.FillText((median - minX) / (maxX - minX) * width - context.MeasureText("x̃ = " + median.ToString(3) + "  ", smallerFont).Width, height * 130 / 128, "x̃ = " + median.ToString(3), smallerFont, BlackColour);
                }

                double stdDev = Math.Sqrt(meanAndVariance.Variance);
                double stdDev2 = stdDev * 0.5;

                context.StrokePath(new GraphicsPath().
                MoveTo((meanAndVariance.Mean - stdDev2 - minX) / (maxX - minX) * width, height * 0.75 - height / 128).
                LineTo((meanAndVariance.Mean - stdDev2 - minX) / (maxX - minX) * width, height * 0.75 + height / 128).
                MoveTo((meanAndVariance.Mean - stdDev2 - minX) / (maxX - minX) * width, height * 0.75).
                LineTo((meanAndVariance.Mean + stdDev2 - minX) / (maxX - minX) * width, height * 0.75).
                MoveTo((meanAndVariance.Mean + stdDev2 - minX) / (maxX - minX) * width, height * 0.75 - height / 128).
                LineTo((meanAndVariance.Mean + stdDev2 - minX) / (maxX - minX) * width, height * 0.75 + height / 128), BlackColour, options.LineWidth * 0.75);

                if (meanAndVariance.Mean < median)
                {
                    context.FillText(Math.Min((meanAndVariance.Mean - stdDev2 - minX) / (maxX - minX) * width, (meanAndVariance.Mean - minX) / (maxX - minX) * width - context.MeasureText("s = " + stdDev.ToString(3) + "  ", smallerFont).Width), height * 0.75 + height / 64, "s = " + stdDev.ToString(3), smallerFont, BlackColour);
                }
                else
                {
                    context.FillText(Math.Max((meanAndVariance.Mean + stdDev2 - minX) / (maxX - minX) * width - context.MeasureText("s = " + stdDev.ToString(3), smallerFont).Width, (meanAndVariance.Mean - minX) / (maxX - minX) * width + context.MeasureText("  ", smallerFont).Width), height * 0.75 + height / 64, "s = " + stdDev.ToString(3), smallerFont, BlackColour);
                }
            }

            if (interestingValues != null)
            {
                Font smallerFont = new Font(new FontFamily(options.FontFamily), fontSize * 0.75);
                for (int i = 0; i < interestingValues.Length; i++)
                {
                    context.StrokePath(new GraphicsPath().
                    MoveTo((interestingValues[i].Item1 - minX) / (maxX - minX) * width, height * 132 / 128).
                    LineTo((interestingValues[i].Item1 - minX) / (maxX - minX) * width, 0), BlackColour, options.LineWidth * 0.75, lineDash: new LineDash((float)(options.LineWidth * 0.75 * 3), (float)(options.LineWidth * 0.75 * 5), 0));

                    bool alignLeft = interestingValues[i].Item3;

                    if (alignLeft)
                    {
                        if ((interestingValues[i].Item1 - minX) / (maxX - minX) * width + context.MeasureText("  " + interestingValues[i].Item2, smallerFont).Width > width - deltaX)
                        {
                            alignLeft = false;
                        }
                    }
                    else
                    {
                        if ((interestingValues[i].Item1 - minX) / (maxX - minX) * width - context.MeasureText(interestingValues[i].Item2 + "  ", smallerFont).Width < -deltaX)
                        {
                            alignLeft = true;
                        }
                    }

                    if (alignLeft)
                    {
                        context.FillText((interestingValues[i].Item1 - minX) / (maxX - minX) * width + context.MeasureText("  ", smallerFont).Width, height * 130 / 128, interestingValues[i].Item2, smallerFont, BlackColour);
                    }
                    else
                    {
                        context.FillText((interestingValues[i].Item1 - minX) / (maxX - minX) * width - context.MeasureText(interestingValues[i].Item2 + "  ", smallerFont).Width, height * 130 / 128, interestingValues[i].Item2, smallerFont, BlackColour);
                    }
                }
            }

            return deltaX;
        }

        public static void PlotHistograms(IEnumerable<(double[] value, string title, object priorDistribution, int multiVariateIndex)> values, BinRules binRule, float pageWidth, float pageHeight, float margin, string path, Options options)
        {
            Document treeDoc = new Document();
            treeDoc.Pages.Add(new Page(pageWidth, pageHeight));

            Graphics context = treeDoc.Pages.Last().Graphics;

            int ind = 0;

            foreach ((double[] value, string title, object priorDistribution, int multiVariateIndex) vals in values)
            {
                if (ind != 0)
                {
                    treeDoc.Pages.Add(new Page(pageWidth, pageHeight));
                    context = treeDoc.Pages.Last().Graphics;
                }

                context.Save();

                Font titleFont = new Font(new FontFamily(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "OpenSans-Bold.ttf")), pageHeight / 32);

                context.FillText(pageWidth / 2 - context.MeasureText(vals.title, titleFont).Width / 2, margin, vals.title, titleFont, BlackColour);

                context.Translate(margin, margin + pageHeight / 32);

                PlotHistogram(vals.value, binRule, pageWidth - 2 * margin, pageHeight - 2 * margin - pageHeight / 32, pageHeight, options, context, Colour.FromRgba(GetColor(ind, 1, values.Count())), vals.priorDistribution, vals.multiVariateIndex);

                context.Restore();

                ind++;
            }
            treeDoc.SaveAsPDF(path);
        }
        public static double GetXMultiplier(this TreeNode tree, double? cachedTotalLength = null)
        {
            if (cachedTotalLength == null)
            {
                cachedTotalLength = tree.getUltimateParent().DownstreamLength();
            }

            double thisLength = tree.UpstreamLength();

            return thisLength / (double)cachedTotalLength;
        }

        public static TreeNode getUltimateParent(this TreeNode tree)
        {
            TreeNode prnt = tree;
            while (prnt.Parent != null)
            {
                prnt = prnt.Parent;
            }
            return prnt;
        }

        public static double GetYMultiplier(this TreeNode tree)
        {
            if (tree.Children.Count == 0)
            {
                List<string> leaves = tree.getUltimateParent().GetLeafNames();

                int ind = 0;

                while (leaves[ind] != tree.Name)
                {
                    ind++;
                }

                return (ind + 0.5) / leaves.Count;
            }
            else
            {
                double tbr = 0;
                for (int i = 0; i < tree.Children.Count; i++)
                {
                    tbr += tree.Children[i].GetYMultiplier();
                }
                return tbr / tree.Children.Count;
            }
        }
    }
}
