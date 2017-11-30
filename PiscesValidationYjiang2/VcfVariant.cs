using System.Collections.Generic;

namespace PiscesValidationYjiang2
{
	public class VcfVariant
	{
		public string RefName;
		public string RefPosition;
		public string RefAllele;
		public List<string> VariantAlleles;
		public double VariantFreq;
		public string Filter;
		public int Quality;
		public List<string> Genotypes; 

		public VcfVariant(string refName,string refPos,string refAllele,List<string> variantAlleles,double variantFreq, string filter,int quality, List<string> genotypes=null)
		{
			RefName = refName;
			RefPosition = refPos;
			RefAllele = refAllele;
			VariantAlleles = variantAlleles;
			VariantFreq = variantFreq;
			Filter = filter;
			Quality = quality;
			Genotypes = genotypes;
		}
	}
}