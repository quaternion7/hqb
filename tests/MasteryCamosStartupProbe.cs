// Temporary in-game diagnostic. Only runs with -hqb-startup-check and quits after checking.
using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("quaternion.hqb.startupcheck", "HQB Startup Check", "1.0.0")]
[BepInDependency("NGA.MasteryCamos")]
[BepInDependency("quaternion.hqb")]
public sealed class MasteryCamosStartupProbe : BaseUnityPlugin
{
    private bool _requested;

    private void Awake()
    {
        _requested = Array.IndexOf(Environment.GetCommandLineArgs(), "-hqb-startup-check") >= 0;
    }

    private void Update()
    {
        if (!_requested) return;
        _requested = false;
        try
        {
            Assembly gameAssembly = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetName().Name == "Assembly-CSharp") gameAssembly = assembly;
            if (gameAssembly == null) throw new Exception("Game assembly not loaded.");
            string[] names = { "FVRFireArmMagazine", "FVRFireArmRound", "FVRFireArmClip", "FVRGrenade", "PinnedGrenade", "Speedloader" };
            foreach (string name in names)
            {
                Type type = gameAssembly.GetType("FistVR." + name, true);
                MethodInfo method = AccessTools.DeclaredMethod(type, "DuplicateFromSpawnLock");
                Patches patches = Harmony.GetPatchInfo(method);
                bool mastery = false;
                bool hqb = false;
                if (patches != null)
                {
                    foreach (Patch patch in patches.Postfixes)
                    {
                        if (patch.owner == "NGA.MasteryCamos") mastery = true;
                        if (patch.owner == "quaternion.hqb") hqb = true;
                    }
                }
                if (!mastery || !hqb) throw new Exception(name + " missing postfix: MasteryCamos=" + mastery + ", HQB=" + hqb);
                Logger.LogInfo("HQB_STARTUP_CHECK: " + name + " has both spawnlock postfixes.");
            }
            Logger.LogMessage("HQB_STARTUP_CHECK: PASS - all six MasteryCamos and HQB spawnlock postfixes installed in H3VR.");
        }
        catch (Exception error)
        {
            Logger.LogError("HQB_STARTUP_CHECK: FAIL - " + error);
        }
        Application.Quit();
    }
}
