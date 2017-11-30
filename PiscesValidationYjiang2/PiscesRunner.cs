using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PiscesValidationYjiang2
{
    public class PiscesRunner: AbstractProgramRunner
    {

	    public override void SetArgument(string program, string fixedArgument, string bamFile, string outFolder)
	    {
			_argument = string.Format(fixedArgument + $" -OutFolder {outFolder} -B {bamFile}");
		    _programPath = program;
	    }


    }
}
