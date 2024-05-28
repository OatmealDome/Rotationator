namespace Rotationator;

public static class VersusRuleUtil
{
    public static VersusRule FromEnumString(string str)
    {
        switch (str)
        {
            case "cPnt":
                return VersusRule.Paint;
            case "cVgl":
                return VersusRule.Goal;
            case "cVar":
                return VersusRule.Area;
            case "cVlf":
                return VersusRule.Lift;
            default: // "cNone"
                return VersusRule.None;
        }
    }

    public static string ToEnumString(this VersusRule rule)
    {
        switch (rule)
        {
            case VersusRule.None:
                return "cNone";
            case VersusRule.Paint:
                return "cPnt";
            case VersusRule.Goal:
                return "cVgl";
            case VersusRule.Area:
                return "cVar";
            case VersusRule.Lift:
                return "cVlf";
            default:
                throw new ArgumentOutOfRangeException(nameof(rule), "VersusRule not supported");
        }
    }
}