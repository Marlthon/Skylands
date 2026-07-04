using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace Skylands
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    [HarmonyPatch(typeof(Player), "Update")]
    public static class BiomeControlPatch
    {
        static void Postfix(Player __instance)
        {
            if (!__instance || Player.m_localPlayer != __instance) return;

            GameObject hover = __instance.m_hovering;
            if (!IsCustomizableIsland(hover, out var biomeControl, out var nview)) return;

            // só aceita input quando o menu de customização está ativo
            if (biomeControl == null || !biomeControl.IsInCustomization) return;
            if (nview == null || !nview.IsValid() || !nview.IsOwner()) return;

            // teclas
            if (Pressed(biomeControl.TreeToggleKey)) ToggleIndex(nview, "biome_trees_index", biomeControl.ArvoresObjects?.Count ?? 0, "Trees");
            if (Pressed(biomeControl.WaterWellToggleKey)) ToggleIndex(nview, "biome_waterwell_index", biomeControl.WaterWellObjects?.Count ?? 0, "WaterWell");
            if (Pressed(biomeControl.LampPostsToggleKey)) ToggleIndex(nview, "biome_lampposts_index", biomeControl.LampPostsObjects?.Count ?? 0, "LampPosts");
            if (Pressed(biomeControl.TextureToggleKey)) CycleMaterial(nview, biomeControl);
        }

        private static bool IsCustomizableIsland(GameObject hoverObject, out BiomeControl biomeControl, out ZNetView nview)
        {
            biomeControl = null;
            nview = null;
            if (!hoverObject) return false;

            // pega o BiomeControl em qualquer pai do objeto hovered
            biomeControl = hoverObject.GetComponentInParent<BiomeControl>();
            if (biomeControl == null) return false;

            // *** AQUI ESTÁ O PULO DO GATO ***
            // Em vez de pegar o ZNetView do objeto hovered, pegamos do PAI da ilha
            nview = biomeControl.GetComponentInParent<ZNetView>();
            if (nview == null || !nview.IsValid()) return false;

            return true;
        }

        private static bool Pressed(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            if (Enum.TryParse<KeyCode>(key, true, out var kc))
            {
                // funciona com Input e com ZInput (Valheim)
                return Input.GetKeyDown(kc) || ZInput.GetKeyDown(kc);
            }
            return false;
        }

        private static void ToggleIndex(ZNetView nview, string zdoKey, int count, string label)
        {
            if (count <= 1) return;

            var zdo = nview.GetZDO();
            int currentIndex = zdo.GetInt(zdoKey, 0);
            int next = (currentIndex + 1) % count;

            zdo.Set(zdoKey, next);
            nview.InvokeRPC(ZNetView.Everybody, "RPC_OnBiomeChanged");

            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"{label}: {(next == 0 ? "On" : "Off")}");
        }

        private static void CycleMaterial(ZNetView nview, BiomeControl biomeControl)
        {
            int count = biomeControl.materiaisDoBioma?.Count ?? 0;
            if (count <= 0) return;

            var zdo = nview.GetZDO();
            int currentIndex = zdo.GetInt("biome_material_index", 0);
            int nextIndex = (currentIndex + 1) % count;

            zdo.Set("biome_material_index", nextIndex);
            nview.InvokeRPC(ZNetView.Everybody, "RPC_OnBiomeChanged");
            biomeControl.ForceUpdate();

            string materialName = biomeControl.materiaisDoBioma[nextIndex]?.name ?? "Unknown";
            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"Texture: {materialName}");
        }
    }
}
