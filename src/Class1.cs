using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace CFDPrototype
{
    internal class Class1
    {
        public static Vector3[] Func(int width, int height)
        {
            Vector3[] arr = new Vector3[width*height];
            for(int m = 0; m < width; m++)
            {
                for (int n = 0; n < height; n++)
                {
                    float a = m + 700;
                    float b = n + 600;
                    arr[m*height + n] = new Vector3((float)F(H(0, (a - 1000.0f) / 500.0f, (601.0f - b) / 500))
                        , (float)F(H(1, (a - 1000.0f) / 500.0f, (601.0f - b) / 500))
                        , (float)F(H(2, (a - 1000.0f) / 500.0f, (601.0f - b) / 500)));
                    //Console.WriteLine(arr[m*height + n].X + "  " + arr[m * height + n].Y + "  " + arr[m * height + n].Z);
                }
            }
            return arr;
        }

        static double F(double x)
        {
            return Math.Floor(((255d * Math.Exp(-1d*Math.Pow(Math.E, -1000d * x))) * Math.Pow(Math.Abs(x), Math.Exp(-1d*Math.Pow(Math.E, -1000d * x)))));
        }

        static double H(double v, double x, double y)
        {
            return Math.Exp(-1*Math.Pow(Math.E, -50d * (y - (x / 3d) - (7d / 50d)))) * (B(x, y) / 5d) + ((20d * K(v, x, y)) / 37d)*(1-Math.Exp(-1*Math.Pow(Math.E,200d*(Math.Pow(x+(y/3d)+(1d/2d),2d)+Math.Pow(y-(x/3d)+(3d/100d),2d)-(3d/100d))-Math.Exp(-200*(y-(x/3d)+(2d/5d)*Math.Sqrt(Math.Abs((3d/100d)-Math.Pow(x+(y/3d)+(1d/2d),2d))))))))*(1d-Math.Exp(-1*Math.Pow(Math.E,200d*(Math.Pow(x+(y/3d)-(1d/2d),2d)+Math.Pow(y-(x/3d)+(3d/100d),2d)-(1d/50d))-Math.Exp(-200d*(y-(x/3d)+(2d/5d)*Math.Sqrt(Math.Abs((1d/50d)-Math.Pow(x+(y/3)-0.5d,2d))))))));
        }

        static double B(double x, double y)
        {
            double result = 0;
            for (int i = 1; i <= 50; i++)
            {
                result += Math.Exp(-1d*Math.Pow(Math.E,-100d*(Math.Pow(Math.Sin(T(i,x,y)),6d)-(199d/200d)))-Math.Exp(Math.Pow(Math.Cos(200d*T(i,x,y)+Math.Pow(i,2d)),6)/-20d)-Math.Exp(1000*(Math.Abs(x+(y/3d))-(7d/10d))));
            }
            return result;
        }

        static double K(double v, double x, double y)
        {
            double result = 0;
            for (int i = 1; i<=50; i++)
            {
                result += Math.Pow(19d / 20d, i) * ((3d/2d)+0.5d*Math.Pow(v-1d,2)+((4d-4d*v)/10d)*Math.Cos(Math.Pow(i,2))) * Math.Exp(-1d*Math.Exp((-3d/2d)*(Math.Cos(Math.Pow(5d/4d,i)*L(1,i,x,y)+Math.Cos(3*Math.Pow(5d/4d,i)*L(3,i,x,y))+2*Math.Cos(32*Math.Pow(i,2)))*Math.Cos(Math.Pow(5d/4d,i)*(Math.Sin(Math.Pow(i,2))*U(x,y)-(3d/2d)*Math.Cos(Math.Pow(i,2))*V(x,y))+Math.Cos(3*Math.Pow(5d/4d,i)*L(5,i,x,y))+2*Math.Cos(12*Math.Pow(i,2)))-(3d/2d)+A(i,x,y))));
            }
            return result;
        }

        static double T(double s, double x, double y)
        {
            return Math.PI * (3*x+y)/3 + ((7+5*Math.Cos(Math.Pow(s,2)))/35d)*Math.Pow(y-(x/3),2)*Math.Cos((6+3*Math.Cos(4*Math.Pow(s,2)))*(3*y-x)/3 + 2*Math.Cos(4*y+2*x+5*Math.Pow(s,2))+3*Math.Pow(s,2));
        }

        static double A(double s, double x, double y)
        {
            return Math.Exp(-1 * Math.Exp(0.5*Math.Sqrt(Math.Pow(x+(y/3)-0.5,2)+6*Math.Pow(y-(x/3),2))*Math.Sqrt(Math.Pow(x+(y/3)+0.5,2)+6*Math.Pow(y-(x/3),2))*Math.Abs(U(x,y)+Math.Pow(Math.Cos(7*Math.Pow(s,2)),4))-(7d/20d)))+Math.Exp(-1*Math.Exp(10*(Math.Pow(x+(y/3)+0.5,2)+6*Math.Pow(y-(x/3),2)-(3d/100d))))+ Math.Exp(-1 * Math.Exp(10 * (Math.Pow(x + (y / 3) - 0.5, 2) + 6 * Math.Pow(y - (x / 3), 2) - (3d / 100d))));
        }

        static double L(double u, double s, double x, double y)
        {
            return Math.Cos(u*Math.Pow(s,2))*U(x,y)+(3d/2d)*Math.Sin(u*Math.Pow(s,2))*V(x,y);
        }

        static double U(double x, double y)
        {
            return Math.Cos(2*Math.Log(Math.Pow(Q(x,y),2)+Math.Pow(P(x,y),2)))*Q(x,y)+Math.Sin(2*Math.Log(Math.Pow(Q(x,y),2)+Math.Pow(P(x,y),2)))*P(x,y);
        }

        static double V(double x, double y)
        {
            return Math.Atan(Q(x,y)/Math.Abs(P(x,y)));
        }

        static double Q(double x, double y)
        {
            return ((3*x+y+(3d/2d)) / (2*Math.Pow(x+(y/3)+0.5,2)+12*Math.Pow(y-(x/3),2)+(1/100000))) + ((x+(y/3)-0.5) / (Math.Pow(x+(y/3)-0.5,2)+6*Math.Pow(y-(x/3),2)+(1/100000)));
        }

        static double P(double x, double y)
        {
            return ((3 * y - x) / 3)*((3/(2*Math.Pow(x+(y/3)+0.5,2)+12*Math.Pow(y-(x/3),2)+(1/100000d)) )+(1/(Math.Pow(x+(y/3)-0.5,2)+6*Math.Pow(y-(x/3),2)+(1/100000))));
        }

    }
}
