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
        int[] vertexIndices;
        float x, y, u, v, p, d;

        public Cell2D(int[] indices, float u, float v, float p, float d)
        {
            vertexIndices = indices;
            this.u = u;
            this.v = v;
            this.p = p;
            this.d = d;
            x = 1;
            y = 1;
        }
    }

    struct Cell3D
    {
        int[] vertexIndices;
        float x, y, z, u, v, w, p, d;
        public Cell3D(int[] indices, float u, float v,float w, float p, float d)
        {
            vertexIndices = indices;
            this.u = u;
            this.v = v;
            this.w = w;
            this.p = p;
            this.d = d;
            x = 1;
            y = 1;
            z = 1;
        }

    }

    internal class Grid
    {
        Vector2[] vertices;
        Cell2D[] cells;

        Grid(int width, int height)
        {
            vertices = new Vector2[width * height];
            cells = new Cell2D[width * height];
        }
    }
}
