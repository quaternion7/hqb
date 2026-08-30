# Changelog

## 0.4.2 - Release candidate

- Added compatibility with H3VR 1.0 stable (Update 120).
- Made hand slots follow H3VR's scene-level quickbelt availability.
- Released stored contents safely when entering a scene without quickbelts.

## 0.4.1

- Added a live integer setting for stored-object size as a percentage of slot diameter.
- Kept miniaturization shrink-only so objects are never enlarged.

## 0.4.0

- Consolidated the configuration into Slots, Placement, and Stored Objects.
- Added independent per-hand slot counts, a shared mirrored anchor pose, and grabbable adjustment handles.
- Matched native quickbelt visuals, hover states, interaction size, and object-size rules.
- Added world-space grid layout, collider-aware insertion, and optional stored-object miniaturization.
- Restored object scale safely before removal from a hand slot.

## 0.1.0 - 0.3.8 - Development

- Established the configurable BepInEx plugin and iterated on native slot discovery, geometry, interaction, layout, and scaling.
- Fixed prefab ownership, scene transitions, overlapping transparent visuals, scaled-hand spacing, and cumulative object growth.
