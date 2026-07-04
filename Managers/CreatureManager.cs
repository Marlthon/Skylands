using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Windows;
using Object = UnityEngine.Object;
using TypeConverter = BepInEx.Configuration.TypeConverter;

namespace CreatureManager
{

    public enum Toggle
    {
        On,
        Off,
    }

    [PublicAPI]
    public enum GlobalKey
    {
        [InternalName("")] None,
        [InternalName("defeated_bonemass")] KilledBonemass,
        [InternalName("defeated_gdking")] KilledElder,
        [InternalName("defeated_goblinking")] KilledYagluth,
        [InternalName("defeated_dragon")] KilledModer,
        [InternalName("defeated_eikthyr")] KilledEikthyr,
        [InternalName("KilledTroll")] KilledTroll,
        [InternalName("killed_surtling")] KilledSurtling,
    }

    [Flags]
    [PublicAPI]
    public enum Weather
    {
        [InternalName("")] None = 0,
        [InternalName("Clear")] ClearSkies = 1 << 0,
        [InternalName("Heath clear")] MeadowsClearSkies = 1 << 2,
        [InternalName("LightRain")] LightRain = 1 << 3,
        [InternalName("Rain")] Rain = 1 << 4,
        [InternalName("ThunderStorm")] ThunderStorm = 1 << 5,
        [InternalName("nofogts")] ClearThunderStorm = 1 << 6,
        [InternalName("SwampRain")] SwampRain = 1 << 7,
        [InternalName("Darklands_dark")] MistlandsDark = 1 << 8,
        [InternalName("Ashrain")] AshlandsAshrain = 1 << 9,
        [InternalName("Snow")] MountainSnow = 1 << 10,
        [InternalName("SnowStorm")] MountainBlizzard = 1 << 11,
        [InternalName("DeepForest Mist")] BlackForestFog = 1 << 12,
        [InternalName("Misty")] Fog = 1 << 13,
        [InternalName("Twilight_Snow")] DeepNorthSnow = 1 << 14,
        [InternalName("Twilight_SnowStorm")] DeepNorthSnowStorm = 1 << 15,
        [InternalName("Twilight_Clear")] DeepNorthClear = 1 << 16,
        [InternalName("Eikthyr")] EikyrsThunderstorm = 1 << 17,
        [InternalName("GDKing")] EldersHaze = 1 << 18,
        [InternalName("Bonemass")] BonemassDownpour = 1 << 19,
        [InternalName("Moder")] ModersVortex = 1 << 20,
        [InternalName("GoblinKing")] YagluthsMagicBlizzard = 1 << 21,
        [InternalName("Crypt")] Crypt = 1 << 22,
        [InternalName("SunkenCrypt")] SunkenCrypt = 1 << 23,
    }

    public class InternalName : Attribute
    {
        public readonly string internalName;
        public InternalName(string internalName) => this.internalName = internalName;
    }

    public enum DropOption
    {
        Disabled,
        Default,
        Custom,
    }

    public enum SpawnOption
    {
        Disabled,
        Default,
        Custom,
    }

    public enum SpawnTime
    {
        Day,
        Night,
        Always,
    }

    public enum SpawnArea
    {
        Center,
        Edge,
        Everywhere,
    }

    public enum Forest
    {
        Yes,
        No,
        Both,
    }

    [PublicAPI]
    public struct Range
    {
        public float min = 0;
        public float max = 0;

        public Range(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }

    [PublicAPI]
    public class Creature
    {
        // Propriedades privadas com valores padrão "desativados"
        private readonly HashSet<string> _configuredProperties = new();
        private bool _canSpawn = false;
        private bool _canBeTamed = false;
        private string _foodItems = "";
        private SpawnTime _specificSpawnTime = SpawnTime.Always;
        private Range _requiredAltitude = new(5, 1000);
        private Range _requiredOceanDepth = new(0, 0);
        private GlobalKey _requiredGlobalKey = GlobalKey.None;
        private Range _groupSize = new(1, 1);
        private Heightmap.Biome _biome = Heightmap.Biome.Meadows;
        private SpawnArea _specificSpawnArea = SpawnArea.Everywhere;
        private Weather _requiredWeather = Weather.None;
        private float _spawnAltitude = 0.5f;
        private bool _canHaveStars = true;
        private bool _attackImmediately = false;
        private int _checkSpawnInterval = 600;
        private float _spawnChance = 100;
        private Forest _forestSpawn = Forest.Both;
        private int _maximum = 1;

        // Health & Damage Modifiers
        private float _health = -1f;
        private float _regenAllHpTime = -1f;
        private HitData.DamageModifier _bluntModifier = HitData.DamageModifier.Normal;
        private HitData.DamageModifier _slashModifier = HitData.DamageModifier.Normal;
        private HitData.DamageModifier _pierceModifier = HitData.DamageModifier.Normal;
        private HitData.DamageModifier _chopModifier = HitData.DamageModifier.Normal;
        private HitData.DamageModifier _pickaxeModifier = HitData.DamageModifier.Normal;
        private HitData.DamageModifier _fireModifier = HitData.DamageModifier.Normal;
        private HitData.DamageModifier _frostModifier = HitData.DamageModifier.Normal;
        private HitData.DamageModifier _lightningModifier = HitData.DamageModifier.Normal;
        private HitData.DamageModifier _poisonModifier = HitData.DamageModifier.Normal;
        private HitData.DamageModifier _spiritModifier = HitData.DamageModifier.Normal;

        public bool ConfigurationEnabled = true;
        public readonly GameObject Prefab;
        public DropList Drops = new();

        // Getters públicos para a lógica interna ler os valores
        public bool CanSpawn => _canSpawn;
        public bool CanBeTamed => _canBeTamed;
        public string FoodItems => _foodItems;
        public SpawnTime SpecificSpawnTime => _specificSpawnTime;
        public Range RequiredAltitude => _requiredAltitude;
        public Range RequiredOceanDepth => _requiredOceanDepth;
        public GlobalKey RequiredGlobalKey => _requiredGlobalKey;
        public Range GroupSize => _groupSize;
        public Heightmap.Biome Biome => _biome;
        public SpawnArea SpecificSpawnArea => _specificSpawnArea;
        public Weather RequiredWeather => _requiredWeather;
        public float SpawnAltitude => _spawnAltitude;
        public bool CanHaveStars => _canHaveStars;
        public bool AttackImmediately => _attackImmediately;
        public int CheckSpawnInterval => _checkSpawnInterval;
        public float SpawnChance => _spawnChance;
        public Forest ForestSpawn => _forestSpawn;
        public int Maximum => _maximum;

        // Health & Damage Modifiers getters
        public float Health => _health;
        public float RegenAllHpTime => _regenAllHpTime;
        public HitData.DamageModifier BluntModifier => _bluntModifier;
        public HitData.DamageModifier SlashModifier => _slashModifier;
        public HitData.DamageModifier PierceModifier => _pierceModifier;
        public HitData.DamageModifier ChopModifier => _chopModifier;
        public HitData.DamageModifier PickaxeModifier => _pickaxeModifier;
        public HitData.DamageModifier FireModifier => _fireModifier;
        public HitData.DamageModifier FrostModifier => _frostModifier;
        public HitData.DamageModifier LightningModifier => _lightningModifier;
        public HitData.DamageModifier PoisonModifier => _poisonModifier;
        public HitData.DamageModifier SpiritModifier => _spiritModifier;

        // Método para verificar se uma propriedade foi configurada
        public bool IsConfigured(string propertyName) => _configuredProperties.Contains(propertyName);

