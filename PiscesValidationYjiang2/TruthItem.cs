using System.Collections.Generic;
using System.IO;


namespace PiscesValidationYjiang2
{
    public class TruthVariant
    {
        public string Chr;
        public string Pos;
        public string Alt;
        public string Ref;
        public string VariantStatus; //possible values: PASS, FAIL, IGNORE
        public bool VariantExist;
        public string VariantSource; //Amino Acid change
        public string VariantConfirmed; //possible values: confirmed, plausible
//        public string TestMethod;
        public string VariantId;
        public static TruthVariant FromLine(string s)
        {
            TruthVariant truth = new TruthVariant();
            string[] splat = s.Split();

            truth.Chr = splat[0];
            truth.Pos = splat[1];
            truth.Ref = splat[2];
            truth.Alt = splat[3];
            truth.VariantExist = splat[4].Equals("1");   
            truth.VariantStatus = splat[5];
            truth.VariantSource = splat[6];
            truth.VariantConfirmed = splat[7];
            truth.VariantId = string.Concat(truth.Chr,"_",truth.Pos,"_",truth.Ref,"_",truth.Alt);
            return truth;
        }


    }
    public class TruthItem
    {
        
        public Dictionary<string,TruthVariant> VariantsInfo;
        public string SampleId;
        public int NumberOfExistedVariant;
        public Dictionary<string,bool> PassPositions = new Dictionary<string, bool>();
        public Dictionary<string, HashSet<string>> ValidPositions = new Dictionary<string, HashSet<string>>();
        public Dictionary<string, string> VariantExistPositions = new Dictionary<string, string>();
        public int NumberOfValidPositions;
        public int NumberOfPassPositions;
         

        public TruthItem(string sampleTruthFile, string sampleId)
        {
            SampleId = sampleId;
            VariantsInfo = new Dictionary<string, TruthVariant>();
            using (StreamReader sr = new StreamReader(sampleTruthFile))
            {
                while (true)
                {
                    string line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line))
                        break;

                    TruthVariant t = TruthVariant.FromLine(line);
                    string variantPositionId = t.Chr + "_" + t.Pos;
                    //Logger.WriteToLog($"\n read variant truth: {variantPositionId}\n");
                    if (!t.VariantStatus.Equals("FAIL"))
                    {
                        if (!ValidPositions.ContainsKey(variantPositionId))
                        {
                            ValidPositions[variantPositionId] = new HashSet<string>() {t.Ref};
                        }
                        else
                        {
                            ValidPositions[variantPositionId].Add(t.Ref);
                        }
                           
                        if (t.VariantStatus.Equals("PASS"))
                        {
                            if(!PassPositions.ContainsKey(variantPositionId))
                                PassPositions[variantPositionId] = true;
                            if (t.VariantExist)
                            {
                                if(!VariantExistPositions.ContainsKey(variantPositionId))
                                    VariantExistPositions[variantPositionId] = t.VariantSource;
                                VariantsInfo.Add(t.VariantId, t);
                            }
                                
                        }
                    }
                }
            }

            NumberOfExistedVariant = VariantExistPositions.Count;
            NumberOfPassPositions = PassPositions.Count;
            NumberOfValidPositions = ValidPositions.Count;

            //note: this is a modification based on our current data and cannot generalized to other cases
            //when more than one variant exist check if they are caused by the same source
            if (NumberOfExistedVariant > 1)
            {
                HashSet<string> variantSourceHashSet = new HashSet<string>();
                foreach (KeyValuePair<string, string> kvp in VariantExistPositions)
                {
                    variantSourceHashSet.Add(kvp.Value);
                }
                NumberOfExistedVariant = variantSourceHashSet.Count;
            }
        }

    }

}
