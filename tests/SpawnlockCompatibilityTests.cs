// Runs in a temporary Unity project with real Unity transforms and HarmonyX.
// FistVR below is a minimal spawning model, not the game or MasteryCamos binary.
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using FistVR;
using HandQuickbelts;
using HarmonyLib;
using UnityEditor;
using UnityEngine;

public static class SpawnlockCompatibilityTests
{
    private static int _passed;
    private static float _copyMultiplier = 1f;
    private static bool _copyScale = true;
    private static bool _nullResult;
    private static bool _returnSource;
    private static readonly Type[] Types = { typeof(FVRFireArmMagazine), typeof(FVRFireArmRound),
        typeof(FVRFireArmClip), typeof(FVRGrenade), typeof(PinnedGrenade), typeof(Speedloader) };

    public static void Run()
    {
        try
        {
            // Install HQB first, then the external postfix: validates priority, not installation order.
            new Harmony("tests.hqb").CreateClassProcessor(typeof(SpawnlockScaleCompatibility)).Patch();
            foreach (Type type in Types)
            {
                Check(type.Name + " vanilla", type, true, 1f, 1f, 1f, 1f);
                Check(type.Name + " vanilla tiny original", type, true, 0.1f, 1f, 1f, 1f);
                Check(type.Name + " vanilla miniaturized", type, true, 1f, 0.3f, 1f, 1f);
            }
            InstallExternal();
            foreach (Type type in Types)
            {
                Check(type.Name + " corrected", type, true, 1f, 1f, 1f, 1f);
                Check(type.Name + " intentionally doubled", type, true, 2f, 1f, 1f, 2f);
                Check(type.Name + " non-unit prefab", type, true, 0.5f, 1f, 0.5f, 0.5f);
                Check(type.Name + " body slot unchanged", type, false, 1f, 1f, 1f, 10f);
                Check(type.Name + " miniaturized 3x corrected", type, true, 1f, 0.3f, 1f, 1f);
                Check(type.Name + " miniaturized 0.5x corrected", type, true, 1f, 0.05f, 1f, 1f);
                Check(type.Name + " miniaturized intentional double", type, true, 2f, 0.3f, 1f, 2f);
                Check(type.Name + " miniaturized non-unit prefab", type, true, 0.5f, 0.3f, 0.5f, 0.5f);
                Check(type.Name + " miniaturized body slot unchanged", type, false, 1f, 0.3f, 1f, 3f);
                // Exercise actual collider fitting, including the magazine/grenade-sized
                // cases that retained the copied scale in 1.0.0.
                Check(type.Name + " collider 4cm corrected", type, true, 1f, 1f, 1f, 1f, colliderLength: 0.04f);
                Check(type.Name + " collider 16cm corrected", type, true, 1f, 1f, 1f, 1f, colliderLength: 0.16f);
                Check(type.Name + " collider 24cm corrected", type, true, 1f, 1f, 1f, 1f, colliderLength: 0.24f);
            }
            _copyMultiplier = 1.005f;
            Check("within tolerance", Types[0], true, 1f, 1f, 1f, 1.005f);
            Check("miniaturized within tolerance", Types[0], true, 1f, 0.3f, 1f, 1.005f);
            _copyMultiplier = 1.02f;
            Check("outside tolerance", Types[0], true, 1f, 1f, 1f, 10.2f);
            Check("miniaturized outside tolerance", Types[0], true, 1f, 0.3f, 1f, 3.06f);
            _copyMultiplier = 1f;
            Check("fresh insertion, untracked", Types[0], true, 1f, 1f, 1f, 1f, false);
            Check("approximately 0.1 parent", Types[0], true, 1f, 1f, 1f, 1f, true, 0.10001f);
            Check("wrong parent scale excluded", Types[0], true, 1f, 1f, 1f, 5f, true, 0.2f);
            Check("parented clone", Types[0], true, 1f, 1f, 1f, 1f, true, 0.1f, true);
            Check("parented miniaturized clone", Types[0], true, 1f, 0.3f, 1f, 1f, true, 0.1f, true);
            _copyScale = false;
            Check("normal clone with external mod loaded", Types[0], true, 0.1f, 0.1f, 1f, 1f);
            _copyScale = true;
            Check("ambiguous prefab match excluded", Types[0], true, 0.1f, 1f, 1f, 1f);
            Check("miniaturized ambiguous prefab match excluded", Types[0], true, 2f, 0.05f, 1f, 1f);
            _nullResult = true;
            Check("null result", Types[0], true, 1f, 1f, 1f, 0f);
            _nullResult = false;
            _returnSource = true;
            Check("source result not modified", Types[0], true, 1f, 1f, 1f, 1f);
            _returnSource = false;
            // Reverse patch installation order as well.
            new Harmony("tests.hqb").UnpatchSelf();
            new Harmony("tests.hqb").CreateClassProcessor(typeof(SpawnlockScaleCompatibility)).Patch();
            Check("reverse installation order", Types[0], true, 1f, 1f, 1f, 1f);
            Check("miniaturized reverse installation order", Types[0], true, 1f, 0.3f, 1f, 1f);
            Debug.Log("HQB_TEST_SUCCESS: " + _passed + " cases passed");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogError("HQB_TEST_FAILURE: " + error);
            EditorApplication.Exit(1);
        }
    }

