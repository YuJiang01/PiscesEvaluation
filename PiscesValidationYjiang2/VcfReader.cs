using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PiscesValidationYjiang2
{
	public class VcfReader
	{
		public string VcfPath;
		private bool _isLofreq;
		private StreamReader _vcfStreamReader;
		private VariantCallMethod _method;


		public VcfReader(string vcfPath, VariantCallMethod method)
		{
			var fileStream = new FileStream(vcfPath, FileMode.Open, FileAccess.Read, FileShare.Read);
			_vcfStreamReader = new StreamReader(fileStream);
			_method = method;
			_isLofreq = _method == VariantCallMethod.Lofreq;
		}

		public IEnumerable<VcfVariant> GetAllVcfVariants()
		{
			string line;
			while ((line = _vcfStreamReader.ReadLine()) != null)
			{
				if(line.StartsWith("#")) continue;

				var variant = GetVcfVariant(line);
				if(variant!=null)
					yield return variant;
			}
		}

		private VcfVariant GetVcfVariant(string line)
		{
			var lineInfo = line.Split('\t');
			if (lineInfo.Length < 8) return null;

			string referenceName = lineInfo[0];
			string referencePos = lineInfo[1];
			string referenceAllele = lineInfo[3];
			List<string> altAlleles = lineInfo[4].Split(',').ToList();
			int quality = Int32.Parse(lineInfo[5]);
			string filter = lineInfo[6];
			double variantFreq = 0;
			List<string> genotypes = new List<string>();

			//parse variantFreq
			string genotype= "./.";

			if (_isLofreq)
			{
				List<string> infos = lineInfo[7].Split(';').ToList();
				foreach (var info in infos)
				{
					if (info.StartsWith("AF="))
					{
						variantFreq = double.Parse(info.Substring(3));
					}
				}
				if (variantFreq > 0 && variantFreq < 1)
					genotype = "0/1";
				genotypes.Add(genotype);
				return new VcfVariant(referenceName, referencePos, referenceAllele, altAlleles, variantFreq, filter, quality,genotypes);
			}

			List<string> descriptions = lineInfo[8].Split(':').ToList();
			List<string> values = lineInfo[9].Split(':').ToList();
			if (descriptions.Count != values.Count)
			{
				throw new Exception("the length of format and values are not equal\n");
			}

			for (var i = 0; i < descriptions.Count; i++)
			{
				if (descriptions[i].Equals("VF") || descriptions[i].Equals("AF"))
				{
					variantFreq = Double.Parse(values[i]);
				}
				if (descriptions[i].Equals("GT"))
				{
					genotype = values[i];
				}
			}
			genotypes.Add(genotype);

			 return new VcfVariant(referenceName, referencePos, referenceAllele, altAlleles, variantFreq, filter, quality,genotypes);

			
		}
	}
}