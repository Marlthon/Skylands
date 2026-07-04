using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace BiomeManager;

public enum BiomeArea
{
    Edge = 1,
    Median = 2,
    Everything = 3
}

public sealed class CustomBiome
{
    public readonly string Id;
    internal string DisplayName;

    internal float MinDistance = 0f;
    internal float MaxDistance = 1f;
    internal float MinAltitude = -1000f;
    internal float MaxAltitude = 10000f;
    internal float CenterX = 0f, CenterY = 0f;
    internal float Amount = 1f, Stretch = 1f;
    internal float WiggleDistanceWidth = 100f;
    internal float WiggleDistanceLength = 20f;

    internal string Terrain = "", Nature = "";
    internal float AltitudeMultiplier = 1f;
    internal float WaterDepthMultiplier = 1f;
    internal float AltitudeDelta = 0f;
    internal float MinimumAltitude = -1000f, MaximumAltitude = 10000f;
    internal float ExcessFactor = 0.5f;
    internal float ForestMultiplier = 1f;

    internal Color MapColor = new Color(0, 0, 0, 0);
    internal Color TerrainColor = new Color(0, 0, 0, 0);

    internal readonly List<(string name, float weight, bool ash, bool deep)> Environments = new();
    internal string MusicDay = "", MusicNight = "", MusicMorning = "morning", MusicEvening = "evening";

    internal readonly List<ClutterEntry> Clutters = new();
    internal readonly List<VegetationEntry> Vegetations = new();
    public CustomBiome(string id) { Id = id.ToLowerInvariant(); DisplayName = id; }

    public CustomBiome SetDisplayName(string v) { DisplayName = v; return this; }
    public CustomBiome SetMinDistance(float v) { MinDistance = v; return this; }
    public CustomBiome SetMaxDistance(float v) { MaxDistance = v; return this; }
    public CustomBiome SetMinAltitude(float v) { MinAltitude = v; return this; }
    public CustomBiome SetMaxAltitude(float v) { MaxAltitude = v; return this; }
    public CustomBiome SetCenter(float x, float y) { CenterX = x; CenterY = y; return this; }
    public CustomBiome SetAmount(float v) { Amount = Mathf.Clamp01(v); return this; }
    public CustomBiome SetStretch(float v) { Stretch = v; return this; }
    public CustomBiome SetWiggleDistance(bool v) { if (!v) WiggleDistanceWidth = 0f; return this; }

    public CustomBiome SetTerrain(string v) { Terrain = v.ToLowerInvariant(); return this; }
    public CustomBiome SetNature(string v) { Nature = v.ToLowerInvariant(); return this; }
    public CustomBiome SetAltitudeMultiplier(float v) { AltitudeMultiplier = v; return this; }
    public CustomBiome SetWaterDepthMultiplier(float v) { WaterDepthMultiplier = v; return this; }
    public CustomBiome SetAltitudeDelta(float v) { AltitudeDelta = v; return this; }
    public CustomBiome SetForestMultiplier(float v) { ForestMultiplier = v; return this; }
    public CustomBiome SetAltitudeLimits(float min, float max, float excess = 0.5f)
    { MinimumAltitude = min; MaximumAltitude = max; ExcessFactor = excess; return this; }

    public CustomBiome SetMapColor(float r, float g, float b, float a = 1f)
    { MapColor = new Color(r, g, b, a); return this; }
    public CustomBiome SetTerrainColor(float r, float g, float b, float a = 1f)
    { TerrainColor = new Color(r, g, b, a); return this; }

    private static Color HexToColor(string hex, float a = 1f)
    {
        hex = hex.TrimStart('#');
        float r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
        float g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
        float b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
        return new Color(r, g, b, a);
    }
    public CustomBiome SetMapColorHex(string hex, float a = 1f)
    { MapColor = HexToColor(hex, a); return this; }
    public CustomBiome SetTerrainColorHex(string hex, float a = 1f)
    { TerrainColor = HexToColor(hex, a); return this; }

    public CustomBiome AddEnvironment(string name, float weight = 1f,
        bool ashOverride = false, bool deepOverride = false)
    { Environments.Add((name, weight, ashOverride, deepOverride)); return this; }
    public CustomBiome SetMusicDay(string v) { MusicDay = v; return this; }
    public CustomBiome SetMusicNight(string v) { MusicNight = v; return this; }
    public CustomBiome SetMusicMorning(string v) { MusicMorning = v; return this; }
    public CustomBiome SetMusicEvening(string v) { MusicEvening = v; return this; }

    public CustomBiome AddClutter(string prefab, int amount = 10,
        float scaleMin = 1f, float scaleMax = 1f, bool inForest = true,
        float forestThresholdMin = 0f, float forestThresholdMax = 1f,
        float minAlt = -1000f, float maxAlt = 1000f,
        float minTilt = 0f, float maxTilt = 35f,
        bool snapToWater = false, float randomOffset = 0.5f)
    {
        Clutters.Add(new ClutterEntry(prefab, amount, scaleMin, scaleMax, inForest,
            forestThresholdMin, forestThresholdMax, minAlt, maxAlt,
            minTilt, maxTilt, snapToWater, randomOffset));
        return this;
    }

