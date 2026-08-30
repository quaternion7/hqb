# Hand Quickbelts

Adds configurable quickbelt slots below both hands without replacing the player's normal body quickbelt.

**Compatibility:** H3VR 1.0 stable (Update 120).

## Features

- Independent large, medium, and small slot counts for each hand
- Native H3VR visuals, hover states, size rules, and scene availability
- Symmetric placement with a mirrored pose for the opposite hand
- Live adjustment handles for moving and rotating the slot grid
- Configurable slot diameter, spacing, columns, and stored-object size
- Optional miniaturization that restores an item's original scale on removal
- Collider-aware insertion for long or unusually shaped objects

## Quick setup guide

1. Install with r2modman or Thunderstore Mod Manager and launch H3VR.
2. Enter a scene that enables quickbelts.
3. Open the wrist menu, then select `Mod Panel` > `Hand Quickbelts`.
4. Configure the slot counts, grid, and stored-object behavior.
5. Enable `Placement` > `Show Adjustment Handles`.
6. Grab either colored handle with the opposite hand, move and rotate it, then release it to save. The other hand updates with the mirrored pose.
7. Disable the adjustment handles when finished.

Most settings update immediately. Changing slot counts or resetting the configuration rebuilds the hand slots and releases their contents. Scenes that disable H3VR's native quickbelt also disable Hand Quickbelts.

## Configuration

- `Slots`: per-hand size counts, grid columns, spacing, and visual/interaction diameter
- `Placement`: shared anchor pose, adjustment handles, and reset
- `Stored Objects`: miniaturization and maximum stored size as a percentage of slot diameter

Miniaturization only shrinks objects that exceed the configured stored size; it never enlarges them.

## Installation

Installing through a mod manager is recommended and installs the dependencies automatically.

For a manual installation, install BepInExPack H3VR and Sodalite, then place `quaternion.hqb.dll` in `BepInEx/plugins`.

## Implementation

The mod clones H3VR's active spherical quickbelt geometry into palm-attached slots, keeping native materials and behavior. Small, targeted patches add collider-aware insertion and safe object-scale restoration.

## Building

Requires the .NET SDK and NuGet access.

```powershell
dotnet build .\HQB.sln -c Release
```

The release DLL is written to `plugin/bin/Release/net35/quaternion.hqb.dll`.

## Credits

Inspired by Ax's [SpineHero](https://thunderstore.io/c/h3vr/p/Ax/SpineHero/). SpineHero was used as a behavioral reference; Hand Quickbelts uses H3VR's native assets and does not require OtherLoader or MeatKit.
