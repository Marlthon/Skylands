using HarmonyLib;
using System.Reflection;
using UnityEngine;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public static class BuildingZonePatch
{
    private static bool _initialized = false;

    public static void Init(Harmony harmony)
    {
        if (_initialized) return;
        _initialized = true;
        harmony.PatchAll(typeof(BuildingZonePatch));
    }

    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.UpdateSupport))]
    [HarmonyPrefix]
    public static bool WearNTear_UpdateSupport_Prefix(WearNTear __instance)
    {
        if (BuildingZone.AllZones.Count == 0)
            return true;

        Vector3 position = __instance.transform.position;

        foreach (Collider zone in BuildingZone.AllZones)
        {
            if (zone != null && zone.bounds.Contains(position))
            {
                __instance.m_supports = true;
                return false;
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    [HarmonyPostfix]
    public static void Player_UpdatePlacementGhost_Postfix(Player __instance)
    {
        if (__instance.m_placementGhost == null) return;
        if (__instance.m_placementStatus == Player.PlacementStatus.Valid) return;

        Vector3 ghostPosition = __instance.m_placementGhost.transform.position;

        foreach (Collider zone in BuildingZone.AllZones)
        {
            if (zone != null && zone.bounds.Contains(ghostPosition))
            {
                __instance.m_placementStatus = Player.PlacementStatus.Valid;
                __instance.SetPlacementGhostValid(true);
                return;
            }
        }
    }
}