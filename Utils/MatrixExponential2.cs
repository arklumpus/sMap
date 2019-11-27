using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics;
using System;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using Utils;

namespace MatrixExponential
{
    public class MatrixExponential
    {
        public Matrix<double> Result;
        public bool Exact;
        public Matrix<Complex> P;
        public Matrix<Complex> PInv;
        public Matrix<Complex> D;

        public MatrixExponential(Matrix<double> result, Matrix<Complex> p, Matrix<Complex> pInv, Matrix<Complex> d)
        {
            Result = result;
            P = p;
            PInv = pInv;
            Exact = p != null;
            D = d;
        }
    }

    public static partial class MatrixExtensions
    {
        static bool ForcePade = false;

        public static MatrixExponential FastExponential(this Matrix<double> mat, double t, MatrixExponential cachedResult = null)
        {
            if (ForcePade)
            {
                return new MatrixExponential((mat * t).PadeExponential().PointwiseAbs(), null, null, null);
            }

            if (cachedResult == null)
            {

                Matrix<Complex> m = mat.ToComplex();
                Evd<Complex> evd = m.Evd();

                HashSet<Complex> eigenValues = new HashSet<Complex>();

                for (int i = 0; i < evd.EigenValues.Count; i++)
                {
                    if (Math.Abs(evd.EigenValues[i].Real) < 1e-5)
                    {
                        evd.EigenValues[i] = new Complex(0, evd.EigenValues[i].Imaginary);
                    }

                    if (Math.Abs(evd.EigenValues[i].Imaginary) < 1e-5)
                    {
                        evd.EigenValues[i] = new Complex(evd.EigenValues[i].Real, 0);
                    }

                    eigenValues.Add(evd.EigenValues[i]);
                }

                if (eigenValues.Count == m.ColumnCount)
                {
                    //Diagonalizable

                    Matrix<Complex> inv = evd.EigenVectors.Inverse();
                    Matrix<Complex> eig = evd.EigenVectors;
                    Matrix<Complex> diag = Matrix<Complex>.Build.DenseOfDiagonalVector(evd.EigenValues);

                    return new MatrixExponential((eig * (diag * t).DiagonalExp() * inv).Real().PointwiseAbs(), eig, inv, diag);
                }
                else
                {
                    //Might not diagonalizable: fallback to Padé approximation [note: this happens "almost never"]
                    return new MatrixExponential((mat * t).PadeExponential().PointwiseAbs(), null, null, null);
                }
            }
            else
            {
                if (cachedResult.Exact)
                {
                    return new MatrixExponential((cachedResult.P * (cachedResult.D * t).DiagonalExp() * cachedResult.PInv).Real().PointwiseAbs(), cachedResult.P, cachedResult.PInv, cachedResult.D);
                }
                else
                {
                    return new MatrixExponential((mat * t).PadeExponential().PointwiseAbs(), null, null, null);
                }
            }
        }

        static Matrix<Complex> DiagonalExp(this Matrix<Complex> m)
        {
            Matrix<Complex> tbr = Matrix<Complex>.Build.DenseOfMatrix(m);

            for (int i = 0; i < m.ColumnCount; i++)
            {
                tbr[i, i] = tbr[i, i].Exp();
            }

            return tbr;
        }


        public static void TimesLogVectorAndAdd(this Matrix<double> mat, double[] logVector, double[] addToVector)
        {
            int maxInd = logVector.MaxInd();

            for (int i = 0; i < mat.RowCount; i++)
            {
                double toBeAdded = logVector[maxInd] + Math.Log(mat[i, maxInd]);

                double log1pArg = 0;

                for (var j = 0; j < mat.ColumnCount; j++)
                {
                    if (j != maxInd)
                    {
                        log1pArg += mat[i, j] / mat[i, maxInd] * Math.Exp(logVector[j] - logVector[maxInd]);
                    }
                }

                if (!double.IsNaN(log1pArg))
                {
                    toBeAdded += Utils.Utils.Log1p(log1pArg);
                    addToVector[i] += toBeAdded;
                    if (addToVector[i] > 0)
                    {
                        addToVector[i] = double.NaN;
                    }
                }
                else
                {
                    double logArg = 0;
                    for (var j = 0; j < mat.ColumnCount; j++)
                    {
                        logArg += mat[i, j] * Math.Exp(logVector[j]);
                    }

                    addToVector[i] += Math.Log(logArg);

                    if (addToVector[i] > 0)
                    {
                        addToVector[i] = double.NaN;
                    }
                }
            }
        }
    }
}