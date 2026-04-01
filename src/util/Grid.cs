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
        E,
        T
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
        public float[,] TxxA;
        public float[,] TxyA;
        public float[,] TyyA;
        float[,] qx,qy;
        float[,] T;
        float dx = 0.1f;
        float dy = 0.1f;
        int unsetValue = 0b0_11111111_10101010101010101010101;
        int noValIDSet = 0b0_11111111_11111000000000000011111;
        int pressureToggle = 1;
        //System.Runtime.CompilerServices.Unsafe.As<int, float>(ref unsetValue)
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
            p = new float[width + 2, height + 2];
            TxxA = new float[width + 2, height + 2];
            TxyA = new float[width + 2, height + 2];
            TyyA = new float[width + 2, height + 2];
            qx = new float[width + 2, height + 2];
            qy = new float[width + 2, height + 2];
            T = new float[width + 2, height + 2];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    float b = 0;
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
                    u[i,j] = 3f*(float)Math.Sin(Math.PI * i / width);
                    v[i,j] = 3f*(float)Math.Sin(Math.PI * j / height);
                    d[i,j] = 1.293f;
                    e[i,j] = 0.718f * 16f + 0.5f*((float)Math.Pow(u[i,j], 2) + (float)Math.Pow(v[i,j], 2));
                }
            }
        }

        //1st order central diff
        float CD(float[,] field, int i, int j, VT valId, Dim dim, bool forwards)
        {
            float val = 0;
            val = (field[i, j] + BC(field, i, j, valId, dim, forwards ? (sbyte)1 : (sbyte)-1)) / 2f;

            return val;
        }

        //1st order upwind
        float FOU(float[,] field, int i, int j, VT valId, Dim dim, bool forwards)
        {
            float val;
            if (forwards)
            {
                val = (dim == Dim.x) ? (u[i, j] >= 0 ? field[i,j] : BC(field,i,j,valId,dim,1)) : (v[i, j] >= 0 ? field[i, j] : BC(field, i, j, valId, dim, 1));
            } else
            {
                val = (dim == Dim.x) ? (u[i, j] >= 0 ? BC(field, i, j, valId, dim, -1) : field[i,j]) : (v[i, j] >= 0 ? BC(field, i, j, valId, dim, -1) : field[i,j]);
            }
            return val;
        }
        
        //second order upwind
        float SOU(float[,] field, int i, int j, VT valId, Dim dim, bool forwards)
        {
            float val = 0;
            if (dim == Dim.x)
            {
                val = forwards ? ((u[i, j] > 0) ? field[i, j] + (field[i, j] - BC(field, i, j, valId, dim, -1)) / 2f : BC(field, i, j, valId, dim, 1) - (BC(field, i, j, valId, dim, 1) - field[i, j]) / 2f)
                    : ((u[i, j] > 0) ? BC(field,i,j,valId,dim,-1) + (BC(field,i,j,valId,dim,-1)-BC(field, i, j, valId, dim, -2)) /2f : field[i, j] - (field[i, j] - BC(field, i, j, valId, dim, -1)) / 2f);
                    ;
            } else
            {
                val = forwards ? ((v[i, j] > 0) ? field[i, j] + (field[i, j] - BC(field, i, j, valId, dim, -1)) / 2f : BC(field, i, j, valId, dim, 1) - (BC(field, i, j, valId, dim, 1) - field[i, j]) / 2f)
    : ((v[i, j] > 0) ? BC(field, i, j, valId, dim, -1) + (BC(field, i, j, valId, dim, -1) - BC(field, i, j, valId, dim, -2)) / 2f : field[i, j] - (field[i, j] - BC(field, i, j, valId, dim, -1)) / 2f);
                ;
            }
            return val;
        }

        //currently in 2d so dim could be a bool but that just seems kinda wierd --deprecated
        float Scheme(float[,] value, int i, int j, VT valId, Dim dim, bool forwards)
        {
            float val = 0;
            switch (dim)
            {
                //x
                case Dim.x:
                    //boundary cond
                    if (i + (forwards ? 1:-1) < 0 || i + (forwards ? 1 : -1) >= width)
                    {
                        switch (valId)
                        {
                            //d
                            case VT.d:
                                val = value[i,j]; // + neumann * dx / 2
                                break;
                            //u
                            case VT.u:
                                val = (value[i,j]) / 2f;
                                break;
                            //v
                            case VT.v:
                                val = (value[i,j]) / 2f;
                                break;
                            //e
                            case VT.E:
                                val = value[i,j]; // + neumann * dx / 2
                                break;
                        }
                    } else
                    {
                        val = (value[i,j] + value[i + (forwards ? 1 : -1),j]) / 2f;
                    }
                    break;
                //y
                case Dim.y:
                    //boundary cond
                    if (j + (forwards ? 1 : -1) < 0 || j + (forwards ? 1 : -1) >= height)
                    {
                        switch (valId)
                        {
                            //d
                            case VT.d:
                                val = value[i,j]; // + neumann * dx / 2
                                break;
                            //u
                            case VT.u:
                                val = (value[i,j]) / 2f;
                                break;
                            //v
                            case VT.v:
                                val = (value[i,j]) / 2f;
                                break;
                            //e
                            case VT.E:
                                val = value[i,j]; // + neumann * dx / 2
                                break;
                        }
                    }
                    else
                    {
                        val = (value[i,j] + value[i,j + (forwards ? 1 : -1)]) / 2f;
                    }
                    break;
            }
                
            return val;
        }

        float BC(float[,] field, int i, int j, VT valId, Dim dim, sbyte dir)
        {
            if (i < 0 || i >= width || j < 0 || j >= height)
            {
                switch (valId)
                {
                    //𝜌
                    case VT.d:
                        return d[Math.Clamp(i, 0, width - 1), Math.Clamp(j, 0, height - 1)];
                    //u
                    case VT.u:
                        return 0;
                    //v
                    case VT.v:
                        return 0;
                    //E
                    case VT.E:
                        return e[Math.Clamp(i, 0, width - 1), Math.Clamp(j, 0, height - 1)];
                    case VT.T:
                        return e[Math.Clamp(i, 0, width - 1), Math.Clamp(j, 0, height - 1)]/0.718f;
                    default:
                        return System.Runtime.CompilerServices.Unsafe.As<int, float>(ref noValIDSet);

                }
            }
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
                            case VT.T:
                                return e[i,j]/0.718f;
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
                            case VT.T:
                                return e[i,j]/0.718f;
                            default:
                                return System.Runtime.CompilerServices.Unsafe.As<int, float>(ref noValIDSet);
                        }
                    }
                    else
                    {
                        return field[i,j+dir];
                    }
            }
            return 0;
        }

        public void calcStressTensor(int i, int j)
        {
            int ti = i + 1;
            int tj = j + 1;
            float uDx = 0;
            float uDy = 0;
            float vDx = 0;
            float vDy = 0;
            float divU = 0;
            if ((i < 0 || i >= width || j < 0 || j >= height))
            {
                uDx = (u[Math.Clamp(i, 0, width - 1), Math.Clamp(j, 0, height - 1)] < 0) ? (BC(u, i, j, VT.u, Dim.x, 1) - BC(u, i, j, VT.u, Dim.x, 0)) / dx : (BC(u, i, j, VT.u, Dim.x, 0) - BC(u, i, j, VT.u, Dim.x, -1)) / dx;
                uDy = (v[Math.Clamp(i, 0, width - 1), Math.Clamp(j, 0, height - 1)] < 0) ? (BC(u, i, j, VT.u, Dim.y, 1) - BC(u, i, j, VT.u, Dim.y, 0)) / dy : (BC(u, i, j, VT.u, Dim.y, 0) - BC(u, i, j, VT.u, Dim.y, -1)) / dy;
                vDx = (u[Math.Clamp(i, 0, width - 1), Math.Clamp(j, 0, height - 1)] < 0) ? (BC(v, i, j, VT.v, Dim.x, 1) - BC(v, i, j, VT.v, Dim.x, 0)) / dx : (BC(v, i, j, VT.v, Dim.x, 0) - BC(v, i, j, VT.v, Dim.x, -1)) / dx;
                vDy = (v[Math.Clamp(i, 0, width - 1), Math.Clamp(j, 0, height - 1)] < 0) ? (BC(v, i, j, VT.v, Dim.y, 1) - BC(v, i, j, VT.v, Dim.y, 0)) / dy : (BC(v, i, j, VT.v, Dim.y, 0) - BC(v, i, j, VT.v, Dim.y, -1)) / dy;

                divU = uDx + vDy;
            }//todo: add check for if value has already been calculated this timestep 
            else if (true)
            {
                uDx = (u[i, j] < 0) ? (BC(u, i, j, VT.u, Dim.x, 1) - u[i, j]) / dx : (u[i, j] - BC(u, i, j, VT.u, Dim.x, -1)) / dx;
                uDy = (v[i, j] < 0) ? (BC(u, i, j, VT.u, Dim.y, 1) - u[i, j]) / dy : (u[i, j] - BC(u, i, j, VT.u, Dim.y, -1)) / dy;
                vDx = (u[i, j] < 0) ? (BC(v, i, j, VT.v, Dim.x, 1) - v[i, j]) / dx : (v[i, j] - BC(v, i, j, VT.v, Dim.x, -1)) / dx;
                vDy = (v[i, j] < 0) ? (BC(v, i, j, VT.v, Dim.y, 1) - v[i, j]) / dy : (v[i, j] - BC(v, i, j, VT.v, Dim.y, -1)) / dy;
                //∇*u
                divU = uDx + vDy;
            }
            TxxA[ti, tj] = ((2f / 3f) * 0.0000186f) * divU + 2f * 0.0000186f * uDx;
            TyyA[ti, tj] = ((2f / 3f) * 0.0000186f) * divU + 2f * 0.0000186f * vDy;
            TxyA[ti, tj] = 0.0000186f * (uDy + vDx);
            if (float.IsNaN(uDx) || float.IsNaN(uDy) || float.IsNaN(vDx) || float.IsNaN(vDy))
            {
              //  Console.WriteLine(System.Runtime.CompilerServices.Unsafe.As<float, int>(ref u[0, 0]));
             //   Console.WriteLine(uDx + uDy + vDx + vDy);
              //  throw new Exception();
            }
            return;
        }

        void calcTemperature(int i, int j)
        {
            float val;
            if ((i < 0 || i >= width || j < 0 || j >= height))
            {
                val = (BC(e, i, j, VT.E, Dim.x, 0) - ((float)Math.Pow(BC(u, i, j, VT.u, Dim.x, 0), 2) + (float)Math.Pow(BC(v, i, j, VT.v, Dim.x, 0), 2))) / 0.718f;
            } else
            {
                val = (e[i, j] - ((float)Math.Pow(u[i, j], 2) + (float)Math.Pow(v[i, j], 2))) / 0.718f;
            }
            T[i + 1, j + 1] = val;
        }

        void calcPressure(int i, int j)
        {
            int ti = i + 1;
            int tj = j + 1;
            float val;
            if ((i < 0 || i >= width || j < 0 || j >= height))
            {
                val = BC(d, i, j, VT.d, Dim.x, 0) * 0.286f * T[i + 1, j + 1];
            } else
            {
                val = d[i, j] * 0.286f * T[i + 1, j + 1];
            }
            p[ti,tj] = val;
        }

        void calcHeatFlux(int i, int j)
        {
            int ti = i + 1;
            int tj = j + 1;
            float TDx;
            float TDy;
            
            if ((i < 0 || i >= width || j < 0 || j >= height))
            {
                bool b1 = ti == 0;
                bool b2 = ti == width;
                bool b3 = tj == 0;
                bool b4 = tj == height;
                TDx = BC(u, i, j, VT.u, Dim.x, 0) < 0 ? (T[b2 ? ti : (ti + 1), tj] - T[ti, tj]) / dx : (T[ti, tj] - T[b1 ? ti : (ti - 1), tj]) / dx;
                TDy = BC(v, i, j, VT.v, Dim.y, 0) < 0 ? (T[ti, b4 ? tj : (tj + 1)] - T[ti, tj]) / dy : (T[ti, tj] - T[ti, b3 ? tj : (tj - 1)]) / dy;
            } else
            {
                TDx = u[i,j] < 0 ? (T[ti + 1, tj] - T[ti, tj]) / dx : (T[ti, tj] - T[ti - 1, tj]) / dx;
                TDy = v[i,j] < 0 ? (T[ti, tj + 1] - T[ti, tj]) / dy : (T[ti, tj] - T[ti, tj - 1]) / dy;
            }

            qx[ti, tj] = -0.02662f * TDx;
            qy[ti, tj] = -0.02662f * TDy;
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

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    calcTemperature(i, j);
                    calcTemperature(i + 1, j);
                    calcTemperature(i - 1, j);
                    calcTemperature(i, j + 1);
                    calcTemperature(i, j - 1);

                    calcPressure(i, j);
                    calcPressure(i + 1, j);
                    calcPressure(i - 1, j);
                    calcPressure(i, j + 1);
                    calcPressure(i, j - 1);

                    calcStressTensor(i, j);
                    calcStressTensor(i - 1, j);
                    calcStressTensor(i + 1, j);
                    calcStressTensor(i, j + 1);
                    calcStressTensor(i, j - 1);

                    calcHeatFlux(i, j);
                    calcHeatFlux(i + 1, j);
                    calcHeatFlux(i - 1, j);
                    calcHeatFlux(i, j + 1);
                    calcHeatFlux(i, j - 1);

                    int ti = i + 1;
                    int tj = j + 1;

                    //stress dim forward/backwward face (central diff)
                    float TxxXF = (TxxA[ti, tj] + TxxA[ti + 1, tj]) / 2f;
                    float TxxXB = (TxxA[ti, tj] + TxxA[ti - 1, tj]) / 2f;

                    float TxyXF = (TxyA[ti, tj] + TxyA[ti + 1, tj]) / 2f;
                    float TxyXB = (TxyA[ti, tj] + TxyA[ti - 1, tj]) / 2f;
                    float TxyYF = (TxyA[ti, tj] + TxyA[ti, tj + 1]) / 2f;
                    float TxyYB = (TxyA[ti, tj] + TxyA[ti, tj - 1]) / 2f;

                    float TyyYF = (TyyA[ti, tj] + TyyA[ti, tj + 1]) / 2f;
                    float TyyYB = (TyyA[ti, tj] + TyyA[ti, tj - 1]) / 2f;

                    //pressure dim forward/backward face (upwind)
                    float pxf = u[i, j] >= 0 ? p[ti, tj] : p[ti + 1, tj];
                    float pxb = u[i, j] >= 0 ? p[ti - 1, tj] : p[ti, tj];
                    float pyf = v[i, j] >= 0 ? p[ti, tj] : p[ti, tj + 1];
                    float pyb = v[i, j] >= 0 ? p[ti, tj - 1] : p[ti, tj];

                    //heat flux dim forward/backward face (central diff)
                    float qxXF = (qx[ti, tj] + qx[ti + 1, tj]) / 2f;
                    float qxXB = (qx[ti, tj] + qx[ti - 1, tj]) / 2f;
                    float qyYF = (qy[ti, tj] + qy[ti, tj + 1]) / 2f;
                    float qyYB = (qy[ti, tj] + qy[ti, tj - 1]) / 2f;

                    //lazy schmazy calculations for dx are left out rn
                    nd[i,j] -= dt * ((FOU(d,i,j,VT.d,Dim.x,true)*FOU(u, i, j, VT.u, Dim.x, true) - FOU(d, i, j, VT.d, Dim.x, false)* FOU(u, i, j, VT.u, Dim.x, false))/dx
                        +(FOU(d, i, j, VT.d, Dim.y, true) * FOU(v, i, j, VT.v, Dim.y, true) - FOU(d, i, j, VT.d, Dim.y, false) * FOU(v, i, j, VT.v, Dim.y, false)/dy));
                    nu[i,j] -= (1f / nd[i, j]) * dt * (((FOU(d, i, j, VT.d, Dim.x, true) * (float)Math.Pow(FOU(u, i, j, VT.u, Dim.x, true), 2)+pressureToggle*pxf) - (FOU(d, i, j, VT.d, Dim.x, false) * (float)Math.Pow(FOU(u, i, j, VT.u, Dim.x, false), 2)+0*pxb)) / dx +
                        (FOU(d, i, j, VT.d, Dim.y, true) * FOU(u, i, j, VT.u, Dim.y, true) * FOU(v, i, j, VT.v, Dim.y, true) - FOU(d, i, j, VT.d, Dim.y, false) * FOU(u, i, j, VT.u, Dim.y, false) * FOU(v, i, j, VT.v, Dim.y, false)) / dy
                        + (TxxXF - TxxXB)/dx +
                        (TxyYF - TxyYB) / dy);
                    nv[i, j] -= (1f / nd[i, j]) * dt * ((FOU(d,i,j,VT.d, Dim.x, true)*FOU(u,i,j,VT.u, Dim.x, true)*FOU(v,i,j,VT.v, Dim.x, true)-FOU(d, i, j, VT.d, Dim.x, false) * FOU(u, i, j, VT.u, Dim.x, false) * FOU(v, i, j, VT.v, Dim.x, false))/dx
                        + ((FOU(d,i,j,VT.d, Dim.y, true)*(float)Math.Pow(FOU(v,i,j,VT.v, Dim.y, true),2f)+ pressureToggle * pyf)- (FOU(d, i, j, VT.d, Dim.y, false) * (float)Math.Pow(FOU(v, i, j, VT.v, Dim.y, false), 2f)+0*pyb)) /dy
                        + (TxyXF - TxyXB) / dx
                        + (TyyYF - TyyYB) / dy);
                    ne[i, j] -= (1f / nd[i, j]) * dt * ((FOU(u, i, j, VT.u, Dim.x, true) * (FOU(d, i, j, VT.d, Dim.x, true) * FOU(e, i, j, VT.E, Dim.x, true) + pxf) - FOU(u, i, j, VT.u, Dim.x, false) * (FOU(d, i, j, VT.d, Dim.x, false) * FOU(e, i, j, VT.E, Dim.x, false) + pxb)) / dx
                        + (FOU(v, i, j, VT.v, Dim.y, true) * (FOU(d, i, j, VT.d, Dim.y, true) * FOU(e, i, j, VT.E, Dim.y, true) + pyf) - FOU(v, i, j, VT.v, Dim.y, false) * (FOU(d, i, j, VT.d, Dim.y, false) * FOU(e, i, j, VT.E, Dim.y, false) + pyb)) / dy  
                        + ((FOU(u,i,j,VT.u,Dim.x,true)*TxxXF+FOU(v,i,j,VT.v,Dim.x,true)*TxyXF-qxXF)- (FOU(u, i, j, VT.u, Dim.x, false) * TxxXB + FOU(v, i, j, VT.v, Dim.x, false) * TxyXB-qxXB)) /dx
                        + ((FOU(u, i, j, VT.u, Dim.y, true) * TxyYF + FOU(v, i, j, VT.v, Dim.y, true) * TyyYF - qyYF) - (FOU(u, i, j, VT.u, Dim.y, false) * TxyYB + FOU(v, i, j, VT.v, Dim.y, false) * TyyYB - qyYB)) / dy);
                    //+ visc terms and oressyre graduebt;
                    if (float.IsNaN(nd[i,j]) || float.IsInfinity(nd[i,j]))
                    {
                    //    nd[i, j] = 0;
                    }
                    if (float.IsNaN(nu[i, j]) || float.IsInfinity(nu[i, j]))
                    {
                    //    nu[i, j] = 0;
                    }
                    if (float.IsNaN(nv[i, j]) || float.IsInfinity(nv[i, j]))
                    {
                     //   nv[i, j] = 0;
                    }
                }
            }
            Array.Copy(nd, 0, d, 0, width*height);
            Array.Copy(nu, 0, u, 0, width*height);
            Array.Copy(nv, 0, v, 0, width * height);
            Array.Copy(ne, 0, e, 0, width * height);
        }
    }
}
