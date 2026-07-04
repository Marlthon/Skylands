using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = UnityEngine.Object;
using TypeConverter = BepInEx.Configuration.TypeConverter;

namespace FaunaManager
{
    public enum Toggle { On, Off }
    public enum SpawnOption { Disabled, Default, Custom }
    public enum SpawnTime { Day, Night, Always }
    public enum SpawnArea { Center, Edge, Everywhere }
    public enum Forest { Yes, No, Both }
    public enum DropOption { Disabled, Default, Custom }

    [PublicAPI]
    public struct Range
    {
        public float min;
        public float max;
        public Range(float min, float max) { this.min = min; this.max = max; }
    }

    public class InternalName : Attribute
    {
        public readonly string internalName;
        public InternalName(string internalName) => this.internalName = internalName;
    }

    [PublicAPI]
    public class Fauna
    {
        private readonly HashSet<string> _configuredProperties = new();
        private bool _canSpawn = false;
        private Heightmap.Biome _biome = Heightmap.Biome.Meadows;
        private SpawnArea _spawnArea = SpawnArea.Everywhere;
        private SpawnTime _spawnTime = SpawnTime.Always;
        private Range _groupSize = new(1, 1);
        private float _groupRadius = 3f;
        private int _maximum = 3;
        private float _spawnChance = 100f;
        private int _spawnInterval = 600;
        private Range _altitude = new(5, 1000);
        private float _spawnAltitude = 0.5f;
        private Forest _forest = Forest.Both;
        private bool _canSpawnNearPlayer = false;
        private Range _spawnRadius = new(0, 0);
        private float _spawnDistance = 10f;

        public bool CanSpawn => _canSpawn;
        public Heightmap.Biome Biome => _biome;
        public SpawnArea SpawnArea => _spawnArea;
        public SpawnTime SpawnTime => _spawnTime;
        public Range GroupSize => _groupSize;
        public float GroupRadius => _groupRadius;
        public int Maximum => _maximum;
        public float SpawnChance => _spawnChance;
        public int SpawnInterval => _spawnInterval;
        public Range Altitude => _altitude;
        public float SpawnAltitude => _spawnAltitude;
        public Forest Forest => _forest;
        public bool CanSpawnNearPlayer => _canSpawnNearPlayer;
        public Range SpawnRadius => _spawnRadius;
        public float SpawnDistance => _spawnDistance;

        public bool ConfigurationEnabled = true;
        public readonly GameObject Prefab;
        public DropList Drops = new();

        public bool IsConfigured(string propertyName) => _configuredProperties.Contains(propertyName);

        #region Fluent API
        public Fauna EnableSpawning(bool enabled = true, bool isConfigurable = true)
        { _canSpawn = enabled; if (isConfigurable) _configuredProperties.Add(nameof(CanSpawn)); return this; }

        public Fauna ConfigureBiome(Heightmap.Biome biome, bool isConfigurable = true)
        { _biome = biome; if (isConfigurable) _configuredProperties.Add(nameof(Biome)); return this; }

        public Fauna ConfigureBiomeRaw(int biomeValue, bool isConfigurable = true)
        { _biome = (Heightmap.Biome)biomeValue; if (isConfigurable) _configuredProperties.Add(nameof(Biome)); return this; }

        public Fauna ConfigureSpawnArea(SpawnArea area, bool isConfigurable = true)
        { _spawnArea = area; if (isConfigurable) _configuredProperties.Add(nameof(SpawnArea)); return this; }

        public Fauna ConfigureSpawnTime(SpawnTime time, bool isConfigurable = true)
        { _spawnTime = time; if (isConfigurable) _configuredProperties.Add(nameof(SpawnTime)); return this; }

        public Fauna ConfigureGroupSize(Range size, bool isConfigurable = true)
        { _groupSize = size; if (isConfigurable) _configuredProperties.Add(nameof(GroupSize)); return this; }

        public Fauna ConfigureGroupRadius(float radius, bool isConfigurable = true)
        { _groupRadius = radius; if (isConfigurable) _configuredProperties.Add(nameof(GroupRadius)); return this; }

        public Fauna ConfigureMaximum(int max, bool isConfigurable = true)
        { _maximum = max; if (isConfigurable) _configuredProperties.Add(nameof(Maximum)); return this; }

