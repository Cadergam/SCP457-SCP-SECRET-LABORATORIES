# SCP457-SCP-SECRET-LABORATORIES
A fully server-side SCP-457 plugin for **SCP: Secret Laboratory**, built with the official LabAPI framework.

The plugin uses SCP-049-2 as the technical base role, replaces its visible role information with **SCP-457**, adds fire-based attacks and introduces portable fire extinguishers that can contain SCP-457. Players do not need to download any client mod or custom asset.

## Features

- SCP-457 uses SCP-049-2 as its gameplay body and keeps the melee attack.
- Custom display name and target information show `SCP-457` instead of `SCP-049-2`.
- Configurable health, burn duration, burn damage and burn tick interval.
- Melee attacks ignite human targets.
- `Fire Burst` ability ignites every human inside a configurable radius.
- Abilities use SCP:SL Server-Specific Settings and configurable keybinds.
- Server-side portable fire extinguishers.
- A special flashlight acts as the extinguisher inventory item.
- A small red 3D extinguisher model follows the holder's right-hand bone.
- If the humanoid hand bone is unavailable, the model uses a safe fallback position near the right hand.
- Improved SCP-457 detection: the aimed target is preferred, otherwise the nearest SCP-457 inside the configured range is selected.
- No client download is required.

## Default gameplay values

| Setting | Default value |
| --- | ---: |
| SCP-457 health | 2200 HP |
| Burn duration | 8 seconds |
| Burn damage | 7 per tick |
| Burn tick interval | 1 second |
| Fire Burst radius | 6 metres |
| Fire Burst cooldown | 25 seconds |
| Extinguisher range | 10 metres |
| Extinguisher charges | 8 |
| Fire intensity removed per hit | 20% |
| Successful hits required to extinguish SCP-457 | 5 |

## Commands and permissions

Commands are entered through Remote Admin.

| Command | Permission | Description |
| --- | --- | --- |
| `scp457 <PlayerID>` | `scp457.spawn` | Turns the selected player into SCP-457. |
| `extincteur <PlayerID>` | `scp457.extinguisher` | Gives the selected human a special extinguisher. |

Command aliases:

- `457` for `scp457`
- `extinguisher` or `giveextincteur` for `extincteur`

## Using the extinguisher

1. An administrator gives the item with `extincteur <PlayerID>`.
2. The player equips the special flashlight received from the command.
3. The player configures `Spray the extinguisher` in Server-Specific Settings. The suggested key is `H`.
4. The player stays within 10 metres of SCP-457 and presses the configured key.
5. Each successful spray removes 20% of SCP-457's fire intensity.
6. SCP-457 is extinguished after five successful sprays.

A normal flashlight is not treated as an extinguisher. Extinguisher serial numbers are tracked by the server.

## Requirements

- SCP: Secret Laboratory Dedicated Server
- LabAPI supplied with the dedicated server
- .NET 8 SDK for compilation
- The following assemblies from `SCPSL_Data/Managed`:
  - `Assembly-CSharp.dll`
  - `CommandSystem.Core.dll`
  - `LabAPI.dll`
  - `Mirror.dll`
  - `UnityEngine.CoreModule.dll`

Game assemblies are intentionally not included in this repository.

## Building

The project targets .NET Framework 4.8. The `.csproj` reads game references from the `SL_REFERENCES` environment variable.

PowerShell example:

```powershell
$env:SL_REFERENCES = "C:\Path\To\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed"
dotnet build .\Scp457\Scp457.csproj -c Release
```

The compiled plugin will be created at:

```text
Scp457/bin/Release/net48/Scp457.dll
```

## Installation

1. Build the project in Release mode.
2. Copy `Scp457.dll` into the LabAPI global plugin directory:

```text
%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\
```

3. Restart the server.
4. Grant `scp457.spawn` and `scp457.extinguisher` to the appropriate Remote Admin groups.
5. Configure the plugin through the LabAPI-generated configuration file if you want to change the default values.

## Server-Specific Settings

The plugin adds an `SCP-457 - Abilities` section containing:

- `SCP-457 Fire Burst` 
- `Spray the extinguisher` 

The strings shown in-game are currently written in French in the source code and can be translated directly in `Scp457ServerSpecificSettings.cs` and `Plugin.cs`.

## Technical limitation

SCP:SL does not provide a native `RoleTypeId.Scp457`. The plugin therefore uses `RoleTypeId.Scp0492` internally. The role name is replaced while aiming at the player, but some unmodified client screens may still display `SCP-049-2`.

The extinguisher is also assembled from native server-side primitives and uses a flashlight as its inventory item. This keeps the plugin completely server-side.

## Project structure

```text
Scp457/
├── Commands/
│   ├── ExtinguisherCommand.cs
│   └── Scp457Command.cs
├── Config.cs
├── Plugin.cs
├── Scp457.csproj
├── Scp457Controller.cs
├── Scp457ExtinguisherModel.cs
└── Scp457ServerSpecificSettings.cs
```

## Author

Created by **Cadergam**.

