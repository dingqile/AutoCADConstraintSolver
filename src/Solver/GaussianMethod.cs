using System;

namespace AutoCADConstraintSolver.Solver;

/// <summary>
/// Gaussian elimination method for solving linear systems
/// Based on NoteCAD implementation
/// </summary>
public static class GaussianMethod
{
    public const double epsilon = 1e-9;

    /// <summary>
    /// Solve a linear system Ax = b using Gaussian elimination with partial pivoting
    /// </summary>
    public static bool Solve(double[,] a, double[] b, ref double[] x)
    {
        int n = b.Length;

        if (x == null || x.Length != n)
            x = new double[n];

        // Copy input arrays
        var aCopy = new double[n, n];
        var bCopy = new double[n];

        for (int i = 0; i < n; i++)
        {
            bCopy[i] = b[i];
            for (int j = 0; j < n; j++)
            {
                aCopy[i, j] = a[i, j];
            }
        }

        // Forward elimination with partial pivoting
        for (int col = 0; col < n; col++)
        {
            // Find pivot
            int maxRow = col;
            double maxVal = Math.Abs(aCopy[col, col]);

            for (int row = col + 1; row < n; row++)
            {
                if (Math.Abs(aCopy[row, col]) > maxVal)
                {
                    maxVal = Math.Abs(aCopy[row, col]);
                    maxRow = row;
                }
            }

            // Check for singular matrix
            if (maxVal < epsilon)
                return false;

            // Swap rows
            if (maxRow != col)
            {
                for (int j = col; j < n; j++)
                {
                    (aCopy[col, j], aCopy[maxRow, j]) = (aCopy[maxRow, j], aCopy[col, j]);
                }
                (bCopy[col], bCopy[maxRow]) = (bCopy[maxRow], bCopy[col]);
            }

            // Eliminate column
            for (int row = col + 1; row < n; row++)
            {
                if (Math.Abs(aCopy[row, col]) < epsilon)
                    continue;

                double factor = aCopy[row, col] / aCopy[col, col];

                for (int j = col; j < n; j++)
                {
                    aCopy[row, j] -= factor * aCopy[col, j];
                }
                bCopy[row] -= factor * bCopy[col];
            }
        }

        // Back substitution
        for (int i = n - 1; i >= 0; i--)
        {
            if (Math.Abs(aCopy[i, i]) < epsilon)
            {
                x[i] = 0;
                continue;
            }

            double sum = bCopy[i];
            for (int j = i + 1; j < n; j++)
            {
                sum -= aCopy[i, j] * x[j];
            }
            x[i] = sum / aCopy[i, i];
        }

        return true;
    }

    /// <summary>
    /// Calculate the rank of a matrix
    /// </summary>
    public static int Rank(double[,] a)
    {
        int rows = a.GetLength(0);
        int cols = a.GetLength(1);
        int rank = 0;

        var aCopy = new double[rows, cols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                aCopy[i, j] = a[i, j];

        for (int col = 0; col < cols && rank < rows; col++)
        {
            // Find pivot
            int maxRow = rank;
            double maxVal = Math.Abs(aCopy[rank, col]);

            for (int row = rank + 1; row < rows; row++)
            {
                if (Math.Abs(aCopy[row, col]) > maxVal)
                {
                    maxVal = Math.Abs(aCopy[row, col]);
                    maxRow = row;
                }
            }

            if (maxVal < epsilon)
                continue;

            // Swap rows
            for (int j = 0; j < cols; j++)
            {
                (aCopy[rank, j], aCopy[maxRow, j]) = (aCopy[maxRow, j], aCopy[rank, j]);
            }

            // Eliminate column
            for (int row = rank + 1; row < rows; row++)
            {
                if (Math.Abs(aCopy[row, col]) < epsilon)
                    continue;

                double factor = aCopy[row, col] / aCopy[rank, col];
                for (int j = col; j < cols; j++)
                {
                    aCopy[row, j] -= factor * aCopy[rank, j];
                }
            }

            rank++;
        }

        return rank;
    }

    /// <summary>
    /// Solve a symmetric positive definite system using Cholesky decomposition
    /// </summary>
    public static bool SolveCholesky(double[,] a, double[] b, ref double[] x)
    {
        int n = b.Length;

        if (x == null || x.Length != n)
            x = new double[n];

        // Check symmetry and positive definiteness
        for (int i = 0; i < n; i++)
        {
            if (Math.Abs(a[i, i]) < epsilon)
                return false;

            for (int j = i + 1; j < n; j++)
            {
                if (Math.Abs(a[i, j] - a[j, i]) > epsilon)
                    return false;
            }
        }

        // Cholesky decomposition: A = L * L^T
        var l = new double[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                double sum = a[i, j];
                for (int k = 0; k < j; k++)
                {
                    sum -= l[i, k] * l[j, k];
                }

                if (i == j)
                {
                    if (sum < epsilon)
                        return false;
                    l[i, j] = Math.Sqrt(sum);
                }
                else
                {
                    l[i, j] = sum / l[j, j];
                }
            }
        }

        // Forward substitution: L * y = b
        var y = new double[n];
        for (int i = 0; i < n; i++)
        {
            double sum = b[i];
            for (int j = 0; j < i; j++)
            {
                sum -= l[i, j] * y[j];
            }
            y[i] = sum / l[i, i];
        }

        // Back substitution: L^T * x = y
        for (int i = n - 1; i >= 0; i--)
        {
            double sum = y[i];
            for (int j = i + 1; j < n; j++)
            {
                sum -= l[j, i] * x[j];
            }
            x[i] = sum / l[i, i];
        }

        return true;
    }

    /// <summary>
    /// Calculate matrix inverse using Gaussian elimination
    /// </summary>
    public static bool Inverse(double[,] a, out double[,] inverse)
    {
        int n = a.GetLength(0);
        if (a.GetLength(1) != n)
        {
            inverse = Array.Empty<double>();
            return false;
        }

        inverse = new double[n, n];

        // Augment with identity matrix
        var augmented = new double[n, 2 * n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                augmented[i, j] = a[i, j];
            }
            augmented[i, i + n] = 1;
        }

        // Forward elimination with partial pivoting
        for (int col = 0; col < n; col++)
        {
            // Find pivot
            int maxRow = col;
            double maxVal = Math.Abs(augmented[col, col]);

            for (int row = col + 1; row < n; row++)
            {
                if (Math.Abs(augmented[row, col]) > maxVal)
                {
                    maxVal = Math.Abs(augmented[row, col]);
                    maxRow = row;
                }
            }

            if (maxVal < epsilon)
            {
                inverse = Array.Empty<double>();
                return false;
            }

            // Swap rows
            if (maxRow != col)
            {
                for (int j = 0; j < 2 * n; j++)
                {
                    (augmented[col, j], augmented[maxRow, j]) = (augmented[maxRow, j], augmented[col, j]);
                }
            }

            // Scale pivot row
            double scale = augmented[col, col];
            for (int j = 0; j < 2 * n; j++)
            {
                augmented[col, j] /= scale;
            }

            // Eliminate column
            for (int row = 0; row < n; row++)
            {
                if (row != col && Math.Abs(augmented[row, col]) > epsilon)
                {
                    double factor = augmented[row, col];
                    for (int j = 0; j < 2 * n; j++)
                    {
                        augmented[row, j] -= factor * augmented[col, j];
                    }
                }
            }
        }

        // Extract inverse from augmented matrix
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                inverse[i, j] = augmented[i, j + n];
            }
        }

        return true;
    }
}
