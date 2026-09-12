# Hand Quickbelts

Adds configurable quickbelt slots around both hands without replacing the player's normal body quickbelt.

**Compatibility:** H3VR 1.0 stable (Update 120).

[Thunderstore](https://thunderstore.io/c/h3vr/p/quaternion/Hand_Quickbelts/) | [Source code](https://github.com/quaternion7/hqb)

## Features

- Independent large, medium, and small slot counts for each hand
- Symmetric placement with a mirrored pose for the opposite hand
- Live adjustment handles for moving and rotating the slot grid
- Configurable slot diameter, spacing, columns, and stored-object size
- Optional miniaturization to make items smaller while stored in the slot
- Collider-aware insertion for long or unusually shaped objects

## Mod compatibility

Includes a targeted correction for MasteryCamos making spawnlocked items approximately 10x too large when pulled from hand slots. It applies only when the size increase matches the hand-parent scale compensation; other scale mismatches are not covered.

## Recent changes

- **1.0.0:** Fixed the approximately 10x spawnlocked-item scaling issue due to mod compatibility (MasteryCamos), limited to hand slots.
- **0.4.2:** Added H3VR 1.0 stable support and respected scenes that disable quickbelts.
- **0.4.1:** Added adjustable stored-object size.

[Full changelog](https://github.com/quaternion7/hqb/blob/main/CHANGELOG.md)

## Quick setup guide

1. Install with r2modman or Thunderstore Mod Manager and launch H3VR.
2. Enter a scene that enables quickbelts.

## In-Game Slots Customization

1. Open the wrist menu, then select `Mod Panel` > `Hand Quickbelts`.
2. Configure the slot counts, grid, and stored-object behavior.
3. Enable `Placement` > `Show Adjustment Handles`.
4. Grab either colored handle with the opposite hand, move and rotate it, then release it to save. The other hand updates with the mirrored pose.
5. Disable the adjustment handles when finished.

Most settings update immediately. Changing slot counts or resetting the configuration rebuilds the hand slots and releases their contents. Scenes that disable H3VR's native quickbelt also disable Hand Quickbelts.

## Configuration

- `Slots`: per-hand size counts, grid columns, spacing, and visual/interaction diameter
- `Placement`: shared anchor pose (mirrored for both hands), adjustment handles, and reset
- `Stored Objects`: miniaturization and maximum stored size as a percentage of slot diameter

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

Run `.\tools\package.ps1` to create a validated Thunderstore ZIP. See the [publishing guide](docs/publishing.md) for first-upload and update steps.

## Support

Tips and donations are welcome on [Ko-fi](https://ko-fi.com/quaternion7). You can find my other projects and links at [quaternion7.github.io](https://quaternion7.github.io/).

## License

Hand Quickbelts is available under the permissive [MIT-0 license](LICENSE).

## Credits

Inspired by Ax's [SpineHero](https://thunderstore.io/c/h3vr/p/Ax/SpineHero/).