    public CustomBiome AddVegetation(string prefab, bool enabled = true, float min = 1f, float max = 1f,
        float scaleMin = 1f, float scaleMax = 1f, float randTilt = 0f,
        BiomeArea biomeArea = BiomeArea.Everything,
        bool forcePlacement = false, float minAltitude = 0f, float maxAltitude = 1000f,
        float maxOceanDepth = 0f, float maxTilt = 90f, float maxTerrainDelta = 2f,
        float forestThresholdMin = 0f, float forestThresholdMax = 1f,
        int groupSizeMin = 1, int groupSizeMax = 1, float groupRadius = 0f, bool inForest = false)
    {
        Vegetations.Add(new VegetationEntry(prefab, enabled, min, max, scaleMin, scaleMax, randTilt,
            biomeArea, forcePlacement, minAltitude, maxAltitude, maxOceanDepth, maxTilt, maxTerrainDelta,
            forestThresholdMin, forestThresholdMax, groupSizeMin, groupSizeMax, groupRadius, inForest));
        return this;
    }

    public void Register() => BiomeManager.Register(this);
}

public sealed class ClutterEntry
{
    public readonly string Prefab;
    public readonly int Amount;
    public readonly float ScaleMin, ScaleMax;
    public readonly bool InForest;
    public readonly float ForestThresholdMin, ForestThresholdMax;
    public readonly float MinAlt, MaxAlt, MinTilt, MaxTilt;
    public readonly bool SnapToWater;
    public readonly float RandomOffset;
    public ClutterEntry(string p, int a, float sMin, float sMax, bool inf, float ftMin, float ftMax,
        float minA, float maxA, float minT, float maxT, bool snap, float roff)
    {
        Prefab = p; Amount = a; ScaleMin = sMin; ScaleMax = sMax; InForest = inf;
        ForestThresholdMin = ftMin; ForestThresholdMax = ftMax;
        MinAlt = minA; MaxAlt = maxA; MinTilt = minT; MaxTilt = maxT;
        SnapToWater = snap; RandomOffset = roff;
    }
}

public sealed class VegetationEntry
{
    public readonly string Prefab;
    public readonly bool Enabled;
    public readonly float Min, Max;
    public readonly float ScaleMin, ScaleMax;
    public readonly float RandTilt;
    public readonly BiomeArea BiomeArea;
    public readonly bool ForcePlacement;
    public readonly float MinAltitude, MaxAltitude;
    public readonly float MaxOceanDepth;
    public readonly float MaxTilt;
    public readonly float MaxTerrainDelta;
    public readonly float ForestThresholdMin, ForestThresholdMax;
    public readonly int GroupSizeMin, GroupSizeMax;
    public readonly float GroupRadius;
    public readonly bool InForest;

    public VegetationEntry(string prefab, bool enabled, float min, float max, float scaleMin, float scaleMax,
        float randTilt, BiomeArea biomeArea, bool forcePlacement, float minAltitude, float maxAltitude,
        float maxOceanDepth, float maxTilt, float maxTerrainDelta, float forestThresholdMin, float forestThresholdMax,
        int groupSizeMin, int groupSizeMax, float groupRadius, bool inForest)
    {
        Prefab = prefab; Enabled = enabled; Min = min; Max = max; ScaleMin = scaleMin; ScaleMax = scaleMax;
        RandTilt = randTilt; BiomeArea = biomeArea; ForcePlacement = forcePlacement;
        MinAltitude = minAltitude; MaxAltitude = maxAltitude; MaxOceanDepth = maxOceanDepth;
        MaxTilt = maxTilt; MaxTerrainDelta = maxTerrainDelta; ForestThresholdMin = forestThresholdMin;
        ForestThresholdMax = forestThresholdMax; GroupSizeMin = groupSizeMin; GroupSizeMax = groupSizeMax;
        GroupRadius = groupRadius; InForest = inForest;
    }
}

public static class BiomeManager
{
    private const string KEY_PRIMARY = "BiomeManager_Primary_v3";
    private const string KEY_LIST = "BiomeManager_List_v3";
    private const string HARMONY_ID = "com.biomemanager.v3";

