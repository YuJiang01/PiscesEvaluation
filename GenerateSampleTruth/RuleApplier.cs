using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SequencingFiles;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace GenerateSampleTruth
{
    
    public class MutationTruth
    {
        public string MutationName;
        public bool MutationExist;
        public string MutationStatus;
        public string MutationSource;
        public string MutationConfirmed;

        public MutationTruth(string mutationName, bool mutationExist,string mutatiionStatus,List<string> mutationConfirmInfor)
        {
            MutationName = mutationName;
            MutationExist = mutationExist;
            MutationStatus = mutatiionStatus;
            MutationSource = mutationConfirmInfor[0];
            MutationConfirmed = mutationConfirmInfor[1];
        }
    }
    public class SampleTruth
    {
        public string SampleName;
        public List<MutationTruth> Variants;

        public SampleTruth(string sampleId)
        {
            SampleName = sampleId;
            Variants = new List<MutationTruth>();
        }

    }

    public class RuleApplier
    {
        //one amino acid change may corespond to multiple base changes
        public Dictionary<string, HashSet<string>> ExonToPositionTable = new Dictionary<string, HashSet<string>>();
        public Dictionary<string, List<string>> AminoAcidToBaseChangeForTherascreenDictionary = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> AminoAcidToBaseChangeForSangerDictionary = new Dictionary<string, List<string>>();
        public Dictionary<string,List<string>> MutationConfirmedInformationFromAminoAcid = new Dictionary<string, List<string>>();

        public List<string> DetectableMutationList = new List<string>(); 
        public List<RefPanelEntry> DetectableMutationEntries = new List<RefPanelEntry>();

        //examples of Therascreen detections:
        //12ASP detected
        //12CYS
        //13ASP
        //WT
        //12VAL detected
        //INVALID

        public RuleApplier(string mutationDataBase)
        {
            if (!File.Exists(mutationDataBase))
            {
                Console.WriteLine($"{mutationDataBase} does not exist");
                System.Environment.Exit(1);
            }
            using (StreamReader mutationDatabaseReader = new StreamReader(mutationDataBase))
            {
                while (!mutationDatabaseReader.EndOfStream)
                {
                    string line = mutationDatabaseReader.ReadLine();

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
                        AddEntryToDictionaries(entry);
                        DetectableMutationEntries.Add(entry);
                    }
                }

                //generate the mutation information 
                foreach (var mutationEntry in DetectableMutationEntries)
                {
                    string currentMutationSource;
                    string currentMutationConfirmed;

                    string mutationID = mutationEntry.Chr + "_" + mutationEntry.FwdStandFirstPositionOfMutation + "_" + mutationEntry.FwdStrandRefAllele + "_" +
                mutationEntry.FwdStrandAltAllele;
                    //check the possible source
                    if (mutationEntry.Gene.Equals("KRAS") && mutationEntry.Exon.Equals("2"))
                    {
                        currentMutationSource = mutationEntry.Gene.Substring(0, 1).ToUpper()+mutationEntry.Exon + "_" + mutationEntry.Codon;
                        string theraAminoAcidChange = "KRAS_" + mutationEntry.AminoAcidPosition + mutationEntry.AminoAcidAlt.ToUpper();
                        if (AminoAcidToBaseChangeForTherascreenDictionary[theraAminoAcidChange].Count == 1)
                        {
                            currentMutationConfirmed = "confirmed";
                        }else if (AminoAcidToBaseChangeForTherascreenDictionary[theraAminoAcidChange].Count > 1)
                        {
                            currentMutationConfirmed = "plausible";
                        }
                        else
                        {
                            Console.WriteLine($"unrecogonized: {theraAminoAcidChange}");
                            throw new Exception("unregonized mutation in therascreen List, must something wrong");
                        }

                    }
                    else
                    {
                        currentMutationSource = mutationEntry.Gene.Substring(0, 1).ToUpper() + mutationEntry.Exon + "_" + mutationEntry.Codon;
                        if (AminoAcidToBaseChangeForSangerDictionary[currentMutationSource].Count == 1)
                        {
                            currentMutationConfirmed = "confirmed";
                        }else if (AminoAcidToBaseChangeForSangerDictionary[currentMutationSource].Count > 1)
                        {
                            currentMutationConfirmed = "plausible";
                        }
                        else
                        {
                            throw new Exception("unregonized mutation in sanger List, must something wrong");
                        }
                    }
	                //if (!MutationConfirmedInformationFromAminoAcid.ContainsKey(mutationID))
		             MutationConfirmedInformationFromAminoAcid.Add(mutationID,
			                new List<string>() {currentMutationSource, currentMutationConfirmed});
                }
            }

        }

        public List<SampleTruth> ApplyRule(List<SampleRecord> sampleTruthRecords)
        {
            List<SampleTruth> SampleTruthItems = new List<SampleTruth>();
            foreach (var sampleTruthRecord in sampleTruthRecords)
            {
                SampleTruth truthItem = ApplyRule(sampleTruthRecord);
                SampleTruthItems.Add(truthItem);
            }
            return SampleTruthItems;

        } 

        public SampleTruth ApplyRule(SampleRecord sampleTruthRecord)
        {
            SampleTruth sampleTruthInfor = new SampleTruth(sampleTruthRecord.SubjectId);
            InitiateSampleTruth(sampleTruthInfor);

            //check the therascreen result
            //if (sampleTruthRecord.TherascreenResult.ToUpper().Equals("INVALID"))
           // Console.WriteLine($"sample thereascreen result is {sampleTruthRecord.TherascreenResult}\t in {sampleTruthRecord.SubjectId}");    
            if (sampleTruthRecord.TherascreenResult.ToUpper().Equals("WT"))
            {
                setTherascreenResultWT(sampleTruthInfor);
            }
            else
            {
                //since therascreen contain name like 12ASP and 12ASP dectected
                string[] theraNameItems = sampleTruthRecord.TherascreenResult.Split();
                string mutationTheraName = "KRAS_" + theraNameItems[0];
                if (AminoAcidToBaseChangeForTherascreenDictionary.ContainsKey(mutationTheraName))
                {
                    setSangerResultIgnored(sampleTruthInfor);
                    setPartMutationStatus(sampleTruthInfor,"K2","PASS");
                    List<string> mutationNames = AminoAcidToBaseChangeForTherascreenDictionary[mutationTheraName];
                    foreach (var mutationName in mutationNames)
                    {
                        SetValueForDetectedTruthMutation(sampleTruthInfor, mutationName, mutationTheraName);
                    }
                    
                }
                else if(!sampleTruthRecord.TherascreenResult.ToUpper().Equals("INVALID"))
                {
                    throw new Exception("unrecogonized therascreen result: {sampleTruthRecord.TherascreenResult}");

                }
            }


            //check the sanger result
           // Console.WriteLine($"sample sanger result is {sampleTruthRecord.SangerResult}\t in {sampleTruthRecord.SubjectId}");
            string[] sangerResultItems = sampleTruthRecord.SangerResult.Split(',');
            if (sangerResultItems.Length == 2)
            {
                if (sangerResultItems[0].Trim().StartsWith("WT") && sangerResultItems[1].Trim().StartsWith("FAIL"))
                {
                    setSangerResultWT(sampleTruthInfor);
                    string failParts = sangerResultItems[1].Trim().Substring(4);
                    if (failParts.Length < 2)
                    {
                        throw new Exception($"unrecogonized therascreen result: {sampleTruthRecord.SangerResult}");
                    }
                    char GeneParts = failParts[0];
                    failParts = failParts.Remove(0,1);
                    while (failParts.Length > 0)
                    {
                        
                        char currentChar = failParts[0];
                        if (currentChar >= '0' && currentChar <= '9')
                        {
                            char[] failChars = new char[2];
                            failChars[0] = GeneParts;
                            failChars[1] = currentChar;
                            string failPart = new string(failChars);
                            setPartMutationStatus(sampleTruthInfor, failPart.ToUpper(),"FAIL");
                        }else if ((currentChar >= 'A' && currentChar <= 'Z') || (currentChar >= 'a' && currentChar <= 'z'))
                        {
                            GeneParts = currentChar;
                        }
                        failParts = failParts.Remove(0, 1);
                    }
                }
                else
                {
                    throw new Exception($"unrecogonized therascreen result: {sampleTruthRecord.SangerResult}");
                }

            }else if (sangerResultItems.Length == 1)
            {
                //ignore NA case, since already processed when dealing with therascreen result
                if (sampleTruthRecord.SangerResult.ToUpper().Equals("NA"))
                {
                    //continue
                }else if (sampleTruthRecord.SangerResult.ToUpper().Equals("WT"))
                {
                    setSangerResultWT(sampleTruthInfor);
                }else if (sampleTruthRecord.SangerResult.ToUpper().Equals("FAIL"))
                {
                   //need this case, in case of therascreen detected mutation but Sanger was performed and failed (unlikely)
                    setSangerResultFail(sampleTruthInfor);
                }
                else
                {
                    //detected a mutation or other unrecognized result
                    if (AminoAcidToBaseChangeForSangerDictionary.ContainsKey(sampleTruthRecord.SangerResult))
                    {
                        setSangerResultWT(sampleTruthInfor);
                        List<string> mutationNames = AminoAcidToBaseChangeForSangerDictionary[sampleTruthRecord.SangerResult];
                        foreach (var mutationName in mutationNames)
                        {
                            SetValueForDetectedTruthMutation(sampleTruthInfor, mutationName, sampleTruthRecord.SangerResult);
                        }
                        
                    }
                    else
                    {
                        throw new Exception($"unrecogonized sanger result: {sampleTruthRecord.SangerResult}");
                    }
                    
                }
                
            }
            else
            {
                throw new Exception($"unrecogonized sanger result: {sampleTruthRecord.SangerResult}");
            }
            



            return sampleTruthInfor;

        }

        private void setPartMutationStatus(SampleTruth sampleTruthInfor, string failPart,string status)
        {
            if (ExonToPositionTable.ContainsKey(failPart))
            {
                foreach (string mutationName in ExonToPositionTable[failPart])
                {
                    foreach (var variant in sampleTruthInfor.Variants)
                    {
                        if (variant.MutationName.Equals(mutationName))
                        {
                            variant.MutationExist = false;
                            variant.MutationStatus = status;
                        }
                    }
                }

            }
            else
            {
                throw new Exception($"unregoniced exon name {failPart}");
            }
        }

        private static void SetValueForDetectedTruthMutation(SampleTruth sampleTruthInfor, string mutationName,
            string mutationDetectedName)
        {
           
            bool foundMutationInPanel = false;
            foreach (var mutation in sampleTruthInfor.Variants)
            {
                if (mutation.MutationName.Equals(mutationName))
                {
                    foundMutationInPanel = true;
                    mutation.MutationStatus = "PASS";
                    mutation.MutationExist = true;
                    break;
                }
            }
            if (!foundMutationInPanel)
            {
                throw new Exception(
                    $"{mutationDetectedName} exist in panel table but {mutationName} is not found\n");
            }
        }

        private void setSangerResultFail(SampleTruth sampleTruthInfor)
        {
            setPartMutationStatus(sampleTruthInfor,"K3","FAIL");
            setPartMutationStatus(sampleTruthInfor, "K4", "FAIL");
            setPartMutationStatus(sampleTruthInfor, "N2", "FAIL");
            setPartMutationStatus(sampleTruthInfor, "N3", "FAIL");
            setPartMutationStatus(sampleTruthInfor, "N4", "FAIL");
        }

        private void setSangerResultWT(SampleTruth sampleTruthInfor)
        {
            setPartMutationStatus(sampleTruthInfor, "K3", "PASS");
            setPartMutationStatus(sampleTruthInfor, "K4", "PASS");
            setPartMutationStatus(sampleTruthInfor, "N2", "PASS");
            setPartMutationStatus(sampleTruthInfor, "N3", "PASS");
            setPartMutationStatus(sampleTruthInfor, "N4", "PASS");
        }

        private void setSangerResultIgnored(SampleTruth sampleTruthInfor)
        {
            setPartMutationStatus(sampleTruthInfor, "K3", "IGNORE");
            setPartMutationStatus(sampleTruthInfor, "K4", "IGNORE");
            setPartMutationStatus(sampleTruthInfor, "N2", "IGNORE");
            setPartMutationStatus(sampleTruthInfor, "N3", "IGNORE");
            setPartMutationStatus(sampleTruthInfor, "N4", "IGNORE");
        }

        private void setTherascreenResultWT(SampleTruth sampleTruthInfor)
        {
            setPartMutationStatus(sampleTruthInfor, "K2", "PASS");
        }


        private void InitiateSampleTruth(SampleTruth sampleTruthInfor)
        {
            foreach (string mutation in DetectableMutationList)
            {
                MutationTruth mutationInfor = new MutationTruth(mutation, false, "FAIL",MutationConfirmedInformationFromAminoAcid[mutation]);
                sampleTruthInfor.Variants.Add(mutationInfor);
            }
        }

        private void AddEntryToDictionaries(RefPanelEntry data)
        {
            string exonName = data.Gene.Substring(0, 1).ToUpper() + data.Exon;
            string mutationName = data.Chr + "_" + data.FwdStandFirstPositionOfMutation +"_"+ data.FwdStrandRefAllele + "_" +           
                data.FwdStrandAltAllele;

            //add entry to ExonToPositionTable
            DetectableMutationList.Add(mutationName);
            if (!ExonToPositionTable.ContainsKey(exonName))
            {
                HashSet<string> setOfMutations = new HashSet<string>() {mutationName};
                ExonToPositionTable.Add(exonName, setOfMutations);
            }
            else
            {
                ExonToPositionTable[exonName].Add(mutationName);
            }

            //Add entry to 
            if (exonName.Equals("K2"))
            {
                string aminoAcidChangeForTherascreen = data.Gene+"_" + data.AminoAcidPosition + data.AminoAcidAlt.ToUpper();
                //Console.WriteLine($"aminoAcidChangeForTherascreen = {aminoAcidChangeForTherascreen}");
                if (!AminoAcidToBaseChangeForTherascreenDictionary.ContainsKey(aminoAcidChangeForTherascreen))
                {
                    List<string> newMutationNameList = new List<string>() {mutationName};
                    AminoAcidToBaseChangeForTherascreenDictionary.Add(aminoAcidChangeForTherascreen, newMutationNameList);
                }
                else
                {
                    AminoAcidToBaseChangeForTherascreenDictionary[aminoAcidChangeForTherascreen].Add(mutationName);
                }
                
            }
            else
            {
                string aminoAcidChangeForSanger = data.Gene.Substring(0, 1).ToUpper() + data.Exon + "_" + data.Codon;
                //Console.WriteLine($"aminoAcidChangeForSanger = {aminoAcidChangeForSanger}");
                if (!AminoAcidToBaseChangeForSangerDictionary.ContainsKey(aminoAcidChangeForSanger))
                {
                    List<string> newMutationNameList = new List<string>() {mutationName};
                    AminoAcidToBaseChangeForSangerDictionary.Add(aminoAcidChangeForSanger, newMutationNameList);
                }
                else
                {
                    AminoAcidToBaseChangeForSangerDictionary[aminoAcidChangeForSanger].Add(mutationName);
                }
                
            }
            


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
    }






}
