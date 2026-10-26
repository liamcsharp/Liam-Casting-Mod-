using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using GorillaNetworking;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LiamCasting
{
    [BepInPlugin("com.liamcsharp.casting", "Liam Casting Mod", "2.3.0")]
    public class LiamCastingPlugin : BaseUnityPlugin
    {
        private const float TitleHeight = 40f;
        private const float SidebarWidth = 140f;

        private static readonly string[] tabNames = new string[]
        {
            "CAST", "CAMERA", "FLIGHT", "ROOM", "TAGS", "SCORE", "FX", "WORLD", "THEME", "PRESETS"
        };
        private static readonly string[] modeLabels = new string[] { "Head", "Body", "Eyes", "Front" };
        private static readonly string[] modeInfo = new string[]
        {
            "Third person behind the head. Follows where the player looks.",
            "Third person behind the body. Steady and smooth.",
            "First person, straight through the player's eyes. Fine tune it in the Camera tab.",
            "Third person in FRONT of the player, looking back at their face."
        };
        private static readonly string[] nameTagFontStyleNames = new string[] { "Default", "Bold", "Italic", "Bold+Italic" };
        private static readonly string[] nameTagFacingModeNames = new string[] { "Camera", "Forward", "Back", "World" };
        private static readonly float[] scoreboardCountdownValues = new float[] { 5f, 10f, 15f };
        private static readonly string[] shaderEffectPresetNames = new string[]
        {
            "None", "Ambient", "Blit", "Bloom", "CAS", "Color Grade", "Fog", "God Rays", "Lens", "Sky", "SSAO", "SSR"
        };
        private static readonly string[] timeNames = new string[]
        {
            "Night", "Morning 1", "Morning 2", "Evening 1", "Evening 2", "Evening 3", "Sunset 1", "Sunset 2", "Midnight"
        };
        private static readonly string[] mapNames = new string[]
        {
            "Forest", "Caves", "City", "Arena", "Hoverpark", "Canyons", "Critters", "Metro"
        };
        private static readonly string[] mapTriggers = new string[]
        {
            "CaveToForest", "ForestToCave", "ForestToCity", "ForestToArena", "ForestToHoverboard", "ForestToCanyon", "CityToCritters", "CityToMetropolis"
        };
        private static readonly Vector3[] forestObserverPoints = new Vector3[]
        {
            new Vector3(-82f, 22f, -94f),
            new Vector3(-53f, 19f, -99f),
            new Vector3(-43f, 16f, -72f),
            new Vector3(-63f, 29f, -57f),
            new Vector3(-88f, 20f, -70f),
            new Vector3(-68f, 33f, -84f)
        };

        private struct ThemePreset
        {
            public string name;
            public float r;
            public float g;
            public float b;
            public Color accent;
        }

        private static readonly ThemePreset[] themePresets = new ThemePreset[]
        {
            new ThemePreset { name = "Classic Pink",   r = 1.00f, g = 0.80f, b = 0.90f, accent = new Color(1.00f, 0.80f, 0.90f, 1f) },
            new ThemePreset { name = "Ocean Blue",     r = 0.20f, g = 0.50f, b = 0.90f, accent = new Color(0.30f, 0.70f, 1.00f, 1f) },
            new ThemePreset { name = "Neon Green",     r = 0.15f, g = 0.90f, b = 0.30f, accent = new Color(0.40f, 1.00f, 0.40f, 1f) },
            new ThemePreset { name = "Sunset Orange",  r = 1.00f, g = 0.50f, b = 0.15f, accent = new Color(1.00f, 0.60f, 0.20f, 1f) },
            new ThemePreset { name = "Purple Haze",    r = 0.60f, g = 0.25f, b = 0.95f, accent = new Color(0.75f, 0.40f, 1.00f, 1f) },
            new ThemePreset { name = "Crimson Red",    r = 0.95f, g = 0.15f, b = 0.20f, accent = new Color(1.00f, 0.25f, 0.30f, 1f) },
            new ThemePreset { name = "Cyber Cyan",     r = 0.15f, g = 0.85f, b = 0.90f, accent = new Color(0.30f, 1.00f, 1.00f, 1f) },
            new ThemePreset { name = "Golden Hour",    r = 1.00f, g = 0.80f, b = 0.25f, accent = new Color(1.00f, 0.85f, 0.30f, 1f) },
            new ThemePreset { name = "Monochrome Gray", r = 0.70f, g = 0.70f, b = 0.70f, accent = new Color(0.85f, 0.85f, 0.85f, 1f) },
            new ThemePreset { name = "Forest Green",   r = 0.25f, g = 0.70f, b = 0.30f, accent = new Color(0.50f, 0.90f, 0.50f, 1f) },
            new ThemePreset { name = "Royal Purple",   r = 0.45f, g = 0.20f, b = 0.85f, accent = new Color(0.60f, 0.30f, 0.95f, 1f) },
            new ThemePreset { name = "Hot Pink",       r = 1.00f, g = 0.20f, b = 0.65f, accent = new Color(1.00f, 0.30f, 0.75f, 1f) },
            new ThemePreset { name = "Ice Blue",       r = 0.50f, g = 0.80f, b = 0.95f, accent = new Color(0.60f, 0.90f, 1.00f, 1f) },
            new ThemePreset { name = "Blood Moon",     r = 0.85f, g = 0.10f, b = 0.10f, accent = new Color(1.00f, 0.20f, 0.20f, 1f) },
            new ThemePreset { name = "Toxic Lime",     r = 0.65f, g = 0.90f, b = 0.15f, accent = new Color(0.75f, 1.00f, 0.20f, 1f) },
            new ThemePreset { name = "Midnight Black", r = 0.12f, g = 0.12f, b = 0.15f, accent = new Color(0.50f, 0.50f, 0.60f, 1f) }
        };

        private struct LeaderboardEntry
        {
            public string text;
            public Color color;
        }

        private ConfigEntry<bool> flyEnabledConfig;
        private ConfigEntry<float> flySpeedConfig;
        private ConfigEntry<bool> stationaryFlyConfig;

        private ConfigEntry<bool> smoothingEnabledConfig;
        private ConfigEntry<float> positionSmoothingConfig;
        private ConfigEntry<float> rotationSmoothingConfig;
        private ConfigEntry<float> fovConfig;
        private ConfigEntry<float> nearClipConfig;
        private ConfigEntry<float> headDistanceConfig;
        private ConfigEntry<float> yOffsetConfig;
        private ConfigEntry<float> xOffsetConfig;
        private ConfigEntry<bool> trackHeadRollConfig;
        private ConfigEntry<int> cameraModeConfig;

        private ConfigEntry<float> frontDistanceConfig;
        private ConfigEntry<float> frontHeightConfig;
        private ConfigEntry<float> frontSideConfig;
        private ConfigEntry<float> frontLookHeightConfig;
        private ConfigEntry<bool> frontFollowHeadConfig;

        private ConfigEntry<float> eyeFovConfig;
        private ConfigEntry<float> eyeNearClipConfig;
        private ConfigEntry<float> eyeForwardConfig;
        private ConfigEntry<float> eyeUpConfig;
        private ConfigEntry<float> eyePositionSmoothingConfig;
        private ConfigEntry<float> eyeRotationSmoothingConfig;
        private ConfigEntry<bool> eyeStabilizeRollConfig;

        private ConfigEntry<bool> rigLerpEnabledConfig;
        private ConfigEntry<float> rigBodyLerpConfig;
        private ConfigEntry<float> rigFingerLerpConfig;

        private ConfigEntry<bool> performanceOptimizationConfig;
        private ConfigEntry<float> rigCacheRefreshIntervalConfig;
        private ConfigEntry<float> cameraRenderScaleConfig;

        private ConfigEntry<bool> nameTagsEnabledConfig;
        private ConfigEntry<bool> nameTagShowHzConfig;
        private ConfigEntry<int> nameTagFontIndexConfig;
        private ConfigEntry<float> nameTagHeightConfig;
        private ConfigEntry<float> nameTagSizeConfig;
        private ConfigEntry<int> nameTagFacingModeConfig;
        private ConfigEntry<bool> nameTagShowDistanceConfig;
        private ConfigEntry<bool> nameTagShowSpeedConfig;
        private ConfigEntry<bool> nameTagShowTaggedConfig;
        private ConfigEntry<bool> nameTagShowPlayerColorConfig;
        private ConfigEntry<bool> nameTagShowBackgroundConfig;
        private ConfigEntry<float> nameTagMaxDistanceConfig;
        private ConfigEntry<string> nameTagPrefixConfig;
        private ConfigEntry<string> nameTagSuffixConfig;

        private ConfigEntry<bool> shaderEffectsEnabledConfig;
        private ConfigEntry<int> shaderEffectPresetIndexConfig;
        private ConfigEntry<float> shaderExposureConfig;
        private ConfigEntry<float> shaderTintStrengthConfig;
        private ConfigEntry<string> shaderTintHexConfig;
        private ConfigEntry<float> shaderVignetteConfig;
        private ConfigEntry<float> shaderScanlineConfig;
        private ConfigEntry<float> shaderLetterboxConfig;
        private ConfigEntry<string> shaderPresetPathConfig;

        private ConfigEntry<bool> autoObserverEnabledConfig;
        private ConfigEntry<float> autoObserverSpeedConfig;
        private ConfigEntry<float> autoObserverDistanceConfig;

        private ConfigEntry<bool> scoreboardEnabledConfig;
        private ConfigEntry<string> leftTeamNameConfig;
        private ConfigEntry<string> rightTeamNameConfig;
        private ConfigEntry<float> scoreboardPositionXConfig;
        private ConfigEntry<float> scoreboardPositionYConfig;
        private ConfigEntry<float> scoreboardScaleConfig;
        private ConfigEntry<float> scoreboardOpacityConfig;
        private ConfigEntry<bool> autoRefEnabledConfig;
        private ConfigEntry<float> autoRefIntervalConfig;
        private ConfigEntry<bool> scoreboardTimerAutoStartConfig;
        private ConfigEntry<int> scoreboardCountdownIndexConfig;

        private ConfigEntry<int> selectedThemeIndexConfig;

        private List<ConfigEntryBase> presetEntries = new List<ConfigEntryBase>();

        private Camera mainGameCamera;
        private float nextRigCacheRefreshTime;
        private readonly List<VRRig> cachedVRRigs = new List<VRRig>();
        private readonly List<LeaderboardEntry> cachedLeaderboardEntries = new List<LeaderboardEntry>();
        private readonly Dictionary<VRRig, GameObject> currentNameTagPlates = new Dictionary<VRRig, GameObject>();
        private readonly Dictionary<VRRig, Vector3> nameTagPreviousPositions = new Dictionary<VRRig, Vector3>();
        private readonly Dictionary<VRRig, float> nameTagSpeeds = new Dictionary<VRRig, float>();

        private int leftTeamScore;
        private int rightTeamScore;
        private bool scoreboardTimerRunning;
        private bool wasScoreboardEnabled;
        private float scoreboardElapsedSeconds;
        private float scoreboardTimeToBeatSeconds;
        private float nextAutoRefTime;

        private bool isMenuVisible;
        private bool typing;
        private int activeTab;
        private Vector2 contentScroll = Vector2.zero;
        private float contentHeight = 100f;
        private Rect windowRect = new Rect(40f, 30f, 680f, 580f);
        private bool isDraggingWindow;
        private Vector2 dragOffset;
        private float ux = 2f;
        private float uw = 400f;

        private string roomCodeInput = "";
        private float uiColorRed;
        private float uiColorGreen;
        private float uiColorBlue;
        private string uiHexColorInput = "#FFCDE6";
        private Color themeAccentColor = new Color(1f, 0.8f, 0.9f, 1f);

        private int timeIndex;
        private string presetNameInput = "";
        private string presetsFolder = "";
        private bool presetsFolderInitialized;
        private string presetStatus = "Save your current setup or load one by name.";
        private string[] presetFiles = new string[0];

        private bool isCameraActive;
        private Camera spectatorCamera;
        private RenderTexture cameraRenderTexture;
        private VRRig targetRig;
        private string targetPlayerName = "You";
        private float targetRigScale = 1f;
        private float originalBodyLerp = 1f;
        private float originalFingerLerp = 1f;
        private bool lerpCaptured;
        private Vector3 cameraTargetPos = Vector3.zero;
        private Quaternion cameraTargetRot = Quaternion.identity;
        private Vector3 lastFlightPosition = Vector3.zero;

        private bool forestLoadedCached;
        private float nextForestCheckTime;

        private Texture2D shaderOverlayTexture;
        private Texture2D shaderScanlineTexture;
        private Texture2D shaderVignetteTexture;
        private Color shaderTintColor = Color.white;
        private string shaderTintHexInput = "#FFFFFF";
        private string shaderPresetPathInput = "";
        private string shaderPresetFolder = "";
        private string shaderPresetStatus = "Use a built-in preset or load a compatible .ini file.";

        private float fpsSmoothingDeltaTime;
        private float currentFPS;

        private bool stylesCreated;
        private Texture2D whiteTex;
        private Texture2D circleTex;
        private readonly List<Texture2D> styleTextures = new List<Texture2D>();
        private readonly Color surfaceColor = new Color(0.10f, 0.11f, 0.14f, 1f);
        private readonly Color offColor = new Color(0.22f, 0.24f, 0.30f, 1f);
        private GUIStyle titleStyle;
        private GUIStyle textStyle;
        private GUIStyle mutedStyle;
        private GUIStyle sectionStyle;
        private GUIStyle valueStyle;
        private GUIStyle buttonStyle;
        private GUIStyle segActiveStyle;
        private GUIStyle tabStyle;
        private GUIStyle tabActiveStyle;
        private GUIStyle rowStyle;
        private GUIStyle inputStyle;
        private GUIStyle sliderStyle;
        private GUIStyle thumbStyle;
        private GUIStyle leaderboardTextStyle;
        private GUIStyle teamNameStyle;
        private GUIStyle teamScoreStyle;
        private GUIStyle targetLabelStyle;

        private static PropertyInfo rigIsTaggedProperty;
        private static FieldInfo rigIsTaggedField;
        private static bool rigIsTaggedPropertyChecked;
        private static bool rigIsTaggedFieldChecked;

        private void Start()
        {
            this.InitializeConfiguration();
            this.InitializePresetsFolder();
            this.InitializeShaderPresetFolder();
            this.RefreshPresetList();
            this.mainGameCamera = Camera.main;
            this.RefreshVRRigCache(true);
            this.ApplyTheme(Mathf.Clamp(this.selectedThemeIndexConfig.Value, 0, themePresets.Length - 1), false);
            this.shaderPresetPathInput = this.shaderPresetPathConfig.Value ?? string.Empty;
            this.ApplyShaderTintHex(this.shaderTintHexConfig.Value, false);
        }

        private void Update()
        {
            this.PreventAFKKick();
            this.ProcessMenuToggle();
            this.RefreshVRRigCache(false);
            this.ProcessFlightControls();
            this.ProcessPlayerSwitching();
            this.UpdateCameraSystem();
            this.UpdateSpectatorLerp();
            this.UpdateFPSCounter();
            this.ProcessAutoRef();
            this.DisplayNameTags();
            this.UpdateScoreboardTimer();
            this.ProcessScoreboardControls();
        }

        private void OnGUI()
        {
            Event e = Event.current;
            bool repaint = e.type == EventType.Repaint;
            this.EnsureStylesCreated();
            if (repaint && this.isCameraActive && this.cameraRenderTexture != null)
            {
                this.DrawCastingFeed();
            }
            this.typing = GUI.GetNameOfFocusedControl().StartsWith("tf_");
            if (repaint)
            {
                this.RenderPlayerLeaderboard();
                this.RenderTeamScoreboard();
                this.RenderTargetIndicator();
            }
            if (!this.isMenuVisible)
            {
                return;
            }
            this.RenderWindow();
        }

        private void OnDestroy()
        {
            this.RestoreTargetLerp();
            this.DeactivateCamera();
            this.UnRenderNameTags();
            this.ReleaseShaderEffectTextures();
            this.DestroyStyleTextures();
            this.cachedVRRigs.Clear();
            this.cachedLeaderboardEntries.Clear();
        }

        private void InitializeConfiguration()
        {
            this.flyEnabledConfig = base.Config.Bind<bool>("Flight", "Enabled", false, "Enable WASD flight mode");
            this.flySpeedConfig = base.Config.Bind<float>("Flight", "Speed", 15f, "Flight movement speed");
            this.stationaryFlyConfig = base.Config.Bind<bool>("Flight", "Stationary", true, "Remain stationary when idle");

            this.smoothingEnabledConfig = base.Config.Bind<bool>("Spectator", "SmoothingEnabled", true, "Enable camera position/rotation smoothing");
            this.positionSmoothingConfig = base.Config.Bind<float>("Spectator", "PositionSmoothing", 0.61f, "Camera position smoothing (0 = instant, 1 = very smooth)");
            this.rotationSmoothingConfig = base.Config.Bind<float>("Spectator", "RotationSmoothing", 0.66f, "Camera rotation smoothing (0 = instant, 1 = very smooth)");
            this.fovConfig = base.Config.Bind<float>("Spectator", "FieldOfView", 90f, "Camera field of view for head, body and front modes");
            this.nearClipConfig = base.Config.Bind<float>("Spectator", "NearClip", 0.21f, "Near clipping plane for head, body and front modes");
            this.headDistanceConfig = base.Config.Bind<float>("Spectator", "HeadDistance", 2.59f, "Distance behind the player");
            this.yOffsetConfig = base.Config.Bind<float>("Spectator", "VerticalOffset", 0.4f, "Vertical camera offset");
            this.xOffsetConfig = base.Config.Bind<float>("Spectator", "HorizontalOffset", 0f, "Horizontal camera offset");
            this.trackHeadRollConfig = base.Config.Bind<bool>("Spectator", "TrackHeadRoll", true, "Follow head tilt in head mode");
            this.cameraModeConfig = base.Config.Bind<int>("Spectator", "CameraMode", 0, "0 = Head, 1 = Body, 2 = Eyes, 3 = Front");

            this.frontDistanceConfig = base.Config.Bind<float>("Front", "Distance", 2.2f, "How far in front of the player the camera sits");
            this.frontHeightConfig = base.Config.Bind<float>("Front", "Height", 0.25f, "Camera height relative to the head");
            this.frontSideConfig = base.Config.Bind<float>("Front", "Side", 0f, "Sideways camera offset");
            this.frontLookHeightConfig = base.Config.Bind<float>("Front", "LookHeight", 0.05f, "Height of the point the camera looks at, relative to the head");
            this.frontFollowHeadConfig = base.Config.Bind<bool>("Front", "FollowHead", true, "Stay in front of where the head faces instead of the body");

            this.eyeFovConfig = base.Config.Bind<float>("Eyes", "FieldOfView", 75f, "Vertical field of view for eye mode");
            this.eyeNearClipConfig = base.Config.Bind<float>("Eyes", "NearClip", 0.03f, "Near clip for eye mode");
            this.eyeForwardConfig = base.Config.Bind<float>("Eyes", "Forward", 0.12f, "Push the camera forward from the head center");
            this.eyeUpConfig = base.Config.Bind<float>("Eyes", "Up", 0.03f, "Raise the camera from the head center");
            this.eyePositionSmoothingConfig = base.Config.Bind<float>("Eyes", "PositionSmoothing", 0.5f, "Eye mode position smoothing");
            this.eyeRotationSmoothingConfig = base.Config.Bind<float>("Eyes", "RotationSmoothing", 0.85f, "Eye mode rotation smoothing");
            this.eyeStabilizeRollConfig = base.Config.Bind<bool>("Eyes", "StabilizeRoll", false, "Keep the horizon level instead of copying head tilt");

            this.rigLerpEnabledConfig = base.Config.Bind<bool>("Spectator", "RigLerpEnabled", false, "Enable rig lerping system");
            this.rigBodyLerpConfig = base.Config.Bind<float>("Spectator", "RigBodyLerp", 0.5f, "Body lerp smoothing for rig (0.01-0.99)");
            this.rigFingerLerpConfig = base.Config.Bind<float>("Spectator", "RigFingerLerp", 0.5f, "Finger lerp smoothing for rig (0.01-0.99)");

            this.performanceOptimizationConfig = base.Config.Bind<bool>("Performance", "Optimization", true, "Reduce repeated rig scans and GUI work");
            this.rigCacheRefreshIntervalConfig = base.Config.Bind<float>("Performance", "RigCacheRefreshInterval", 0.5f, "How often the mod refreshes the player rig cache in seconds");
            this.cameraRenderScaleConfig = base.Config.Bind<float>("Performance", "CameraRenderScale", 0.75f, "Spectator camera render resolution scale");

            this.nameTagsEnabledConfig = base.Config.Bind<bool>("NameTag", "Enabled", true, "Show world-space name tag plates above players");
            this.nameTagShowHzConfig = base.Config.Bind<bool>("NameTag", "ShowHz", false, "Show local FPS for you and unavailable status for remote players");
            this.nameTagFontIndexConfig = base.Config.Bind<int>("NameTag", "FontStyle", 0, "Index into the name tag font style list");
            this.nameTagHeightConfig = base.Config.Bind<float>("NameTag", "Height", 0.51f, "Vertical offset of the name tag above the player");
            this.nameTagSizeConfig = base.Config.Bind<float>("NameTag", "Size", 0.8f, "Scale of the name tag plate");
            this.nameTagFacingModeConfig = base.Config.Bind<int>("NameTag", "FacingMode", 0, "0 = Camera, 1 = Player Forward, 2 = Player Back, 3 = World Forward");
            this.nameTagShowDistanceConfig = base.Config.Bind<bool>("NameTag", "ShowDistance", true, "Show distance from the local player");
            this.nameTagShowSpeedConfig = base.Config.Bind<bool>("NameTag", "ShowSpeed", false, "Show approximate movement speed");
            this.nameTagShowTaggedConfig = base.Config.Bind<bool>("NameTag", "ShowTaggedStatus", true, "Show tagged status");
            this.nameTagShowPlayerColorConfig = base.Config.Bind<bool>("NameTag", "UsePlayerColor", true, "Color the name with the player's material color");
            this.nameTagShowBackgroundConfig = base.Config.Bind<bool>("NameTag", "ShowBackground", true, "Show a subtle plate behind the tag");
            this.nameTagMaxDistanceConfig = base.Config.Bind<float>("NameTag", "MaxDistance", 80f, "Hide name tags beyond this distance");
            this.nameTagPrefixConfig = base.Config.Bind<string>("NameTag", "Prefix", "", "Text added before each player name");
            this.nameTagSuffixConfig = base.Config.Bind<string>("NameTag", "Suffix", "", "Text added after each player name");

            this.shaderEffectsEnabledConfig = base.Config.Bind<bool>("Shaders", "Enabled", false, "Apply lightweight effects to the casting output only");
            this.shaderEffectPresetIndexConfig = base.Config.Bind<int>("Shaders", "Preset", 0, "Selected built-in casting effect preset");
            this.shaderExposureConfig = base.Config.Bind<float>("Shaders", "Exposure", 1f, "Casting output exposure multiplier");
            this.shaderTintStrengthConfig = base.Config.Bind<float>("Shaders", "TintStrength", 0f, "Casting output tint strength");
            this.shaderTintHexConfig = base.Config.Bind<string>("Shaders", "TintHex", "#FFFFFF", "Casting output tint color");
            this.shaderVignetteConfig = base.Config.Bind<float>("Shaders", "Vignette", 0f, "Casting output vignette strength");
            this.shaderScanlineConfig = base.Config.Bind<float>("Shaders", "Scanlines", 0f, "Casting output scanline strength");
            this.shaderLetterboxConfig = base.Config.Bind<float>("Shaders", "Letterbox", 0f, "Casting output letterbox size");
            this.shaderPresetPathConfig = base.Config.Bind<string>("Shaders", "IniPresetPath", "", "Path to a .ini casting effect preset");

            this.autoObserverEnabledConfig = base.Config.Bind<bool>("Spectator", "AutoObserverEnabled", false, "Tour fixed camera locations in Forest only");
            this.autoObserverSpeedConfig = base.Config.Bind<float>("Spectator", "AutoObserverSpeed", 0.12f, "Forest observer travel speed");
            this.autoObserverDistanceConfig = base.Config.Bind<float>("Spectator", "AutoObserverDistance", 12f, "Forest observer route scale");

            this.scoreboardEnabledConfig = base.Config.Bind<bool>("Scoreboard", "Enabled", false, "Show the two-team score board");
            this.leftTeamNameConfig = base.Config.Bind<string>("Scoreboard", "LeftTeamName", "TEAM ONE", "Name of the left score board team");
            this.rightTeamNameConfig = base.Config.Bind<string>("Scoreboard", "RightTeamName", "TEAM TWO", "Name of the right score board team");
            this.scoreboardPositionXConfig = base.Config.Bind<float>("Scoreboard", "PositionX", 0f, "Horizontal score board position as a fraction of the screen");
            this.scoreboardPositionYConfig = base.Config.Bind<float>("Scoreboard", "PositionY", 0.02f, "Vertical score board position as a fraction of the screen");
            this.scoreboardScaleConfig = base.Config.Bind<float>("Scoreboard", "Scale", 1f, "Scoreboard size multiplier");
            this.scoreboardOpacityConfig = base.Config.Bind<float>("Scoreboard", "Opacity", 0.92f, "Scoreboard background opacity");
            this.autoRefEnabledConfig = base.Config.Bind<bool>("AutoRef", "Enabled", false, "Automatically cycle the spectator camera between players");
            this.autoRefIntervalConfig = base.Config.Bind<float>("AutoRef", "Interval", 12f, "Seconds between automatic referee camera targets");
            this.scoreboardTimerAutoStartConfig = base.Config.Bind<bool>("Scoreboard", "TimerAutoStart", true, "Start the score board timer when the board is enabled");
            this.scoreboardCountdownIndexConfig = base.Config.Bind<int>("Scoreboard", "CountdownIndex", 1, "Countdown selection: 0 = 5 seconds, 1 = 10 seconds, 2 = 15 seconds");

            this.selectedThemeIndexConfig = base.Config.Bind<int>("Themes", "SelectedTheme", 0, "Index of the currently active UI theme");

            this.presetEntries = new List<ConfigEntryBase>
            {
                this.flyEnabledConfig, this.flySpeedConfig, this.stationaryFlyConfig,
                this.smoothingEnabledConfig, this.positionSmoothingConfig, this.rotationSmoothingConfig,
                this.fovConfig, this.nearClipConfig, this.headDistanceConfig, this.yOffsetConfig, this.xOffsetConfig,
                this.trackHeadRollConfig, this.cameraModeConfig,
                this.frontDistanceConfig, this.frontHeightConfig, this.frontSideConfig, this.frontLookHeightConfig, this.frontFollowHeadConfig,
                this.eyeFovConfig, this.eyeNearClipConfig, this.eyeForwardConfig, this.eyeUpConfig,
                this.eyePositionSmoothingConfig, this.eyeRotationSmoothingConfig, this.eyeStabilizeRollConfig,
                this.rigLerpEnabledConfig, this.rigBodyLerpConfig, this.rigFingerLerpConfig,
                this.cameraRenderScaleConfig,
                this.nameTagsEnabledConfig, this.nameTagShowHzConfig, this.nameTagFontIndexConfig,
                this.nameTagHeightConfig, this.nameTagSizeConfig, this.nameTagFacingModeConfig,
                this.nameTagShowDistanceConfig, this.nameTagShowSpeedConfig, this.nameTagShowTaggedConfig,
                this.nameTagShowPlayerColorConfig, this.nameTagShowBackgroundConfig, this.nameTagMaxDistanceConfig,
                this.nameTagPrefixConfig, this.nameTagSuffixConfig,
                this.shaderEffectsEnabledConfig, this.shaderEffectPresetIndexConfig, this.shaderExposureConfig,
                this.shaderTintStrengthConfig, this.shaderTintHexConfig, this.shaderVignetteConfig,
                this.shaderScanlineConfig, this.shaderLetterboxConfig,
                this.autoObserverSpeedConfig, this.autoObserverDistanceConfig,
                this.scoreboardEnabledConfig, this.leftTeamNameConfig, this.rightTeamNameConfig,
                this.selectedThemeIndexConfig
            };

            this.ResetScoreboardTimer();
        }

        private Color AccentAlpha(float alpha)
        {
            return new Color(this.themeAccentColor.r, this.themeAccentColor.g, this.themeAccentColor.b, alpha);
        }

        private Texture2D SolidTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private Texture2D CircleTexture(int size, Color color)
        {
            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - distance);
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private Texture2D Tex(Color color)
        {
            Texture2D texture = this.SolidTexture(color);
            this.styleTextures.Add(texture);
            return texture;
        }

        private Texture2D Circle(Color color)
        {
            Texture2D texture = this.CircleTexture(32, color);
            this.styleTextures.Add(texture);
            return texture;
        }

        private void DestroyStyleTextures()
        {
            for (int i = 0; i < this.styleTextures.Count; i++)
            {
                if (this.styleTextures[i] != null)
                {
                    UnityEngine.Object.Destroy(this.styleTextures[i]);
                }
            }
            this.styleTextures.Clear();
        }

        private void SetText(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
        }

        private void EnsureStylesCreated()
        {
            if (this.stylesCreated)
            {
                return;
            }
            if (this.whiteTex == null)
            {
                this.whiteTex = this.SolidTexture(Color.white);
                this.circleTex = this.CircleTexture(64, Color.white);
            }
            this.DestroyStyleTextures();

            Color accent = this.AccentAlpha(1f);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color sideBg = new Color(0.032f, 0.036f, 0.048f, 1f);
            Color hoverFill = Color.Lerp(this.surfaceColor, accent, 0.22f);
            Color activeFill = Color.Lerp(this.surfaceColor, accent, 0.5f);

            this.titleStyle = new GUIStyle(GUI.skin.label);
            this.titleStyle.fontSize = 16;
            this.titleStyle.fontStyle = FontStyle.Bold;
            this.titleStyle.alignment = TextAnchor.MiddleLeft;
            this.titleStyle.wordWrap = false;
            this.titleStyle.padding = new RectOffset(0, 0, 0, 0);
            this.SetText(this.titleStyle, Color.white);

            this.textStyle = new GUIStyle(GUI.skin.label);
            this.textStyle.fontSize = 12;
            this.textStyle.alignment = TextAnchor.MiddleLeft;
            this.textStyle.wordWrap = false;
            this.textStyle.padding = new RectOffset(0, 0, 0, 0);
            this.SetText(this.textStyle, new Color(0.86f, 0.89f, 0.95f, 1f));

            this.mutedStyle = new GUIStyle(GUI.skin.label);
            this.mutedStyle.fontSize = 11;
            this.mutedStyle.alignment = TextAnchor.UpperLeft;
            this.mutedStyle.wordWrap = true;
            this.mutedStyle.padding = new RectOffset(0, 0, 0, 0);
            this.SetText(this.mutedStyle, new Color(0.56f, 0.61f, 0.71f, 1f));

            this.sectionStyle = new GUIStyle(GUI.skin.label);
            this.sectionStyle.fontSize = 11;
            this.sectionStyle.fontStyle = FontStyle.Bold;
            this.sectionStyle.alignment = TextAnchor.MiddleLeft;
            this.sectionStyle.wordWrap = false;
            this.sectionStyle.padding = new RectOffset(0, 0, 0, 0);
            this.SetText(this.sectionStyle, accent);

            this.valueStyle = new GUIStyle(this.textStyle);
            this.valueStyle.fontStyle = FontStyle.Bold;
            this.valueStyle.alignment = TextAnchor.MiddleRight;
            this.SetText(this.valueStyle, accent);

            this.buttonStyle = new GUIStyle(GUI.skin.label);
            this.buttonStyle.fontSize = 12;
            this.buttonStyle.fontStyle = FontStyle.Bold;
            this.buttonStyle.alignment = TextAnchor.MiddleCenter;
            this.buttonStyle.wordWrap = false;
            this.buttonStyle.border = new RectOffset(0, 0, 0, 0);
            this.buttonStyle.padding = new RectOffset(8, 8, 2, 2);
            this.buttonStyle.normal.background = this.Tex(this.surfaceColor);
            this.buttonStyle.hover.background = this.Tex(hoverFill);
            this.buttonStyle.active.background = this.Tex(activeFill);
            this.buttonStyle.focused.background = this.Tex(this.surfaceColor);
            this.SetText(this.buttonStyle, Color.white);

            this.segActiveStyle = new GUIStyle(this.buttonStyle);
            this.segActiveStyle.normal.background = this.Tex(activeFill);
            this.segActiveStyle.hover.background = this.Tex(activeFill);
            this.segActiveStyle.active.background = this.Tex(activeFill);
            this.segActiveStyle.focused.background = this.Tex(activeFill);

            this.tabStyle = new GUIStyle(GUI.skin.label);
            this.tabStyle.fontSize = 12;
            this.tabStyle.fontStyle = FontStyle.Bold;
            this.tabStyle.alignment = TextAnchor.MiddleLeft;
            this.tabStyle.wordWrap = false;
            this.tabStyle.padding = new RectOffset(18, 8, 0, 0);
            this.tabStyle.normal.background = this.Tex(clear);
            this.tabStyle.hover.background = this.Tex(Color.Lerp(sideBg, accent, 0.12f));
            this.tabStyle.active.background = this.Tex(Color.Lerp(sideBg, accent, 0.2f));
            this.tabStyle.focused.background = this.Tex(clear);
            this.SetText(this.tabStyle, new Color(0.7f, 0.74f, 0.84f, 1f));

            this.tabActiveStyle = new GUIStyle(this.tabStyle);
            this.tabActiveStyle.normal.background = this.Tex(Color.Lerp(sideBg, accent, 0.18f));
            this.tabActiveStyle.hover.background = this.Tex(Color.Lerp(sideBg, accent, 0.18f));
            this.tabActiveStyle.active.background = this.Tex(Color.Lerp(sideBg, accent, 0.18f));
            this.tabActiveStyle.focused.background = this.Tex(Color.Lerp(sideBg, accent, 0.18f));
            this.SetText(this.tabActiveStyle, Color.white);

            this.rowStyle = new GUIStyle(GUI.skin.label);
            this.rowStyle.normal.background = this.Tex(clear);
            this.rowStyle.hover.background = this.Tex(new Color(1f, 1f, 1f, 0.05f));
            this.rowStyle.active.background = this.Tex(new Color(1f, 1f, 1f, 0.09f));
            this.rowStyle.focused.background = this.Tex(clear);

            this.inputStyle = new GUIStyle(GUI.skin.textField);
            this.inputStyle.fontSize = 12;
            this.inputStyle.alignment = TextAnchor.MiddleLeft;
            this.inputStyle.border = new RectOffset(0, 0, 0, 0);
            this.inputStyle.padding = new RectOffset(10, 10, 4, 4);
            this.inputStyle.normal.background = this.Tex(new Color(0.03f, 0.035f, 0.05f, 1f));
            this.inputStyle.hover.background = this.Tex(new Color(0.04f, 0.045f, 0.065f, 1f));
            this.inputStyle.focused.background = this.Tex(Color.Lerp(new Color(0.03f, 0.035f, 0.05f, 1f), accent, 0.2f));
            this.inputStyle.active.background = this.Tex(Color.Lerp(new Color(0.03f, 0.035f, 0.05f, 1f), accent, 0.2f));
            this.SetText(this.inputStyle, Color.white);

            this.sliderStyle = new GUIStyle(GUI.skin.horizontalSlider);
            this.sliderStyle.fixedHeight = 0f;
            this.sliderStyle.border = new RectOffset(0, 0, 0, 0);
            this.sliderStyle.padding = new RectOffset(0, 0, 0, 0);
            this.sliderStyle.margin = new RectOffset(0, 0, 0, 0);
            this.sliderStyle.normal.background = this.Tex(clear);
            this.sliderStyle.hover.background = this.Tex(clear);
            this.sliderStyle.active.background = this.Tex(clear);
            this.sliderStyle.focused.background = this.Tex(clear);

            this.thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
            this.thumbStyle.fixedWidth = 16f;
            this.thumbStyle.fixedHeight = 16f;
            this.thumbStyle.border = new RectOffset(0, 0, 0, 0);
            this.thumbStyle.padding = new RectOffset(0, 0, 0, 0);
            this.thumbStyle.margin = new RectOffset(0, 0, 0, 0);
            this.thumbStyle.normal.background = this.Circle(accent);
            this.thumbStyle.hover.background = this.Circle(Color.white);
            this.thumbStyle.active.background = this.Circle(accent);
            this.thumbStyle.focused.background = this.Circle(accent);

            this.leaderboardTextStyle = new GUIStyle(GUI.skin.label);
            this.leaderboardTextStyle.fontSize = 13;
            this.leaderboardTextStyle.fontStyle = FontStyle.Bold;
            this.leaderboardTextStyle.alignment = TextAnchor.MiddleLeft;
            this.leaderboardTextStyle.wordWrap = false;
            this.leaderboardTextStyle.padding = new RectOffset(0, 0, 0, 0);
            this.SetText(this.leaderboardTextStyle, Color.white);

            this.teamNameStyle = new GUIStyle(this.leaderboardTextStyle);
            this.teamNameStyle.fontSize = 13;
            this.teamNameStyle.alignment = TextAnchor.MiddleCenter;

            this.teamScoreStyle = new GUIStyle(this.leaderboardTextStyle);
            this.teamScoreStyle.fontSize = 30;
            this.teamScoreStyle.alignment = TextAnchor.MiddleCenter;

            this.targetLabelStyle = new GUIStyle(this.leaderboardTextStyle);
            this.targetLabelStyle.fontSize = 12;

            GUI.skin.verticalScrollbar.normal.background = this.Tex(new Color(1f, 1f, 1f, 0.04f));
            GUI.skin.verticalScrollbar.fixedWidth = 8f;
            GUI.skin.verticalScrollbar.border = new RectOffset(0, 0, 0, 0);
            GUI.skin.verticalScrollbarThumb.normal.background = this.Tex(Color.Lerp(this.surfaceColor, accent, 0.6f));
            GUI.skin.verticalScrollbarThumb.hover.background = this.Tex(Color.Lerp(this.surfaceColor, accent, 0.85f));
            GUI.skin.verticalScrollbarThumb.active.background = this.Tex(accent);
            GUI.skin.verticalScrollbarThumb.fixedWidth = 8f;
            GUI.skin.verticalScrollbarThumb.border = new RectOffset(0, 0, 0, 0);
            GUI.skin.verticalScrollbarUpButton.fixedHeight = 0f;
            GUI.skin.verticalScrollbarUpButton.fixedWidth = 0f;
            GUI.skin.verticalScrollbarDownButton.fixedHeight = 0f;
            GUI.skin.verticalScrollbarDownButton.fixedWidth = 0f;

            this.stylesCreated = true;
        }

        private void Fill(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint || this.whiteTex == null)
            {
                return;
            }
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, this.whiteTex);
            GUI.color = old;
        }

        private void DrawCircle(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint || this.circleTex == null)
            {
                return;
            }
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, this.circleTex, ScaleMode.StretchToFill, true);
            GUI.color = old;
        }

        private void Pill(Rect rect, Color color)
        {
            float h = rect.height;
            this.Fill(new Rect(rect.x + h * 0.5f, rect.y, rect.width - h, h), color);
            this.DrawCircle(new Rect(rect.x, rect.y, h, h), color);
            this.DrawCircle(new Rect(rect.xMax - h, rect.y, h, h), color);
        }

        private void ColoredLabel(Rect rect, string text, GUIStyle style, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.Label(rect, text, style);
            GUI.color = old;
        }

        private void Section(ref float y, string text)
        {
            y += 8f;
            GUI.Label(new Rect(this.ux, y, this.uw, 18f), text.ToUpperInvariant(), this.sectionStyle);
            y += 20f;
            this.Fill(new Rect(this.ux, y, this.uw, 1f), this.AccentAlpha(0.35f));
            y += 8f;
        }

        private void Note(ref float y, string text)
        {
            float h = this.mutedStyle.CalcHeight(new GUIContent(text), this.uw);
            GUI.Label(new Rect(this.ux, y, this.uw, h), text, this.mutedStyle);
            y += h + 6f;
        }

        private bool Btn(ref float y, string label, float height = 28f)
        {
            bool clicked = GUI.Button(new Rect(this.ux, y, this.uw, height), label, this.buttonStyle);
            y += height + 6f;
            return clicked;
        }

        private int BtnRow(ref float y, params string[] labels)
        {
            float gap = 6f;
            float width = (this.uw - gap * (labels.Length - 1)) / labels.Length;
            int result = -1;
            for (int i = 0; i < labels.Length; i++)
            {
                if (GUI.Button(new Rect(this.ux + i * (width + gap), y, width, 28f), labels[i], this.buttonStyle))
                {
                    result = i;
                }
            }
            y += 34f;
            return result;
        }

        private int Segmented(ref float y, string[] labels, int selected, int perRow)
        {
            float gap = 4f;
            float width = (this.uw - gap * (perRow - 1)) / perRow;
            int rows = (labels.Length + perRow - 1) / perRow;
            int result = selected;
            for (int i = 0; i < labels.Length; i++)
            {
                int row = i / perRow;
                int col = i % perRow;
                Rect rect = new Rect(this.ux + col * (width + gap), y + row * 32f, width, 28f);
                if (GUI.Button(rect, labels[i], i == selected ? this.segActiveStyle : this.buttonStyle))
                {
                    result = i;
                }
            }
            y += rows * 32f + 4f;
            return result;
        }

        private bool Switch(ref float y, string label, bool value)
        {
            Rect row = new Rect(this.ux, y, this.uw, 28f);
            bool clicked = GUI.Button(row, GUIContent.none, this.rowStyle);
            GUI.Label(new Rect(row.x + 8f, row.y, row.width - 64f, row.height), label, this.textStyle);
            Rect track = new Rect(row.xMax - 48f, row.y + 5f, 38f, 18f);
            this.Pill(track, value ? this.AccentAlpha(1f) : this.offColor);
            float knobX = value ? track.xMax - 16f : track.x + 2f;
            this.DrawCircle(new Rect(knobX, track.y + 2f, 14f, 14f), Color.white);
            y += 30f;
            return clicked ? !value : value;
        }

        private void SwitchCfg(ref float y, string label, ConfigEntry<bool> config)
        {
            config.Value = this.Switch(ref y, label, config.Value);
        }

        private float Slide(ref float y, string label, float value, float min, float max, string format)
        {
            GUI.Label(new Rect(this.ux + 8f, y, this.uw - 90f, 18f), label, this.textStyle);
            GUI.Label(new Rect(this.ux + this.uw - 90f, y, 82f, 18f), value.ToString(format, CultureInfo.InvariantCulture), this.valueStyle);
            y += 20f;
            Rect rect = new Rect(this.ux + 8f, y, this.uw - 16f, 16f);
            float t = Mathf.InverseLerp(min, max, value);
            this.Fill(new Rect(rect.x, rect.y + 6f, rect.width, 4f), this.offColor);
            this.Fill(new Rect(rect.x, rect.y + 6f, 8f + t * (rect.width - 16f), 4f), this.AccentAlpha(0.85f));
            float result = GUI.HorizontalSlider(rect, value, min, max, this.sliderStyle, this.thumbStyle);
            y += 24f;
            return result;
        }

        private void SlideCfg(ref float y, string label, ConfigEntry<float> config, float min, float max, string format = "F2")
        {
            config.Value = this.Slide(ref y, label, config.Value, min, max, format);
        }

        private string Field(ref float y, string label, string value, string name, int maxLength)
        {
            GUI.Label(new Rect(this.ux + 2f, y, this.uw, 16f), label, this.mutedStyle);
            y += 18f;
            Rect rect = new Rect(this.ux, y, this.uw, 28f);
            GUI.SetNextControlName("tf_" + name);
            string result = maxLength > 0 ? GUI.TextField(rect, value ?? string.Empty, maxLength, this.inputStyle) : GUI.TextField(rect, value ?? string.Empty, this.inputStyle);
            y += 34f;
            return result;
        }

        private void RenderWindow()
        {
            Event e = Event.current;
            Rect titleBar = new Rect(this.windowRect.x, this.windowRect.y, this.windowRect.width - 50f, TitleHeight);
            if (e.type == EventType.MouseDown && e.button == 0 && titleBar.Contains(e.mousePosition))
            {
                this.isDraggingWindow = true;
                this.dragOffset = e.mousePosition - this.windowRect.position;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                this.isDraggingWindow = false;
            }
            else if (e.type == EventType.MouseDrag && this.isDraggingWindow)
            {
                this.windowRect.position = e.mousePosition - this.dragOffset;
                e.Use();
            }
            this.KeepPanelOnScreen(ref this.windowRect);

            float wx = this.windowRect.x;
            float wy = this.windowRect.y;
            float ww = this.windowRect.width;
            float wh = this.windowRect.height;
            Color accent = this.AccentAlpha(1f);

            this.Fill(this.windowRect, this.AccentAlpha(0.55f));
            this.Fill(new Rect(wx + 1f, wy + 1f, ww - 2f, wh - 2f), new Color(0.05f, 0.055f, 0.07f, 0.97f));
            this.Fill(new Rect(wx + 1f, wy + 1f + TitleHeight, SidebarWidth, wh - 2f - TitleHeight), new Color(0.032f, 0.036f, 0.048f, 1f));
            this.Fill(new Rect(wx + 1f, wy + 1f, ww - 2f, TitleHeight), new Color(0.07f, 0.08f, 0.1f, 1f));
            this.Fill(new Rect(wx + 1f, wy + 1f + TitleHeight, ww - 2f, 2f), accent);

            this.ColoredLabel(new Rect(wx + 16f, wy + 1f, 60f, TitleHeight), "LIAM", this.titleStyle, accent);
            GUI.Label(new Rect(wx + 68f, wy + 1f, 160f, TitleHeight), "CASTING", this.titleStyle);
            this.ColoredLabel(new Rect(wx + ww - 190f, wy + 1f, 130f, TitleHeight), this.isCameraActive ? "LIVE" : "OFFLINE", this.valueStyle, this.isCameraActive ? accent : new Color(0.5f, 0.54f, 0.62f, 1f));
            if (GUI.Button(new Rect(wx + ww - 42f, wy + 8f, 30f, 26f), "X", this.buttonStyle))
            {
                this.isMenuVisible = false;
            }

            for (int i = 0; i < tabNames.Length; i++)
            {
                Rect tabRect = new Rect(wx + 1f, wy + TitleHeight + 12f + i * 36f, SidebarWidth, 34f);
                bool active = i == this.activeTab;
                if (GUI.Button(tabRect, tabNames[i], active ? this.tabActiveStyle : this.tabStyle))
                {
                    this.activeTab = i;
                    this.contentScroll = Vector2.zero;
                    if (i == 9)
                    {
                        this.RefreshPresetList();
                    }
                }
                if (active)
                {
                    this.Fill(new Rect(tabRect.x, tabRect.y + 5f, 3f, tabRect.height - 10f), accent);
                }
            }
            GUI.Label(new Rect(wx + 16f, wy + wh - 40f, SidebarWidth - 20f, 16f), "Liam Casting Mod", this.mutedStyle);
            GUI.Label(new Rect(wx + 16f, wy + wh - 24f, SidebarWidth - 20f, 16f), "v2.3.0  |  H to toggle", this.mutedStyle);

            Rect area = new Rect(wx + SidebarWidth + 12f, wy + TitleHeight + 14f, ww - SidebarWidth - 24f, wh - TitleHeight - 24f);
            float viewWidth = area.width - 14f;
            this.contentScroll = GUI.BeginScrollView(area, this.contentScroll, new Rect(0f, 0f, viewWidth, Mathf.Max(this.contentHeight, area.height)), false, false);
            this.ux = 2f;
            this.uw = viewWidth - 4f;
            float y = 0f;
            switch (this.activeTab)
            {
                case 0: this.DrawCastTab(ref y); break;
                case 1: this.DrawCameraTab(ref y); break;
                case 2: this.DrawFlightTab(ref y); break;
                case 3: this.DrawRoomTab(ref y); break;
                case 4: this.DrawNameTagTab(ref y); break;
                case 5: this.DrawScoreboardTab(ref y); break;
                case 6: this.DrawShadersTab(ref y); break;
                case 7: this.DrawWorldTab(ref y); break;
                case 8: this.DrawThemeTab(ref y); break;
                case 9: this.DrawPresetsTab(ref y); break;
            }
            this.contentHeight = y + 12f;
            GUI.EndScrollView();

            if (e.type == EventType.MouseDown && this.windowRect.Contains(e.mousePosition))
            {
                GUI.FocusControl(string.Empty);
            }
        }

        private void KeepPanelOnScreen(ref Rect panelRect)
        {
            panelRect.x = Mathf.Clamp(panelRect.x, 0f, Mathf.Max(0f, (float)Screen.width - panelRect.width));
            panelRect.y = Mathf.Clamp(panelRect.y, 0f, Mathf.Max(0f, (float)Screen.height - panelRect.height));
        }

        private void DrawCastTab(ref float y)
        {
            this.Section(ref y, "Status");
            Rect card = new Rect(this.ux, y, this.uw, 54f);
            this.Fill(card, this.surfaceColor);
            this.Fill(new Rect(card.x, card.y, 4f, card.height), this.isCameraActive ? this.AccentAlpha(1f) : this.offColor);
            GUI.Label(new Rect(card.x + 16f, card.y + 8f, card.width - 24f, 18f), this.isCameraActive ? "CASTING ACTIVE" : "CASTING OFFLINE", this.sectionStyle);
            GUI.Label(new Rect(card.x + 16f, card.y + 30f, card.width - 24f, 18f), this.isCameraActive ? "Live spectator camera is running." : "Enable casting to start the spectator camera.", this.mutedStyle);
            y += 64f;

            if (this.Btn(ref y, this.isCameraActive ? "DISABLE CASTING" : "ENABLE CASTING", 34f))
            {
                this.ToggleCameraSystem();
            }
            if (!this.isCameraActive)
            {
                this.Note(ref y, "Press H any time to show or hide this menu.");
                return;
            }

            this.Section(ref y, "Viewing");
            GUI.Label(new Rect(this.ux, y, this.uw, 26f), this.targetPlayerName, this.titleStyle);
            y += 32f;

            this.Section(ref y, "Camera mode");
            int mode = Mathf.Clamp(this.cameraModeConfig.Value, 0, modeLabels.Length - 1);
            int newMode = this.Segmented(ref y, modeLabels, mode, 4);
            if (newMode != mode)
            {
                this.cameraModeConfig.Value = newMode;
                mode = newMode;
            }
            this.Note(ref y, modeInfo[mode]);
            this.Note(ref y, "Press M to cycle modes.");

            this.Section(ref y, "Target");
            if (this.Btn(ref y, "[1]   YOU"))
            {
                this.SelectSelf();
            }
            int slot = 0;
            for (int i = 0; i < this.cachedVRRigs.Count && slot < 9; i++)
            {
                VRRig rig = this.cachedVRRigs[i];
                if (rig == null || this.IsOfflineRig(rig))
                {
                    continue;
                }
                if (this.Btn(ref y, "[" + this.SlotKeyLabel(slot) + "]   " + this.GetRigName(rig).ToUpperInvariant()))
                {
                    this.SelectTarget(rig);
                }
                slot++;
            }
            if (slot == 0)
            {
                this.Note(ref y, "No other players found. Join a room to cast someone.");
            }

            this.Section(ref y, "Auto referee");
            this.SwitchCfg(ref y, "Auto-cycle player targets", this.autoRefEnabledConfig);
            if (this.autoRefEnabledConfig.Value)
            {
                this.SlideCfg(ref y, "Seconds per target", this.autoRefIntervalConfig, 3f, 60f, "F0");
                this.Note(ref y, "Automatically switches the spectator camera between players while casting.");
            }

            this.Section(ref y, "Auto observer");
            this.SwitchCfg(ref y, "Forest tour camera", this.autoObserverEnabledConfig);
            if (this.autoObserverEnabledConfig.Value)
            {
                this.SlideCfg(ref y, "Tour speed", this.autoObserverSpeedConfig, 0.03f, 0.5f);
                this.SlideCfg(ref y, "Route scale", this.autoObserverDistanceConfig, 4f, 30f, "F1");
            }
        }

        private void DrawCameraTab(ref float y)
        {
            int mode = Mathf.Clamp(this.cameraModeConfig.Value, 0, modeLabels.Length - 1);
            this.Section(ref y, "Mode");
            int newMode = this.Segmented(ref y, modeLabels, mode, 4);
            if (newMode != mode)
            {
                this.cameraModeConfig.Value = newMode;
                mode = newMode;
            }
            this.Note(ref y, modeInfo[mode]);

            if (mode == 2)
            {
                this.Section(ref y, "Eye view");
                if (this.Btn(ref y, "APPLY BEST EYE SETTINGS", 32f))
                {
                    this.ApplyBestEyeSettings();
                }
                this.Note(ref y, "Best preset: 75 degree FOV (about 100 degrees across a 16:9 screen), tiny near clip so the head mesh never blocks the view, camera moved just in front of the eyes, light smoothing to hide jitter, and full render scale for a sharp image.");
                this.SlideCfg(ref y, "Field of view", this.eyeFovConfig, 40f, 120f, "F0");
                this.SlideCfg(ref y, "Near clip", this.eyeNearClipConfig, 0.005f, 0.3f, "F3");
                this.SlideCfg(ref y, "Forward from head", this.eyeForwardConfig, 0f, 0.4f);
                this.SlideCfg(ref y, "Up from head", this.eyeUpConfig, -0.2f, 0.3f);
                this.SlideCfg(ref y, "Position smooth", this.eyePositionSmoothingConfig, 0f, 1f);
                this.SlideCfg(ref y, "Rotation smooth", this.eyeRotationSmoothingConfig, 0f, 1f);
                this.SwitchCfg(ref y, "Keep horizon level", this.eyeStabilizeRollConfig);
            }
            else if (mode == 3)
            {
                this.Section(ref y, "Front camera");
                this.SlideCfg(ref y, "Distance in front", this.frontDistanceConfig, 0.5f, 5f);
                this.SlideCfg(ref y, "Height", this.frontHeightConfig, -1f, 1.5f);
                this.SlideCfg(ref y, "Side offset", this.frontSideConfig, -1.5f, 1.5f);
                this.SlideCfg(ref y, "Look at height", this.frontLookHeightConfig, -0.5f, 1f);
                this.SlideCfg(ref y, "Field of view", this.fovConfig, 40f, 120f, "F0");
                this.SlideCfg(ref y, "Near clip", this.nearClipConfig, 0.01f, 1f);
                this.SwitchCfg(ref y, "Follow head direction", this.frontFollowHeadConfig);
            }
            else
            {
                this.Section(ref y, mode == 0 ? "Head camera" : "Body camera");
                this.SlideCfg(ref y, "Distance behind", this.headDistanceConfig, -1f, 5f);
                this.SlideCfg(ref y, "Y offset", this.yOffsetConfig, -1f, 1f);
                this.SlideCfg(ref y, "X offset", this.xOffsetConfig, -1f, 1f);
                this.SlideCfg(ref y, "Field of view", this.fovConfig, 40f, 120f, "F0");
                this.SlideCfg(ref y, "Near clip", this.nearClipConfig, 0.01f, 1f);
                if (mode == 0)
                {
                    this.SwitchCfg(ref y, "Follow head tilt", this.trackHeadRollConfig);
                }
            }

            if (mode != 2)
            {
                this.Section(ref y, "Smoothing");
                this.SwitchCfg(ref y, "Camera smoothing", this.smoothingEnabledConfig);
                if (this.smoothingEnabledConfig.Value)
                {
                    this.SlideCfg(ref y, "Position smooth", this.positionSmoothingConfig, 0f, 1f);
                    this.SlideCfg(ref y, "Rotation smooth", this.rotationSmoothingConfig, 0f, 1f);
                }
            }

            this.Section(ref y, "Quality");
            this.SlideCfg(ref y, "Render scale", this.cameraRenderScaleConfig, 0.5f, 1f);
            this.SwitchCfg(ref y, "Performance optimization", this.performanceOptimizationConfig);

            this.Section(ref y, "Rig lerp");
            this.SwitchCfg(ref y, "Rig lerping", this.rigLerpEnabledConfig);
            if (this.rigLerpEnabledConfig.Value)
            {
                this.SlideCfg(ref y, "Body lerp", this.rigBodyLerpConfig, 0.01f, 0.99f);
                this.SlideCfg(ref y, "Finger lerp", this.rigFingerLerpConfig, 0.01f, 0.99f);
            }
        }

        private void ApplyBestEyeSettings()
        {
            this.eyeFovConfig.Value = 75f;
            this.eyeNearClipConfig.Value = 0.03f;
            this.eyeForwardConfig.Value = 0.12f;
            this.eyeUpConfig.Value = 0.03f;
            this.eyePositionSmoothingConfig.Value = 0.5f;
            this.eyeRotationSmoothingConfig.Value = 0.85f;
            this.eyeStabilizeRollConfig.Value = false;
            this.cameraRenderScaleConfig.Value = 1f;
        }

        private void DrawFlightTab(ref float y)
        {
            this.Section(ref y, "WASD flight");
            this.SwitchCfg(ref y, "Flight", this.flyEnabledConfig);
            if (this.flyEnabledConfig.Value)
            {
                this.SwitchCfg(ref y, "Stationary when idle", this.stationaryFlyConfig);
                this.SlideCfg(ref y, "Speed", this.flySpeedConfig, 1f, 50f, "F1");
            }
            this.Section(ref y, "Controls");
            this.Note(ref y, "W / A / S / D  move\nSpace  up\nLeft Ctrl  down\nLeft Shift  2x speed");
        }

        private void DrawRoomTab(ref float y)
        {
            this.Section(ref y, "Room");
            this.roomCodeInput = this.Field(ref y, "Room code", this.roomCodeInput, "room", 12);
            int clicked = this.BtnRow(ref y, "JOIN ROOM", "DISCONNECT");
            if (clicked == 0)
            {
                this.JoinRoom();
            }
            else if (clicked == 1)
            {
                this.DisconnectFromRoom();
            }
            string status = "Not in a room.";
            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
            {
                status = "In room: " + PhotonNetwork.CurrentRoom.Name;
            }
            this.Note(ref y, status);
        }

        private void DrawNameTagTab(ref float y)
        {
            this.Section(ref y, "Name tags");
            this.SwitchCfg(ref y, "Name tags", this.nameTagsEnabledConfig);
            this.SwitchCfg(ref y, "FPS readout (always N/A)", this.nameTagShowHzConfig);
            this.SwitchCfg(ref y, "Show distance", this.nameTagShowDistanceConfig);
            this.SwitchCfg(ref y, "Show movement speed", this.nameTagShowSpeedConfig);
            this.SwitchCfg(ref y, "Show tagged status", this.nameTagShowTaggedConfig);
            this.SwitchCfg(ref y, "Use player color", this.nameTagShowPlayerColorConfig);
            this.SwitchCfg(ref y, "Show background plate", this.nameTagShowBackgroundConfig);
            this.SlideCfg(ref y, "Max distance", this.nameTagMaxDistanceConfig, 5f, 150f);
            string prefix = this.Field(ref y, "Name prefix", this.nameTagPrefixConfig.Value, "tagprefix", 12);
            if (prefix != this.nameTagPrefixConfig.Value) this.nameTagPrefixConfig.Value = prefix;
            string suffix = this.Field(ref y, "Name suffix", this.nameTagSuffixConfig.Value, "tagsuffix", 12);
            if (suffix != this.nameTagSuffixConfig.Value) this.nameTagSuffixConfig.Value = suffix;
            this.Section(ref y, "Font style");
            int font = Mathf.Clamp(this.nameTagFontIndexConfig.Value, 0, nameTagFontStyleNames.Length - 1);
            this.nameTagFontIndexConfig.Value = this.Segmented(ref y, nameTagFontStyleNames, font, 4);
            this.Section(ref y, "Facing");
            int facing = Mathf.Clamp(this.nameTagFacingModeConfig.Value, 0, nameTagFacingModeNames.Length - 1);
            this.nameTagFacingModeConfig.Value = this.Segmented(ref y, nameTagFacingModeNames, facing, 4);
            this.Section(ref y, "Placement");
            this.SlideCfg(ref y, "Tag height", this.nameTagHeightConfig, 0f, 2f);
            this.SlideCfg(ref y, "Tag size", this.nameTagSizeConfig, 0.4f, 2.5f);
        }

        private void DrawScoreboardTab(ref float y)
        {
            this.Section(ref y, "Scoreboard");
            this.SwitchCfg(ref y, "Scoreboard", this.scoreboardEnabledConfig);

            string left = this.Field(ref y, "Left team", this.leftTeamNameConfig.Value, "left", 16);
            if (!string.Equals(left, this.leftTeamNameConfig.Value, StringComparison.Ordinal))
            {
                this.leftTeamNameConfig.Value = left;
            }
            string right = this.Field(ref y, "Right team", this.rightTeamNameConfig.Value, "right", 16);
            if (!string.Equals(right, this.rightTeamNameConfig.Value, StringComparison.Ordinal))
            {
                this.rightTeamNameConfig.Value = right;
            }

            this.Section(ref y, "Score  " + this.leftTeamScore + " - " + this.rightTeamScore);
            int c = this.BtnRow(ref y, "LEFT -", "LEFT +");
            if (c == 0) this.leftTeamScore = Mathf.Max(0, this.leftTeamScore - 1);
            if (c == 1) this.leftTeamScore++;
            c = this.BtnRow(ref y, "RIGHT -", "RIGHT +");
            if (c == 0) this.rightTeamScore = Mathf.Max(0, this.rightTeamScore - 1);
            if (c == 1) this.rightTeamScore++;
            if (this.Btn(ref y, "RESET SCORES"))
            {
                this.leftTeamScore = 0;
                this.rightTeamScore = 0;
            }

            this.Section(ref y, "Timers");
            c = this.BtnRow(ref y, this.scoreboardTimerRunning ? "PAUSE TIMER 1" : "START TIMER 1", "CLEAR TIMER 1");
            if (c == 0)
            {
                if (this.scoreboardTimerRunning) this.PauseScoreboardTimer(); else this.StartScoreboardTimer();
            }
            if (c == 1) this.ResetScoreboardTimer();
            c = this.BtnRow(ref y, "SAVE TO TIMER 2", "CLEAR TIMER 2");
            if (c == 0) this.SetScoreboardTimeToBeat();
            if (c == 1) this.ClearScoreboardTimes();
            if (this.Btn(ref y, string.Format("TIMER STARTS AT: {0:F0}s", this.GetScoreboardCountdownSeconds())))
            {
                this.scoreboardCountdownIndexConfig.Value = (this.GetScoreboardCountdownIndex() + 1) % scoreboardCountdownValues.Length;
                this.ResetScoreboardTimer();
            }
            this.SwitchCfg(ref y, "Auto start timer", this.scoreboardTimerAutoStartConfig);
            this.Note(ref y, this.scoreboardTimeToBeatSeconds > 0f ? "Timer 2: " + this.FormatScoreboardDuration(this.scoreboardTimeToBeatSeconds) : "Timer 2: not set");
            this.Note(ref y, "Space  start / stop\nDelete  clear timer 1\nShift + Space  save timer 2\nCtrl + Delete  clear timer 2\n=  left +    -  left -\n]  right +    [  right -");

            this.Section(ref y, "Position");
            this.SlideCfg(ref y, "Position X", this.scoreboardPositionXConfig, -0.45f, 0.45f);
            this.SlideCfg(ref y, "Position Y", this.scoreboardPositionYConfig, 0f, 0.85f);
            this.SlideCfg(ref y, "Board scale", this.scoreboardScaleConfig, 0.6f, 1.5f);
            this.SlideCfg(ref y, "Background opacity", this.scoreboardOpacityConfig, 0.2f, 1f);
            if (this.Btn(ref y, "CENTER AT TOP"))
            {
                this.scoreboardPositionXConfig.Value = 0f;
                this.scoreboardPositionYConfig.Value = 0.02f;
            }
        }

        private string GetShaderNameForPreset(int index)
        {
            switch (Mathf.Clamp(index, 0, shaderEffectPresetNames.Length - 1))
            {
                case 1: return "Hidden/TUPshaders/Ambient";
                case 2: return "Hidden/TUPshaders/Blit";
                case 3: return "Hidden/TUPshaders/Bloom";
                case 4: return "Hidden/TUPshaders/CAS";
                case 5: return "Hidden/TUPshaders/ColorGrading";
                case 6: return "Hidden/TUPshaders/Fog";
                case 7: return "Hidden/TUPshaders/GodRays";
                case 8: return "Hidden/TUPshaders/Lens";
                case 9: return "Hidden/TUPshaders/Sky";
                case 10: return "Hidden/TUPshaders/SSAO";
                case 11: return "Hidden/TUPshaders/SSR";
                default: return string.Empty;
            }
        }

        private void DrawShadersTab(ref float y)
        {
            this.Section(ref y, "Casting shaders");
            this.SwitchCfg(ref y, "Shader effects", this.shaderEffectsEnabledConfig);
            int preset = Mathf.Clamp(this.shaderEffectPresetIndexConfig.Value, 0, shaderEffectPresetNames.Length - 1);
            int newPreset = this.Segmented(ref y, shaderEffectPresetNames, preset, 3);
            this.Note(ref y, "Uses compiled Hidden/TUPshaders shaders when available.");
            if (newPreset != preset)
            {
                this.ApplyShaderEffectPreset(newPreset);
            }
            this.Section(ref y, "Adjust");
            this.SlideCfg(ref y, "Exposure", this.shaderExposureConfig, 0.25f, 2f);
            this.SlideCfg(ref y, "Tint strength", this.shaderTintStrengthConfig, 0f, 1f);
            this.SlideCfg(ref y, "Vignette", this.shaderVignetteConfig, 0f, 1f);
            this.SlideCfg(ref y, "Scanlines", this.shaderScanlineConfig, 0f, 1f);
            this.SlideCfg(ref y, "Letterbox", this.shaderLetterboxConfig, 0f, 1f);
            this.shaderTintHexInput = this.Field(ref y, "Tint hex (#RRGGBB)", this.shaderTintHexInput, "tint", 9);
            if (this.Btn(ref y, "APPLY TINT"))
            {
                this.ApplyShaderTintHex(this.shaderTintHexInput, true);
            }
            this.Section(ref y, "Preset file");
            this.shaderPresetPathInput = this.Field(ref y, "Compatible .ini preset path", this.shaderPresetPathInput, "ini", 0);
            if (this.Btn(ref y, "LOAD .INI"))
            {
                this.LoadShaderIniPreset();
            }
            this.Note(ref y, this.shaderPresetStatus);
        }

        private void DrawWorldTab(ref float y)
        {
            this.Section(ref y, "Time of day");
            GUI.Label(new Rect(this.ux, y, this.uw, 26f), timeNames[this.timeIndex], this.titleStyle);
            y += 30f;
            int c = this.BtnRow(ref y, "PREVIOUS", "NEXT");
            if (c == 0) this.ChangeTime(-1);
            if (c == 1) this.ChangeTime(1);

            this.Section(ref y, "Map loader");
            for (int i = 0; i < mapNames.Length; i += 2)
            {
                string a = "LOAD " + mapNames[i].ToUpperInvariant();
                int result;
                if (i + 1 < mapNames.Length)
                {
                    result = this.BtnRow(ref y, a, "LOAD " + mapNames[i + 1].ToUpperInvariant());
                }
                else
                {
                    result = this.BtnRow(ref y, a);
                }
                if (result >= 0)
                {
                    this.TryLoadMap(mapNames[i + result]);
                }
            }
        }

        private void ChangeTime(int direction)
        {
            this.timeIndex = (this.timeIndex + direction + timeNames.Length) % timeNames.Length;
            try
            {
                BetterDayNightManager.instance.SetTimeOfDay(this.timeIndex);
            }
            catch
            {
            }
        }

        private void TryLoadMap(string map)
        {
            try
            {
                LiamCastingPlugin.MapLoader(map);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to load " + map + ": " + ex.Message);
            }
        }

        public static void MapLoader(string map)
        {
            int index = Array.FindIndex(mapNames, delegate(string n) { return string.Equals(n, map, StringComparison.OrdinalIgnoreCase); });
            if (index < 0)
            {
                return;
            }
            string path = "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/Regional Transition/" + mapTriggers[index];
            GameObject obj = GameObject.Find(path);
            if (obj == null)
            {
                Debug.LogError("Map trigger not found: " + path);
                return;
            }
            GorillaSetZoneTrigger trigger = obj.GetComponent<GorillaSetZoneTrigger>();
            if (trigger != null)
            {
                trigger.OnBoxTriggered();
            }
        }

        private void DrawThemeTab(ref float y)
        {
            this.Section(ref y, "Accent themes");
            for (int i = 0; i < themePresets.Length; i++)
            {
                Rect row = new Rect(this.ux, y, this.uw, 28f);
                bool selected = this.selectedThemeIndexConfig.Value == i;
                if (GUI.Button(row, "   " + themePresets[i].name, selected ? this.segActiveStyle : this.buttonStyle))
                {
                    this.ApplyTheme(i);
                }
                this.DrawCircle(new Rect(row.x + 8f, row.y + 7f, 14f, 14f), themePresets[i].accent);
                y += 32f;
            }

            this.Section(ref y, "Custom accent");
            this.uiColorRed = this.Slide(ref y, "Red", this.uiColorRed, 0f, 1f, "F2");
            this.uiColorGreen = this.Slide(ref y, "Green", this.uiColorGreen, 0f, 1f, "F2");
            this.uiColorBlue = this.Slide(ref y, "Blue", this.uiColorBlue, 0f, 1f, "F2");
            this.uiHexColorInput = this.Field(ref y, "Hex color (#RRGGBB)", this.uiHexColorInput, "hex", 9);
            int c = this.BtnRow(ref y, "APPLY HEX", "APPLY RGB");
            if (c == 0) this.ApplyHexColor();
            if (c == 1) this.ApplyCustomColors();
            if (this.Btn(ref y, "RESET TO DEFAULT"))
            {
                this.ApplyTheme(0);
            }
        }

        private void ApplyCustomColors()
        {
            this.themeAccentColor = new Color(this.uiColorRed, this.uiColorGreen, this.uiColorBlue, 1f);
            this.uiHexColorInput = "#" + ColorUtility.ToHtmlStringRGB(this.themeAccentColor);
            this.stylesCreated = false;
        }

        private void ApplyHexColor()
        {
            string hex = (this.uiHexColorInput ?? string.Empty).Trim();
            if (!hex.StartsWith("#"))
            {
                hex = "#" + hex;
            }
            Color color;
            if (!ColorUtility.TryParseHtmlString(hex, out color))
            {
                return;
            }
            this.uiColorRed = color.r;
            this.uiColorGreen = color.g;
            this.uiColorBlue = color.b;
            this.themeAccentColor = new Color(color.r, color.g, color.b, 1f);
            this.uiHexColorInput = "#" + ColorUtility.ToHtmlStringRGB(this.themeAccentColor);
            this.stylesCreated = false;
        }

        private void ApplyTheme(int index, bool unused = true)
        {
            if (index < 0 || index >= themePresets.Length)
            {
                return;
            }
            ThemePreset preset = themePresets[index];
            this.uiColorRed = preset.r;
            this.uiColorGreen = preset.g;
            this.uiColorBlue = preset.b;
            this.themeAccentColor = preset.accent;
            this.uiHexColorInput = "#" + ColorUtility.ToHtmlStringRGB(this.themeAccentColor);
            this.selectedThemeIndexConfig.Value = index;
            this.stylesCreated = false;
        }

        private void DrawPresetsTab(ref float y)
        {
            this.Section(ref y, "Save manager");
            this.presetNameInput = this.Field(ref y, "Preset name", this.presetNameInput, "preset", 32);
            int c = this.BtnRow(ref y, "SAVE PRESET", "LOAD PRESET");
            if (c == 0) this.SavePreset();
            if (c == 1) this.LoadPreset();
            this.Note(ref y, this.presetStatus);
            this.Section(ref y, "Saved presets");
            if (this.presetFiles.Length == 0)
            {
                this.Note(ref y, "No presets saved yet.");
            }
            for (int i = 0; i < this.presetFiles.Length && i < 16; i++)
            {
                if (this.Btn(ref y, this.presetFiles[i]))
                {
                    this.presetNameInput = this.presetFiles[i];
                }
            }
        }

        private void InitializePresetsFolder()
        {
            if (this.presetsFolderInitialized)
            {
                return;
            }
            try
            {
                this.presetsFolder = Path.Combine(Paths.ConfigPath, "LiamCasting", "Presets");
                Directory.CreateDirectory(this.presetsFolder);
                this.presetsFolderInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to create presets folder: " + ex.Message);
            }
        }

        private void RefreshPresetList()
        {
            try
            {
                if (!this.presetsFolderInitialized)
                {
                    return;
                }
                this.presetFiles = Directory.GetFiles(this.presetsFolder, "*.preset").Select(delegate(string f) { return Path.GetFileNameWithoutExtension(f); }).OrderBy(delegate(string f) { return f; }).ToArray();
            }
            catch
            {
                this.presetFiles = new string[0];
            }
        }

        private string PresetKey(ConfigEntryBase entry)
        {
            return entry.Definition.Section + "." + entry.Definition.Key;
        }

        private void SavePreset()
        {
            string name = string.Concat((this.presetNameInput ?? string.Empty).Trim().Split(Path.GetInvalidFileNameChars()));
            if (string.IsNullOrEmpty(name) || !this.presetsFolderInitialized)
            {
                this.presetStatus = "Type a preset name first.";
                return;
            }
            try
            {
                List<string> lines = new List<string>();
                for (int i = 0; i < this.presetEntries.Count; i++)
                {
                    lines.Add(this.PresetKey(this.presetEntries[i]) + "=" + this.presetEntries[i].GetSerializedValue());
                }
                File.WriteAllLines(Path.Combine(this.presetsFolder, name + ".preset"), lines.ToArray());
                this.presetStatus = "Saved preset \"" + name + "\".";
                this.RefreshPresetList();
            }
            catch (Exception ex)
            {
                this.presetStatus = "Could not save: " + ex.Message;
                Debug.LogError("Failed to save preset: " + ex.Message);
            }
        }

        private void LoadPreset()
        {
            string name = string.Concat((this.presetNameInput ?? string.Empty).Trim().Split(Path.GetInvalidFileNameChars()));
            if (string.IsNullOrEmpty(name) || !this.presetsFolderInitialized)
            {
                this.presetStatus = "Type a preset name first.";
                return;
            }
            try
            {
                string path = Path.Combine(this.presetsFolder, name + ".preset");
                if (!File.Exists(path))
                {
                    this.presetStatus = "Preset \"" + name + "\" was not found.";
                    return;
                }
                Dictionary<string, ConfigEntryBase> lookup = new Dictionary<string, ConfigEntryBase>();
                for (int i = 0; i < this.presetEntries.Count; i++)
                {
                    lookup[this.PresetKey(this.presetEntries[i])] = this.presetEntries[i];
                }
                foreach (string line in File.ReadAllLines(path))
                {
                    int separator = line.IndexOf('=');
                    if (separator <= 0)
                    {
                        continue;
                    }
                    string key = line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1);
                    ConfigEntryBase entry;
                    if (!lookup.TryGetValue(key, out entry))
                    {
                        continue;
                    }
                    try
                    {
                        entry.SetSerializedValue(value);
                    }
                    catch
                    {
                    }
                }
                this.ApplyTheme(Mathf.Clamp(this.selectedThemeIndexConfig.Value, 0, themePresets.Length - 1));
                this.ApplyShaderTintHex(this.shaderTintHexConfig.Value, false);
                this.UnRenderNameTags();
                this.presetStatus = "Loaded preset \"" + name + "\".";
            }
            catch (Exception ex)
            {
                this.presetStatus = "Could not load: " + ex.Message;
                Debug.LogError("Failed to load preset: " + ex.Message);
            }
        }

        private void DrawCastingFeed()
        {
            GUI.DrawTexture(new Rect(0f, 0f, (float)Screen.width, (float)Screen.height), this.cameraRenderTexture, ScaleMode.StretchToFill, false);
        }

        private void EnsureShaderEffectTextures()
        {
            if (this.shaderOverlayTexture == null)
            {
                this.shaderOverlayTexture = this.SolidTexture(Color.white);
            }
            if (this.shaderScanlineTexture == null)
            {
                this.shaderScanlineTexture = new Texture2D(2, 2);
                this.shaderScanlineTexture.wrapMode = TextureWrapMode.Repeat;
                this.shaderScanlineTexture.filterMode = FilterMode.Point;
                this.shaderScanlineTexture.SetPixels(new Color[]
                {
                    new Color(1f, 1f, 1f, 0f), Color.white,
                    new Color(1f, 1f, 1f, 0f), Color.white
                });
                this.shaderScanlineTexture.Apply();
            }
            if (this.shaderVignetteTexture != null)
            {
                return;
            }
            const int size = 128;
            this.shaderVignetteTexture = new Texture2D(size, size);
            this.shaderVignetteTexture.filterMode = FilterMode.Bilinear;
            this.shaderVignetteTexture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float horizontal = ((float)x / (float)(size - 1) - 0.5f) * 2f;
                    float vertical = ((float)y / (float)(size - 1) - 0.5f) * 2f;
                    float edge = Mathf.Clamp01((Mathf.Max(Mathf.Abs(horizontal), Mathf.Abs(vertical)) - 0.35f) / 0.65f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, edge * edge);
                }
            }
            this.shaderVignetteTexture.SetPixels(pixels);
            this.shaderVignetteTexture.Apply();
        }

        private void ReleaseShaderEffectTextures()
        {
            if (this.shaderOverlayTexture != null)
            {
                UnityEngine.Object.Destroy(this.shaderOverlayTexture);
                this.shaderOverlayTexture = null;
            }
            if (this.shaderScanlineTexture != null)
            {
                UnityEngine.Object.Destroy(this.shaderScanlineTexture);
                this.shaderScanlineTexture = null;
            }
            if (this.shaderVignetteTexture != null)
            {
                UnityEngine.Object.Destroy(this.shaderVignetteTexture);
                this.shaderVignetteTexture = null;
            }
        }

        private void ApplyShaderEffectPreset(int index)
        {
            index = Mathf.Clamp(index, 0, shaderEffectPresetNames.Length - 1);
            this.shaderEffectPresetIndexConfig.Value = index;
            this.shaderEffectsEnabledConfig.Value = index != 0;
            this.shaderExposureConfig.Value = 1f;
            this.shaderTintStrengthConfig.Value = 0f;
            this.shaderVignetteConfig.Value = 0f;
            this.shaderScanlineConfig.Value = 0f;
            this.shaderLetterboxConfig.Value = 0f;
            string tint = "#FFFFFF";
            if (index == 1) { this.shaderExposureConfig.Value = 1.08f; this.shaderTintStrengthConfig.Value = 0.16f; this.shaderVignetteConfig.Value = 0.18f; tint = "#C5E4FF"; }
            else if (index == 2) { this.shaderExposureConfig.Value = 0.92f; this.shaderTintStrengthConfig.Value = 0.2f; this.shaderVignetteConfig.Value = 0.48f; this.shaderLetterboxConfig.Value = 0.36f; tint = "#FFD3AA"; }
            else if (index == 3) { this.shaderExposureConfig.Value = 1.12f; this.shaderTintStrengthConfig.Value = 0.18f; this.shaderVignetteConfig.Value = 0.12f; tint = "#D3F2FF"; }
            else if (index == 4) { this.shaderExposureConfig.Value = 0.86f; this.shaderTintStrengthConfig.Value = 0.44f; this.shaderVignetteConfig.Value = 0.35f; tint = "#B8B8B8"; }
            else if (index == 5) { this.shaderExposureConfig.Value = 0.95f; this.shaderTintStrengthConfig.Value = 0.12f; this.shaderVignetteConfig.Value = 0.38f; this.shaderScanlineConfig.Value = 0.35f; tint = "#8FD4B8"; }
            this.ApplyShaderTintHex(tint, true);
            this.shaderPresetStatus = "Applied " + shaderEffectPresetNames[index] + " preset.";
        }

        private void ApplyShaderTintHex(string value, bool saveValue)
        {
            string hex = (value ?? string.Empty).Trim();
            if (!hex.StartsWith("#"))
            {
                hex = "#" + hex;
            }
            Color color;
            if (!ColorUtility.TryParseHtmlString(hex, out color))
            {
                this.shaderPresetStatus = "Tint must be a valid #RRGGBB color.";
                return;
            }
            this.shaderTintColor = new Color(color.r, color.g, color.b, 1f);
            this.shaderTintHexInput = "#" + ColorUtility.ToHtmlStringRGB(this.shaderTintColor);
            if (saveValue)
            {
                this.shaderTintHexConfig.Value = this.shaderTintHexInput;
            }
        }

        private void InitializeShaderPresetFolder()
        {
            try
            {
                this.shaderPresetFolder = Path.Combine(Paths.ConfigPath, "LiamCasting", "Shaders");
                Directory.CreateDirectory(this.shaderPresetFolder);
                string templatePath = Path.Combine(this.shaderPresetFolder, "LiamCastingEffects.ini");
                if (!File.Exists(templatePath))
                {
                    File.WriteAllLines(templatePath, new string[]
                    {
                        "[LiamCasting]",
                        "Enabled=true",
                        "Exposure=1.05",
                        "TintHex=#C5E4FF",
                        "TintStrength=0.12",
                        "Vignette=0.18",
                        "Scanlines=0",
                        "Letterbox=0"
                    });
                }
            }
            catch (Exception exception)
            {
                this.shaderPresetStatus = "Could not create shader preset folder: " + exception.Message;
            }
        }

        private void LoadShaderIniPreset()
        {
            try
            {
                string path = (this.shaderPresetPathInput ?? string.Empty).Trim().Trim('"');
                if (!path.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
                {
                    this.shaderPresetStatus = "Select a .ini preset file.";
                    return;
                }
                string fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath))
                {
                    this.shaderPresetStatus = "Preset file was not found.";
                    return;
                }
                Dictionary<string, string> values = this.ReadIniValues(fullPath);
                this.ApplyShaderIniValues(values);
                this.shaderPresetPathConfig.Value = fullPath;
                this.shaderPresetPathInput = fullPath;
                this.shaderEffectsEnabledConfig.Value = true;
                this.shaderPresetStatus = "Loaded compatible values from " + Path.GetFileName(fullPath) + ".";
            }
            catch (Exception exception)
            {
                this.shaderPresetStatus = "Could not load preset: " + exception.Message;
            }
        }

        private Dictionary<string, string> ReadIniValues(string path)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("["))
                {
                    continue;
                }
                int separator = line.IndexOf('=');
                if (separator > 0)
                {
                    values[line.Substring(0, separator).Trim()] = line.Substring(separator + 1).Trim();
                }
            }
            return values;
        }

        private void ApplyShaderIniValues(Dictionary<string, string> values)
        {
            bool enabled;
            if (this.TryReadIniBool(values, "Enabled", out enabled)) this.shaderEffectsEnabledConfig.Value = enabled;
            float value;
            if (this.TryReadIniFloat(values, "Exposure", out value) || this.TryReadIniFloat(values, "Brightness", out value)) this.shaderExposureConfig.Value = Mathf.Clamp(value, 0.25f, 2f);
            if (this.TryReadIniFloat(values, "TintStrength", out value) || this.TryReadIniFloat(values, "Vibrance", out value)) this.shaderTintStrengthConfig.Value = Mathf.Clamp01(value);
            if (this.TryReadIniFloat(values, "Vignette", out value) || this.TryReadIniFloat(values, "VignetteAmount", out value)) this.shaderVignetteConfig.Value = Mathf.Clamp01(value);
            if (this.TryReadIniFloat(values, "Scanlines", out value) || this.TryReadIniFloat(values, "ScanlineIntensity", out value)) this.shaderScanlineConfig.Value = Mathf.Clamp01(value);
            if (this.TryReadIniFloat(values, "Letterbox", out value)) this.shaderLetterboxConfig.Value = Mathf.Clamp01(value);
            string tint;
            if (values.TryGetValue("TintHex", out tint) || values.TryGetValue("TintColor", out tint)) this.ApplyShaderTintHex(tint, true);
            string techniques;
            if (values.TryGetValue("Techniques", out techniques))
            {
                if (techniques.IndexOf("Vignette", StringComparison.OrdinalIgnoreCase) >= 0) this.shaderVignetteConfig.Value = Mathf.Max(this.shaderVignetteConfig.Value, 0.3f);
                if (techniques.IndexOf("CRT", StringComparison.OrdinalIgnoreCase) >= 0) this.shaderScanlineConfig.Value = Mathf.Max(this.shaderScanlineConfig.Value, 0.25f);
                if (techniques.IndexOf("Luma", StringComparison.OrdinalIgnoreCase) >= 0) this.shaderExposureConfig.Value = Mathf.Max(this.shaderExposureConfig.Value, 1.05f);
            }
        }

        private bool TryReadIniFloat(Dictionary<string, string> values, string key, out float value)
        {
            value = 0f;
            string text;
            return values.TryGetValue(key, out text) && (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || float.TryParse(text, out value));
        }

        private bool TryReadIniBool(Dictionary<string, string> values, string key, out bool value)
        {
            string text;
            if (values.TryGetValue(key, out text))
            {
                if (text == "1") { value = true; return true; }
                if (text == "0") { value = false; return true; }
                return bool.TryParse(text, out value);
            }
            value = false;
            return false;
        }

        private void DisplayNameTags()
        {
            if (!this.nameTagsEnabledConfig.Value)
            {
                if (this.currentNameTagPlates.Count > 0)
                {
                    this.UnRenderNameTags();
                }
                return;
            }
            List<VRRig> rigs = this.GetAllVRRigs();
            if (rigs == null)
            {
                return;
            }
            List<VRRig> staleRigs = this.currentNameTagPlates.Keys.Where(delegate(VRRig rig)
            {
                return rig == null || !rigs.Contains(rig) || !this.currentNameTagPlates[rig];
            }).ToList();
            for (int i = 0; i < staleRigs.Count; i++)
            {
                GameObject stalePlate;
                if (this.currentNameTagPlates.TryGetValue(staleRigs[i], out stalePlate) && stalePlate != null)
                {
                    UnityEngine.Object.Destroy(stalePlate);
                }
                if (staleRigs[i] != null)
                {
                    this.currentNameTagPlates.Remove(staleRigs[i]);
                }
            }
            List<VRRig> deadKeys = this.currentNameTagPlates.Keys.Where(delegate(VRRig rig) { return rig == null; }).ToList();
            for (int i = 0; i < deadKeys.Count; i++)
            {
                this.currentNameTagPlates.Remove(deadKeys[i]);
            }
            for (int i = 0; i < rigs.Count; i++)
            {
                VRRig rig = rigs[i];
                if (rig == null)
                {
                    continue;
                }
                if (!this.currentNameTagPlates.ContainsKey(rig))
                {
                    this.RenderNameTagPlate(rig);
                }
                GameObject plate;
                if (this.currentNameTagPlates.TryGetValue(rig, out plate) && plate != null)
                {
                    this.UpdateNameTagPlate(rig, plate);
                }
            }
        }

        private void RenderNameTagPlate(VRRig rig)
        {
            if (rig == null || rig.transform == null)
            {
                return;
            }
            GameObject plate = new GameObject("LiamNameTag");
            plate.transform.position = this.GetNameTagAnchor(rig);
            plate.transform.localScale = Vector3.one * (0.07f * this.nameTagSizeConfig.Value);

            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(plate.transform, false);
            TextMeshPro nameText = nameObj.AddComponent<TextMeshPro>();
            nameText.font = TMP_Settings.defaultFontAsset;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontSize = 7.5f;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.outlineWidth = 0.2f;
            nameText.outlineColor = Color.black;

            GameObject hzObj = new GameObject("Hz");
            hzObj.transform.SetParent(plate.transform, false);
            hzObj.transform.localPosition = new Vector3(0f, -0.75f, 0f);
            TextMeshPro hzText = hzObj.AddComponent<TextMeshPro>();
            hzText.font = TMP_Settings.defaultFontAsset;
            hzText.alignment = TextAlignmentOptions.Center;
            hzText.fontSize = 4.0f;
            hzText.color = Color.white;
            hzText.outlineWidth = 0.16f;
            hzText.outlineColor = Color.black;
            hzObj.SetActive(false);

            GameObject infoObj = new GameObject("Info");
            infoObj.transform.SetParent(plate.transform, false);
            infoObj.transform.localPosition = new Vector3(0f, -1.25f, 0f);
            TextMeshPro infoText = infoObj.AddComponent<TextMeshPro>();
            infoText.font = TMP_Settings.defaultFontAsset;
            infoText.alignment = TextAlignmentOptions.Center;
            infoText.fontSize = 3.0f;
            infoText.textWrappingMode = TextWrappingModes.NoWrap;
            infoText.overflowMode = TextOverflowModes.Ellipsis;
            infoText.color = new Color(0.88f, 0.92f, 1f, 1f);
            infoText.outlineWidth = 0.18f;
            infoText.outlineColor = Color.black;

            GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            background.name = "Background";
            background.transform.SetParent(plate.transform, false);
            background.transform.localPosition = new Vector3(0f, -0.35f, -0.08f);
            background.transform.localScale = new Vector3(3.6f, 1.9f, 1f);
            Collider backgroundCollider = background.GetComponent<Collider>();
            if (backgroundCollider != null) UnityEngine.Object.Destroy(backgroundCollider);
            Renderer backgroundRenderer = background.GetComponent<Renderer>();
            Shader backgroundShader = Shader.Find("Sprites/Default");
            if (backgroundRenderer != null && backgroundShader != null)
            {
                Material backgroundMaterial = new Material(backgroundShader);
                backgroundMaterial.color = new Color(0.025f, 0.03f, 0.045f, 0.72f);
                backgroundRenderer.material = backgroundMaterial;
            }

            this.currentNameTagPlates.Add(rig, plate);
            plate.SetActive(false);
        }

        private void UpdateNameTagPlate(VRRig rig, GameObject plate)
        {
            if (plate == null)
            {
                return;
            }
            Camera mainCamera = this.GetMainGameCamera();
            float distance = mainCamera != null ? Vector3.Distance(mainCamera.transform.position, this.GetNameTagAnchor(rig)) : 0f;
            bool withinDistance = distance <= Mathf.Clamp(this.nameTagMaxDistanceConfig.Value, 5f, 150f);
            plate.SetActive(withinDistance);
            if (!withinDistance) return;
            plate.transform.position = this.GetNameTagAnchor(rig);
            plate.transform.localScale = Vector3.one * (0.07f * this.nameTagSizeConfig.Value);

            Transform nameTransform = plate.transform.Find("Name");
            Transform hzTransform = plate.transform.Find("Hz");
            Transform infoTransform = plate.transform.Find("Info");
            Transform backgroundTransform = plate.transform.Find("Background");
            if (nameTransform == null || hzTransform == null)
            {
                return;
            }
            TextMeshPro nameText = nameTransform.GetComponent<TextMeshPro>();
            TextMeshPro hzText = hzTransform.GetComponent<TextMeshPro>();
            TextMeshPro infoText = infoTransform != null ? infoTransform.GetComponent<TextMeshPro>() : null;
            if (nameText == null || hzText == null)
            {
                return;
            }

            string displayName = "Unknown";
            Color nameColor = Color.white;
            try
            {
                if (this.IsOfflineRig(rig))
                {
                    displayName = "YOU";
                }
                else if (rig.playerText1 != null && !string.IsNullOrEmpty(rig.playerText1.text))
                {
                    displayName = rig.playerText1.text;
                }
                if (rig.materialsToChangeTo != null && rig.materialsToChangeTo.Length != 0 && rig.materialsToChangeTo[0] != null)
                {
                    nameColor = rig.materialsToChangeTo[0].color;
                    nameColor.a = 1f;
                }
            }
            catch
            {
            }

            nameText.text = this.nameTagPrefixConfig.Value + displayName + this.nameTagSuffixConfig.Value;
            nameText.fontStyle = this.GetSelectedFontStyle();
            nameText.color = this.nameTagShowPlayerColorConfig.Value ? this.GetReadableNameColor(this.IsRigTagged(rig), nameColor) : Color.white;

            bool showHz = this.nameTagShowHzConfig.Value;
            hzText.gameObject.SetActive(showHz);
            if (showHz)
            {
                hzText.text = "N/A";
                bool boldItalic = this.GetSelectedFontStyle() == (FontStyles.Bold | FontStyles.Italic);
                hzText.transform.localPosition = boldItalic ? new Vector3(0f, -0.85f, 0f) : new Vector3(0f, -0.75f, 0f);
            }

            if (infoText != null)
            {
                List<string> details = new List<string>();
                if (this.nameTagShowTaggedConfig.Value) details.Add(this.IsRigTagged(rig) ? "TAGGED" : "SAFE");
                if (this.nameTagShowDistanceConfig.Value) details.Add(distance.ToString("F1", CultureInfo.InvariantCulture) + " m");
                if (this.nameTagShowSpeedConfig.Value) details.Add(this.GetNameTagSpeed(rig).ToString("F1", CultureInfo.InvariantCulture) + " m/s");
                infoText.text = string.Join("  |  ", details.ToArray());
                infoText.gameObject.SetActive(details.Count > 0);
            }

            if (backgroundTransform != null)
            {
                backgroundTransform.gameObject.SetActive(this.nameTagShowBackgroundConfig.Value);
                backgroundTransform.localScale = new Vector3(3.6f, this.nameTagShowHzConfig.Value ? 2.25f : 1.9f, 1f);
            }

            this.ApplyNameTagFacing(plate.transform, rig.transform);
        }

        private float GetNameTagSpeed(VRRig rig)
        {
            if (rig == null) return 0f;
            Vector3 current = rig.transform.position;
            Vector3 previous;
            float speed = 0f;
            if (this.nameTagPreviousPositions.TryGetValue(rig, out previous) && Time.unscaledDeltaTime > 0.0001f)
            {
                speed = Vector3.Distance(current, previous) / Time.unscaledDeltaTime;
                speed = Mathf.Min(speed, 50f);
            }
            this.nameTagPreviousPositions[rig] = current;
            this.nameTagSpeeds[rig] = speed;
            return speed;
        }

        private Vector3 GetNameTagAnchor(VRRig rig)
        {
            if (rig != null && rig.headMesh != null)
            {
                return rig.headMesh.transform.position + Vector3.up * this.nameTagHeightConfig.Value;
            }
            return rig.transform.position + Vector3.up * this.nameTagHeightConfig.Value;
        }

        private Color GetReadableNameColor(bool isTagged, Color playerColor)
        {
            if (isTagged)
            {
                return new Color(0.9f, 0.22f, 0.06f, 1f);
            }
            playerColor.a = 1f;
            float brightness = playerColor.r * 0.2126f + playerColor.g * 0.7152f + playerColor.b * 0.0722f;
            return brightness < 0.35f ? Color.Lerp(playerColor, Color.white, 0.7f) : playerColor;
        }

        private void ApplyNameTagFacing(Transform plate, Transform rigTransform)
        {
            if (plate == null || rigTransform == null)
            {
                return;
            }
            int facingMode = Mathf.Clamp(this.nameTagFacingModeConfig.Value, 0, nameTagFacingModeNames.Length - 1);
            Vector3 facingDirection;
            switch (facingMode)
            {
                case 1:
                    facingDirection = rigTransform.forward;
                    break;
                case 2:
                    facingDirection = -rigTransform.forward;
                    break;
                case 3:
                    facingDirection = Vector3.forward;
                    break;
                default:
                    Camera camera = this.GetMainGameCamera();
                    facingDirection = camera != null ? camera.transform.position - plate.position : rigTransform.forward;
                    break;
            }
            facingDirection = Vector3.ProjectOnPlane(facingDirection, Vector3.up);
            if (facingDirection.sqrMagnitude > 0.001f)
            {
                plate.rotation = Quaternion.LookRotation(facingDirection.normalized, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
            }
        }

        private FontStyles GetSelectedFontStyle()
        {
            int index = Mathf.Clamp(this.nameTagFontIndexConfig.Value, 0, nameTagFontStyleNames.Length - 1);
            switch (index)
            {
                case 1:
                    return FontStyles.Bold;
                case 2:
                    return FontStyles.Italic;
                case 3:
                    return FontStyles.Bold | FontStyles.Italic;
                default:
                    return FontStyles.Normal;
            }
        }

        private void UnRenderNameTags()
        {
            foreach (GameObject plate in this.currentNameTagPlates.Values)
            {
                if (plate != null)
                {
                    UnityEngine.Object.Destroy(plate);
                }
            }
            this.currentNameTagPlates.Clear();
            this.nameTagPreviousPositions.Clear();
            this.nameTagSpeeds.Clear();
        }

        private void UpdateFPSCounter()
        {
            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }
            this.fpsSmoothingDeltaTime += (deltaTime - this.fpsSmoothingDeltaTime) * 0.1f;
            this.currentFPS = (this.fpsSmoothingDeltaTime > 0f) ? (1f / this.fpsSmoothingDeltaTime) : 0f;
        }

        private List<VRRig> GetAllVRRigs()
        {
            if (!this.performanceOptimizationConfig.Value)
            {
                List<VRRig> list = new List<VRRig>();
                VRRig[] rigs = UnityEngine.Object.FindObjectsOfType<VRRig>();
                if (rigs != null)
                {
                    foreach (VRRig rig in rigs)
                    {
                        if (rig != null && !list.Contains(rig))
                        {
                            list.Add(rig);
                        }
                    }
                }
                if (GorillaTagger.Instance != null && GorillaTagger.Instance.offlineVRRig != null && !list.Contains(GorillaTagger.Instance.offlineVRRig))
                {
                    list.Insert(0, GorillaTagger.Instance.offlineVRRig);
                }
                return list;
            }
            return this.cachedVRRigs;
        }

        private Camera GetMainGameCamera()
        {
            if (this.mainGameCamera == null)
            {
                this.mainGameCamera = Camera.main;
            }
            return this.mainGameCamera;
        }

        private bool IsOfflineRig(VRRig rig)
        {
            return rig != null && GorillaTagger.Instance != null && rig == GorillaTagger.Instance.offlineVRRig;
        }

        private string GetRigName(VRRig rig)
        {
            if (rig != null && rig.playerText1 != null && !string.IsNullOrEmpty(rig.playerText1.text))
            {
                return rig.playerText1.text;
            }
            return "Unknown";
        }

        private string SlotKeyLabel(int slot)
        {
            return slot == 8 ? "0" : (slot + 2).ToString();
        }

        private void RefreshVRRigCache(bool force)
        {
            if (!this.performanceOptimizationConfig.Value)
            {
                return;
            }
            float interval = Mathf.Clamp(this.rigCacheRefreshIntervalConfig.Value, 0.1f, 2f);
            if (!force && Time.unscaledTime < this.nextRigCacheRefreshTime)
            {
                return;
            }
            this.nextRigCacheRefreshTime = Time.unscaledTime + interval;
            this.cachedVRRigs.Clear();
            VRRig[] rigs = UnityEngine.Object.FindObjectsOfType<VRRig>();
            if (rigs != null)
            {
                foreach (VRRig rig in rigs)
                {
                    if (rig != null && !this.cachedVRRigs.Contains(rig))
                    {
                        this.cachedVRRigs.Add(rig);
                    }
                }
            }
            if (GorillaTagger.Instance != null && GorillaTagger.Instance.offlineVRRig != null && !this.cachedVRRigs.Contains(GorillaTagger.Instance.offlineVRRig))
            {
                this.cachedVRRigs.Insert(0, GorillaTagger.Instance.offlineVRRig);
            }
            this.RefreshLeaderboardCache();
        }

        private void RefreshLeaderboardCache()
        {
            this.cachedLeaderboardEntries.Clear();
            for (int i = 0; i < this.cachedVRRigs.Count; i++)
            {
                VRRig rig = this.cachedVRRigs[i];
                if (rig == null)
                {
                    continue;
                }
                string name = "Unknown";
                if (this.IsOfflineRig(rig))
                {
                    name = "YOU";
                }
                else if (rig.playerText1 != null && !string.IsNullOrEmpty(rig.playerText1.text))
                {
                    name = rig.playerText1.text.ToUpperInvariant();
                }
                Color color = Color.white;
                try
                {
                    if (rig.materialsToChangeTo != null && rig.materialsToChangeTo.Length != 0 && rig.materialsToChangeTo[0] != null)
                    {
                        color = rig.materialsToChangeTo[0].color;
                    }
                }
                catch
                {
                }
                color.a = 1f;
                if (this.IsRigTagged(rig))
                {
                    color = new Color(1f, 0.3f, 0.3f, 1f);
                }
                else if ((color.r + color.g + color.b) / 3f < 0.4f)
                {
                    color = Color.Lerp(color, Color.white, 0.5f);
                }
                this.cachedLeaderboardEntries.Add(new LeaderboardEntry
                {
                    text = name,
                    color = color
                });
            }
        }

        private void DrawShadowedLabel(Rect rect, string text, GUIStyle style, Color color)
        {
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.Label(new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width, rect.height), text, style);
            GUI.color = color;
            GUI.Label(rect, text, style);
            GUI.color = old;
        }

        private void RenderPlayerLeaderboard()
        {
            if (!PhotonNetwork.InRoom || this.leaderboardTextStyle == null)
            {
                return;
            }
            const float width = 280f;
            const float rowHeight = 27f;
            float totalHeight = (float)this.cachedLeaderboardEntries.Count * rowHeight;
            float y = (float)Screen.height - totalHeight - 12f;
            for (int i = 0; i < this.cachedLeaderboardEntries.Count; i++)
            {
                LeaderboardEntry entry = this.cachedLeaderboardEntries[i];
                Rect row = new Rect(12f, y, width, rowHeight - 3f);
                this.Fill(row, new Color(0.04f, 0.045f, 0.06f, 0.88f));
                this.Fill(new Rect(row.x, row.y, 3f, row.height), entry.color);
                this.DrawShadowedLabel(new Rect(row.x + 12f, row.y, 24f, row.height), (i + 1).ToString(), this.leaderboardTextStyle, this.AccentAlpha(1f));
                this.DrawShadowedLabel(new Rect(row.x + 40f, row.y, row.width - 48f, row.height), entry.text, this.leaderboardTextStyle, entry.color);
                y += rowHeight;
            }
        }

        private void RenderTeamScoreboard()
        {
            if (!this.scoreboardEnabledConfig.Value || this.teamNameStyle == null || this.teamScoreStyle == null)
            {
                return;
            }
            float scale = Mathf.Clamp(this.scoreboardScaleConfig.Value, 0.6f, 1.5f);
            float width = 520f * scale;
            float height = 92f * scale;
            float x = (float)Screen.width * 0.5f + Mathf.Clamp(this.scoreboardPositionXConfig.Value, -0.45f, 0.45f) * (float)Screen.width - width * 0.5f;
            float y = Mathf.Clamp(this.scoreboardPositionYConfig.Value, 0f, 0.85f) * (float)Screen.height;
            x = Mathf.Clamp(x, 4f, Mathf.Max(4f, (float)Screen.width - width - 4f));
            y = Mathf.Clamp(y, 4f, Mathf.Max(4f, (float)Screen.height - height - 4f));
            Rect board = new Rect(x, y, width, height);
            this.Fill(board, new Color(0.04f, 0.045f, 0.06f, Mathf.Clamp(this.scoreboardOpacityConfig.Value, 0.2f, 1f)));
            this.Fill(new Rect(board.x, board.y, board.width, 3f), this.AccentAlpha(1f));
            this.Fill(new Rect(board.x, board.y + 3f, 4f, board.height - 3f), new Color(0.4f, 0.75f, 1f, 1f));
            this.Fill(new Rect(board.xMax - 4f, board.y + 3f, 4f, board.height - 3f), new Color(1f, 0.45f, 0.38f, 1f));
            float teamWidth = (width - 100f) * 0.5f;
            Rect leftRect = new Rect(board.x + 12f, board.y + 6f, teamWidth, height - 10f);
            Rect rightRect = new Rect(board.xMax - teamWidth - 12f, board.y + 6f, teamWidth, height - 10f);
            Rect centerRect = new Rect(leftRect.xMax, board.y + 6f, 76f, height - 10f);
            this.DrawShadowedLabel(new Rect(leftRect.x, leftRect.y, leftRect.width, 22f), this.GetTeamName(this.leftTeamNameConfig.Value, "TEAM ONE"), this.teamNameStyle, new Color(0.4f, 0.75f, 1f, 1f));
            this.DrawShadowedLabel(new Rect(leftRect.x, leftRect.y + 26f, leftRect.width, 50f), this.leftTeamScore.ToString(), this.teamScoreStyle, Color.white);
            this.DrawShadowedLabel(new Rect(centerRect.x, centerRect.y + 4f, centerRect.width, 24f), this.FormatScoreboardDuration(this.scoreboardElapsedSeconds), this.teamNameStyle, Color.white);
            this.DrawShadowedLabel(new Rect(centerRect.x, centerRect.y + 30f, centerRect.width, 20f), this.scoreboardTimeToBeatSeconds > 0f ? this.FormatScoreboardDuration(this.scoreboardTimeToBeatSeconds) : "--:--.-", this.teamNameStyle, this.AccentAlpha(1f));
            this.DrawShadowedLabel(new Rect(centerRect.x, centerRect.y + 54f, centerRect.width, 18f), "VS", this.teamNameStyle, this.AccentAlpha(1f));
            this.DrawShadowedLabel(new Rect(rightRect.x, rightRect.y, rightRect.width, 22f), this.GetTeamName(this.rightTeamNameConfig.Value, "TEAM TWO"), this.teamNameStyle, new Color(1f, 0.45f, 0.38f, 1f));
            this.DrawShadowedLabel(new Rect(rightRect.x, rightRect.y + 26f, rightRect.width, 50f), this.rightTeamScore.ToString(), this.teamScoreStyle, Color.white);
        }

        private string GetTeamName(string name, string fallback)
        {
            return string.IsNullOrEmpty(name) ? fallback : name.ToUpperInvariant();
        }

        private void RenderTargetIndicator()
        {
            if (!this.isCameraActive || string.IsNullOrEmpty(this.targetPlayerName) || this.targetLabelStyle == null)
            {
                return;
            }
            string text;
            if (this.autoObserverEnabledConfig.Value && this.IsForestLoaded())
            {
                text = "FOREST OBSERVER";
            }
            else
            {
                int mode = Mathf.Clamp(this.cameraModeConfig.Value, 0, modeLabels.Length - 1);
                text = this.targetPlayerName.ToUpperInvariant() + "   |   " + modeLabels[mode].ToUpperInvariant();
            }
            Vector2 size = this.targetLabelStyle.CalcSize(new GUIContent(text));
            Rect rect = new Rect(8f, 8f, size.x + 28f, 26f);
            this.Fill(rect, new Color(0.04f, 0.045f, 0.06f, 0.85f));
            this.Fill(new Rect(rect.x, rect.y, 3f, rect.height), this.AccentAlpha(1f));
            GUI.Label(new Rect(rect.x + 14f, rect.y, rect.width - 16f, rect.height), text, this.targetLabelStyle);
        }

        private void ProcessScoreboardControls()
        {
            if (!this.scoreboardEnabledConfig.Value || Keyboard.current == null || this.typing)
            {
                return;
            }
            Keyboard keyboard = Keyboard.current;
            if (keyboard.shiftKey.isPressed && keyboard.spaceKey.wasPressedThisFrame)
            {
                this.SetScoreboardTimeToBeat();
                return;
            }
            if (keyboard.ctrlKey.isPressed && keyboard.deleteKey.wasPressedThisFrame)
            {
                this.ClearScoreboardTimes();
                return;
            }
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                if (this.scoreboardTimerRunning)
                {
                    this.PauseScoreboardTimer();
                }
                else
                {
                    this.StartScoreboardTimer();
                }
            }
            if (keyboard.deleteKey.wasPressedThisFrame)
            {
                this.ResetScoreboardTimer();
            }
            if (keyboard.equalsKey.wasPressedThisFrame)
            {
                this.leftTeamScore++;
            }
            if (keyboard.minusKey.wasPressedThisFrame)
            {
                this.leftTeamScore = Mathf.Max(0, this.leftTeamScore - 1);
            }
            if (keyboard.rightBracketKey.wasPressedThisFrame)
            {
                this.rightTeamScore++;
            }
            if (keyboard.leftBracketKey.wasPressedThisFrame)
            {
                this.rightTeamScore = Mathf.Max(0, this.rightTeamScore - 1);
            }
        }

        private void UpdateScoreboardTimer()
        {
            bool scoreboardEnabled = this.scoreboardEnabledConfig.Value;
            if (scoreboardEnabled && !this.wasScoreboardEnabled && this.scoreboardTimerAutoStartConfig.Value)
            {
                this.ResetScoreboardTimer();
                this.StartScoreboardTimer();
            }
            if (!scoreboardEnabled && this.wasScoreboardEnabled)
            {
                this.PauseScoreboardTimer();
            }
            this.wasScoreboardEnabled = scoreboardEnabled;
            if (this.scoreboardTimerRunning)
            {
                this.scoreboardElapsedSeconds = Mathf.Min(180f, this.scoreboardElapsedSeconds + Time.unscaledDeltaTime);
                if (this.scoreboardElapsedSeconds >= 180f)
                {
                    this.scoreboardTimerRunning = false;
                }
            }
        }

        private void StartScoreboardTimer()
        {
            this.scoreboardTimerRunning = true;
        }

        private void PauseScoreboardTimer()
        {
            this.scoreboardTimerRunning = false;
        }

        private void ResetScoreboardTimer()
        {
            this.scoreboardElapsedSeconds = -this.GetScoreboardCountdownSeconds();
            this.scoreboardTimerRunning = false;
        }

        private void SetScoreboardTimeToBeat()
        {
            if (this.scoreboardElapsedSeconds < 0f)
            {
                return;
            }
            this.scoreboardTimeToBeatSeconds = this.scoreboardElapsedSeconds;
        }

        private void ClearScoreboardTimes()
        {
            this.scoreboardTimeToBeatSeconds = 0f;
        }

        private int GetScoreboardCountdownIndex()
        {
            return Mathf.Clamp(this.scoreboardCountdownIndexConfig.Value, 0, scoreboardCountdownValues.Length - 1);
        }

        private float GetScoreboardCountdownSeconds()
        {
            return scoreboardCountdownValues[this.GetScoreboardCountdownIndex()];
        }

        private string FormatScoreboardDuration(float timeInSeconds)
        {
            float seconds = Mathf.Abs(timeInSeconds);
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remainingSeconds = seconds % 60f;
            return timeInSeconds < 0f ? string.Format("-{0}:{1:00.0}", minutes, remainingSeconds) : string.Format("{0}:{1:00.0}", minutes, remainingSeconds);
        }

        private bool IsRigTagged(VRRig rig)
        {
            if (rig == null)
            {
                return false;
            }
            try
            {
                if (!rigIsTaggedPropertyChecked)
                {
                    rigIsTaggedProperty = typeof(VRRig).GetProperty("isTagged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    rigIsTaggedPropertyChecked = true;
                }
                if (rigIsTaggedProperty != null)
                {
                    object value = rigIsTaggedProperty.GetValue(rig, null);
                    if (value is bool)
                    {
                        return (bool)value;
                    }
                }
            }
            catch
            {
            }
            try
            {
                if (!rigIsTaggedFieldChecked)
                {
                    rigIsTaggedField = typeof(VRRig).GetField("isTagged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    rigIsTaggedFieldChecked = true;
                }
                if (rigIsTaggedField != null)
                {
                    object value2 = rigIsTaggedField.GetValue(rig);
                    if (value2 is bool)
                    {
                        return (bool)value2;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        private void ProcessMenuToggle()
        {
            if (Keyboard.current == null || this.typing)
            {
                return;
            }
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                this.isMenuVisible = !this.isMenuVisible;
            }
        }

        private void ProcessFlightControls()
        {
            if (!this.flyEnabledConfig.Value || GorillaTagger.Instance == null || Keyboard.current == null || this.typing)
            {
                return;
            }
            Camera camera = this.GetMainGameCamera();
            if (camera == null)
            {
                return;
            }
            Rigidbody body = GorillaTagger.Instance.rigidbody;
            if (body == null)
            {
                return;
            }
            bool moving = this.IsPlayerMoving();
            if (moving || this.stationaryFlyConfig.Value)
            {
                body.velocity = Vector3.zero;
            }
            if (!moving)
            {
                return;
            }
            float speed = Keyboard.current.leftShiftKey.isPressed ? this.flySpeedConfig.Value * 2f : this.flySpeedConfig.Value;
            Vector3 direction = this.CalculateMovementDirection(camera.transform);
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }
            Vector3 newPosition = body.transform.position + direction * speed * Time.deltaTime;
            body.transform.position = newPosition;
            body.position = newPosition;
            body.velocity = Vector3.zero;
        }

        private bool IsPlayerMoving()
        {
            Keyboard k = Keyboard.current;
            return k.wKey.isPressed || k.aKey.isPressed || k.sKey.isPressed || k.dKey.isPressed || k.spaceKey.isPressed || k.leftCtrlKey.isPressed;
        }

        private Vector3 CalculateMovementDirection(Transform cameraTransform)
        {
            Vector3 result = Vector3.zero;
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            Keyboard k = Keyboard.current;
            if (k.wKey.isPressed) result += forward;
            if (k.sKey.isPressed) result -= forward;
            if (k.dKey.isPressed) result += right;
            if (k.aKey.isPressed) result -= right;
            if (k.spaceKey.isPressed) result += Vector3.up;
            if (k.leftCtrlKey.isPressed) result -= Vector3.up;
            return result.normalized;
        }

        private void ToggleCameraSystem()
        {
            this.isCameraActive = !this.isCameraActive;
            if (this.isCameraActive)
            {
                this.ActivateCamera();
                return;
            }
            this.RestoreTargetLerp();
            this.DeactivateCamera();
        }

        private void ActivateCamera()
        {
            Camera mainCamera = this.GetMainGameCamera();
            if (this.spectatorCamera == null && mainCamera != null)
            {
                GameObject cameraObject = new GameObject("LiamSpectatorCam");
                this.spectatorCamera = cameraObject.AddComponent<Camera>();
                this.spectatorCamera.cameraType = CameraType.Game;
                this.spectatorCamera.cullingMask = mainCamera.cullingMask;
                this.spectatorCamera.clearFlags = mainCamera.clearFlags;
                this.spectatorCamera.backgroundColor = mainCamera.backgroundColor;
                this.spectatorCamera.renderingPath = mainCamera.renderingPath;
                this.spectatorCamera.depthTextureMode = mainCamera.depthTextureMode;
                this.spectatorCamera.allowHDR = mainCamera.allowHDR;
                this.spectatorCamera.allowMSAA = mainCamera.allowMSAA;
                this.spectatorCamera.useOcclusionCulling = mainCamera.useOcclusionCulling;
                this.spectatorCamera.stereoTargetEye = StereoTargetEyeMask.None;
                cameraObject.tag = "Untagged";
                LiamShaderEffect shaderEffect = cameraObject.AddComponent<LiamShaderEffect>();
                shaderEffect.Configure(this.shaderEffectsEnabledConfig.Value, this.GetShaderNameForPreset(this.shaderEffectPresetIndexConfig.Value), this.shaderExposureConfig.Value, this.shaderTintStrengthConfig.Value, this.shaderTintColor, this.shaderVignetteConfig.Value);
            }
            if (this.spectatorCamera != null)
            {
                LiamShaderEffect shaderEffect = this.spectatorCamera.GetComponent<LiamShaderEffect>();
                if (shaderEffect != null) shaderEffect.Configure(this.shaderEffectsEnabledConfig.Value, this.GetShaderNameForPreset(this.shaderEffectPresetIndexConfig.Value), this.shaderExposureConfig.Value, this.shaderTintStrengthConfig.Value, this.shaderTintColor, this.shaderVignetteConfig.Value);
            }
            this.EnsureCameraRenderTexture();
            if (this.spectatorCamera != null)
            {
                this.spectatorCamera.targetTexture = this.cameraRenderTexture;
                this.spectatorCamera.gameObject.SetActive(true);
            }
            this.targetRig = null;
            this.targetPlayerName = "You";
        }

        private void EnsureCameraRenderTexture()
        {
            if (!this.isCameraActive)
            {
                return;
            }
            float scale = Mathf.Clamp(this.cameraRenderScaleConfig.Value, 0.5f, 1f);
            int width = Mathf.Max(1, Mathf.RoundToInt(Screen.width * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(Screen.height * scale));
            if (this.cameraRenderTexture != null && this.cameraRenderTexture.width == width && this.cameraRenderTexture.height == height)
            {
                return;
            }
            if (this.cameraRenderTexture != null)
            {
                if (this.spectatorCamera != null)
                {
                    this.spectatorCamera.targetTexture = null;
                }
                this.cameraRenderTexture.Release();
                UnityEngine.Object.Destroy(this.cameraRenderTexture);
                this.cameraRenderTexture = null;
            }
            Camera mainCamera = this.GetMainGameCamera();
            RenderTextureFormat format = (mainCamera != null && mainCamera.allowHDR) ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            this.cameraRenderTexture = new RenderTexture(width, height, 24, format);
            this.cameraRenderTexture.filterMode = FilterMode.Bilinear;
            this.cameraRenderTexture.useMipMap = false;
            this.cameraRenderTexture.autoGenerateMips = false;
            this.cameraRenderTexture.antiAliasing = 1;
            this.cameraRenderTexture.Create();
        }

        private void DeactivateCamera()
        {
            if (this.spectatorCamera != null)
            {
                this.spectatorCamera.targetTexture = null;
                UnityEngine.Object.Destroy(this.spectatorCamera.gameObject);
                this.spectatorCamera = null;
            }
            if (this.cameraRenderTexture != null)
            {
                this.cameraRenderTexture.Release();
                UnityEngine.Object.Destroy(this.cameraRenderTexture);
                this.cameraRenderTexture = null;
            }
        }

        private void UpdateCameraSystem()
        {
            if (!this.isCameraActive || this.spectatorCamera == null)
            {
                return;
            }
            Camera mainCamera = this.GetMainGameCamera();
            if (mainCamera == null)
            {
                return;
            }
            this.EnsureCameraRenderTexture();
            LiamShaderEffect shaderEffect = this.spectatorCamera.GetComponent<LiamShaderEffect>();
            if (shaderEffect != null) shaderEffect.Configure(this.shaderEffectsEnabledConfig.Value, this.GetShaderNameForPreset(this.shaderEffectPresetIndexConfig.Value), this.shaderExposureConfig.Value, this.shaderTintStrengthConfig.Value, this.shaderTintColor, this.shaderVignetteConfig.Value);
            if (this.spectatorCamera.targetTexture != this.cameraRenderTexture)
            {
                this.spectatorCamera.targetTexture = this.cameraRenderTexture;
            }
            if (!this.spectatorCamera.gameObject.activeInHierarchy)
            {
                this.spectatorCamera.gameObject.SetActive(true);
            }
            if (this.autoObserverEnabledConfig.Value)
            {
                if (this.IsForestLoaded())
                {
                    this.UpdateAutoObserver();
                    return;
                }
                this.targetPlayerName = "Forest Observer (Forest Only)";
            }
            else if (this.targetRig == null && this.targetPlayerName != "You")
            {
                this.targetPlayerName = "You";
            }
            VRRig activeRig = (this.targetRig != null) ? this.targetRig : ((GorillaTagger.Instance != null) ? GorillaTagger.Instance.offlineVRRig : null);
            if (activeRig == null || activeRig.headMesh == null)
            {
                return;
            }
            this.UpdateAdvancedCameraSystem(activeRig);
        }

        private void UpdateAdvancedCameraSystem(VRRig rig)
        {
            Transform head = rig.headMesh.transform;
            Transform body = rig.transform;
            if (body.localScale != Vector3.zero)
            {
                this.targetRigScale = body.localScale.y;
            }
            float s = this.targetRigScale;
            int mode = Mathf.Clamp(this.cameraModeConfig.Value, 0, modeLabels.Length - 1);

            float fov = this.fovConfig.Value;
            float nearClip = this.nearClipConfig.Value;
            float positionSmooth = this.positionSmoothingConfig.Value;
            float rotationSmooth = this.rotationSmoothingConfig.Value;
            bool smoothing = this.smoothingEnabledConfig.Value;

            if (mode == 2)
            {
                fov = this.eyeFovConfig.Value;
                nearClip = this.eyeNearClipConfig.Value * s;
                positionSmooth = this.eyePositionSmoothingConfig.Value;
                rotationSmooth = this.eyeRotationSmoothingConfig.Value;
                smoothing = true;
                this.cameraTargetPos = head.position + head.forward * (this.eyeForwardConfig.Value * s) + head.up * (this.eyeUpConfig.Value * s);
                this.cameraTargetRot = this.eyeStabilizeRollConfig.Value ? Quaternion.LookRotation(head.forward, Vector3.up) : head.rotation;
            }
            else if (mode == 3)
            {
                Vector3 baseForward = this.frontFollowHeadConfig.Value ? head.forward : body.forward;
                Vector3 flat = Vector3.ProjectOnPlane(baseForward, Vector3.up);
                if (flat.sqrMagnitude < 0.001f)
                {
                    flat = Vector3.ProjectOnPlane(body.forward, Vector3.up);
                }
                if (flat.sqrMagnitude < 0.001f)
                {
                    flat = Vector3.forward;
                }
                flat.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, flat);
                this.cameraTargetPos = head.position + flat * (this.frontDistanceConfig.Value * s) + Vector3.up * (this.frontHeightConfig.Value * s) + right * (this.frontSideConfig.Value * s);
                Vector3 lookPoint = head.position + Vector3.up * (this.frontLookHeightConfig.Value * s);
                Vector3 direction = lookPoint - this.cameraTargetPos;
                this.cameraTargetRot = direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction, Vector3.up) : head.rotation;
            }
            else
            {
                Transform basis = mode == 0 ? head : body;
                this.cameraTargetPos = basis.position + basis.right * (this.xOffsetConfig.Value * s) + basis.up * (this.yOffsetConfig.Value * s) - basis.forward * (this.headDistanceConfig.Value * s);
                this.cameraTargetRot = basis.rotation;
                if (mode == 0 && !this.trackHeadRollConfig.Value)
                {
                    Vector3 euler = this.cameraTargetRot.eulerAngles;
                    euler.z = 0f;
                    this.cameraTargetRot = Quaternion.Euler(euler);
                }
            }

            this.ApplySmoothing(smoothing, positionSmooth, rotationSmooth);
            this.spectatorCamera.fieldOfView = Mathf.Clamp(fov, 20f, 140f);
            this.spectatorCamera.nearClipPlane = Mathf.Max(0.005f, nearClip);
            this.spectatorCamera.enabled = true;
        }

        private void ApplySmoothing(bool enabled, float positionSmooth, float rotationSmooth)
        {
            Transform cameraTransform = this.spectatorCamera.transform;
            if (cameraTransform.parent != null)
            {
                cameraTransform.SetParent(null, true);
            }
            if (!enabled)
            {
                cameraTransform.position = this.cameraTargetPos;
                cameraTransform.rotation = this.cameraTargetRot;
                return;
            }
            positionSmooth = Mathf.Clamp01(positionSmooth);
            rotationSmooth = Mathf.Clamp01(rotationSmooth);
            if (positionSmooth <= 0.001f)
            {
                cameraTransform.position = this.cameraTargetPos;
            }
            else
            {
                float k = Mathf.Clamp01(Mathf.Pow(positionSmooth, 8f) * 2f);
                float rate = Mathf.Lerp(35f, 0.1f, k);
                cameraTransform.position = Vector3.Lerp(cameraTransform.position, this.cameraTargetPos, Mathf.Clamp01(rate * Time.deltaTime));
            }
            if (rotationSmooth <= 0.001f)
            {
                cameraTransform.rotation = this.cameraTargetRot;
            }
            else
            {
                float k = Mathf.Clamp01(Mathf.Pow(rotationSmooth, 8f) * 2f);
                float rate = Mathf.Lerp(30f, 0.08f, k);
                cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, this.cameraTargetRot, Mathf.Clamp01(rate * Time.deltaTime));
            }
        }

        private void UpdateAutoObserver()
        {
            Transform cameraTransform = this.spectatorCamera.transform;
            if (cameraTransform.parent != null)
            {
                cameraTransform.SetParent(null, true);
            }
            float speed = Mathf.Clamp(this.autoObserverSpeedConfig.Value, 0.03f, 0.5f);
            float segmentDuration = Mathf.Lerp(24f, 2.5f, Mathf.InverseLerp(0.03f, 0.5f, speed));
            float tourProgress = Time.unscaledTime / segmentDuration;
            int currentPointIndex = Mathf.FloorToInt(tourProgress) % forestObserverPoints.Length;
            int nextPointIndex = (currentPointIndex + 1) % forestObserverPoints.Length;
            float pointProgress = Mathf.SmoothStep(0f, 1f, tourProgress - Mathf.Floor(tourProgress));
            Vector3 forestOffset = this.GetForestObserverOffset();
            Vector3 lookTarget = new Vector3(-66f, 15f, -80f) + forestOffset;
            float routeScale = Mathf.Clamp(this.autoObserverDistanceConfig.Value, 4f, 30f) / 12f;
            Vector3 routePoint = Vector3.Lerp(forestObserverPoints[currentPointIndex], forestObserverPoints[nextPointIndex], pointProgress) + forestOffset;
            Vector3 desiredPosition = lookTarget + (routePoint - lookTarget) * routeScale;
            Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - desiredPosition, Vector3.up);
            float smoothAmount = 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime);
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, smoothAmount);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, desiredRotation, smoothAmount);
            this.spectatorCamera.fieldOfView = this.fovConfig.Value;
            this.spectatorCamera.nearClipPlane = this.nearClipConfig.Value;
            this.spectatorCamera.enabled = true;
            this.targetPlayerName = "Forest Observer";
        }

        private bool IsForestLoaded()
        {
            if (Time.unscaledTime < this.nextForestCheckTime)
            {
                return this.forestLoadedCached;
            }
            this.nextForestCheckTime = Time.unscaledTime + 1f;
            bool loaded = false;
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.isLoaded && string.Equals(scene.name, "Forest", StringComparison.OrdinalIgnoreCase))
                {
                    loaded = true;
                    break;
                }
            }
            if (!loaded)
            {
                GameObject forest = GameObject.Find("Forest");
                loaded = forest != null && forest.activeInHierarchy;
            }
            this.forestLoadedCached = loaded;
            return loaded;
        }

        private Vector3 GetForestObserverOffset()
        {
            GameObject forest = GameObject.Find("Forest");
            return forest != null ? forest.transform.position : Vector3.zero;
        }

        private void SelectTarget(VRRig rig)
        {
            if (rig == null)
            {
                return;
            }
            this.RestoreTargetLerp();
            this.autoObserverEnabledConfig.Value = false;
            this.targetRig = rig;
            this.targetRigScale = rig.transform.localScale.y > 0f ? rig.transform.localScale.y : 1f;
            this.originalBodyLerp = rig.lerpValueBody;
            this.originalFingerLerp = rig.lerpValueFingers;
            this.lerpCaptured = true;
            this.targetPlayerName = this.GetRigName(rig);
        }

        private void SelectSelf()
        {
            this.RestoreTargetLerp();
            this.autoObserverEnabledConfig.Value = false;
            this.targetRig = null;
            this.targetPlayerName = "You";
        }

        private void RestoreTargetLerp()
        {
            if (this.targetRig != null && this.lerpCaptured)
            {
                this.targetRig.lerpValueBody = this.originalBodyLerp;
                this.targetRig.lerpValueFingers = this.originalFingerLerp;
            }
            this.lerpCaptured = false;
        }

        private void UpdateSpectatorLerp()
        {
            if (!this.isCameraActive || this.targetRig == null || !this.lerpCaptured || this.IsOfflineRig(this.targetRig))
            {
                return;
            }
            if (this.rigLerpEnabledConfig.Value)
            {
                this.targetRig.lerpValueBody = this.rigBodyLerpConfig.Value;
                this.targetRig.lerpValueFingers = this.rigFingerLerpConfig.Value;
            }
            else
            {
                this.targetRig.lerpValueBody = this.originalBodyLerp;
                this.targetRig.lerpValueFingers = this.originalFingerLerp;
            }
        }

        private VRRig GetPlayerRigAt(int slot)
        {
            List<VRRig> list = this.GetAllVRRigs();
            if (list == null)
            {
                return null;
            }
            int current = 0;
            for (int i = 0; i < list.Count; i++)
            {
                VRRig rig = list[i];
                if (rig == null || this.IsOfflineRig(rig))
                {
                    continue;
                }
                if (current == slot)
                {
                    return rig;
                }
                current++;
            }
            return null;
        }

        private void ProcessAutoRef()
        {
            if (!this.autoRefEnabledConfig.Value || !this.isCameraActive || this.autoObserverEnabledConfig.Value)
            {
                return;
            }
            if (Time.unscaledTime < this.nextAutoRefTime)
            {
                return;
            }
            this.nextAutoRefTime = Time.unscaledTime + Mathf.Clamp(this.autoRefIntervalConfig.Value, 3f, 60f);
            List<VRRig> players = this.cachedVRRigs.Where(rig => rig != null && !this.IsOfflineRig(rig)).ToList();
            if (players.Count == 0)
            {
                return;
            }
            int current = this.targetRig == null ? -1 : players.IndexOf(this.targetRig);
            int next = (current + 1) % players.Count;
            this.SelectTarget(players[next]);
        }

        private void ProcessPlayerSwitching()
        {
            if (Keyboard.current == null || !this.isCameraActive || this.typing)
            {
                return;
            }
            Keyboard keyboard = Keyboard.current;
            if (keyboard.mKey.wasPressedThisFrame)
            {
                this.cameraModeConfig.Value = (Mathf.Clamp(this.cameraModeConfig.Value, 0, modeLabels.Length - 1) + 1) % modeLabels.Length;
            }
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                this.SelectSelf();
                return;
            }
            int slot = this.GetNumberKeyPressed();
            if (slot < 0)
            {
                return;
            }
            VRRig rig = this.GetPlayerRigAt(slot);
            if (rig != null)
            {
                this.SelectTarget(rig);
            }
        }

        private int GetNumberKeyPressed()
        {
            Keyboard k = Keyboard.current;
            if (k.digit2Key.wasPressedThisFrame) return 0;
            if (k.digit3Key.wasPressedThisFrame) return 1;
            if (k.digit4Key.wasPressedThisFrame) return 2;
            if (k.digit5Key.wasPressedThisFrame) return 3;
            if (k.digit6Key.wasPressedThisFrame) return 4;
            if (k.digit7Key.wasPressedThisFrame) return 5;
            if (k.digit8Key.wasPressedThisFrame) return 6;
            if (k.digit9Key.wasPressedThisFrame) return 7;
            if (k.digit0Key.wasPressedThisFrame) return 8;
            return -1;
        }

        private void PreventAFKKick()
        {
            if (PhotonNetwork.InRoom && PhotonNetworkController.Instance != null)
            {
                PhotonNetworkController.Instance.disableAFKKick = true;
            }
        }

        private void JoinRoom()
        {
            if (string.IsNullOrEmpty(this.roomCodeInput) || PhotonNetworkController.Instance == null)
            {
                return;
            }
            string code = this.roomCodeInput.ToUpper().Trim();
            PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(code, 0);
        }

        private void DisconnectFromRoom()
        {
            PhotonNetwork.Disconnect();
        }
    }

    public class LiamShaderEffect : MonoBehaviour
    {
        private bool effectEnabled;
        private string shaderName = string.Empty;
        private float exposure = 1f;
        private float tintStrength;
        private Color tintColor = Color.white;
        private float vignette;
        private Material effectMaterial;
        private static AssetBundle shaderBundle;
        private static bool shaderBundleLoadAttempted;
        private static readonly Dictionary<string, Shader> loadedShaders = new Dictionary<string, Shader>();

        public void Configure(bool enabled, string selectedShader, float selectedExposure, float selectedTintStrength, Color selectedTint, float selectedVignette)
        {
            effectEnabled = enabled;
            shaderName = selectedShader ?? string.Empty;
            exposure = Mathf.Clamp(selectedExposure, 0.25f, 2f);
            tintStrength = Mathf.Clamp01(selectedTintStrength);
            tintColor = selectedTint;
            vignette = Mathf.Clamp01(selectedVignette);
            Shader selected = ResolveShader(shaderName);
            if (effectMaterial != null && effectMaterial.shader != selected)
            {
                UnityEngine.Object.Destroy(effectMaterial);
                effectMaterial = null;
            }
        }

        private static Shader ResolveShader(string requestedName)
        {
            if (string.IsNullOrEmpty(requestedName)) return null;
            Shader found = Shader.Find(requestedName);
            if (found != null) return found;
            Shader cached;
            if (loadedShaders.TryGetValue(requestedName, out cached)) return cached;
            if (!shaderBundleLoadAttempted)
            {
                shaderBundleLoadAttempted = true;
                string[] candidates = new string[]
                {
                    Path.Combine(Paths.PluginPath, "LiamCasting", "Shaders", "TUPshaders"),
                    Path.Combine(Paths.PluginPath, "LiamCasting", "Shaders", "TUPshaders.bundle"),
                    Path.Combine(Paths.PluginPath, "LiamCasting", "TUPshaders")
                };
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (!File.Exists(candidates[i])) continue;
                    try
                    {
                        shaderBundle = AssetBundle.LoadFromFile(candidates[i]);
                        if (shaderBundle != null) break;
                    }
                    catch
                    {
                    }
                }
                if (shaderBundle != null)
                {
                    Shader[] shaders = shaderBundle.LoadAllAssets<Shader>();
                    for (int i = 0; i < shaders.Length; i++)
                    {
                        if (shaders[i] != null) loadedShaders[shaders[i].name] = shaders[i];
                    }
                }
            }
            loadedShaders.TryGetValue(requestedName, out found);
            return found;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!effectEnabled || string.IsNullOrEmpty(shaderName))
            {
                Graphics.Blit(source, destination);
                return;
            }
            Shader shader = ResolveShader(shaderName);
            if (shader == null || !shader.isSupported)
            {
                Graphics.Blit(source, destination);
                return;
            }
            if (effectMaterial == null || effectMaterial.shader != shader)
            {
                if (effectMaterial != null) UnityEngine.Object.Destroy(effectMaterial);
                effectMaterial = new Material(shader);
            }
            if (effectMaterial.HasProperty("_Exposure")) effectMaterial.SetFloat("_Exposure", exposure);
            if (effectMaterial.HasProperty("_Intensity")) effectMaterial.SetFloat("_Intensity", tintStrength);
            if (effectMaterial.HasProperty("_Tint")) effectMaterial.SetColor("_Tint", tintColor);
            if (effectMaterial.HasProperty("_Vignette")) effectMaterial.SetFloat("_Vignette", vignette);
            if (effectMaterial.HasProperty("_Strength")) effectMaterial.SetFloat("_Strength", tintStrength);
            Graphics.Blit(source, destination, effectMaterial);
        }

        private void OnDestroy()
        {
            if (effectMaterial != null)
            {
                UnityEngine.Object.Destroy(effectMaterial);
                effectMaterial = null;
            }
        }
    }
}