    private static void InstallExternal()
    {
        Harmony harmony = new Harmony("tests.unrelated.scale.copier");
        foreach (Type type in Types)
        {
            harmony.Patch(AccessTools.DeclaredMethod(type, "DuplicateFromSpawnLock"),
                postfix: new HarmonyMethod(typeof(SpawnlockCompatibilityTests), "ExternalPostfix"));
        }
    }

    private static void ExternalPostfix(FVRPhysicalObject __instance, ref GameObject __result)
    {
        if (_nullResult) { UnityEngine.Object.DestroyImmediate(__result); __result = null; return; }
        if (_returnSource) { UnityEngine.Object.DestroyImmediate(__result); __result = __instance.gameObject; return; }
        if (_copyScale) __result.transform.localScale = __instance.transform.localScale * _copyMultiplier;
        if (FVRPhysicalObject.CloneParent != null)
            __result.transform.SetParent(FVRPhysicalObject.CloneParent, true);
        __result.name = "camo-preserved";
    }

    private static void Check(string name, Type type, bool hqb, float original, float fit, float prefabScale,
        float expected, bool track = true, float parentScale = 0.1f, bool parentClone = false, float colliderLength = 0f)
    {
        GameObject root = new GameObject("palm");
        root.transform.localScale = Vector3.one * parentScale;
        root.transform.rotation = colliderLength > 0f ? Quaternion.identity : Quaternion.Euler(23f, 41f, -17f);
        GameObject slotObject = new GameObject("slot");
        slotObject.transform.SetParent(root.transform, false);
        FVRQuickBeltSlot slot = slotObject.AddComponent<FVRQuickBeltSlot>();
        slot.QuickbeltRoot = slotObject.transform;
        if (hqb) slotObject.AddComponent<HandQuickbeltSlotMarker>();
        GameObject prefab = new GameObject("prefab");
        prefab.transform.localScale = Vector3.one * prefabScale;
        GameObject source = new GameObject("source");
        source.transform.localScale = Vector3.one * original;
        source.transform.SetParent(slotObject.transform, true);
        FVRPhysicalObject item = (FVRPhysicalObject)source.AddComponent(type);
        item.ObjectWrapper = new FVRObject { Prefab = prefab };
        item.QuickbeltSlot = slot;
        slot.HeldObject = item;
        if (colliderLength > 0f)
        {
            BoxCollider collider = source.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.01f, 0.01f, colliderLength);
        }
        if (track)
        {
            HandQuickbeltMiniaturizer mini = slotObject.AddComponent<HandQuickbeltMiniaturizer>();
            typeof(HandQuickbeltMiniaturizer).GetMethod("Track", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(mini, new object[] { item });
            if (colliderLength > 0f)
            {
                typeof(HandQuickbeltMiniaturizer).GetMethod("ApplyScale", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(mini, null);
                AssertScale(name + " fit", source.transform.lossyScale,
                    Vector3.one * Mathf.Min(1f, 0.08f / colliderLength));
            }
        }
        source.transform.localScale *= fit;
        Vector3 storedScale = source.transform.localScale;
        GameObject cloneParent = new GameObject("clone parent");
        cloneParent.transform.localScale = Vector3.one * 0.25f;
        FVRPhysicalObject.CloneParent = parentClone ? cloneParent.transform : null;
        for (int pull = 0; pull < 3; pull++)
        {
            GameObject clone = item.DuplicateFromSpawnLock(null);
            if (_nullResult)
            {
                if (clone != null) throw new Exception(name + ": expected null");
            }
            else
            {
                AssertScale(name, clone.transform.lossyScale, Vector3.one * expected);
                if (!_returnSource && Harmony.GetPatchInfo(AccessTools.DeclaredMethod(type, "DuplicateFromSpawnLock")).Owners.Contains("tests.unrelated.scale.copier")
                    && clone.name != "camo-preserved") throw new Exception(name + ": other postfix effects lost");
                if (clone != source) UnityEngine.Object.DestroyImmediate(clone);
            }
            AssertScale(name + " source", source.transform.localScale, storedScale);
        }
        FVRPhysicalObject.CloneParent = null;
        UnityEngine.Object.DestroyImmediate(root);
        UnityEngine.Object.DestroyImmediate(prefab);
        UnityEngine.Object.DestroyImmediate(cloneParent);
        _passed++;
        Debug.Log("PASS: " + name);
    }

    private static void AssertScale(string name, Vector3 actual, Vector3 expected)
    {
        if ((actual - expected).magnitude > 0.0002f * Mathf.Max(1f, expected.magnitude))
            throw new Exception(name + ": expected " + expected + ", got " + actual);
    }
}

namespace FistVR
{
    public class FVRViveHand : MonoBehaviour { }
    public class FVRQuickBeltSlot : MonoBehaviour { public Transform QuickbeltRoot; public FVRPhysicalObject HeldObject; }
    public class FVRObject { public GameObject Prefab; public GameObject GetGameObject() { return Prefab; } }
    public class FVRPhysicalObject : MonoBehaviour
    {
        public static Transform CloneParent;
        public FVRQuickBeltSlot QuickbeltSlot;
        public FVRObject ObjectWrapper;
        public bool IsHeld;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual GameObject DuplicateFromSpawnLock(FVRViveHand hand) { return UnityEngine.Object.Instantiate(ObjectWrapper.GetGameObject()); }
    }
    public class FVRFireArmMagazine : FVRPhysicalObject { [MethodImpl(MethodImplOptions.NoInlining)] public override GameObject DuplicateFromSpawnLock(FVRViveHand hand) { return base.DuplicateFromSpawnLock(hand); } }
    public class FVRFireArmRound : FVRPhysicalObject { [MethodImpl(MethodImplOptions.NoInlining)] public override GameObject DuplicateFromSpawnLock(FVRViveHand hand) { return base.DuplicateFromSpawnLock(hand); } }
    public class FVRFireArmClip : FVRPhysicalObject { [MethodImpl(MethodImplOptions.NoInlining)] public override GameObject DuplicateFromSpawnLock(FVRViveHand hand) { return base.DuplicateFromSpawnLock(hand); } }
    public class FVRGrenade : FVRPhysicalObject { [MethodImpl(MethodImplOptions.NoInlining)] public override GameObject DuplicateFromSpawnLock(FVRViveHand hand) { return base.DuplicateFromSpawnLock(hand); } }
    public class PinnedGrenade : FVRPhysicalObject { [MethodImpl(MethodImplOptions.NoInlining)] public override GameObject DuplicateFromSpawnLock(FVRViveHand hand) { return base.DuplicateFromSpawnLock(hand); } }
    public class Speedloader : FVRPhysicalObject { [MethodImpl(MethodImplOptions.NoInlining)] public override GameObject DuplicateFromSpawnLock(FVRViveHand hand) { return base.DuplicateFromSpawnLock(hand); } }
}

namespace HandQuickbelts
{
    internal sealed class HandQuickbeltSlotMarker : MonoBehaviour { }
    internal class Setting<T> { internal T Value; internal Setting(T value) { Value = value; } }
    internal static class Plugin
    {
        internal static Setting<bool> MiniaturizeStoredObjects = new Setting<bool>(true);
        internal static Setting<int> SlotDiameterMillimeters = new Setting<int>(100);
        internal static Setting<int> StoredSizePercent = new Setting<int>(80);
    }
}
