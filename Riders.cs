using CsvHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnduroApp
{
    [Serializable()]
    public class Riders
    {
        #region Public Vars
            public int nrOfCheckpoints;
            public string nume;
            public int nrConcurs;
            public string club;
            public categ categoria;
        #endregion
        #region Properties
            public string Nume
            {
                get
                {
                    return nume;
                }
                set
                {
                    nume = value; 
                }
            }
            public string Club
            {
                get
                {
                    return club;
                }
                set
                {
                    club = value;
                }
            }
            public int NrConcurs
            {
                get
                {
                    return nrConcurs;
                }
                set
                {
                    nrConcurs = value;
                }
            }
            public string Categoria
            {
                get
                {
                    switch (categoria)
                    {
                        case categ.MASTER_WOMEN_35_PLUS:
                            return "Master Women 35+";
                        case categ.WOMEN_21_34:
                            return "Women 21-34";
                        case categ.U21_WOMEN:
                            return "U21 Women";
                        case categ.U21_MEN:
                            return "U21 Men";
                        case categ.MEN_21_39:
                            return "Men 21-39";
                        case categ.MASTER_MEN_40_PLUS:
                            return "Master Men 40+";
                        case categ.U15_JUNIORS:
                            return "U15 Juniors";
                        default:
                            throw new Exception("Category unknown");
                    } 
                }
                set
                {
                    switch (value)
                    {
                        case "Master Women 35+":
                            categoria = categ.MASTER_WOMEN_35_PLUS;
                            break;
                        case "Women 21-34":
                            categoria = categ.WOMEN_21_34;
                            break;
                        case "U21 Women":
                            categoria = categ.U21_WOMEN;
                            break;
                        case "U21 Men":
                            categoria = categ.U21_MEN;
                            break;
                        case "Men 21-39":
                            categoria = categ.MEN_21_39;
                            break;
                        case "Master Men 40+":
                            categoria = categ.MASTER_MEN_40_PLUS;
                            break;
                        case "U15 Juniors":
                            categoria = categ.U15_JUNIORS;
                            break;
                        default:
                            throw new Exception(value + " category unknown");
                    }
                }
            }
        #endregion

            public enum categ : int { U15_JUNIORS, MASTER_MEN_40_PLUS, MEN_21_39, U21_MEN, U21_WOMEN, WOMEN_21_34, MASTER_WOMEN_35_PLUS };
        public bool hasAllCheckpoints = true;
        private byte[] tagID = new byte[4];
        private int[][] time = new int[20][]; // 20 de timpi (unul pentru fiecare checkpoint) 
        private int[][] timpEtape = new int[19][]; // 19 timpi (unul pentru fiecare etapa, inclusiv tranzitiile)
        private int[][] penalizari = new int[10][]; // 10 penalizari (cate una pentru fiecare etapa)
        private int[] timpTotal = new int[3];

        public class RidersMap : CsvHelper.Configuration.CsvClassMap<Riders>
        {
            public RidersMap()
            {
                AutoMap();
                Map(m => m.NrConcurs).Name("Cip");
                Map(m => m.Nume).Name("Nume/Prenume");
                Map(m => m.Club).Name("Club, Oras");
            }
        }

        public Riders()
        {
            this.timpTotal = new int[3] { 0, 0, 0 };
        }

        public Riders(string name, int compNr, categ cat, int totalNrOfCheckpoints)
        {
            this.nrOfCheckpoints = totalNrOfCheckpoints;
            this.nume = name;
            this.nrConcurs = compNr;
            this.categoria = cat;
            this.timpTotal = new int[3] { 0, 0, 0 };
        }

        public Riders(string name, int compNr, string club, categ cat, int totalNrOfCheckpoints)
        {
            this.nrOfCheckpoints = totalNrOfCheckpoints;
            this.nume = name;
            this.nrConcurs = compNr;
            this.club = club;
            this.categoria = cat;
        }

        public int[] GetTime(int etapa)
        {
            try
            {
                return this.timpEtape[etapa - 1];
            }
            catch (IndexOutOfRangeException)
            {
                return new int[3] { 0, 0, 0 };
            }
        }

        public int[] GetTimestamp(int checkpointNr)
        {
            try
            {
                return this.time[checkpointNr - 1];
            }
            catch (IndexOutOfRangeException)
            {
                return new int[3] { 0, 0, 0 };
            }
        }

        public int[] GetPenalty(int etapa)
        {
            try
            {
                return this.penalizari[etapa - 1];
            }
            catch (IndexOutOfRangeException)
            {
                return new int[3] { 0, 0, 0 };
            }
        }

        public int[] GetTotalTime()
        {
            return this.timpTotal;
        }

        public void DeleteTimes()
        {
            this.time = new int[20][];
            this.timpEtape = new int[19][];
            this.timpTotal = new int[3] {0, 0, 0};
            for (int i = 0; i < this.penalizari.Count(); i++)
            {
                this.timpTotal = AddTimes(this.timpTotal, this.penalizari[i]);
            }
            
        }

        public void DeleteTimesAndPenalties()
        {
            this.time = new int[20][];
            this.timpEtape = new int[19][];
            this.timpTotal = new int[3] { 0, 0, 0 };
            this.penalizari = new int [10][];
        }

        public void SetTime(byte[] rawTime, int checkpointNr)
        {
            byte[] faratimp = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            if (!rawTime.SequenceEqual(faratimp))
            {
                int hr = (int)(((rawTime[0] - 0x30) * 10) + ((rawTime[1] - 0x30)));
                int min = (int)(((rawTime[3] - 0x30) * 10) + ((rawTime[4] - 0x30)));
                int sec = (int)(((rawTime[6] - 0x30) * 10) + ((rawTime[7] - 0x30)));
                this.time[checkpointNr - 1] = new int[3] { hr, min, sec };
            }
            if (checkpointNr >= 2)
            {
                try
                {
                    int[] timp1 = new int[3] { this.time[checkpointNr - 1][0], this.time[checkpointNr - 1][1], this.time[checkpointNr - 1][2] };
                    int[] timp2 = new int[3] { this.time[checkpointNr - 2][0], this.time[checkpointNr - 2][1], this.time[checkpointNr - 2][2] };
                    int[] timpEtapa = new int[3];

                    try
                    {
                        timpEtapa = SubtractTimes(timp1, timp2);
                    }
                    catch
                    {
                        timpEtapa = new int[3] { 0, 0, 0 };
                    }

                    this.timpEtape[(checkpointNr) - 2] = timpEtapa;

                    if (checkpointNr % 2 == 0)
                    {
                        this.timpTotal = AddTimes(this.timpTotal, timpEtapa);
                    }

                }
                catch (NullReferenceException)
                {
                    this.hasAllCheckpoints = false;
                    this.timpEtape[(checkpointNr) - 2] = new int[3] { 0, 0, 0 };
                    return;
                }

            }
        }

        public void SetPenalty(int[] Pen, int etapa)
        {
            if (this.penalizari[etapa - 1] != null)
            {
                this.timpTotal = SubtractTimes(this.timpTotal, this.penalizari[etapa - 1]);
            }

            this.penalizari[etapa - 1] = Pen;

            if (Pen != null)
                this.timpTotal = AddTimes(this.timpTotal, Pen);
        }

        public byte[] GetTagID()
        {
            return this.tagID;
        }

        public void SetTagID(byte[] ID)
        {
            this.tagID = ID;
        }

        public int IsFasterThen(Riders altul)
        {
            int nrDePorti = this.nrOfCheckpoints;
            int etapeParcurseThis = 0;
            int etapeParcurseAltul = 0;
            int[] zero = {0, 0, 0};
            try
            {
                for (int i = 0; i < nrDePorti; i += 2)
                {
                    try
                    {
                        if (!this.timpEtape[i].SequenceEqual(zero))
                        {
                            etapeParcurseThis++;
                        }
                    }
                    catch
                    {
                    }
                    try
                    {
                        if (!altul.timpEtape[i].SequenceEqual(zero))
                        {
                            etapeParcurseAltul++;
                        }
                    }
                    catch
                    {
                    }
                }

                if (etapeParcurseThis == etapeParcurseAltul)
                {
                    if (this.timpTotal[0] < altul.timpTotal[0])
                        return 1;
                    if (this.timpTotal[0] > altul.timpTotal[0])
                        return -1;
                    if (this.timpTotal[1] < altul.timpTotal[1])
                        return 1;
                    if (this.timpTotal[1] > altul.timpTotal[1])
                        return -1;
                    if (this.timpTotal[2] < altul.timpTotal[2])
                        return 1;
                    if (this.timpTotal[2] > altul.timpTotal[2])
                        return -1;

                    for (int i = nrDePorti - 2; i >= 0; i -= 2)
                    {
                        if ((this.timpEtape[i] == null) && (altul.timpEtape[i] == null))
                            continue;
                        if ((this.timpEtape[i].SequenceEqual(zero)) && (altul.timpEtape[i].SequenceEqual(zero)))
                            continue;
                        if (this.timpEtape[i].SequenceEqual(zero))
                            return -1;
                        if (altul.timpEtape[i].SequenceEqual(zero))
                            return 1;
                        if (this.timpEtape[i][0] < altul.timpEtape[i][0])
                            return 1;
                        if (this.timpEtape[i][0] > altul.timpEtape[i][0])
                            return -1;
                        if (this.timpEtape[i][1] < altul.timpEtape[i][1])
                            return 1;
                        if (this.timpEtape[i][1] > altul.timpEtape[i][1])
                            return -1;
                        if (this.timpEtape[i][2] < altul.timpEtape[i][2])
                            return 1;
                        if (this.timpEtape[i][2] > altul.timpEtape[i][2])
                            return -1;
                    }
                    return 0;
                }
                else if (etapeParcurseThis > etapeParcurseAltul)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            catch
            {
                throw new Exception("Nu pot compara riderii " + this.nrConcurs.ToString() + " " + this.nume + " si "
                        + altul.nrConcurs.ToString() + " " + altul.nume);
            }
        }

        private int[] AddTimes(int[] time1, int[] time2)
        {
            int[] result = new int[3];

            try
            {
                result = time1;
                result[2] += time2[2];
                if (result[2] >= 60)
                {
                    result[1]++;
                    result[2] -= 60;
                }
                result[1] += time2[1];
                if (result[1] >= 60)
                {
                    result[0]++;
                    result[1] -= 60;
                }
                result[0] += time2[0];
            }
            catch
            {
            }

            return result;
        }

        private int[] SubtractTimes(int[] minuend, int[] subtrahend)
        {
            int[] result = new int[3];
            int minuendSec = minuend[2] + 60 * minuend[1] + 3600 * minuend[0];
            int subtrahendSec = subtrahend[2] + 60 * subtrahend[1] + 3600 * subtrahend[0];

            if (minuendSec > subtrahendSec)
            {
                if (minuend[2] < subtrahend[2])
                {
                    minuend[2] += 60;
                    minuend[1]--;
                }
                result[2] = minuend[2] - subtrahend[2];
                if (minuend[1] < subtrahend[1])
                {
                    minuend[1] += 60;
                    minuend[0]--;
                }
                result[1] = minuend[1] - subtrahend[1];
                result[0] = minuend[0] - subtrahend[0];
            }
            else
            {
                throw new Exception("Cannot subtract " + subtrahend.ToString() + " from " + minuend.ToString());
            }

            return result;
        }

    }
}
