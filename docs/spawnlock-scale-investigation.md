# Spawnlock clone scale investigation

## 1.0.0 implementation: conservative behavior-based correction

The requested release uses a narrower heuristic than the general compatibility design below. It has no dependency, plugin-ID check, or explicit ordering tie to MasteryCamos. A `Priority.Last` postfix runs on the six affected spawnlock overrides, after ordinary-priority postfixes.

The prefix accepts only sources in an HQB-marked slot and actually parented below its quickbelt root, with positive, approximately uniform parent scale near 0.1. It records the unminiaturized source world scale using the miniaturizer's saved original local scale (or current local scale if untracked), plus the prefab scale. The postfix corrects only a clone whose world scale on every axis matches the original world scale divided by the measured parent scale, within 1% relative tolerance. Correction multiplies the clone's local scale by that measured uniform parent scale. The stored source is never changed.

Normal prefab-scale clones are explicitly excluded. This avoids mistaking vanilla duplication of an intentionally small source for a compatibility error. Null results, results referring to the source, other slots, different parent scales, nonpositive scales, and mismatched axes are also excluded. NaN/infinite values fail the comparison.

This intentionally does **not** fix all possible scale-copy conflicts: a 3x or 0.5x clone caused by miniaturization will not match the approximately 10x condition. A conflict whose result equals normal prefab scale is also left alone. An unrelated mod intentionally creating the same 10x signature could still match the heuristic. `Priority.Last` places this after normal-priority patches; another mod with explicit ordering or equally late patches can still affect the final result. These are the limits of identifying behavior without identifying a specific mod.

`tools/test-spawnlock-compatibility.ps1` builds an isolated Unity 5.6 project using the actual correction and miniaturizer source, real Unity transforms, and the production HarmonyX/MonoMod dependencies. Its small FistVR spawning model and unrelated-ID scale-copying postfix test scope, expected/no-op cases, repeated pulls, and both patch installation orders. This is a regression harness, not an H3VR gameplay reproduction or a test of the complete MasteryCamos binary.

Validation on 2026-09-12: all 59 Unity 5.6.7f1 regression cases passed, with three consecutive pulls per case. Release compilation succeeded with zero warnings/errors. The packaging script validated the manifest, icon, DLL version, and six expected archive entries. Package SHA-256: `3B26F7E623E9AAE1CA5A2E0406B368768276E43AA9F3010088F9852E4063FB00`. The package is prepared for the requested review before publication; no upload was performed.

## 2026-09-12 update: MasteryCamos identified

**Recommendation:** disclose the reported incompatibility now, then validate a targeted HQB compatibility fix. This is a concrete code-level conflict, not a reason to declare the two mods permanently incompatible. The tester's complete in-game reproduction remains unverified locally. No production code, package, or published mod page was changed during this investigation.

The findings below supersede the earlier unidentified-mod hypothesis and the instruction to wait for a mod list. The historical investigation is retained afterward.

### Evidence from the released package