        public Fauna ConfigureSpawnChance(float chance, bool isConfigurable = true)
        { _spawnChance = chance; if (isConfigurable) _configuredProperties.Add(nameof(SpawnChance)); return this; }

        public Fauna ConfigureSpawnInterval(int seconds, bool isConfigurable = true)
        { _spawnInterval = seconds; if (isConfigurable) _configuredProperties.Add(nameof(SpawnInterval)); return this; }

        public Fauna ConfigureAltitude(Range altitude, bool isConfigurable = true)
        { _altitude = altitude; if (isConfigurable) _configuredProperties.Add(nameof(Altitude)); return this; }

        public Fauna ConfigureSpawnAltitude(float altitude, bool isConfigurable = true)
        { _spawnAltitude = altitude; if (isConfigurable) _configuredProperties.Add(nameof(SpawnAltitude)); return this; }

        public Fauna ConfigureForest(Forest forest, bool isConfigurable = true)
        { _forest = forest; if (isConfigurable) _configuredProperties.Add(nameof(Forest)); return this; }

        public Fauna ConfigureCanSpawnNearPlayer(bool canSpawn = true, bool isConfigurable = true)
        { _canSpawnNearPlayer = canSpawn; if (isConfigurable) _configuredProperties.Add(nameof(CanSpawnNearPlayer)); return this; }

        public Fauna ConfigureSpawnRadius(Range radius, bool isConfigurable = true)
        { _spawnRadius = radius; if (isConfigurable) _configuredProperties.Add(nameof(SpawnRadius)); return this; }

        public Fauna ConfigureSpawnDistance(float distance, bool isConfigurable = true)
        { _spawnDistance = distance; if (isConfigurable) _configuredProperties.Add(nameof(SpawnDistance)); return this; }

        public DropList ConfigureDrops()
        { _configuredProperties.Add(nameof(Drops)); return Drops; }
        #endregion

        public Fauna(string assetBundleFileName, string prefabName, string folderName = "assets")
            : this(FaunaPrefabManager.RegisterAssetBundle(assetBundleFileName, folderName), prefabName) { }

        public Fauna(AssetBundle bundle, string prefabName)
            : this(FaunaPrefabManager.RegisterPrefab(bundle, prefabName)) { }

        public Fauna(GameObject prefab)
        {
            Prefab = prefab;
            registeredFauna.Add(this);
        }

        public FaunaLocalizeKey Localize() => new(Prefab.name);

        // ── Drop List ─────────────────────────────────────────────────────────
        [PublicAPI]
        public class DropList
        {
            internal Dictionary<string, Drop>? drops = null;

            public void None() => drops = new Dictionary<string, Drop>();

            public Drop this[string prefabName] => (drops ??= new Dictionary<string, Drop>()).TryGetValue(prefabName, out Drop drop) ? drop : drops[prefabName] = new Drop();

            internal static void UpdateDrops(Fauna fauna)
            {
                if (!faunaConfigs.ContainsKey(fauna) || faunaConfigs[fauna].Drops.get is null) return;
                DropOption option = faunaConfigs[fauna].Drops.get();
                if (option == DropOption.Default && fauna.Drops.drops is null) return;

                DropOnDestroyed dropComp = fauna.Prefab.GetComponent<DropOnDestroyed>();
                if (dropComp == null) return;

                List<KeyValuePair<string, Drop>> list = faunaConfigs[fauna].Drops.get() switch
                {
                    DropOption.Custom => new SerializedDrops(faunaConfigs[fauna].CustomDrops.get()).Drops,
                    DropOption.Disabled => new List<KeyValuePair<string, Drop>>(),
                    _ => fauna.Drops.drops!.ToList(),
                };

                dropComp.m_dropWhenDestroyed.m_drops.Clear();
                foreach (KeyValuePair<string, Drop> kv in list)
                {
                    if (kv.Key == "" || ZNetScene.instance is null) continue;
                    if (ZNetScene.instance.GetPrefab(kv.Key) is not { } prefab)
                    {
                        Debug.LogWarning($"FaunaManager: item inválido '{kv.Key}' para {fauna.Prefab.name}");
                        continue;
                    }
                    dropComp.m_dropWhenDestroyed.m_drops.Add(new DropTable.DropData
                    {
                        m_item = prefab,
                        m_stackMin = (int)kv.Value.Amount.min,
                        m_stackMax = (int)kv.Value.Amount.max,
                        m_weight = kv.Value.DropChance / 100f,
                        m_dontScale = false,
                    });
                }
                dropComp.m_dropWhenDestroyed.m_dropMin = list.Count;
                dropComp.m_dropWhenDestroyed.m_dropMax = list.Count;
                dropComp.m_dropWhenDestroyed.m_oneOfEach = false;
            }

