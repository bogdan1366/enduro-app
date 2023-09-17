using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EnduroApp.ImportExport
{
    public static  class ImportFromCSV
    {
        public static Database Import(string csvFile)
        {
            Database data = null;
            try
            {
                using (TextReader textReader = File.OpenText(csvFile))
                {
                    using (var reader = new CsvReader(textReader))
                    {
                        reader.Configuration.RegisterClassMap<Riders.RidersMap>();
                        var riders = reader.GetRecords<Riders>().ToList();
                        data = new Database();
                        data.AllRiders = riders;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, " ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            return data;
        }
    }
}