        #region Fluent Configuration Methods
        public Creature EnableSpawning(bool enabled = true, bool isConfigurable = true)
        {
            _canSpawn = enabled;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.CanSpawn));
            }
            return this;
        }

        public Creature EnableTaming(bool enabled = true, bool isConfigurable = true)
        {
            _canBeTamed = enabled;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.CanBeTamed));
            }
            return this;
        }

        public DropList ConfigureDrops()
        {
            _configuredProperties.Add(nameof(this.Drops));
            return this.Drops;
        }

        public Creature ConfigureFoodItems(string foodItems, bool isConfigurable = true)
        {
            _foodItems = foodItems;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.FoodItems));
            }
            return this;
        }

        public Creature ConfigureSpawnTime(SpawnTime spawnTime, bool isConfigurable = true)
        {
            _specificSpawnTime = spawnTime;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.SpecificSpawnTime));
            }
            return this;
        }

        public Creature ConfigureRequiredAltitude(Range altitude, bool isConfigurable = true)
        {
            _requiredAltitude = altitude;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.RequiredAltitude));
            }
            return this;
        }

        public Creature ConfigureRequiredOceanDepth(Range depth, bool isConfigurable = true)
        {
            _requiredOceanDepth = depth;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.RequiredOceanDepth));
            }
            return this;
        }

        public Creature ConfigureRequiredGlobalKey(GlobalKey globalKey, bool isConfigurable = true)
        {
            _requiredGlobalKey = globalKey;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.RequiredGlobalKey));
            }
            return this;
        }

        public Creature ConfigureGroupSize(Range groupSize, bool isConfigurable = true)
        {
            _groupSize = groupSize;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.GroupSize));
            }
            return this;
        }

        public Creature ConfigureBiome(Heightmap.Biome biome, bool isConfigurable = true)
        {
            _biome = biome;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.Biome));
            }
            return this;
        }

        public Creature ConfigureSpawnArea(SpawnArea spawnArea, bool isConfigurable = true)
        {
            _specificSpawnArea = spawnArea;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.SpecificSpawnArea));
            }
            return this;
        }

        public Creature ConfigureRequiredWeather(Weather weather, bool isConfigurable = true)
        {
            _requiredWeather = weather;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.RequiredWeather));
            }
            return this;
        }

        public Creature ConfigureSpawnAltitude(float altitude, bool isConfigurable = true)
        {
            _spawnAltitude = altitude;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.SpawnAltitude));
            }
            return this;
        }

        public Creature EnableStars(bool enabled = true, bool isConfigurable = true)
        {
            _canHaveStars = enabled;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.CanHaveStars));
            }
            return this;
        }

        public Creature ConfigureAttackImmediately(bool attackImmediately = true, bool isConfigurable = true)
        {
            _attackImmediately = attackImmediately;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.AttackImmediately));
            }
            return this;
        }

        public Creature ConfigureSpawnInterval(int interval, bool isConfigurable = true)
        {
            _checkSpawnInterval = interval;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.CheckSpawnInterval));
            }
            return this;
        }

        public Creature ConfigureSpawnChance(float chance, bool isConfigurable = true)
        {
            _spawnChance = chance;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.SpawnChance));
            }
            return this;
        }

        public Creature ConfigureForestSpawn(Forest forest, bool isConfigurable = true)
        {
            _forestSpawn = forest;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.ForestSpawn));
            }
            return this;
        }

        public Creature ConfigureMaximum(int max, bool isConfigurable = true)
        {
            _maximum = max;
            if (isConfigurable)
            {
                _configuredProperties.Add(nameof(this.Maximum));
            }
            return this;
        }

        public Creature ConfigureHealth(float health, bool isConfigurable = true)
        {
            _health = health;
            if (isConfigurable) _configuredProperties.Add(nameof(this.Health));
            return this;
        }

        public Creature ConfigureRegenAllHpTime(float time, bool isConfigurable = true)
        {
            _regenAllHpTime = time;
            if (isConfigurable) _configuredProperties.Add(nameof(this.RegenAllHpTime));
            return this;
        }

        public Creature ConfigureBluntModifier(HitData.DamageModifier modifier, bool isConfigurable = true)
        {
            _bluntModifier = modifier;
            if (isConfigurable) _configuredProperties.Add(nameof(this.BluntModifier));
            return this;
        }

        public Creature ConfigureSlashModifier(HitData.DamageModifier modifier, bool isConfigurable = true)
        {
            _slashModifier = modifier;
            if (isConfigurable) _configuredProperties.Add(nameof(this.SlashModifier));
            return this;
        }

        public Creature ConfigurePierceModifier(HitData.DamageModifier modifier, bool isConfigurable = true)
        {
            _pierceModifier = modifier;
            if (isConfigurable) _configuredProperties.Add(nameof(this.PierceModifier));
            return this;
        }

        public Creature ConfigureChopModifier(HitData.DamageModifier modifier, bool isConfigurable = true)
        {
            _chopModifier = modifier;
            if (isConfigurable) _configuredProperties.Add(nameof(this.ChopModifier));
            return this;
        }

        public Creature ConfigurePickaxeModifier(HitData.DamageModifier modifier, bool isConfigurable = true)
        {
            _pickaxeModifier = modifier;
            if (isConfigurable) _configuredProperties.Add(nameof(this.PickaxeModifier));
            return this;
        }

        public Creature ConfigureFireModifier(HitData.DamageModifier modifier, bool isConfigurable = true)
        {
            _fireModifier = modifier;
            if (isConfigurable) _configuredProperties.Add(nameof(this.FireModifier));
            return this;
        }

        public Creature ConfigureFrostModifier(HitData.DamageModifier modifier, bool isConfigurable = true)
        {
            _frostModifier = modifier;
            if (isConfigurable) _configuredProperties.Add(nameof(this.FrostModifier));
            return this;
        }

        public Creature ConfigureLightningModifier(HitData.DamageModifier modifier, bool isConfigurable = true)
        {
            _lightningModifier = modifier;
            if (isConfigurable) _configuredProperties.Add(nameof(this.LightningModifier));
            return this;
        }

        public Creature ConfigurePoisonModifier(HitData.DamageModifier modifier, bool isConfigurable = true)
        {
            _poisonModifier = modifier;
            if (isConfigurable) _configuredProperties.Add(nameof(this.PoisonModifier));
            return this;
        }

        public Creature ConfigureSpiritModifier(HitData.DamageModifier modifier, bool isConfigurable = true)
        {
            _spiritModifier = modifier;
            if (isConfigurable) _configuredProperties.Add(nameof(this.SpiritModifier));
            return this;
        }
        #endregion

        [PublicAPI]
        public class DropList
        {
            private Dictionary<string, Drop>? drops = null;

            public void None() => drops = new Dictionary<string, Drop>();

            public Drop this[string prefabName] => (drops ??= new Dictionary<string, Drop>()).TryGetValue(prefabName, out Drop drop) ? drop : drops[prefabName] = new Drop();

            [HarmonyPriority(Priority.VeryHigh)]
            internal static void AddDropsToCreature()
            {
                foreach (Creature creature in registeredCreatures)
                {
                    UpdateDrops(creature);
                }
            }

            internal static void UpdateDrops(Creature creature)
            {
                if (!creatureConfigs.ContainsKey(creature) || creatureConfigs[creature].Drops.get is null) return;

                DropOption option = creatureConfigs[creature].Drops.get();
                if (option == DropOption.Default && creature.Drops.drops is null)
                {
                    return;
                }

                (creature.Prefab.GetComponent<CharacterDrop>() ?? creature.Prefab.AddComponent<CharacterDrop>()).m_drops = (creatureConfigs[creature].Drops.get() switch
                {
                    DropOption.Custom => new SerializedDrops(creatureConfigs[creature].CustomDrops.get()).Drops,
                    DropOption.Disabled => new List<KeyValuePair<string, Drop>>(),
                    _ => creature.Drops.drops!.ToList(),
                }).Select(kv =>
                {
                    if (kv.Key == "" || ZNetScene.instance is null)
                    {
                        return null;
                    }
                    if (ZNetScene.instance.GetPrefab(kv.Key) is not { } prefab)
                    {
                        Debug.LogWarning($"Found invalid prefab name {kv.Key} for creature {creature.Prefab.name}");
                        return null;
                    }
                    return new CharacterDrop.Drop
                    {
                        m_prefab = prefab,
                        m_amountMin = (int)kv.Value.Amount.min,
                        m_amountMax = (int)kv.Value.Amount.max,
                        m_chance = kv.Value.DropChance / 100,
                        m_onePerPlayer = kv.Value.DropOnePerPlayer,
                        m_levelMultiplier = kv.Value.MultiplyDropByLevel,
                    };
                }).Where(d => d != null).ToList();
            }

            internal class SerializedDrops
            {
                public readonly List<KeyValuePair<string, Drop>> Drops;

                public SerializedDrops(DropList drops, Creature creature) => Drops = (drops.drops ?? creature.Prefab.GetComponent<CharacterDrop>()?.m_drops.ToDictionary(drop => drop.m_prefab.name, drop => new Drop
                {
                    Amount = new Range(drop.m_amountMin, drop.m_amountMax),
                    DropChance = drop.m_chance,
                    DropOnePerPlayer = drop.m_onePerPlayer,
                    MultiplyDropByLevel = drop.m_levelMultiplier,
                }) ?? new Dictionary<string, Drop>()).ToList();

                public SerializedDrops(List<KeyValuePair<string, Drop>> drops) => Drops = drops;

                public SerializedDrops(string reqs)
                {
                    Drops = reqs.Split(',').Select(r => r.Split(':')).ToDictionary(l => l[0], parts =>
                    {
                        Range amount = new(1, 1);
                        if (parts.Length > 1)
                        {
                            string[] range = parts[1].Split('-');
                            if (!int.TryParse(range[0], out int min))
                            {
                                min = 1;
                            }
                            if (range.Length == 1 || !int.TryParse(range[0], out int max))
                            {
                                max = min;
                            }
                            amount = new Range(min, max);
                        }
                        return new Drop
                        {
                            Amount = amount,
                            DropChance = parts.Length > 2 && float.TryParse(parts[2], out float chance) ? chance : 100,
                            DropOnePerPlayer = parts.Length > 3 && parts[3] == "onePerPlayer",
                            MultiplyDropByLevel = parts.Length > 4 && parts[4] == "multiplyByLevel",
                        };
                    }).ToList();
                }

                public override string ToString()
                {
                    return string.Join(",", Drops.Select(kv => $"{kv.Key}:{kv.Value.Amount.min}-{kv.Value.Amount.max}:{kv.Value.DropChance}:{(kv.Value.DropOnePerPlayer ? "onePerPlayer" : "unrestricted")}:{(kv.Value.MultiplyDropByLevel ? "multiplyByLevel" : "unaffectedByLevel")}"));
                }
            }
        }

        [PublicAPI]
        public class Drop
        {
            public Range Amount = new(1, 1);
            public float DropChance = 100f;
            public bool DropOnePerPlayer = false;
            public bool MultiplyDropByLevel = true;
        }

        private static readonly List<Creature> registeredCreatures = new();

        internal static bool IsRegisteredCreature(string prefabName) =>
            registeredCreatures.Any(c => c.Prefab.name == prefabName);

        public Creature(string assetBundleFileName, string prefabName, string folderName = "assets") : this(PrefabManager.RegisterAssetBundle(assetBundleFileName, folderName), prefabName) { }

        public Creature(AssetBundle bundle, string prefabName) : this(PrefabManager.RegisterPrefab(bundle, prefabName)) { }

        public Creature(GameObject creature)
        {
            Prefab = creature;
            registeredCreatures.Add(this);
        }

        public LocalizeKey Localize() => new(Prefab.GetComponent<Character>().m_name);

        private class CustomConfig<T>
        {
            public Func<T> get = null!;
            public ConfigEntry<T>? config = null;
        }

        private class CreatureConfig
        {
            public readonly CustomConfig<SpawnOption> Spawn = new();
            public readonly CustomConfig<Toggle> CanBeTamed = new();
            public readonly CustomConfig<string> ConsumesItemName = new();
            public readonly CustomConfig<SpawnTime> SpecificSpawnTime = new();
            public readonly CustomConfig<Range> RequiredAltitude = new();
            public readonly CustomConfig<Range> RequiredOceanDepth = new();
            public readonly CustomConfig<GlobalKey> RequiredGlobalKey = new();
            public readonly CustomConfig<Range> GroupSize = new();
            public readonly CustomConfig<Heightmap.Biome> Biome = new();
            public readonly CustomConfig<SpawnArea> SpecificSpawnArea = new();
            public readonly CustomConfig<Weather> RequiredWeather = new();
            public readonly CustomConfig<float> SpawnAltitude = new();
            public readonly CustomConfig<Toggle> CanHaveStars = new();
            public readonly CustomConfig<Toggle> AttackImmediately = new();
            public readonly CustomConfig<int> CheckSpawnInterval = new();
            public readonly CustomConfig<float> SpawnChance = new();
            public readonly CustomConfig<Forest> ForestSpawn = new();
            public readonly CustomConfig<int> Maximum = new();
            public readonly CustomConfig<DropOption> Drops = new();
            public readonly CustomConfig<string> CustomDrops = new();

            // Health & Damage Modifiers
            public readonly CustomConfig<float> Health = new();
            public readonly CustomConfig<float> RegenAllHpTime = new();
            public readonly CustomConfig<HitData.DamageModifier> BluntModifier = new();
            public readonly CustomConfig<HitData.DamageModifier> SlashModifier = new();
            public readonly CustomConfig<HitData.DamageModifier> PierceModifier = new();
            public readonly CustomConfig<HitData.DamageModifier> ChopModifier = new();
            public readonly CustomConfig<HitData.DamageModifier> PickaxeModifier = new();
            public readonly CustomConfig<HitData.DamageModifier> FireModifier = new();
            public readonly CustomConfig<HitData.DamageModifier> FrostModifier = new();
            public readonly CustomConfig<HitData.DamageModifier> LightningModifier = new();
            public readonly CustomConfig<HitData.DamageModifier> PoisonModifier = new();
            public readonly CustomConfig<HitData.DamageModifier> SpiritModifier = new();
        }

        private static Dictionary<Creature, CreatureConfig> creatureConfigs = new();

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string? Category;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }

        private class AcceptableEnumValues<T> : AcceptableValueBase where T : struct, IConvertible
        {
            public AcceptableEnumValues(params T[] acceptableValues) : base(typeof(T))
            {
                AcceptableValues = acceptableValues;
            }

            [PublicAPI] public virtual T[] AcceptableValues { get; }

            public override object Clamp(object value) => IsValid(value) ? value : AcceptableValues[0];
            public override bool IsValid(object value) => AcceptableValues.Contains((T)value);
            public override string ToDescriptionString() => string.Join(", ", AcceptableValues);
        }

        private static object? configManager;

        internal static void Patch_FejdStartup()
        {
            Assembly? bepinexConfigManager = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "ConfigurationManager");

            Type? configManagerType = bepinexConfigManager?.GetType("ConfigurationManager.ConfigurationManager");
            configManager = configManagerType == null ? null : BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(configManagerType);

            static void reloadConfigDisplay()
            {
                if (configManager?.GetType().GetProperty("DisplayingWindow")!.GetValue(configManager) is true)
                {
                    configManager.GetType().GetMethod("BuildSettingList")!.Invoke(configManager, Array.Empty<object>());
                }
            }

            if (!TomlTypeConverter.CanConvert(typeof(Range)))
            {
                TomlTypeConverter.AddConverter(typeof(Range), new TypeConverter
                {
                    ConvertToObject = (s, _) =>
                    {
                        Match match = Regex.Match(s, @"^(-?\d+(?:\.\d*)?)\s*-\s*(-?\d+(?:\.\d*)?)$");
                        return match.Success ? new Range(float.Parse(match.Groups[1].Value), float.Parse(match.Groups[2].Value)) : new Range();
                    },
                    ConvertToString = (obj, _) =>
                    {
                        Range range = (Range)obj;
                        return $"{range.min} - {range.max}";
                    },
                });
            }

            bool SaveOnConfigSet = plugin.Config.SaveOnConfigSet;
            plugin.Config.SaveOnConfigSet = false;

            foreach (Creature creature in registeredCreatures)
            {
                CreatureConfig cfg = creatureConfigs[creature] = new CreatureConfig();
                string nameKey = creature.Prefab.GetComponent<Character>().m_name;
                string englishName = new Regex("['[\"\\]]").Replace(english.Localize(nameKey), "").Trim();

                string localizedName = englishName;
                if (Localization.m_instance != null)
                {
                    localizedName = Localization.instance.Localize(nameKey).Trim();
                }

                int order = 0;
                void configWithDesc<T>(CustomConfig<T> customConfig, Func<T> getter, Action configChanged, string name, ConfigDescription desc)
                {
                    if (creature.ConfigurationEnabled)
                    {
                        customConfig.config = pluginConfig(englishName, name, getter(), new ConfigDescription(desc.Description, desc.AcceptableValues, desc.Tags.Concat(new[] { new ConfigurationManagerAttributes { Order = --order, CustomDrawer = (object)customConfig == cfg.CustomDrops ? drawConfigTable : typeof(T) == typeof(Range) ? drawRange : null, Category = localizedName } }).ToArray()));
                        customConfig.config.SettingChanged += (_, _) => configChanged();
                        customConfig.get = () => customConfig.config.Value;
                    }
                }
                void config<T>(CustomConfig<T> customConfig, Func<T> getter, Action configChanged, string name, string desc) => configWithDesc(customConfig, getter, configChanged, name, new ConfigDescription(desc));

                void updateAllSpawnConfigs()
                {
                    foreach (SpawnSystem spawnSystem in UnityEngine.Object.FindObjectsByType<SpawnSystem>(FindObjectsSortMode.None))
                    {
                        foreach (SpawnSystemList spawnList in spawnSystem.m_spawnLists)
                        {
                            foreach (SpawnSystem.SpawnData spawnData in spawnList.m_spawners)
                            {
                                if (creature.Prefab == spawnData.m_prefab)
                                {
                                    creature.updateSpawnData(spawnData);
                                }
                            }
                        }
                    }
                }

                void updateAI()
                {
                    if (ObjectDB.instance)
                    {
                        foreach (BaseAI ai in UnityEngine.Object.FindObjectsByType<BaseAI>(FindObjectsSortMode.None))
                        {
                            creature.updateAi(ai);
                        }
                        creature.updateAi(creature.Prefab.GetComponent<BaseAI>());
                    }
                }

                cfg.Spawn.get = () => creature.CanSpawn ? SpawnOption.Default : SpawnOption.Disabled;
                cfg.CanBeTamed.get = () => creature.CanBeTamed ? Toggle.On : Toggle.Off;
                cfg.ConsumesItemName.get = () => creature.FoodItems;
                cfg.SpecificSpawnTime.get = () => creature.SpecificSpawnTime;
                cfg.RequiredAltitude.get = () => creature.RequiredAltitude;
                cfg.RequiredOceanDepth.get = () => creature.RequiredOceanDepth;
                cfg.RequiredGlobalKey.get = () => creature.RequiredGlobalKey;
                cfg.GroupSize.get = () => creature.GroupSize;
                cfg.Biome.get = () => creature.Biome;
                cfg.SpecificSpawnArea.get = () => creature.SpecificSpawnArea;
                cfg.RequiredWeather.get = () => creature.RequiredWeather;
                cfg.SpawnAltitude.get = () => creature.SpawnAltitude;
                cfg.CanHaveStars.get = () => creature.CanHaveStars ? Toggle.On : Toggle.Off;
                cfg.AttackImmediately.get = () => creature.AttackImmediately ? Toggle.On : Toggle.Off;
                cfg.CheckSpawnInterval.get = () => creature.CheckSpawnInterval;
                cfg.SpawnChance.get = () => creature.SpawnChance;
                cfg.ForestSpawn.get = () => creature.ForestSpawn;
                cfg.Maximum.get = () => creature.Maximum;
                cfg.Drops.get = () => DropOption.Default;
                cfg.CustomDrops.get = () => new DropList.SerializedDrops(creature.Drops, creature).ToString();

                // Health & Damage Modifiers defaults
                cfg.Health.get = () => creature.Prefab.GetComponent<Character>()?.m_health ?? creature.Health;
                cfg.RegenAllHpTime.get = () =>
                {
                    Character? ch = creature.Prefab.GetComponent<Character>();
                    if (ch == null) return creature.RegenAllHpTime;
                    FieldInfo? fi = typeof(Character).GetField("m_regenAllHPTime", BindingFlags.Public | BindingFlags.Instance)
                                   ?? typeof(Character).GetField("m_regenAllHpTime", BindingFlags.Public | BindingFlags.Instance);
                    return fi != null ? (float)fi.GetValue(ch) : creature.RegenAllHpTime;
                };
                cfg.BluntModifier.get = () => creature.BluntModifier;
                cfg.SlashModifier.get = () => creature.SlashModifier;
                cfg.PierceModifier.get = () => creature.PierceModifier;
                cfg.ChopModifier.get = () => creature.ChopModifier;
                cfg.PickaxeModifier.get = () => creature.PickaxeModifier;
                cfg.FireModifier.get = () => creature.FireModifier;
                cfg.FrostModifier.get = () => creature.FrostModifier;
                cfg.LightningModifier.get = () => creature.LightningModifier;
                cfg.PoisonModifier.get = () => creature.PoisonModifier;
                cfg.SpiritModifier.get = () => creature.SpiritModifier;

                ConfigurationManagerAttributes tameConfigVisibility = new();
                if (creature.IsConfigured(nameof(Creature.CanBeTamed)))
                {
                    config(cfg.CanBeTamed, cfg.CanBeTamed.get, () =>
                    {
                        tameConfigVisibility.Browsable = cfg.CanBeTamed.get() == Toggle.On;
                        reloadConfigDisplay();
                        updateAI();
                    }, "Can be tamed", "Decides, if the creature can be tamed.");
                }

                tameConfigVisibility.Browsable = cfg.CanBeTamed.get() == Toggle.On;
                if (creature.IsConfigured(nameof(Creature.FoodItems)))
                {
                    configWithDesc(cfg.ConsumesItemName, cfg.ConsumesItemName.get, updateAI, "Food items", new ConfigDescription("The items the creature consumes to get tame.", null, tameConfigVisibility));
                }

                ConfigurationManagerAttributes spawnConfigVisibility = new();
                ConfigurationManagerAttributes dropConfigVisibility = new();
                void spawnConfig<T>(CustomConfig<T> customConfig, Func<T> getter, string name, string desc, AcceptableValueBase? acceptableValues = null) => configWithDesc(customConfig, getter, updateAllSpawnConfigs, name, new ConfigDescription(desc, acceptableValues, spawnConfigVisibility));

                if (creature.IsConfigured(nameof(Creature.CanSpawn)))
                {
                    config(cfg.Spawn, cfg.Spawn.get, () =>
                    {
                        spawnConfigVisibility.Browsable = cfg.Spawn.get() == SpawnOption.Custom;
                        reloadConfigDisplay();
                        updateAllSpawnConfigs();
                    }, "Spawn", "Configures the spawn for the creature.");
                }

                spawnConfigVisibility.Browsable = cfg.Spawn.get() == SpawnOption.Custom;

                if (creature.IsConfigured(nameof(Creature.SpecificSpawnTime))) spawnConfig(cfg.SpecificSpawnTime, cfg.SpecificSpawnTime.get, "Spawn time", "Configures the time of day for the creature to spawn.");
                if (creature.IsConfigured(nameof(Creature.RequiredAltitude))) spawnConfig(cfg.RequiredAltitude, cfg.RequiredAltitude.get, "Required altitude", "Configures the altitude required for the creature to spawn.");
                if (creature.IsConfigured(nameof(Creature.RequiredOceanDepth))) spawnConfig(cfg.RequiredOceanDepth, cfg.RequiredOceanDepth.get, "Required ocean depth", "Configures the ocean depth required for the creature to spawn.");
                if (creature.IsConfigured(nameof(Creature.RequiredGlobalKey))) spawnConfig(cfg.RequiredGlobalKey, cfg.RequiredGlobalKey.get, "Required global key", "Configures the global key required for the creature to spawn.");
                if (creature.IsConfigured(nameof(Creature.GroupSize))) spawnConfig(cfg.GroupSize, cfg.GroupSize.get, "Group size", "Configures the size of the groups in which the creature spawns.");
                if (creature.IsConfigured(nameof(Creature.Biome))) spawnConfig(cfg.Biome, cfg.Biome.get, "Biome", "Configures the biome required for the creature to spawn.");
                if (creature.IsConfigured(nameof(Creature.SpecificSpawnArea))) spawnConfig(cfg.SpecificSpawnArea, cfg.SpecificSpawnArea.get, "Spawn area", "Configures if the creature spawns more towards the center or the edge of the biome.");
                if (creature.IsConfigured(nameof(Creature.RequiredWeather))) spawnConfig(cfg.RequiredWeather, cfg.RequiredWeather.get, "Required weather", "Configures the weather required for the creature to spawn.");
                if (creature.IsConfigured(nameof(Creature.SpawnAltitude))) spawnConfig(cfg.SpawnAltitude, cfg.SpawnAltitude.get, "Spawn altitude", "Configures the height from the ground in which the creature will spawn.");
                if (creature.IsConfigured(nameof(Creature.CanHaveStars))) spawnConfig(cfg.CanHaveStars, cfg.CanHaveStars.get, "Can have stars", "If the creature can have stars.");
                if (creature.IsConfigured(nameof(Creature.AttackImmediately))) spawnConfig(cfg.AttackImmediately, cfg.AttackImmediately.get, "Hunt player", "Makes the creature immediately hunt down the player after it spawns.");
                if (creature.IsConfigured(nameof(Creature.CheckSpawnInterval))) spawnConfig(cfg.CheckSpawnInterval, cfg.CheckSpawnInterval.get, "Maximum spawn interval", "Configures the timespan that Valheim has to make the creature spawn.");
                if (creature.IsConfigured(nameof(Creature.SpawnChance))) spawnConfig(cfg.SpawnChance, cfg.SpawnChance.get, "Spawn chance", "Sets the chance for the creature to be spawned, every time Valheim checks the spawn.");
                if (creature.IsConfigured(nameof(Creature.ForestSpawn))) spawnConfig(cfg.ForestSpawn, cfg.ForestSpawn.get, "Forest condition", "If the creature can spawn in forests or cannot spawn in forests. Or both.");
                if (creature.IsConfigured(nameof(Creature.Maximum))) spawnConfig(cfg.Maximum, cfg.Maximum.get, "Maximum creature count", "The maximum number of this creature near the player, before Valheim stops spawning it in. Setting this lower than the upper limit of the group size does not make sense.");

                if (creature.IsConfigured(nameof(Creature.Drops)))
                {
                    config(cfg.Drops, cfg.Drops.get, () =>
                    {
                        dropConfigVisibility.Browsable = cfg.Drops.get() == DropOption.Custom;
                        reloadConfigDisplay();
                        DropList.UpdateDrops(creature);
                    }, "Drops", "Configures the drops for the creature.");

                    dropConfigVisibility.Browsable = cfg.Drops.get() == DropOption.Custom;

                    configWithDesc(cfg.CustomDrops, cfg.CustomDrops.get, () => DropList.UpdateDrops(creature), "Drop config", new ConfigDescription("", null, dropConfigVisibility));
                }

                void updateCharacterStats()
                {
                    creature.applyCharacterStats(creature.Prefab.GetComponent<Character>());
                    foreach (Character ch in UnityEngine.Object.FindObjectsByType<Character>(FindObjectsSortMode.None))
                    {
                        if (ch.gameObject.name.StartsWith(creature.Prefab.name))
                        {
                            creature.applyCharacterStats(ch);
                        }
                    }
                }

                if (creature.IsConfigured(nameof(Creature.Health)))
                    config(cfg.Health, cfg.Health.get, updateCharacterStats, "Health", "Maximum health points of the creature.");

                if (creature.IsConfigured(nameof(Creature.RegenAllHpTime)))
                    config(cfg.RegenAllHpTime, cfg.RegenAllHpTime.get, updateCharacterStats, "Regen All HP Time", "Time in seconds to fully regenerate HP (0 = disabled).");

                // Damage Modifiers
                if (creature.IsConfigured(nameof(Creature.BluntModifier)))
                    config(cfg.BluntModifier, cfg.BluntModifier.get, updateCharacterStats, "Damage Modifier: Blunt", "Resistance to blunt damage.");
                if (creature.IsConfigured(nameof(Creature.SlashModifier)))
                    config(cfg.SlashModifier, cfg.SlashModifier.get, updateCharacterStats, "Damage Modifier: Slash", "Resistance to slash damage.");
                if (creature.IsConfigured(nameof(Creature.PierceModifier)))
                    config(cfg.PierceModifier, cfg.PierceModifier.get, updateCharacterStats, "Damage Modifier: Pierce", "Resistance to pierce damage.");
                if (creature.IsConfigured(nameof(Creature.ChopModifier)))
                    config(cfg.ChopModifier, cfg.ChopModifier.get, updateCharacterStats, "Damage Modifier: Chop", "Resistance to chop damage.");
                if (creature.IsConfigured(nameof(Creature.PickaxeModifier)))
                    config(cfg.PickaxeModifier, cfg.PickaxeModifier.get, updateCharacterStats, "Damage Modifier: Pickaxe", "Resistance to pickaxe damage.");
                if (creature.IsConfigured(nameof(Creature.FireModifier)))
                    config(cfg.FireModifier, cfg.FireModifier.get, updateCharacterStats, "Damage Modifier: Fire", "Resistance to fire damage.");
                if (creature.IsConfigured(nameof(Creature.FrostModifier)))
                    config(cfg.FrostModifier, cfg.FrostModifier.get, updateCharacterStats, "Damage Modifier: Frost", "Resistance to frost damage.");
                if (creature.IsConfigured(nameof(Creature.LightningModifier)))
                    config(cfg.LightningModifier, cfg.LightningModifier.get, updateCharacterStats, "Damage Modifier: Lightning", "Resistance to lightning damage.");
                if (creature.IsConfigured(nameof(Creature.PoisonModifier)))
                    config(cfg.PoisonModifier, cfg.PoisonModifier.get, updateCharacterStats, "Damage Modifier: Poison", "Resistance to poison damage.");
                if (creature.IsConfigured(nameof(Creature.SpiritModifier)))
                    config(cfg.SpiritModifier, cfg.SpiritModifier.get, updateCharacterStats, "Damage Modifier: Spirit", "Resistance to spirit damage.");
            }

            if (SaveOnConfigSet)
            {
                plugin.Config.SaveOnConfigSet = true;
                plugin.Config.Save();
            }

            // Configs globais de HP Display — registradas uma única vez mesmo com múltiplos mods
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
            {
                config.Value = new Range(min, max);
            }
        }

        private static void drawConfigTable(ConfigEntryBase cfg)
        {
            bool locked = cfg.Description.Tags.Select(a => a.GetType().Name == "ConfigurationManagerAttributes" ? (bool?)a.GetType().GetField("ReadOnly")?.GetValue(a) : null).FirstOrDefault(v => v != null) ?? false;

            List<KeyValuePair<string, Drop>> newDrops = new();
            bool wasUpdated = false;

            int RightColumnWidth = (int)(configManager?.GetType().GetProperty("RightColumnWidth", BindingFlags.Instance | BindingFlags.NonPublic)!.GetGetMethod(true).Invoke(configManager, Array.Empty<object>()) ?? 130);

            GUILayout.BeginVertical();
            foreach (KeyValuePair<string, Drop> drop in new DropList.SerializedDrops((string)cfg.BoxedValue).Drops)
            {
                GUILayout.BeginHorizontal();

                int minAmount = Mathf.RoundToInt(drop.Value.Amount.min);
                if (int.TryParse(GUILayout.TextField(minAmount.ToString(), new GUIStyle(GUI.skin.textField) { fixedWidth = 35 }), out int newMinAmount) && newMinAmount != minAmount && !locked)
                {
                    minAmount = newMinAmount;
                    wasUpdated = true;
                }

                GUILayout.Label(" - ", new GUIStyle(GUI.skin.label) { fixedWidth = 14 });

                int maxAmount = Mathf.RoundToInt(drop.Value.Amount.max);
                if (int.TryParse(GUILayout.TextField(maxAmount.ToString(), new GUIStyle(GUI.skin.textField) { fixedWidth = 35 }), out int newMaxAmount) && newMaxAmount != maxAmount && !locked)
                {
                    maxAmount = newMaxAmount;
                    wasUpdated = true;
                }

                GUILayout.Label(" ", new GUIStyle(GUI.skin.label) { fixedWidth = 10 });

                string newItemName = GUILayout.TextField(drop.Key, new GUIStyle(GUI.skin.textField) { fixedWidth = RightColumnWidth - 35 - 14 - 35 - 10 - 21 - 18 });
                string itemName = locked ? drop.Key : newItemName;
                wasUpdated = wasUpdated || itemName != drop.Key;

                bool removed = GUILayout.Button("x", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }) && !locked;

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();

                float chance = drop.Value.DropChance;
                if (float.TryParse(GUILayout.TextField(chance.ToString(CultureInfo.InvariantCulture), new GUIStyle(GUI.skin.textField) { fixedWidth = 45 }), out float newChance) && Math.Abs(newChance - chance) > 0.00001f && !locked)
                {
                    chance = newChance;
                    wasUpdated = true;
                }
                GUILayout.Label("% ");

                string oldTooltip = GUI.tooltip;

                bool multiplyPerLevel = drop.Value.MultiplyDropByLevel;
                bool newMultiplyPerLevel = GUILayout.Toggle(multiplyPerLevel, new GUIContent(multiplyPerLevel ? "per level" : "fixed", "Loot is multiplied by the creature's level."));
                if (newMultiplyPerLevel != multiplyPerLevel && !locked)
                {
                    multiplyPerLevel = newMultiplyPerLevel;
                    wasUpdated = true;
                }

                bool perPlayer = drop.Value.DropOnePerPlayer;
                bool newPerPlayer = GUILayout.Toggle(perPlayer, new GUIContent(perPlayer ? "per player" : "independent", "Drops one per player."));
                if (newPerPlayer != perPlayer && !locked)
                {
                    perPlayer = newPerPlayer;
                    wasUpdated = true;
                }

                if (GUI.tooltip != oldTooltip)
                {
                    Vector3 mouse = UnityEngine.Input.mousePosition;
                    GUI.Label(new Rect(mouse.x, mouse.y, 100, 35), GUI.tooltip);
                }

                if (removed)
                {
                    wasUpdated = true;
                }
                else
                {
                    Drop newDrop = new()
                    {
                        Amount = new Range(minAmount, maxAmount),
                        DropChance = chance,
                        MultiplyDropByLevel = multiplyPerLevel,
                        DropOnePerPlayer = perPlayer,
                    };
                    newDrops.Add(new KeyValuePair<string, Drop>(itemName, newDrop));
                }

                if (GUILayout.Button("+", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }) && !locked)
                {
                    wasUpdated = true;
                    newDrops.Add(new KeyValuePair<string, Drop>("", new Drop()));
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            if (wasUpdated)
            {
                cfg.BoxedValue = new DropList.SerializedDrops(newDrops).ToString();
            }
        }

        private void updateAi(BaseAI ai)
        {
            CreatureConfig cfg = creatureConfigs[this];
            if (ai.GetComponent<Tameable>() != (cfg.CanBeTamed.get() == Toggle.On))
            {
                if (cfg.CanBeTamed.get() == Toggle.On)
                {
                    ai.m_tamable = ai.gameObject.AddComponent<Tameable>();
                }
                else
                {
                    Object.Destroy(ai.m_tamable);
                    ai.m_tamable = null;
                }
            }

            if (ai is MonsterAI monsterAI)
            {
                monsterAI.m_consumeItems.Clear();
                string[] items = cfg.ConsumesItemName.get().Split(',');
                foreach (string itemName in items)
                {
                    ItemDrop? item = ObjectDB.instance.GetItemPrefab(itemName.Trim())?.GetComponent<ItemDrop>();
                    if (item is not null)
                    {
                        monsterAI.m_consumeItems.Add(item);
                    }
                }
            }
        }

        internal static void UpdateCreatures(ObjectDB __instance)
        {
            foreach (Creature registeredCreature in registeredCreatures)
            {
                if (registeredCreature.Prefab.TryGetComponent(out BaseAI ai))
                {
                    registeredCreature.updateAi(ai);
                }
                if (registeredCreature.Prefab.TryGetComponent(out Character character))
                {
                    registeredCreature.applyCharacterStats(character);
                }
            }
        }

        private void applyCharacterStats(Character? character)
        {
            if (character == null) return;
            CreatureConfig cfg = creatureConfigs[this];

            if (this.IsConfigured(nameof(this.Health)))
                character.m_health = cfg.Health.get();

            if (this.IsConfigured(nameof(this.RegenAllHpTime)))
            {
                FieldInfo? fi = typeof(Character).GetField("m_regenAllHPTime", BindingFlags.Public | BindingFlags.Instance)
                               ?? typeof(Character).GetField("m_regenAllHpTime", BindingFlags.Public | BindingFlags.Instance);
                fi?.SetValue(character, cfg.RegenAllHpTime.get());
            }

            if (this.IsConfigured(nameof(this.BluntModifier))) character.m_damageModifiers.m_blunt = cfg.BluntModifier.get();
            if (this.IsConfigured(nameof(this.SlashModifier))) character.m_damageModifiers.m_slash = cfg.SlashModifier.get();
            if (this.IsConfigured(nameof(this.PierceModifier))) character.m_damageModifiers.m_pierce = cfg.PierceModifier.get();
            if (this.IsConfigured(nameof(this.ChopModifier))) character.m_damageModifiers.m_chop = cfg.ChopModifier.get();
            if (this.IsConfigured(nameof(this.PickaxeModifier))) character.m_damageModifiers.m_pickaxe = cfg.PickaxeModifier.get();
            if (this.IsConfigured(nameof(this.FireModifier))) character.m_damageModifiers.m_fire = cfg.FireModifier.get();
            if (this.IsConfigured(nameof(this.FrostModifier))) character.m_damageModifiers.m_frost = cfg.FrostModifier.get();
            if (this.IsConfigured(nameof(this.LightningModifier))) character.m_damageModifiers.m_lightning = cfg.LightningModifier.get();
            if (this.IsConfigured(nameof(this.PoisonModifier))) character.m_damageModifiers.m_poison = cfg.PoisonModifier.get();
            if (this.IsConfigured(nameof(this.SpiritModifier))) character.m_damageModifiers.m_spirit = cfg.SpiritModifier.get();
        }

        internal static void ApplyCreatureStats(ZNetScene __instance)
        {
            foreach (Creature creature in registeredCreatures)
            {
                creature.applyCharacterStats(creature.Prefab.GetComponent<Character>());
            }
        }

        private static List<SpawnSystem.SpawnData> lastRegisteredSpawns = new();

        private void updateSpawnData(SpawnSystem.SpawnData spawnData)
        {
            CreatureConfig cfg = creatureConfigs[this];
            spawnData.m_enabled = cfg.Spawn.get() != SpawnOption.Disabled;
            spawnData.m_biome = cfg.Biome.get();
            spawnData.m_biomeArea = cfg.SpecificSpawnArea.get() switch
            {
                SpawnArea.Center => Heightmap.BiomeArea.Median,
                SpawnArea.Edge => Heightmap.BiomeArea.Edge,
                _ => Heightmap.BiomeArea.Everything,
            };
            spawnData.m_maxSpawned = cfg.Maximum.get();
            spawnData.m_spawnInterval = cfg.CheckSpawnInterval.get();
            spawnData.m_spawnChance = cfg.SpawnChance.get();
            spawnData.m_requiredGlobalKey = ((InternalName)typeof(GlobalKey).GetMember(cfg.RequiredGlobalKey.get().ToString())[0].GetCustomAttributes(typeof(InternalName)).First()).internalName;
            spawnData.m_requiredEnvironments = Enum.GetValues(typeof(Weather)).Cast<Weather>().Where(w => (w & cfg.RequiredWeather.get()) != Weather.None).Select(w => ((InternalName)typeof(Weather).GetMember(w.ToString())[0].GetCustomAttributes(typeof(InternalName)).First()).internalName).ToList();
            spawnData.m_groupSizeMin = (int)cfg.GroupSize.get().min;
            spawnData.m_groupSizeMax = (int)cfg.GroupSize.get().max;
            spawnData.m_spawnAtNight = cfg.SpecificSpawnTime.get() is SpawnTime.Always or SpawnTime.Night;
            spawnData.m_spawnAtDay = cfg.SpecificSpawnTime.get() is SpawnTime.Always or SpawnTime.Day;
            spawnData.m_minAltitude = cfg.RequiredAltitude.get().min;
            spawnData.m_maxAltitude = cfg.RequiredAltitude.get().max;
            spawnData.m_inForest = cfg.ForestSpawn.get() is Forest.Both or Forest.Yes;
            spawnData.m_outsideForest = cfg.ForestSpawn.get() is Forest.Both or Forest.No;
            spawnData.m_minOceanDepth = cfg.RequiredOceanDepth.get().min;
            spawnData.m_maxOceanDepth = cfg.RequiredOceanDepth.get().max;
            spawnData.m_huntPlayer = cfg.AttackImmediately.get() == Toggle.On;
            spawnData.m_groundOffset = cfg.SpawnAltitude.get();
            spawnData.m_maxLevel = cfg.CanHaveStars.get() == Toggle.On ? 3 : 1;
        }

        [HarmonyPriority(Priority.VeryHigh)]
        internal static void AddToSpawnSystem(SpawnSystem __instance)
        {
            SpawnSystemList spawnList = __instance.m_spawnLists.First();

            foreach (SpawnSystem.SpawnData spawnData in lastRegisteredSpawns)
            {
                spawnList.m_spawners.Remove(spawnData);
            }
            lastRegisteredSpawns.Clear();

            foreach (Creature creature in registeredCreatures)
            {
                SpawnSystem.SpawnData spawnData = new()
                {
                    m_name = creature.Prefab.name,
                    m_prefab = creature.Prefab,
                };
                creature.updateSpawnData(spawnData);
                lastRegisteredSpawns.Add(spawnData);
                spawnList.m_spawners.Add(spawnData);
            }
        }

        private static Localization? _english;

        private static Localization english => _english ??= LocalizationCache.ForLanguage("English");

        private static BaseUnityPlugin? _plugin;

        private static BaseUnityPlugin plugin
        {
            get
            {
                if (_plugin is null)
                {
                    IEnumerable<TypeInfo> types;
                    try
                    {
                        types = Assembly.GetExecutingAssembly().DefinedTypes.ToList();
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        types = e.Types.Where(t => t != null).Select(t => t.GetTypeInfo());
                    }
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
                    if (Assembly.GetExecutingAssembly().GetType("ServerSync.ConfigSync") is { } configSyncType)
                    {
                        _configSync = Activator.CreateInstance(configSyncType, plugin.Info.Metadata.GUID + " CreatureManager");
                        configSyncType.GetField("CurrentVersion").SetValue(_configSync, plugin.Info.Metadata.Version.ToString());
                        configSyncType.GetProperty("IsLocked")!.SetValue(_configSync, true);
                    }
                    else
                    {
                        hasConfigSync = false;
                    }
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

        private static ConfigEntry<T> pluginConfig<T>(string group, string name, T value, string description) => pluginConfig(group, name, value, new ConfigDescription(description));
    }

    [PublicAPI]
    public class LocalizeKey
    {
        private static readonly List<LocalizeKey> keys = new();

        public readonly string Key;
        public readonly Dictionary<string, string> Localizations = new();

        public LocalizeKey(string key)
        {
            Key = key.Replace("$", "");
            keys.Add(this);
        }

        public void Alias(string alias)
        {
            Localizations.Clear();
            if (!alias.Contains("$"))
            {
                alias = $"${alias}";
            }
            Localizations["alias"] = alias;

            if (Localization.m_instance != null)
            {
                Localization.instance.AddWord(Key, Localization.instance.Localize(alias));
            }
        }

        public LocalizeKey English(string key) => addForLang("English", key);
        public LocalizeKey Swedish(string key) => addForLang("Swedish", key);
        public LocalizeKey French(string key) => addForLang("French", key);
        public LocalizeKey Italian(string key) => addForLang("Italian", key);
        public LocalizeKey German(string key) => addForLang("German", key);
        public LocalizeKey Spanish(string key) => addForLang("Spanish", key);
        public LocalizeKey Russian(string key) => addForLang("Russian", key);
        public LocalizeKey Romanian(string key) => addForLang("Romanian", key);
        public LocalizeKey Bulgarian(string key) => addForLang("Bulgarian", key);
        public LocalizeKey Macedonian(string key) => addForLang("Macedonian", key);
        public LocalizeKey Finnish(string key) => addForLang("Finnish", key);
        public LocalizeKey Danish(string key) => addForLang("Danish", key);
        public LocalizeKey Norwegian(string key) => addForLang("Norwegian", key);
        public LocalizeKey Icelandic(string key) => addForLang("Icelandic", key);
        public LocalizeKey Turkish(string key) => addForLang("Turkish", key);
        public LocalizeKey Lithuanian(string key) => addForLang("Lithuanian", key);
        public LocalizeKey Czech(string key) => addForLang("Czech", key);
        public LocalizeKey Hungarian(string key) => addForLang("Hungarian", key);
        public LocalizeKey Slovak(string key) => addForLang("Slovak", key);
        public LocalizeKey Polish(string key) => addForLang("Polish", key);
        public LocalizeKey Dutch(string key) => addForLang("Dutch", key);
        public LocalizeKey Portuguese_European(string key) => addForLang("Portuguese_European", key);
        public LocalizeKey Portuguese_Brazilian(string key) => addForLang("Portuguese_Brazilian", key);
        public LocalizeKey Chinese(string key) => addForLang("Chinese", key);
        public LocalizeKey Japanese(string key) => addForLang("Japanese", key);
        public LocalizeKey Korean(string key) => addForLang("Korean", key);
        public LocalizeKey Hindi(string key) => addForLang("Hindi", key);
        public LocalizeKey Thai(string key) => addForLang("Thai", key);
        public LocalizeKey Abenaki(string key) => addForLang("Abenaki", key);
        public LocalizeKey Croatian(string key) => addForLang("Croatian", key);
        public LocalizeKey Georgian(string key) => addForLang("Georgian", key);
        public LocalizeKey Greek(string key) => addForLang("Greek", key);
        public LocalizeKey Serbian(string key) => addForLang("Serbian", key);
        public LocalizeKey Ukrainian(string key) => addForLang("Ukrainian", key);

        private LocalizeKey addForLang(string lang, string value)
        {
            Localizations[lang] = value;

            if (Localization.m_instance != null)
            {
                if (Localization.instance.GetSelectedLanguage() == lang)
                {
                    Localization.instance.AddWord(Key, value);
                }
                else if (lang == "English" && !Localization.instance.m_translations.ContainsKey(Key))
                {
                    Localization.instance.AddWord(Key, value);
                }
            }

            return this;
        }

        [HarmonyPriority(Priority.LowerThanNormal)]
        internal static void AddLocalizedKeys(Localization __instance, string language)
        {
            foreach (LocalizeKey key in keys)
            {
                if (key.Localizations.TryGetValue(language, out string Translation) || key.Localizations.TryGetValue("English", out Translation))
                {
                    __instance.AddWord(key.Key, Translation);
                }
                else if (key.Localizations.TryGetValue("alias", out string alias))
                {
                    __instance.AddWord(key.Key, __instance.Localize(alias));
                }
            }
        }
    }
    public static class LocalizationCache
    {
        private static readonly Dictionary<string, Localization> localizations = new();

        internal static void LocalizationPostfix(Localization __instance, string language)
        {
            if (localizations.FirstOrDefault(l => l.Value == __instance).Key is { } oldValue)
            {
                localizations.Remove(oldValue);
            }

            if (!localizations.ContainsKey(language))
            {
                localizations.Add(language, __instance);
            }
        }

        public static Localization ForLanguage(string? language = null)
        {
            if (localizations.TryGetValue(language ?? PlayerPrefs.GetString("language", "English"),
                out Localization localization))
            {
                return localization;
            }

            localization = new Localization();
            if (language is not null)
            {
                localization.SetupLanguage(language);
            }

            return localization;
        }
    }

    public static class PrefabManager
    {
        static PrefabManager()
        {
            Harmony harmony = new("org.bepinex.helpers.CreatureManager");
            harmony.Patch(AccessTools.DeclaredMethod(typeof(ZNetScene), nameof(ZNetScene.Awake)), new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PrefabManager), nameof(Patch_ZNetSceneAwake))));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(ZNetScene), nameof(ZNetScene.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Creature), nameof(Creature.ApplyCreatureStats))));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(ZNetScene), nameof(ZNetScene.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Creature.DropList), nameof(Creature.DropList.AddDropsToCreature))));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(SpawnSystem), nameof(SpawnSystem.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Creature), nameof(Creature.AddToSpawnSystem))));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(ObjectDB), nameof(ObjectDB.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Creature), nameof(Creature.UpdateCreatures))));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(FejdStartup), nameof(FejdStartup.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Creature), nameof(Creature.Patch_FejdStartup))));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(Localization), nameof(Localization.LoadCSV)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(LocalizeKey), nameof(LocalizeKey.AddLocalizedKeys))));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(Localization), nameof(Localization.SetupLanguage)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(LocalizationCache), nameof(LocalizationCache.LocalizationPostfix))));
            if (!typeof(Heightmap.Biome).GetCustomAttributes(typeof(FlagsAttribute), false).Any())
            {
                // ReSharper disable once PossibleMistakenCallToGetType.2
                harmony.Patch(AccessTools.Method(typeof(Heightmap.Biome).GetType(), "GetCustomAttributes", new[] { typeof(Type), typeof(bool) }), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PrefabManager), nameof(BiomeIsFlags))));
            }
        }

        private static void BiomeIsFlags(Type __instance, Type attributeType, ref object[] __result)
        {
            if (__instance == typeof(Heightmap.Biome) && attributeType == typeof(FlagsAttribute))
            {
                __result = new object[] { new FlagsAttribute() };
            }
        }

        private struct BundleId
        {
            [UsedImplicitly]
            public string assetBundleFileName;
            [UsedImplicitly]
            public string folderName;
        }

        private static readonly Dictionary<BundleId, AssetBundle> bundleCache = new();

        public static AssetBundle RegisterAssetBundle(string assetBundleFileName, string folderName = "assets")
        {
            BundleId id = new() { assetBundleFileName = assetBundleFileName, folderName = folderName };
            if (!bundleCache.TryGetValue(id, out AssetBundle assets))
            {
                assets = bundleCache[id] = Resources.FindObjectsOfTypeAll<AssetBundle>().FirstOrDefault(a => a.name == assetBundleFileName) ?? AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name + $".{folderName}." + assetBundleFileName));
            }
            return assets;
        }

        private static readonly List<GameObject> prefabs = new();

        public static GameObject RegisterPrefab(AssetBundle assets, string prefabName)
        {
            GameObject prefab = assets.LoadAsset<GameObject>(prefabName);

            prefabs.Add(prefab);

            return prefab;
        }

        [HarmonyPriority(Priority.VeryHigh)]
        private static void Patch_ZNetSceneAwake(ZNetScene __instance)
        {
            foreach (GameObject prefab in prefabs)
            {
                __instance.m_prefabs.Add(prefab);
            }
        }
    }

}

