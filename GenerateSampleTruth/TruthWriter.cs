using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GenerateSampleTruth
{
    public class TruthWriter
    {
        public string OutDir;

        public TruthWriter(string outDir)
        {
            OutDir = outDir;
        }

        public void DumpSampleTruth(List<SampleTruth> sampleTruthData)
        {
            foreach (SampleTruth sampleTruth in sampleTruthData)
            {
                string outfile = Path.Combine(OutDir, sampleTruth.SampleName + "_truth.txt");

                using (StreamWriter sw = new StreamWriter(outfile))
                {
                    sw.WriteLine("#Chr\tPos\tRef\tAlt\tExist\tAvailability\tSource\tConfirmed");
                    foreach (MutationTruth mutation in sampleTruth.Variants)
                    {
                        string mutationInfor = mutation.MutationName.Replace("_", "\t");
                        sw.WriteLine($"{mutationInfor}\t{Convert.ToInt32(mutation.MutationExist)}\t{mutation.MutationStatus}\t{mutation.MutationSource}\t{mutation.MutationConfirmed}");              
                    }
                }


            }
        }
    }
}
