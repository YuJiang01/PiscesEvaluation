using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RunDifferentCombinationArguments
{
	class RunDifferntFreqs
	{
		static void Main(string[] args)
		{

            //string[] testFreqs = {"0.01", "0.02", "0.05", "0.10", "0.20", "0.30", "0.40", "0.50"};
            //string[] testFreqs = {"0.20", "0.30", "0.40", "0.50" };
            //string[] testFreqs = { "0.001", "0.003", "0.005", "0.008" };
            string[] testFreqs = { "0.005", "0.01", "0.02", "0.05" };
            foreach (var testFreq in testFreqs)
			{
				Process p = new Process();
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.RedirectStandardOutput = false;
				p.StartInfo.RedirectStandardError = false;
				p.StartInfo.ErrorDialog = false;
				p.StartInfo.CreateNoWindow = false;
                
                //run in my local computer
                //p.StartInfo.FileName = @"C:\Projects\Illumina.Bioinformatics\Dev\Trunk\yjiang2\PiscesValidationYjiang2\PiscesValidationYjiang2\bin\x64\Release\PiscesValidationYjiang2.exe";

                //run in bioinf02
                p.StartInfo.FileName = @"C:\Users\yjiang2\Source\Workspaces\Workspace\Illumina.Bioinformatics\Dev\Trunk\yjiang2\PiscesValidationYjiang2\PiscesValidationYjiang2\bin\x64\Release\PiscesValidationYjiang2.exe";
                var testFreqInfors = testFreq.Split('.');
				string testFreqPercent = testFreqInfors[1];
                string outputDir = @"\\ussd-prd-isi04\pisces\ValidationResults\Pisces5.0.1\Pumice\11302015\PiscesValidationROC\A1B20Freq" + testFreqPercent;
                p.StartInfo.Arguments = @"-MakeIntervals false -MakeVcfs true -ScoreResults false -t \\ussd-prd-isi04\pisces\ValidationResults\Pisces5.0.1\Pumice\data\pumice_truth\ -i \\sdwrk-0001\ -g \\ussd-prd-isi04\igenomes\Homo_sapiens\UCSC\hg19\Sequence\WholeGenomeFasta -p \\ussd-prd-isi04\Pisces\Builds\5.0.1.10\CallSomaticVariants.exe  -interval \\ussd-prd-isi04\pisces\ValidationResults\Pisces5.0.1\Pumice\data\KRASandNRASinterval.picard -a 1 -b 20 -c 900 -f "+ testFreq+" -o "+outputDir;
				//Console.WriteLine(p.StartInfo.FileName);
				//Console.WriteLine(p.StartInfo.Arguments);
				p.Start();
				p.WaitForExit();

			}
            Console.ReadKey();
			
		}
	}
}
