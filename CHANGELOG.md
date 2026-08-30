# Changelog

## 0.3.8

- Fixed column and row offsets being compressed by the tracked palm hierarchy's `0.1` scale; configured millimeters are now converted to true world-space distances.
- Verified directly from H3VR's assets that Harness and normal quickbelt spheres share `QuickSlotGlow` and `QuickSlotGlowConstant`; the reported extra brightness was transparent-material stacking caused by the overlap.
- Prefer the player's active spherical quickbelt slot as the visual source, preserving any preset-specific or custom material overrides, with Harness retained as fallback.
- Promoted the latest tested shared anchor pose to the position and orientation defaults.

## 0.3.7

- Changed the default slot diameter to 100 mm and the stored-object fitting target to 80 mm.
- Changed both grid-spacing defaults to 120 mm, leaving a 20 mm edge gap between default slots.
- Promoted the tested live anchor pose to the shared position and orientation defaults.
- Fixed cumulative object growth by restoring the saved scale before H3VR reparents or removes an object from an HQB slot.
- Removed the user-facing diagnostics toggle and gated development logging behind the source-only `DeveloperDiagnosticsEnabled` flag.
- Replaced the temporary orange line with cloned native base/hover sphere renderers and their standard H3VR materials and state colors.

## 0.3.6

- Fixed cloned slots retaining `QuickbeltRoot`, `HoverGeo`, and `PoseOverride` references from the original quickbelt configuration instead of objects below the hand-attached clone.
- Gave every generated slot a self-contained active hierarchy so its visible ring, spherical hit test, storage parent, and item pose all follow the same palm anchor.
- Hid inherited clone renderers that were following the controllers without representing the registered HQB interaction volume.
- Added explicit ownership/activity diagnostics for every runtime slot reference.
- Increased stored-object bounds log precision and added the actual storage parent and active state; no scaling is inferred from rounded vector output.

## 0.3.5

- Changed the generated slot ring to orange so it can be distinguished from H3VR's cyan and purple controller geometry during testing.
- Added a delayed post-tracking measurement for ring diameter, head and palm distance, angular size, and the complete runtime hand/body scale chain.
- Kept the configured geometry unchanged pending runtime evidence for the reported visual-size mismatch.

## 0.3.4

- Stopped treating native mesh bounds as evidence of visible size; the transparent Fresnel shader does not render the complete mesh uniformly.
- Replaced both native sphere renderers with one unlit camera-facing world-space ring while retaining the native `HoverGeo` hit transform.
- Constructed the visible ring directly from world positions, with opposite endpoints exactly equal to `Slot Diameter (mm)`.
- Added the measured line-ring endpoint diameter and width to automatic and manual diagnostics.

## 0.3.3

- Changed HQB insertion detection from H3VR's held-object `PoseOverride` point to overlap between the item's enabled physical colliders and the visible slot sphere.
- Kept the collider-based override scoped to HQB; native body slots and empty-hand retrieval remain unchanged.
- Restored the native dynamic hover material on the same single base renderer, giving a clear highlight without overlaying a second circle.
- Added hover state transitions to the log for direct interaction verification.

## 0.3.2

- Fixed native prefab discovery by removing the invalid requirement that spherical slots populate `RectBounds`.
- Discovered the always-visible base sphere as the matching sibling mesh used by vanilla H3VR and SpineHero.
- Fixed the base-circle resize path to operate on that actual sibling renderer.
- Replaced the per-frame missing-prefab error flood with one warning and a throttled one-second retry while H3VR initializes.

## 0.3.1

- Standardized generated slots on H3VR's original `QuickBeltSlot_Harness` prefab, whose native indicator pair is 20 cm.
- Made `Slot Diameter (mm)` a world-space measurement by compensating for the complete hand-parent scale.
- Forced the base sphere, hover sphere, and H3VR interaction sphere to share one world center, rotation, and scale.
- Replaced the two overlaid native materials with one persistent indicator renderer; hover and stored-object colors now affect that same circle.
- Increased the stored-object target from 50 mm to 160 mm so objects remain clearly visible within a 200 mm slot.
- Added measured object bounds/scaling and renderer world bounds to diagnostics.

## 0.3.0

- Replaced the custom nested visual/interaction wrapper with SpineHero's native quickbelt structure: direct sibling `HoverGeo` and `RectBounds` branches at one shared scale.
- Unified the base circle, hover highlight, and interaction area under one uncapped `Slot Diameter (mm)` setting, now 200 mm by default.
- Ported SpineHero's collider-bounds object fitting as an optional stored-object miniaturizer, enabled by default with a 50 mm target.
- Restored original object scale when an item is grabbed, removed, the option is disabled, or a hand slot is destroyed.
- Added attachment-hierarchy handling and scaler state to the runtime diagnostics report.

## 0.2.7

- Matched the always-visible `RectBounds` circle and highlighted `HoverGeo` circle to one configurable diameter.
- Added an independently configurable, larger invisible interaction diameter.
- Preserved the cloned vanilla `QuickbeltRoot` used when H3VR parents stored objects.
- Added an in-game live slot diagnostics dump.
- Added repeatable Unity asset-bundle inspection tooling and documented the vanilla, H3VR, and SpineHero structures.

## 0.2.6

- Fixed quickbelt hover and insertion by preserving the vanilla interaction-volume transform instead of shrinking it to marker size.
- Decoupled the one visible native marker from the invisible interaction volume.
- Mirrored the shared anchor position and quaternion across the local X plane for the left hand.
- Unmirrored left-handle adjustments before saving the canonical shared pose.

## 0.2.5

- Removed configured limits from columns, spacing, and slot diameter.
- Restored the known-working generated hover/hit sphere and synchronously removed the cloned marker to prevent duplicates.
- Replaced per-hand anchor poses with one shared symmetric pose.
- Adopted the latest tested right-hand pose as the shared default.

## 0.2.4

- Restored exactly one native H3VR hover marker per slot and removed the generated replacement sphere.
- Consolidated each hand's anchor position and rotation into two Vector3 config entries.
- Added `x, y, z` vector editing support for Sodalite's raw Mod Panel fields.

## 0.2.3

- Removed the position clamp and 10 mm release snapping from adjustment handles; released anchors now persist at 1 mm precision.
- Added an in-game `Reset All Settings` action.
- Made slot bounds fully invisible and removed the redundant preview option.

## 0.2.2

- Changed all millimeter controls to advance in 10 mm increments in the in-game Mod Panel.
- Adjustment-handle releases now round persisted positions to the nearest 10 mm.
- Normalized layout defaults to the 10 mm grid.

## 0.2.1

- Replaced Sodalite-incompatible float ranges with safe integer millimeter/degree controls.
- Moved preview and adjustment-handle toggles into a top-level Live Adjustment section.
- Fixed Mod Panel page construction aborting before all settings were displayed.

## 0.2.0

- Anchored hand slots to H3VR's palm transform instead of the controller/pointer origin.
- Added independent palm-local position and rotation settings for both hands.
- Added live-adjustable column count, row/column spacing, and constant slot diameter.
- Replaced inherited vanilla hover meshes with unit spheres for predictable sizing.
- Layout-only config changes now update in place without dropping stored items.
- Added an optional always-visible slot preview for in-game layout tuning.
- Added grabbable adjustment handles that persist released anchor poses to config.

## 0.1.0

- Initial configurable BepInEx project.
- Added independent large, medium, and small slot counts for both hands.
- Added code-only slot creation using vanilla H3VR quickbelt prefabs.
