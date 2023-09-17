using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Deployment;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using System.Threading;
using FTD2XX_NET;
/* Obsolete
using iTextSharp.text;
using iTextSharp.text.pdf;
*/
using EnduroApp.ImportExport;

namespace EnduroApp
{
    public partial class MainForm : Form
    {

        System.Windows.Forms.Timer timer1 = new System.Windows.Forms.Timer();

        FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;

        byte[] rxBuffer;

        // Create new instance of the FTDI device class
        FTDI myFtdiDevice = new FTDI();

        public IComparer<Riders> comparer = new MyOrderingClass();

        public Database date = new Database(); 

        private AllRidersNew riders;
        private RankingsNew rankings;
        private About about;

        public RankingsNew RankingsForm
        {
            get
            {
                if (rankings == null)
                {
                    rankings = new RankingsNew(this);
                    rankings.Etape = this.date.NrofCheckpoints / 2;
                    rankings.Show();
                }
                return rankings;
            }
        }
        public  AllRidersNew AllRidersForm
        {
            get
            {
                if (riders == null)
                {
                    riders = new AllRidersNew(this);
                    riders.Etape = this.date.NrofCheckpoints-1;
                    riders.Show();
                }
                return riders;
            }
        }
        public About AboutForm
        {
            get
            {
                if (about == null)
                {
                    about = new About(GetRunningVersion());
                    about.Show();
                }
                return about;
            }
        }

        public MainForm(int arg)
        {
            InitializeComponent();
            timer1.Tick += new EventHandler(SynchClock);
            date.NrofCheckpoints = arg;
            rxBuffer = Enumerable.Repeat((byte)0, 100).ToArray();
            AllRidersForm.Show();
            this.Show();
        }

