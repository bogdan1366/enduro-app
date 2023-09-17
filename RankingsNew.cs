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
            this.grdDataMasculin_40_plus.Columns.Clear();
            this.grdDataMasculin_30_39.Columns.Clear();
            this.grdDataMasculin_19_29.Columns.Clear();
            this.grdDataMasculin_15_18.Columns.Clear();
            this.grdDataFeminin_19_plus.Columns.Clear();
            this.grdDataFeminin_15_18.Columns.Clear();
            this.grdDataHobby.Columns.Clear();
            
            foreach (Riders.categ cat in Enum.GetValues(typeof(Riders.categ)))
            {
                DataGridViewTextBoxColumn newColumn;
                DataGridView grdToUpdate;
                switch (cat)
                {
                    case Riders.categ.FEMININ_15_18:
                        grdToUpdate = this.grdDataFeminin_15_18;
                        break;
                    case Riders.categ.FEMININ_19_PLUS:
                        grdToUpdate = this.grdDataFeminin_19_plus;
                        break;
                    case Riders.categ.MASCULIN_15_18:
                        grdToUpdate = this.grdDataMasculin_15_18;
                        break;
                    case Riders.categ.MASCULIN_19_29:
                        grdToUpdate = this.grdDataMasculin_19_29;
                        break;
                    case Riders.categ.MASCULIN_30_39:
                        grdToUpdate = this.grdDataMasculin_30_39;
                        break;
                    case Riders.categ.MASCULIN_40_PLUS:
                        grdToUpdate = this.grdDataMasculin_40_plus;
                        break;
                    case Riders.categ.HOBBY:
                        grdToUpdate = this.grdDataHobby;
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

                if (toRefresh.Equals(mainInstance.date.Feminin_15_18))
                    gridToRefresh = grdDataFeminin_15_18;
                else if (toRefresh.Equals(mainInstance.date.Feminin_19_plus))
                    gridToRefresh = grdDataFeminin_19_plus;
                else if (toRefresh.Equals(mainInstance.date.Masculin_15_18))
                    gridToRefresh = grdDataMasculin_15_18;
                else if (toRefresh.Equals(mainInstance.date.Masculin_19_29))
                    gridToRefresh = grdDataMasculin_19_29;
                else if (toRefresh.Equals(mainInstance.date.Masculin_30_39))
                    gridToRefresh = grdDataMasculin_30_39;
                else if (toRefresh.Equals(mainInstance.date.Masculin_40_plus))
                    gridToRefresh = grdDataMasculin_40_plus;
                else if (toRefresh.Equals(mainInstance.date.Hobby))
                    gridToRefresh = grdDataHobby;
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
