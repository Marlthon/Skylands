using UnityEngine;
using System.Collections.Generic;

namespace Skylands
{
    public class IslandRemover : MonoBehaviour, Hoverable, Interactable
    {
        public static HashSet<long> playersAwaitingConfirmation = new HashSet<long>();

        public string GetHoverName()
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer != null && playersAwaitingConfirmation.Contains(localPlayer.GetPlayerID()))
            {
                return "Confirmação de Desmontagem";
            }
            return "Controle da Ilha";
        }

        public string GetHoverText()
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer == null) return "";

            if (playersAwaitingConfirmation.Contains(localPlayer.GetPlayerID()))
            {
                return $"\n<color=red><b>ARE YOU SURE?</b></color>" +
                       $"\n[<color=yellow><b>P</b></color>] <color=orange>Yes, I'm sure.</color>" +
                       $"\n[<color=yellow><b>Esc</b></color>] <color=green>Cancelar</color>";
            }

            return "\n[<color=yellow><b>E</b></color>] Dismantle Island";
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (!hold)
            {
                if (user as Player != Player.m_localPlayer) return false;
                Player player = user as Player;
                long playerId = player.GetPlayerID();

                if (playersAwaitingConfirmation.Contains(playerId))
                {
                    playersAwaitingConfirmation.Remove(playerId);
                    return true;
                }

                playersAwaitingConfirmation.Add(playerId);
                return true;
            }

            return false;
        }

        public static void CancelConfirmation(long playerId)
        {
            if (playersAwaitingConfirmation.Contains(playerId))
            {
                playersAwaitingConfirmation.Remove(playerId);
            }
        }

        public static bool TryConfirmDismantling(long playerId)
        {
            if (playersAwaitingConfirmation.Contains(playerId))
            {
                playersAwaitingConfirmation.Remove(playerId);
                return true;
            }
            return false;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    }
}