            internal class SerializedDrops
            {
                public readonly List<KeyValuePair<string, Drop>> Drops;

                public SerializedDrops(DropList drops, Fauna fauna)
                {
                    DropOnDestroyed? dod = fauna.Prefab.GetComponent<DropOnDestroyed>();
                    Drops = (drops.drops ?? dod?.m_dropWhenDestroyed.m_drops.ToDictionary(
                        d => d.m_item.name,
                        d => new Drop { Amount = new Range(d.m_stackMin, d.m_stackMax), DropChance = d.m_weight * 100f }
                    ) ?? new Dictionary<string, Drop>()).ToList();
                }

                public SerializedDrops(string raw)
                {
                    Drops = new List<KeyValuePair<string, Drop>>();
                    foreach (string entry in raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] parts = entry.Trim().Split(':');
                        if (parts.Length < 2) continue;
                        string name = parts[0].Trim();
                        string[] vals = parts[1].Split('-');
                        Drop drop = new();
                        if (vals.Length >= 2 && float.TryParse(vals[0], out float mn) && float.TryParse(vals[1], out float mx))
                            drop.Amount = new Range(mn, mx);
                        if (parts.Length >= 3 && float.TryParse(parts[2], out float chance))
                            drop.DropChance = chance;
                        Drops.Add(new KeyValuePair<string, Drop>(name, drop));
                    }
                }

                public SerializedDrops(List<KeyValuePair<string, Drop>> drops)
                {
                    Drops = drops;
                }

                public override string ToString() => string.Join(",", Drops.Select(kv => $"{kv.Key}:{kv.Value.Amount.min}-{kv.Value.Amount.max}:{kv.Value.DropChance}"));
            }

            [PublicAPI]
            public class Drop
            {
                public Range Amount = new(1, 1);
                public float DropChance = 100f;
                public bool DropOnePerPlayer = false;
            }
        }

        // ── Spawn Data ────────────────────────────────────────────────────────
        internal void UpdateSpawnData(SpawnSystem.SpawnData data)
        {
            FaunaConfig cfg = faunaConfigs[this];
            data.m_enabled = cfg.Spawn.get() != SpawnOption.Disabled;
            data.m_prefab = Prefab;
            data.m_name = Prefab.name;
            data.m_biome = cfg.Biome.get();
            data.m_biomeArea = cfg.SpawnArea.get() switch
            {
                SpawnArea.Center => Heightmap.BiomeArea.Median,
                SpawnArea.Edge => Heightmap.BiomeArea.Edge,
                _ => Heightmap.BiomeArea.Everything,
            };
            data.m_maxSpawned = cfg.Maximum.get();
            data.m_spawnInterval = cfg.SpawnInterval.get();
            data.m_spawnChance = cfg.SpawnChance.get();
            data.m_groupSizeMin = (int)cfg.GroupSize.get().min;
            data.m_groupSizeMax = (int)cfg.GroupSize.get().max;
            data.m_groupRadius = cfg.GroupRadius.get();
            data.m_spawnAtDay = cfg.SpawnTime.get() is SpawnTime.Always or SpawnTime.Day;
            data.m_spawnAtNight = cfg.SpawnTime.get() is SpawnTime.Always or SpawnTime.Night;
            data.m_minAltitude = cfg.Altitude.get().min;
            data.m_maxAltitude = cfg.Altitude.get().max;
            data.m_groundOffset = cfg.SpawnAltitude.get();
            data.m_inForest = cfg.Forest.get() is Forest.Both or Forest.Yes;
            data.m_outsideForest = cfg.Forest.get() is Forest.Both or Forest.No;
            data.m_canSpawnCloseToPlayer = cfg.CanSpawnNearPlayer.get() == Toggle.On;
            data.m_spawnRadiusMin = cfg.SpawnRadius.get().min;
            data.m_spawnRadiusMax = cfg.SpawnRadius.get().max;
            data.m_spawnDistance = cfg.SpawnDistance.get();
            data.m_maxLevel = 1;
            data.m_huntPlayer = false;
        }

