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
    public partial class InitForm : Form
    {
        public Database date = new Database(); 

        public InitForm()
        {
            InitializeComponent();
        }

        private void cancel_button_Click(object sender, EventArgs e)
        {
            this.Dispose();
            Application.Exit();
        }

        private void ok_button_Click(object sender, EventArgs e)
        {
            try
            {
                int NrOfCheckpoints = Convert.ToInt32(textBox1.Text);
                if ((NrOfCheckpoints % 2 == 0) && (NrOfCheckpoints < 21) && (NrOfCheckpoints > 1))
                {
                    this.Visible = false;
                    MainForm Main = new MainForm(NrOfCheckpoints);
                }
                else
                {
                    MessageBox.Show("Numarul de checkpoint-uri trebuie sa fie un numar par intre 2 si 20", " ", MessageBoxButtons.OK);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "", MessageBoxButtons.OK);
                return;
            }
        }
    }
}