    private static readonly string _myId = typeof(BiomeManager).Assembly.FullName!;
    private static bool _patchesApplied;
    private static bool? _isPrimary;
    private static BiomeManagerRunner? _runner;
    internal static BiomeManagerRunner Runner
    {
        get
        {
            if (_runner != null) return _runner;
            var go = new GameObject("BiomeManager_Runner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<BiomeManagerRunner>();
            return _runner;
        }
    }

    private static List<CustomBiome> SharedList
    {
        get
        {
            if (AppDomain.CurrentDomain.GetData(KEY_LIST) is not List<CustomBiome> list)
            {
                list = new();
                AppDomain.CurrentDomain.SetData(KEY_LIST, list);
            }
            return list;
        }
    }

    internal static bool IsPrimary
    {
        get
        {
            if (_isPrimary.HasValue) return _isPrimary.Value;
            _isPrimary = (AppDomain.CurrentDomain.GetData(KEY_PRIMARY) as string) == _myId;
            return _isPrimary.Value;
        }
    }

    internal static readonly Dictionary<string, Heightmap.Biome> NameToEnum = new(StringComparer.OrdinalIgnoreCase);
    internal static readonly Dictionary<Heightmap.Biome, string> EnumToName = new();
    internal static readonly Dictionary<Heightmap.Biome, CustomBiome> BiomeData = new();
    internal static readonly Dictionary<Heightmap.Biome, Heightmap.Biome> BiomeToTerrain = new();
    internal static readonly Dictionary<Heightmap.Biome, Heightmap.Biome> BiomeToNature = new();
    internal static readonly Dictionary<Heightmap.Biome, float> Offsets = new();

    private static readonly (string n, Heightmap.Biome v)[] Vanilla =
    {
        ("None",Heightmap.Biome.None),("Meadows",Heightmap.Biome.Meadows),
        ("Swamp",Heightmap.Biome.Swamp),("Mountain",Heightmap.Biome.Mountain),
        ("BlackForest",Heightmap.Biome.BlackForest),("Plains",Heightmap.Biome.Plains),
        ("AshLands",Heightmap.Biome.AshLands),("DeepNorth",Heightmap.Biome.DeepNorth),
        ("Ocean",Heightmap.Biome.Ocean),("Mistlands",Heightmap.Biome.Mistlands),
    };

    public static void Register(CustomBiome biome)
    {
        var list = SharedList;
        if (list.Any(b => b.Id == biome.Id))
            throw new InvalidOperationException($"[BiomeManager] '{biome.Id}' already registered.");
        list.Add(biome);

        if (_patchesApplied) return;
        _patchesApplied = true;

        if (AppDomain.CurrentDomain.GetData(KEY_PRIMARY) == null)
            AppDomain.CurrentDomain.SetData(KEY_PRIMARY, _myId);
        _isPrimary = (AppDomain.CurrentDomain.GetData(KEY_PRIMARY) as string) == _myId;

        SetupBiomeArrays();

        var h = new Harmony(HARMONY_ID);
        foreach (var t in typeof(BiomeManager).Assembly.GetTypes()
            .Where(t => t.Namespace == "BiomeManager"))
        {
            try { h.CreateClassProcessor(t).Patch(); }
            catch (Exception e) { Log($"Patch failed {t.Name}: {e.Message}"); }
        }
    }

    public static void SetHost(BepInEx.BaseUnityPlugin host) { /* Runner auto-criado. SetHost mantido para compatibilidade. */ }

    private static bool _built = false;

    internal static void Build()
    {
        if (_built) return;
        _built = true;

        NameToEnum.Clear();
        EnumToName.Clear();
        BiomeData.Clear();
        BiomeToTerrain.Clear();
        BiomeToNature.Clear();

        foreach (var (n, v) in Vanilla)
        {
            NameToEnum[n] = v;
            EnumToName[v] = n;
        }

        var custom = SharedList.OrderBy(b => b.Id).ToList();
        var lastBiome = Heightmap.Biome.Mistlands;

        foreach (var b in custom)
        {
            lastBiome = NextBiome(lastBiome);
            NameToEnum[b.Id] = lastBiome;
            EnumToName[lastBiome] = b.Id;
            BiomeData[lastBiome] = b;
        }

        foreach (var b in custom)
        {
            var e = NameToEnum[b.Id];
            var terrain = !string.IsNullOrEmpty(b.Terrain) && NameToEnum.TryGetValue(b.Terrain, out var t) ? t : e;
            BiomeToTerrain[e] = terrain;
            var nature = !string.IsNullOrEmpty(b.Nature) && NameToEnum.TryGetValue(b.Nature, out var n) ? n : terrain;
            BiomeToNature[e] = nature;
        }

        if (Localization.instance != null)
        {
            foreach (var b in custom)
            {
                var e = NameToEnum[b.Id];
                var key = "biome_" + b.Id;
                var keyNum = "biome_" + ((int)e).ToString();
                Localization.instance.m_translations[key] = b.DisplayName;
                Localization.instance.m_translations[keyNum] = b.DisplayName;
            }
        }

        Log($"Registered {custom.Count} custom biome(s).");
    }

    internal static void InvokeRegenerate()
    {
        if (!IsPrimary) return;
        if (WorldGenerator.instance?.m_world?.m_menu != false) return;
        Runner.ScheduleRegenerate();
    }
    // private static void DoRegenerate() => AutomaticRegenerate();

    internal static float _playerSpawnTime = -1f;

    internal static void AutomaticRegenerate()
    {
        if (WorldGenerator.instance == null) return;
        Log("Regenerating the world.");
        WorldGenerator.instance.Pregenerate();
        foreach (var hm in UnityEngine.Object.FindObjectsByType<Heightmap>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            hm.m_buildData = null;
            hm.Regenerate();
        }
        LoadClutter();
        ClutterSystem.instance?.ClearAll();

        // S� regenera o minimapa se: n�o for headless, player spawnou
        // E j� passou tempo suficiente desde o spawn inicial (evita a segunda
        // gera��o causada pelo ConfigSync terminando logo ap�s o loading).
        bool graphicsOk = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
        bool playerReady = Player.m_localPlayer != null;
        bool pastCooldown = _playerSpawnTime > 0f && (Time.time - _playerSpawnTime) > 120f;

        if (graphicsOk && playerReady && pastCooldown)
            Minimap.instance?.GenerateWorldMap();
    }

    internal static void LoadEnvironments()
    {
        if (BiomeData.Count == 0) return;
        var em = EnvMan.instance;
        if (em == null) return;

        foreach (var kvp in BiomeData)
        {
            var enumVal = kvp.Key;
            var b = kvp.Value;
            if (b.Environments.Count == 0 && string.IsNullOrEmpty(b.MusicDay)) continue;

            em.m_biomes.RemoveAll(x => x.m_biome == enumVal);

            em.AppendBiomeSetup(new BiomeEnvSetup
            {
                m_name = b.Id,
                m_biome = enumVal,
                m_musicDay = b.MusicDay,
                m_musicNight = b.MusicNight,
                m_musicMorning = b.MusicMorning,
                m_musicEvening = b.MusicEvening,
                m_environments = b.Environments.Select(e => new EnvEntry
                {
                    m_environment = e.name,
                    m_weight = e.weight,
                    m_ashlandsOverride = e.ash,
                    m_deepnorthOverride = e.deep,
                }).ToList()
            });
        }
        em.m_environmentPeriod = -1;
        em.m_firstEnv = true;
        Log("Environments (Clima/Musica) registrados com sucesso.");
    }

    internal static void LoadClutter()
    {
        if (BiomeData.Count == 0) return;
        var cs = ClutterSystem.instance;
        if (cs == null) return;

        foreach (var kvp in BiomeData)
        {
            var enumVal = kvp.Key;
            var b = kvp.Value;
            cs.m_clutter.RemoveAll(c => c.m_biome == enumVal);

            foreach (var cd in b.Clutters)
            {
                GameObject prefab = null;
                string clutterName = cd.Prefab;

                foreach (var c in cs.m_clutter)
                {
                    if (c.m_prefab != null && c.m_prefab.name == cd.Prefab)
                    {
                        prefab = c.m_prefab;
                        if (!string.IsNullOrEmpty(c.m_name)) clutterName = c.m_name;
                        break;
                    }
                }

                if (prefab == null)
                    prefab = ZNetScene.instance?.GetPrefab(cd.Prefab);

                if (prefab == null)
                {
                    Log($"Clutter prefab not found: {cd.Prefab}");
                    continue;
                }

                bool instanced = false;
                foreach (var vc in cs.m_clutter)
                    if (vc.m_prefab != null && vc.m_prefab.name == cd.Prefab)
                    { instanced = vc.m_instanced; break; }
                if (!instanced) instanced = cd.Prefab.StartsWith("instanced_");

                cs.m_clutter.Add(new ClutterSystem.Clutter
                {
                    m_name = clutterName,
                    m_prefab = prefab,
                    m_biome = enumVal,
                    m_enabled = true,
                    m_instanced = instanced,
                    m_amount = cd.Amount,
                    m_scaleMin = cd.ScaleMin,
                    m_scaleMax = cd.ScaleMax,
                    m_inForest = cd.InForest,
                    m_forestTresholdMin = cd.ForestThresholdMin,
                    m_forestTresholdMax = cd.ForestThresholdMax,
                    m_minAlt = cd.MinAlt,
                    m_maxAlt = cd.MaxAlt,
                    m_minTilt = cd.MinTilt,
                    m_maxTilt = cd.MaxTilt,
                    m_snapToWater = cd.SnapToWater,
                    m_randomOffset = cd.RandomOffset,
                });
            }
        }
        cs.ClearAll();
        Log("Clutter registrado.");
    }

    internal static void LoadVegetation()
    {
        if (BiomeData.Count == 0) return;
        var zs = ZoneSystem.instance;
        if (zs == null) return;

        foreach (var kvp in BiomeData)
        {
            var enumVal = kvp.Key;
            var b = kvp.Value;

            foreach (var vd in b.Vegetations)
            {
                var prefab = ZNetScene.instance?.GetPrefab(vd.Prefab);
                if (prefab == null)
                {
                    Log($"Vegetation prefab not found: {vd.Prefab}");
                    continue;
                }

                zs.m_vegetation.RemoveAll(v => v.m_prefab == prefab && v.m_biome == enumVal);

                zs.m_vegetation.Add(new ZoneSystem.ZoneVegetation
                {
                    m_prefab = prefab,
                    m_enable = vd.Enabled,
                    m_min = vd.Min,
                    m_max = vd.Max,
                    m_scaleMin = vd.ScaleMin,
                    m_scaleMax = vd.ScaleMax,
                    m_randTilt = vd.RandTilt,
                    m_biome = enumVal,
                    m_biomeArea = (Heightmap.BiomeArea)vd.BiomeArea,
                    m_forcePlacement = vd.ForcePlacement,
                    m_minAltitude = vd.MinAltitude,
                    m_maxAltitude = vd.MaxAltitude,
                    m_maxOceanDepth = vd.MaxOceanDepth,
                    m_maxTilt = vd.MaxTilt,
                    m_maxTerrainDelta = vd.MaxTerrainDelta,
                    m_forestTresholdMin = vd.ForestThresholdMin,
                    m_forestTresholdMax = vd.ForestThresholdMax,
                    m_groupSizeMin = vd.GroupSizeMin,
                    m_groupSizeMax = vd.GroupSizeMax,
                    m_groupRadius = vd.GroupRadius,
                    m_inForest = vd.InForest
                });
            }
        }
        Log("Vegetation registered.");
    }

    internal static Heightmap.Biome GetBiomeAt(WorldGenerator wg, float wx, float wy)
    {
        if (BiomeData.Count == 0) return Heightmap.Biome.None;

        float worldAngle = Mathf.Atan2(wx, wy);
        float sx = wx * WorldInfo.Stretch;
        float sy = wy * WorldInfo.Stretch;
        float magnitude = new Vector2(sx, sy).magnitude;
        if (magnitude > WorldInfo.TotalRadius) return Heightmap.Biome.None;

        float altitude = wg.GetBaseHeight(wx, wy, false) * 200f - WorldInfo.WaterLevel;
        float radius = WorldInfo.Radius;
        float bx = wx / WorldInfo.BiomeStretch;
        float by = wy / WorldInfo.BiomeStretch;

        foreach (var b in BiomeData.Values.OrderBy(x => x.Id))
        {
            if (b.MinAltitude >= altitude || b.MaxAltitude <= altitude) continue;

            float mag = magnitude;
            float min = b.MinDistance * radius;
            float max = b.MaxDistance * radius;

            if (b.CenterX != 0f || b.CenterY != 0f)
                mag = new Vector2(sx - b.CenterX * radius, sy - b.CenterY * radius).magnitude;

            if (min > 0f && b.WiggleDistanceWidth > 0f)
                min += Mathf.Sin(worldAngle * b.WiggleDistanceLength) * b.WiggleDistanceWidth;
            else if (min == 0f)
                min = -0.1f;

            if (!(mag > min && (max >= radius || mag < max))) continue;

            if (b.Amount < 1f && NameToEnum.TryGetValue(b.Id, out var eOff))
            {
                float off = Offsets.TryGetValue(eOff, out var o) ? o : 0f;
                if (Mathf.PerlinNoise((off + bx / b.Stretch) * 0.001f,
                                      (off + by / b.Stretch) * 0.001f) <= 1f - b.Amount)
                    continue;
            }

            if (NameToEnum.TryGetValue(b.Id, out var result)) return result;
        }
        return Heightmap.Biome.None;
    }

    internal static Heightmap.Biome GetTerrain(Heightmap.Biome biome) =>
        BiomeToTerrain.TryGetValue(biome, out var t) ? t : biome;

    internal static Heightmap.Biome GetNature(Heightmap.Biome biome) =>
        BiomeToNature.TryGetValue(biome, out var n) ? n : biome;

    internal static bool TryGetBiome(string name, out Heightmap.Biome b) =>
        NameToEnum.TryGetValue(name, out b);

    internal static bool TryGetData(Heightmap.Biome biome, out CustomBiome data) =>
        BiomeData.TryGetValue(biome, out data!);

    private static void SetupBiomeArrays()
    {
        var weights = new float[33];
        var idx2b = weights.Select((_, i) =>
            (Heightmap.Biome)(i < 2 ? i : 2 << (i - 2))).ToArray();
#pragma warning disable CS8500
        unsafe
        {
            fixed (void* p = &Heightmap.s_indexToBiome) *(object*)p = idx2b;
            fixed (void* p = &Heightmap.s_tempBiomeWeights) *(object*)p = weights;
        }
#pragma warning restore CS8500
        for (int i = 0; i < idx2b.Length; i++)
            Heightmap.s_biomeToIndex[idx2b[i]] = i;
    }

    private static Heightmap.Biome NextBiome(Heightmap.Biome b)
    {
        uint n = (uint)b;
        if (n == 0x80) throw new Exception("[BiomeManager] Max biomes reached.");
        if (n == 0x80000000) return (Heightmap.Biome)0x80;
        return (Heightmap.Biome)(n * 2);
    }

    internal static void Log(string msg) => Debug.Log($"[BiomeManager] {msg}");
}

internal static class WorldInfo
{
    public static float WaterLevel = 30f;
    public static float Radius = 10000f;
    public static float TotalRadius = 10500f;
    public static float Stretch = 1f;
    public static float BiomeStretch = 1f;
}


// =============================================================================
// HARMONY PATCHES
// =============================================================================

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.VersionSetup))]
[HarmonyPriority(Priority.VeryLow)]
internal static class Patch_VersionSetup
{
    static void Postfix()
    {
        if (!BiomeManager.IsPrimary) return;

        BiomeManager.Build();
    }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
[HarmonyPriority(Priority.VeryLow)]
internal static class Patch_ZoneSystem_Start
{
    internal static bool _loaded = false;

