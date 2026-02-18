using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Collections;

namespace CFDPrototype.util
{
    internal class Matrix
    {
        float[][] data;
        int m, n;
        public Matrix(int m, int n)
        {
            this.m = m;this.n = n;
            data = new float[m][];
        }

        public Matrix(float[][] data)
        {
            this.data = data;
            m = data.Length;
            n = data[0].Length;
        }

        public Matrix(float[,] data)
        {
            m = data.GetLength(0);
            n = data.GetLength(1);
            this.data = new float[m][];
            for (int i = 0; i < m; i++)
            {
                this.data[i] = new float[n];
                for (int j = 0; j < n; j++)
                {
                    this.data[i][j] = data[i,j];
                }
            }
        }

        public Matrix(float[] data, int m)
        {
            if (data.Length % m != 0)
            {
                throw new ArgumentException("data arr length not divisible by given m-dimension");
            }
        }

        public static Matrix Identity(int size)
        {
            Matrix matrix = new Matrix(size,size);
            for (int i = 0; i < size; i++)
            {
                matrix.data[i][i] = 1;
            }
            return matrix;
        }


        public Matrix Transpose()
        {
            Matrix matrix = new Matrix(n,m); 
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    matrix.data[i][j] = data[j][i];
                }
            }
            return matrix;
        }

        public Matrix T()
        {
            return Transpose();
        }

        public Matrix Submatrix(int i, int j)
        {
            Matrix matrix = new Matrix(m - 1, n - 1);
            int a = 0;
            for (int k = 0; k < m-1; k++)
            {
                int b = 0;
                if (k == i-1) a++;
                for (int l = 0; l < n-1; l++)
                {
                    if (l == j-1) b++;
                    matrix.data[k][l] = data[a][b];
                    b++;
                }
                a++;
            }
            return matrix;
        }

        public float Trace()
        {
            if (m!=n)
            {
                throw new Exception("Trace is only defined for square matrices");
            }
            float result = data[0][0];
            for (int i = 1; i<m;i++)
            {
                result += data[i][i];
            }
            return result;
        }

        public float Determinant()
        {
            if (m!=n)
            {
                throw new Exception("Determinant is only defined for square matrices");
            }
            if (m==2)
            {
                return data[0][0] * data[1][1] - data[0][1] * data[1][0];
            } else if (m==3)
            {
                return data[0][0] * data[1][1] * data[2][2] + data[0][1] * data[1][2] * data[2][0] + data[0][2] * data[1][0] * data[2][1] - data[2][0] * data[1][1] * data[0][2] - data[2][1] * data[1][2] * data[0][0] - data[2][2] * data[1][0] * data[0][1];
            } else
            {
                return -1;
            }
        }

        public float Det()
        {
            return Determinant();
        }

        public Matrix SwapRow(int i1, int i2)
        {
            int a = i1 - 1;
            int b = i2 - 1;
            float[] temp = data[a];
            data[a] = data[b];
            data[b] = temp;
            return this;
        }

        public Matrix SwapColumn(int j1, int j2)
        {
            int a = j1 - 1;
            int b = j2 - 1;
            for (int i = 0; i < m; i++)
            {
                float temp = data[i][a];
                data[i][a] = data[i][b];
                data[i][b] = temp;
            }

            return this;
        }

        public Matrix GuassianElim()
        {
            return Matrix.Identity(2);
        }

        //DO MORE FOR TS
        public override string ToString()
        {
            string str = "";
            string[] c = new string[m];
            string l = "";
            for (int j = 0; j < n; j++)
            {
                int len = 0;
                string[] d = new string[m];
                for (int i = 0; i < m; i++)
                {
                    d[i] += data[i][j];
                    if (len < data[i][j].ToString().Length) len = data[i][j].ToString().Length;
                }
                for (int i = 0; i < m; i++)
                {
                    c[i] += d[i] + new string(' ', len - d[i].Length) + " | ";
                }
            }
            string u = new string('-', c[0].Length);
            for (int i = 0; i < c.Length-1; i++)
            {
                str += c[i] + "\n" + u + "\n";
            }
            str += c[c.Length-1];
            return str;
        }

        public float this[int x,int y]
        {
            get => data[x][y];
            set => data[x][y] = value;
        }

        public static Matrix operator +(Matrix a, Matrix  b)
        {
            if (a.m != b.m || a.n != b.n)
            {
                throw new Exception("Matrices Must Be Same Size For Addition");
            }
            Matrix c = new Matrix(a.m, a.n);
            for (int i = 0; i < a.m; i++)
            {
                for (int j = 0; j < a.n; j++)
                {
                    c.data[i][j] = a.data[i][j] + b.data[i][j];
                }
            }
            return c;
        }

        public static Matrix operator +(Matrix a, float s)
        {
            Matrix c = new Matrix(a.m, a.n);
            for (int i = 0; i < a.m; i++)
            {
                for (int j = 0; j < a.n; j++)
                {
                    c.data[i][j] = a.data[i][j] + s;
                }
            }
            return c;
        }

        public static Matrix operator +(float s, Matrix a)
        {
            Matrix c = new Matrix(a.m, a.n);
            for (int i = 0; i < a.m; i++)
            {
                for (int j = 0; j < a.n; j++)
                {
                    c.data[i][j] = a.data[i][j] + s;
                }
            }
            return c;
        }

        public static Matrix operator -(Matrix a, Matrix b)
        {
            if (a.m != b.m || a.n != b.n)
            {
                throw new Exception("Matrices Must Be Same Size For Addition");
            }
            Matrix c = new Matrix(a.m, a.n);
            for (int i = 0; i < a.m; i++)
            {
                for (int j = 0; j < a.n; j++)
                {
                    c.data[i][j] = a.data[i][j] - b.data[i][j];
                }
            }
            return c;
        }

        public static Matrix operator -(Matrix a, float s)
        {
            Matrix c = new Matrix(a.m, a.n);
            for (int i = 0; i < a.m; i++)
            {
                for (int j = 0; j < a.n; j++)
                {
                    c.data[i][j] = a.data[i][j] - s;
                }
            }
            return c;
        }

        public static Matrix operator -(float s, Matrix a)
        {
            Matrix c = new Matrix(a.m, a.n);
            for (int i = 0; i < a.m; i++)
            {
                for (int j = 0; j < a.n; j++)
                {
                    c.data[i][j] = a.data[i][j] - s;
                }
            }
            return c;
        }

        //TODO implement strassen/BLAS algorithm 

        public static Matrix operator *(Matrix a, Matrix b)
        {
            if (a.n != b.m)
            {
                throw new Exception($"Invalid Matrix Dimensions For Multiplication A:{a.m},{a.n} B:{b.m},{b.n}");
            }
            Matrix c = new Matrix(a.m, a.n);
            for (int i = 0; i < a.m; i++)
            {
                for (int j = 0; j < b.n; j++)
                {
                    for (int k = 0; k < a.n; k++)
                    {
                        c.data[i][j] += a.data[i][k] * b.data[k][j];
                    }
                }
            }
            return c;
        }


        public static Matrix operator *(Matrix a, float s)
        {

            Matrix c = new Matrix(a.m, a.n);
            for (int i = 0; i < a.m; i++)
            {
                for (int j = 0; j < a.n; j++)
                {
                    c.data[i][j] = a.data[i][j] * s;
                }
            }
            return c;
        }

        public static Matrix operator *(float s, Matrix a)
        {

            Matrix c = new Matrix(a.m, a.n);
            for (int i = 0; i < a.m; i++)
            {
                for (int j = 0; j < a.n; j++)
                {
                    c.data[i][j] = a.data[i][j] * s;
                }
            }
            return c;
        }


    }
}