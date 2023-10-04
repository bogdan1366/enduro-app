using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace EnduroApp
{
    public partial class AllRidersNew : Form
    {
        MainForm mainInstance;

        public int Etape
        {
            get;
            set;
        }

        public AllRidersNew(MainForm arg)
        {
            mainInstance = arg;
            InitializeComponent();           
        }

        public void CreateGrid()
        {
            this.grdData.Columns.Clear();
            DataGridViewTextBoxColumn No = new DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn Rider = new DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn Category = new DataGridViewTextBoxColumn();
            DataGridViewTextBoxColumn Club = new DataGridViewTextBoxColumn();
            // 
            // No
            //
            No.HeaderText = "Cip";
            No.Name = "No";
            No.ReadOnly = true;
            // 
            // Rider
            // 
            Rider.HeaderText = "Nume/Prenume";
            Rider.Name = "Rider";
            Rider.ReadOnly = true;
            // 
            // Category
            // 
            Category.HeaderText = "Categoria";
            Category.Name = "Category";
            Category.ReadOnly = true;
            //
            // Club
            //
            Club.HeaderText = "Club, Oras";
            Club.Name = "Club";
            Club.ReadOnly = true;

            this.grdData.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {No,Rider,Club, Category});

            DataGridViewTextBoxColumn newColumn;
            for (int i = 0; i <= Etape; i++)
            {
                newColumn = new DataGridViewTextBoxColumn();
                string et = "";
                if (i % 2 == 0)
                {
                    et += "PS ";
                    et += Convert.ToString(i / 2 + 1);
                    newColumn.ReadOnly = true;
                }
                else
                {
                    et += "Penalizari PS ";
                    et += Convert.ToString(i / 2 + 1);
                    newColumn.ReadOnly = false;
                }
                newColumn.HeaderText = et;
                this.grdData.Columns.Add(newColumn);
            }
        }

        private void AllRidersNew_Load(object sender, EventArgs e)
        {
            CreateGrid();
        }

        private void AllRidersNew_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Visible = false;
            e.Cancel = true;
        }

        public void Add_Rider(Riders toAdd)
        {
            DataGridViewRow objRow = new DataGridViewRow();
            var index = grdData.Rows.Add();

            grdData.Rows[index].Cells[0].Value = toAdd.nrConcurs;
            grdData.Rows[index].Cells[1].Value = toAdd.nume;
            grdData.Rows[index].Cells[2].Value = toAdd.club;
            grdData.Rows[index].Cells[3].Value = toAdd.Categoria;
        }

        public void Add_Time(Riders toAdd)
        {

            foreach (DataGridViewRow objRow in grdData.Rows)
            {
                if (((string)objRow.Cells[1].Value == toAdd.nume) && ((int)objRow.Cells[0].Value == toAdd.nrConcurs) && 
                    ((string)objRow.Cells[2].Value == toAdd.club))
                {
                    for (int j = 0; j < (mainInstance.date.NrofCheckpoints); j++)
                    {
                        try
                        {
                            string time;
                            if (j % 2 == 0)
                            {
                                time = toAdd.GetTime(j + 1)[0].ToString("00") + ":" +
                                       toAdd.GetTime(j + 1)[1].ToString("00") + ":" +
                                       toAdd.GetTime(j + 1)[2].ToString("00");
                                if (time.Equals("00:00:00"))
                                {
                                    int[] time_start, time_finish;
                                    time_start = toAdd.GetTimestamp(j + 1);
                                    time_finish = toAdd.GetTimestamp(j + 2);
                                    if (time_start == null)
                                        objRow.Cells[j + 4].Value = "DNS";
                                    else if (time_finish == null)
                                        objRow.Cells[j + 4].Value = "DNF";
                                }
                                else
                                {
                                    objRow.Cells[j + 4].Value = time;
                                }
                            }
                            else
                            {
                                time = toAdd.GetPenalty((j + 1) / 2)[0].ToString("00") + ":" +
                                       toAdd.GetPenalty((j + 1) / 2)[1].ToString("00") + ":" +
                                       toAdd.GetPenalty((j + 1) / 2)[2].ToString("00");
                                objRow.Cells[j + 4].Value = time;
                            }
                        }
                        catch (NullReferenceException)
                        {
                            objRow.Cells[j + 4].Value = null;
                        }
                        catch (ArgumentNullException)
                        {
                            objRow.Cells[j + 4].Value = null;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            objRow.Cells[j + 4].Value = null;
                        }
                    }
                } 
            }

            //http://msdn.microsoft.com/en-us/library/system.windows.forms.datagridview.rows.aspx
            
        }

        public void Add_Rider_and_Time(Riders toAdd)
        {
            DataGridViewRow objRow = new DataGridViewRow();
            var index = grdData.Rows.Add();

            grdData.Rows[index].Cells[0].Value = toAdd.nrConcurs;
            grdData.Rows[index].Cells[1].Value = toAdd.nume;
            grdData.Rows[index].Cells[2].Value = toAdd.club;
            grdData.Rows[index].Cells[3].Value = toAdd.Categoria;

            for (int j = 0; j < (mainInstance.date.NrofCheckpoints); j++)
            {
                try
                {
                    string time;
                    if (j % 2 == 0)
                    {
                        time = toAdd.GetTime(j + 1)[0].ToString("00") + ":" +
                               toAdd.GetTime(j + 1)[1].ToString("00") + ":" +
                               toAdd.GetTime(j + 1)[2].ToString("00");
                        if (time.Equals("00:00:00"))
                        {
                            int[] time_start, time_finish;
                            time_start = toAdd.GetTimestamp(j + 1);
                            time_finish = toAdd.GetTimestamp(j + 2);
                            if (time_start == null)
                                grdData.Rows[index].Cells[j + 4].Value = "DNS";
                            else if (time_finish == null)
                                grdData.Rows[index].Cells[j + 4].Value = "DNF";
                        }
                        else
                        {
                            grdData.Rows[index].Cells[j + 4].Value = time;
                        }
                    }
                    else
                    {
                        time = toAdd.GetPenalty((j + 1) / 2)[0].ToString("00") + ":" +
                               toAdd.GetPenalty((j + 1) / 2)[1].ToString("00") + ":" +
                               toAdd.GetPenalty((j + 1) / 2)[2].ToString("00");
                        grdData.Rows[index].Cells[j + 4].Value = time;
                    }
                }
                catch (NullReferenceException)
                {
                    grdData.Rows[index].Cells[j + 4].Value = null;
                }
                catch (ArgumentNullException)
                {
                    grdData.Rows[index].Cells[j + 4].Value = null;
                }
                catch (ArgumentOutOfRangeException)
                {
                    grdData.Rows[index].Cells[j + 4].Value = null;
                }
            }
        }

        public void Remove_Rider(Riders toRemove)
        {
            foreach (DataGridViewRow objRow in grdData.Rows)
            {
                if (((string)objRow.Cells[1].Value == toRemove.nume) && ((int)objRow.Cells[0].Value == toRemove.nrConcurs))
                {
                    grdData.Rows.Remove(objRow);
                }
            }
        }

        public void Remove_Time(Riders toRemove)
        {
            foreach (DataGridViewRow objRow in grdData.Rows)
            {
                if (((string)objRow.Cells[1].Value == toRemove.nume) && ((int)objRow.Cells[0].Value == toRemove.nrConcurs))
                {
                    for (int j = 0; j < (mainInstance.date.NrofCheckpoints - 1); j++)
                    {
                        objRow.Cells[j + 4].Value = "";
                    }
                }
            }
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string result = Microsoft.VisualBasic.Interaction.InputBox("Introduceti numele rider-ului cautat", " ", " ");
            Boolean riderFound = false;

            foreach (DataGridViewRow objRow in grdData.Rows)
            {
                objRow.Selected = false;
                switch (result.Split().Length)
                {
                    case 1:
                        if (objRow.Cells[1].Value.ToString().ToLower().Contains(result.ToLower().Split()[0]))
                        {
                            objRow.Selected = true;
                            riderFound = true;
                        }
                        break;
                    case 2:
                        if (objRow.Cells[1].Value.ToString().ToLower().Contains(result.ToLower().Split()[0]) && objRow.Cells[1].Value.ToString().ToLower().Contains(result.ToLower().Split()[1]))
                        {
                            objRow.Selected = true;
                            riderFound = true;
                        }
                        break;
                    case 3:
                        if ((objRow.Cells[1].Value.ToString().ToLower().Contains(result.ToLower().Split()[0]) && objRow.Cells[1].Value.ToString().ToLower().Contains(result.ToLower().Split()[1]))
                            || (objRow.Cells[1].Value.ToString().ToLower().Contains(result.ToLower().Split()[1]) && objRow.Cells[1].Value.ToString().ToLower().Contains(result.ToLower().Split()[2]))
                               || (objRow.Cells[1].Value.ToString().ToLower().Contains(result.ToLower().Split()[0]) && objRow.Cells[1].Value.ToString().ToLower().Contains(result.ToLower().Split()[2])))
                        {
                            objRow.Selected = true;
                            riderFound = true;
                        }
                        break;
                }
            }
            if (riderFound == false)
            {
                DialogResult message = MessageBox.Show("Rider-ul cautat nu se afla in baza de date", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void cauatDupaNrConcursToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string result = Microsoft.VisualBasic.Interaction.InputBox("Introduceti numarul de concurs al rider-ului cautat", " ", " ");
            Boolean riderFound = false;

            foreach (DataGridViewRow objRow in grdData.Rows)
            {
                for (int i = 0; i < objRow.Cells.Count - 1; i++)
                {
                    objRow.Cells[i].Selected = false;
                }

                if (objRow.Cells[0].Value.ToString().ToLower().Equals(result.ToLower()))
                {
                    objRow.Selected = true;
                    grdData.CurrentCell = objRow.Cells[0];
                    riderFound = true;
                }
            }
            if (riderFound == false)
            {
                DialogResult message = MessageBox.Show("Rider-ul cautat nu se afla in baza de date", " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }  
        }

        private void grdData_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            foreach (Riders rider in mainInstance.date.AllRiders)
            {
                if ((rider.nume == grdData.Rows[e.RowIndex].Cells[1].Value.ToString()) &&
                    (rider.club == grdData.Rows[e.RowIndex].Cells[2].Value.ToString()) &&
                    (rider.nrConcurs == Int32.Parse(grdData.Rows[e.RowIndex].Cells[0].Value.ToString())))
                {
                    string cellContent;
                    int etapa = (e.ColumnIndex - 3) / 2;
                    int[] pen = new int[3];
                    try
                    {
                        cellContent = grdData.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                    }
                    catch (NullReferenceException)
                    {
                        pen = null;
                        goto setPen;
                    }
                    try
                    {
                        string[] times = cellContent.Split(':');
                        if (times.Count() != 3)
                            throw new Exception("Penalizarile trebuie sa fie de forma hh:mm:ss");
                        for (int i=0 ; i<3; i++)
                        {
                            pen[i] = Int32.Parse(times[i]);
                        }
                        if (pen[0] > 23)
                            throw new Exception("Numarul de ore de penalizare nu poate fi mai mare decat 23");
                        if (pen[1] > 59)
                            throw new Exception("Numarul de minute de penalizare nu poate fi mai mare decat 59");
                        if (pen[2] > 59)
                            throw new Exception("Numarul de secunde de penalizare nu poate fi mai mare decat 59");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        grdData.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = "";
                        return;
                    }
                setPen:
                    try
                    {
                        rider.SetPenalty(pen, etapa);
                        List<Riders> rankingList;
                        switch(rider.categoria)
                        {
                            case Riders.categ.MASTER_WOMEN_35_PLUS:
                                rankingList = mainInstance.date.Master_Women_35_plus;
                                break;
                            case Riders.categ.WOMEN_21_34:
                                rankingList = mainInstance.date.Women_21_34;
                                break;
                            case Riders.categ.U21_WOMEN:
                                rankingList = mainInstance.date.U21_Women;
                                break;
                            case Riders.categ.U21_MEN:
                                rankingList = mainInstance.date.U21_Men;
                                break;
                            case Riders.categ.MEN_21_39:
                                rankingList = mainInstance.date.Men_21_39;
                                break;
                            case Riders.categ.MASTER_MEN_40_PLUS:
                                rankingList = mainInstance.date.Master_Men_40_plus;
                                break;
                            case Riders.categ.U15_JUNIORS:
                                rankingList = mainInstance.date.U15_Juniors;
                                break;
                            default:
                                throw new Exception("Categorie inexistenta");
                        }
                        for (int i = 0; i < rankingList.Count; i++)
                        {
                            if ((rider.nume == rankingList[i].nume) && (rider.club == rankingList[i].club) &&
                                (rider.nrConcurs == rankingList[i].nrConcurs))
                            {
                                rankingList.Remove(rankingList[i]);
                            }
                        }
                        rankingList.Add(rider);
                        rankingList.Sort(mainInstance.comparer);
                        mainInstance.RankingsForm.RefreshCat(rankingList);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, " ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        grdData.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = "";
                        return;
                    }
                }
            }
        }
    }
}
