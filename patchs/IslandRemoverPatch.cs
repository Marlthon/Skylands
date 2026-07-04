using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Skylands.Patches
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    [HarmonyPatch(typeof(Player), nameof(Player.Interact))]
    public static class IslandRemover_Interact_Patch
    {
        private static bool Prefix(Player __instance, GameObject go)
        {
            if (go == null || go.GetComponent<IslandRemover>() == null) return true;

            var rootNView = go.GetComponentInParent<ZNetView>();
            if (rootNView == null || !rootNView.IsValid()) return false;

            var piece = rootNView.GetComponent<Piece>();
            if (piece == null) return false;

            long creatorID = piece.GetCreator();
            long playerPermanentID = __instance.GetPlayerID();
            bool isCreator = (creatorID != 0L && creatorID == playerPermanentID);
            bool isAdmin = SkylandsPlugin.AllowAdminRemoveIslands.Value && ZNet.instance.LocalPlayerIsAdminOrHost();

            if (!isCreator && !isAdmin)
            {
                __instance.Message(MessageHud.MessageType.TopLeft, "This floating crystal does not belong to you.");
                return false;
            }

            return true;
        }
    }

    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    [HarmonyPatch(typeof(Player), "Update")]
    public static class IslandRemover_Update_Patch
    {
        static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return;
            long playerId = __instance.GetPlayerID();
            if (!IslandRemover.playersAwaitingConfirmation.Contains(playerId)) return;

            GameObject hoveredObject = __instance.m_hovering;

            if (hoveredObject == null || hoveredObject.GetComponent<IslandRemover>() == null)
            {
                IslandRemover.CancelConfirmation(playerId);
                return;
            }

            if (ZInput.GetKeyDown(KeyCode.P))
            {
                if (IslandRemover.TryConfirmDismantling(playerId))
                {
                    DismantleHoveredIsland(__instance);
                }
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                IslandRemover.CancelConfirmation(playerId);
            }
        }
        private static void DismantleHoveredIsland(Player player)
        {
            GameObject hoveredObject = player.m_hovering;
            if (hoveredObject == null || hoveredObject.GetComponent<IslandRemover>() == null)
            {
                IslandRemover.CancelConfirmation(player.GetPlayerID());
                return;
            }
            var rootNView = hoveredObject.GetComponentInParent<ZNetView>();
            if (rootNView == null || !rootNView.IsValid()) return;

            var piece = rootNView.GetComponent<Piece>();
            if (piece != null)
            {

                Vector3 originalPosition = hoveredObject.transform.position;

                float alturaAdicional = 1.8f;

                Vector3 posicaoEfeito = originalPosition + Vector3.up * alturaAdicional;

                GameObject vfxPrefab = ZNetScene.instance.GetPrefab("vfx_destroyed_skylands");
                if (vfxPrefab != null)
                {

                    Object.Instantiate(vfxPrefab, posicaoEfeito, Quaternion.identity);
                }

                GameObject sfxPrefab = ZNetScene.instance.GetPrefab("sfx_destroyed_skylands");
                if (sfxPrefab != null)
                {

                    Object.Instantiate(sfxPrefab, posicaoEfeito, Quaternion.identity);
                }

                piece.DropResources();
                ZNetScene.instance.Destroy(rootNView.gameObject);
                player.Message(MessageHud.MessageType.TopLeft, "Floating Island successfully dismantled.");
            }
        }
    }
}