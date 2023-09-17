using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EnduroApp
{
    public partial class Register : Form
    {        
        MainForm mainInstance;

        public Register(MainForm arg)
        {
            mainInstance = arg;
            InitializeComponent();
            this.CenterToParent();
        }

        private void ok_button_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length == 0 && comboBox1.SelectedIndex < 0)
            {
                DialogResult message = MessageBox.Show("Introduceti numele concurentului si selectati categoria de participare.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (textBox1.Text.Length == 0)
            {
                DialogResult message = MessageBox.Show("Introduceti numele concurentului.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (comboBox1.SelectedIndex < 0)
            {
                DialogResult message = MessageBox.Show("Selectati categoria de participare", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            mainInstance.date.AllRiders.Add(new Riders(textBox1.Text, mainInstance.date.compNr, textBox2.Text, (Riders.categ)comboBox1.SelectedIndex, mainInstance.date.NrofCheckpoints));
            mainInstance.AllRidersForm.Add_Rider(mainInstance.date.AllRiders[mainInstance.date.AllRiders.Count - 1]);
            mainInstance.date.compNr++;
            string comboText = mainInstance.date.AllRiders[(mainInstance.date.AllRiders.Count - 1)].nrConcurs.ToString() + " " + mainInstance.date.AllRiders[(mainInstance.date.AllRiders.Count - 1)].nume;

            mainInstance.comboBox3.Items.Add(comboText);
            mainInstance.comboBox4.Items.Add(comboText);
            mainInstance.comboBox5.Items.Add(comboText);
            this.Dispose();
        }

        private void cancel_button_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}
