using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace CFDPrototype.util
{
    enum VT
    {
        d,
        u,
        v,
        w,
        E
    }

    enum Dim
    {
        x,y,z
    }

    struct Cell2D
    {
        //vertices (probably faster????), though indices (memory efficient) idk which is better
        int[]? vertexIndices;
        float x, y;
        Vector2[]? vertices;

        public Cell2D(int[] indices)
        {
            vertexIndices = indices;
            x = 0;
            y = 0;
            vertices = null;
        }
        public Cell2D(Vector2[] vertices)
        {
            this.vertices = vertices;
            x = 0;
            y = 0;
            vertexIndices = null;
        }

        public Cell2D(float a, float b)
        {
            x = a;
            y = b;
            vertices = null;
            vertexIndices = null;
        }

    }

    struct Cell3D
    {
        int[] vertexIndices;
        float u, v, w, p, d, e;
        public Cell3D(int[] indices, float u, float v,float w, float p, float d, float e)
        {
            vertexIndices = indices;
            this.u = u;
            this.v = v;
            this.w = w;
            this.p = p;
            this.d = d;
            this.e = e;
        }

    }

    class Grid
    {
        Vector2[] vertices;
        public Cell2D[,] cells;
        public float[,] u, v, p, d, e;
        int width;
        int height;
        float[,] TxxA;
        float[,] TxyA;
        float[,] TyyA;
        int unsetValue = 0b0_11111111_10101010101010101010101;
        int noValIDSet = 0b0_11111111_11111000000000000011111;

        public Grid(int width, int height)
        {
            this.width = width;
            this.height = height;
            //instantiate init type shih
            vertices = new Vector2[(width+1) * (height+1)];
            cells = new Cell2D[width,height];
            u = new float[width,height];
            v = new float[width, height];
            d = new float[width, height];
            e = new float[width, height];
            TxxA = new float[width + 2, height + 2];
            TxyA = new float[width + 2, height + 2];
            TyyA = new float[width + 2, height + 2];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    float b = System.Runtime.CompilerServices.Unsafe.As<int, float>(ref unsetValue);
                    TxxA[i,j] = b;
                    TxyA[i,j] = b;
                    TyyA[i,j] = b;
                    if (j == height - 1)
                    {
                        TxxA[i,j+1] = b;
                        TxyA[i,j+1] = b;
                        TyyA[i,j+1] = b;
                        TxxA[i,j + 2] = b;
                        TxyA[i,j + 2] = b;
                        TyyA[i,j + 2] = b;
                        if (i == width - 1)
                        {
                            TxxA[i + 1,j + 1] = b;
                            TxyA[i + 1,j + 1] = b;
                            TyyA[i + 1,j + 1] = b;
                            TxxA[i + 2,j + 1] = b;
                            TxyA[i + 2,j + 1] = b;
                            TyyA[i + 2,j + 1] = b;

                            TxxA[i + 1,j + 2] = b;
                            TxyA[i + 1,j + 2] = b;
                            TyyA[i + 1,j + 2] = b;
                            TxxA[i + 2,j + 2] = b;
                            TxyA[i + 2,j + 2] = b;
                            TyyA[i + 2,j + 2] = b;
                        }
                    }

                    cells[i,j] = new Cell2D(i, j);
                    u[i,j] = (float)Math.Sin(Math.PI * i / width);
                    v[i,j] = (float)Math.Sin(Math.PI * j / height);
                    d[i,j] = 1.293f;
                    e[i,j] = 0.718f * 30 + 0.5f*((float)Math.Pow(u[i,j], 2) + (float)Math.Pow(v[i,j], 2));
                }
            }
        }
        
        //currently in 2d so dim could be a bool but that just seems kinda wierd
        float Scheme(float[,] value, int i, int j, byte valId, byte dim, bool forwards)
        {
            float val = 0;
            switch (dim)
            {
                //x
                case 0:
                    //boundary cond
                    if (i + (forwards ? 1:-1) < 0 || i + (forwards ? 1 : -1) >= width)
                    {
                        switch (valId)
                        {
                            //d
                            case 0:
                                val = value[i,j]; // + neumann * dx / 2
                                break;
                            //u
                            case 1:
                                val = (value[i,j]) / 2;
                                break;
                            //v
                            case 2:
                                val = (value[i,j]) / 2;
                                break;
                            //p
                            case 3:
                                val = value[i,j]; // + neumann * dx / 2
                                break;
                            //e
                            case 4:
                                val = value[i,j]; // + neumann * dx / 2
                                break;
                        }
                    } else
                    {
                        val = (value[i,j] + value[i + (forwards ? 1 : -1),j]) / 2;
                    }
                    break;
                //y
                case 1:
                    //boundary cond
                    if (j + (forwards ? 1 : -1) < 0 || j + (forwards ? 1 : -1) >= height)
                    {
                        switch (valId)
                        {
                            //d
                            case 0:
                                val = value[i,j]; // + neumann * dx / 2
                                break;
                            //u
                            case 1:
                                val = (value[i,j]) / 2;
                                break;
                            //v
                            case 2:
                                val = (value[i,j]) / 2;
                                break;
                            //p
                            case 3:
                                val = value[i,j]; // + neumann * dx / 2
                                break;
                            //e
                            case 4:
                                val = value[i,j]; // + neumann * dx / 2
                                break;
                        }
                    }
                    else
                    {
                        val = (value[i,j] + value[i,j + (forwards ? 1 : -1)]) / 2;
                    }
                    break;
            }
                
            return val;
        }

        float BC(float[,] field, int i, int j, VT valId, Dim dim, sbyte dir)
        {
            switch (dim)
            {
                case Dim.x:
                    if (i+dir < 0 || i+dir >= width)
                    {
                        switch (valId)
                        {
                            //𝜌
                            case VT.d:
                                return d[i,j];
                            //u
                            case VT.u:
                                return 0;
                            //v
                            case VT.v:
                                return 0;
                            //E
                            case VT.E:
                                return e[i,j];
                            default:
                                return System.Runtime.CompilerServices.Unsafe.As<int, float>(ref noValIDSet);

                        }
                    } else
                    {
                        return field[i+dir,j];
                    }
                case Dim.y:
                    if (j + dir < 0 || j + dir >= height)
                    {
                        switch (valId)
                        {
                            //𝜌
                            case VT.d:
                                return d[i,j];
                            //u
                            case VT.u:
                                return 0;
                            //v
                            case VT.v:
                                return 0;
                            //E
                            case VT.E:
                                return e[i,j];
                            default:
                                return System.Runtime.CompilerServices.Unsafe.As<int, float>(ref noValIDSet);
                        }
                    }
                    else
                    {
                        return field[i,j+dir];
                    }
            }
            return 4678231647923164812;
        }

        public void tensorSetCheck(int i, int j)
        {
            int ti = i + 1;
            int tj = j + 1;
            if ((i<0 || i>=width || j<0 || j>=height))
            {
                TxxA[ti,tj] = 0;
                TxyA[ti,tj] = 0;
                TyyA[ti,tj] = 0;
                return;
            }
            if (System.Runtime.CompilerServices.Unsafe.As<float, int>(ref TxxA[ti,tj]) == unsetValue)
            {
                float uDx = (u[i,j] > 0) ? (BC(u, i, j, VT.u, Dim.x, 1) - u[i,j]) / 1 : (u[i,j] - BC(u, i, j, VT.u, Dim.x, -1)) / 1;
                float uDy = (v[i,j] > 0) ? (BC(u, i, j, VT.u, Dim.y, 1) - u[i,j]) / 1 : (u[i,j] - BC(u, i, j, VT.u, Dim.y, -1)) / 1;
                float vDx = (u[i,j] > 0) ? (BC(v, i, j, VT.v, Dim.x, 1) - v[i,j]) / 1 : (v[i,j] - BC(v, i, j, VT.v, Dim.x, -1)) / 1;
                float vDy = (v[i,j] > 0) ? (BC(v, i, j, VT.v, Dim.y, 1) - v[i,j]) / 1 : (v[i,j] - BC(v, i, j, VT.v, Dim.y, -1)) / 1;
                //∇*u
                float divU = uDx + vDy;

                TxxA[ti,tj] = ((2f / 3f) * 0.0000186f) * divU + 2 * 0.0000186f * uDx;
                TyyA[ti,tj] = ((2f / 3f) * 0.0000186f) * divU + 2 * 0.0000186f * vDy;
                TxyA[ti,tj] = 0.0000186f * (uDy + vDx);
                if (float.IsNaN(uDx + uDy + vDx + vDy))
                {
                  //  Console.WriteLine(System.Runtime.CompilerServices.Unsafe.As<float, int>(ref u[0,0]));
                //    Console.WriteLine(uDx + uDy + vDx + vDy);
                   throw new Exception();
                }
            }
        }
        
        public void TimeStep(float dt)
        {
            float[,] nd = new float[width, height];
            Array.Copy(d, 0, nd, 0, width * height);
            float[,] nu = new float[width, height];
            Array.Copy(u, 0, nu, 0, width * height);
            float[,] nv = new float[width, height];
            Array.Copy(v, 0, nv, 0, width * height);
            float[,] ne = new float[width, height];
            Array.Copy(e, 0, ne, 0, width * height);
            float[,] p = new float[width,height];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    p[i,j] = d[i,j] * 0.286f * ((e[i,j] - ((float)Math.Pow(u[i,j], 2) + (float)Math.Pow(v[i,j], 2))) / 0.718f);

                    // Console.WriteLine("i:" + i + " j:" + j);
                    tensorSetCheck(i, j);
                 //   Console.Write("A");
                    tensorSetCheck(i - 1, j);
                 //   Console.Write("B");
                    tensorSetCheck(i + 1, j);
                 //   Console.Write("C");
                    tensorSetCheck(i, j + 1);
                //    Console.Write("D");
                    tensorSetCheck(i, j - 1);
                    int ti = i + 1;
                    int tj = j + 1;
                    //lazy schmazy calculations for dx are left out rn
                    nd[i,j] -= dt * ((Scheme(d,i,j,0,0,true)*Scheme(u, i, j, 1, 0, true) - Scheme(d, i, j, 0, 0, false)* Scheme(u, i, j, 1, 0, false))/1
                        +(Scheme(d, i, j, 0, 1, true) * Scheme(v, i, j, 2, 1, true) - Scheme(d, i, j, 0, 1, false) * Scheme(v, i, j, 2, 1, false)/1));
                    nu[i,j] -= dt * (((Scheme(d, i, j, 0, 0, true) * (float)Math.Pow(Scheme(u, i, j, 1, 0, true), 2)) - (Scheme(d, i, j, 0, 0, false) * (float)Math.Pow(Scheme(u, i, j, 0, 0, false), 2))) / 1 +
                        (Scheme(d, i, j, 0, 1, true) * Scheme(u, i, j, 1, 1, true) * Scheme(v, i, j, 2, 1, true) - Scheme(d, i, j, 0, 1, false) * Scheme(u, i, j, 1, 1, false) * Scheme(v, i, j, 2, 1, false)) / 1
                        + ((TxxA[ti,tj]+ TxxA[ti + 1,tj])/2 - (TxxA[ti,tj] + TxxA[ti - 1,tj])/2)/1 +
                        ((TxxA[ti,tj] + TxxA[ti,tj+1]) / 2 - (TxxA[ti,tj] + TxxA[ti,tj-1]) / 2) / 1);
                    nv[i, j] -= dt * ((Scheme(d,i,j,0,0,true)*Scheme(u,i,j,1,0,true)*Scheme(v,i,j,2,0,true)-Scheme(d, i, j, 0, 0, false) * Scheme(u, i, j, 1, 0, false) * Scheme(v, i, j, 2, 0, false))/1
                        + (Scheme(d,i,j,0,1,true)*(float)Math.Pow(Scheme(v,i,j,2,1,true),2f)- Scheme(d, i, j, 0, 1, false) * (float)Math.Pow(Scheme(v, i, j, 2, 1, false), 2f)) /1
                        + ((TxyA[ti, tj] + TxyA[ti + 1, tj]) / 2 - (TxyA[ti, tj] + TxyA[ti - 1, tj]) / 2) / 1
                        + ((TyyA[ti, tj] + TyyA[ti, tj + 1]) / 2 - (TyyA[ti, tj] + TyyA[ti, tj - 1]) / 2) / 1);
                    //+ visc terms and oressyre graduebt;
                }
            }
            Array.Copy(nd, 0, d, 0, width*height);
            Array.Copy(nu, 0, u, 0, width*height);
            Array.Copy(nv, 0, v, 0, width * height);
        }
    }
}