    static void Postfix()
    {
        if (!BiomeManager.IsPrimary) return;
        if (BiomeManager.BiomeData.Count == 0) return;
        if (_loaded) return;
        _loaded = true;

        BiomeManager.LoadEnvironments();
        BiomeManager.LoadClutter();
        BiomeManager.LoadVegetation();
    }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.OnDestroy))]
internal static class Patch_ZoneSystem_OnDestroy
{
    static void Postfix() => Patch_ZoneSystem_Start._loaded = false;
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
internal static class Patch_ZNetTimeout
{
    static void Postfix()
    {
        if (ZRpc.m_timeout < 90f)
            ZRpc.m_timeout = 90f;
        // Reseta o spawn time ao entrar em nova sess�o
        BiomeManager._playerSpawnTime = -1f;
    }
}

// =============================================================================
// Minimap assíncrono — elimina o freeze na tela de loading.
//
// Estratégia (igual ao mod do Jere):
//   1. Bloqueia o GenerateWorldMap durante o loading (Player ainda não spawnou).
//      -> O jogo carrega normalmente sem freeze.
//   2. Quando o player spawna, inicia a geração do minimapa em coroutine.
//      -> Minimapa fica em background por ~30s, sem travar o jogo.
//      -> No final aplica tudo de uma vez (travadinha de ~1s).
// =============================================================================

[HarmonyPatch(typeof(Minimap), "GenerateWorldMap")]
internal static class Patch_GenerateWorldMap_Async
{
    private static readonly AccessTools.FieldRef<Minimap, Texture2D> _mapTexRef =
        AccessTools.FieldRefAccess<Minimap, Texture2D>("m_mapTexture");
    private static readonly AccessTools.FieldRef<Minimap, Texture2D> _forestRef =
        AccessTools.FieldRefAccess<Minimap, Texture2D>("m_forestMaskTexture");
    private static readonly AccessTools.FieldRef<Minimap, Texture2D> _heightRef =
        AccessTools.FieldRefAccess<Minimap, Texture2D>("m_heightTexture");

