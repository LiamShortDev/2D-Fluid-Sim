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
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.D1)
            {
                _dyeInjecting = false;
                _waterInteracting = true;
            } 
            if(e.KeyCode == Keys.D2)
            {

                _dyeInjecting = true;
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
        private bool _dyeInjecting = true;
        private bool _waterInteracting = false;
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
            pictureBox1.Size = new Size(width, height);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.MouseDown += new MouseEventHandler(pictureBox1_MouseDown);
            pictureBox1.MouseUp += new MouseEventHandler(pictureBox1_MouseUp);
            this.Controls.Add(pictureBox1);

            bitmap = new Bitmap(width, height);
            pictureBox1.Image = bitmap;
            Program.InitialiseVelocities();
            Program.InitialiseDye();
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
                if (_dyeInjecting)
                {
                    Program.InjectDye(pos.X, pos.Y);
                }
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
            Color[,] dye = Program.GetDye();

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
                        Color c = dye[x, y];

                        int index = x * 4;
                        row[index + 0] = c.B;       // Blue
                        row[index + 1] = c.G;       // Green
                        row[index + 2] = c.R;     // Red
                        row[index + 3] = 255;     // Alpha
                    }
                }
            }

            bitmap.UnlockBits(data);
            pictureBox1.Image = bitmap;
            Program.UpdateSimulation(32.0f/1000);
        }
    }
}
