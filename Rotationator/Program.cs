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

const int defaultPhaseLength = 4;
const int defaultScheduleLength = 30;

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

Option<int> phaseLengthOption =
    new Option<int>("--phaseLength", () => defaultPhaseLength, "The length of each phase in hours.");

Option<int> scheduleLengthOption = new Option<int>("--scheduleLength", () => defaultScheduleLength,
    "How long the schedule should be in days.");

Option<string?> overridePhasesOption =
    new Option<string?>("--overridePhases", () => null, "The override phases file.");

Command command = new RootCommand("Generates a new VSSetting BYAMl file.")
{
    lastByamlArg,
    outputByamlArg,
    phaseLengthOption,
    scheduleLengthOption,
    overridePhasesOption
};

command.SetHandler(context => Run(context));

command.Invoke(args);

//
// Entrypoint
//

void Run(InvocationContext context)
{
    string lastByamlPath = context.ParseResult.GetValueForArgument(lastByamlArg);
    string outputByamlPath = context.ParseResult.GetValueForArgument(outputByamlArg);
    int phaseLength = context.ParseResult.GetValueForOption(phaseLengthOption);
    int scheduleLength = context.ParseResult.GetValueForOption(scheduleLengthOption);
    string? overridePhasesPath = context.ParseResult.GetValueForOption(overridePhasesOption);
    
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
    currentPhases.Last().Length = phaseLength;
    
    //
    // Load the override phases
    //

    Dictionary<DateTime, OverridePhase> overridePhases;

    if (overridePhasesPath != null)
    {
        string overridePhasesJson = File.ReadAllText(overridePhasesPath);
        Dictionary<string, OverridePhase> overridePhasesStrKey =
            JsonSerializer.Deserialize<Dictionary<string, OverridePhase>>(overridePhasesJson)!;

        overridePhases = overridePhasesStrKey.Select(p =>
            new KeyValuePair<DateTime, OverridePhase>(DateTime.Parse(p.Key).ToUniversalTime(), p.Value)).ToDictionary();
        
        Console.WriteLine($"Loaded {overridePhases.Count} override phases");
    }
    else
    {
        overridePhases = new Dictionary<DateTime, OverridePhase>();
    }
    
    //
    // Find the maximum number of phases to add.
    //
    
    DateTime endTime = baseTime.AddDays(scheduleLength);
    
    loopTime = baseTime;
    
    for (int i = 0; i < currentPhases.Count; i++)
    {
        GambitVersusPhase phase = currentPhases[i];

        // This is the most convenient place to do this.
        if (overridePhases.TryGetValue(loopTime, out OverridePhase? overridePhase))
        {
            phase.ApplyOverridePhase(overridePhase);
        }
        
        loopTime = loopTime.AddHours(phase.Length);
    }

    DateTime newPhaseBaseTime = loopTime;

    int maximumPhases = currentPhases.Count;

    // This definitely isn't the most efficient way to do this, but it works.
    while (endTime > loopTime)
    {
        maximumPhases++;
        
        int length;
        
        if (overridePhases.TryGetValue(loopTime, out OverridePhase? phase))
        {
            length = phase.Length;
        }
        else
        {
            length = phaseLength;
        }
        
        loopTime = loopTime.AddHours(length);
    }

    Console.WriteLine($"Generating {maximumPhases} phases to reach {endTime:O} (already have {currentPhases.Count})");
    
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

    DateTime currentTime = newPhaseBaseTime;

    for (int i = currentPhases.Count; i < maximumPhases; i++)
    {
        GambitVersusPhase currentPhase = new GambitVersusPhase();
        GambitVersusPhase lastPhase = i != 0 ? currentPhases[i - 1] : new GambitVersusPhase();

        if (overridePhases.TryGetValue(currentTime, out OverridePhase? overridePhase))
        {
            currentPhase.ApplyOverridePhase(overridePhase);
        }
        
        // Calculate next phase time
        
        if (currentPhase.Length <= 0)
        {
            currentPhase.Length = phaseLength;
        }
        
        DateTime nextPhaseTime = currentTime.AddHours(currentPhase.Length);
        
        // Grab the next override phase for later use

        overridePhases.TryGetValue(nextPhaseTime, out OverridePhase? nextOverridePhase);
        
        // Populate rules and stages

        currentPhase.RegularInfo.Rule = VersusRule.Paint;

        for (int j = currentPhase.RegularInfo.Stages.Count; j < 2; j++)
        {
            currentPhase.RegularInfo.Stages.Add(PickStage(currentPhase, lastPhase, nextOverridePhase, VersusRule.Paint,
                stagePools[VersusRule.Paint]));
        }
        
        currentPhase.RegularInfo.Stages.Sort();
        
        if (currentPhase.GachiInfo.Rule == VersusRule.None)
        {
            currentPhase.GachiInfo.Rule = PickGachiRule(currentPhase.GachiInfo, lastPhase.GachiInfo, nextOverridePhase,
                gachiRulePool);
        }
        
        for (int j = currentPhase.GachiInfo.Stages.Count; j < 2; j++)
        {
            currentPhase.GachiInfo.Stages.Add(PickStage(currentPhase, lastPhase, nextOverridePhase,
                currentPhase.GachiInfo.Rule, stagePools[currentPhase.GachiInfo.Rule]));
        }
        
        currentPhase.GachiInfo.Stages.Sort();
        
        currentPhases.Add(currentPhase);

        currentTime = nextPhaseTime;
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
    
    Console.WriteLine("Done!");
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

VersusRule PickGachiRule(GambitStageInfo stageInfo, GambitStageInfo lastStageInfo, OverridePhase? nextPhaseOverride,
    List<VersusRule> pool)
{
    if (pool.Count == 0)
    {
        pool.AddRange(defaultGachiRulePool);
    }

    return GetRandomElementFromPool(pool, rule =>
    {
        if (nextPhaseOverride != null)
        {
            if (nextPhaseOverride.GachiRule == rule)
            {
                return false;
            }
        }
        
        return rule != lastStageInfo.Rule;
    });
}

int PickStage(GambitVersusPhase phase, GambitVersusPhase lastPhase, OverridePhase? nextPhaseOverride, VersusRule rule,
    List<int> pool)
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
        // - the next phase's stages (in both Regular and Gachi, if known)
        // - all banned stages for this rule
        IEnumerable<int> newPool = defaultStagePool.Except(phase.RegularInfo.Stages)
            .Except(phase.GachiInfo.Stages)
            .Except(lastPhase.RegularInfo.Stages)
            .Except(lastPhase.GachiInfo.Stages)
            .Except(bannedStagesForRule);

        if (nextPhaseOverride != null)
        {
            newPool = newPool.Except(nextPhaseOverride.RegularStages)
                .Except(nextPhaseOverride.GachiStages);
        }

        pool = newPool.ToList();
    }

    return GetRandomElementFromPool(pool, IsStageValid);
}
