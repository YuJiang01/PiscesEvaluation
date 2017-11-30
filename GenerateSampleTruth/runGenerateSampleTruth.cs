using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace GenerateSampleTruth
{
    class Program
    {
        static void Main(string[] args)
        {

			//read truth spread sheet (modified for input)
			string concordanceData = @"C:\Users\yjiang2\ProjectsRelated\PISCES\PiscesValidation\Data\Data_from_Tamsen\modified_COLO_T02_002_ValidOnly_DNA_day.txt";
			//string raspanelData = @"C:\Users\yjiang2\ProjectsRelated\PISCES\PiscesValidation\Data\Data_from_Tamsen\ExtendedRASPanelReferenceTableRev2.txt";
           // string outDir = @"C:\Users\yjiang2\ProjectsRelated\PISCES\PiscesValidation\Data\Pumice_TruthData\testProgram\";
			//string raspanelData = @"C:\Users\yjiang2\ProjectsRelated\PISCES\PiscesValidation\Data\Data_from_Tamsen\ExtendedRASPanelReferenceTableRev2_removeMVN.txt";
			//string outDir = @"\\ussd-prd-isi04\pisces\TestData\ByProject\RasPanel181\Scratch\TruthData\truth_removeMVN\";


			string raspanelData = @"C:\Users\yjiang2\ProjectsRelated\PISCES\PiscesValidation\Data\Data_from_Tamsen\ExtendedRASPanelReferenceTableRev2_reviseMVN.txt";
			string outDir = @"\\ussd-prd-isi04\pisces\TestData\ByProject\RasPanel181\Scratch\TruthData\truth_reviseMVN\";


			Console.WriteLine("reading records");
            TruthReader tr = new TruthReader(concordanceData);
            List<SampleRecord> records = tr.GetSampleData();

            //apply rules per sample
            //need panel and parser
            Console.WriteLine("applying rules");
            RuleApplier ra = new RuleApplier(raspanelData);
            List<SampleTruth> truthData = ra.ApplyRule(records);

            //spit out truth per sample.
            Console.WriteLine("dumping data");
            TruthWriter tw = new TruthWriter(outDir);
            tw.DumpSampleTruth(truthData);
            Console.WriteLine("done");
            Console.ReadKey();
            //optional:
            //compare sample truth with available vcf,
            //spit out score card (with Q score)

            //calcluate precision, recall, FPR (%false detections of all called positions), FDR (%false of all detections)
        }
    }
}
