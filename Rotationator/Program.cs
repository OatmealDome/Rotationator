using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using OatmealDome.BinaryData;
using OatmealDome.NinLib.Byaml;
using OatmealDome.NinLib.Byaml.Dynamic;
using Rotationator;

//
// Constants
//

const int maximumPhases = 192; // TODO correct?

Dictionary<VersusRule, List<int>> bannedStages = new Dictionary<VersusRule, List<int>>()
{
    {
        VersusRule.Paint,
        new List<int>() // nothing banned
    },
    {
        VersusRule.Goal,
        new List<int>()
        {
            2, // Saltspray Rig
            4, // Blackbelly Skatepark
            14 // Piranha Pit
        }
    },
    {
        VersusRule.Area,
        new List<int>() // nothing banned
    },
    {
        VersusRule.Lift,
        new List<int>()
        {
            2, // Saltspray Rig
            6, // Port Mackerel
        }
    }
};

List<int> defaultStagePool = new List<int>()
{
    0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15
};

List<VersusRule> defaultGachiRulePool = new List<VersusRule>()
{
    VersusRule.Goal,
    VersusRule.Area,
    VersusRule.Lift
};

Random random = new Random();

//
// Command handling
//

Argument<string> lastByamlArg = new Argument<string>("lastByaml", "The last VSSetting BYAML file.");

Argument<string> outputByamlArg = new Argument<string>("outputByaml", "The output VSSetting BYAML file.");

Command command = new RootCommand("Generates a new VSSetting BYAMl file.")
{
    lastByamlArg,
    outputByamlArg
};

command.SetHandler(context => Run(context));

command.Invoke(args);

//
// Entrypoint
//

void Run(InvocationContext context)
{
    Console.WriteLine("run");

    string lastByamlPath = context.ParseResult.GetValueForArgument(lastByamlArg);
    string outputByamlPath = context.ParseResult.GetValueForArgument(outputByamlArg);
    
    dynamic lastByaml = ByamlFile.Load(lastByamlPath);
    
    DateTime lastBaseTime = DateTime.Parse(lastByaml["DateTime"]).ToUniversalTime();
    List<dynamic> lastPhases = lastByaml["Phases"];
    
    //
    // Find phase start point
    //
    
    DateTime loopTime = lastBaseTime;
    DateTime referenceNow = DateTime.UtcNow;

    int lastPhasesStartIdx = -1;

    for (int i = 0; i < lastPhases.Count; i++)
    {
        Dictionary<string, dynamic> phase = lastPhases[i];

        DateTime phaseEndTime = loopTime.AddHours((int)phase["Time"]);

        if (referenceNow >= loopTime && phaseEndTime > referenceNow)
        {
            lastPhasesStartIdx = i;
            break;
        }

        loopTime = phaseEndTime;
    }

    DateTime baseTime;
    List<GambitVersusPhase> currentPhases;

    if (lastPhasesStartIdx != -1)
    {
        baseTime = loopTime;
        currentPhases = lastPhases.Skip(lastPhasesStartIdx).Select(p => new GambitVersusPhase(p)).ToList();
    }
    else
    {
        throw new NotImplementedException("not supported yet");
    }

    // The last phase is set to 10 years, so correct this to the correct phase length.
    currentPhases.Last().Length = 4;
    
    //
    // Generate new phases to fill out the schedule
    //

    List<VersusRule> gachiRulePool = new List<VersusRule>();
    Dictionary<VersusRule, List<int>> stagePools = new Dictionary<VersusRule, List<int>>()
    {
        { VersusRule.Paint, new List<int>() },
        { VersusRule.Goal, new List<int>() },
        { VersusRule.Area, new List<int>() },
        { VersusRule.Lift, new List<int>() }
    };

    for (int i = currentPhases.Count; i < maximumPhases; i++)
    {
        GambitVersusPhase currentPhase = new GambitVersusPhase();
        GambitVersusPhase lastPhase = i != 0 ? currentPhases[i - 1] : new GambitVersusPhase();
        
        VersusRule gachiRule = PickGachiRule(currentPhase.GachiInfo, lastPhase.GachiInfo, gachiRulePool);

        currentPhase.RegularInfo.Rule = VersusRule.Paint;
        currentPhase.RegularInfo.Stages.Add(PickStage(currentPhase, lastPhase, VersusRule.Paint,
            stagePools[VersusRule.Paint]));
        currentPhase.RegularInfo.Stages.Add(PickStage(currentPhase, lastPhase, VersusRule.Paint,
            stagePools[VersusRule.Paint]));
        currentPhase.RegularInfo.Stages.Sort();
        
        currentPhase.GachiInfo.Rule = gachiRule;
        currentPhase.GachiInfo.Stages.Add(PickStage(currentPhase, lastPhase, gachiRule, stagePools[gachiRule]));
        currentPhase.GachiInfo.Stages.Add(PickStage(currentPhase, lastPhase, gachiRule, stagePools[gachiRule]));
        currentPhase.GachiInfo.Stages.Sort();
        
        currentPhase.Length = 4;
        
        currentPhases.Add(currentPhase);
    }
    
    //
    // Write BYAML
    //
    
    // As a fallback in case the schedule isn't updated in time, make the last phase 10 years long.
    currentPhases.Last().Length = 24 * 365 * 10;

    // Set the new base DateTime (this is usually in the JST time zone, but it accepts UTC time as well).
    lastByaml["DateTime"] = baseTime.ToString("yyyy-MM-dd'T'HH:mm:ssK");

    // Set the new phases.
    lastByaml["Phases"] = currentPhases.Select(p => p.ToByamlPhase());
    
    // Add some metadata about this BYAML file and how it was built.
    lastByaml["ByamlInfo"] = new Dictionary<string, dynamic>()
    {
        { "Generator", "Rotationator 1" },
        { "GenerationTime", referenceNow.ToString("O") },
        { "BaseByamlStartTime", baseTime.ToString("O") },
    };
    
    ByamlFile.Save(outputByamlPath, lastByaml, new ByamlSerializerSettings()
    {
        ByteOrder = ByteOrder.BigEndian,
        SupportsBinaryData = false,
        Version = ByamlVersion.One
    });

    File.WriteAllText(outputByamlPath + ".json", JsonSerializer.Serialize(lastByaml, new JsonSerializerOptions()
    {
        WriteIndented = true
    }));
}

