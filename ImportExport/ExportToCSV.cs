using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EnduroApp.ImportExport
{
    public static class ExportToCSV
    {
        public static void ExportDataToCSV(List<Riders> clasament, int NrofCheckpoints,string file)
        {
            using (TextWriter textWriter = File.CreateText(file))
            {
                using (var writer = new CsvWriter(textWriter))
                {

                    var etape = NrofCheckpoints - 1;
                    writer.WriteField("Cip");
                    writer.WriteField("Nume/Prenume");
                    writer.WriteField("Club, Oras");
                    writer.WriteField("Categoria");
                    for (int i = 0; i < etape; i+=2)
                    {
                        string et = "";
                        et += "PS";
                        et += Convert.ToString(i / 2 + 1);
                        string cp = "(CP";
                        cp += Convert.ToString(i + 1);
                        cp += ")";
                        writer.WriteField("Start" + et + cp);
                        cp = "(CP";
                        cp += Convert.ToString(i + 2);
                        cp += ")";
                        writer.WriteField("Finish" + et + cp);
                        writer.WriteField(et);
                        writer.WriteField("Pen " + et);
                    }
                    writer.WriteField("Timp total");
                    writer.WriteField("Loc");
                    writer.NextRecord();
                    int pozClas = 0;
                    foreach(var rider in clasament)
                    {
                        writer.WriteField(rider.NrConcurs);
                        writer.WriteField(rider.Nume);
                        writer.WriteField(rider.Club);
                        writer.WriteField(rider.Categoria);

                        for (int j = 0; j < etape; j+=2)
                        {
                            try
                            {
                                // Start PSn
                                string time = rider.GetTimestamp(j + 1)[0].ToString("00") + ":" + rider.GetTimestamp(j + 1)[1].ToString("00") + ":" + rider.GetTimestamp(j + 1)[2].ToString("00");
                                writer.WriteField(time);
                            }
                            catch
                            {
                                writer.WriteField("");
                            }
                            try
                            {
                                // Finish PSn
                                string time = rider.GetTimestamp(j + 2)[0].ToString("00") + ":" + rider.GetTimestamp(j + 2)[1].ToString("00") + ":" + rider.GetTimestamp(j + 2)[2].ToString("00");
                                writer.WriteField(time);
                            }
                            catch
                            {
                                writer.WriteField("");
                            }
                            try
                            {
                                // Timp PSn
                                string time = rider.GetTime(j + 1)[0].ToString("00") + ":" + rider.GetTime(j + 1)[1].ToString("00") + ":" + rider.GetTime(j + 1)[2].ToString("00");
                                if (time.Equals("00:00:00"))
                                {
                                    int[] time_start, time_finish;
                                    time_start = rider.GetTimestamp(j + 1);
                                    time_finish = rider.GetTimestamp(j + 2);
                                    if (time_start == null)
                                        time = "DNS";
                                    else if (time_finish == null)
                                        time = "DNF";
                                }
                                writer.WriteField(time);
                            }
                            catch
                            {
                                writer.WriteField("");
                            }
                            try
                            {
                                // Penalizari PSn
                                string time = rider.GetPenalty(j / 2 + 1)[0].ToString("00") + ":" + rider.GetPenalty(j / 2 + 1)[1].ToString("00") + ":" + rider.GetPenalty(j / 2 + 1)[2].ToString("00");
                                writer.WriteField(time);
                            }
                            catch
                            {
                                writer.WriteField("");
                            }
                        }
                        try
                        {
                            // Timp total
                            string time = rider.GetTotalTime()[0].ToString("00") + ":" + rider.GetTotalTime()[1].ToString("00") + ":" + rider.GetTotalTime()[2].ToString("00");
                            writer.WriteField(time);
                        }
                        catch
                        {
                            writer.WriteField("");
                        }
                        // Pozitie in clasament
                        pozClas++;
                        writer.WriteField(pozClas.ToString());
                        writer.NextRecord();
                    }
                }
            }
        }
    }
}
