# P.I.A 3.0 (public version)

A small .NET console application (P.I.A 3.0 public version).

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