    private static readonly Func<Minimap, Heightmap.Biome, Color> _getPixelColor =
        AccessTools.MethodDelegate<Func<Minimap, Heightmap.Biome, Color>>(
            AccessTools.Method(typeof(Minimap), "GetPixelColor", new[] { typeof(Heightmap.Biome) }));

    private static readonly Func<Minimap, float, float, float, Heightmap.Biome, Color> _getMaskColor =
        AccessTools.MethodDelegate<Func<Minimap, float, float, float, Heightmap.Biome, Color>>(
            AccessTools.Method(typeof(Minimap), "GetMaskColor",
                new[] { typeof(float), typeof(float), typeof(float), typeof(Heightmap.Biome) }));

    private static readonly Action<Minimap, Texture2D, Texture2D, Texture2D> _saveTextures =
        AccessTools.MethodDelegate<Action<Minimap, Texture2D, Texture2D, Texture2D>>(
            AccessTools.Method(typeof(Minimap), "SaveMapTextureDataToDisk"));

    internal static Coroutine _activeCoroutine;

    static bool Prefix()
    {
        if (!BiomeManager.IsPrimary || BiomeManager.BiomeData.Count == 0)
        {
            return true;
        }

        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            return false;
        }

        if (Player.m_localPlayer == null)
        {
            return false;
        }

