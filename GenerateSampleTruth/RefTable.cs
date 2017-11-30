using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SequencingFiles;

namespace GenerateSampleTruth
{

    internal enum DxMode
    {
        DiagnosticsMode,
        ResearchMode,
        Unknown
    };


    internal interface IPanel
    {
        void LoadEntry(string[] data);
    }

    internal class ControlPanelEntry : RefPanelEntry, IPanel
    {
        public float LowerBound = 0f;
        public float UpperBound = 0f;

        public override void LoadEntry(string[] data)
        {
            base.LoadEntry(data);
            LowerBound = float.Parse(data[6]);
            UpperBound = float.Parse(data[7]);
        }

    }

    public class RefPanelEntry : IPanel
    {
        public string Chr;
        public int FwdStandFirstPositionOfMutation = -1;
        public string Gene;
        public string Exon;
        public string Codon;
        public string ReverseStrandMutationString;
        public string ReverseStrandCodingPositionString;
        public string FwdStrandRefAllele;
        public string FwdStrandAltAllele;
        public string AminoAcidPosition;
        public string AminoAcidRef;
        public string AminoAcidAlt;

        public virtual void LoadEntry(string[] data)
        {
            //+
            //note, all these values coming straight off the table are 
            //for the reverse strand.
            //(ie, in genomic coordinates, exon 4 would come before exon 1)
            // So we have to do some funny business to get these
            //values in the fwd-stranded coordinate system.

            string[] fullMutationDescription = data[5].Trim().Split(' ');
            ReverseStrandCodingPositionString = fullMutationDescription[0];
            ReverseStrandMutationString = string.Empty;
            if (fullMutationDescription.Length > 1)
            {
                ReverseStrandMutationString = fullMutationDescription[1];
            }
            string[] reverseStrandMutationSplat = ReverseStrandMutationString.Split('>');
            string reverseStrandRef = reverseStrandMutationSplat[0];
            string reverseStrandAlt = reverseStrandRef;
            if (reverseStrandMutationSplat.Length > 1)
            {

                reverseStrandAlt = reverseStrandMutationSplat[1];
            }

            if (reverseStrandRef.Length != reverseStrandAlt.Length)
                throw (new ApplicationException("Alert.  This panel should not contain indels!!"));

            //-


            Chr = data[0].Trim();
            int test = -1;
            if (int.TryParse(data[1].Trim(), out test))
                FwdStandFirstPositionOfMutation = test;

            Gene = data[2].Trim();
            Exon = data[3].Trim();
            Codon = data[4].Trim();

            FwdStrandRefAllele = SeqUtils.GetReverseComplement(reverseStrandRef);
            FwdStrandAltAllele = SeqUtils.GetReverseComplement(reverseStrandAlt);

            AminoAcidRef = CodonTable.OneToThreeLetterLookup[Codon.Substring(0, 1)];
            AminoAcidAlt = CodonTable.OneToThreeLetterLookup[Codon.Substring((Codon.Length - 1), 1)];
            AminoAcidPosition = Codon.Substring(1, (Codon.Length - 2));
            //Console.WriteLine($"Gene={Gene}\t Codon={Codon} \t AminoRef = {AminoAcidRef}\t AminoAlt = {AminoAcidAlt}\tAminoPos={AminoAcidPosition}");

        }

        public override string ToString()
        {

            string reportLine = string.Join("\t",
                Chr, FwdStandFirstPositionOfMutation, Gene, Exon, Codon);

            return reportLine;
        }

        public string CodonChangeToNucleotideChange()
        {
            return "c." + ReverseStrandCodingPositionString + ReverseStrandMutationString;
        }

        public string CodonChangeToLongAminoAcidChange()
        {
            string codonChange = Codon;
            string originalAminoAcid = CodonTable.OneToThreeLetterLookup[codonChange[0].ToString()];
            string resultAminoAcid = CodonTable.OneToThreeLetterLookup[codonChange[codonChange.Length - 1].ToString()];
            string aminoAcidNumber = codonChange.Substring(1, codonChange.Length - 2);

            return string.Format("p.{0}{1}{2}", originalAminoAcid, aminoAcidNumber, resultAminoAcid);
        }
    }

    public class Mapping
    {
        public VcfVariant Var;
        public RefPanelEntry Entry;
    }

    public class RefTable
    {

        public Dictionary<string, Dictionary<int, List<RefPanelEntry>>> TableDataByChrAndPos =
            new Dictionary<string, Dictionary<int, List<RefPanelEntry>>>();



        public RefTable(string pathToDatabase)
        {
            if (File.Exists(pathToDatabase))
                ReadPanelFile(pathToDatabase);
        }




        public Dictionary<string, Dictionary<int, List<Mapping>>> IntersectVcfWithPanelVar(string file)
        {

            List<VcfVariant> allVariantsFromVcf = VcfReader.GetAllVariantsInFile(file);
            Dictionary<string, Dictionary<int, List<Mapping>>> foundVariantDataByChrAndPos =
                new Dictionary<string, Dictionary<int, List<Mapping>>>();
            foundVariantDataByChrAndPos["chr12"] = new Dictionary<int, List<Mapping>>();
            foundVariantDataByChrAndPos["chr1"] = new Dictionary<int, List<Mapping>>();

            foreach (VcfVariant vcfVar in allVariantsFromVcf)
            {
                string myChr = vcfVar.ReferenceName;
                int myPos = vcfVar.ReferencePosition;

                if ((myChr != "chr12") && (myChr != "chr1"))
                    continue;

                if (!TableDataByChrAndPos.ContainsKey(myChr))
                    continue;

                //we have a Var or ref call at a position we care about
                if (TableDataByChrAndPos[myChr].ContainsKey(vcfVar.ReferencePosition))
                {
                    List<RefPanelEntry> panelEntryAtPOsition = TableDataByChrAndPos[myChr][myPos];

                    if (!foundVariantDataByChrAndPos[myChr].ContainsKey(myPos))
                    {
                        foundVariantDataByChrAndPos[myChr][myPos] = new List<Mapping>();
                    }

                    foreach (RefPanelEntry entry in panelEntryAtPOsition)
                    {
                        if (CheckForMatch(entry, vcfVar))
                        {
                            Mapping mapping = new Mapping();
                            mapping.Var = vcfVar;
                            mapping.Entry = entry;
                            foundVariantDataByChrAndPos[myChr][myPos].Add(mapping);
                            break;
                        }
                    }

                }
            }
            return foundVariantDataByChrAndPos;
        }

