using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GenerateSampleTruth
{
    public class SampleRecord
    {
        public string SubjectId;
        public bool Valid;
        public string SangerResult;
        public string TherascreenResult;
    }



    public class TruthReader
    {
        //SUBJID Sanger  Therascreen S_ACCEPT					
        //203711006	NA	12ASP VALID

        public string truthFile;

        public TruthReader(string fileName)
        {
            truthFile = fileName;
        }

        public List<SampleRecord> GetSampleData()
        {
            List<SampleRecord> records = new List<SampleRecord>();

            using (StreamReader sr = new StreamReader(truthFile))
            {
                while (true)
                {
                    string line = sr.ReadLine();

                    if (string.IsNullOrEmpty(line))
                        break;

                    string[] splat = line.Split('\t');
                    if (splat[0] == "SUBJID")
                        continue;


                    SampleRecord newRec = new SampleRecord();
                    newRec.SubjectId = splat[0].Trim();
                    newRec.SangerResult = splat[1].Trim();
                    newRec.TherascreenResult = splat[2].Trim();

                    newRec.Valid = (splat[3] == "VALID");
 
                    records.Add(newRec);
                }

                return records;
            }
        }

    }
}