        if (_activeCoroutine != null)
            BiomeManager.Runner.StopCoroutine(_activeCoroutine);
        _activeCoroutine = BiomeManager.Runner.StartCoroutine(
            GenerateAsync(Minimap.instance));
        return false;
    }

    internal static System.Collections.IEnumerator GenerateAsync(Minimap instance)
    {
        if (instance == null) { ZLog.LogWarning("[BiomeManager] GenerateAsync: instance é null, abortando"); yield break; }

        float waited = 0f;
        while (WorldGenerator.instance == null || ZNet.World == null)
        {
            waited += Time.deltaTime;
            if (waited > 60f)
            {
                ZLog.LogWarning("[BiomeManager] GenerateAsync: WorldGenerator não ficou pronto em 60s, abortando.");
                yield break;
            }
            yield return null;
        }

        yield return new WaitForSeconds(1f);

        if (instance == null || WorldGenerator.instance == null || ZNet.World == null)
        {
            ZLog.LogWarning("[BiomeManager] GenerateAsync: referências inválidas após delay, abortando");
            yield break;
        }

        Minimap.DeleteMapTextureData(ZNet.World.m_name);

        int size = instance.m_textureSize;
        float pxSize = instance.m_pixelSize;
        int half = size / 2;
        float halfPx = pxSize / 2f;
        const float num3 = 127.5f;

        // Captura tudo que a thread vai precisar — não pode acessar Unity API de thread secundária
        var wg = WorldGenerator.instance;
        var getPixelColor = _getPixelColor;
        var getMaskColor = _getMaskColor;

        Color32[] colors1 = new Color32[size * size];
        Color32[] colors2 = new Color32[size * size];
        Color[] colors3 = new Color[size * size];
        Color32[] colors4 = new Color32[size * size];

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Roda o cálculo pesado em thread separada — zero impacto no FPS
        bool done = false;
        bool abort = false;
        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float wx = (float)(x - half) * pxSize + halfPx;
                        float wy = (float)(y - half) * pxSize + halfPx;
                        Heightmap.Biome biome = wg.GetBiome(wx, wy);
                        float bh = wg.GetBiomeHeight(biome, wx, wy, out Color _);
                        int idx = y * size + x;

                        colors1[idx] = (Color32)getPixelColor(instance, biome);
                        colors2[idx] = (Color32)getMaskColor(instance, wx, wy, bh, biome);
                        colors3[idx].r = bh;
                        int n = Mathf.Clamp((int)(bh * num3), 0, 65025);
                        colors4[idx] = new Color32((byte)(n >> 8), (byte)(n & 255), 0, 255);
                    }
                }
            }
            catch (System.Exception ex)
            {
                ZLog.LogWarning($"[BiomeManager] GenerateAsync thread erro: {ex.Message}");
                abort = true;
            }
            finally
            {
                done = true;
            }
        });

        // Aguarda a thread terminar sem travar o main thread
        while (!done)
        {
            if (Minimap.instance == null) yield break; // player saiu
            yield return null;
        }

        if (abort) yield break;

        // Aplica as texturas na main thread (único lugar onde pode chamar Unity API)
        Texture2D mapTex = _mapTexRef(instance);
        Texture2D forestTex = _forestRef(instance);
        Texture2D heightTex = _heightRef(instance);

        forestTex.SetPixels32(colors2); forestTex.Apply();
        mapTex.SetPixels32(colors1); mapTex.Apply();
        heightTex.SetPixels(colors3); heightTex.Apply();

        Texture2D ht = new Texture2D(size, size);
        ht.SetPixels32(colors4);
        ht.Apply();

        ZLog.Log($"Generating new world minimap done [{sw.ElapsedMilliseconds}ms] (thread)");

        if (FileHelpers.LocalStorageSupport == LocalStorageSupport.Supported)
            _saveTextures?.Invoke(instance, forestTex, mapTex, ht);

        _activeCoroutine = null;
    }
}