        public static bool CheckForMatch(SequencingFiles.VcfVariant panelVar, SequencingFiles.VcfVariant var)
        {
            bool match = (panelVar.ReferenceName == var.ReferenceName) &&
                         (panelVar.ReferencePosition == var.ReferencePosition) &&
                         (panelVar.ReferenceAllele == var.ReferenceAllele) &&
                         (panelVar.VariantAlleles[0] == var.VariantAlleles[0]);
            return match;
        }

        public static bool CheckForMatch(RefPanelEntry entry, VcfVariant var)
        {
            bool match = (entry.Chr == var.ReferenceName) &&
                         (entry.FwdStandFirstPositionOfMutation == var.ReferencePosition) &&
                         (entry.FwdStrandRefAllele == var.ReferenceAllele) &&
                         (entry.FwdStrandAltAllele == var.VariantAlleles[0]);
            return match;
        }

        internal static void WriteEncryptedPanelFile(string workerPath, string encryptedDataPath,
            string fileName, string revision, string passPhrase)
        {
            //go look to see if we can recreate it from source...
            //revision = "Rev8";

            string srcPath =
                Directory.GetParent(
                    Directory.GetParent(Directory.GetParent(Directory.GetParent(workerPath).ToString()).ToString())
                        .ToString()).ToString();
            string rasPanelDocs = Path.Combine(Path.Combine(Path.Combine(srcPath, "Workflows"), "RasPanelWorker"),
                "Documentation");
            string rawPanelDataPath = Path.Combine(rasPanelDocs, fileName + revision + ".txt");

            if (File.Exists(rawPanelDataPath))
                WriteEncryptedFile(rawPanelDataPath, encryptedDataPath, passPhrase);
            else
            {
                throw (new ApplicationException("Cannot find PanelData in " + encryptedDataPath));
            }
        }

        private void ReadPanelFile(string pathToDatabase)
        {

            using (
                StreamReader reader = new StreamReader(pathToDatabase))
            //new StreamReader(new EncryptedFileStream(pathToDatabase, passPhrase, FileAccess.Read)))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (line == null)
                        break;

                    line = line.Trim();

                    //check its not a header line or comment.
                    if (IsItAComment(line))
                        continue;

                    string[] data = line.Split('\t');

                    //check its not an empty line
                    if (data.Length > 5)
                    {
                        RefPanelEntry entry = new RefPanelEntry();
                        entry.LoadEntry(data);
                        AddEntryToTable(entry);
                    }
                }
            }

        }

        private void AddEntryToTable(RefPanelEntry entry)
        {
            if (!TableDataByChrAndPos.ContainsKey(entry.Chr))
            {
                TableDataByChrAndPos[entry.Chr] = new Dictionary<int, List<RefPanelEntry>>();
            }

            if (!TableDataByChrAndPos[entry.Chr].ContainsKey(entry.FwdStandFirstPositionOfMutation))
            {
                TableDataByChrAndPos[entry.Chr][entry.FwdStandFirstPositionOfMutation] = new List<RefPanelEntry>();
            }

            TableDataByChrAndPos[entry.Chr][entry.FwdStandFirstPositionOfMutation].Add(entry);
        }

        private static bool IsItAComment(string line)
        {
            //new way
            if (line.Contains("#"))
                return true;

            //old way (lame) 
            if ((line.Contains("Chr\tPos\tGene\tExon")) ||
                (line.Contains("*note that these mutations")) ||
                (line.Contains("For example,")) ||
                (line.Contains("The genomic coordinates")))
                return true;
            return false;
        }

        public static void WriteEncryptedFile(string inFile, string outFile, string passPhrase)
        {

            using (StreamReader reader = new StreamReader(inFile))
            {

                using (StreamWriter sw =
                    new StreamWriter(new EncryptedFileStream(outFile, passPhrase, FileAccess.Write)))
                {
                    while (!reader.EndOfStream)
                    {
                        string originalStringsDatabase = reader.ReadLine();
                        sw.WriteLine(originalStringsDatabase);
                    }

                }
            }
        }
    }

    public class SeqUtils
    {
        public static string GetReverseComplement(string dna)
        {
            StringBuilder result = new StringBuilder();
            for (int charIndex = dna.Length - 1; charIndex >= 0; charIndex--)
            {
                switch (dna[charIndex])
                {
                    case 'A':
                        result.Append("T");
                        break;
                    case 'C':
                        result.Append("G");
                        break;
                    case 'G':
                        result.Append("C");
                        break;
                    case 'T':
                        result.Append("A");
                        break;
                    case 'a':
                        result.Append("t");
                        break;
                    case 'c':
                        result.Append("g");
                        break;
                    case 'g':
                        result.Append("c");
                        break;
                    case 't':
                        result.Append("a");
                        break;
                    default:
                        result.Append(dna[charIndex]);
                        break;
                }
            }
            return result.ToString();
        }

    }

}