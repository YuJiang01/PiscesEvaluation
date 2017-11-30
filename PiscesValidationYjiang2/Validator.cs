using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PiscesValidationYjiang2
{
	/*
        Definition of validating result
        TP-true positive: the mutation exist = 1 in the truth table and is detected by variant caller
        TN-true negative: the mutation exist =0 & status = "PASS" in the truth table and is not detected by variant caller
        FP-false positive: the mutation exist =0 & status = "PASS" in the truth table and is detected by variant caller
        FN-false negative: the mutation exist =1 & status = "PASS" in the truth table and is detected by variant caller

        Considering variants not are labeled as IGNORE, based on the assumption that each sample has at most one variant in the extended sheet
        TNp-true negative prime: the mutation exist =0 & status != "FAIL" in the truth table and is not detected by variant caller
        FPp-false positive prime: the mutation exist =0 & status = != "FAIL" in the truth table and is detected by variant caller
        FNp-false negative prime:  the mutation exist =1 & status = "PASS" in the truth table and is detected by variant caller (same as FN)


        complexCase = when the mutation identified in vcf exist but it is different to the true mutations (not a reference allele)
    */

	public class Validator
	{

		#region members

		private readonly VariantCallMethod _method;
		private readonly List<string> _bamFiles;
		private readonly Dictionary<string, TruthItem> _truthBySample;
		private readonly double _minimumFreq;
		private readonly string _outPutDir;
		#endregion

		public Validator(List<string> bamFiles, Dictionary<string, TruthItem> truthBySample, VariantCallMethod method,
			string outDir, double minFreq)
		{
			_bamFiles = bamFiles;
			_truthBySample = truthBySample;
			_method = method;
			_minimumFreq = minFreq;
			_outPutDir = outDir;

		}

		public void Validate()
		{
			var resultsDir = Path.Combine(_outPutDir, "AddFreq" + _minimumFreq.ToString("0.####"));
			if (!Directory.Exists(resultsDir))
				Directory.CreateDirectory(resultsDir);

			var resultsLog = Path.Combine(resultsDir, "ResultSummary.txt");
			var variantsLog = Path.Combine(resultsDir, "VariantsSummary.txt");
			var finalLog = Path.Combine(resultsDir, "FinalSummary.txt");
			var totalFalsePositive = 0;
			var totalTruePostive = 0;
			var totalTruthVariants = 0;

			CleanUpLogs(resultsLog, variantsLog);

			using (StreamWriter resWriter = new StreamWriter(resultsLog, true))
			using (StreamWriter varWriter = new StreamWriter(variantsLog, true))
			{
				resWriter.WriteLine(
					"sampleId\t vcfPath\t truePositive \t falseNegative \t falsePositive \t trueNegative \t complexCase \t falsePositivePrime \t trueNegativePrime \t ValidPositions \t PassPositions \t ExistedVariant ");

				foreach (var bamPath in _bamFiles)
				{
					int truePositive = 0;
					int trueNegative = 0;
					int falsePositive = 0;
					int falseNegative = 0;
					int trueNegativePrime = 0;
					int falsePositivePrime = 0;
					int complexCase = 0;

					string rootFileName = Path.GetFileNameWithoutExtension(bamPath);
					string vcfPath = FindVcfFile(rootFileName);
					//Path.Combine(_outPutDir, rootFileName + ".vcf");
					string sampleId = GetGenerateSampleIdFromRootFileName(rootFileName);

					TruthItem truth = _truthBySample[sampleId];

					if (!File.Exists(vcfPath))
					{
						Logger.WriteToLog($"{vcfPath} does not exist");
						continue;
					}

					var allIdentifiedVariants = new List<VcfVariant>();
					bool goodVcf = true;
					try
					{
						var piscesVcfReader = new VcfReader(vcfPath, _method);
						allIdentifiedVariants = piscesVcfReader.GetAllVcfVariants().ToList();
					}
					catch (Exception ex)
					{
						goodVcf = false;
						Logger.WriteToLog("Problem with " + vcfPath);
						Logger.WriteToLog(ex.ToString());
					}


					if(goodVcf)
						ComputeAccuracy(varWriter, ref truePositive, ref trueNegative, ref falsePositive, ref falseNegative, ref trueNegativePrime, ref falsePositivePrime, ref complexCase, rootFileName, sampleId, truth, allIdentifiedVariants);

					resWriter.WriteLine($"{sampleId}\t{Path.GetFileNameWithoutExtension(vcfPath)}\t{truePositive}\t{falseNegative}\t{falsePositive}\t{trueNegative}\t{complexCase}\t{falsePositivePrime}\t{trueNegativePrime}\t{truth.NumberOfValidPositions}\t{truth.NumberOfPassPositions}\t{truth.NumberOfExistedVariant}");
					totalFalsePositive += falsePositive;
					totalTruePostive += truePositive;
					totalTruthVariants += truth.NumberOfExistedVariant;
				}

			}
			//write the total summary result
			double recall = (double)totalTruePostive / totalTruthVariants;
			double precision = (double)totalTruePostive / (totalTruePostive + totalFalsePositive);

			using (var finalWriter = new StreamWriter(finalLog))
			{
				finalWriter.WriteLine($"{_minimumFreq}\t{_method}\t{recall.ToString("0.####")}\t{precision.ToString("0.####")}\t{totalTruePostive}\t{totalFalsePositive}\t{totalTruthVariants}");
			}
		}

		private void ComputeAccuracy(StreamWriter varWriter, ref int truePositive, ref int trueNegative, ref int falsePositive, ref int falseNegative, ref int trueNegativePrime, ref int falsePositivePrime, ref int complexCase, string rootFileName, string sampleId, TruthItem truth, List<VcfVariant> allIdentifiedVariants)
		{
			foreach (VcfVariant vcfVariant in allIdentifiedVariants)
			{
				//confirm if variant position exist in the truth table
				string varPosId = vcfVariant.RefName + "_" + vcfVariant.RefPosition;

				if (!truth.ValidPositions.ContainsKey(varPosId))
					continue;

				//check if reference allele in truth table and vcf file match
				//if not match means this variants is not detectable in the analysis
				if (!truth.ValidPositions[varPosId].Contains(vcfVariant.RefAllele))
				{
					//Logger.WriteToLog($"{sampleId} at position {varPosId} is {vcfVariant.RefAllele} but in the truth table is ");
					//foreach (var refAllele in truth.ValidPositions[varPosId])
					// Logger.WriteToLog($"{refAllele} ; ");
				}
				//valid position but no call
				if (vcfVariant.VariantAlleles[0] == "." && vcfVariant.Genotypes[0].Contains("."))
				{
					Logger.WriteToLog(
						$"{sampleId} has no call at valid truth position{vcfVariant.RefName}:{vcfVariant.RefPosition}");
					continue;
				}
				if (truth.PassPositions.ContainsKey(varPosId))
				{
					//check if the identified variants a reference call 
					if (vcfVariant.Genotypes[0] == "0/0")
					{
						if (truth.VariantExistPositions.ContainsKey(varPosId))
						{
							falseNegative++;
							WriteToVariantLog(rootFileName, vcfVariant, _method, "FN", varWriter);
						}
						else
						{
							trueNegative++;
							WriteToVariantLog(rootFileName, vcfVariant, _method, "TN", varWriter);
						}

					}
					else
					{
						if (truth.VariantExistPositions.ContainsKey(varPosId))
						{
							string variantAltId = vcfVariant.RefName + "_" +
													   vcfVariant.RefPosition + "_" + vcfVariant.RefAllele + "_" +
													   vcfVariant.VariantAlleles[0];
							if (truth.VariantsInfo.ContainsKey(variantAltId) && vcfVariant.VariantFreq > _minimumFreq)
							{
								truePositive++;
								WriteToVariantLog(rootFileName, vcfVariant, _method, "TP", varWriter);
							}
							else
							{
								if (vcfVariant.VariantFreq < _minimumFreq) continue;
								complexCase++;
								WriteToVariantLog(rootFileName, vcfVariant, _method, "CC", varWriter);
							}
						}
						else
						{
							if (vcfVariant.VariantFreq < _minimumFreq) continue;
							falsePositive++;
							WriteToVariantLog(rootFileName, vcfVariant, _method, "FP", varWriter);
						}
					}

				}
				else
				{
					//check if the identified variants a reference call
					if (vcfVariant.Genotypes[0] == "0/0")
					{
						trueNegativePrime++;
						WriteToVariantLog(rootFileName, vcfVariant, _method, "TNP", varWriter);
					}
					else
					{
						if (vcfVariant.VariantFreq < _minimumFreq) continue;
						falsePositivePrime++;
						WriteToVariantLog(rootFileName, vcfVariant, _method, "FPP", varWriter);
					}
				}

			}
		}

		private string FindVcfFile(string rootFileName)
		{
			switch (_method)
			{
				case VariantCallMethod.Pisces:
					return Path.Combine(_outPutDir, rootFileName + ".vcf");
				case VariantCallMethod.Lofreq:
					return Path.Combine(_outPutDir, rootFileName + ".lofreq.vcf");
				case VariantCallMethod.Vardict:
					return Path.Combine(_outPutDir, rootFileName + ".vardict.vcf");
				default:
					throw new Exception("unknown method for checking vcf file");
			}
		}

		private void WriteToVariantLog(string rootFileName, VcfVariant variant, VariantCallMethod method, string type, StreamWriter writer)
		{

			writer.WriteLine($"{rootFileName}\t{variant.RefName}\t{variant.RefPosition}\t{variant.RefAllele}\t{variant.VariantAlleles[0]}\t" + method + $"\t{type}" + "\t" + variant.VariantFreq.ToString("0.####"));
		}

		public static string GetGenerateSampleIdFromRootFileName(string rootFileName)
		{
			string[] fileNameElements = rootFileName.Split('-');
			if (fileNameElements.Length >= 2)
				return fileNameElements[0];
			string[] fileNameElementsType2 = rootFileName.Split('_');
			return fileNameElementsType2[0];
		}

		private static void CleanUpLogs(string varLog, string resLogg)
		{
			if (File.Exists(varLog))
				File.Delete(varLog);

			if (File.Exists(resLogg))
				File.Delete(resLogg);
		}

	}

}


