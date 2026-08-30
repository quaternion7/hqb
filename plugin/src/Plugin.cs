using System;
using System.ComponentModel;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FistVR;
using HarmonyLib;
using UnityEngine;

namespace HandQuickbelts
{
    [BepInAutoPlugin]
    [BepInProcess("h3vr.exe")]
    public partial class Plugin : BaseUnityPlugin
    {
        internal static readonly Vector3 DefaultAnchorPosition = new Vector3(-1.4656025f, 0.07558223f, -0.8075327f);
        internal static readonly Vector3 DefaultAnchorRotation = new Vector3(-6.0018616f, 36.728462f, 174.04753f);

        internal static Plugin Instance { get; private set; }
        internal new static ManualLogSource Logger { get; private set; }

        internal static ConfigEntry<int> LeftLargeSlots { get; private set; }
        internal static ConfigEntry<int> LeftMediumSlots { get; private set; }
        internal static ConfigEntry<int> LeftSmallSlots { get; private set; }
        internal static ConfigEntry<int> RightLargeSlots { get; private set; }
        internal static ConfigEntry<int> RightMediumSlots { get; private set; }
        internal static ConfigEntry<int> RightSmallSlots { get; private set; }
        internal static ConfigEntry<int> GridColumns { get; private set; }
        internal static ConfigEntry<int> SlotSpacingMillimeters { get; private set; }
        internal static ConfigEntry<int> SlotDiameterMillimeters { get; private set; }
        internal static ConfigEntry<Vector3> AnchorPosition { get; private set; }
        internal static ConfigEntry<Vector3> AnchorRotation { get; private set; }
        internal static ConfigEntry<bool> ShowAdjustmentHandles { get; private set; }
        internal static ConfigEntry<bool> ResetAllSettings { get; private set; }
        internal static ConfigEntry<bool> MiniaturizeStoredObjects { get; private set; }
        internal static ConfigEntry<int> StoredSizePercent { get; private set; }

        private HandQuickbeltController _controller;
        private Harmony _harmony;
        private bool _resetting;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            RegisterVectorConverter();
            BindConfig();

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
                _controller.Tick();
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

        private void BindConfig()
        {
            LeftLargeSlots = BindSlotCount("Left Large Slots", 1, "Large quickbelt slots attached below the left hand.");
            LeftMediumSlots = BindSlotCount("Left Medium Slots", 0, "Medium quickbelt slots attached below the left hand.");
            LeftSmallSlots = BindSlotCount("Left Small Slots", 0, "Small quickbelt slots attached below the left hand.");
            RightLargeSlots = BindSlotCount("Right Large Slots", 1, "Large quickbelt slots attached below the right hand.");
            RightMediumSlots = BindSlotCount("Right Medium Slots", 0, "Medium quickbelt slots attached below the right hand.");
            RightSmallSlots = BindSlotCount("Right Small Slots", 0, "Small quickbelt slots attached below the right hand.");
            GridColumns = Config.Bind("Slots", "Grid Columns", 2, "Slots per row before the grid wraps toward the elbow.");
            SlotSpacingMillimeters = Config.Bind("Slots", "Slot Spacing (mm)", 120, "World-space center-to-center spacing on both grid axes.");
            SlotDiameterMillimeters = Config.Bind("Slots", "Slot Diameter (mm)", 100, "World-space diameter of each native sphere visual and interaction volume.");

            AnchorPosition = Config.Bind("Placement", "Anchor Position (meters)", DefaultAnchorPosition, "Shared palm-local position. The left hand uses its mirrored pose.");
            AnchorRotation = Config.Bind("Placement", "Anchor Rotation (degrees)", DefaultAnchorRotation, "Shared palm-local Euler rotation. The left hand uses its mirrored pose.");
            ShowAdjustmentHandles = Config.Bind("Placement", "Show Adjustment Handles", false, "Show grabbable handles for moving and rotating the shared anchor pose.");
            ResetAllSettings = Config.Bind("Placement", "Reset All Settings", false, "Restore every Hand Quickbelts setting to its default, then turn this toggle off.");

            MiniaturizeStoredObjects = Config.Bind("Stored Objects", "Miniaturize Stored Objects", true, "Reduce stored items to fit and restore their original scale before removal.");
            StoredSizePercent = Config.Bind(
                "Stored Objects",
                "Stored Size (% of Slot Diameter)",
                80,
                new ConfigDescription(
                    "Maximum stored-object size as a percentage of the slot diameter. Objects are reduced to fit but never enlarged.",
                    new AcceptableValueRange<int>(1, 1000)));
        }

