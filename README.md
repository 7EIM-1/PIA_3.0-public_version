# P.I.A 3.0 (public version)

The P.I.A project is my attempt at creating a local AI assistant. It all started as a simple voice control with a stupid but somewhat interesting UI (you can find the original project here: https://github.com/7EIM-1/PIA).\
P.I.A 3.0 began when I discovered local AI.


## Overview

This repository contains the source for P.I.A 3.0 (public release). It is a .NET project with the main entry in `Program.cs` and additional UI/agent files such as `Agent.cs` and `CGUI.cs`.

## Prerequisites

- .NET SDK 10.0 or later

## Build

From the repository root run:

```bash
dotnet build
```

## Run

Run the project using:

```bash
dotnet run --project P.I.A_3.0-public_version.csproj
```

Or execute the published binary (after `dotnet publish`).

## Publish (optional)

```bash
dotnet publish -c Release -r linux-x64 -o ./publish
```

## Notes

- Configuration is under the `PIA/config.conf` file.
- Agents are in the `PIA/Agents/` folder and skins in `PIA/skins/`.

If you'd like more detail (usage, flags, or examples), tell me what to include and I will update this README.
