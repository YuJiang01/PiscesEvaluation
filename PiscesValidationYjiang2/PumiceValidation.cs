using System;
using System.IO;
using NDesk.Options;


namespace PiscesValidationYjiang2
{
    class PumiceValidation
    {

	    private static int Main(string[] args)
	    {
		    var opts = new OptionSet
		    {
			    {
				    "in|i=",
				    "input bam {directory}",
				    v => ConfigurationSettings.InputBamDirectory = v
			    },
			    {
				    "vcf",
				    "enable variant caller",
				    v => ConfigurationSettings.MakeVcf = v != null
			    },
			    {
				    "truth|t=",
				    "truth file {directory}",
				    v => ConfigurationSettings.TruthDicrectory = v 
			    },
			    {
				    "out|o=",
				    "output {directory}",
				    v => ConfigurationSettings.OutPutDirectory = v
			    },
			    {
				    "pro=",
				    "the program to run variant caller",
				    v => ConfigurationSettings.RunningProgram = v
			    },
			    {
				    "arg=",
				    "the argument to run variant caller",
				    v => ConfigurationSettings.VariantCallerArgument = v
			    },
			    {
				    "method=",
				    "the variant calling method",
				    v => ConfigurationSettings.VariantCaller = ConfigurationSettings.ParseVariantCaller(v)
				},
			    {
				    "minF=",
					"addition Frequency cutoff",
					v=>ConfigurationSettings.MinFreq = v
			    }

		    };

		    opts.Parse(args);

			var doExit = ConfigurationSettings.VariantCaller == VariantCallMethod.None || ConfigurationSettings.TruthDicrectory == null;

			if (doExit)
			{
				ShowHelp(opts);
				return -1;
			}


			if (!Directory.Exists(ConfigurationSettings.OutPutDirectory))
			    Directory.CreateDirectory(ConfigurationSettings.OutPutDirectory);
		    Logger.TryOpenLog(ConfigurationSettings.OutPutDirectory);

		    var validator = new PumiceValidator(ConfigurationSettings.TruthDicrectory, ConfigurationSettings.InputBamDirectory,
			    ConfigurationSettings.MakeVcf, ConfigurationSettings.OutPutDirectory, ConfigurationSettings.VariantCaller,
			    Convert.ToDouble(ConfigurationSettings.MinFreq), ConfigurationSettings.RunningProgram, ConfigurationSettings.VariantCallerArgument);
		    validator.Execute();

		    return 0;
	    }

		static void ShowHelp(OptionSet options)
		{
			Console.WriteLine("Usage: PiscesValidationYjiang2.exe [PARAMETERS]");
			Console.WriteLine();
			Console.WriteLine("Parameters:");
			options.WriteOptionDescriptions(Console.Out);
		}

	}
}
