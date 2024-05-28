namespace Rotationator;

public class GambitStageInfo
{
    public VersusRule Rule
    {
        get;
        set;
    }

    public List<int> Stages
    {
        get;
        set;
    }

    public GambitStageInfo()
    {
        Rule = VersusRule.None;
        Stages = new List<int>();
    }

    public dynamic ToByamlStagesList()
    {
        List<dynamic> byamlStages = new List<dynamic>();
        
        foreach (int id in Stages)
        {
            byamlStages.Add(new Dictionary<string, dynamic>()
            {
                { "MapID", id }
            });
        }

        return byamlStages;
    }
}