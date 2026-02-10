using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace CFDPrototype
{
    internal class Grid
    {
        Vector2[] vertices;
        Cell[] cells;

        Grid(int width, int height)
        {
            vertices = new Vector2[width*height];
            cells = new Cell[width*height];
        }
    }
}
