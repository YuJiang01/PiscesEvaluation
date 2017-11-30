using System.IO;


namespace PiscesValidationYjiang2
{
	class LofreqRunner:AbstractProgramRunner
	{

		public override void SetArgument(string program, string fixedArgument, string bamFile, string outFolder)
		{
			string rootFileName = Path.GetFileNameWithoutExtension(bamFile);
			string outFile = Path.Combine(outFolder, rootFileName + ".lofreq.vcf");

			_argument = string.Format(fixedArgument + $" -o {outFile}  {bamFile}");
			_programPath = program;
		}



	}
}
