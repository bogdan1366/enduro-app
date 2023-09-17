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
    public partial class RankingsNew : Form
    {
        MainForm mainInstance;

        public int Etape
        {
            get;
            set;
        }

        public RankingsNew(MainForm arg)
        {
            mainInstance = arg;
            InitializeComponent();
        }

        public void CreateGrid()
        {
            this.grdDataMaster_Men_40_plus.Columns.Clear();
            this.grdDataMen_21_39.Columns.Clear();
            this.grdDataU21_Men.Columns.Clear();
            this.grdDataU21_Women.Columns.Clear();
            this.grdDataMaster_Women_35_plus.Columns.Clear();
            this.grdDataWomen_21_39.Columns.Clear();
            this.grdDataU15Juniors.Columns.Clear();
            
            foreach (Riders.categ cat in Enum.GetValues(typeof(Riders.categ)))
            {
                DataGridViewTextBoxColumn newColumn;
                DataGridView grdToUpdate;
                switch (cat)
                {
                    case Riders.categ.MASTER_WOMEN_35_PLUS:
                        grdToUpdate = this.grdDataMaster_Women_35_plus;
                        break;
                    case Riders.categ.WOMEN_21_39:
                        grdToUpdate = this.grdDataWomen_21_39;
                        break;
                    case Riders.categ.U21_WOMEN:
                        grdToUpdate = this.grdDataU21_Women;
                        break;
                    case Riders.categ.U21_MEN:
                        grdToUpdate = this.grdDataU21_Men;
                        break;
                    case Riders.categ.MEN_21_39:
                        grdToUpdate = this.grdDataMen_21_39;
                        break;
                    case Riders.categ.MASTER_MEN_40_PLUS:
                        grdToUpdate = this.grdDataMaster_Men_40_plus;
                        break;
                    case Riders.categ.U15_JUNIORS:
                        grdToUpdate = this.grdDataU15Juniors;
                        break;
                    default:
                        return;
                }
                newColumn = new DataGridViewTextBoxColumn();
                newColumn.ReadOnly = true;
                newColumn.HeaderText = "Cip";
                grdToUpdate.Columns.Add(newColumn);
                newColumn = new DataGridViewTextBoxColumn();
                newColumn.ReadOnly = true;
                newColumn.HeaderText = "Nume/Prenume";
                grdToUpdate.Columns.Add(newColumn);
                newColumn = new DataGridViewTextBoxColumn();
                newColumn.ReadOnly = true;
                newColumn.HeaderText = "Club, Oras";
                grdToUpdate.Columns.Add(newColumn);
                newColumn = new DataGridViewTextBoxColumn();
                newColumn.ReadOnly = true;
                newColumn.HeaderText = "Categoria";
                grdToUpdate.Columns.Add(newColumn);
                for (int i = 0; i < this.Etape; i++)
                {
                    string et = "PS ";
                    et += Convert.ToString(i + 1);
                    newColumn = new DataGridViewTextBoxColumn();
                    newColumn.ReadOnly = true;
                    newColumn.HeaderText = et;
                    grdToUpdate.Columns.Add(newColumn);
                    et = "Penalizari PS " + Convert.ToString(i + 1);
                    newColumn = new DataGridViewTextBoxColumn();
                    newColumn.ReadOnly = true;
                    newColumn.HeaderText = et;
                    grdToUpdate.Columns.Add(newColumn);
                }
                newColumn = new DataGridViewTextBoxColumn();
                newColumn.ReadOnly = true;
                newColumn.HeaderText = "Timp total";
                grdToUpdate.Columns.Add(newColumn);
                newColumn = new DataGridViewTextBoxColumn();
                newColumn.ReadOnly = true;
                newColumn.HeaderText = "Loc";
                grdToUpdate.Columns.Add(newColumn);
            }

        }

        public void RefreshCat(List<Riders> toRefresh)
        {
            try
            {
                System.Windows.Forms.DataGridView gridToRefresh;

                if (toRefresh.Equals(mainInstance.date.Master_Women_35_plus))
                    gridToRefresh = grdDataMaster_Women_35_plus;
                else if (toRefresh.Equals(mainInstance.date.Women_21_39))
                    gridToRefresh = grdDataWomen_21_39;
                else if (toRefresh.Equals(mainInstance.date.U21_Women))
                    gridToRefresh = grdDataU21_Women;
                else if (toRefresh.Equals(mainInstance.date.U21_Men))
                    gridToRefresh = grdDataU21_Men;
                else if (toRefresh.Equals(mainInstance.date.Men_21_39))
                    gridToRefresh = grdDataMen_21_39;
                else if (toRefresh.Equals(mainInstance.date.Master_Men_40_plus))
                    gridToRefresh = grdDataMaster_Men_40_plus;
                else if (toRefresh.Equals(mainInstance.date.U15_Juniors))
                    gridToRefresh = grdDataU15Juniors;
                else
                    return;
                
                gridToRefresh.Rows.Clear();
                
                foreach (Riders rider in toRefresh)
                {
                    var index = gridToRefresh.Rows.Add();

                    gridToRefresh.Rows[index].Cells[0].Value = rider.nrConcurs;
                    gridToRefresh.Rows[index].Cells[1].Value = rider.nume;
                    gridToRefresh.Rows[index].Cells[2].Value = rider.club;
                    gridToRefresh.Rows[index].Cells[3].Value = rider.Categoria;

                    // adauga timpul fiecarei etape
                    for (int j = 0; j < (mainInstance.date.NrofCheckpoints); j ++)
                    {
                        try
                        {
                            string time;
                            if (j % 2 == 0)
                            {
                                time = rider.GetTime(j + 1)[0].ToString("00") + ":" +
                                       rider.GetTime(j + 1)[1].ToString("00") + ":" +
                                       rider.GetTime(j + 1)[2].ToString("00");
                                if (time.Equals("00:00:00"))
                                {
                                    int[] time_start, time_finish;
                                    time_start = rider.GetTimestamp(j + 1);
                                    time_finish = rider.GetTimestamp(j + 2);
                                    if (time_start == null)
                                        gridToRefresh.Rows[index].Cells[j + 4].Value = "DNS";
                                    else if (time_finish == null)
                                        gridToRefresh.Rows[index].Cells[j + 4].Value = "DNF";
                                }
                                else
                                {
                                    gridToRefresh.Rows[index].Cells[j + 4].Value = time;
                                }
                            }
                            else
                            {
                                time = rider.GetPenalty((j + 1) / 2)[0].ToString("00") + ":" +
                                       rider.GetPenalty((j + 1) / 2)[1].ToString("00") + ":" +
                                       rider.GetPenalty((j + 1) / 2)[2].ToString("00");
                                gridToRefresh.Rows[index].Cells[j + 4].Value = time;
                            }
                        }
                        catch (NullReferenceException)
                        {
                            gridToRefresh.Rows[index].Cells[j + 4].Value = null;
                        }
                        catch (ArgumentNullException)
                        {
                            gridToRefresh.Rows[index].Cells[j + 4].Value = null;
                        }
                    }

                    // adauga timpul total
                    try
                    {
                        string time = rider.GetTotalTime()[0].ToString("00") + ":" + rider.GetTotalTime()[1].ToString("00") + ":" + rider.GetTotalTime()[2].ToString("00");
                        gridToRefresh.Rows[index].Cells[gridToRefresh.Columns.Count - 2].Value = time;
                    }
                    catch (NullReferenceException)
                    {
                        gridToRefresh.Rows[index].Cells[gridToRefresh.Columns.Count - 2].Value = null;
                    }
                    catch (ArgumentNullException)
                    {
                        gridToRefresh.Rows[index].Cells[gridToRefresh.Columns.Count - 2].Value = null;
                    }
                    // adauga pozitia in clasament
                    gridToRefresh.Rows[index].Cells[gridToRefresh.Columns.Count - 1].Value = toRefresh.IndexOf(rider) + 1;
                }
            }
            catch
            {

            }
        }

        private void RankingsNew_Load(object sender, EventArgs e)
        {
            CreateGrid();
        }

        private void RankingsNew_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Visible = false;
            e.Cancel = true;
        }
        
    }
}
