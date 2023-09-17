using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Deployment;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EnduroApp
{
    public partial class About : Form
    {
        private Version ver;
        public About()
        {
            InitializeComponent();
        }
        public About(Version vers)
        {
            InitializeComponent();
            ver = vers;
            label1.Text = "Enduro App v" + ver.ToString();
        }

        private void About_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Visible = false;
            e.Cancel = true;
        }
    }
}