//
// Utility function to pick a random element from a pool.
//

T GetRandomElementFromPool<T>(List<T> pool, Func<T, bool> validityChecker)
{
    T element;
    
    do
    {
        element = pool[random.Next(0, pool.Count)];
    } while (!validityChecker(element));
    
    pool.Remove(element);

    return element;
}

//
// Random stage + rule pickers.
//

VersusRule PickGachiRule(GambitStageInfo stageInfo, GambitStageInfo lastStageInfo, List<VersusRule> pool)
{
    if (pool.Count == 0)
    {
        pool.AddRange(defaultGachiRulePool);
    }

    return GetRandomElementFromPool(pool, rule => rule != lastStageInfo.Rule);
}

int PickStage(GambitVersusPhase phase, GambitVersusPhase lastPhase, VersusRule rule, List<int> pool)
{
    List<int> bannedStagesForRule = bannedStages[rule];

    if (pool.Count == 0)
    {
        pool.AddRange(defaultStagePool.Except(bannedStagesForRule));
    }

    bool IsStageValid(int stageId)
    {
        // Don't pick this stage if it's already used in this phase.
        if (phase.RegularInfo.Stages.Contains(stageId) || phase.GachiInfo.Stages.Contains(stageId))
        {
            return false;
        }

        // Don't pick this stage if it's present in the last phase.
        if (lastPhase.RegularInfo.Stages.Contains(stageId) || lastPhase.GachiInfo.Stages.Contains(stageId))
        {
            return false;
        }

        return true;
    }

    // Check if all of our options are invalid.
    if (pool.All(i => !IsStageValid(i)))
    {
        // If so, pick a random stage from the default pool, excluding:
        // - the current phase's stages (in both Regular and Gachi)
        // - the last phase's stages (in both Regular and Gachi)
        // - all banned stages for this rule
        pool = defaultStagePool.Except(phase.RegularInfo.Stages)
            .Except(phase.GachiInfo.Stages)
            .Except(lastPhase.RegularInfo.Stages)
            .Except(lastPhase.GachiInfo.Stages)
            .Except(bannedStagesForRule)
            .ToList();
    }

    return GetRandomElementFromPool(pool, IsStageValid);
}