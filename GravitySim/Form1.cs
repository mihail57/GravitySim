using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using CliLib;

namespace GravitySim
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.MouseWheel += new MouseEventHandler(MouseScroll);

            bodies.Add(new Body("0 coords", 0, 0, 0, new Vector(0, 0), Color.White, 0));
            bodies.Add(new Body("Тело 1", 300, 400, 5E16, new Vector(11500, Math.PI / 2), Color.Violet, idGen++));
            bodies.Add(new Body("Тело 2", 300, 500, 5E16, new Vector(9000, Math.PI / 2), Color.Red, idGen++));
            bodies.Add(new Body("Тело 3", 300, 300, 2E15, new Vector(20000, Math.PI / 2), Color.Green, idGen++));
            bodies.Add(new Body("Тело 4", 300, 200, 2E19, new Vector(0, 0), Color.Blue, idGen++));
            //foreach(Body bb in bodies) bb.visualCoords = Body.CalculateVisualCoords(bodies[0], bb.visualCoords, scale);
            CreateLabels();
            label12.Text = "1 п.:" + Body.scale / scale * Math.Pow(10, 11) + " м";
        }

        public static List<Body> bodies = new List<Body>();

        private void Form1_Load(object sender, EventArgs e)
        {
        }
    


        private void Timer1_Tick(object sender, EventArgs e)
        {
            time++;
            label1.Text = "t = " + Convert.ToString(time);

            foreach (Body b in bodies)
            {
                if(b.id != 0) Body.AddPoint(new PointF((float)b.coords[0], (float)b.coords[1]), b.tracing);
            }

            for (int i = 0; i < calcPerTick; i++)
            {
                foreach (Body b in bodies)
                {
                    b.forces.Clear();
                }
                
                for (int a = 1; a < bodies.Count - 1; a++)
                {
                    for (int j = a + 1; j < bodies.Count; j++)
                    {
                        Vector res = Vector.Gravity(bodies[a].coords, bodies[a].mass, bodies[j].coords, bodies[j].mass, Body.scale, Body.gravityConst, Body.eps);
                        bodies[a].forces.Add(res);
                        bodies[j].forces.Add(Vector.NegativeVector(res));
                    }
                    
                }

                foreach (Body b in bodies)
                {
                    if (b.id != 0)
                    {
                        b.acceleration = b.forces[0];
                        for (int j = 1; j < b.forces.Count; j++) 
                            b.acceleration = Vector.Sum(b.acceleration, b.forces[j]);
                        b.acceleration.modulus /= b.mass;
                        b.speed = Vector.CalculateSpeed(b.acceleration, b.speed, timePerCalc);
                        b.coords = Vector.CalculatePos(b.acceleration, timePerCalc, b.coords, b.speed, Body.scale);
                    }
                }
            }


            if (panel1.Visible) ReloadProperties(bodies[clickedBody]);

            LockCamera();

            Invalidate();
        }

        public static int idGen = 1;

        public static int timePerCalc = 50;
        public static int calcPerTick = 2500;

        public static bool isPressed = false;
        public static bool isScrolling = false;
        public static bool drawVector = false;
        public static bool drawAcc = false;

        public static bool rotate = false;
        public static bool drag = false;

        public static bool createNewBody = false;

        public static int time = 0;

        public static int mouseX = 0;
        public static int mouseY = 0;

        public static float xMove = 0;
        public static float yMove = 0;

        public static double scale = 1;
        public static double deltaScale = 1;

        public static PointF newPoint = new PointF();

        public Random rnd = new Random();

        public Body oldVersion;

        List<Button> generatedButtons = new List<Button>();
        List<CheckBox> generatedBoxes = new List<CheckBox>();


        int clickedBody = 0;

        bool lockCamera = false;
        int lockedId = 0;


        private void button2_Click(object sender, EventArgs e)
        {
            if (isPressed)
            {
                isPressed = false;
                button1.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = true;
                button5.Enabled = true;
                button6.Enabled = true;
                button7.Enabled = true;
                timer1.Stop();
                Invalidate();
            }
            else
            {
                isPressed = true;
                button1.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
                button5.Enabled = false;
                button6.Enabled = false;
                button7.Enabled = false;
                timer1.Start();
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (rotate)
            {
                bodies[clickedBody].speed = Vector.ChangeAngle(bodies[clickedBody].speed, newPoint.X, newPoint.Y, bodies[clickedBody].visualCoords);
                label14.Text = "Угол: " + bodies[clickedBody].speed.angle / Math.PI * 180;
            }

            for (int i = 0; i < bodies.Count; i++)
            {
                PointF p = new PointF();
                Body.Draw(bodies[i], bodies[0], scale, g, ref p, drawVector, drawAcc) ;
                bodies[i].rotatePoint = p;
            }

            isScrolling = false;

            xMove = 0;
            yMove = 0;

            GC.Collect();
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.MouseMove += new MouseEventHandler(MoveMouse);
                mouseX = e.X;
                mouseY = e.Y;
            }
            else if (e.Button == MouseButtons.Left)
            {
                int getClick = Body.LocateClick(e.X, e.Y, bodies);
                if (panel3.Visible)
                { //Ручное перемещение объекта
                    if (getClick != -1)
                    {
                        drag = true;
                        this.MouseMove += new MouseEventHandler(MoveMouse);
                        mouseX = e.X;
                        mouseY = e.Y;
                    }
                    else
                    {
                        if (panel3.Visible && e.X >= bodies[clickedBody].rotatePoint.X - 2 && e.X < bodies[clickedBody].rotatePoint.X + 5 && e.Y >= bodies[clickedBody].rotatePoint.Y - 2 && e.Y < bodies[clickedBody].rotatePoint.Y + 5)
                        { //Изменение угла вектора скорости
                            rotate = true;
                            this.MouseMove += new MouseEventHandler(MoveMouse);
                            newPoint = new PointF(e.X, e.Y);
                        }
                    }
                }
                else
                {
                    clickedBody = getClick;
                    if (clickedBody != -1) //Информация о теле
                    {
                        panel1.Visible = true;
                        ReloadProperties(bodies[clickedBody]);
                    }
                    else if (panel1.Visible)
                    {
                        clickedBody = getClick;
                        panel1.Visible = false;
                    }
                }
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            this.MouseMove -= new MouseEventHandler(MoveMouse);
            xMove = 0;
            yMove = 0;
            rotate = false;
            drag = false;
        }

        private void MoveMouse(object sender, MouseEventArgs e)
        {
            if (!rotate && !drag && !lockCamera)
            {
                xMove = (e.Location.X - mouseX);
                yMove = (e.Location.Y - mouseY);
                mouseX = e.Location.X;
                mouseY = e.Location.Y;
                bodies[0].visualCoords = Body.RecalculateZeroPos(bodies[0], deltaScale, xMove, yMove, mouseX, mouseY, isScrolling, lockCamera, new double[2] { bodies[lockedId].visualCoords[0], bodies[lockedId].visualCoords[1] });
            }
            else if (drag)
            {
                bodies[clickedBody].visualCoords[0] += (e.Location.X - mouseX);
                bodies[clickedBody].coords[0] += (e.Location.X - mouseX) / scale;
                bodies[clickedBody].visualCoords[1] += (e.Location.Y - mouseY);
                bodies[clickedBody].coords[1] += (e.Location.Y - mouseY) / scale;

                textBox2.Text = Convert.ToString(bodies[clickedBody].coords[0]);
                textBox3.Text = Convert.ToString(bodies[clickedBody].coords[1]);

                mouseX = e.Location.X;
                mouseY = e.Location.Y;

                Invalidate();
            }
            else newPoint = new PointF(e.X, e.Y);
            

            Invalidate();
        }

        private void MouseScroll(object sender, MouseEventArgs e)
        {
            deltaScale = Math.Pow(1.25, e.Delta / 120);
            if (scale * deltaScale > Math.Pow(10, 3) || scale * deltaScale < Math.Pow(10, -3)) deltaScale = 1;
            else
            {
                scale *= deltaScale;
                mouseX = e.Location.X;
                mouseY = e.Location.Y;

                isScrolling = true;
                bodies[0].visualCoords = Body.RecalculateZeroPos(bodies[0], deltaScale, xMove, yMove, mouseX, mouseY, isScrolling, lockCamera, new double[2] { bodies[lockedId].visualCoords[0], bodies[lockedId].visualCoords[1] });

                Invalidate();
            }
            label12.Text = "1 п.:" + Body.scale / scale * Math.Pow(10, 11) + " м";
        }

        private void Form1_Click(object sender, EventArgs e)
        {
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
        }



        public void ReloadProperties(Body newBody)
        {
            label6.Text = newBody.name + ":";
            label2.Text = "Скорость: " + newBody.speed.modulus;
            label3.Text = "Угол: " + (newBody.speed.angle / Math.PI * 180);
            label4.Text = "Ускорение: " + newBody.acceleration.modulus;
            label5.Text = "Угол: " + (newBody.acceleration.angle / Math.PI * 180);
            label7.Text = "x: " + newBody.coords[0];
            label8.Text = "y: " + newBody.coords[1];
            label18.Text = "Масса: " + newBody.mass * Math.Pow(10, 11);
            pictureBox1.BackColor = newBody.color;
        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
            panel3.Visible = true;
            createNewBody = true;
            button2.Enabled = false;
            drawVector = true;
            pictureBox2.BackColor = Color.FromArgb(rnd.Next());
            label14.Text = "Угол: 0";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
            panel3.Visible = true;
            button2.Enabled = false;
            drawVector = true;

            textBox1.Text = bodies[clickedBody].name;
            textBox2.Text = Convert.ToString(bodies[clickedBody].coords[0]);
            textBox3.Text = Convert.ToString(bodies[clickedBody].coords[1]);
            textBox4.Text = Convert.ToString(bodies[clickedBody].mass * Math.Pow(10, 11));
            textBox5.Text = Convert.ToString(bodies[clickedBody].speed.modulus);
            pictureBox2.BackColor = bodies[clickedBody].color;
            label14.Text = "Угол: " + (bodies[clickedBody].speed.angle / Math.PI * 180);
            oldVersion = new Body(bodies[clickedBody]);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            timer2.Stop();
            panel3.Visible = false;
            panel1.Visible = true;
            drawVector = false;

            if (!createNewBody)
            {
                int index = 0;
                for (int i = 1; i < bodies.Count; i++) if (bodies[clickedBody].id == bodies[i].id) { index = i; break; }

                bodies[clickedBody].name = textBox1.Text;
                double newX = Convert.ToDouble(textBox2.Text);
                double newY = Convert.ToDouble(textBox3.Text);
                bodies[clickedBody].visualCoords[0] += (newX - bodies[clickedBody].coords[0]) * scale;
                bodies[clickedBody].coords[0] = newX;
                bodies[clickedBody].visualCoords[1] += (newY - bodies[clickedBody].coords[1]) * scale;
                bodies[clickedBody].coords[1] = newY;
                bodies[clickedBody].mass = Convert.ToDouble(textBox4.Text) / Math.Pow(10, 11);
                bodies[clickedBody].color = pictureBox2.BackColor;
                bodies[clickedBody].speed.modulus = Convert.ToDouble(textBox5.Text);

                bodies[index] = bodies[clickedBody];
            }
            else
            {
                bodies.Add(new Body(textBox1.Text, Convert.ToDouble(textBox2.Text), Convert.ToDouble(textBox3.Text), Convert.ToDouble(textBox4.Text) / Math.Pow(10, 11), new Vector(Convert.ToDouble(textBox5.Text) / Math.Pow(10, 11), 0), pictureBox2.BackColor, idGen++));
                bodies[bodies.Count - 1].visualCoords = Body.CalculateVisualCoords(bodies[0], bodies[bodies.Count - 1].visualCoords, scale);
                clickedBody = bodies.Count - 1;
            }
            ReloadProperties(bodies[clickedBody]);
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
            textBox5.Text = "";
            createNewBody = false;
            if (bodies.Count > 2) button2.Enabled = true;
            button6.Enabled = true;

            Invalidate();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            timer2.Stop();
            panel3.Visible = false;
            drawVector = false;
            if (createNewBody)
            {
                createNewBody = false;
            }
            else
            {
                bodies[clickedBody] = new Body(oldVersion);
                panel1.Visible = true;
            }
            if (bodies.Count > 2) button2.Enabled = true;

            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
            textBox5.Text = "";

            Invalidate();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            cd.Color = pictureBox2.BackColor;
            if (cd.ShowDialog() == DialogResult.OK) pictureBox2.BackColor = cd.Color;
        }

        private void label3_Click(object sender, EventArgs e)
        {
            drawVector = !drawVector;
            Invalidate();
        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            bodies.RemoveRange(1, bodies.Count - 1); //Все кроме якоря 0 координаты
            button2.Enabled = false;
            button6.Enabled = false;
            generatedBoxes.Clear();
            generatedButtons.Clear();
            tabPage2.Controls.Clear();
            time = 0;
            idGen = 1;
            label1.Text = "t = 0";
            Invalidate();
        }

        private void label5_Click(object sender, EventArgs e)
        {
            drawAcc = !drawAcc;
            Invalidate();
        }

        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SimulationProperties simProp = new SimulationProperties(calcPerTick, timePerCalc, time);
            SaveFileDialog file = new SaveFileDialog();
            file.Filter = "JSON|*.json";
            if (file.ShowDialog() == DialogResult.OK) 
            {
                string[] s = new string[bodies.Count + 1];
                s[0] = JsonSerializer.Serialize(simProp, new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All), IncludeFields = true });
                for (int i = 1; i < bodies.Count; i++)
                {
                    bodies[i].clr = bodies[i].color.ToArgb();
                    s[i] = JsonSerializer.Serialize(bodies[i], new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All), IncludeFields = true });
                }
                File.WriteAllLines(file.FileName, s);
            }
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();
            file.Filter = "JSON|*.json";
            if (file.ShowDialog() == DialogResult.OK)
            {
                string[] s = File.ReadAllLines(file.FileName);
                SimulationProperties simProp = JsonSerializer.Deserialize<SimulationProperties>(s[0], new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All), IncludeFields = true });
                s[0] = "";
                timePerCalc = simProp.timePerCalc;
                calcPerTick = simProp.calcPerTick;
                time = simProp.time;
                textBox1.Text = "t = " + time;
                bodies.RemoveRange(1, bodies.Count - 1); //Все кроме якоря 0 координаты
                bodies[0].visualCoords = new double[] { 0, 0 };
                scale = 1;
                deltaScale = 1;
                generatedBoxes.Clear();
                generatedButtons.Clear();
                tabPage2.Controls.Clear();
                foreach (string b in s) if (b != "")
                {
                    Body bb = JsonSerializer.Deserialize<Body>(b, new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All), IncludeFields = true });
                    bb.visualCoords = Body.CalculateVisualCoords(bodies[0], bb.visualCoords, scale);
                    bodies.Add(bb);
                }
            }
            button2.Enabled = true;
            button6.Enabled = true;
            
            CreateLabels();
            time = 0;
            label1.Text = "t = 0";
            idGen = bodies[bodies.Count - 1].id;
            label12.Text = "1 п.:" + Body.scale / scale * Math.Pow(10, 11) + " м";
            Invalidate();
        }

        void CreateLabels()
        {
            tabPage2.Controls.Clear();
            generatedButtons.Clear();
            int x = 25;
            int x2 = 5;
            int x3 = 105;
            int y = -10;
            foreach(Body b in bodies)
            {
                Button newButton = new Button();
                newButton.Text = b.name;
                newButton.Location = new Point(x, y += 25);
                newButton.Click += new EventHandler(ClickInfoButton);
                newButton.Tag = b.id;
                tabPage2.Controls.Add(newButton);
                generatedButtons.Add(newButton);

                CheckBox ch = new CheckBox();
                ch.Location = new Point(x3, y + 5);
                ch.Text = "";
                //ch.Size = new Size(20, Size.Height);
                ch.AutoSize = true;
                ch.Tag = b.id;
                ch.CheckedChanged += new EventHandler(ChangeCheck);
                tabPage2.Controls.Add(ch);
                generatedBoxes.Add(ch);

                if (b.id != 0)
                {
                    PictureBox pbox = new PictureBox();
                    pbox.Size = new Size(15, 15);
                    pbox.BackColor = b.color;
                    pbox.Location = new Point(x2, y + 4);
                    tabPage2.Controls.Add(pbox);
                }
            }
        }

        private void ClickInfoButton(object sender, EventArgs e)
        {
            Button selected = (Button)sender;
            int selectedBody = 0;
            for(int i = 0; i < bodies.Count; i++)
            {
                if(bodies[i].id == Convert.ToInt32(selected.Tag)) { selectedBody = i; break; }
            }

            LockCamera(selectedBody);
            lockCamera = false;
        }

        private void ChangeCheck(object sender, EventArgs e)
        {
            CheckBox changed = (CheckBox)sender;
            foreach(CheckBox ch in generatedBoxes) if(ch.Focused) { changed = ch; break; }

            if (changed.Checked) LockCamera();
            else lockCamera = false;
        }

        void LockCamera(int l = -1)
        {
            bool noChecked = true;
            foreach (CheckBox ch in generatedBoxes) if (ch.Checked) { lockedId = Convert.ToInt32(ch.Tag); noChecked = false; break; }
            if (l != -1) { lockedId = l; noChecked = false; }
            if (lockedId != -1 && !noChecked)
            {
                for (int i = 0; i < bodies.Count; i++) if (lockedId == bodies[i].id) { lockedId = i; break; }

                xMove = (float)(Size.Width / 2 - bodies[lockedId].visualCoords[0]);
                yMove = (float)(Size.Height / 2 - bodies[lockedId].visualCoords[1]);
                //scale = 1;
                lockCamera = true;
                bodies[0].visualCoords = Body.RecalculateZeroPos(bodies[0], scale, xMove, yMove, mouseX, mouseY, isScrolling, lockCamera, bodies[lockedId].visualCoords);
                Invalidate();
            }
            else if (noChecked)
            {
                lockCamera = false;
                lockedId = 0;
            }

            
        }

        private void свойстваToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 f = new Form2();
            f.Show();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            bodies.RemoveAt(clickedBody);
            clickedBody = -1;
            panel1.Visible = false;
            Invalidate();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            timer2.Stop();
            timer2.Start();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            timer2.Stop();
            try
            {
                bodies[clickedBody].coords[0] = Convert.ToDouble(textBox2.Text);
                bodies[clickedBody].coords[1] = Convert.ToDouble(textBox3.Text);
                bodies[clickedBody].visualCoords = Body.CalculateVisualCoords(bodies[0], bodies[clickedBody].visualCoords, scale);
                Invalidate();
            }
            catch(Exception) { }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        { 
            timer2.Stop();
            timer2.Start();
        }
    }
}