        // ── Statics ───────────────────────────────────────────────────────────
        private static readonly List<Fauna> registeredFauna = new();
        private static readonly List<SpawnSystem.SpawnData> lastRegisteredSpawns = new();
        private static Dictionary<Fauna, FaunaConfig> faunaConfigs = new();
        private static object? configManager;

        private class FaunaConfig
        {
            public readonly CustomConfig<SpawnOption> Spawn = new();
            public readonly CustomConfig<SpawnTime> SpawnTime = new();
            public readonly CustomConfig<Range> Altitude = new();
            public readonly CustomConfig<Range> GroupSize = new();
            public readonly CustomConfig<Heightmap.Biome> Biome = new();
            public readonly CustomConfig<SpawnArea> SpawnArea = new();
            public readonly CustomConfig<float> SpawnChance = new();
            public readonly CustomConfig<Forest> Forest = new();
            public readonly CustomConfig<int> Maximum = new();
            public readonly CustomConfig<float> GroupRadius = new();
            public readonly CustomConfig<Toggle> CanSpawnNearPlayer = new();
            public readonly CustomConfig<Range> SpawnRadius = new();
            public readonly CustomConfig<float> SpawnDistance = new();
            public readonly CustomConfig<int> SpawnInterval = new();
            public readonly CustomConfig<float> SpawnAltitude = new();
            public readonly CustomConfig<DropOption> Drops = new();
            public readonly CustomConfig<string> CustomDrops = new();
        }

        private class CustomConfig<T>
        {
            public Func<T> get = null!;
            public ConfigEntry<T>? config = null;
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string? Category;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }

        internal static void Patch_FejdStartup()
        {
            Assembly? bepinexConfigManager = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "ConfigurationManager");
            Type? configManagerType = bepinexConfigManager?.GetType("ConfigurationManager.ConfigurationManager");
            configManager = configManagerType == null ? null : BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(configManagerType);

            if (!TomlTypeConverter.CanConvert(typeof(Range)))
            {
                TomlTypeConverter.AddConverter(typeof(Range), new TypeConverter
                {
                    ConvertToObject = (s, _) =>
                    {
                        Match match = Regex.Match(s, @"^(-?\d+(?:\.\d*)?)\s*-\s*(-?\d+(?:\.\d*)?)$");
                        return match.Success ? new Range(float.Parse(match.Groups[1].Value), float.Parse(match.Groups[2].Value)) : new Range();
                    },
                    ConvertToString = (obj, _) => { Range r = (Range)obj; return $"{r.min} - {r.max}"; },
                });
            }

            bool SaveOnConfigSet = plugin.Config.SaveOnConfigSet;
            plugin.Config.SaveOnConfigSet = false;

