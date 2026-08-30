# H3VR quickbelt research

This note records the concrete runtime and asset relationships used by Hand Quickbelts. It is intended to prevent future changes from conflating slot visuals, hit testing, and stored-object transforms.

## Vanilla prefab hierarchy

The vanilla quickbelt prefabs are stored in `h3vr_Data/resources.assets`. The tactical configuration is named `QuickBeltPrefab_Config0_Tactical`. Its spherical slots use this repeated structure:

```text
FVRQuickBeltSlot
└── QB_TransformTarget          (QuickbeltRoot)
    ├── GlowSphere              (HoverGeo; hidden until hovered)
    ├── GlowSphere (variant)    (unreferenced always-visible base circle)
    └── PoseOverride
```

The two `GlowSphere` transforms have matching scales in the source prefab. They also reference the same sphere mesh, but use different materials for the base and hover states. Examples from the tactical configuration:

- Large slots: both are `(0.32, 0.32, 0.32)`.
- Medium slots: both are `(0.10, 0.10, 0.10)`.

The `RectBounds` field is used by `IsPointInsideRectBound()` for rectilinear slots and is null on the live spherical prefabs. The second sphere requires no component field because it is an always-active child renderer. HQB 0.3.1 incorrectly assumed it was `RectBounds`; the resulting live prefab lookup rejected every spherical candidate and exposed that mistaken mapping. For spherical slots, the base renderer must instead be found as the sibling renderer with the same sphere mesh as `HoverGeo`.

The hierarchy can be reproduced with:

```powershell
python .\.tools\inspect_unity_bundle.py `
  '<H3VR install>\h3vr_Data\resources.assets' `
  'QuickBeltPrefab_Config0_Tactical' --max-depth 4
```

## Game-code path

The installed `Assembly-CSharp.dll` was inspected with ILSpy's command-line decompiler.

`FVRViveHand.TestQuickBeltDistances()` chooses a test point as follows:

- Empty hand: the hand `PoseOverride.position`.
- Held item: the held interactive object's `PoseOverride.position`, falling back to its root position.

It checks `QBSlots_Internal`, then `QBSlots_Added`, then the normal quickbelt list. A spherical slot calls `FVRQuickBeltSlot.IsPointInsideSphereGeo()`, which transforms the point through `HoverGeo.transform.InverseTransformPoint()` and accepts it when the normalized magnitude is below `0.5`.

Consequences:

- `HoverGeo.transform` is functional hit-test geometry, not merely a visual.
- Shrinking `HoverGeo` to make a small marker also shrinks the insertion volume.
- The simplest native behavior keeps the hover renderer and hit-test transform together, as both vanilla H3VR and SpineHero do.
- A long or unusually posed object can visibly touch a slot without highlighting because its one `PoseOverride` point remains outside. HQB 0.3.3 patches only this case for HQB slots: enabled, non-trigger collider bounds are tested against the visible sphere, with the native pose point retained as a fallback when an object has no usable physical collider.

When an item is released into a valid slot, `FVRPhysicalObject.EndInteractionIntoInventorySlot()` calls `SetQuickBeltSlot()` and then parents the object to `QuickbeltRoot`. `SetParentage()` calls Unity's one-argument `Transform.SetParent()`, whose default preserves world transform. Scaling the slot parent therefore does not automatically miniaturize the newly parented item.

## SpineHero comparison

SpineHero 1.0.3 was downloaded from its Thunderstore package and both DLLs were decompiled. Its asset bundle contains this slot structure:

```text
QuickBeltSlot_Small
└── QB_TransformTarget          (QuickbeltRoot)
    ├── GlowSphere              (HoverGeo), scale 0.06
    ├── GlowSphere (5)          (unreferenced base renderer), scale 0.06
    └── PoseOverride
```

Its custom `ScalableQBSlot` explicitly miniaturizes stored objects. It computes collider bounds, selects a uniform scale that fits a serialized target size of approximately `0.05 m` per axis, applies the scale while stored, and restores the saved original scale when the object leaves. It also checks attachment-hierarchy changes and hardened-object state. The effect is not produced by parent scaling.

The two SpineHero `GlowSphere` branches use the same mesh (`path_id 10207`) and the same `0.06` transform scale, but different material references. Therefore an apparent bright center inside the hover effect can come from its highlight shader/material rather than a second smaller transform.

## HQB 0.3 port

HQB 0.3 removes the generated nested `HQB_HoverBounds` object. Each cloned vanilla slot retains the SpineHero/vanilla topology. HQB 0.3.2 discovers the unreferenced base renderer by matching its sibling mesh to `HoverGeo`, then gives both transforms one configured world-space scale. Because `HoverGeo` is also the native hit-test geometry, `Slot Diameter (mm)` controls the rendered circle and interaction diameter together.