// Quando o player spawna: registra o tempo e inicia a geração do minimapa em background.
[HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
internal static class Patch_Player_OnSpawned
{
    static void Postfix(Player __instance)
    {
        if (__instance != Player.m_localPlayer) return;

        if (BiomeManager._playerSpawnTime < 0f)
            BiomeManager._playerSpawnTime = Time.time;

        if (!BiomeManager.IsPrimary || BiomeManager.BiomeData.Count == 0) return;
        if (Minimap.instance == null) { ZLog.LogWarning("[BiomeManager] OnSpawned: Minimap.instance é null!"); return; }
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;

        if (Patch_GenerateWorldMap_Async._activeCoroutine != null)
            BiomeManager.Runner.StopCoroutine(Patch_GenerateWorldMap_Async._activeCoroutine);

        Patch_GenerateWorldMap_Async._activeCoroutine =
            BiomeManager.Runner.StartCoroutine(
                Patch_GenerateWorldMap_Async.GenerateAsync(Minimap.instance));
    }
}


[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.Initialize))]
internal static class Patch_Initialize
{
    static void Prefix()
    {
        if (!BiomeManager.IsPrimary) return;
        BiomeManager.Offsets.Clear();
    }
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.Pregenerate))]
[HarmonyPriority(Priority.HigherThanNormal)]
internal static class Patch_Pregenerate_ClearCache
{
    static void Prefix(WorldGenerator __instance)
    {
        if (!BiomeManager.IsPrimary) return;
        __instance.m_riverCacheLock.EnterWriteLock();
        __instance.m_riverPoints = new Dictionary<Vector2i, WorldGenerator.RiverPoint[]>();
        __instance.m_rivers = new List<WorldGenerator.River>();
        __instance.m_streams = new List<WorldGenerator.River>();
        __instance.m_lakes = new List<Vector2>();
        __instance.m_cachedRiverGrid = new Vector2i(-999999, -999999);
        __instance.m_cachedRiverPoints = new WorldGenerator.RiverPoint[0];
        __instance.m_riverCacheLock.ExitWriteLock();
    }
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.Pregenerate))]
[HarmonyPriority(Priority.VeryHigh)]
internal static class Patch_Pregenerate_Offsets
{
    static void Prefix(WorldGenerator __instance)
    {
        if (!BiomeManager.IsPrimary) return;
        if (BiomeManager.Offsets.Count > 0) return;

        var oldState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(__instance.m_world.m_seed);

        foreach (var e in BiomeManager.BiomeData.Keys)
            BiomeManager.Offsets[e] = UnityEngine.Random.Range(-10000f, 10000f);

        UnityEngine.Random.state = oldState;
    }
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome),
    typeof(float), typeof(float), typeof(float), typeof(bool))]
internal static class Patch_GetBiome
{
    static bool Prefix(WorldGenerator __instance, float wx, float wy,
        float oceanLevel, bool waterAlwaysOcean, ref Heightmap.Biome __result)
    {
        if (!BiomeManager.IsPrimary) return true;
        if (__instance.m_world.m_menu) return true;
        var custom = BiomeManager.GetBiomeAt(__instance, wx, wy);
        if (custom == Heightmap.Biome.None) return true;
        if (waterAlwaysOcean && __instance.GetHeight(wx, wy) <= oceanLevel)
        { __result = Heightmap.Biome.Ocean; return false; }
        __result = custom;
        return false;
    }
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight))]
internal static class Patch_GetBiomeHeight
{
    static void Prefix(WorldGenerator __instance, ref Heightmap.Biome biome,
        ref Heightmap.Biome __state)
    {
        if (!BiomeManager.IsPrimary) return;
        if (__instance.m_world.m_menu) return;
        __state = biome;
        biome = BiomeManager.GetTerrain(biome);
    }

    static void Postfix(WorldGenerator __instance, Heightmap.Biome __state,
        Heightmap.Biome biome, float wx, float wy, ref Color mask, ref float __result)
    {
        if (!BiomeManager.IsPrimary) return;
        if (__instance.m_world.m_menu) return;
        if (!BiomeManager.TryGetData(__state, out var data)) return;

        float excessSign = Mathf.Sign(data.ExcessFactor);
        float excessFactor = Mathf.Abs(data.ExcessFactor);
        __result -= WorldInfo.WaterLevel;
        __result *= data.AltitudeMultiplier;
        __result += data.AltitudeDelta;
        if (__result < 0f)
            __result *= data.WaterDepthMultiplier;
        if (__result > data.MaximumAltitude)
            __result = data.MaximumAltitude + excessSign * Mathf.Pow(__result - data.MaximumAltitude, excessFactor);
        if (__result < data.MinimumAltitude)
            __result = data.MinimumAltitude - excessSign * Mathf.Pow(data.MinimumAltitude - __result, excessFactor);
        __result += WorldInfo.WaterLevel;
    }
}