Downloaded [NGA-MasteryCamos 2.2.3](https://old.thunderstore.io/package/download/NGA/MasteryCamos/2.2.3/) and inspected `NGAMasteryCamos.dll` with ILSpy. DLL SHA-256: `5E23497931B73B4CE68E5775AAAAB912F4D43DA7FB7B7BF3D0D32B47EA6D9E80`.

Its Harmony owner is `NGA.MasteryCamos`. Six `DuplicateFromSpawnLock` postfixes all perform the following operation (normalized C# from the decompilation):

```csharp
__result.transform.localScale = __instance.transform.localScale;
```

The patched types are `FVRFireArmMagazine`, `FVRFireArmRound`, `FVRFireArmClip`, `FVRGrenade`, `PinnedGrenade`, and `Speedloader`. In the decompiled file, the corresponding nested patch classes occupy lines 336–425. The scale assignment precedes the optional camo propagation. It does not check for CamoShop, an applied camo, or an item that was intentionally resized. `Awake` installs the patches with `PatchAll()` after initialization; `SetUpConfigFields` is empty, so this release exposes no configuration switch for this behavior.

This explains why installing MasteryCamos without CamoShop can trigger the report. MasteryCamos' package manifest lists Sodalite 1.4.1, JsonFileIO 0.0.1, and ProfileSaveFolder 1.0.0. CamoShop is not a manifest dependency. Those dependency binaries were not audited: the faulty scale assignment is already directly present in MasteryCamos itself, so there is no evidence here to blame the shared Sodalite dependency.

The [published changelog](https://old.thunderstore.io/c/h3vr/p/NGA/MasteryCamos/) also describes spawnlock size preservation in 2.2.0. Its 2.2.2 rollback concerns rounds coming out of guns; the six spawnlock scale assignments remain in 2.2.3. The chamber-ejection and magazine/clip round-removal postfixes inspected in 2.2.3 are empty.

### Why HQB exposes it

HQB parents stored objects below the palm. World-preserving parenting compensates for the parent's scale in the item's `localScale`. HQB then optionally reduces that local scale to fit the slot. MasteryCamos treats this storage transform as the item's intended size and copies it to a clone in a different parent space.

For an originally unit-scale item, uniform hand ancestry scale `p`, and HQB fit factor `f`, the source local scale is `f / p`. With an unparented clone, MasteryCamos therefore produces clone world scale `f / p`. Using the previously measured `p = 0.1`:

| HQB fit factor | Stored world scale | Clone world scale | Intended clone world scale |
| --- | --- | --- | --- |
| 1.0 | 1.0 | 10.0 | 1.0 |
| 0.3 | 0.3 | 3.0 | 1.0 |
| 0.05 | 0.05 | 0.5 | 1.0 |

These are transform calculations, not new in-game measurements. They explain why different items/settings can enlarge, shrink, or accidentally mask the conflict. Turning off miniaturization does not remove the parent-space mismatch and can make enlargement worse.

HQB's existing `StoredObjectScalePatch` restores the source on ordinary removal/reparenting. Spawnlocking leaves that source stored while creating a different object, so restoring on removal does not fix this path. MasteryCamos also patches specific overrides, so a correction only on the base `FVRPhysicalObject.DuplicateFromSpawnLock` would run too early and could be overwritten by its derived-method postfixes.

### Feasible targeted fix

1. Activate compatibility only when the affected MasteryCamos patches are present. Use the Harmony owner and inspected method identity; its assembly plugin version is `0.0.1` even though the package is 2.2.3, so the plugin version alone cannot reliably identify this release.
2. Capture the source's intended, unminiaturized world scale before duplication, only when its quickbelt has `HandQuickbeltSlotMarker`. If the miniaturizer currently tracks that exact source, use its saved `_originalScale` converted through the source's current parent scale. Otherwise, including miniaturization disabled or insertion before the first tracking update, use the source's current world scale. Do not divide by a fit factor blindly: the source may already have been restored during interaction.
3. Correct the returned clone after MasteryCamos on each affected override, explicitly ordered with `HarmonyAfter("NGA.MasteryCamos")`. Convert the captured world scale into the clone's actual parent space. Guard null results and leave the source untouched.
4. Preserve the rest of MasteryCamos' postfix, including camo copying. Leave body slots and unrelated duplication paths alone. Test nested override calls and actual Harmony ordering; a base-method-only patch is insufficient.

For uniform scale, the intended world scale is `originalStoredLocalScale * sourceParent.lossyScale` component by component; the clone local scale is that value divided by its parent's world scale (or unchanged if unparented). Rotated, nonuniform parents can introduce shear, so that simple conversion needs a defined limitation or additional handling rather than being advertised as universal.

Do not force `Vector3.one`, reset to prefab scale, globally remove MasteryCamos' postfixes, or simply replace its `localScale` read with `lossyScale`. The first two discard intentional resizing and non-unit prefab scales; unpatching loses camo propagation; copying current world scale still copies HQB's temporary miniaturization. Capturing the intended size is essential. Resizing while already stored also needs validation because HQB caches the scale at entry.

MasteryCamos additionally has separate scale serialization logic: `GetFlagDic` walks to the topmost transform and saves its local scale as `nga_mc_changedScale`, while `ConfigureFromFlagDic` applies that value to the item. This is outside the confirmed spawnlock mechanism. Include vault saving in regression testing, but do not claim a spawnlock correction fixes that separate path.

### Validation needed before shipping a fix

- Compare HQB plus its normal dependencies against HQB plus MasteryCamos and its required dependencies, initially without CamoShop. Record exact package/game versions and HQB settings.
- Test each affected spawnlock type in hand and body slots, with miniaturization on/off, an item that fits without shrinking, and an item that must shrink. Record source local/world scales, source parent scale, and clone local/world scales immediately around duplication.
- Verify deliberately resized items with CamoShop, non-unit prefab scales, both hands, repeated pulls, and a pull immediately after insertion. Confirm stored originals stay visually stable.
- Verify ammo counts, round palming/proxies, camo preservation, ordinary removal, and vault save/load. Confirm baseline behavior without MasteryCamos and that actual postfix order places the correction last relative to MasteryCamos.

No new Unity runtime reproduction or compatibility patch test was performed in this investigation. The evidence establishes the conflicting code operation and a viable design; it does not certify a released fix.

### Suggested temporary mod-page notice

> **Reported compatibility issue — MasteryCamos:** With NGA MasteryCamos installed, items pulled from spawnlocked Hand Quickbelts may appear at the wrong size, even without CamoShop or use of its resizer. A scale-copying conflict has been identified and a compatibility fix is under investigation. Until resolved, avoid using spawnlock in hand slots with this combination. Disabling Hand Quickbelts' miniaturization is not a reliable workaround.

This notice is a draft, not a published change. Avoid promising that installing CamoShop resolves the issue: there is no such condition in the inspected spawnlock patches.

## Historical investigation (before MasteryCamos was identified)

## Findings and confidence

Two reports describe enlarged ammo, magazines, and clips pulled from spawnlocked hand slots. The stored source looks correct; hardened weapons and disposable melee items reportedly work. The reporting players' mod lists are not yet available, and their in-game issue has not been reproduced locally.

Inspection of the installed H3VR Update 120 `Assembly-CSharp.dll` establishes that `FVRPhysicalObject.DuplicateFromSpawnLock` instantiates `ObjectWrapper.GetGameObject()` at the source pose, clears the clone's quickbelt state, and starts interaction. It does **not** copy the source's scale. The round, magazine, clip, and speedloader overrides call that base implementation, then populate ammo state. They do not assign the root scale either.

Therefore, the source scale does not by itself explain a giant vanilla clone. An additional scale-copying patch or custom spawning implementation is the leading hypothesis, not a confirmed identification of a particular mod. A different game build or modified prefab remains possible.

The hand ancestry was previously measured at scale `0.1` (see [quickbelt research](quickbelt-research.md)). A normal object parented there with world position preserved gets local scale `10`. With HQB's miniaturization factor `f`, its stored local scale becomes `10 * f`. Copying that local scale onto an unparented clone produces world scale `10 * f`, instead of the original world scale `1`:

- An item that fits without shrinking (`f = 1`) becomes ten times larger.
- An item reduced to 30% (`f = 0.3`) becomes three times larger.
- An item reduced to 5% (`f = 0.05`) becomes half size.

This mechanism is consistent with both enlargement and shrinkage reports in scaled storage systems. It also means disabling miniaturization alone does not necessarily resolve a local-scale-copying conflict. Unity documents [localScale as relative to the parent](https://docs.unity3d.com/ScriptReference/Transform-localScale.html).

## Status and next steps

The speculative scale guard and its test sources were removed. A synthetic Unity/Harmony test demonstrated the scale-copy mechanism, but did not establish the cause in either reporting player's profile. Enforcing clone scale could interfere with intentional behavior in other mods. HQB's production code remains unchanged.

Wait for an affected player's mod list or profile export before making a compatibility change. Then:

1. Reproduce with that profile and the reported item; record game version and HQB settings.
2. Compare with a minimal HQB profile and isolate the responsible mod or combination.
3. Inspect the actual spawning path and source/clone scales before choosing a targeted fix.
4. Verify ammo contents, palming, ordinary removal, and other quickbelt/spawnlock behavior with the identified combination.

No experimental build was published or installed into a game profile.
