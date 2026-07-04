    using BepInEx;
    using BepInEx.Configuration;
    using BepInEx.Logging;
    using HarmonyLib;
    using HarmonyLibs;
    using LocationManager;
    using PieceManager;
    using ServerSync;
    using ItemManager;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;

namespace Skylands
{

    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class SkylandsPlugin : BaseUnityPlugin
    {
        internal const string ModName = "Skylands";
        internal const string ModVersion = "0.0.6";
        internal const string Author = "marlthon";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource SkylandsLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);
                
        public static ConfigEntry<bool> AllowAdminRemoveIslands;

        private static readonly ConfigSync ConfigSync = new(ModGUID)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion, ModRequired = true };

        // DEUS SEJA LOUVADO!
        public void Awake()
        {
            _serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
            AllowAdminRemoveIslands = config("General", "Allow Admin Remove Islands", true, "If true, server admins can deconstruct player-created islands.");

            HarmonyCore.Instance.Init("SkyLands");
            HarmonyCore.Instance.Ciano("Marlthon Mods");
            HarmonyCore.Instance.Verde("Download more mods at marlthon.com");

            BuildingZonePatch.Init(_harmony);

            #region SKYLANDS

            BuildPiece SL_Isle01 = new("mar_skylands", "SL_Isle01");
            SL_Isle01.Crafting.Set(PieceManager.CraftingTable.None);
            SL_Isle01.Category.Set("Skylands");
            SL_Isle01.SpecialProperties.AdminOnly = false;
            SL_Isle01.Name.Portuguese_Brazilian("Skylands 01");
            SL_Isle01.Name.English("Skylands 01");
            SL_Isle01.Description.Portuguese_Brazilian("Ilha Flutuante com 25 metros de raio.");
            SL_Isle01.Description.English("Floating Isle with a radius of 25 meters.");
            SL_Isle01.RequiredItems.Add("FloatingCrystal", 1, true);
            SL_Isle01.RequiredItems.Add("SurtlingCore", 10, true);
            SL_Isle01.RequiredItems.Add("Stone", 250, true);

            BuildPiece SL_Isle02 = new("mar_skylands", "SL_Isle02");
            SL_Isle02.Crafting.Set(PieceManager.CraftingTable.None);
            SL_Isle02.Category.Set("Skylands");
            SL_Isle02.SpecialProperties.AdminOnly = false;
            SL_Isle02.Name.Portuguese_Brazilian("Skylands 02");
            SL_Isle02.Name.English("Skylands 02");
            SL_Isle02.Description.Portuguese_Brazilian("Ilha Flutuante com 48 metros de raio.");
            SL_Isle02.Description.English("Floating Isle with a radius of 48 meters.");
            SL_Isle02.RequiredItems.Add("FloatingCrystal", 1, true);
            SL_Isle02.RequiredItems.Add("SurtlingCore", 10, true);
            SL_Isle02.RequiredItems.Add("Stone", 350, true);

            BuildPiece SL_Isle03 = new("mar_skylands", "SL_Isle03");
            SL_Isle03.Crafting.Set(PieceManager.CraftingTable.None);
            SL_Isle03.Category.Set("Skylands");
            SL_Isle03.SpecialProperties.AdminOnly = false;
            SL_Isle03.Name.Portuguese_Brazilian("Skylands 03");
            SL_Isle03.Name.English("Skylands 03");
            SL_Isle03.Description.Portuguese_Brazilian("Ilha Flutuante com 58 metros de raio.");
            SL_Isle03.Description.English("Floating Isle with a radius of 58 meters.");
            SL_Isle03.RequiredItems.Add("FloatingCrystal", 1, true);
            SL_Isle03.RequiredItems.Add("SurtlingCore", 10, true);
            SL_Isle03.RequiredItems.Add("Stone", 400, true);

            BuildPiece SL_Isle04 = new("mar_skylands", "SL_Isle04");
            SL_Isle04.Crafting.Set(PieceManager.CraftingTable.None);
            SL_Isle04.Category.Set("Skylands");
            SL_Isle04.SpecialProperties.AdminOnly = false;
            SL_Isle04.Name.Portuguese_Brazilian("Skylands 04");
            SL_Isle04.Name.English("Skylands 04");
            SL_Isle04.Description.Portuguese_Brazilian("Ilha Flutuante com 64 metros de raio.");
            SL_Isle04.Description.English("Floating Island with a radius of 64 meters.");
            SL_Isle04.RequiredItems.Add("FloatingCrystal", 1, true);
            SL_Isle04.RequiredItems.Add("SurtlingCore", 10, true);
            SL_Isle04.RequiredItems.Add("Stone", 500, true);

            BuildPiece SL_Isle05 = new("mar_skylands", "SL_Isle05");
            SL_Isle05.Crafting.Set(PieceManager.CraftingTable.None);
            SL_Isle05.Category.Set("Skylands");
            SL_Isle05.SpecialProperties.AdminOnly = false;
            SL_Isle05.Name.Portuguese_Brazilian("Skylands 05");
            SL_Isle05.Name.English("Skylands 05");
            SL_Isle05.Description.Portuguese_Brazilian("Ilha Flutuante com 80 metros de raio.");
            SL_Isle05.Description.English("Floating Island with a radius of 80 meters.");
            SL_Isle05.RequiredItems.Add("FloatingCrystal", 1, true);
            SL_Isle05.RequiredItems.Add("SurtlingCore", 10, true);
            SL_Isle05.RequiredItems.Add("Stone", 600, true);

            BuildPiece SL_Isle06 = new("mar_skylands", "SL_Isle06");
            SL_Isle06.Crafting.Set(PieceManager.CraftingTable.None);
            SL_Isle06.Category.Set("Skylands");
            SL_Isle06.SpecialProperties.AdminOnly = false;
            SL_Isle06.Name.Portuguese_Brazilian("Skylands 06");
            SL_Isle06.Name.English("Skylands 06");
            SL_Isle06.Description.Portuguese_Brazilian("Arquipélago Flutuante com 75 metros de raio.");
            SL_Isle06.Description.English("Floating Archipelago with a radius of 75 meters.");
            SL_Isle06.RequiredItems.Add("FloatingCrystal", 1, true);
            SL_Isle06.RequiredItems.Add("SurtlingCore", 10, true);
            SL_Isle06.RequiredItems.Add("Stone", 600, true);
            SL_Isle06.RequiredItems.Add("Wood", 150, true);

            #endregion

            #region ITENS

            Item FloatingCrystal = new("mar_skylands", "FloatingCrystal");
            FloatingCrystal.Name.English("Floating Crystal");
            FloatingCrystal.Name.Portuguese_Brazilian("Cristal Flutuante");
            FloatingCrystal.Description.English("Floating Crystal");
            FloatingCrystal.Description.Portuguese_Brazilian("Cristal Flutuante");
            FloatingCrystal.DropsFrom.Add("Eikthyr", 0.05f, 1, 1);
            FloatingCrystal.DropsFrom.Add("gd_king", 0.05f, 1, 1);
            FloatingCrystal.DropsFrom.Add("Bonemass", 0.05f, 1, 1);
            FloatingCrystal.DropsFrom.Add("Dragon", 0.05f, 1, 1);
            FloatingCrystal.DropsFrom.Add("SeekerQueen", 0.05f, 1, 1);
            FloatingCrystal.DropsFrom.Add("Fader", 0.05f, 1, 1);
            FloatingCrystal.Configurable = Configurability.Recipe | Configurability.Drop;

            #endregion

            #region EFEITOS

            GameObject vfx_placeisle_skylands = ItemManager.PrefabManager.RegisterPrefab("mar_skylands", "vfx_placeisle_skylands");
            GameObject vfx_destroyed_skylands = ItemManager.PrefabManager.RegisterPrefab("mar_skylands", "vfx_destroyed_skylands");
            GameObject sfx_placeisle_skylands = ItemManager.PrefabManager.RegisterPrefab("mar_skylands", "sfx_placeisle_skylands");
            GameObject sfx_destroyed_skylands = ItemManager.PrefabManager.RegisterPrefab("mar_skylands", "sfx_destroyed_skylands");

            #endregion

            SetupWatcher();
            _harmony.PatchAll();
        }

        private void OnDestroy()
        {
            Config.Save();
        }
        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                SkylandsLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                SkylandsLogger.LogError($"There was an issue loading your {ConfigFileName}");
                SkylandsLogger.LogError("Please check your config entries for spelling and format!");
            }
        }

        private static ConfigEntry<bool>? _serverConfigLocked;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }
    }
}