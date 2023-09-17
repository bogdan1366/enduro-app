using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Management;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using FTD2XX_NET;

namespace EnduroApp
{

    class Program
    {
        [STAThread]
        static void Main()
        {
            {
                Application.EnableVisualStyles();
                Application.Run(new InitForm());
                return;
            }
        }

    }



}