Stored objects are miniaturized by a separate component on the cloned slot. It follows SpineHero's bounds-fitting approach without replacing or subclassing H3VR's `FVRQuickBeltSlot`: this lets the original game component continue running its own private `Update` normally. The scaler restores the object's original scale on removal and accounts for attachments parented while the root object is already small.

HQB 0.3.1 standardizes all generated slots on the vanilla `QuickBeltSlot_Harness` prefab, whose native sphere pair is exactly `0.20` local units. Runtime geometry scaling compensates for `QuickbeltRoot.lossyScale`, making the configured diameter a true world-space measurement even below a scaled hand hierarchy. The two visual transforms are explicitly made coincident. Because `HoverGeo` is the same transform used by `IsPointInsideSphereGeo`, the visible indicator and hit-test sphere now necessarily share their center and dimensions. To avoid the two native materials reading as nested circles, HQB disables only the hover branch's renderer and applies hover/spawnlock/hardened rim colors to the always-present base renderer in `LateUpdate`; the original `HoverGeo` transform remains intact for H3VR's hit testing.

That still did not make mesh bounds a valid measurement of the visible boundary: both `QuickSlotGlow` and `QuickSlotGlowConstant` are transparent Fresnel/rim materials with zero base alpha and a rim power of 4. HQB 0.3.4 disables both native renderers and draws one unlit, camera-facing `LineRenderer` ring from explicit world-space points. The distance between opposite vertices is the configured diameter, and the collider-overlap patch derives its sphere radius from the same `HoverGeo.lossyScale`.

HQB 0.3.5 colors that generated ring orange to separate it visually from the controller model and records a second geometry measurement after tracking has settled. This delayed measurement includes the rendered endpoint diameter, head and palm distance, angular diameter, and the body/palm/hand-root scale chain. The startup geometry check alone is insufficient because it runs while the slots can still be at their placeholder position.

The delayed measurement exposed a separate cloning error: the 200 mm interaction transform remained at the vanilla configuration placeholder while the palm moved away. Instantiating only `template.gameObject` was not sufficient for this live template because its `QuickbeltRoot`, `HoverGeo`, and `PoseOverride` fields could still reference objects outside the cloned hierarchy. This also gave stored objects an external storage parent: H3VR's `EndInteractionIntoInventorySlot()` explicitly parents the item to `slot.QuickbeltRoot`, so an inactive external root makes a successfully stored item invisible.

HQB 0.3.6 replaces all three references with an explicit self-contained hierarchy below each cloned `FVRQuickBeltSlot`. The hidden native `HoverGeo` clone remains present because the game's private `FVRQuickBeltSlot.Update()` unconditionally accesses its cached renderer material. The visible line ring, hit-test transform, `PoseOverride`, and active storage root now share the hand-attached slot ancestry. Runtime logs verify this with `IsChildOf(slot.transform)` and `activeInHierarchy` checks.

With that ownership fixed, HQB 0.3.7 removes the temporary line and clones the native constant and hover spheres individually into the self-contained root. Both receive the configured world diameter. `FVRQuickBeltSlot.Update()` once again owns hover activation and its standard white, blue spawnlock, and green hardened rim states.

Asset inspection confirms that the normal tactical spheres and `QuickBeltSlot_Harness` reference the same materials: `QuickSlotGlow` (path ID 104) and `QuickSlotGlowConstant` (path ID 105). The brighter 0.3.7 appearance was not a material mismatch. Grid offsets had been assigned as local meters below the tracked palm's `0.1` scale, turning 120 mm into 12 mm and stacking several transparent constant spheres. HQB 0.3.8 converts desired offsets through `Transform.InverseTransformVector`, making configured spacing a true world-space measurement. It also prefers an active spherical body slot as the clone source so custom quickbelt presets retain their own visual overrides.

Runtime scale logs also established the stored-object growth mechanism: beneath the hand's `0.1` scale hierarchy, Unity's world-preserving reparent produced an expected local scale of `10`. Restoring that saved local scale only after H3VR had moved the object out of `QuickbeltRoot` multiplied its world size by ten; each later cycle captured `100`, `1000`, and so on. HQB 0.3.7 patches the removal paths and restores the saved scale before `SetParentage`, `BeginInteraction`, or quickbelt clearing changes the hierarchy.

## HQB 0.4 release-candidate structure

The RC keeps the proven self-contained slot topology while removing the temporary runtime-diagnostic framework. The cloned template's unused children are disabled and destroyed after the local `QuickbeltRoot`, native sphere pair, and `PoseOverride` are bound.

User configuration is reduced to three sections. Column and row distance share one world-space spacing value. The miniaturization target is an integer percentage of the configured slot diameter, defaults to 80%, and only reduces objects that exceed that target. Scale restoration remains patched immediately before `SetParentage` or `SetQuickBeltSlot(null)` can move an item out of its scaled storage hierarchy.
