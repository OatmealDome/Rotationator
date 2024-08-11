namespace Rotationator;

public class GambitVersusPhase
{
    public int Length
    {
        get;
        set;
    }

    public GambitStageInfo RegularInfo
    {
        get;
        set;
    }

    public GambitStageInfo GachiInfo
    {
        get;
        set;
    }
    
    public GambitVersusPhase()
    {
        Length = 0;
        RegularInfo = new GambitStageInfo();
        GachiInfo = new GambitStageInfo();
    }

    public GambitVersusPhase(dynamic byamlPhase)
    {
        Length = byamlPhase["Time"];
        
        dynamic byamlRegularStages = byamlPhase["RegularStages"];

        RegularInfo = new GambitStageInfo()
        {
            Rule = VersusRuleUtil.FromEnumString(byamlPhase["RegularRule"]),
            Stages = new List<int>()
            {
                byamlRegularStages[0]["MapID"],
                byamlRegularStages[1]["MapID"]
            }
        };
        
        dynamic byamlGachiStages = byamlPhase["GachiStages"];

        GachiInfo = new GambitStageInfo()
        {
            Rule = VersusRuleUtil.FromEnumString(byamlPhase["GachiRule"]),
            Stages = new List<int>()
            {
                byamlGachiStages[0]["MapID"],
                byamlGachiStages[1]["MapID"]
            }
        };
    }
    
    public dynamic ToByamlPhase()
    {
        dynamic byamlPhase = new Dictionary<string, dynamic>();

        byamlPhase["Time"] = Length;

        byamlPhase["GachiRule"] = GachiInfo.Rule.ToEnumString();
        byamlPhase["GachiStages"] = GachiInfo.ToByamlStagesList();
        
        byamlPhase["RegularRule"] = RegularInfo.Rule.ToEnumString();
        byamlPhase["RegularStages"] = RegularInfo.ToByamlStagesList();

        return byamlPhase;
    }

    public void ApplyOverridePhase(OverridePhase overridePhase)
    {
        if (overridePhase.Length > 0)
        {
            Length = overridePhase.Length;
        }

        List<int> GetStageList(List<int> overrideList, List<int> normalList)
        {
            List<int> stages = new List<int>();
            
            for (int i = 0; i < 2; i++)
            {
                int overrideStage = overrideList[i];
            
                if (overrideStage != -1)
                {
                    stages.Add(overrideStage);
                }
                else if (i < normalList.Count)
                {
                    stages.Add(normalList[i]);
                }
            }

            return stages;
        }

        RegularInfo.Stages = GetStageList(overridePhase.RegularStages, RegularInfo.Stages);
        GachiInfo.Stages = GetStageList(overridePhase.GachiStages, GachiInfo.Stages);

        if (overridePhase.GachiRule != VersusRule.None)
        {
            GachiInfo.Rule = overridePhase.GachiRule;
        }
    }
}