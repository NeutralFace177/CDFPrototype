using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace CFDPrototype.util
{
    struct Cell2D
    {
        public float x, y, u, v, p, d, e;

        public Cell2D(float x, float y, float u, float v, float p, float d, float e)
        {
            this.x = x;
            this.y = y;
            this.u = u;
            this.v = v;
            this.p = p;
            this.d = d;
            this.e = e;
        }

        public Cell2D(float x, float y)
        {
            this.x = x; this.y = y;
            u = 0;v = 0;p = 0;d = 0;e = 0;
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
        public Cell2D[][] cells;

        public Grid(int width, int height)
        {
            //instantiate init type shih
            vertices = new Vector2[width * height];
            cells = new Cell2D[width][];
            for (int i = 0; i < width; i++)
            {
                cells[i] = new Cell2D[height];
                for (int j = 0; j < height; j++)
                {
                    cells[i][j].x = i;
                    cells[i][j].y = j;
                    cells[i][j].u = (float)Math.Sin(Math.PI * (float)i / (float)width);
                    cells[i][j].v = (float)Math.Sin(Math.PI * (float)j / (float)height);
                    cells[i][j].d = 1.293f;
                    cells[i][j].e = 0.718f * 30 + 0.5f*((float)Math.Pow(cells[i][j].u,2) + (float)Math.Pow(cells[i][j].v, 2));
                    cells[i][j].p = 1.293f * 30 * 0.286f;
                }
            }
        }
    }
}
