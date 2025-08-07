using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _2DFluidSim
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form1 form = new Form1();
            Application.Run(form);
        }

        public struct Vector
        {
            public double x;
            public double y;
            public Vector(double x, double y)
            {
                this.x = x;
                this.y = y;
            }

            public static Vector operator +(Vector a, Vector b)
            {
                return new Vector(a.x + b.x, a.y + b.y);
            }
            public static Vector operator -(Vector a, Vector b)
            {
                return new Vector(a.x - b.x, a.y - b.y);
            }
            public static Vector operator *(Vector a, double b)
            {
                return new Vector(a.x*b, a.y * b);
            }
        }

        static Vector[,] velocities = new Vector[400, 400];
        static Color[,] dye = new Color[400, 400];
        public static Vector[,] GetVelocities()
        {
            return velocities;
        }
        public static Color[,] GetDye()
        {
            return dye;
        }
        public static void UpdateSimulation(double t)
        {
            //ApplyGravity(t);
            Advection(t);
        }
        public static void InitialiseVelocities()
        {
            for(int i = 0; i < 400; i++)
            {
                for(int j = 0; j < 400; j++)
                {
                    velocities[i,j] = new Vector(0,0);
                }
            }
        }
        public static void InitialiseDye()
        {
            for (int i = 0; i < 400; i++)
            {
                for (int j = 0; j < 400; j++)
                {
                    dye[i, j] = Color.Blue;
                }
            }
        }
        public static void InjectDye(int x, int y)
        {
            for (int i = x - 8; i < x + 8; i++)
            {
                for (int j = y - 8; j < y + 8; j++)
                {
                    if (i > 0 && j > 0 && i < 400 && j < 400)
                    {
                        dye[i, j] = Color.Red;
                    }
                }
            }
        }
        public static Vector mousePos1 = new Vector(double.NaN, double.NaN);
        public static Vector mousePos2 = new Vector(double.NaN, double.NaN);
        public static void ApplyMouseAcceleration(int x, int y)
        {
            Vector currentPos = new Vector(x, y);

            if (double.IsNaN(mousePos2.x))
            {
                mousePos2 = currentPos;
                return;
            }

            double deltaTime = 0.032;
            Vector velocity = new Vector(
                (currentPos.x - mousePos2.x) / deltaTime,
                (currentPos.y - mousePos2.y) / deltaTime
            );

            int radius = 5;
            double strength = 0.5;
            for (int i = x - radius; i <= x + radius; i++)
            {
                for (int j = y - radius; j <= y + radius; j++)
                {
                    if (i >= 0 && j >= 0 && i < velocities.GetLength(0) && j < velocities.GetLength(1))
                    {
                        double dx = i - x;
                        double dy = j - y;
                        double dist = Math.Sqrt(dx * dx + dy * dy);

                        if (dist <= radius)
                        {
                            double weight = 1.0 - (dist / radius);
                            velocities[i, j] = new Vector(
                                velocities[i, j].x + velocity.x * weight * strength,
                                velocities[i, j].y + velocity.y * weight * strength
                            );
                        }
                    }
                }
                mousePos2 = currentPos;
            }
        }
        static void Advection(double t)
        {
            Vector[,] newVelocities = new Vector[400, 400];
            Color[,] newDye = new Color[400, 400];

            Parallel.For(0, 400, y =>
            {
                for (int x = 0; x < 400; x++)
                {
                    Vector v = velocities[x, y];
                    Vector pos = new Vector(x, y) - new Vector(v.x * t, v.y * t);
                    (newVelocities[x, y], newDye[x, y]) = SamplePropertiesAtPos(pos);
                    if (double.IsNaN(newVelocities[x, y].x))
                    {
                        newVelocities[x, y] = new Vector(-velocities[x, y].x, -velocities[x, y].y);
                        newDye[x, y] = dye[x, y];
                    }
                }
            });

            // After parallel processing, copy results back to main arrays
            Parallel.For(0, 400, y =>
            {
                for (int x = 0; x < 400; x++)
                {
                    velocities[x, y] = newVelocities[x, y];
                    dye[x, y] = newDye[x, y];
                }
            });
        }
        static (Vector, Color) SamplePropertiesAtPos(Vector pos)
        {
            int x = Convert.ToInt32(Math.Floor(pos.x));
            int y = Convert.ToInt32(Math.Floor(pos.y));
            Vector vResult = new Vector(0, 0);
            (int, int, int) cResult = (0, 0, 0);
            int samples = 0;
            for(int i = x-5; i < x+5; i++)
            {
                for(int j = y-5; j < y+5; j++)
                {
                    if(i > 0 && j > 0 && i < 400 && j < 400)
                    {
                        vResult += velocities[i, j];
                        cResult = (dye[i, j].R + cResult.Item1, dye[i,j].G + cResult.Item2, dye[i,j].B + cResult.Item3);
                        samples++;
                    }
                }
            }
            if (samples != 0)
            {
                vResult = new Vector(vResult.x / samples, vResult.y / samples);
                Color color = Color.FromArgb(0, cResult.Item1 / samples, cResult.Item2 / samples, cResult.Item3 / samples);
                return (vResult, color);
            }
            else
            {
                return (new Vector(double.NaN, double.NaN), Color.FromArgb(0,0,0,0));
            }
        }
    }
}
