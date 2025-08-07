using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _2DFluidSim
{
    public partial class Form1 : Form
    {
        public struct Vector
        {
            public double x;
            public double y;
            public Vector(double x, double y)
            {
                this.x = x;
                this.y = y;
            }
        }
        PictureBox pictureBox1 = new PictureBox();
        Bitmap bitmap;
        int width = 400;
        int height = 400;
        int scale = 2;
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.D1)
            {
                _waterInteracting = true;
            } 
            if(e.KeyCode == Keys.D2)
            {
                _waterInteracting = false;
            }
        }
        public Form1()
        {
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
            InitializeComponent();
            InitializeSimulationDisplay();
            StartSimulationLoop();
        }
        //private bool _dyeInjecting = false;
        private bool _waterInteracting = true;
        private bool _mousedown = false;
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            _mousedown = true;
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            _mousedown = false;
        }
        public void InitializeSimulationDisplay()
        {
            pictureBox1.Size = new Size(width*scale, height*scale);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.MouseDown += new MouseEventHandler(pictureBox1_MouseDown);
            pictureBox1.MouseUp += new MouseEventHandler(pictureBox1_MouseUp);
            this.Controls.Add(pictureBox1);

            bitmap = new Bitmap(width, height);
            pictureBox1.Image = bitmap;
            Program.InitialiseVelocities();
        }

        public void StartSimulationLoop()
        {
            Timer timer = new Timer();
            timer.Interval = 32; // ~60 FPS
            timer.Tick += (s, e) => UpdateDisplay();
            timer.Start();
        }

        public void UpdateDisplay()
        {
            if (_mousedown)
            {
                var pos = pictureBox1.PointToClient(Cursor.Position);
                pos.X /= scale;
                pos.Y /= scale;
                if (_waterInteracting)
                {
                    Program.ApplyMouseAcceleration(pos.X, pos.Y);
                }
                Program.mousePos1 = Program.mousePos2;
                Program.mousePos2 = new Program.Vector(pos.X, pos.Y);
            }
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb
            );
            Program.Vector[,] velocities = Program.GetVelocities();

            unsafe
            {
                byte* scan0 = (byte*)data.Scan0;
                int stride = data.Stride;

                double maxVelocity = 50.0; // set a reasonable upper bound

                for (int y = 0; y < 400; y++)
                {
                    byte* row = scan0 + y * stride;
                    for (int x = 0; x < 400; x++)
                    {
                        /* velocity visualisation
                        Program.Vector v = velocities[x, y];
                        double magnitude = Math.Sqrt(v.x * v.x + v.y * v.y);
                        double normalized = Math.Min(1.0, magnitude / maxVelocity);
                        byte red = (byte)(normalized * 255);
                        */

                        Program.Vector v = velocities[x, y];
                        double angle = Math.Atan2(v.y, v.x);  
                        double speed = v.Magnitude();            

                        // Map angle to hue (0–360), speed to brightness (0–1)
                        double hue = (angle * (180 / Math.PI) + 360) % 360;
                        double saturation = 1.0;
                        double value = Math.Min(speed * 5.0, 1.0);  // adjust multiplier for brightness
                        int r = 0;
                        int g = 0;
                        int b = 0;
                        (r, g, b) = HSVtoRGB(hue, saturation, value);

                        int index = x * 4;
                        row[index + 0] = (byte)r;       // Blue
                        row[index + 1] = (byte)g;       // Green
                        row[index + 2] = (byte)b;       // Red
                        row[index + 3] = 255;     // Alpha
                    }
                }
            }

            bitmap.UnlockBits(data);
            pictureBox1.Image = bitmap;
            Program.UpdateSimulation(32.0f/1000);
        }

        public (int, int, int) HSVtoRGB(double hue, double saturation, double value)
        {
            double chroma = value * saturation;
            double hPrime = hue / 60;
            double X = chroma * (1 - Math.Abs((hPrime % 2) - 1));
            double r;
            double g;
            double b;
            switch(Math.Floor(hPrime) % 6)
            {
                case 0: r = chroma; g = X; b = 0; break;
                case 1: r = X; g = chroma; b = 0; break;
                case 2: r = 0; g = chroma; b = X; break;
                case 3: r = 0; g = X; b = chroma; break;
                case 4: r = X; g = 0; b = chroma; break;
                case 5: r = chroma; g = 0; b = X; break;
                default: r = 0; g = 0; b = 0; break;
            }
            double m = value - chroma;
            r += m; g += m; b += m;
            return (
                (int)(Math.Max(0, Math.Min(1, r)) * 255),
                (int)(Math.Max(0, Math.Min(1, g)) * 255),
                (int)(Math.Max(0, Math.Min(1, b)) * 255)
            );
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