        private ConfigEntry<int> BindSlotCount(string name, int defaultValue, string description)
        {
            return Config.Bind(
                "Slots",
                name,
                defaultValue,
                new ConfigDescription(description + " Changing a slot count drops items stored in hand slots.", new AcceptableValueRange<int>(0, 10)));
        }

        private void SubscribeToConfigChanges()
        {
            ConfigEntry<int>[] slotCounts =
            {
                LeftLargeSlots, LeftMediumSlots, LeftSmallSlots,
                RightLargeSlots, RightMediumSlots, RightSmallSlots
            };
            for (int index = 0; index < slotCounts.Length; index++)
            {
                slotCounts[index].SettingChanged += OnSlotCountChanged;
            }

            GridColumns.SettingChanged += OnLayoutChanged;
            SlotSpacingMillimeters.SettingChanged += OnLayoutChanged;
            SlotDiameterMillimeters.SettingChanged += OnLayoutChanged;
            AnchorPosition.SettingChanged += OnLayoutChanged;
            AnchorRotation.SettingChanged += OnLayoutChanged;
            ShowAdjustmentHandles.SettingChanged += OnLayoutChanged;
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
                _controller.RequestLayout();
            }
        }

        private void OnResetAllSettingsChanged(object sender, EventArgs eventArgs)
        {
            if (_resetting || !ResetAllSettings.Value)
            {
                return;
            }

            _resetting = true;
            LeftLargeSlots.Value = 1;
            LeftMediumSlots.Value = 0;
            LeftSmallSlots.Value = 0;
            RightLargeSlots.Value = 1;
            RightMediumSlots.Value = 0;
            RightSmallSlots.Value = 0;
            GridColumns.Value = 2;
            SlotSpacingMillimeters.Value = 120;
            SlotDiameterMillimeters.Value = 100;
            AnchorPosition.Value = DefaultAnchorPosition;
            AnchorRotation.Value = DefaultAnchorRotation;
            ShowAdjustmentHandles.Value = false;
            MiniaturizeStoredObjects.Value = true;
            StoredSizePercent.Value = 80;
            ResetAllSettings.Value = false;
            Config.Save();
            _resetting = false;

            if (_controller != null)
            {
                _controller.RequestRebuild();
            }
            Logger.LogInfo("Reset all settings to defaults.");
        }

        internal void RebuildAfterQuickbeltChange(FVRPlayerBody body)
        {
            if (_controller != null)
            {
                _controller.RebuildAfterQuickbeltChange(body);
            }
        }

        internal void SaveAnchorPose(bool isLeftHand, Vector3 position, Quaternion rotation)
        {
            Vector3 canonicalPosition = isLeftHand ? MirrorPosition(position) : position;
            Quaternion canonicalRotation = isLeftHand ? MirrorRotation(rotation) : rotation;
            Vector3 euler = canonicalRotation.eulerAngles;

            AnchorPosition.Value = canonicalPosition;
            AnchorRotation.Value = new Vector3(NormalizeAngle(euler.x), NormalizeAngle(euler.y), NormalizeAngle(euler.z));
            Config.Save();
            Logger.LogInfo(string.Format("Saved shared anchor from the {0} handle: position {1}, rotation {2}.", isLeftHand ? "left" : "right", AnchorPosition.Value, AnchorRotation.Value));
        }

        internal static Vector3 MirrorPosition(Vector3 position)
        {
            return new Vector3(-position.x, position.y, position.z);
        }

        internal static Quaternion MirrorRotation(Quaternion rotation)
        {
            return new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180.0f ? angle - 360.0f : angle;
        }

        private static void RegisterVectorConverter()
        {
            if (!TypeDescriptor.GetConverter(typeof(Vector3)).CanConvertFrom(typeof(string)))
            {
                TypeDescriptor.AddAttributes(typeof(Vector3), new TypeConverterAttribute(typeof(Vector3ConfigTypeConverter)));
            }
        }
    }

    public sealed class Vector3ConfigTypeConverter : System.ComponentModel.TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string text = value as string;
            if (text == null)
            {
                return base.ConvertFrom(context, culture, value);
            }

            string[] parts = text.Trim().Trim('(', ')').Split(',');
            if (parts.Length != 3)
            {
                throw new FormatException("Expected a vector in x, y, z format.");
            }

            return new Vector3(
                float.Parse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture),
                float.Parse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture),
                float.Parse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture));
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is Vector3)
            {
                Vector3 vector = (Vector3)value;
                return string.Format(CultureInfo.InvariantCulture, "{0:0.######}, {1:0.######}, {2:0.######}", vector.x, vector.y, vector.z);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