        public Version GetRunningVersion()
        {
            try
            {
                return System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }
            catch
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        public void SynchClock (object sender, EventArgs e)
            {
                // Stop the timer
                timer1.Enabled = false;
                int hr, min, sec, milsec;
                milsec = DateTime.Now.Millisecond;
                sec = DateTime.Now.Second;
                min = DateTime.Now.Minute;
                hr = DateTime.Now.Hour;

                int zsec = sec / 10 + 0x30;
                sec = sec % 10 + 0x30;
                int zmin = min / 10 + 0x30;
                min = min % 10 + 0x30;
                int zhr = hr / 10 + 0x30;
                hr = hr % 10 + 0x30;

                byte[] timeCmd = { 0xCC, 0x0A, 0x01, 0x00, 0x00, 0x3A, 0x00, 0x00, 0x3A, 0x00, 0x00 };

                byte[] intBytes = BitConverter.GetBytes(sec);
                timeCmd[10] = intBytes[0];
                intBytes = BitConverter.GetBytes(zsec);
                timeCmd[9] = intBytes[0];
                intBytes = BitConverter.GetBytes(min);
                timeCmd[7] = intBytes[0];
                intBytes = BitConverter.GetBytes(zmin);
                timeCmd[6] = intBytes[0];
                intBytes = BitConverter.GetBytes(hr);
                timeCmd[4] = intBytes[0];
                intBytes = BitConverter.GetBytes(zhr);
                timeCmd[3] = intBytes[0];

                if (WriteFT(timeCmd))
                {
                    byte[] answer = AnswerFT(3);
                    byte[] expectedAnswer = { 0xCC, 0x02, 0x01 };
                    byte[] errorAnswer = { 0xCC, 0x02, 0xFE };
                    if (answer.SequenceEqual(expectedAnswer))
                    {
                        DialogResult message = MessageBox.Show("Sincronizare reusita.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (answer.SequenceEqual(errorAnswer))
                    {
                        DialogResult message = MessageBox.Show("Sincronizare esuata.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }

                // Close our device
                ftStatus = myFtdiDevice.Close();
        }

        private void CloseMessage (object sender, EventArgs e)
        {

        }

        private bool InitializeFT () 
        {
            uint ftdiDeviceCount = 0;
            ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);

            // check is the number of devices is correctly read
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                DialogResult message = MessageBox.Show("Error: " + ftStatus.ToString(), " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // check if there are devices connected
            if (ftdiDeviceCount == 0)
            {
                DialogResult message = MessageBox.Show("Conecteaza si porneste dispozitivul de cronometrare.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;

            }
            // Allocate storage for device info list
            FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];

            // Populate our device list
            ftStatus = myFtdiDevice.GetDeviceList(ftdiDeviceList);

            // Open first device in our list by serial number
            ftStatus = myFtdiDevice.OpenBySerialNumber(ftdiDeviceList[0].SerialNumber);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                DialogResult message = MessageBox.Show("Failed to open device (error: " + ftStatus.ToString() + ")", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // Set up device data parameters
            // Set Baud rate to 9600
            ftStatus = myFtdiDevice.SetBaudRate(9600);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                DialogResult message = MessageBox.Show("Failed to set Baud rate (error: " + ftStatus.ToString() + ")", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // Set data characteristics - Data bits, Stop bits, Parity
            ftStatus = myFtdiDevice.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_1, FTDI.FT_PARITY.FT_PARITY_NONE);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                DialogResult message = MessageBox.Show("Failed to set data characteristics (error: " + ftStatus.ToString() + ")", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // Set flow control - no flow control
            ftStatus = myFtdiDevice.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_NONE, 0x11, 0x13);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                DialogResult message = MessageBox.Show("Failed to set flow control (error: " + ftStatus.ToString() + ")", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // Set latency timer
            ftStatus = myFtdiDevice.SetLatency(2);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                DialogResult message = MessageBox.Show("Failed to set the latency timer (error: " + ftStatus.ToString() + ")", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // Set read and write timeouts to 500 miliseconds
            ftStatus = myFtdiDevice.SetTimeouts(500, 500);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                DialogResult message = MessageBox.Show("Failed to set timeouts (error: " + ftStatus.ToString() + ")", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }

        private bool WriteFT (byte[] cmd)
        {
            Thread.Sleep(10);
            UInt32 numBytesWritten = 0;
            // Note that the Write method is overloaded, so can write string or byte array data
            ftStatus = myFtdiDevice.Write(cmd, cmd.Length, ref numBytesWritten);
            if ((ftStatus != FTDI.FT_STATUS.FT_OK) || (cmd.Length != numBytesWritten))
            {
                DialogResult message = MessageBox.Show("Failed to write to device (error: " + ftStatus.ToString() + ")", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }

        private byte[] AnswerFT(uint nrOfBytes)
        {
            uint numBytesRead = 0;
            rxBuffer = Enumerable.Repeat((byte)0, 100).ToArray();

            // Note that the Read method is overloaded, so can read string or byte array data
            ftStatus = myFtdiDevice.Read(rxBuffer, nrOfBytes, ref numBytesRead);
            if (ftStatus != FTDI.FT_STATUS.FT_OK )
            {
                DialogResult message = MessageBox.Show("Failed to read data (error: " + ftStatus.ToString() + ")", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            if (numBytesRead == 0)
            {
                DialogResult message = MessageBox.Show("Porneste dispozitivul de cronometrare si incearca din nou.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                while (message != DialogResult.OK) ;
            }
            Array.Resize(ref rxBuffer, (int)numBytesRead);
            return rxBuffer;
        }

        private void sincronizeaza_cronometrul_button_Click(object sender, EventArgs e)
        {
            Console.WriteLine("click");
            if (InitializeFT())
            {
                timer1.Interval = (1000 - DateTime.Now.Millisecond);
                timer1.Enabled = true;
            }

        }

        private void seteaza_nr_checkpointului_button_Click(object sender, EventArgs e)
        {
            int selItem = comboBox1.SelectedIndex;
            if (selItem == -1)
            {
                DialogResult message = MessageBox.Show("Selecteaza numarul checkpoint-ului.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            byte[] checkpointNr = BitConverter.GetBytes(selItem+1);
            byte[] cmd = {0xCC, 0x03, 0x02, checkpointNr[0]};
            if (InitializeFT())
            {
                if (WriteFT(cmd))
                {
                    byte[] answer = AnswerFT(3);
                    byte[] expectedAnswer = { 0xCC, 0x02, 0x02 };
                    byte[] errorAnswer = { 0xCC, 0x02, 0xFD };
                    if (answer.SequenceEqual(expectedAnswer))
                    {
                        DialogResult message = MessageBox.Show("Numarul checkpoint-ului a fost setat.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (answer.SequenceEqual(errorAnswer))
                    {
                        DialogResult message = MessageBox.Show("Dispozitivul a returnat eroare. Incearca inca o data!", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }

            // Close our device
            ftStatus = myFtdiDevice.Close();
        }

        private void seteaza_nr_checkpointuri_button_Click(object sender, EventArgs e)
        {
            int selItem = (comboBox2.SelectedIndex+1) * 2;
            if (selItem == 0)
            {
                DialogResult message = MessageBox.Show("Selecteaza numarul de checkpoint-uri", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            date.NrofCheckpoints = selItem;
            RankingsForm.Etape = selItem / 2;
            RankingsForm.CreateGrid();
            AllRidersForm.Etape = selItem-1;
            AllRidersForm.CreateGrid();

            DialogResult mesage = MessageBox.Show("Numarul de checkpoint-uri a fost setat.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void inregistreaza_concurent_button_Click(object sender, EventArgs e)
        {
            Register RegisterWindow = new Register(this);
            RegisterWindow.Show();
        }

        private void aloca_cip_button_Click(object sender, EventArgs e)
        {
            int selItem = comboBox3.SelectedIndex;
            if (selItem == -1)
            {
                DialogResult message = MessageBox.Show("Selectati concurentul caruia vreti sa ii atribuiti cip.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int selectedCompNr = Int32.Parse(comboBox3.SelectedItem.ToString().Split(' ')[0]);
            byte[] cmd = { 0xAB, 0x02, 0x02 };
            if (InitializeFT())
            {
                if (WriteFT(cmd))
                {                   
                    byte[] answer = AnswerFT(7);
                    Console.WriteLine(string.Join(" ", answer));
                    byte[] tagNotPresent = new byte[] { 0xAB, 0x02, 0xFD };
                    if (answer.SequenceEqual(tagNotPresent))
                    {
                        DialogResult message = MessageBox.Show("Aseaza cip-ul pe dispozitivul de cronometrare si mai incearca o data!", " ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        goto error;
                    }
                    else if (answer.Length == 0)
                    {
                        goto error;
                    }
                    byte[] tagID = new byte[4];
                    Array.Copy(answer, 3, tagID, 0, 4);
                    foreach (Riders rider in date.AllRiders)
                    {
                        if (tagID.SequenceEqual(rider.GetTagID()))
                        {
                            DialogResult message = MessageBox.Show("Cip-ul apartine deja lui " + rider.nume, " ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            goto error;
                        }
                    }
                    foreach (Riders rider in date.AllRiders)
                    {
                        if (selectedCompNr == rider.nrConcurs)
                        {
                            if (rider.GetTagID().SequenceEqual(new byte[4] { 0, 0, 0, 0 }))
                            {
                                rider.SetTagID(tagID);
                                DialogResult message = MessageBox.Show("Cip alocat!", " ", MessageBoxButtons.OK, MessageBoxIcon.Information); 
                            }
                            else
                            {
                                DialogResult dialogResult = MessageBox.Show(date.AllRiders[selItem].nume + " are deja un cip alocat.\nTotusi, doriti sa ii atribuiti acest cip?", " ", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                                if (dialogResult == DialogResult.Yes)
                                {
                                    date.AllRiders[selItem].SetTagID(tagID);
                                }
                            }
                        }
                    }                   
                }
            }

            error:
            // Close our device
            ftStatus = myFtdiDevice.Close();

        }

        private void descarca_timp_button_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            byte[] cmd = { 0xAB, 0x02, 0x02 };
            if (InitializeFT())
            {
                if (WriteFT(cmd))
                {
                    byte[] answer = AnswerFT(7);
                    byte[] tagNotPresent = new byte[] { 0xAB, 0x02, 0xFD };
                    if (answer.SequenceEqual(tagNotPresent))
                    {
                        DialogResult message = MessageBox.Show("Aseaza cipul pe dispozitivul de cronometrare si mai incearca o data!", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Cursor = Cursors.Default;
                        goto error;
                    }
                    else if (answer.Length == 0)
                    {
                        goto error;
                    }
                    byte[] tagID = new byte[4];
                    Array.Copy(answer, 3, tagID, 0, 4);
                    foreach (Riders rider in date.AllRiders)
                    {
                        if (tagID.SequenceEqual(rider.GetTagID()))
                        {
                            Console.WriteLine("Rider gasit: " + rider.nume);
                            rider.hasAllCheckpoints = true;
                            rider.DeleteTimes();
                            int totalSteps = date.NrofCheckpoints;
                            for (int i = 0; i < date.NrofCheckpoints; i++)
                            {
                                int progress = i * 100 / totalSteps;
                                this.progressBar1.Value = progress;
                                byte blockNr = (byte)((i + 1) + i / 3 + 3);
                                Console.WriteLine("Scot timpul de la checkpointul " + (i+1).ToString() +" citind din blocul " + string.Join(" ", blockNr));
                                byte[] readBlock = new byte[11] {0xAB, 0x0A, 0x03, blockNr, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
                                if (WriteFT(readBlock))
                                {                                   
                                    answer = AnswerFT(19);
                                    Console.WriteLine(BitConverter.ToString(answer)+ '\n');
                                    tagNotPresent = new byte[] { 0xAB, 0x02, 0xFC };
                                    if (answer.SequenceEqual(tagNotPresent))
                                    {
                                        Cursor = Cursors.Default;
                                        DialogResult message = MessageBox.Show("Aseaza cipul pe dispozitivul de cronometrare si mai incearca o data!", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        this.progressBar1.Value = 0;                                        
                                        goto error;
                                    }
                                    else if (answer.Length == 0)
                                    {
                                        goto error;
                                    }
                                    byte[] time = new byte[8];
                                    Array.Copy(answer, 3, time, 0, 8);
                                    rider.SetTime(time, (i + 1));                                    
                                }
                            }
                            AllRidersForm.Add_Time(rider);
                            List<Riders> catList;
                            switch (rider.categoria)
                            {
                                case Riders.categ.MASTER_WOMEN_35_PLUS:
                                    catList = date.Master_Women_35_plus;
                                    break;
                                case Riders.categ.WOMEN_21_39:
                                    catList = date.Women_21_39;
                                    break;
                                case Riders.categ.U21_WOMEN:
                                    catList = date.U21_Women;
                                    break;
                                case Riders.categ.U21_MEN:
                                    catList = date.U21_Men;
                                    break;
                                case Riders.categ.MEN_21_39:
                                    catList = date.Men_21_39;
                                    break;
                                case Riders.categ.MASTER_MEN_40_PLUS:
                                    catList = date.Master_Men_40_plus;
                                    break;
                                case Riders.categ.U15_JUNIORS:
                                    catList = date.U15_Juniors;
                                    break;
                                default:
                                    return;
                            }
                            for (int i = 0; i < catList.Count; i++)
                            {
                                if (catList[i].nrConcurs == rider.nrConcurs)
                                {
                                    catList.Remove(catList[i]);
                                }
                            }
                            catList.Add(rider);
                            catList.Sort(comparer);
                            RankingsForm.RefreshCat(catList);
                            this.progressBar1.Value = 0;
                            Cursor = Cursors.Default;
                            DialogResult messag = MessageBox.Show("Succes!", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            goto error;                            
                        }
                    }
                    DialogResult messa = MessageBox.Show("Cipul nu apartine nici unui concurent!", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Cursor = Cursors.Default;
                }
            }
            error:
            Cursor = Cursors.Default;
            // Close our device
            ftStatus = myFtdiDevice.Close();
        }

        private void sterge_cip_button_Click(object sender, EventArgs e)
        {
            int i;

            DialogResult question = MessageBox.Show("Sigur vreti sa stergeti cipul?", " ", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (question == DialogResult.Yes)
            {
                Cursor = Cursors.WaitCursor;
                if (InitializeFT())
                {
                    for (i = 0; i < 45; i++)
                    {
                        this.progressBar1.Value = i * 100 / 45;
                        byte blockNr = (byte)((i + 1) + i / 3 + 3);
                        Console.WriteLine("Sterg timpul de la checkpointul " + (i + 1).ToString() + " stergand blocul " + string.Join(" ", blockNr));
                        byte[] eraseBlock = new byte[27] { 0xAB, 0x1A, 0x04, blockNr, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                        if (WriteFT(eraseBlock))
                        {
                            byte[] answer = AnswerFT(3);
                            byte[] tagNotPresent = new byte[] { 0xAB, 0x02, 0xFB };
                            if (answer.SequenceEqual(tagNotPresent))
                            {
                                this.progressBar1.Value = 0;
                                Cursor = Cursors.Default;
                                DialogResult message = MessageBox.Show("Aseaza cipul pe dispozitivul de cronometrare si mai incearca o data!", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                break;
                            }
                            else if (answer.SequenceEqual(new byte[] { 0xAB, 0x02, 0x04 }))
                            {
                                Console.Write(" Block sters\n");
                            }
                            else if (answer.Length == 0)
                            {
                                break;
                            }
                        }
                    }
                    this.progressBar1.Value = 0;
                    Cursor = Cursors.Default;
                    if (i == 45)
                    {
                        DialogResult mesage = MessageBox.Show("Succes!", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }

            }
            else if (question == DialogResult.No)
            {
                return;
            }

            // Close our device
            ftStatus = myFtdiDevice.Close();

        }

        private void citeste_cip_button_Click(object sender, EventArgs e)
        {
            if (InitializeFT())
            {
                for (int i = 0; i < 45; i++)
                {
                    byte blockNr = (byte)((i + 1) + i / 3 + 3);
                    Console.WriteLine("Scot timpul de la checkpointul " + (i + 1).ToString() + " citind blocul " + string.Join(" ", blockNr));
                    byte[] readBlock = new byte[11] { 0xAB, 0x0A, 0x03, blockNr, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                    if (WriteFT(readBlock))
                    {
                        byte[] answer = AnswerFT(19);
                        Console.WriteLine(BitConverter.ToString(answer) + '\n');
                        byte[] tagNotPresent = new byte[] { 0xAB, 0x02, 0xFB };
                        if (answer.SequenceEqual(tagNotPresent))
                        {
                            DialogResult message = MessageBox.Show("Aseaza cipul pe dispozitivul de cronometrare si mai incearca o data!", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            goto error;
                        }
                        else if (answer.Length == 0)
                        {
                            goto error;
                        }
                    }
                }
                Console.WriteLine("\n");
            }

            error:
            // Close our device
            ftStatus = myFtdiDevice.Close();

        }

        private void revoca_cip_button_Click(object sender, EventArgs e)
        {
            int selItem = comboBox3.SelectedIndex;
            if (selItem == -1)
            {
                DialogResult message = MessageBox.Show("Selectati concurentul caruia vreti sa ii revocati cipul.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int selectedCompNr = Int32.Parse(comboBox3.SelectedItem.ToString().Split(' ')[0]);
            DialogResult dialogResult = MessageBox.Show("Sunteti sigur ca vreti sa ii revocati cipul lui " + date.AllRiders[selItem].nume + " ?", " ", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                foreach (Riders rider in date.AllRiders)
                {
                    if (rider.nrConcurs == selectedCompNr)
                    {
                        date.AllRiders[selItem].SetTagID(new byte[4] { 0, 0, 0, 0 });
                        DialogResult message = MessageBox.Show("Succes!.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else if (dialogResult == DialogResult.No)
            {
                return;
            }            
        }

        private void detalii_cip_button_Click(object sender, EventArgs e)
        {

            Cursor = Cursors.WaitCursor;
            byte[] cmd = { 0xAB, 0x02, 0x02 };
            string num = "-";
            string nr = "-";
            string cat = "-";
            if (InitializeFT())
            {
                if (WriteFT(cmd))
                {
                    byte[] answer = AnswerFT(7);
                    byte[] tagNotPresent = new byte[] { 0xAB, 0x02, 0xFD };
                    if (answer.SequenceEqual(tagNotPresent))
                    {
                        DialogResult message = MessageBox.Show("Aseaza cipul pe dispozitivul de cronometrare si mai incearca o data!", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Cursor = Cursors.Default;
                        goto error;
                    }
                    else if (answer.Length == 0)
                    {
                        goto error;
                    }
                    byte[] tagID = new byte[4];
                    Array.Copy(answer, 3, tagID, 0, 4);
                    foreach (Riders rider in date.AllRiders)
                    {
                        if (tagID.SequenceEqual(rider.GetTagID()))
                        {
                            Console.WriteLine("Rider gasit: " + rider.nume);
                            num = rider.nume;
                            nr = rider.nrConcurs.ToString();
                            cat = rider.Categoria;
                        }
                    }
                    int totalSteps = date.NrofCheckpoints;
                    string alltimes = null;
                    for (int i = 0; i < date.NrofCheckpoints; i++)
                    {
                        int progress = i * 100 / totalSteps;
                        this.progressBar1.Value = progress;
                        byte blockNr = (byte)((i + 1) + i / 3 + 3);
                        Console.WriteLine("Scot timpul de la checkpointul " + (i + 1).ToString() + " citind din blocul " + string.Join(" ", blockNr));
                        byte[] readBlock = new byte[11] { 0xAB, 0x0A, 0x03, blockNr, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                        if (WriteFT(readBlock))
                        {
                            answer = AnswerFT(19);
                            Console.WriteLine(BitConverter.ToString(answer) + '\n');
                            tagNotPresent = new byte[] { 0xAB, 0x02, 0xFC };
                            if (answer.SequenceEqual(tagNotPresent))
                            {
                                Cursor = Cursors.Default;
                                DialogResult message = MessageBox.Show("Aseaza cipul pe dispozitivul de cronometrare si mai incearca o data!", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                this.progressBar1.Value = 0;
                                goto error;
                            }
                            else if (answer.Length == 0)
                            {
                                goto error;
                            }
                            byte[] time = new byte[8];
                            Array.Copy(answer, 3, time, 0, 8);
                            byte[] empty = new byte[] {0, 0, 0, 0, 0, 0, 0, 0};
                            if (time.SequenceEqual(empty))
                            {
                                alltimes += "Checkpoint " + (i+1).ToString() + " -> -\n";
                            }
                            else
                            {
                                UTF8Encoding enc = new UTF8Encoding();
                                string str = enc.GetString(time);
                                alltimes += "Checkpoint " + (i + 1).ToString() + " -> " + str + "\n";
                            }
                        }
                    }
                    Cursor = Cursors.Default;
                    DialogResult messge = MessageBox.Show("Rider: " + num + "\nNr.concurs: " + nr + "\nCategorie: " + cat + "\n" + alltimes, " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.progressBar1.Value = 0;
                }
            }
            error:
            Cursor = Cursors.Default;
            // Close our device
            ftStatus = myFtdiDevice.Close();

        }

        private void elimina_concurent_button_Click(object sender, EventArgs e)
        {
            Riders selectedRider = new Riders();
            int selItem = comboBox4.SelectedIndex;
            if (selItem == -1)
            {
                DialogResult message = MessageBox.Show("Selectati concurentul pe care vreti sa il eliminati.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }  
            int selectedCompNr = Int32.Parse(comboBox4.SelectedItem.ToString().Split(' ')[0]);
            foreach (Riders rider in date.AllRiders)
            {
                if (rider.nrConcurs == selectedCompNr)
                {
                    selectedRider = rider;
                }
            }
            date.AllRiders.Remove(selectedRider);
            AllRidersForm.Remove_Rider(selectedRider);
            List<Riders> catList;
            switch (selectedRider.categoria)
            {
                case Riders.categ.MASTER_WOMEN_35_PLUS:
                    catList = date.Master_Women_35_plus;
                    break;
                case Riders.categ.WOMEN_21_39:
                    catList = date.Women_21_39;
                    break;
                case Riders.categ.U21_WOMEN:
                    catList = date.U21_Women;
                    break;
                case Riders.categ.U21_MEN:
                    catList = date.U21_Men;
                    break;
                case Riders.categ.MEN_21_39:
                    catList = date.Men_21_39;
                    break;
                case Riders.categ.MASTER_MEN_40_PLUS:
                    catList = date.Master_Men_40_plus;
                    break;
                case Riders.categ.U15_JUNIORS:
                    catList = date.U15_Juniors;
                    break;
                default:
                    return;
            }
            catList.Remove(selectedRider);
            catList.Sort(comparer);
            RankingsForm.RefreshCat(catList);
            comboBox3.Items.RemoveAt(comboBox4.SelectedIndex);
            comboBox5.Items.RemoveAt(comboBox4.SelectedIndex);
            comboBox4.Items.RemoveAt(comboBox4.SelectedIndex);

        }

        private void mod_dispozitiv_unic_button_Click(object sender, EventArgs e)
        {
            byte[] cmd = { 0xCC, 0x02, 0x03 };
            if (InitializeFT())
            {
                if (WriteFT(cmd))
                {
                    byte[] answer = AnswerFT(3);
                    byte[] expectedAnswer = { 0xCC, 0x02, 0x03 };
                    byte[] errorAnswer = { 0xCC, 0x02, 0xFC };
                    if (answer.SequenceEqual(expectedAnswer))
                    {
                        DialogResult message = MessageBox.Show("Dispozitivul a fost programat in modul 'Dispozitiv unic'.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (answer.SequenceEqual(errorAnswer))
                    {
                        DialogResult message = MessageBox.Show("Dispozitivul a returnat eroare. Incearca inca o data!", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }

            // Close our device
            ftStatus = myFtdiDevice.Close();
        }

        private void mod_dispozitive_multiple_button_Click(object sender, EventArgs e)
        {
            byte[] cmd = { 0xCC, 0x02, 0x04 };
            if (InitializeFT())
            {
                if (WriteFT(cmd))
                {
                    byte[] answer = AnswerFT(3);
                    byte[] positiveAnswer = { 0xCC, 0x02, 0x04 };
                    byte[] errorAnswer = { 0xCC, 0x02, 0xFB };
                    if (answer.SequenceEqual(positiveAnswer))
                    {
                        DialogResult message = MessageBox.Show("Dispozitivul a fost programat in modul 'Dispozitive multiple'.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (answer.SequenceEqual(errorAnswer))
                    {
                        DialogResult message = MessageBox.Show("Dispozitivul a returnat eroare. Incearca inca o data!", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }

            // Close our device
            ftStatus = myFtdiDevice.Close();
        }

        /* Obsolete feature
        private void listaConcurentiToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "PDF File|*.pdf";
            saveFileDialog1.Title = "Export";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {

                Document doc = new Document(iTextSharp.text.PageSize.A4);
                System.IO.FileStream file = new System.IO.FileStream(saveFileDialog1.FileName, System.IO.FileMode.OpenOrCreate);
                PdfWriter writer = PdfWriter.GetInstance(doc, file);
                doc.Open();
                PdfPTable tab = new PdfPTable(date.NrofCheckpoints + 2);
                tab.TotalWidth = 550;
                tab.LockedWidth = true;
                float[] widths = new float[date.NrofCheckpoints + 2];
                widths[0] = 20;
                widths[1] = 100;
                widths[2] = 50;
                for (int i = 0; i < (date.NrofCheckpoints - 1); i++)
                {
                    widths[i + 3] = (tab.TotalWidth - widths[0] - widths[1] - widths[2]) / (date.NrofCheckpoints - 1);
                }
                tab.SetWidths(widths);
                tab.AddCell(new Phrase("Nr.", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                tab.AddCell(new Phrase("Rider", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                tab.AddCell(new Phrase("Category", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                for (int i = 0; i < date.NrofCheckpoints - 1; i++)
                {
                    string et = "";
                    if (i % 2 == 0)
                    {
                        et += "Coborare ";
                        et += Convert.ToString(i / 2 + 1);
                    }
                    else
                    {
                        et += "Urcare ";
                        et += Convert.ToString(i / 2 + 1);
                    }
                    tab.AddCell(new Phrase(et, new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 8)));
                }
                foreach (Riders rider in date.AllRiders)
                {
                    Phrase cellPhrase = new Phrase(rider.nrConcurs.ToString(), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                    PdfPCell cell = new PdfPCell(cellPhrase);
                    cell.VerticalAlignment = 1;
                    if (date.AllRiders.IndexOf(rider) % 2 == 0)
                        cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                    else
                        cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                    tab.AddCell(cell);
                    cellPhrase = new Phrase(rider.nume, new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                    cell = new PdfPCell(cellPhrase);
                    if (date.AllRiders.IndexOf(rider) % 2 == 0)
                        cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                    else
                        cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                    cell.VerticalAlignment = 1;
                    tab.AddCell(cell);
                    cellPhrase = new Phrase(rider.Categoria, new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                    cell = new PdfPCell(cellPhrase);
                    if (date.AllRiders.IndexOf(rider) % 2 == 0)
                        cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                    else
                        cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                    cell.VerticalAlignment = 1;
                    tab.AddCell(cell);
                    for (int i = 0; i < date.NrofCheckpoints - 1; i++)
                    {
                        try
                        {
                            cellPhrase = new Phrase(string.Join(":", rider.GetTime(i + 1)), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        }
                        catch (ArgumentNullException)
                        {
                            cellPhrase = new Phrase(" ", new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        }
                        cell = new PdfPCell(cellPhrase);
                        if (date.AllRiders.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        cell.VerticalAlignment = 1;
                        tab.AddCell(cell);
                    }

                }
                doc.Add(tab);
                doc.Close();
                file.Close();
            }
        }

        private void clasamentMASTERSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "PDF File|*.pdf";
            saveFileDialog1.Title = "Export";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {

                Document doc = new Document(iTextSharp.text.PageSize.A4);
                try
                {
                    System.IO.FileStream file = new System.IO.FileStream(saveFileDialog1.FileName, System.IO.FileMode.OpenOrCreate);
                    PdfWriter writer = PdfWriter.GetInstance(doc, file);
                    doc.Open();
                    PdfPTable tab = new PdfPTable(date.NrofCheckpoints / 2 + 4);
                    tab.TotalWidth = 550;
                    tab.LockedWidth = true;
                    float[] widths = new float[date.NrofCheckpoints / 2 + 4];
                    widths[0] = 20;
                    widths[1] = 20;
                    widths[2] = 100;
                    for (int i = 0; i < (date.NrofCheckpoints / 2 + 1); i++)
                    {
                        widths[i + 3] = (tab.TotalWidth - widths[0] - widths[1] - widths[2] - widths[3]) / (date.NrofCheckpoints / 2 + 1);
                    }

                    tab.SetWidths(widths);
                    tab.AddCell(new Phrase("Loc", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    tab.AddCell(new Phrase("Nr.", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    tab.AddCell(new Phrase("Rider", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    for (int i = 0; i < date.NrofCheckpoints / 2; i++)
                    {
                        string et = "PS ";
                        et += Convert.ToString(i + 1);
                        tab.AddCell(new Phrase(et, new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    }
                    tab.AddCell(new Phrase("Timp Total", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    foreach (Riders rider in date.Men_21_39)
                    {
                        Phrase cellPhrase = new Phrase((date.Men_21_39.IndexOf(rider) + 1).ToString(), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        PdfPCell cell = new PdfPCell(cellPhrase);
                        cell.VerticalAlignment = 1;
                        if (date.Men_21_39.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        tab.AddCell(cell);
                        cellPhrase = new Phrase(rider.nrConcurs.ToString(), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        cell = new PdfPCell(cellPhrase);
                        cell.VerticalAlignment = 1;
                        if (date.Men_21_39.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        tab.AddCell(cell);
                        cellPhrase = new Phrase(rider.nume, new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        cell = new PdfPCell(cellPhrase);
                        if (date.Men_21_39.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        cell.VerticalAlignment = 1;
                        tab.AddCell(cell);
                        for (int i = 1; i < date.NrofCheckpoints; i += 2)
                        {
                            try
                            {
                                cellPhrase = new Phrase(string.Join(":", rider.GetTime(i)), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                            }
                            catch (ArgumentNullException)
                            {
                                cellPhrase = new Phrase(" ", new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                            }
                            cell = new PdfPCell(cellPhrase);
                            if (date.Men_21_39.IndexOf(rider) % 2 == 0)
                                cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                            else
                                cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                            cell.VerticalAlignment = 1;
                            tab.AddCell(cell);
                        }
                        try
                        {
                            cellPhrase = new Phrase(string.Join(":", rider.GetTotalTime()), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        }
                        catch (ArgumentNullException)
                        {
                            cellPhrase = new Phrase(" ", new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        }
                        cell = new PdfPCell(cellPhrase);
                        if (date.Men_21_39.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        cell.VerticalAlignment = 1;
                        tab.AddCell(cell);
                    }
                    doc.Add(tab);
                    doc.Close();
                    file.Close();
                }
                catch
                {
                    DialogResult message = MessageBox.Show("Exportarea nu a avut loc deoarece fisierul este deja utilizat de alta aplicatie!", " ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }                
            }
        }

        private void clasamentSENIORIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "PDF File|*.pdf";
            saveFileDialog1.Title = "Export";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {

                Document doc = new Document(iTextSharp.text.PageSize.A4);
                try
                {
                    System.IO.FileStream file = new System.IO.FileStream(saveFileDialog1.FileName, System.IO.FileMode.OpenOrCreate);
                    PdfWriter writer = PdfWriter.GetInstance(doc, file);
                    doc.Open();
                    PdfPTable tab = new PdfPTable(date.NrofCheckpoints / 2 + 4);
                    tab.TotalWidth = 550;
                    tab.LockedWidth = true;
                    float[] widths = new float[date.NrofCheckpoints / 2 + 4];
                    widths[0] = 20;
                    widths[1] = 20;
                    widths[2] = 100;
                    for (int i = 0; i < (date.NrofCheckpoints / 2 + 1); i++)
                    {
                        widths[i + 3] = (tab.TotalWidth - widths[0] - widths[1] - widths[2] - widths[3]) / (date.NrofCheckpoints / 2 + 1);
                    }

                    tab.SetWidths(widths);
                    tab.AddCell(new Phrase("Loc", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    tab.AddCell(new Phrase("Nr.", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    tab.AddCell(new Phrase("Rider", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    for (int i = 0; i < date.NrofCheckpoints / 2; i++)
                    {
                        string et = "PS ";
                        et += Convert.ToString(i + 1);
                        tab.AddCell(new Phrase(et, new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    }
                    tab.AddCell(new Phrase("Timp Total", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    foreach (Riders rider in date.U21_Men)
                    {
                        Phrase cellPhrase = new Phrase((date.U21_Men.IndexOf(rider) + 1).ToString(), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        PdfPCell cell = new PdfPCell(cellPhrase);
                        cell.VerticalAlignment = 1;
                        if (date.U21_Men.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        tab.AddCell(cell);
                        cellPhrase = new Phrase(rider.nrConcurs.ToString(), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        cell = new PdfPCell(cellPhrase);
                        cell.VerticalAlignment = 1;
                        if (date.U21_Men.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        tab.AddCell(cell);
                        cellPhrase = new Phrase(rider.nume, new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        cell = new PdfPCell(cellPhrase);
                        if (date.U21_Men.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        cell.VerticalAlignment = 1;
                        tab.AddCell(cell);
                        for (int i = 1; i < date.NrofCheckpoints; i += 2)
                        {
                            try
                            {
                                cellPhrase = new Phrase(string.Join(":", rider.GetTime(i)), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                            }
                            catch (ArgumentNullException)
                            {
                                cellPhrase = new Phrase(" ", new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                            }
                            cell = new PdfPCell(cellPhrase);
                            if (date.U21_Men.IndexOf(rider) % 2 == 0)
                                cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                            else
                                cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                            cell.VerticalAlignment = 1;
                            tab.AddCell(cell);
                        }
                        try
                        {
                            cellPhrase = new Phrase(string.Join(":", rider.GetTotalTime()), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        }
                        catch (ArgumentNullException)
                        {
                            cellPhrase = new Phrase(" ", new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        }
                        cell = new PdfPCell(cellPhrase);
                        if (date.U21_Men.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        cell.VerticalAlignment = 1;
                        tab.AddCell(cell);
                    }
                    doc.Add(tab);
                    doc.Close();
                    file.Close();
                }
                catch
                {
                    DialogResult message = MessageBox.Show("Exportarea nu a avut loc deoarece fisierul este deja utilizat de alta aplicatie!", " ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }
        }

        private void clasamentJUNIORIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "PDF File|*.pdf";
            saveFileDialog1.Title = "Export";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {

                Document doc = new Document(iTextSharp.text.PageSize.A4);
                try
                {
                    System.IO.FileStream file = new System.IO.FileStream(saveFileDialog1.FileName, System.IO.FileMode.OpenOrCreate);
                    PdfWriter writer = PdfWriter.GetInstance(doc, file);
                    doc.Open();
                    PdfPTable tab = new PdfPTable(date.NrofCheckpoints / 2 + 4);
                    tab.TotalWidth = 550;
                    tab.LockedWidth = true;
                    float[] widths = new float[date.NrofCheckpoints / 2 + 4];
                    widths[0] = 20;
                    widths[1] = 20;
                    widths[2] = 100;
                    for (int i = 0; i < (date.NrofCheckpoints / 2 + 1); i++)
                    {
                        widths[i + 3] = (tab.TotalWidth - widths[0] - widths[1] - widths[2] - widths[3]) / (date.NrofCheckpoints / 2 + 1);
                    }

                    tab.SetWidths(widths);
                    tab.AddCell(new Phrase("Loc", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    tab.AddCell(new Phrase("Nr.", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    tab.AddCell(new Phrase("Rider", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    for (int i = 0; i < date.NrofCheckpoints / 2; i++)
                    {
                        string et = "PS ";
                        et += Convert.ToString(i + 1);
                        tab.AddCell(new Phrase(et, new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    }
                    tab.AddCell(new Phrase("Timp Total", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    foreach (Riders rider in date.U21_Women)
                    {
                        Phrase cellPhrase = new Phrase((date.U21_Women.IndexOf(rider) + 1).ToString(), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        PdfPCell cell = new PdfPCell(cellPhrase);
                        cell.VerticalAlignment = 1;
                        if (date.U21_Women.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        tab.AddCell(cell);
                        cellPhrase = new Phrase(rider.nrConcurs.ToString(), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        cell = new PdfPCell(cellPhrase);
                        cell.VerticalAlignment = 1;
                        if (date.U21_Women.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        tab.AddCell(cell);
                        cellPhrase = new Phrase(rider.nume, new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        cell = new PdfPCell(cellPhrase);
                        if (date.U21_Women.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        cell.VerticalAlignment = 1;
                        tab.AddCell(cell);
                        for (int i = 1; i < date.NrofCheckpoints; i += 2)
                        {
                            try
                            {
                                cellPhrase = new Phrase(string.Join(":", rider.GetTime(i)), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                            }
                            catch (ArgumentNullException)
                            {
                                cellPhrase = new Phrase(" ", new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                            }
                            cell = new PdfPCell(cellPhrase);
                            if (date.U21_Women.IndexOf(rider) % 2 == 0)
                                cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                            else
                                cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                            cell.VerticalAlignment = 1;
                            tab.AddCell(cell);
                        }
                        try
                        {
                            cellPhrase = new Phrase(string.Join(":", rider.GetTotalTime()), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        }
                        catch (ArgumentNullException)
                        {
                            cellPhrase = new Phrase(" ", new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        }
                        cell = new PdfPCell(cellPhrase);
                        if (date.U21_Women.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        cell.VerticalAlignment = 1;
                        tab.AddCell(cell);
                    }
                    doc.Add(tab);
                    doc.Close();
                    file.Close();
                }
                catch
                {
                    DialogResult message = MessageBox.Show("Exportarea nu a avut loc deoarece fisierul este deja utilizat de alta aplicatie!", " ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }
        }

        private void clasamentFETEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "PDF File|*.pdf";
            saveFileDialog1.Title = "Export";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {

                Document doc = new Document(iTextSharp.text.PageSize.A4);
                try
                {
                    System.IO.FileStream file = new System.IO.FileStream(saveFileDialog1.FileName, System.IO.FileMode.OpenOrCreate);
                    PdfWriter writer = PdfWriter.GetInstance(doc, file);
                    doc.Open();
                    PdfPTable tab = new PdfPTable(date.NrofCheckpoints / 2 + 4);
                    tab.TotalWidth = 550;
                    tab.LockedWidth = true;
                    float[] widths = new float[date.NrofCheckpoints / 2 + 4];
                    widths[0] = 20;
                    widths[1] = 20;
                    widths[2] = 100;
                    for (int i = 0; i < (date.NrofCheckpoints / 2 + 1); i++)
                    {
                        widths[i + 3] = (tab.TotalWidth - widths[0] - widths[1] - widths[2] - widths[3]) / (date.NrofCheckpoints / 2 + 1);
                    }

                    tab.SetWidths(widths);
                    tab.AddCell(new Phrase("Loc", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    tab.AddCell(new Phrase("Nr.", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    tab.AddCell(new Phrase("Rider", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    for (int i = 0; i < date.NrofCheckpoints / 2; i++)
                    {
                        string et = "PS ";
                        et += Convert.ToString(i + 1);
                        tab.AddCell(new Phrase(et, new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    }
                    tab.AddCell(new Phrase("Timp Total", new iTextSharp.text.Font(iTextSharp.text.Font.BOLD, 10)));
                    foreach (Riders rider in date.Master_Women_35_plus)
                    {
                        Phrase cellPhrase = new Phrase((date.Master_Women_35_plus.IndexOf(rider) + 1).ToString(), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        PdfPCell cell = new PdfPCell(cellPhrase);
                        cell.VerticalAlignment = 1;
                        if (date.Master_Women_35_plus.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        tab.AddCell(cell);
                        cellPhrase = new Phrase(rider.nrConcurs.ToString(), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        cell = new PdfPCell(cellPhrase);
                        cell.VerticalAlignment = 1;
                        if (date.Master_Women_35_plus.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        tab.AddCell(cell);
                        cellPhrase = new Phrase(rider.nume, new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        cell = new PdfPCell(cellPhrase);
                        if (date.Master_Women_35_plus.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        cell.VerticalAlignment = 1;
                        tab.AddCell(cell);
                        for (int i = 1; i < date.NrofCheckpoints; i += 2)
                        {
                            try
                            {
                                cellPhrase = new Phrase(string.Join(":", rider.GetTime(i)), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                            }
                            catch (ArgumentNullException)
                            {
                                cellPhrase = new Phrase(" ", new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                            }
                            cell = new PdfPCell(cellPhrase);
                            if (date.Master_Women_35_plus.IndexOf(rider) % 2 == 0)
                                cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                            else
                                cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                            cell.VerticalAlignment = 1;
                            tab.AddCell(cell);
                        }
                        try
                        {
                            cellPhrase = new Phrase(string.Join(":", rider.GetTotalTime()), new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        }
                        catch (ArgumentNullException)
                        {
                            cellPhrase = new Phrase(" ", new iTextSharp.text.Font(iTextSharp.text.Font.NORMAL, 10));
                        }
                        cell = new PdfPCell(cellPhrase);
                        if (date.Master_Women_35_plus.IndexOf(rider) % 2 == 0)
                            cell.BackgroundColor = iTextSharp.text.Color.LIGHT_GRAY;
                        else
                            cell.BackgroundColor = iTextSharp.text.Color.WHITE;
                        cell.VerticalAlignment = 1;
                        tab.AddCell(cell);
                    }
                    doc.Add(tab);
                    doc.Close();
                    file.Close();
                }
                catch
                {
                    DialogResult message = MessageBox.Show("Exportarea nu a avut loc deoarece fisierul este deja utilizat de alta aplicatie!", " ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }

        }
        */
        
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Binary File|*.bin";
            saveFileDialog1.Title = "Save";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                try
                {
                    using (Stream stream = File.Open(saveFileDialog1.FileName, FileMode.Create))
                    {
                        BinaryFormatter bin = new BinaryFormatter();
                        bin.Serialize(stream, date);
                        Console.WriteLine("Saving succeeded");
                        MessageBox.Show("Success !"); 
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine("Saving failed");
                }
            }
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Create an instance of the open file dialog box.
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            // Set filter options and filter index.
            openFileDialog1.Filter = "Binary Files (.bin)|*.bin";
            openFileDialog1.FilterIndex = 1;

            openFileDialog1.Multiselect = true;

            // Process input if the user clicked OK.
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (Stream stream = File.Open(openFileDialog1.FileName, FileMode.Open))
                    {
                        BinaryFormatter bin = new BinaryFormatter();
                        date = (Database)bin.Deserialize(stream);
                        Cursor defaultCursor = Cursor.Current;
                        Cursor.Current = Cursors.WaitCursor;
                        RefreshAfterLoad(date);
                        Cursor.Current = defaultCursor;
                        MessageBox.Show("Load successful !"); 
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine("Load failed");
                }
            }
        }
        private void RefreshAfterLoad(Database data)
        {
            Console.WriteLine(data.NrofCheckpoints.ToString());
            //
            // Lista concurenti
            //
            AllRidersForm.Etape = data.NrofCheckpoints - 1;
            AllRidersForm.CreateGrid();
            for (int i=0; i<data.AllRiders.Count(); i++)
            {
                Console.WriteLine("Adding rider #" + data.AllRiders[i].nrConcurs);
                this.progressBar1.Value = (i + 1) * 58 / data.AllRiders.Count();
                AllRidersForm.Add_Rider_and_Time(data.AllRiders[i]);
            }
            //
            // Clasament
            //
            RankingsForm.Etape = data.NrofCheckpoints / 2;
            RankingsForm.CreateGrid();
            RankingsForm.RefreshCat(data.Master_Women_35_plus);
            this.progressBar1.Value += 7;
            RankingsForm.RefreshCat(data.Women_21_39);
            this.progressBar1.Value += 7;
            RankingsForm.RefreshCat(data.U21_Women);
            this.progressBar1.Value += 7;
            RankingsForm.RefreshCat(data.U21_Men);
            this.progressBar1.Value += 7;
            RankingsForm.RefreshCat(data.Men_21_39);
            this.progressBar1.Value += 7;
            RankingsForm.RefreshCat(data.Master_Men_40_plus);
            this.progressBar1.Value += 7;
            RankingsForm.RefreshCat(data.U15_Juniors);
            foreach (Riders rider in data.AllRiders)
            {
                if (!string.IsNullOrEmpty(rider.nume))
                {
                    string comboText = rider.nrConcurs.ToString() + " " + rider.nume;
                    comboBox3.Items.Add(comboText);
                    comboBox4.Items.Add(comboText);
                    comboBox5.Items.Add(comboText);
                }
            }
            this.progressBar1.Value = 0;
        }
        private void newListaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (AllRidersForm.Visible == true)
            {
                AllRidersForm.Focus();
                if (AllRidersForm.WindowState == FormWindowState.Minimized)
                {
                    AllRidersForm.WindowState = FormWindowState.Normal;
                }
                AllRidersForm.BringToFront();
            }
            else
            {
                AllRidersForm.Visible = true;
            }
        }

        private void newClasamentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (RankingsForm.Visible == true)
            {
                RankingsForm.Focus();
                if (RankingsForm.WindowState == FormWindowState.Minimized)
                {
                    RankingsForm.WindowState = FormWindowState.Normal;
                }
                RankingsForm.BringToFront();
            }
            else
            {
                RankingsForm.Visible = true;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show("Vreti sa salvati setarile concursului, timpii si numele concurentilor?", "Confirmation", MessageBoxButtons.YesNoCancel);
            if (result == DialogResult.Yes)
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "Binary File|*.bin";
                saveFileDialog1.Title = "Save";
                saveFileDialog1.ShowDialog();

                if (saveFileDialog1.FileName != "")
                {
                    try
                    {
                        using (Stream stream = File.Open(saveFileDialog1.FileName, FileMode.Create))
                        {
                            BinaryFormatter bin = new BinaryFormatter();
                            bin.Serialize(stream, date);
                            Console.WriteLine("Saving succeeded");
                        }
                    }
                    catch (IOException)
                    {
                        Console.WriteLine("Saving failed");
                    }
                }
            }
            else
            {
                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }
            this.Dispose();
            RankingsForm.Dispose();
            AllRidersForm.Dispose();
            Application.Exit();
        }

        private void importFromCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            // Set filter options and filter index.
            openFileDialog1.Filter = "CSV File (.csv)|*.csv";
            openFileDialog1.FilterIndex = 1;

            openFileDialog1.Multiselect = false;

            // Process input if the user clicked OK.
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string file = openFileDialog1.FileName;
                var data = ImportFromCSV.Import(file);
                if (data != null)
                {
                    Cursor defaultCursor = Cursor.Current;
                    Cursor.Current = Cursors.WaitCursor;
                    int maxCompNr = 0;
                    date.AllRiders = data.AllRiders;
                    // Lista concurenti
                    AllRidersForm.CreateGrid();
                    foreach (Riders rider in date.AllRiders)
                    {
                        rider.nrOfCheckpoints = date.NrofCheckpoints;
                        AllRidersForm.Add_Rider(rider);
                        if (rider.nrConcurs > maxCompNr)
                        {
                            maxCompNr = rider.nrConcurs;
                        }
                    }
                    date.compNr = maxCompNr + 1;
                    // Clasament
                    RankingsForm.CreateGrid();
                    comboBox3.Items.Clear();
                    comboBox4.Items.Clear();
                    comboBox5.Items.Clear();
                    foreach (Riders rider in date.AllRiders)
                    {
                        if (!string.IsNullOrEmpty(rider.nume))
                        {
                            string comboText = rider.nrConcurs.ToString() + " " + rider.nume;
                            comboBox3.Items.Add(comboText);
                            comboBox4.Items.Add(comboText);
                            comboBox5.Items.Add(comboText);
                        }
                    }
                    Cursor.Current = defaultCursor;
                    MessageBox.Show("Import realizat cu succes !"); 
                }
            }
        }

        private void sterge_timpi_button_Click(object sender, EventArgs e)
        {
            Riders selectedRider = new Riders();
            int selItem = comboBox5.SelectedIndex;
            if (selItem == -1)
            {
                DialogResult message = MessageBox.Show("Selectati concurentul ai carui timpi doriti sa ii stergeti.", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int selectedCompNr = Int32.Parse(comboBox5.SelectedItem.ToString().Split(' ')[0]);
            foreach (Riders rider in date.AllRiders)
            {
                if (rider.nrConcurs == selectedCompNr)
                {
                    selectedRider = rider;
                }
            }
            selectedRider.DeleteTimesAndPenalties();
            AllRidersForm.Remove_Time(selectedRider);
            List<Riders> catList;
            switch (selectedRider.categoria)
            {
                case Riders.categ.MASTER_WOMEN_35_PLUS:
                    catList = date.Master_Women_35_plus;
                    break;
                case Riders.categ.WOMEN_21_39:
                    catList = date.Women_21_39;
                    break;
                case Riders.categ.U21_WOMEN:
                    catList = date.U21_Women;
                    break;
                case Riders.categ.U21_MEN:
                    catList = date.U21_Men;
                    break;
                case Riders.categ.MEN_21_39:
                    catList = date.Men_21_39;
                    break;
                case Riders.categ.MASTER_MEN_40_PLUS:
                    catList = date.Master_Men_40_plus;
                    break;
                case Riders.categ.U15_JUNIORS:
                    catList = date.U15_Juniors;
                    break;
                default:
                    return;
            }
            catList.Remove(selectedRider);
            catList.Sort(comparer);
            RankingsForm.RefreshCat(catList);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (AboutForm.Visible == true)
            {
                AboutForm.Focus();
                if (AboutForm.WindowState == FormWindowState.Minimized)
                {
                    AboutForm.WindowState = FormWindowState.Normal;
                }
                AboutForm.BringToFront();
            }
            else
            {
                AboutForm.Visible = true;
            }
        }

        private void generalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV File|*.csv";
            saveFileDialog1.Title = "Export";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "")
            {
                try
                {
                    Cursor defaultCursor = Cursor.Current;
                    Cursor.Current = Cursors.WaitCursor;
                    List<Riders> clasamentGeneral = new List<Riders>();
                    clasamentGeneral.AddRange(date.AllRiders);
                    clasamentGeneral.Sort(comparer);
                    ExportToCSV.ExportDataToCSV(clasamentGeneral, date.NrofCheckpoints, saveFileDialog1.FileName);
                    Cursor.Current = defaultCursor;
                    MessageBox.Show("Export realizat cu succes !");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void feminin1518ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV File|*.csv";
            saveFileDialog1.Title = "Export";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "")
            {
                try
                {
                    Cursor defaultCursor = Cursor.Current;
                    Cursor.Current = Cursors.WaitCursor;
                    ExportToCSV.ExportDataToCSV(date.Master_Women_35_plus, date.NrofCheckpoints, saveFileDialog1.FileName);
                    Cursor.Current = defaultCursor;
                    MessageBox.Show("Export realizat cu succes !");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void feminin19ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV File|*.csv";
            saveFileDialog1.Title = "Export";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "")
            {
                try
                {
                    Cursor defaultCursor = Cursor.Current;
                    Cursor.Current = Cursors.WaitCursor;
                    ExportToCSV.ExportDataToCSV(date.Women_21_39, date.NrofCheckpoints, saveFileDialog1.FileName);
                    Cursor.Current = defaultCursor;
                    MessageBox.Show("Export realizat cu succes !");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void masculin1518ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV File|*.csv";
            saveFileDialog1.Title = "Export";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "")
            {
                try
                {
                    Cursor defaultCursor = Cursor.Current;
                    Cursor.Current = Cursors.WaitCursor;
                    ExportToCSV.ExportDataToCSV(date.U21_Women, date.NrofCheckpoints, saveFileDialog1.FileName);
                    Cursor.Current = defaultCursor;
                    MessageBox.Show("Export realizat cu succes !");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void masculin1929ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV File|*.csv";
            saveFileDialog1.Title = "Export";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "")
            {
                try
                {
                    Cursor defaultCursor = Cursor.Current;
                    Cursor.Current = Cursors.WaitCursor;
                    ExportToCSV.ExportDataToCSV(date.U21_Men, date.NrofCheckpoints, saveFileDialog1.FileName);
                    Cursor.Current = defaultCursor;
                    MessageBox.Show("Export realizat cu succes !");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void masculin3039ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV File|*.csv";
            saveFileDialog1.Title = "Export";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "")
            {
                try
                {
                    Cursor defaultCursor = Cursor.Current;
                    Cursor.Current = Cursors.WaitCursor;
                    ExportToCSV.ExportDataToCSV(date.Men_21_39, date.NrofCheckpoints, saveFileDialog1.FileName);
                    Cursor.Current = defaultCursor;
                    MessageBox.Show("Export realizat cu succes !");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void masculin40ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV File|*.csv";
            saveFileDialog1.Title = "Export";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "")
            {
                try
                {
                    Cursor defaultCursor = Cursor.Current;
                    Cursor.Current = Cursors.WaitCursor;
                    ExportToCSV.ExportDataToCSV(date.Master_Men_40_plus, date.NrofCheckpoints, saveFileDialog1.FileName);
                    Cursor.Current = defaultCursor;
                    MessageBox.Show("Export realizat cu succes !");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void hobbyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV File|*.csv";
            saveFileDialog1.Title = "Export";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "")
            {
                try
                {
                    Cursor defaultCursor = Cursor.Current;
                    Cursor.Current = Cursors.WaitCursor;
                    ExportToCSV.ExportDataToCSV(date.U15_Juniors, date.NrofCheckpoints, saveFileDialog1.FileName);
                    Cursor.Current = defaultCursor;
                    MessageBox.Show("Export realizat cu succes !");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void userManualToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this, "EnduroAppHelp.chm");
        }
    }
}
