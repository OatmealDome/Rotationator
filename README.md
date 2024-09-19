# Rotationator

Generates a new `VSSetting.byaml` file for Splatoon 1. This application is currently being used by [Pretendo](pretendo.network) to generate rotations for their custom Splatoon 1 server.

## Usage

```
$ dotnet Rotationator.dll --help
Description:
  Generates a new VSSetting BYAMl file.

Usage:
  Rotationator <lastByaml> <outputByaml> [options]

Arguments:
  <lastByaml>    The last VSSetting BYAML file.
  <outputByaml>  The output VSSetting BYAML file.

Options:
  --phaseLength <phaseLength>        The length of each phase in hours. [default: 4]
  --scheduleLength <scheduleLength>  How long the schedule should be in days. [default: 30]
  --overridePhases <overridePhases>  The override phases file. []
  --randomSeed <randomSeed>          The seed for the random number generator. []
  --version                          Show version information
  -?, -h, --help                     Show help and usage information
```

### Phase Override

You can force the generator to override a specific phase at a certain time through an override phases JSON file.

Here is an example of an override file:

```
{
  "2024-09-03T02:00:00.0000000Z": {
    "Length": 4,
    "RegularStages": [
      4,
      8
    ],
    "GachiRule": 2,
    "GachiStages": [
      13,
      15
    ]
  }
}
```

This override forces the phase at `2024-09-03T02:00:00.0000000Z` to have the following characteristics:

* 4 hours in length
* Blackbelly Skatepark and Moray Towers in Regular Battle
* Ancho-V Games and Mahi-Mahi Resort on Splat Zones in Ranked Battle

While this example only contains one phase override, multiple overrides can be specified in a single file.

For all possible values for `GachiRule`, consult the enum defined by `VersusRule.cs`. For a list of stage IDs, consult [this list](https://gist.github.com/OatmealDome/0028b73261ceb702f57531ea48eb7ae0#stages).
