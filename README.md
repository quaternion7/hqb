# Hand Quickbelts

Hand Quickbelts is a code-only BepInEx mod for H3VR. It adds configurable quickbelt slots beside both hands without replacing the player's normal quickbelt layout.

![Hand Quickbelts presentation](assets/hand-quickbelts-presentation.png)

## Features

- Independent large, medium, and small slot counts for each hand
- Native H3VR quickbelt visuals, hover behavior, size rules, and state colors
- Symmetric hand placement with in-game grabbable adjustment handles
- Live diameter, spacing, column, and anchor updates
- Optional stored-item miniaturization inspired by SpineHero
- Collider-aware insertion, so long objects can activate a slot when their body touches it

The default layout has one large 100 mm slot on each hand. Additional slots use a two-column grid with 120 mm center-to-center spacing.

## Configuration

The generated BepInEx config has three sections:

- `Slots`: per-hand slot counts, grid columns, shared spacing, and slot diameter
- `Placement`: shared position and rotation vectors, adjustment handles, and reset
- `Stored Objects`: miniaturization toggle and stored-size percentage

Most settings update immediately. Changing a slot count rebuilds the hand inventory and drops objects stored in those slots.

With Sodalite installed, open Hand Quickbelts from the wrist menu's Mod Panel. Enable `Placement / Show Adjustment Handles`, grab either colored handle with the opposite hand, then move and rotate it. Releasing the handle saves a canonical pose; the other hand receives its mirrored equivalent.

The default palm-local pose is:

- Position: `(-1.4656025, 0.07558223, -0.8075327)` meters
- Rotation: `(-6.0018616, 36.728462, 174.04753)` degrees

When miniaturization is enabled, stored objects larger than `Stored Size (% of Slot Diameter)` are scaled uniformly to fit. The default is 80%. Their original scale is restored before H3VR changes their quickbelt parent, and smaller objects are never enlarged.

## Implementation

Each generated slot owns a hand-attached `QuickbeltRoot`, `HoverGeo`, and `PoseOverride`. Its base and hover spheres are cloned from the player's active spherical quickbelt slot, preserving native materials and preset-specific overrides. `HoverGeo` supplies the same world-space sphere used for the visible indicator and interaction test.

H3VR normally tests a held object's single pose point when selecting quickbelt slots. Hand Quickbelts extends only its own slots with physical-collider overlap while leaving normal body slots and empty-hand retrieval unchanged.

See [the quickbelt research note](docs/quickbelt-research.md) for the extracted vanilla and SpineHero hierarchies and the relevant decompiled H3VR code paths.

## Building

Requirements: the .NET SDK and NuGet access.

```powershell
dotnet build .\HQB.sln -c Release
```

The plugin is written to `plugin/bin/Release/net35/quaternion.hqb.dll`. Install it in an H3VR profile's `BepInEx/plugins` directory.

## Credits

The requested behavior was inspired by Ax's [SpineHero](https://thunderstore.io/c/h3vr/p/Ax/SpineHero/). SpineHero's Thunderstore assemblies and asset bundle were used as a behavioral reference; Hand Quickbelts creates its slots from H3VR's native quickbelt assets and does not require OtherLoader or a MeatKit bundle.
