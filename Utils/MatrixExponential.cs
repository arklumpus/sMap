using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics;
using System;

//Adapted from https://github.com/horribleheffalump/MatrixExponential/blob/master/MatrixExponential.cs
namespace MatrixExponential
{
    public static partial class MatrixExtensions
    {
        public static Matrix<double> PadeExponential(this Matrix<double> m)
        {
            Matrix<double> exp_m = null;

            // last hope: Padé approximation method
            // details could be found in 
            // M.Arioli, B.Codenotti, C.Fassino The Padé method for computing the matrix exponential // Linear Algebra and its Applications, 1996, V. 240, P. 111-130
            // https://www.sciencedirect.com/science/article/pii/0024379594001901

            //Assume that matrix is not diagonalizable (otherwise, FastExponential would have succeded

            int p = 5; // order of Padé 

            // high matrix norm may result in high roundoff erroros, 
            // so first we have to find normalizing coefficient such that || m / norm_coeff || < 0.5
            // to reduce the following computations we set it norm_coeff = 2^k

            double k = 0;
            double mNorm = m.L1Norm();
            if (mNorm > 0.5)
            {
                k = Math.Ceiling(Math.Log(mNorm) / Math.Log(2.0));
                m = m / Math.Pow(2.0, k);
            }

            Matrix<double> N = DenseMatrix.CreateIdentity(m.RowCount);
            Matrix<double> D = DenseMatrix.CreateIdentity(m.RowCount);
            Matrix<double> m_pow_j = m;

            int q = p; // here we use simmetric approximation, but in general p may not be equal to q.
            for (int j = 1; j <= Math.Max(p, q); j++)
            {
                if (j > 1)
                    m_pow_j = m_pow_j * m;
                if (j <= p)
                    N = N + SpecialFunctions.Factorial(p + q - j) * SpecialFunctions.Factorial(p) / SpecialFunctions.Factorial(p + q) / SpecialFunctions.Factorial(j) / SpecialFunctions.Factorial(p - j) * m_pow_j;
                if (j <= q)
                    D = D + Math.Pow(-1.0, j) * SpecialFunctions.Factorial(p + q - j) * SpecialFunctions.Factorial(q) / SpecialFunctions.Factorial(p + q) / SpecialFunctions.Factorial(j) / SpecialFunctions.Factorial(q - j) * m_pow_j;
            }

            // calculate inv(D)*N with LU decomposition
            exp_m = D.LU().Solve(N);

            // denormalize if need
            if (k > 0)
            {
                for (int i = 0; i < k; i++)
                {
                    exp_m = exp_m * exp_m;
                }
            }

            return exp_m;
        }
    }
}