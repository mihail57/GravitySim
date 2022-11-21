using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GravitySim
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
            textBox1.Text = Form1.calcPerTick.ToString();
            textBox2.Text = Form1.timePerCalc.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form1.calcPerTick = Convert.ToInt32(textBox1.Text);
            Form1.timePerCalc = Convert.ToInt32(textBox2.Text);
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
