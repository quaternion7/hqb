using System;
using System.ComponentModel;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FistVR;
using HarmonyLib;

namespace HandQuickbelts
{
    [BepInAutoPlugin]
    [BepInProcess("h3vr.exe")]
    public partial class Plugin : BaseUnityPlugin
    {
        // Change this locally and rebuild when full hierarchy/geometry logging is
        // needed during development. It is intentionally not a user config entry.
        internal static readonly bool DeveloperDiagnosticsEnabled = false;

        internal static readonly UnityEngine.Vector3 DefaultAnchorPosition = new UnityEngine.Vector3(-1.7515284f, -0.57437253f, -0.59017724f);
        internal static readonly UnityEngine.Vector3 DefaultAnchorRotation = new UnityEngine.Vector3(-62.12213f, -120.375916f, -44.089874f);

        private Harmony _harmony;
        private HandQuickbeltController _controller;
        private bool _resetInProgress;

        internal static Plugin Instance { get; private set; }
        internal new static ManualLogSource Logger { get; private set; }

        internal static ConfigEntry<int> LeftLargeSlots { get; private set; }
        internal static ConfigEntry<int> LeftMediumSlots { get; private set; }
        internal static ConfigEntry<int> LeftSmallSlots { get; private set; }
        internal static ConfigEntry<int> RightLargeSlots { get; private set; }
        internal static ConfigEntry<int> RightMediumSlots { get; private set; }
        internal static ConfigEntry<int> RightSmallSlots { get; private set; }
        internal static ConfigEntry<int> LayoutColumns { get; private set; }
        internal static ConfigEntry<int> ColumnSpacingMillimeters { get; private set; }
        internal static ConfigEntry<int> RowSpacingMillimeters { get; private set; }
        internal static ConfigEntry<int> SlotDiameterMillimeters { get; private set; }
        internal static ConfigEntry<bool> MiniaturizeStoredObjects { get; private set; }
        internal static ConfigEntry<int> StoredObjectTargetSizeMillimeters { get; private set; }
        internal static ConfigEntry<bool> EnableAdjustmentHandles { get; private set; }
        internal static ConfigEntry<bool> ResetAllSettings { get; private set; }
        internal static ConfigEntry<UnityEngine.Vector3> AnchorPosition { get; private set; }
        internal static ConfigEntry<UnityEngine.Vector3> AnchorRotation { get; private set; }

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            if (!TypeDescriptor.GetConverter(typeof(UnityEngine.Vector3)).CanConvertFrom(typeof(string)))
            {
                TypeDescriptor.AddAttributes(typeof(UnityEngine.Vector3), new TypeConverterAttribute(typeof(Vector3ConfigTypeConverter)));
            }

            EnableAdjustmentHandles = Config.Bind("Live Adjustment", "Enable Adjustment Handles", false, "Show grabbable anchor handles. Grab each handle with the opposite hand; releasing it saves the new palm-local position and rotation.");
            ResetAllSettings = Config.Bind("Live Adjustment", "Reset All Settings", false, "Turn this on to restore every Hand Quickbelts setting to its default. It turns itself off after resetting.");

            LeftLargeSlots = BindSlotCount("Left Hand", "Large Slots", 1);
            LeftMediumSlots = BindSlotCount("Left Hand", "Medium Slots", 0);
            LeftSmallSlots = BindSlotCount("Left Hand", "Small Slots", 0);
            RightLargeSlots = BindSlotCount("Right Hand", "Large Slots", 1);
            RightMediumSlots = BindSlotCount("Right Hand", "Medium Slots", 0);
            RightSmallSlots = BindSlotCount("Right Hand", "Small Slots", 0);

            LayoutColumns = Config.Bind("Layout", "Columns", 2, "Number of slots across the forearm before starting another row. Values below 1 behave as 1.");
            ColumnSpacingMillimeters = Config.Bind("Layout", "Column Spacing (mm)", 120, "Horizontal center-to-center spacing between slots, in millimeters. The default leaves a 20 mm gap between 100 mm slots. This setting has no configured limit.");
            RowSpacingMillimeters = Config.Bind("Layout", "Row Spacing (mm)", 120, "Spacing toward the elbow between rows, in millimeters. The default leaves a 20 mm gap between 100 mm slots. This setting has no configured limit.");
            SlotDiameterMillimeters = Config.Bind("Layout", "Slot Diameter (mm)", 100, "World-space diameter shared by the native sphere visuals and spherical interaction area. This setting has no configured limit.");

            MiniaturizeStoredObjects = Config.Bind("Stored Objects", "Miniaturize Stored Objects", true, "Scale objects down while they are stored, using the same collider-bounds fitting approach as SpineHero. Their original scale is restored when they leave the slot.");
            StoredObjectTargetSizeMillimeters = Config.Bind("Stored Objects", "Target Size (mm)", 80, "Maximum size of a stored object's combined collider bounds on each axis. Scaling is uniform and never enlarges small objects. The 80 mm default fits inside the 100 mm slot. This setting has no configured limit.");

            AnchorPosition = Config.Bind("Anchor", "Position (meters)", DefaultAnchorPosition, "Shared palm-local anchor position as x, y, z in meters. The same pose is applied symmetrically to both hands.");
            AnchorRotation = Config.Bind("Anchor", "Rotation (degrees)", DefaultAnchorRotation, "Shared palm-local Euler rotation as x, y, z in degrees. Adjustment handles are the recommended way to change this.");

