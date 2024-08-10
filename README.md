# Rotationator

Generates a new `VSSetting.byaml` file for Splatoon 1.

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
  --phaseLength <phaseLength>  The length of each phase, in hours. [default: 4]
  --version                    Show version information
  -?, -h, --help               Show help and usage information
```
