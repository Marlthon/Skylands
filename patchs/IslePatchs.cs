using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Skylands
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    [HarmonyPatch]
    public static class IslePatch
    {
        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.UpdateSupport))]
        [HarmonyPrefix]
        public static bool WearNTear_UpdateSupport_Prefix(WearNTear __instance)
        {
            if (SupportZone.AllSupportZones.Count == 0)
            {
                return true;
            }
            Vector3 piecePosition = __instance.transform.position;
            foreach (Collider zone in SupportZone.AllSupportZones)
            {
                if (zone.bounds.Contains(piecePosition))
                {
                    __instance.m_supports = true;
                    return false;
                }
            }
            return true;
        }
    }

    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class Skylands_UpdatePlacementGhost_Patch
    {
        static void Postfix(Player __instance)
        {
            if (__instance.m_placementGhost == null) return;
            if (__instance.m_placementStatus == Player.PlacementStatus.Valid) return;

            if (Skylands.SupportZone.IsPlayerInside(__instance))
            {
                __instance.m_placementStatus = Player.PlacementStatus.Valid;
                __instance.SetPlacementGhostValid(true);
            }
        }
    }

}