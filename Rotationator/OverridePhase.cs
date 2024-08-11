namespace Rotationator;

public class OverridePhase
{
    public int Length
    {
        get;
        set;
    }
    
    public List<int> RegularStages
    {
        get;
        set;
    }

    public VersusRule GachiRule
    {
        get;
        set;
    }

    public List<int> GachiStages
    {
        get;
        set;
    }

    public Dictionary<string, dynamic> ToByamlCompatibleFormat()
    {
        return new Dictionary<string, dynamic>()
        {
            { "Length", Length },
            { "RegularStages", RegularStages },
            { "GachiRule", GachiRule.ToEnumString() },
            { "GachiStages", GachiStages }
        };
    }
}