[HarmonyPatch(typeof(Heightmap), nameof(Heightmap.GetBiomeColor), typeof(Heightmap.Biome))]
internal static class Patch_GetBiomeColor
{
    static bool Prefix(Heightmap.Biome biome, ref Color32 __result)
    {
        if (!BiomeManager.IsPrimary) return true;
        if (!BiomeManager.TryGetData(biome, out var d)) return true;
        if (d.TerrainColor.a == 0f) return true;
        __result = d.TerrainColor;
        return false;
    }
}

[HarmonyPatch(typeof(Minimap), nameof(Minimap.GetPixelColor))]
internal static class Patch_GetPixelColor
{
    static bool Prefix(Heightmap.Biome biome, ref Color __result)
    {
        if (!BiomeManager.IsPrimary) return true;
        if (!BiomeManager.TryGetData(biome, out var d)) return true;
        if (d.MapColor.a == 0f) return true;
        __result = d.MapColor;
        return false;
    }
}

[HarmonyPatch(typeof(Minimap), nameof(Minimap.GetMaskColor))]
internal static class Patch_GetMaskColor
{
    static void Prefix(ref Heightmap.Biome biome)
    {
        if (!BiomeManager.IsPrimary) return;
        biome = BiomeManager.GetTerrain(biome);
    }
}

[HarmonyPatch(typeof(Heightmap), nameof(Heightmap.GetBiome))]
internal static class Patch_GetBiomeHM
{
    internal static bool UseNature;
    static Heightmap.Biome Postfix(Heightmap.Biome biome)
    { return (BiomeManager.IsPrimary && UseNature) ? BiomeManager.GetNature(biome) : biome; }
}

[HarmonyPatch(typeof(Heightmap), nameof(Heightmap.FindBiome))]
internal static class Patch_FindBiome
{
    static Heightmap.Biome Postfix(Heightmap.Biome biome)
    { return (BiomeManager.IsPrimary && Patch_GetBiomeHM.UseNature) ? BiomeManager.GetNature(biome) : biome; }
}

[HarmonyPatch(typeof(Plant), nameof(Plant.UpdateHealth))]
internal static class Patch_PlantNature
{ static void Prefix() { if (BiomeManager.IsPrimary) Patch_GetBiomeHM.UseNature = true; } static void Finalizer() { Patch_GetBiomeHM.UseNature = false; } }

[HarmonyPatch(typeof(Beehive), nameof(Beehive.CheckBiome))]
internal static class Patch_BeehiveNature
{ static void Prefix() { if (BiomeManager.IsPrimary) Patch_GetBiomeHM.UseNature = true; } static void Finalizer() { Patch_GetBiomeHM.UseNature = false; } }

[HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
internal static class Patch_PlacementNature
{ static void Prefix() { if (BiomeManager.IsPrimary) Patch_GetBiomeHM.UseNature = true; } static void Finalizer() { Patch_GetBiomeHM.UseNature = false; } }

[HarmonyPatch(typeof(Localization), nameof(Localization.Translate))]
internal static class Patch_Translate
{
    static void Postfix(string word, ref string __result)
    {
        if (!BiomeManager.IsPrimary || string.IsNullOrEmpty(word)) return;
        if (word.StartsWith("biome_"))
        {
            string biomeName = word.Substring(6);
            if (BiomeManager.NameToEnum.TryGetValue(biomeName, out var bEnum) &&
                BiomeManager.TryGetData(bEnum, out var data))
            {
                __result = data.DisplayName;
            }
        }
    }
}

[HarmonyPatch]
internal static class Patch_TryParseBiome
{
#pragma warning disable IDE0051
    static IEnumerable<MethodBase> TargetMethods() =>
        typeof(Enum).GetMethods()
            .Where(method => method.Name == "TryParse")
            .Take(2)
            .Select(method => method.MakeGenericMethod(typeof(Heightmap.Biome)))
            .Cast<MethodBase>();

    static bool Prefix(string value, ref Heightmap.Biome result, ref bool __result)
    {
        if (!BiomeManager.IsPrimary) return true;
        __result = BiomeManager.TryGetBiome(value, out result);
        return false;
    }
#pragma warning restore IDE0051
}

[HarmonyPatch(typeof(Enum), nameof(Enum.GetName))]
internal static class Patch_GetName
{
    static bool Prefix(Type enumType, object value, ref string __result)
    {
        if (!BiomeManager.IsPrimary) return true;
        if (enumType == typeof(Heightmap.Biome))
        {
            if (BiomeManager.EnumToName.TryGetValue((Heightmap.Biome)value, out var result))
                __result = result;
            else
                __result = "None";
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(Enum), nameof(Enum.Parse), typeof(Type), typeof(string))]
internal static class Patch_EnumParse
{
    static bool Prefix(Type enumType, string value, ref object __result)
    {
        if (!BiomeManager.IsPrimary) return true;
        if (enumType == typeof(Heightmap.Biome))
        {
            if (BiomeManager.TryGetBiome(value, out var biome))
            {
                __result = biome;
                return false;
            }
        }
        return true;
    }
}


// Runner interno — permite coroutines e Invoke sem depender do plugin externo.
// Criado automaticamente na primeira vez que for necessario.
internal class BiomeManagerRunner : MonoBehaviour
{
    private bool _scheduled = false;

    public void ScheduleRegenerate()
    {
        if (_scheduled) return;
        _scheduled = true;
        Invoke(nameof(DoRegenerate), 1.0f);
    }

    private void DoRegenerate()
    {
        _scheduled = false;
        BiomeManager.AutomaticRegenerate();
    }
}
