using System;
using System.Diagnostics;
using System.Net;

namespace PiscesValidationYjiang2
{
	public static class ConfigurationSettings
	{
		public static string TruthDicrectory;
		public static string InputBamDirectory;
		public static string OutPutDirectory;
		public static VariantCallMethod VariantCaller;
		public static string RunningProgram;
		public static string VariantCallerArgument;
		public static bool MakeVcf;
		public static string MinFreq;


		public static VariantCallMethod ParseVariantCaller(string description)
		{
			switch (description.ToLower())
			{
				case "pisces":
					return VariantCallMethod.Pisces;
				case "lofreq":
					return VariantCallMethod.Lofreq;
				case "vardict":
					return VariantCallMethod.Vardict;
				default:
					throw new ApplicationException("unknown variant calling method");

			}
		}
	}

	public enum VariantCallMethod : byte
	{
		None,
		Pisces,
		Lofreq,
		Vardict
	};

	
}