            foreach (Fauna fauna in registeredFauna)
            {
                FaunaConfig cfg = faunaConfigs[fauna] = new FaunaConfig();
                string nameKey = fauna.Prefab.name;
                string englishName = nameKey;
                string localizedName = nameKey;

                int order = 0;
                void configWithDesc<T>(CustomConfig<T> customConfig, Func<T> getter, Action configChanged, string name, ConfigDescription desc)
                {
                    if (fauna.ConfigurationEnabled)
                    {
                        customConfig.config = pluginConfig(englishName, name, getter(), new ConfigDescription(desc.Description, desc.AcceptableValues,
                            desc.Tags.Concat(new[] { new ConfigurationManagerAttributes { Order = --order, Category = localizedName,
                                CustomDrawer = (object)customConfig == cfg.CustomDrops ? drawConfigTable : typeof(T) == typeof(Range) ? drawRange : null } }).ToArray()));
                        customConfig.config.SettingChanged += (_, _) => configChanged();
                        customConfig.get = () => customConfig.config.Value;
                    }
                }
                void config<T>(CustomConfig<T> cc, Func<T> getter, Action changed, string name, string desc) =>
                    configWithDesc(cc, getter, changed, name, new ConfigDescription(desc));

                void updateAllSpawns()
                {
                    foreach (SpawnSystem ss in Object.FindObjectsByType<SpawnSystem>(FindObjectsSortMode.None))
                        foreach (SpawnSystemList sl in ss.m_spawnLists)
                            foreach (SpawnSystem.SpawnData sd in sl.m_spawners)
                                if (fauna.Prefab == sd.m_prefab) fauna.UpdateSpawnData(sd);
                }

                cfg.Spawn.get = () => fauna.CanSpawn ? SpawnOption.Default : SpawnOption.Disabled;
                cfg.SpawnTime.get = () => fauna.SpawnTime;
                cfg.Altitude.get = () => fauna.Altitude;
                cfg.GroupSize.get = () => fauna.GroupSize;
                cfg.Biome.get = () => fauna.Biome;
                cfg.SpawnArea.get = () => fauna.SpawnArea;
                cfg.SpawnChance.get = () => fauna.SpawnChance;
                cfg.Forest.get = () => fauna.Forest;
                cfg.Maximum.get = () => fauna.Maximum;
                cfg.GroupRadius.get = () => fauna.GroupRadius;
                cfg.CanSpawnNearPlayer.get = () => fauna.CanSpawnNearPlayer ? Toggle.On : Toggle.Off;
                cfg.SpawnRadius.get = () => fauna.SpawnRadius;
                cfg.SpawnDistance.get = () => fauna.SpawnDistance;
                cfg.SpawnInterval.get = () => fauna.SpawnInterval;
                cfg.SpawnAltitude.get = () => fauna.SpawnAltitude;
                cfg.Drops.get = () => DropOption.Default;
                cfg.CustomDrops.get = () => new DropList.SerializedDrops(fauna.Drops, fauna).ToString();

                ConfigurationManagerAttributes spawnVis = new();
                ConfigurationManagerAttributes dropVis = new();

                void spawnConfig<T>(CustomConfig<T> cc, Func<T> getter, string name, string desc) =>
                    configWithDesc(cc, getter, updateAllSpawns, name, new ConfigDescription(desc, null, spawnVis));

                if (fauna.IsConfigured(nameof(CanSpawn)))
                {
                    config(cfg.Spawn, cfg.Spawn.get, () =>
                    {
                        spawnVis.Browsable = cfg.Spawn.get() == SpawnOption.Custom;
                        updateAllSpawns();
                    }, "Spawn", "Configures spawning for this fauna.");
                }

                spawnVis.Browsable = cfg.Spawn.get() == SpawnOption.Custom;

                if (fauna.IsConfigured(nameof(SpawnTime))) spawnConfig(cfg.SpawnTime, cfg.SpawnTime.get, "Spawn time", "Day, Night or Always.");
                if (fauna.IsConfigured(nameof(Altitude))) spawnConfig(cfg.Altitude, cfg.Altitude.get, "Required altitude", "Min and max altitude for spawning.");
                if (fauna.IsConfigured(nameof(GroupSize))) spawnConfig(cfg.GroupSize, cfg.GroupSize.get, "Group size", "Min and max group size.");
                if (fauna.IsConfigured(nameof(Biome))) spawnConfig(cfg.Biome, cfg.Biome.get, "Biome", "Biome where this fauna spawns.");
                if (fauna.IsConfigured(nameof(SpawnArea))) spawnConfig(cfg.SpawnArea, cfg.SpawnArea.get, "Spawn area", "Center, Edge or Everywhere in the biome.");
                if (fauna.IsConfigured(nameof(SpawnChance))) spawnConfig(cfg.SpawnChance, cfg.SpawnChance.get, "Spawn chance", "Chance to spawn each interval (0-100).");
                if (fauna.IsConfigured(nameof(Forest))) spawnConfig(cfg.Forest, cfg.Forest.get, "Forest", "Forest, outside forest or both.");
                if (fauna.IsConfigured(nameof(Maximum))) spawnConfig(cfg.Maximum, cfg.Maximum.get, "Maximum", "Max number of this fauna near the player.");
                if (fauna.IsConfigured(nameof(GroupRadius))) spawnConfig(cfg.GroupRadius, cfg.GroupRadius.get, "Group radius", "Radius of the group spawn circle.");
                if (fauna.IsConfigured(nameof(CanSpawnNearPlayer))) spawnConfig(cfg.CanSpawnNearPlayer, cfg.CanSpawnNearPlayer.get, "Can spawn near player", "Allow spawning close to the player.");
                if (fauna.IsConfigured(nameof(SpawnRadius))) spawnConfig(cfg.SpawnRadius, cfg.SpawnRadius.get, "Spawn radius", "Min and max distance from player.");
                if (fauna.IsConfigured(nameof(SpawnDistance))) spawnConfig(cfg.SpawnDistance, cfg.SpawnDistance.get, "Spawn distance", "Minimum distance between two of this fauna.");
                if (fauna.IsConfigured(nameof(SpawnInterval))) spawnConfig(cfg.SpawnInterval, cfg.SpawnInterval.get, "Spawn interval", "Seconds between spawn checks.");
                if (fauna.IsConfigured(nameof(SpawnAltitude))) spawnConfig(cfg.SpawnAltitude, cfg.SpawnAltitude.get, "Spawn altitude", "Height offset above ground when spawning.");

                if (fauna.IsConfigured(nameof(Drops)))
                {
                    config(cfg.Drops, cfg.Drops.get, () =>
                    {
                        dropVis.Browsable = cfg.Drops.get() == DropOption.Custom;
                        DropList.UpdateDrops(fauna);
                    }, "Drops", "Configures drops for this fauna.");
                    dropVis.Browsable = cfg.Drops.get() == DropOption.Custom;
                    configWithDesc(cfg.CustomDrops, cfg.CustomDrops.get, () => DropList.UpdateDrops(fauna), "Drop config", new ConfigDescription("", null, dropVis));
                }
            }

