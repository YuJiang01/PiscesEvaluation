using System;
using System.Diagnostics;
using System.Threading;

namespace PiscesValidationYjiang2
{
	public abstract	class AbstractProgramRunner
	{
		protected string _programPath;
		protected string _argument;

		public abstract void SetArgument(string program, string fixedArgument, string bamFile,string outFolder);

		public void RunCmd()
		{
			var p = new Process();
			// Redirect the output stream of the child process.
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.ErrorDialog = false;
			p.StartInfo.CreateNoWindow = true;

			p.StartInfo.FileName = _programPath;
			p.StartInfo.Arguments = _argument;



			Console.WriteLine(_programPath + " " + _argument);


			var res = p.Start();

			p.WaitForExit();
			if (!res)
			{
				Logger.WriteToLog(_argument +" failed");
			}
		}

	}
}