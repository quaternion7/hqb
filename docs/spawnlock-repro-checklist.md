# HQB / MasteryCamos reproduction and local fix testing

## 1.0.1 gameplay validation completed

The user reproduced the issue on 1.0.0 on 2026-09-14. The user tested local 1.0.1 in **HQB MasteryCamo test**, confirmed the correction worked, and authorized publication. The prior 1.0.0 package files, manager metadata, and latest log are preserved inside the profile under `_diagnostics/hqb-1.0.0-baseline-before-1.0.1-*`.

Launch **Start modded** in that same profile and confirm `Hand Quickbelts 1.0.1` in `BepInEx/LogOutput.log`. Keep the settings and exact item types that reproduced the bug. Insert fresh magazines/grenades/rounds, spawnlock them, and pull several copies beside a normal reference copy. Stored sources should remain miniature; pulled copies should have their original size. Already oversized objects are not repaired retroactively. Repeat with miniaturization disabled and, optionally, with MasteryCamos disabled as a control. Restart r2modman if its installed-version display is stale.

The 106-case Unity/Harmony harness passes, including actual collider fitting and all six patched object types. The user also confirmed the fix in gameplay. The following sections are historical setup and 1.0.0 reproduction notes.

## Current verified profile: HQB MasteryCamo test

Use **HQB MasteryCamo test**, the user's fresh r2modman installation, for the next reproduction run. The earlier manually assembled profile was removed. The fresh profile had JsonFileIO 0.0.3 correctly selected but still lacked MasteryCamos's undeclared Newtonsoft.Json library. Added the same five support DLLs described below to this fresh profile.

On 2026-09-14, an actual H3VR startup check confirmed MasteryCamos finished initialization and all six MasteryCamos and HQB spawnlock postfixes were installed. The game then exited automatically; the temporary diagnostic DLL was removed. The preserved log is `_diagnostics/startup-verified-20260914.log` inside this profile. HQB's binary is unchanged published 1.0.0. Launch normally using **Start modded**. This verifies startup and patch installation; gameplay reproduction is the next step.

The following earlier setup notes document the previous attempts; apply the gameplay checklist to **HQB MasteryCamo test** now.

In r2modman, select **H3VR**, then the profile **HQB MasteryCamo test**, and click **Start modded**. If the profile picker was already open, return to it or restart r2modman so it rereads the directory.

This is the published HQB 1.0.0, with MasteryCamos 2.2.3 and four dependencies. JsonFileIO must be **0.0.3**: 0.0.1 has the wrong internal plugin ID and prevents MasteryCamos loading. The first local run exposed that mismatch; the dependency has now been updated. CamoShop is intentionally absent, matching the original report.

Before interpreting a reproduction attempt, inspect `BepInEx/LogOutput.log`: MasteryCamos must load and reach its initialization messages without a missing-dependency or initialization error. A package shown as enabled in r2modman does not prove that BepInEx loaded its plugin. MasteryCamos's plugin log version is 0.0.1 even though its package version is 2.2.3.

The second local run exposed another missing dependency: `Newtonsoft.Json, Version=13.0.0.0`. The profile now includes the official Newtonsoft.Json 13.0.3 NuGet library's .NET 3.5 build under `BepInEx/plugins/HQB-Repro-Runtime-Libraries` (this library's assembly version is 13.0.0.0). It is not a BepInEx gameplay plugin and will not add a mod-list entry. Its license and origin note are alongside the DLL. Keep this folder for reproduction. A fresh run is needed to verify initialization after this addition.

The third run failed on System.Data during JSON serialization. Four matching Unity Mono support libraries (System.Data, System.Xml.Linq, System.Transactions, Mono.Data.Tds) were subsequently added to the same folder. The real MasteryCamos quest-state JSON round-trip now passes in a local Unity Mono probe, and all 18 declared patch targets exist in the installed game. Full in-game startup remains unverified. This profile was assembled manually; compare against a separate fresh profile installed through r2modman if diagnosing installation differences.

1. Enter a sandbox scene that permits spawnlocking, such as the Indoor Range. In the wrist menu, open **Mod Panel > Hand Quickbelts**. Confirm miniaturization is enabled, Slot Diameter is **100 mm**, and Stored Size is **80%**.
2. Spawn a short loose cartridge, a full-sized magazine, and a grenade that supports spawnlocking. Record their exact item names. Keep an ordinary freshly spawned copy nearby for size comparison.
3. Put each test source in a hand slot and enable spawnlock. Wait briefly for the source to shrink, then pull three copies. Note whether the source shrinks while stored and whether the resulting copies are normal, enlarged, or smaller. Try both hands if results differ.
4. Repeat with **Miniaturize Stored Objects disabled**. Remove and reinsert the source before pulling fresh copies; already oversized clones will not repair themselves. Prediction: a larger item missed in step 3 now reaches the 10x signature and is corrected by HQB 1.0.0.
5. Optional size comparison: re-enable miniaturization and raise Stored Size from **80% to 300%** (a 30 cm target at the default diameter). For an item that now fits without shrinking, the same 1.0.0 guard should begin correcting it. Orientation can affect world-aligned collider bounds.
6. Compare the same item in a normal body slot. Record the outcome rather than assuming it is immune to other scaling behavior.
7. For the control run, close H3VR and disable **MasteryCamos** in this test profile's Installed list. Keep HQB enabled, restore its 100 mm / 80% defaults, launch again, and repeat. Re-enable MasteryCamos afterward if you want to leave the profile ready to reproduce the conflict.

Save the item names, HQB settings, and results for each pass, plus `BepInEx/LogOutput.log` from this profile after each run (the next launch may replace the log). A screenshot beside a normal copy will help estimate the enlargement factor.

The confirmed test-harness mechanism is size-dependent: a 4 cm collider is corrected, a 16 cm collider remains 5x, and a 24 cm collider remains 3.33x. These are representative test dimensions, not measured dimensions of any specific H3VR item.
