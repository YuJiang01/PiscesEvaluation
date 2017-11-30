using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PiscesValidationYjiang2
{
	public class PumiceValidator
	{
		#region members
		public Dictionary<string, TruthItem> TruthBySample ;
		public List<string> BamFiles;
		private bool _makevcf;
		private VariantCallMethod _method;
		private string _outputDirectory;
		private string _program;
		private string _fixedArguments;
		private double _minFreq;
		#endregion

		public PumiceValidator(string truthDirectory, string inputBamDirectory,bool makevcf,string outputDirectory,VariantCallMethod method, double minFreq =0.0001, string program=null, string fixedArguments=null)
		{
			TruthBySample = new Dictionary<string, TruthItem>();
			BamFiles = new List<string>();

			_makevcf = makevcf;
			_method = method;
			_outputDirectory = outputDirectory;
			_program = program;
			_fixedArguments = fixedArguments;
			_minFreq = minFreq;

			var sampleTruthFiles = Directory.GetFiles(truthDirectory, "*_truth.txt");

			foreach (var sampleTruthFile in sampleTruthFiles)
			{
				var tmpFileName = Path.GetFileNameWithoutExtension(sampleTruthFile);
				var sampleId = tmpFileName.Split('_')[0];

				//read truthFile
				TruthItem t = new TruthItem(sampleTruthFile, sampleId);
				TruthBySample.Add(sampleId, t);


				var sampleBamFiles = Directory.GetFiles(inputBamDirectory, sampleId + "*.bam",
					SearchOption.AllDirectories);
				if (sampleBamFiles.Length <= 0) continue;
				BamFiles.AddRange(sampleBamFiles);
			}

		}


		public void Execute()
		{
			if (_makevcf)
			{
				RunVariantCaller();
			}

			var validator = new Validator(BamFiles,TruthBySample,_method,_outputDirectory,_minFreq);
			validator.Validate();
		}

		private void RunVariantCaller()
		{
			var bamBatches = new List<List<string>>() {new List<string>()};
			GenerateBatchBams(bamBatches);

			Logger.WriteToLog($"number of bam files: {BamFiles.Count}");
			Logger.WriteToLog($"number of bam batches: {bamBatches.Count}");
			Console.WriteLine($"number of bam files: {BamFiles.Count}");
			Console.WriteLine($"number of bam batches: {bamBatches.Count}");

			for (int i = 0; i < bamBatches.Count; i++)
			{
				var batch = bamBatches[i];
				long bytesOfBam = 0;

				foreach (string bamFileinBatch in batch)
				{
					bytesOfBam += (new FileInfo(bamFileinBatch)).Length;
				}
				if (batch.Count == 0) continue;

				DateTime startingPisces = DateTime.Now;

				Logger.WriteToLog($"Run variant caller for batch {i} at {startingPisces}");

				VaraintCall(batch);
				DateTime endingPisces = DateTime.Now;
				double secondsInPisces = (endingPisces - startingPisces).TotalSeconds;
				double secondPerMegabyte = 1000000*secondsInPisces/(double) bytesOfBam;
				double secondPerBam = secondsInPisces/(double) batch.Count;
				Logger.WriteToLog(string.Join("\t", secondsInPisces, bytesOfBam, secondPerMegabyte, secondPerBam, bamBatches.Count));
			}
		}

		private void VaraintCall(List<string> bamBatch)
		{
			Console.WriteLine(string.Join(" ,",bamBatch));

			switch (_method)
			{
				case VariantCallMethod.Pisces:
					var piscesRunner = new PiscesRunner();
					piscesRunner.SetArgument(_program, _fixedArguments, string.Join(",", bamBatch), _outputDirectory);
					piscesRunner.RunCmd();
					break;

				case VariantCallMethod.Lofreq:
					var lofreqRunner = new LofreqRunner();
					Parallel.ForEach(bamBatch, (bamFile) =>
					{
						lofreqRunner.SetArgument(_program, _fixedArguments, bamFile, _outputDirectory);
						lofreqRunner.RunCmd();
					});
					break;
				case VariantCallMethod.Vardict:
					var vardictRunner = new VardictRunner();
					Parallel.ForEach(bamBatch, (bamFile) =>
					{
						vardictRunner.SetArgument(_program, _fixedArguments, bamFile, _outputDirectory);
						vardictRunner.RunCmd();
					});
					break;

				default:
					throw new Exception("unknown variant caller");
			}
		}


		private void GenerateBatchBams(List<List<string>> bamBatches )
		{
			var batchIdx = 0;

			foreach (var path in BamFiles)
			{
				bamBatches[batchIdx].Add(path);
				if (bamBatches[batchIdx].Count < 10) continue;
				bamBatches.Add(new List<string>());
				batchIdx++;
			}
		}
	}
}