            if (SaveOnConfigSet)
            {
                plugin.Config.SaveOnConfigSet = true;
                plugin.Config.Save();
            }
        }

        [HarmonyPriority(Priority.VeryHigh)]
        internal static void Patch_ZNetSceneAwake(ZNetScene __instance)
        {
            foreach (Fauna f in registeredFauna)
            {
                if (!__instance.m_prefabs.Contains(f.Prefab))
                    __instance.m_prefabs.Add(f.Prefab);
            }
        }

        [HarmonyPriority(Priority.VeryHigh)]
        internal static void Patch_SpawnSystemAwake(SpawnSystem __instance)
        {
            SpawnSystemList spawnList = __instance.m_spawnLists.First();
            foreach (SpawnSystem.SpawnData old in lastRegisteredSpawns) spawnList.m_spawners.Remove(old);
            lastRegisteredSpawns.Clear();
            foreach (Fauna f in registeredFauna)
            {
                SpawnSystem.SpawnData data = new() { m_name = f.Prefab.name, m_prefab = f.Prefab };
                f.UpdateSpawnData(data);
                spawnList.m_spawners.Add(data);
                lastRegisteredSpawns.Add(data);
            }
        }

        internal static void Patch_ObjectDBAwake()
        {
            foreach (Fauna f in registeredFauna) DropList.UpdateDrops(f);
        }

        // ── Config helpers ────────────────────────────────────────────────────
        private static BaseUnityPlugin? _plugin;
        private static BaseUnityPlugin plugin
        {
            get
            {
                if (_plugin is null)
                {
                    IEnumerable<TypeInfo> types;
                    try { types = Assembly.GetExecutingAssembly().DefinedTypes.ToList(); }
                    catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).Select(t => t.GetTypeInfo()); }
                    _plugin = (BaseUnityPlugin)BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(types.First(t => t.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(t)));
                }
                return _plugin;
            }
        }

        private static bool hasConfigSync = true;
        private static object? _configSync;
        private static object? configSync
        {
            get
            {
                if (_configSync == null && hasConfigSync)
                {
                    if (Assembly.GetExecutingAssembly().GetType("ServerSync.ConfigSync") is { } t)
                    {
                        _configSync = Activator.CreateInstance(t, plugin.Info.Metadata.GUID + " FaunaManager");
                        t.GetField("CurrentVersion").SetValue(_configSync, plugin.Info.Metadata.Version.ToString());
                        t.GetProperty("IsLocked")!.SetValue(_configSync, true);
                    }
                    else hasConfigSync = false;
                }
                return _configSync;
            }
        }

        private static ConfigEntry<T> pluginConfig<T>(string group, string name, T value, ConfigDescription description)
        {
            ConfigEntry<T> configEntry = plugin.Config.Bind(group, name, value, description);
            configSync?.GetType().GetMethod("AddConfigEntry")!.MakeGenericMethod(typeof(T)).Invoke(configSync, new object[] { configEntry });
            return configEntry;
        }

        private static void drawRange(ConfigEntryBase cfg)
        {
            bool locked = cfg.Description.Tags.Select(a => a.GetType().Name == "ConfigurationManagerAttributes" ? (bool?)a.GetType().GetField("ReadOnly")?.GetValue(a) : null).FirstOrDefault(v => v != null) ?? false;
            ConfigEntry<Range> config = (ConfigEntry<Range>)cfg;
            GUILayout.BeginHorizontal();
            float.TryParse(GUILayout.TextField(config.Value.min.ToString(CultureInfo.InvariantCulture)), out float min);
            GUILayout.Label(" - ", new GUIStyle(GUI.skin.label) { fixedWidth = 14 });
            float.TryParse(GUILayout.TextField(config.Value.max.ToString(CultureInfo.InvariantCulture)), out float max);
            GUILayout.EndHorizontal();
            if (!locked && (Math.Abs(config.Value.min - min) > 0.00001f || Math.Abs(config.Value.max - max) > 0.00001f))
                config.Value = new Range(min, max);
        }

        private static void drawConfigTable(ConfigEntryBase cfg)
        {
            bool locked = cfg.Description.Tags.Select(a => a.GetType().Name == "ConfigurationManagerAttributes" ? (bool?)a.GetType().GetField("ReadOnly")?.GetValue(a) : null).FirstOrDefault(v => v != null) ?? false;
            List<KeyValuePair<string, DropList.Drop>> newDrops = new();
            bool wasUpdated = false;
            int RightColumnWidth = (int)(configManager?.GetType().GetProperty("RightColumnWidth", BindingFlags.Instance | BindingFlags.NonPublic)!.GetGetMethod(true).Invoke(configManager, Array.Empty<object>()) ?? 130);
            GUILayout.BeginVertical();
            foreach (KeyValuePair<string, DropList.Drop> drop in new DropList.SerializedDrops((string)cfg.BoxedValue).Drops)
            {
                GUILayout.BeginHorizontal();
                int minA = Mathf.RoundToInt(drop.Value.Amount.min);
                if (int.TryParse(GUILayout.TextField(minA.ToString(), new GUIStyle(GUI.skin.textField) { fixedWidth = 35 }), out int newMin) && newMin != minA && !locked) { minA = newMin; wasUpdated = true; }
                GUILayout.Label(" - ", new GUIStyle(GUI.skin.label) { fixedWidth = 14 });
                int maxA = Mathf.RoundToInt(drop.Value.Amount.max);
                if (int.TryParse(GUILayout.TextField(maxA.ToString(), new GUIStyle(GUI.skin.textField) { fixedWidth = 35 }), out int newMax) && newMax != maxA && !locked) { maxA = newMax; wasUpdated = true; }
                GUILayout.Label(" ", new GUIStyle(GUI.skin.label) { fixedWidth = 10 });
                string newName = GUILayout.TextField(drop.Key, new GUIStyle(GUI.skin.textField) { fixedWidth = RightColumnWidth - 35 - 14 - 35 - 10 - 21 - 18 });
                string itemName = locked ? drop.Key : newName;
                wasUpdated = wasUpdated || itemName != drop.Key;
                bool removed = GUILayout.Button("x", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }) && !locked;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                float chance = drop.Value.DropChance;
                if (float.TryParse(GUILayout.TextField(chance.ToString(CultureInfo.InvariantCulture), new GUIStyle(GUI.skin.textField) { fixedWidth = 45 }), out float newChance) && Math.Abs(newChance - chance) > 0.00001f && !locked) { chance = newChance; wasUpdated = true; }
                GUILayout.Label("% ");
                bool perPlayer = drop.Value.DropOnePerPlayer;
                bool newPerPlayer = GUILayout.Toggle(perPlayer, new GUIContent(perPlayer ? "per player" : "independent"));
                if (newPerPlayer != perPlayer && !locked) { perPlayer = newPerPlayer; wasUpdated = true; }
                if (!removed) newDrops.Add(new KeyValuePair<string, DropList.Drop>(itemName, new DropList.Drop { Amount = new Range(minA, maxA), DropChance = chance, DropOnePerPlayer = perPlayer }));
                else wasUpdated = true;
                if (GUILayout.Button("+", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }) && !locked) { wasUpdated = true; newDrops.Add(new KeyValuePair<string, DropList.Drop>("", new DropList.Drop())); }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            if (wasUpdated) cfg.BoxedValue = new DropList.SerializedDrops(newDrops).ToString();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    [PublicAPI]
    public static class FaunaPrefabManager
    {
        static FaunaPrefabManager()
        {
            Harmony harmony = new(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".faunamanager");
            harmony.Patch(AccessTools.DeclaredMethod(typeof(ZNetScene), nameof(ZNetScene.Awake)), new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Fauna), nameof(Fauna.Patch_ZNetSceneAwake))));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(SpawnSystem), nameof(SpawnSystem.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Fauna), nameof(Fauna.Patch_SpawnSystemAwake))));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(ObjectDB), nameof(ObjectDB.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Fauna), nameof(Fauna.Patch_ObjectDBAwake))));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(FejdStartup), nameof(FejdStartup.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Fauna), nameof(Fauna.Patch_FejdStartup))));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(Localization), nameof(Localization.LoadCSV)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(FaunaLocalizeKey), nameof(FaunaLocalizeKey.AddLocalizedKeys))));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(Localization), nameof(Localization.SetupLanguage)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(FaunaLocalizationCache), nameof(FaunaLocalizationCache.LocalizationPostfix))));
        }

        private struct BundleId { public string assetBundleFileName; public string folderName; }
        private static readonly Dictionary<BundleId, AssetBundle> bundleCache = new();

        public static AssetBundle RegisterAssetBundle(string assetBundleFileName, string folderName = "assets")
        {
            BundleId id = new() { assetBundleFileName = assetBundleFileName, folderName = folderName };
            if (!bundleCache.TryGetValue(id, out AssetBundle assets))
                assets = bundleCache[id] = Resources.FindObjectsOfTypeAll<AssetBundle>().FirstOrDefault(a => a.name == assetBundleFileName)
                    ?? AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name + $".{folderName}." + assetBundleFileName));
            return assets;
        }

        public static GameObject RegisterPrefab(AssetBundle assets, string prefabName) => assets.LoadAsset<GameObject>(prefabName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    [PublicAPI]
    public class FaunaLocalizeKey
    {
        private static readonly List<FaunaLocalizeKey> keys = new();
        public readonly string Key;
        public readonly Dictionary<string, string> Localizations = new();

        public FaunaLocalizeKey(string key) { Key = key.Replace("$", ""); keys.Add(this); }

        public FaunaLocalizeKey English(string key) => addForLang("English", key);
        public FaunaLocalizeKey Portuguese_Brazilian(string key) => addForLang("Portuguese_Brazilian", key);
        public FaunaLocalizeKey Spanish(string key) => addForLang("Spanish", key);
        public FaunaLocalizeKey French(string key) => addForLang("French", key);
        public FaunaLocalizeKey German(string key) => addForLang("German", key);
        public FaunaLocalizeKey Italian(string key) => addForLang("Italian", key);
        public FaunaLocalizeKey Russian(string key) => addForLang("Russian", key);
        public FaunaLocalizeKey Chinese(string key) => addForLang("Chinese", key);

        private FaunaLocalizeKey addForLang(string lang, string value)
        {
            Localizations[lang] = value;
            if (Localization.m_instance != null)
            {
                if (Localization.instance.GetSelectedLanguage() == lang)
                    Localization.instance.AddWord(Key, value);
                else if (lang == "English" && !Localization.instance.m_translations.ContainsKey(Key))
                    Localization.instance.AddWord(Key, value);
            }
            return this;
        }

        [HarmonyPriority(Priority.LowerThanNormal)]
        internal static void AddLocalizedKeys(Localization __instance, string language)
        {
            foreach (FaunaLocalizeKey key in keys)
            {
                if (key.Localizations.TryGetValue(language, out string t) || key.Localizations.TryGetValue("English", out t))
                    __instance.AddWord(key.Key, t);
                else if (key.Localizations.TryGetValue("alias", out string alias))
                    __instance.AddWord(key.Key, __instance.Localize(alias));
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    public static class FaunaLocalizationCache
    {
        private static readonly Dictionary<string, Localization> localizations = new();

        internal static void LocalizationPostfix(Localization __instance, string language)
        {
            if (localizations.FirstOrDefault(l => l.Value == __instance).Key is { } oldValue)
                localizations.Remove(oldValue);
            if (!localizations.ContainsKey(language))
                localizations.Add(language, __instance);
        }

        public static Localization ForLanguage(string? language = null)
        {
            if (localizations.TryGetValue(language ?? PlayerPrefs.GetString("language", "English"), out Localization loc))
                return loc;
            loc = new Localization();
            if (language is not null) loc.SetupLanguage(language);
            return loc;
        }
    }
}