            _controller = new HandQuickbeltController();
            SubscribeToConfigChanges();

            _harmony = new Harmony(Id);
            _harmony.PatchAll();
            Logger.LogMessage(string.Format("{0} {1} loaded.", Name, Version));
        }

        private void Update()
        {
            if (_controller != null)
            {
                _controller.Update();
            }
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.Dispose();
                _controller = null;
            }

            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
            }

            Instance = null;
        }

        internal void OnQuickbeltConfigured(FVRPlayerBody body)
        {
            if (_controller != null)
            {
                _controller.OnQuickbeltConfigured(body);
            }
        }

        private ConfigEntry<int> BindSlotCount(string hand, string size, int defaultValue)
        {
            return Config.Bind(
                hand,
                size,
                defaultValue,
                new ConfigDescription(
                    string.Format("Number of {0} quickbelt slots attached below the {1}. Changing this drops items held by hand slots.", size.ToLowerInvariant(), hand.ToLowerInvariant()),
                    new AcceptableValueRange<int>(0, 10)));
        }

        private void SubscribeToConfigChanges()
        {
            LeftLargeSlots.SettingChanged += OnSlotCountChanged;
            LeftMediumSlots.SettingChanged += OnSlotCountChanged;
            LeftSmallSlots.SettingChanged += OnSlotCountChanged;
            RightLargeSlots.SettingChanged += OnSlotCountChanged;
            RightMediumSlots.SettingChanged += OnSlotCountChanged;
            RightSmallSlots.SettingChanged += OnSlotCountChanged;

            LayoutColumns.SettingChanged += OnLayoutChanged;
            ColumnSpacingMillimeters.SettingChanged += OnLayoutChanged;
            RowSpacingMillimeters.SettingChanged += OnLayoutChanged;
            SlotDiameterMillimeters.SettingChanged += OnLayoutChanged;
            EnableAdjustmentHandles.SettingChanged += OnLayoutChanged;
            AnchorPosition.SettingChanged += OnLayoutChanged;
            AnchorRotation.SettingChanged += OnLayoutChanged;
            ResetAllSettings.SettingChanged += OnResetAllSettingsChanged;
        }

        private void OnSlotCountChanged(object sender, EventArgs eventArgs)
        {
            if (_controller != null)
            {
                _controller.RequestRebuild();
            }
        }

        private void OnLayoutChanged(object sender, EventArgs eventArgs)
        {
            if (_controller != null)
            {
                _controller.RequestLayoutRefresh();
            }
        }

        internal void SaveAnchorPose(bool isLeftHand, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation)
        {
            UnityEngine.Vector3 canonicalPosition = isLeftHand ? MirrorPositionAcrossX(position) : position;
            UnityEngine.Quaternion canonicalRotation = isLeftHand ? MirrorRotationAcrossX(rotation) : rotation;
            UnityEngine.Vector3 eulerAngles = canonicalRotation.eulerAngles;

            AnchorPosition.Value = canonicalPosition;
            AnchorRotation.Value = new UnityEngine.Vector3(
                NormalizeAngle(eulerAngles.x),
                NormalizeAngle(eulerAngles.y),
                NormalizeAngle(eulerAngles.z));
            Config.Save();

            Logger.LogInfo(string.Format(
                "Saved {0} anchor: position {1} meters, rotation {2} degrees.",
                isLeftHand ? "left" : "right",
                AnchorPosition.Value,
                AnchorRotation.Value));
        }

        internal static UnityEngine.Vector3 MirrorPositionAcrossX(UnityEngine.Vector3 position)
        {
            return new UnityEngine.Vector3(-position.x, position.y, position.z);
        }

        internal static UnityEngine.Quaternion MirrorRotationAcrossX(UnityEngine.Quaternion rotation)
        {
            return new UnityEngine.Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
        }

        private void OnResetAllSettingsChanged(object sender, EventArgs eventArgs)
        {
            if (_resetInProgress || !ResetAllSettings.Value)
            {
                return;
            }

            _resetInProgress = true;

            EnableAdjustmentHandles.Value = false;

            LeftLargeSlots.Value = 1;
            LeftMediumSlots.Value = 0;
            LeftSmallSlots.Value = 0;
            RightLargeSlots.Value = 1;
            RightMediumSlots.Value = 0;
            RightSmallSlots.Value = 0;

            LayoutColumns.Value = 2;
            ColumnSpacingMillimeters.Value = 120;
            RowSpacingMillimeters.Value = 120;
            SlotDiameterMillimeters.Value = 100;

            MiniaturizeStoredObjects.Value = true;
            StoredObjectTargetSizeMillimeters.Value = 80;

            AnchorPosition.Value = DefaultAnchorPosition;
            AnchorRotation.Value = DefaultAnchorRotation;

            ResetAllSettings.Value = false;
            Config.Save();
            _resetInProgress = false;

            if (_controller != null)
            {
                _controller.RequestRebuild();
            }

            Logger.LogInfo("Reset all settings to defaults.");
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180.0f ? angle - 360.0f : angle;
        }
    }
}
