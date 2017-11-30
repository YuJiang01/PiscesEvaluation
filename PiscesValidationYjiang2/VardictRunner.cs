using System.IO;

namespace PiscesValidationYjiang2
{
	public class VardictRunner : AbstractProgramRunner
	{
		public override void SetArgument(string program, string fixedArgument, string bamFile, string outFolder)
		{
			string rootFileName = Path.GetFileNameWithoutExtension(bamFile);
			string outFile = Path.Combine(outFolder, rootFileName + ".vardict.vcf");

			_argument = string.Format($"/bioinfoSD/users/yjiang2/Pisces/pumice/scripts/runVardict.py  \" -b {bamFile} " + fixedArgument +"\" "+ $" {outFile}");

			_programPath = program;
		}
	}
}